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
using Common.Logging;

// TODO comments aren't showing up from NuGet import

namespace Ricochet {
    /// <summary>
    /// An RPC Client represents a client through which RPC requests can be
    /// sent.  The client maintains a connection to an RPC server, and 
    /// sends the requests to that server.
    /// </summary>
    public class Client : IDisposable {
        private static readonly ILog l = LogManager.GetCurrentClassLogger();

        const int maxQueueSize = 2000;
        const int connectionTimeout = 50;
        static int softQueryTimeout = 500; // time (ms) before it gets sent
        internal static int HardQueryTimeout = 2000; // total time of round trip (still takes this long to give up even if soft is hit)
        Random r = new Random();
        private bool disposed = false;
        Serializer serializer;

        private IBoundedQueue<Query> outgoingQueries = new BoundedQueue<Query>(maxQueueSize);
        private PendingRequests pendingRequests = new PendingRequests();

        Thread readerThread;
        Thread writerThread;

        readonly StableConnection connection;

        /// <summary>
        /// Returns true if this client is still alive (hasn't been disposed).
        /// </summary>
        public bool IsAlive {
            get {
                return !disposed;
            }
        }

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
        public async Task WaitUntilConnected() {
            l.WarnFormat("Waiting...");
            while (! await Ping()) {
                if (disposed) { throw new ObjectDisposedException("Client was disposed, so can't connect"); }
                // Console.Write(".");
                System.Threading.Thread.Sleep(100);
            }
            l.WarnFormat("Connected.");
        }

        /// <summary>
        /// Pings the server once.
        /// </summary>
        /// <returns>Returns true on success, false on failure.</returns>
        public async Task<bool> Ping() {
            int pingVal = r.Next();
            var pingResult = await this.TryCallAsync<int, int>("_ping", pingVal);
            if (!pingResult.OK) {
                return false;
            }
            if (pingResult.Value != pingVal) {
                l.ErrorFormat("Ping() returned the wrong value");
                return false;
            }
            return true;
        }

        private void WriteQueries() {
            Query queryToRetry = null;

            while (!disposed) {
                Query query;
                if (queryToRetry == null) {
                    if (!outgoingQueries.TryDequeue(out query)) {
                        continue;
                    }
                } else {
                    query = queryToRetry;
                    queryToRetry = null;
                }
                if (query.SW.ElapsedMilliseconds > softQueryTimeout) {
                    l.DebugFormat("Soft timeout reached");
                    continue;
                }
                if (!connection.Write(query)) {
                    queryToRetry = query;
                    // outgoingQueries.EnqueAtFrontWithoutFail(query);
                    System.Threading.Thread.Sleep(connectionTimeout);
                }
            }
        }

        private void ReadResponses() {
            while (!disposed) {
                Response response;
                if (!connection.Read(out response)) {
                    System.Threading.Thread.Sleep(connectionTimeout);
                    continue;
                }

                int dispatch = response.Dispatch;
                this.pendingRequests.SetResponse(dispatch, response);
            }
        }
        
        /// <summary>
        /// Tries to make an RPC call.  May timeout or otherwise fail.
        /// </summary>
        /// <typeparam name="T1">Type of input.</typeparam>
        /// <typeparam name="T2">Type of output.</typeparam>
        /// <param name="name">Name of function call</param>
        /// <param name="input">Input to function</param>
        /// <returns>An option type, possibly containing the result</returns>
        //public Option<T2> TryCall<T1, T2>(string name, T1 input) {
        //    return TryCallAsync<T1, T2>(name, input).Result;
        //}

        //public bool TryCallThrowAway<T1, T2>(string name, T1 input) {
        //    return TryCallAsyncThrowAway<T1, T2>(name, input).Result;
        //}

        /// <summary>
        /// Tries to make an RPC call.  May timeout or otherwise fail.
        /// </summary>
        /// <typeparam name="T1">Type of input.</typeparam>
        /// <typeparam name="T2">Type of output.</typeparam>
        /// <param name="name">Name of function call</param>
        /// <param name="input">Input to function</param>
        /// <returns>An option type, possibly containing the result</returns>
        public async Task<Option<T2>> TryCallAsync<T1, T2>(string name, T1 input) {
            var response = await GetResponse(name, input);
            return ExtractResult<T2>(response);
        }

        public async Task<bool> TryCallAsyncThrowAway<T1, T2>(string name, T1 input) {
            var response = await GetResponse(name, input);
            return response != null && response.OK && response.MessageData != null;
        }

        private async Task<Response> GetResponse<T1>(string name, T1 input) {
            SignaledResponse sr = StartCall(name, input);
            if (sr == null) {
                return null;
            }
            Response response = await sr.GetResponse();
            return response;
        }

        private SignaledResponse StartCall<T1>(string name, T1 input) {
            if (disposed) { return null; }
            Query query = Query.CreateQuery<T1>(name, input, serializer);
            var sr = pendingRequests.AddRequest(query);

            if (!outgoingQueries.EnqueueIfRoom(query)) {
                pendingRequests.DeleteRequest(query.Dispatch);
                return null;
            }
            return sr;
        }

        private Option<T2> ExtractResult<T2>(Response response) {
            if (response == null || !response.OK) {
                return Option<T2>.None();
            }

            if (response.MessageData == null) {
                return Option<T2>.None();
            }
            var ret = serializer.Deserialize<T2>(response.MessageData);
            return Option<T2>.Some(ret);
        }

        /// <summary>
        /// Releases the resources held by the Client.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources held by the Client.
        /// </summary>
        /// <param name="disposing">If true, Disposes of owned, managed objects</param>
        public virtual void Dispose(bool disposing) {
            if (disposed) { return; }
            disposed = true;
            if (disposing) {
                try { outgoingQueries.Close(); } catch (Exception) { }
                // pendingRequests.Dispose();
                connection.Dispose();
            }
        }
    }
}
