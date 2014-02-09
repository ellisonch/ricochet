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
        BufferedStream writeStream;
        BufferedStream readStream;

        readonly BoundedQueue<QueryWithDestination> incomingQueries;
        protected BoundedQueue<Response> outgoingResponses = new BoundedQueue<Response>(maxQueueSize);

        Thread readerThread;
        Thread writerThread;
        Serializer serializer;

        /// <summary>
        /// Creates a new ClientManager that is not yet running.
        /// </summary>
        /// <param name="client">TcpClient to handle.</param>
        /// <param name="incomingQueries">Global queue in which to insert incoming queries.</param>
        /// <param name="serializer">Serializer user to send and receive messages over the wire.</param>
        public ClientManager(TcpClient client, BoundedQueue<QueryWithDestination> incomingQueries, Serializer serializer) {
            this.client = client;
            this.incomingQueries = incomingQueries;
            this.serializer = serializer;
        }

        /// <summary>
        /// Starts managing the client using new threads.  Returns immediately.
        /// </summary>
        public void Start() {
            l.Log(Logger.Flag.Warning, "Accepted client");
            writeStream = new BufferedStream(client.GetStream());
            readStream = new BufferedStream(client.GetStream());

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
                    byte[] bytes = serializer.SerializeResponse(response);
                    serializer.WriteToStream(writeStream, bytes);
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
                    byte[] bytes = serializer.ReadFromStream(readStream);
                    Query query = serializer.DeserializeQuery(bytes);
                    if (query == null) {
                        l.Log(Logger.Flag.Warning, "Invalid query received, ignoring it");
                        throw new RPCException("Error reading query");
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
                if (writeStream != null) { writeStream.Close(); }
                if (readStream != null) { readStream.Close(); }
                if (client != null) { client.Close(); }
            }
        }
    }
}
