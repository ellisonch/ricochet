using System;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Concurrent;
using System.Threading;
using System.Net.Sockets;

namespace RPC {
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
        Logger l = new Logger(Logger.Flag.Default);

        const int maxQueueSize = 2000;
        const int numWorkerThreads = 8;

        Serializer serializer;
        private readonly IPAddress address;
        private readonly int port;

        private ConcurrentDictionary<string, Func<Query, Response>> handlers = new ConcurrentDictionary<string, Func<Query, Response>>();

        /// <summary>
        /// Only contains non-null queries
        /// </summary>
        private BoundedQueue<QueryWithDestination> incomingQueries = new BoundedQueue<QueryWithDestination>(maxQueueSize);


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
            l.Log(Logger.Flag.Info, "Configuring server as {0}:{1}", address, port);

            for (int i = 0; i < numWorkerThreads; i++) {
                new Thread(this.DoWork).Start();
            }

            Register<int, int>("_ping", Ping);
        }

        /// <summary>
        /// Starts the server.  Blocks while server is running.
        /// </summary>
        public void Start() {
            try {
                TcpListener listener = new TcpListener(address, port);
                listener.Start();
                while (true) {
                    l.Log(Logger.Flag.Info, "Waiting for new client...");

                    var client = listener.AcceptTcpClient();
                    l.Log(Logger.Flag.Info, "Client connected.");
                    var clientHandler = new ClientManager(client, incomingQueries, serializer);
                    clientHandler.Start();
                }
            } catch (AggregateException e) {
                l.Log(Logger.Flag.Error, "Exception thrown: {0}", e.InnerException.Message);
            } catch (Exception e) {
                l.Log(Logger.Flag.Error, "Exception thrown: {0}", e);
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
                // T1 arg = Serialization.DeserializeFromString<T1>(query.MessageData);
                T1 arg = serializer.Deserialize<T1>(query.MessageData);
                var res = fun(arg);
                Response resp = Response.CreateResponse<T2>(query, res, serializer);
                return resp;
            });
        }

        private void DoWork() {
            while (true) {
                QueryWithDestination qwd;
                // TODO consider not allowing it to fail
                if (!incomingQueries.TryDequeue(out qwd)) {
                    continue;
                }
                // TODO what about doing work for connections that died
                Response response = GetResponseForQuery(qwd.Query);
                // l.Log(Logger.Flag.Warning, "Response calculated by thread {0}", Thread.CurrentThread.ManagedThreadId);
                qwd.Destination.EnqueueIfRoom(response);
            }
        }

        private Response GetResponseForQuery(Query query) {
            Response response;
            try {
                l.Log(Logger.Flag.Info, "Data is: {0}", query.MessageData);
                if (query.Handler == null) {
                    l.Log(Logger.Flag.Warning, "No query name given: {0}", query.MessageData);
                    throw new RPCException(String.Format("Do not handle query {0}", query.Handler));
                }
                if (!handlers.ContainsKey(query.Handler)) {
                    l.Log(Logger.Flag.Warning, "Do not handle query {0}", query.Handler);
                    throw new RPCException(String.Format("Do not handle query {0}", query.Handler));
                }
                Func<Query, Response> fun = handlers[query.Handler];
                l.Log(Logger.Flag.Info, "Calling handler {0}...", query.Handler);
                response = fun(query);
                l.Log(Logger.Flag.Info, "Back from handler {0}.", query.Handler);
            } catch (Exception e) {
                response = new Response(e.Message);
            }
            response.Dispatch = query.Dispatch;
            return response;
        }

        private int Ping(int x) {
            return x;
        }
    }
}
