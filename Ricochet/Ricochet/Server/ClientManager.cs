using Common.Logging;
using ServiceStack.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace Ricochet {
    // TODO: should really be using timeout that comes from the client

    /// <summary>
    /// A ClientManager is in charge of receiving queries and sending responses
    /// to a single client.
    /// </summary>
    internal sealed class ClientManager : IDisposable {
        private readonly ILog l = LogManager.GetCurrentClassLogger();
        const int maxQueueSize = 2000;
        //const int readTimeout = 500;
        //const int writeTimeout = 500;

        private bool disposed = false;
        // private object clientLock = new object();

        private TcpClient client;
        Stream underlyingStream;
        MessageReadStream readStream;
        MessageWriteStream writeStream;

        private long queriesReceived;
        private long responsesReturned;

        public long QueriesReceived {
            get {
                return Interlocked.Read(ref queriesReceived);
            }
        }
        public long ResponsesReturned {
            get {
                return Interlocked.Read(ref responsesReturned);
            }
        }

        readonly IBoundedQueue<QueryWithDestination> incomingQueries;
        private IBoundedQueue<byte[]> outgoingResponses = new BoundedQueueSingleConsumer<byte[]>(maxQueueSize);

        Thread readerThread;
        Thread writerThread;
        Serializer serializer;

        /// <summary>
        /// Creates a new ClientManager that is not yet running.
        /// </summary>
        /// <param name="client">TcpClient to handle.</param>
        /// <param name="incomingQueries">Global queue in which to insert incoming queries.</param>
        /// <param name="serializer">Serializer user to send and receive messages over the wire.</param>
        public ClientManager(TcpClient client, IBoundedQueue<QueryWithDestination> incomingQueries, Serializer serializer) {
            this.client = client;
            this.incomingQueries = incomingQueries;
            this.serializer = serializer;
            this.underlyingStream = client.GetStream();

            //this.underlyingStream.ReadTimeout = readTimeout;
            //this.underlyingStream.WriteTimeout = writeTimeout;

            this.writeStream = new MessageWriteStream(underlyingStream);
            this.readStream = new MessageReadStream(underlyingStream);


            //l.Log(Logger.Flag.Warning, "RCanTimeout: {0}", underlyingStream.CanTimeout);
            //l.Log(Logger.Flag.Warning, "ReadTimeout: {0}", underlyingStream.ReadTimeout);
            //l.Log(Logger.Flag.Warning, "WriteTimeout: {0}", underlyingStream.ReadTimeout);
        }

        internal int IncomingCount {
            get {
                return incomingQueries.Count;
            }
        }
        internal int OutgoingCount {
            get {
                return outgoingResponses.Count;
            }
        }
        internal bool IsAlive {
            get {
                return !disposed;
            }
        }

        /// <summary>
        /// Starts managing the client using new threads.  Returns immediately.
        /// </summary>
        internal void Start() {
            l.WarnFormat("Accepted client");

            this.readerThread = new Thread(this.ReadQueries);
            readerThread.Start();
            this.writerThread = new Thread(this.WriteResponses);
            writerThread.Start();
        }

        private void ReadQueries() {
            try {
                while (!disposed) {
                    // NetworkInstability();
                    byte[] bytes = readStream.ReadFromStream();
                    if (bytes == null) {
                        l.WarnFormat("Invalid query received, ignoring it");
                        throw new RPCException("Error reading query");
                    }
                    Interlocked.Increment(ref queriesReceived);
                    var qwd = new QueryWithDestination(bytes, outgoingResponses, serializer);
                    if (!incomingQueries.EnqueueIfRoom(qwd)) {
                        l.WarnFormat("Reached maximum queue size!  Query dropped.");
                    }
                }
            } catch (Exception e) {
                l.InfoFormat("Error in ReadQueries(): {0}", e.Message);
            } finally {
                Dispose();
            }
            // l.Log(Logger.Flag.Warning, "Finishing Reader");
        }

        private void WriteResponses() {
            try {
                while (!disposed) {
                    byte[] bytes;
                    if (!outgoingResponses.TryDequeue(out bytes)) {
                        continue;
                    }
                    writeStream.WriteToStream(bytes);
                    Interlocked.Increment(ref responsesReturned);
                }
            } catch (Exception e) {
                l.WarnFormat("Error in WriteResponses(): {0}", e.Message);
            } finally {
                this.Dispose();
            }
            // l.Log(Logger.Flag.Warning, "Finishing Writer");
        }


        // Used to test what happens when the network is unstable
        public static Random r = new Random(0);
        private void NetworkInstability() {
            if (r.NextDouble() < 0.00001) {
                l.WarnFormat("Network Instability!");
                this.client.Close();
            }
        }

        public void Dispose() {
            if (disposed) { return; }
            try { outgoingResponses.Close(); } catch (Exception) { }

            if (underlyingStream != null) {
                try { underlyingStream.Close(); } catch (Exception) { 
                    // l.Log(Logger.Flag.Warning, "Error closing stream: {0}", e.Message); 
                }
            }
            if (writeStream != null) {
                try { writeStream.Dispose(); } catch (Exception) {
                    // l.Log(Logger.Flag.Warning, "Error closing write stream: {0}", e.Message);
                }
            }
            if (readStream != null) {
                try { readStream.Dispose(); } catch (Exception) {
                    // l.Log(Logger.Flag.Warning, "Error closing read stream: {0}", e.Message);
                }
            }
            if (client != null) {
                try { client.Close(); } catch (Exception) {
                    // l.Log(Logger.Flag.Warning, "Error closing client: {0}", e.Message);
                }
                client = null;
            }

            disposed = true;
        }
    }
}
