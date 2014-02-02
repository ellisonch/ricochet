using ServiceStack.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    // TODO: the readers and writer die if any exception is throw, not only actually being disconnected
    // TODO: worker threads aren't being cleaned up
    public class ClientHandler {
        Logger l = new Logger(Logger.Flag.Default);
        // static int softQueryTimeout = 500; // time (ms) before it gets sent
        const int maxQueueSize = 2000;

        const int numWorkerThreads = 8;

        private ConcurrentDictionary<string, Func<Query, Response>> handlers;
        private TcpClient client;
        private object clientLock = new object();

        Thread readerThread;
        Thread writerThread;
        // Thread workerThread;

        StreamReader reader;
        StreamWriter writer;

        protected BlockingQueue<Query> incomingQueries = new BlockingQueue<Query>(maxQueueSize);
        protected BlockingQueue<Response> outgoingResponses = new BlockingQueue<Response>(maxQueueSize);

        public ClientHandler(TcpClient client, ConcurrentDictionary<string, Func<Query, Response>> handlers) {
            this.client = client;
            this.handlers = handlers;
            this.readerThread = new Thread(this.ReadQueries);
            readerThread.Start();
            this.writerThread = new Thread(this.WriteResponses);
            writerThread.Start();

            for (int i = 0; i < numWorkerThreads; i++) {
                new Thread(this.DoWork).Start();
            }
        }

        private void WriteResponses() {
            try {
                while (true) {
                    Response response = outgoingResponses.Dequeue();
                    writer.WriteLine(response.Serialize());
                    writer.Flush();
                }
            } catch (Exception e) {
                l.Log(Logger.Flag.Warning, "Error in WriteResponses(): {0}", e.Message);
                Cleanup();
            }
        }

        private void DoWork() {
            while (true) {
                Query query = incomingQueries.Dequeue();
                Response response = GetResponseForQuery(query);
                // l.Log(Logger.Flag.Warning, "Response calculated by thread {0}", Thread.CurrentThread.ManagedThreadId);
                outgoingResponses.EnqueueIfRoom(response);
                // ProcessQuery(query);
            }
        }

        private void ReadQueries() {
            try {
                l.Log(Logger.Flag.Info, "Accepted client");
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream());

                while (true) {
                    var s = reader.ReadLine();
                    if (s == null) {
                        throw new RPCException("End of input stream reached");
                    }
                    l.Log(Logger.Flag.Debug, "Server Received {0}", s);
                    Query query = JsonSerializer.DeserializeFromString<Query>(s);
                    if (query == null) {
                        l.Log(Logger.Flag.Warning, "Invalid query received, ignoring it: {0}", s);
                        continue;
                    }
                    if (!incomingQueries.EnqueueIfRoom(query)) {
                        l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Query dropped.");
                    }                    
                }
            } catch (Exception e) {
                l.Log(Logger.Flag.Warning, "Error in ReadQueries(): {0}", e.Message);
                Cleanup();
            }
        }

        private void Cleanup() {
            lock (clientLock) {
                if (reader != null) { reader.Close(); }
                if (writer != null) { writer.Close(); }
                if (client != null) { client.Close(); }
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
    }
}
