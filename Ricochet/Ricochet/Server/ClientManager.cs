using Common.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

        // readonly IBoundedQueue<QueryWithDestination> incomingQueries;
        // private IBoundedQueue<Tuple<byte[], Stopwatch>> outgoingResponses = new BoundedQueue<Tuple<byte[], Stopwatch>>(maxQueueSize);

        Thread readerThread;
        Thread writerThread;
        Serializer serializer;
        Server server;

        /// <summary>
        /// Creates a new ClientManager that is not yet running.
        /// </summary>
        /// <param name="client">TcpClient to handle.</param>
        /// <param name="incomingQueries">Global queue in which to insert incoming queries.</param>
        /// <param name="serializer">Serializer user to send and receive messages over the wire.</param>
        public ClientManager(TcpClient client, Serializer serializer, Server server) {
            this.client = client;
            this.server = server;
            // this.incomingQueries = incomingQueries;
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

        //internal int IncomingCount {
        //    get {
        //        return incomingQueries.Count;
        //    }
        //}
        //internal int OutgoingCount {
        //    get {
        //        return outgoingResponses.Count;
        //    }
        //}
        internal bool IsAlive {
            get {
                return !disposed;
            }
        }

        /// <summary>
        /// Starts managing the client using new threads.  Returns immediately.
        /// </summary>
        internal async Task Start() {
            l.InfoFormat("Accepted client");
            try {

                Task wor = WriteOutResponses();

                while (!disposed) {
                    // NetworkInstability();
                    byte[] queryBytes = await readStream.ReadFromStream();
                    if (queryBytes == null) {
                        l.WarnFormat("Invalid query received, ignoring it");
                        throw new RPCException("Error reading query");
                    }
                    Interlocked.Increment(ref queriesReceived);

                    // l.InfoFormat("Handling bytes...");
                    var task = HandleBytes(queryBytes);
                    // l.InfoFormat("Handled bytes.");
                    
                    // Stopwatch sw = Stopwatch.StartNew();
                    //qwd.Destination.EnqueueIfRoom(new Tuple<byte[], Stopwatch>(responseBytes, sw));

                    //var qwd = new QueryWithDestination(bytes, outgoingResponses, serializer);
                    //if (!incomingQueries.EnqueueIfRoom(qwd)) {
                    //    l.WarnFormat("Reached maximum queue size!  Query dropped.");
                    //}

                }
            } catch (Exception e) {
                l.InfoFormat("Error in ReadQueries():", e);
            } finally {
                Dispose();
            }
            // l.Log(Logger.Flag.Warning, "Finishing Reader");
        }

        private async Task WriteOutResponses() {
            while (true) {
                while (outgoingQueue.Count > 0) {
                    // l.InfoFormat("Thread: {0}", Thread.CurrentThread.ManagedThreadId);
                    byte[] bytes;
                    bytes = outgoingQueue.Dequeue();
                    // if (!outgoingQueue.TryDequeue(out bytes)) {
                    //     l.InfoFormat("Couldn't dequeue");
                    // }
                    if (bytes == null) {
                        l.WarnFormat("bytes is null :(");
                        throw new RPCException("Bytes shouldn't be null");
                    }
                    await WriteResponses(bytes);
                    // l.InfoFormat("Wrote responses");
                }
                // await Task.Yield();
                //l.InfoFormat("Waiting for queue to have stuff");
                await slimshim.WaitAsync();
                //l.InfoFormat("Through WaitAsync");
                // slimshim.Release();
                // QueueHasStuff = new TaskCompletionSource<int>();
            }
        }

        Queue<byte[]> outgoingQueue = new Queue<byte[]>();

        // TaskCompletionSource<int> QueueHasStuff = new TaskCompletionSource<int>();
        SemaphoreSlim slimshim = new SemaphoreSlim(0, 1);

        private async System.Threading.Tasks.Task HandleBytes(byte[] queryBytes) {
            Query query = serializer.DeserializeQuery(queryBytes);
            if (query == null) {
                throw new RPCException("Error deserializing query");
            }

            // l.DebugFormat("Getting response");
            Response response = await server.GetResponseForQuery(query);
            // l.DebugFormat("Got response");
            byte[] responseBytes = serializer.SerializeResponse(response);
            // l.DebugFormat("Got response bytes");

            // l.InfoFormat("Enqueing Thread: {0}", Thread.CurrentThread.ManagedThreadId);
            if (responseBytes == null) {
                l.WarnFormat("responseBytes is null :(");
                Environment.Exit(1);
                throw new RPCException("Bytes shouldn't be null");
            }

            // l.InfoFormat("Signaling");
            if (slimshim.CurrentCount == 0) {
                slimshim.Release();
            }
            // l.InfoFormat("bytes: {0}", responseBytes.Length);
            outgoingQueue.Enqueue(responseBytes);
            // await WriteResponses(responseBytes);
            // WriteResponsesSync(responseBytes);
            
            // QueueHasStuff.TrySetResult(1);
            // l.InfoFormat("Signaled");

            // l.DebugFormat("Wrote response");
        }

        //private async void ReadQueries() {
            
        //}

        private async Task WriteResponses(byte[] bytes) {
            try {
                // Tuple<byte[], Stopwatch> tup;
                //if (!outgoingResponses.TryDequeue(out tup)) {
                //    continue;
                //}
                //var sw = tup.Item2;
                // Console.WriteLine(sw.Elapsed.TotalMilliseconds);

                // TODO TimingHelper.Add("Response Queue", sw);
                // var bytes = tup.Item1;
                await writeStream.WriteToStreamAsync(bytes);
                Interlocked.Increment(ref responsesReturned);
            } catch (Exception e) {
                l.WarnFormat("Error in WriteResponses()", e);
            }
            //finally {
            //    this.Dispose();
            //}
            // l.Log(Logger.Flag.Warning, "Finishing Writer");
        }

        private void WriteResponsesSync(byte[] bytes) {
            try {
                // Tuple<byte[], Stopwatch> tup;
                //if (!outgoingResponses.TryDequeue(out tup)) {
                //    continue;
                //}
                //var sw = tup.Item2;
                // Console.WriteLine(sw.Elapsed.TotalMilliseconds);

                // TODO TimingHelper.Add("Response Queue", sw);
                // var bytes = tup.Item1;
                writeStream.WriteToStream(bytes);
                Interlocked.Increment(ref responsesReturned);
            } catch (Exception e) {
                l.WarnFormat("Error in WriteResponses()", e);
            }
            //finally {
            //    this.Dispose();
            //}
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
            // try { outgoingResponses.Close(); } catch (Exception) { }

            if (underlyingStream != null) {
                try { underlyingStream.Close(); } catch (Exception) { }
            }
            if (writeStream != null) {
                try { writeStream.Dispose(); } catch (Exception) { }
            }
            if (readStream != null) {
                try { readStream.Dispose(); } catch (Exception) { }
            }
            if (client != null) {
                try { client.Close(); } catch (Exception) { }
                client = null;
            }

            l.InfoFormat("Disposing ClientManager");
            disposed = true;
        }
    }
}
