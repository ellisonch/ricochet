using System;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Concurrent;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using Common.Logging;
using System.Diagnostics;

namespace Ricochet {
    /// <summary>
    /// An RPC Server represents a server capable of handling RPC requests.  
    /// A server can be made to understand different kinds of queries (through
    /// the use of <see cref="Register{T1, T2}(string, Func{T1, T2})"/>).
    /// 
    /// The server reserves all function names starting with "_" for internal 
    /// purposes.
    /// 
    /// A server currently does not release its resources if things go bad.
    /// </summary>
    public class Server {
        // Logger l = new Logger(Logger.Flag.Default);
        // private readonly Logger l = new Logger(LogManager.GetCurrentClassLogger());
        // private readonly ILog l;
        private readonly ILog l = LogManager.GetCurrentClassLogger();

        const int maxQueueSize = 2000;
        const int maxWorkerThreads = 32;
        private Semaphore workerSemaphore = new Semaphore(maxWorkerThreads, maxWorkerThreads);

        //const int minWorkerThreads = 2;
        //const int minCompletionPortThreads = 0; // 0 means don't change from default
        //const int maxWorkerThreads = 64;
        //const int maxCompletionPortThreads = 0; // 0 means don't change from default

        Serializer serializer;
        private readonly IPAddress address;
        private readonly int port;

        private ConcurrentDictionary<string, Func<Query, Response>> handlers = new ConcurrentDictionary<string, Func<Query, Response>>();

        /// <summary>
        /// Only contains non-null queries
        /// </summary>
        private IBoundedQueue<QueryWithDestination> workQueue = new BoundedQueue<QueryWithDestination>(maxQueueSize);
        private ConcurrentBag<ClientManager> clients = new ConcurrentBag<ClientManager>();
        private ConcurrentBag<Thread> workers = new ConcurrentBag<Thread>();

        /// <summary>
        /// Creates a new server that is not yet running.
        /// </summary>
        /// <param name="address">The IPAddress on which to start the server</param>
        /// <param name="port">The port to use</param>
        /// <param name="serializer">Serializer to use for serialization</param>
        public Server(IPAddress address, int port, Serializer serializer) {
            this.address = address;
            this.port = port;
            this.serializer = serializer;
            // l.Log(Logger.Flag.Info, "Configuring server as {0}:{1}", address, port);
            this.l.InfoFormat("Configuring server as {0}:{1}", address, port);

            int origMinWorkerThreads, origMinCompletionPortThreads, origMaxWorkerThreads, origMaxCompletionPortThreads;
            ThreadPool.GetMinThreads(out origMinWorkerThreads, out origMinCompletionPortThreads);
            ThreadPool.GetMaxThreads(out origMaxWorkerThreads, out origMaxCompletionPortThreads);

            this.l.WarnFormat("min/max worker threads: {0} / {1}", origMinWorkerThreads, origMaxWorkerThreads);
            this.l.WarnFormat("min/max completion port threads: {0} / {1}", origMinCompletionPortThreads, origMaxCompletionPortThreads);

            //int newMinWorkerThreads = (minWorkerThreads == 0 ? origMinWorkerThreads : minWorkerThreads);
            //int newMinCompletionPortThreads = (maxCompletionPortThreads == 0 ? origMinCompletionPortThreads : maxCompletionPortThreads);
            //int newMaxWorkerThreads = (maxWorkerThreads == 0 ? origMaxWorkerThreads : maxWorkerThreads);
            //int newMaxCompletionPortThreads = (maxCompletionPortThreads == 0 ? origMaxCompletionPortThreads : maxCompletionPortThreads);

            //ThreadPool.SetMinThreads(newMinWorkerThreads, newMinCompletionPortThreads);
            //ThreadPool.SetMaxThreads(newMaxWorkerThreads, newMaxCompletionPortThreads);
            

            //for (int i = 0; i < numWorkerThreads; i++) {
            //    var t = new Thread(this.DoWork);
            //    workers.Add(t);
            //    // t.Start();
            //}

            new Thread(this.WorkerHandler).Start();
            new Thread(this.CleanUp).Start();

            Register<int, int>("_ping", Ping);
            Register<bool, ServerStats>("_getStats", GetStats);
        }

        private void CleanUp() {
            while (true) {
                try {
                    ConcurrentBag<ClientManager> toAddBack = new ConcurrentBag<ClientManager>();
                    ClientManager client;
                    while (clients.TryTake(out client)) {
                        toAddBack.Add(client);
                    }

                    // replace the good entries
                    while (toAddBack.TryTake(out client)) {
                        if (!client.IsAlive) {
                            l.WarnFormat("Dropping a client");
                            continue;
                        }
                        clients.Add(client);
                    }
                    System.Threading.Thread.Sleep(2000);
                } catch (Exception e) {
                    l.ErrorFormat("Unexpected exception in Cleanup:", e);
                }
            }
        }

        /// <summary>
        /// Starts the server.  Blocks while server is running.
        /// </summary>
        public void Start() {
            try {
                TcpListener listener = new TcpListener(address, port);
                listener.Start();
                while (true) {
                    l.InfoFormat("Waiting for new client...");

                    var client = listener.AcceptTcpClient();
                    l.InfoFormat("Client connected.");
                    var clientHandler = new ClientManager(client, workQueue, serializer);
                    clients.Add(clientHandler);
                    clientHandler.Start();
                }
            } catch (AggregateException e) {
                l.ErrorFormat("Exception thrown: {0}", e.InnerException.Message);
            } catch (Exception e) {
                l.ErrorFormat("Exception thrown", e);
            }
        }

