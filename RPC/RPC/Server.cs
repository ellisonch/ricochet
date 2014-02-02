using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
// using NLog;
using System.Threading.Tasks;
using System.IO;
using ServiceStack.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.Generic;


namespace RPC {
    public class Server {
        const int maxQueueSize = 2000;
        const int numWorkerThreads = 8;

        // private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IPAddress address;
        private readonly int port;
        ConcurrentDictionary<string, Func<Query, Response>> handlers = new ConcurrentDictionary<string, Func<Query, Response>>();
        Logger l = new Logger(Logger.Flag.Default);

        protected BlockingQueue<QueryWithDestination> incomingQueries = new BlockingQueue<QueryWithDestination>(maxQueueSize);

        public Server(IPAddress address, int port) {
            this.address = address;
            this.port = port;
            Console.WriteLine("Configuring server as {0}:{1}", address, port);

            for (int i = 0; i < numWorkerThreads; i++) {
                new Thread(this.DoWork).Start();
            }

            Register<int, int>("ping", Ping);
        }

        private void DoWork() {
            while (true) {
                QueryWithDestination qwd;
                if (!incomingQueries.TryDequeue(out qwd)) {
                    continue;
                }
                Response response = GetResponseForQuery(qwd.Query);
                // l.Log(Logger.Flag.Warning, "Response calculated by thread {0}", Thread.CurrentThread.ManagedThreadId);
                qwd.Destination.EnqueueIfRoom(response);
                // ProcessQuery(query);
            }
        }

        private Response GetResponseForQuery(Query query) {
            Response response;
            try {
                l.Log(Logger.Flag.Info, "Data is: {0}", query.MessageData);
                if (query.Handler == null) {
                    l.Log(Logger.Flag.Warning, "No query name given");
                    throw new Exception(String.Format("Do not handle query {0}", query.Handler));
                }
                if (!handlers.ContainsKey(query.Handler)) {
                    l.Log(Logger.Flag.Warning, "Do not handle query {0}", query.Handler);
                    throw new Exception(String.Format("Do not handle query {0}", query.Handler));
                }
                Func<Query, Response> fun = handlers[query.Handler];
                l.Log(Logger.Flag.Info, "Calling handler {0}...", query.Handler);
                response = fun(query);
                response.Dispatch = query.Dispatch;
                l.Log(Logger.Flag.Info, "Back from handler {0}.", query.Handler);
            } catch (Exception e) {
                response = new Response(e);
            }
            return response;
        }

        public void Start() {
            try {
                TcpListener listener = new TcpListener(address, port);
                listener.Start();
                while (true) {
                    Console.WriteLine("Waiting for new client...");

                    var client = listener.AcceptTcpClient();
                    var clientHandler = new ClientHandler(client, handlers, incomingQueries);
                    // clientHandlers.Add(clientHandler);
                }
            } catch (AggregateException e) {
                Console.WriteLine("Exception thrown: {0}", e.InnerException.Message);
            } catch (Exception e) {
                Console.WriteLine("Exception thrown: {0}", e);
            }
        }

        public void Register<T1, T2>(string name, Func<T1, T2> fun) {
            if (handlers.ContainsKey(name)) {
                throw new Exception(String.Format("A handler is already registered for the name '{0}'", name));
            }
            handlers[name] = (Func<Query, Response>)((query) => {
                T1 arg = JsonSerializer.DeserializeFromString<T1>(query.MessageData);
                var res = fun(arg);
                Response resp = Response.CreateResponse<T2>(query, res);
                return resp;
            });
        }

        private int Ping(int x) {
            return x;
        }
    }
}
