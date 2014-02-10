using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;

namespace RPC {
    /// <summary>
    /// An RPC Client represents a client through which RPC requests can be
    /// sent.  The client maintains a connection to an RPC server, and 
    /// sends the requests to that server.
    /// 
    /// TODO: A client currently does not release its resources if things go bad.
    /// </summary>
    public class Client {
        Logger l = new Logger(Logger.Flag.Default);

        const int maxQueueSize = 2000;
        const int connectionTimeout = 50;
        static int softQueryTimeout = 500; // time (ms) before it gets sent
        internal static int HardQueryTimeout = 2000; // total time of round trip (still takes this long to give up even if soft is hit)

        Serializer serializer;

        private BoundedQueue<Query> outgoingQueries = new BoundedQueue<Query>(maxQueueSize);
        private PendingRequests pendingRequests = new PendingRequests();

        Thread readerThread;
        Thread writerThread;

        readonly StableConnection connection;

        /// <summary>
        /// Create a new RPC Client.
        /// </summary>
        /// <param name="hostname">The hostname of the server</param>
        /// <param name="port">The port of the server</param>
        /// <param name="serializer">The Serializer to use</param>
        public Client(string hostname, int port, Serializer serializer) {
            this.serializer = serializer;
            this.connection = new StableConnection(hostname, port, serializer);
            
            readerThread = new Thread(this.ReadResponses);
            readerThread.Start();

            writerThread = new Thread(this.WriteQueries);
            writerThread.Start();
        }

        /// <summary>
        /// Blocks until connected to the server and the server responds to a ping.
        /// Does not guarantee the server will still be connected when another query is sent.
        /// </summary>
        public void WaitUntilConnected() {
            Console.Write("Waiting... ");
            while (!Ping()) {
                Console.Write(".");
                System.Threading.Thread.Sleep(100);
            }
            Console.WriteLine(" Connected.");
        }

        private bool Ping() {
            int pingResult;
            if (!this.TryCall<int, int>("_ping", 9001, out pingResult)) {
                return false;
            }
            if (pingResult != 9001) {
                return false;
            }
            return true;
        }

        private void WriteQueries() {
            while (true) {
                Query query;
                if (!outgoingQueries.TryDequeue(out query)) {
                    continue;
                }
                if (query.SW.ElapsedMilliseconds > softQueryTimeout) {
                    l.Log(Logger.Flag.Info, "Soft timeout reached");
                    continue;
                }
                if (!connection.Write(query)) {
                    // TODO it's kind of weird we'd fail here.  may want to cause EnqueuAtFront to kick out old stuff
                    if (!outgoingQueries.EnqueAtFront(query)) {
                        l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Query dropped.");
                    }
                    System.Threading.Thread.Sleep(connectionTimeout);
                }
            }
        }

        private void ReadResponses() {
            while (true) {
                Response response;
                if (!connection.Read(out response)) {
                    System.Threading.Thread.Sleep(connectionTimeout);
                    continue;
                }

                int dispatch = response.Dispatch;
                this.pendingRequests.Set(dispatch, response);
            }
        }
        
        /// <summary>
        /// Tries to make an RPC call.  May timeout or otherwise fail.
        /// </summary>
        /// <typeparam name="T1">Type of input.</typeparam>
        /// <typeparam name="T2">Type of output.</typeparam>
        /// <param name="name">Name of function call</param>
        /// <param name="input">Input to function</param>
        /// <param name="ret">Result from function</param>
        /// <returns>True if the call was successful, false otherwise.</returns>
        public bool TryCall<T1, T2>(string name, T1 input, out T2 ret) {
            ret = default(T2);
            Query query = Query.CreateQuery<T1>(name, input, serializer);
            pendingRequests.Add(query);
            if (!outgoingQueries.EnqueueIfRoom(query)) {
                l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Query dropped.");
                pendingRequests.Delete(query.Dispatch);
                return false;
            }
            Response response = pendingRequests.Get(query.Dispatch);
            if (!response.OK) {
                return false;
            }

            if (response.MessageData == null) {
                return false;
            }
            ret = serializer.Deserialize<T2>(response.MessageData);
            return true;
        }
    }
}
