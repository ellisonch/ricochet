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
        private bool running = true;

        private ConcurrentDictionary<string, Func<Query, Response>> handlers;
        private TcpClient client;
        private object clientLock = new object();

        Thread readerThread;
        Thread writerThread;
        // Thread workerThread;

        StreamReader reader;
        StreamWriter writer;

        readonly BlockingQueue<QueryWithDestination> incomingQueries;
        protected BlockingQueue<Response> outgoingResponses = new BlockingQueue<Response>(maxQueueSize);

        public ClientHandler(TcpClient client, ConcurrentDictionary<string, Func<Query, Response>> handlers, BlockingQueue<QueryWithDestination> incomingQueries) {
            this.client = client;
            this.handlers = handlers;
            this.incomingQueries = incomingQueries;

            this.readerThread = new Thread(this.ReadQueries);
            readerThread.Start();
            this.writerThread = new Thread(this.WriteResponses);
            writerThread.Start();
        }

        private void WriteResponses() {
            try {
                while (running) {
                    Response response;
                    if (!outgoingResponses.TryDequeue(out response)) {
                        continue;
                    }
                    writer.WriteLine(response.Serialize());
                    writer.Flush();
                }
            } catch (Exception e) {
                l.Log(Logger.Flag.Warning, "Error in WriteResponses(): {0}", e.Message);
            } finally {
                Cleanup();
            }
            l.Log(Logger.Flag.Warning, "Finishing Writer");
        }

        private void ReadQueries() {
            try {
                l.Log(Logger.Flag.Info, "Accepted client");
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream());

                while (running) {
                    var s = reader.ReadLine();
                    if (s == null) {
                        throw new RPCException("End of input stream reached");
                    }
                    l.Log(Logger.Flag.Debug, "Server Received {0}", s);
                    Query query = JsonSerializer.DeserializeFromString<Query>(s);
                    //var pieces = s.Split(new char[]{'|'});
                    //Query query = new Query() {
                    //    Handler = pieces[0],
                    //    Dispatch = Convert.ToInt32(pieces[1]),
                    //    MessageType = Type.GetType(pieces[2]),
                    //    MessageData = pieces[3],
                    //};

                    if (query == null) {
                        l.Log(Logger.Flag.Warning, "Invalid query received, ignoring it: {0}", s);
                        continue;
                    }
                    var qwd = new QueryWithDestination(query, outgoingResponses);
                    if (!incomingQueries.EnqueueIfRoom(qwd)) {
                        l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Query dropped.");
                    }                    
                }
            } catch (Exception e) {
                l.Log(Logger.Flag.Warning, "Error in ReadQueries(): {0}", e.Message);
            } finally {
                Cleanup();
            }
            l.Log(Logger.Flag.Warning, "Finishing Reader");
        }

        private void Cleanup() {
            lock (clientLock) {
                // l.Log(Logger.Flag.Warning, "Cleaning up ClientHandler");
                running = false;
                outgoingResponses.Close();
                if (reader != null) { reader.Close(); }
                if (writer != null) { writer.Close(); }
                if (client != null) { client.Close(); }
            }
        }
    }
}
