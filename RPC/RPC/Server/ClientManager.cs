﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace RPC {
    // TODO: the readers and writer die if any exception is throw, not only actually being disconnected
    // TODO: worker threads aren't being cleaned up

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
        // StreamReader reader;
        NetworkStream reader;

        StreamWriter writer;
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
            l.Log(Logger.Flag.Info, "Accepted client");
            // reader = new StreamReader(client.GetStream());
            reader = client.GetStream();
            writer = new StreamWriter(client.GetStream());

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
            // l.Log(Logger.Flag.Warning, "Finishing Writer");
        }

        private void ReadQueries() {
            try {
                while (running) {
                    // var s = reader.ReadLine();
                    var len = reader.ReadByte(); // reader.Read();
                    // l.Log(Logger.Flag.Warning, "Looking to read {0} bytes", len+1);
                    if (len < 0) {
                        throw new RPCException("End of input stream reached");
                    }

                    byte[] s = readn(len+1);
                    
                    if (s == null) {
                        throw new RPCException("End of input stream reached");
                    }
                    // l.Log(Logger.Flag.Debug, "Server Received {0}", s);
                    Query query = Serialization.DeserializeQuery(s);

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
            // l.Log(Logger.Flag.Warning, "Finishing Reader");
        }

        private byte[] readn(int len) {
            byte[] buffer = new byte[len];
            int remaining = len;
            int done = 0;
            do {
                int got = reader.Read(buffer, done, remaining);
                done += got;
                remaining -= got;
            } while (remaining > 0);
            if (done != len) {
                throw new RPCException(String.Format("Wanted {0}, got {1} bytes", len, done));
            }
            if (remaining != 0) {
                throw new RPCException(String.Format("{0} bytes remaining", remaining));
            }
            return buffer;
        }

        private void Cleanup() {
            lock (clientLock) {
                // l.Log(Logger.Flag.Warning, "Cleaning up ClientHandler");
                // l.Log(Logger.Flag.Warning, "Client disconnected.");
                running = false;
                outgoingResponses.Close();
                if (reader != null) { reader.Close(); }
                if (writer != null) { writer.Close(); }
                if (client != null) { client.Close(); }
            }
        }
    }
}
