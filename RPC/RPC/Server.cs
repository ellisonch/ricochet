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
        // private readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IPAddress address;
        private readonly int port;
        ConcurrentDictionary<string, Func<Query, Response>> handlers = new ConcurrentDictionary<string, Func<Query, Response>>();
        Logger l = new Logger(Logger.Flag.Default);
        // ConcurrentBag<ClientHandler> clientHandlers = new ConcurrentBag<ClientHandler>();

        public Server(IPAddress address, int port) {
            this.address = address;
            this.port = port;
            Console.WriteLine("Configuring server as {0}:{1}", address, port);

            Register<int, int>("ping", Ping);
        }
        public void Start() {
            try {
                TcpListener listener = new TcpListener(address, port);
                listener.Start();
                while (true) {
                    Console.WriteLine("Waiting for new client...");

                    var client = listener.AcceptTcpClient();
                    var clientHandler = new ClientHandler(client, handlers);
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
