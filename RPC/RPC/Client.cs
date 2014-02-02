using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using ServiceStack.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;

namespace RPC {
    public class Client {
        Logger l = new Logger(Logger.Flag.Default);
        const int maxQueueSize = 2000;

        const int connectionTimeout = 50;
        static int softQueryTimeout = 500; // time (ms) before it gets sent
        public static int HardQueryTimeout = 2000; // total time of round trip (still takes this long to give up even if soft is hit)

        protected BlockingQueue<Query> outgoingQueries = new BlockingQueue<Query>(maxQueueSize);
        // protected Semaphore outgoingCount = new Semaphore(0, maxQueueSize);
        protected PendingRequests pendingRequests = new PendingRequests();

        Thread readerThread;
        Thread writerThread;

        readonly Connection connection;

        public Client(string hostname, int port) {
            this.connection = new Connection(hostname, port);

            readerThread = new Thread(this.ReadResponses);
            readerThread.Start();

            writerThread = new Thread(this.WriteQueries);
            writerThread.Start();

            // Ping();
        }
        
        private void Ping() {
            Query msg = Query.CreateQuery<int>("ping", 9001);
            Response r = this.Call(msg);
            if (r.OK) {
                int ar = JsonSerializer.DeserializeFromString<int>(r.MessageData);
                if (ar != 9001) {
                    throw new RPCException("Didn't get good response from server");
                }
            } else {
                throw new RPCException("Didn't get good response from server");
            }
        }

        private bool shouldDequeue(Query query) {
            if (query.SW.ElapsedMilliseconds > softQueryTimeout) {
                l.Log(Logger.Flag.Warning, "Soft timeout reached");
                return true;
            }
            if (this.TrySendQuery(query)) {
                return true;
            } else {
                System.Threading.Thread.Sleep(connectionTimeout);
                return false;
            }
        }

        private void WriteQueries() {
            while (true) {
                outgoingQueries.TryCallingFunOnElement(shouldDequeue);
            }
        }

        private bool TrySendQuery(Query query) {
            string msg = "";
            try {
                msg = query.Serialize();
            } catch (Exception e) {
                l.Log(Logger.Flag.Info, "Error serializing: {0}", e);
                throw;
            }

            return connection.Write(msg);
        }

        public void ReadResponses() {
            while (true) {
                string res;
                if (!connection.Read(out res)) {
                    System.Threading.Thread.Sleep(connectionTimeout);
                    continue;
                }

                var response = JsonSerializer.DeserializeFromString<Response>(res);
                if (response == null) {
                    // TODO should probably kill connection here
                    l.Log(Logger.Flag.Info, "Failed to deserialize response.  Something's really messed up");
                    continue;
                }
                if (response.OK == true && response.MessageData == null) {
                    response.OK = false;
                    response.Error = new Exception(String.Format("Something went wrong deserializing the message data of length {0}", res.Length));
                }
                int dispatch = response.Dispatch;
                this.pendingRequests.Set(dispatch, response);
                // l.Info("Got response {0}", response.MessageData);

                // sw.Stop();
                // response.sw = sw;
                // System.IO.File.WriteAllText("c:\\temp\\ResponseEncoded.txt", res);
                // return response;
            }
        }
        
        public Response Call(Query query) {
            pendingRequests.Add(query);
            enqueueMessage(query);
            return pendingRequests.Get(query.Dispatch);
        }
        
        private void enqueueMessage(Query query) {
            //lock (outgoingQueries) {
            //    if (outgoingQueries.Count < maxQueueSize) {
            //        outgoingQueries.Enqueue(query);
            //    } else {
            //        l.Log(Logger.Flag.Info, "Reached maximum queue size!  Query dropped.");
            //    }
            //    Monitor.Pulse(outgoingQueries);
            //}
            if (!outgoingQueries.EnqueueIfRoom(query)) {
                l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Query dropped.");
            }
        }
    }
}