        /// <summary>
        /// Register a new RPC function.
        /// </summary>
        /// <typeparam name="T1">Input type</typeparam>
        /// <typeparam name="T2">Output type</typeparam>
        /// <param name="name">External name of function</param>
        /// <param name="fun">Function definition</param>
        public void Register<T1, T2>(string name, Func<T1, T2> fun) {
            if (handlers.ContainsKey(name)) {
                throw new Exception(String.Format("A handler is already registered for the name '{0}'", name));
            }
            handlers[name] = (Func<Query, Response>)((query) => {
                try {
                    T1 arg = serializer.Deserialize<T1>(query.MessageData);
                    T2 res = fun(arg);
                    return Response.CreateResponse<T2>(query, res, serializer);
                } catch (Exception e) {
                    l.WarnFormat("Something went wrong handling {0}:", e, name);
                    throw;
                }
            });
        }

        // TODO not limiting the number of pending requests
        private void WorkerHandler() {
            while (true) {
                try {
                    QueryWithDestination qwd;
                    // TODO if TryDequeue fails, we're probably being shut down
                    if (!workQueue.TryDequeue(out qwd)) {
                        l.WarnFormat("TryDequeue failed");
                        continue;
                    }
                    qwd.sw.Stop();
                    TimingHelper.Add("Work Queue", qwd.sw);
                    qwd.sw.Restart();
                    try {
                        workerSemaphore.WaitOne();
                        ThreadPool.QueueUserWorkItem(DoWork, qwd);
                    } catch (Exception) {
                        workerSemaphore.Release();
                        throw;
                    }
                } catch (Exception e) {
                    l.WarnFormat("Problem in WorkerHandler:", e);
                }
            }
        }

        private void DoWork(Object obj) {
            try {
                QueryWithDestination qwd = (QueryWithDestination)obj;
                qwd.sw.Stop();
                TimingHelper.Add("WorkerSpawn", qwd.sw);

                Stopwatch sw = Stopwatch.StartNew();
                Response response = GetResponseForQuery(qwd.Query);
                sw.Stop();
                TimingHelper.Add("GetResponse", sw);

                byte[] bytes = serializer.SerializeResponse(response);
                // l.Log(Logger.Flag.Warning, "Response calculated by thread {0}", Thread.CurrentThread.ManagedThreadId);
                sw.Restart();
                qwd.Destination.EnqueueIfRoom(new Tuple<byte[], Stopwatch>(bytes, sw));
            } catch (Exception e) {
                l.WarnFormat("Problem doing work:", e);
            } finally {
                workerSemaphore.Release();
            }
        }

        private Response GetResponseForQuery(Query query) {
            Response response;
            try {
                l.InfoFormat("Data is: {0}", query.MessageData);
                if (query.Handler == null) {
                    l.WarnFormat("No query name given: {0}", query.MessageData);
                    throw new RPCException(String.Format("Do not handle query {0}", query.Handler));
                }
                Func<Query, Response> fun;
                if (!handlers.TryGetValue(query.Handler, out fun)) {
                    l.WarnFormat("Do not handle query {0}", query.Handler);
                    throw new RPCException(String.Format("Do not handle query {0}", query.Handler));
                }
                // Func<Query, Response> fun = handlers[query.Handler];
                l.InfoFormat("Calling handler {0}...", query.Handler);
                // Stopwatch sw = Stopwatch.StartNew();
                response = fun(query);
                // sw.Stop();
                // TimingHelper.Add("handler", sw);
                l.InfoFormat("Back from handler {0}.", query.Handler);
            } catch (Exception e) {
                l.WarnFormat("Something went wrong calling handler:", e);
                response = Response.Failure(e.Message);
            }
            response.Dispatch = query.Dispatch;
            return response;
        }

        #region Builtin procedures

        private int Ping(int x) {
            l.InfoFormat("ping of {0}", x);
            return x;
        }

        // private bool warmedUp = false;
        private ServerStats GetStats(bool junk) {
            
            //if (!warmedUp) {
            //    TimingHelper.Reset();
            //    warmedUp = true;
            //}
            int wt, cpt, awt, acpt;
            ThreadPool.GetMaxThreads(out wt, out cpt);
            ThreadPool.GetAvailableThreads(out awt, out acpt);

            //var times = workQueueTimes.ToArray();
            //Array.Sort(times);
            //var workQueueTime50 = times[(int)(times.Length * 0.50)];

            ServerStats ss = new ServerStats() {
                WorkQueueLength = workQueue.Count,
                ActiveWorkerThreads = wt - awt,
                ActiveCompletionPortThreads = cpt - acpt,
                Timers = TimingHelper.Summary()
            };
            foreach (var client in clients) {
                ClientStats cs = new ClientStats() {
                    OutgoingQueueLength = client.OutgoingCount,
                    IncomingTotal = client.QueriesReceived,
                    OutgoingTotal = client.ResponsesReturned,
                };
                ss.AddClient(cs);
            }
            TimingHelper.Reset();
            return ss;
        }

        #endregion
    }
}
