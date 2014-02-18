﻿using System;
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
using AsyncBridge;

namespace Ricochet {
    /// <summary>
    /// An RPC Client represents a client through which RPC requests can be
    /// sent.  The client maintains a connection to an RPC server, and 
    /// sends the requests to that server.
    /// </summary>
    public class Client : IDisposable {
        private readonly ILog l = LogManager.GetCurrentClassLogger();

        const int maxQueueSize = 2000;
        const int connectionTimeout = 50;
        static int softQueryTimeout = 500; // time (ms) before it gets sent
        internal static int HardQueryTimeout = 2000; // total time of round trip (still takes this long to give up even if soft is hit)

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
        public void WaitUntilConnected() {
            l.WarnFormat("Waiting...");
            //while (!Ping()) {
            //    if (disposed) { throw new ObjectDisposedException("Client was disposed, so can't connect"); }
            //    // Console.Write(".");
            //    System.Threading.Thread.Sleep(100);
            //}
            l.WarnFormat("Connected.");
        }

        //private bool Ping() {
        //    int pingResult;
        //    if (!this.TryCall<int, int>("_ping", 9001, out pingResult)) {
        //        return false;
        //    }
        //    if (pingResult != 9001) {
        //        return false;
        //    }
        //    return true;
        //}

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
                    l.InfoFormat("Soft timeout reached");
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
        /// <param name="ret">Result from function</param>
        /// <returns>True if the call was successful, false otherwise.</returns>
        public async Task<Tuple<bool, T2>> TryCallAsync<T1, T2>(string name, T1 input) {
            T2 ret = default(T2);
            if (disposed) { throw new ObjectDisposedException("This client has been disposed."); }
            Query query = Query.CreateQuery<T1>(name, input, serializer);
            var tcs = pendingRequests.Add(query);
            if (!outgoingQueries.EnqueueIfRoom(query)) {
                // l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Query dropped.");
                pendingRequests.Delete(query.Dispatch);
                return new Tuple<bool, T2>(false, ret);
            }

            Response response = await pendingRequests.Get(query.Dispatch);

            if (!response.OK) {
                return new Tuple<bool, T2>(false, ret);
            }

            if (response.MessageData == null) {
                return new Tuple<bool, T2>(false, ret);
            }
            ret = serializer.Deserialize<T2>(response.MessageData);
            return new Tuple<bool, T2>(true, ret);
        }

        /// <summary>
        /// Same as TryCallAsync, but synchronous.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="name"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public Tuple<bool, T2> TryCall<T1, T2>(string name, T1 input) {
            Tuple<bool, T2> ret = null;
            using (var async = AsyncHelper.Wait) {
                async.Run(TryCallAsync<T1, T2>(name, input), x => ret = x);
            }
            return ret;
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
