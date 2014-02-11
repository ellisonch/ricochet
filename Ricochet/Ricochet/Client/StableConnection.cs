using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    // TODO should worry about cleaning up thread
    // TODO i think i saw this mess up once; need reporting for weird cases.  i think getting write lock was timing out
    // TODO i think sometimes it's still trying to reconnect twice
    // TODO possible upon Disposal, that it reconnects
    internal sealed class StableConnection : IDisposable {
        const int lockTimeout = 50;
        const int reconnectTimer = 500;
        private bool disposed = false;

        private readonly string hostname;
        private readonly int port;

        Logger l = new Logger(Logger.Flag.Default);

        ReaderWriterLock rwl = new ReaderWriterLock();

        Serializer serializer;
        private TcpClient sender;
        private BufferedStream writeStream;
        private BufferedStream readStream;

        bool connected = false;
        // Semaphore shouldReconnect = new Semaphore(1, 1);
        AutoResetEvent shouldReconnect = new AutoResetEvent(true);
        Thread reconnectThread;

        public StableConnection(string hostname, int port, Serializer serializer) {
            this.hostname = hostname;
            this.port = port;
            this.serializer = serializer;

            reconnectThread = new Thread(this.Reconnect);
            reconnectThread.Start();
        }
        // TODO consider using lock slim
        private void Reconnect() {
            while (!disposed) {
                shouldReconnect.WaitOne();
                if (disposed) { return; }
                try {
                    rwl.AcquireWriterLock(500);
                    // l.Log(Logger.Flag.Warning, "Reconnect: got writer lock");
                    try {
                        while (!ConnectWithWriteLock()) {
                            // l.Info("Couldn't reconnect, sleeping...");
                            System.Threading.Thread.Sleep(reconnectTimer);
                        }
                    } finally {
                        // l.Info("Connected in thingie...");
                        // Ensure that the lock is released.
                        rwl.ReleaseWriterLock();
                        // l.Log(Logger.Flag.Warning, "Reconnect: released writer lock");
                    }
                } catch (ApplicationException) {
                    // lock timed out
                    l.Log(Logger.Flag.Info, "Getting writer lock timed out...");
                    // l.Info("Setting need");
                    shouldReconnect.Set();
                }
            }
            // l.Log(Logger.Flag.Warning, "Connection closing");
        }

        internal bool Write(Query query) {
            try {
                rwl.AcquireReaderLock(lockTimeout);
                // l.Log(Logger.Flag.Warning, "Write: got reader lock");
                try {
                    if (disposed) { return false; }
                    if (!connected) { return false; }
                    byte[] bytes = serializer.SerializeQuery(query);
                    MessageStream.WriteToStream(writeStream, bytes);
                } catch (Exception e) {
                    l.Log(Logger.Flag.Info, "Error writing: {0}", e.Message);
                    RequestReconnect();
                    return false;
                } finally {
                    // Ensure that the lock is released.
                    rwl.ReleaseReaderLock();
                    // l.Log(Logger.Flag.Warning, "Write: released reader lock");
                }
            } catch (ApplicationException) {
                // lock timed out
                return false;
            }
            return true;
        }

        internal bool Read(out Response response) {
            response = null;
            try {
                rwl.AcquireReaderLock(lockTimeout);
                // l.Log(Logger.Flag.Warning, "Read: got reader lock");
                try {
                    if (disposed) { return false; }
                    if (!connected) { return false; }
                    byte[] bytes = MessageStream.ReadFromStream(readStream);
                    response = serializer.DeserializeResponse(bytes);
                    
                    if (response == null) {
                        RequestReconnect();
                        return false;
                    }
                } catch (Exception e) {
                    l.Log(Logger.Flag.Info, "Error reading: {0}", e.Message);
                    RequestReconnect();
                    return false;
                } finally {
                    // Ensure that the lock is released.
                    rwl.ReleaseReaderLock();
                    // l.Log(Logger.Flag.Warning, "Read: released reader lock");
                }
            } catch (ApplicationException) {
                // lock timed out
                // l.Log(Logger.Flag.Warning, "Getting reader lock timed out");
                return false;
            }

            if (response.OK == true && response.MessageData == null) {
                response.OK = false;
                response.ErrorMsg = "Something went wrong deserializing the message data";
            }

            return true;
        }

        // should be called from a thread with a read lock held
        // TODO if both timeout, don't even reconnect
        // TODO don't reconnect until there is a timeout below
        private void RequestReconnect() {
            Debug.Assert(rwl.IsReaderLockHeld);
            try {
                var lc = rwl.UpgradeToWriterLock(lockTimeout);
                // l.Log(Logger.Flag.Warning, "RequestReconnect: got writer lock");
                try {
                    // l.Info("Setting new need");
                    shouldReconnect.Set();
                } finally {
                    // Ensure that the lock is released.
                    rwl.DowngradeFromWriterLock(ref lc);
                    // l.Log(Logger.Flag.Warning, "RequestReconnect: downgraded lock");
                }
            } catch (ApplicationException) {
                // lock timed out
                l.Log(Logger.Flag.Info, "Trying to upgrade lock timed out");
            }
        }
        
        private bool ConnectWithWriteLock() {
            Debug.Assert(rwl.IsWriterLockHeld);

            connected = false;

            if (sender != null) {
                sender.Close();
            }
            sender = new TcpClient();
            //sender.ReceiveBufferSize = 1024 * 32;
            //sender.SendBufferSize = 1024 * 32;
            l.Log(Logger.Flag.Info, "Connecting to {0}:{1}...", hostname, port);
            // await sender.ConnectAsync(hostname, port);
            try {
                sender.Connect(hostname, port);
            } catch (SocketException e) {
                l.Log(Logger.Flag.Info, "Couldn't connect: {0}", e.Message);
                return false;
            }

            if (readStream != null) {
                readStream.Close();
            }
            if (writeStream != null) {
                writeStream.Close();
            }

            writeStream = new BufferedStream(sender.GetStream());
            readStream = new BufferedStream(sender.GetStream());

            l.Log(Logger.Flag.Info, "Connected to {0}:{1}", hostname, port);
            connected = true;
            return true;
        }
        private void Connect() {
            try {
                rwl.AcquireWriterLock(5000);
                try {
                    ConnectWithWriteLock();
                } finally {
                    // Ensure that the lock is released.
                    rwl.ReleaseWriterLock();
                }
            } catch (ApplicationException) {
                // The reader lock request timed out.
                throw new RPCException("Connect: Timed out on trying to get writer lock; this should never happen");
            }
        }

        public void Dispose() {
            disposed = true;
            if (writeStream != null) {
                try { writeStream.Close(); } catch (Exception) { }
            }
            if (readStream != null) {
                try { readStream.Close(); } catch (Exception) { }
            }
            if (sender != null) {
                try {sender.Close(); } catch (Exception) {}
            }
        }
    }
}
