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
    /// A server currently does not release its resources if things go bad.
    /// </summary>
    public class Client {
        Logger l = new Logger(Logger.Flag.Default);

        const int maxQueueSize = 2000;
        const int connectionTimeout = 50;
        static int softQueryTimeout = 500; // time (ms) before it gets sent
        internal static int HardQueryTimeout = 2000; // total time of round trip (still takes this long to give up even if soft is hit)

        private BlockingQueue<Query> outgoingQueries = new BlockingQueue<Query>(maxQueueSize);
        private PendingRequests pendingRequests = new PendingRequests();

        Thread readerThread;
        Thread writerThread;

        readonly StableConnection connection;

        /// <summary>
        /// Create a new RPC Client.
        /// </summary>
        /// <param name="hostname">The hostname of the server</param>
        /// <param name="port">The port of the server</param>        
        public Client(string hostname, int port) {
            this.connection = new StableConnection(hostname, port);

            readerThread = new Thread(this.ReadResponses);
            readerThread.Start();

            writerThread = new Thread(this.WriteQueries);
            writerThread.Start();

            // TODO: should return and not fail if connection isn't available
            //int pingResult;
            //if (!this.TryCall<int, int>("_ping", 9001, out pingResult)) {
            //    l.Log(Logger.Flag.Error, "Couldn't ping");
            //    throw new RPCException("Couldn't connect properly; ping failed");
            //}
            //if (pingResult != 9001) {
            //    l.Log(Logger.Flag.Error, "Couldn't ping");
            //    throw new RPCException("Couldn't connect properly; ping failed with wrong response");
            //}
        }

        private void WriteQueries() {
            while (true) {
                Query query;
                if (!outgoingQueries.TryDequeue(out query)) {
                    continue;
                }
                if (query.SW.ElapsedMilliseconds > softQueryTimeout) {
                    l.Log(Logger.Flag.Warning, "Soft timeout reached");
                    continue;
                }
                if (!connection.Write(query)) {
                    // TODO it's possible that the queue has filled up in the time it took us to try to send; 
                    // it's kind of weird we'd fail here.  may want to cause EnqueuAtFront to kick out
                    // old stuff
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
        /// <param name="ret">Output from function</param>
        /// <returns>True if the call was successful, false otherwise.</returns>
        public bool TryCall<T1, T2>(string name, T1 input, out T2 ret) {
            Query query = Query.CreateQuery<T1>(name, input);
            pendingRequests.Add(query);
            if (!outgoingQueries.EnqueueIfRoom(query)) {
                l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Query dropped.");
            }
            Response response = pendingRequests.Get(query.Dispatch);
            if (!response.OK) {
                ret = default(T2);
                return false;
            }

            if (response.MessageData == null) {
                ret = default(T2);
                return false;
            }
            ret = Serialization.DeserializeFromString<T2>(response.MessageData);
            return true;
        }
    }
}
