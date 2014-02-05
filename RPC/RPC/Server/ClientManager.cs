using ServiceStack.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace RPC {
    // TODO: should really be using timeout that comes from the client

    /// <summary>
    /// A ClientManager is in charge of receiving queries and sending responses
    /// to a single client.
    /// </summary>
    internal class ClientManager {
        Logger l = new Logger(Logger.Flag.Default);
        const int maxQueueSize = 2000;

        private bool running = true;
        private object clientLock = new object();

        private TcpClient client;
        NetworkStream networkStream;
        StreamReader streamReader;
        StreamWriter streamWriter;

        readonly BlockingQueue<QueryWithDestination> incomingQueries;
        protected BlockingQueue<Response> outgoingResponses = new BlockingQueue<Response>(maxQueueSize);

        Thread readerThread;
        Thread writerThread;

        /// <summary>
        /// Creates a new ClientManager that is not yet running.
        /// </summary>
        /// <param name="client">TcpClient to handle.</param>
        /// <param name="incomingQueries">Global queue in which to insert incoming queries.</param>
        public ClientManager(TcpClient client, BlockingQueue<QueryWithDestination> incomingQueries) {
            this.client = client;
            this.incomingQueries = incomingQueries;
        }

        /// <summary>
        /// Starts managing the client using new threads.  Returns immediately.
        /// </summary>
        public void Start() {
            l.Log(Logger.Flag.Warning, "Accepted client");
            networkStream = client.GetStream();
            streamReader = new StreamReader(client.GetStream());
            streamWriter = new StreamWriter(client.GetStream());

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
                    Serialization.WriteResponse(networkStream, streamWriter, response);
                }
            } catch (Exception e) {
                l.Log(Logger.Flag.Warning, "Error in WriteResponses(): {0}", e.Message);
            } finally {
                Cleanup();
            }
            // l.Log(Logger.Flag.Warning, "Finishing Writer");
        }

        private void ReadQueries() {
            try {
                while (running) {
                    Query query = Serialization.ReadQuery(networkStream, streamReader);
                    if (query == null) {
                        throw new RPCException("Error reading query");
                        //l.Log(Logger.Flag.Warning, "Invalid query received, ignoring it");
                        //continue;
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
            // l.Log(Logger.Flag.Warning, "Finishing Reader");
        }
        // TODO consider idisposable stuff
        private void Cleanup() {
            lock (clientLock) {
                // l.Log(Logger.Flag.Warning, "Cleaning up ClientHandler");
                // l.Log(Logger.Flag.Warning, "Client disconnected.");
                running = false;
                outgoingResponses.Close();
                if (networkStream != null) { networkStream.Close(); }
                if (streamWriter != null) { streamWriter.Close(); }
                if (streamReader != null) { streamReader.Close(); }
                if (client != null) { client.Close(); }
            }
        }
    }
}
