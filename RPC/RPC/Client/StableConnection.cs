using ProtoBuf;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
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
    internal class StableConnection {
        const int lockTimeout = 50;
        const int reconnectTimer = 500;

        private readonly string hostname;
        private readonly int port;

        Logger l = new Logger(Logger.Flag.Default);

        ReaderWriterLock rwl = new ReaderWriterLock();

        private TcpClient sender;
        NetworkStream networkStream;
        private StreamWriter streamWriter;
        private StreamReader streamReader;

        bool connected = false;
        // Semaphore shouldReconnect = new Semaphore(1, 1);
        AutoResetEvent shouldReconnect = new AutoResetEvent(true);
        Thread reconnectThread;

        public StableConnection(string hostname, int port) {
            this.hostname = hostname;
            this.port = port;

            reconnectThread = new Thread(this.Reconnect);
            reconnectThread.Start();
        }

        private void Reconnect() {
            while (true) {
                // l.Info("Waiting for need...");
                shouldReconnect.WaitOne();
                // l.Info("Need found.");
                try {
                    rwl.AcquireWriterLock(500);
                    try {
                        while (!ConnectWithWriteLock()) {
                            // l.Info("Couldn't reconnect, sleeping...");
                            System.Threading.Thread.Sleep(reconnectTimer);
                        }
                    } finally {
                        // l.Info("Connected in thingie...");
                        // Ensure that the lock is released.
                        rwl.ReleaseWriterLock();
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

        public bool Write(Query query) {
            try {
                rwl.AcquireReaderLock(lockTimeout);
                try {
                    if (!connected) { return false; }
                    Serialization.WriteQuery(networkStream, streamWriter, query);
                } catch (IOException e) {
                    l.Log(Logger.Flag.Info, "Error writing: {0}", e.Message);
                    RequestReconnect();
                    return false;
                } finally {
                    // Ensure that the lock is released.
                    rwl.ReleaseReaderLock();
                }
            } catch (ApplicationException) {
                // lock timed out
                return false;
            }
            return true;
        }

        public bool Read(out Response response) {
            response = null;
            try {
                rwl.AcquireReaderLock(lockTimeout);
                try {
                    if (!connected) { return false; }
                    response = Serialization.ReadResponse(networkStream, streamReader);
                    
                    if (response == null) {
                        RequestReconnect();
                        return false;
                    }
                } catch (IOException e) {
                    l.Log(Logger.Flag.Info, "Error reading: {0}", e.Message);
                    RequestReconnect();
                    return false;
                } finally {
                    // Ensure that the lock is released.
                    rwl.ReleaseReaderLock();
                }
            } catch (ApplicationException) {
                // lock timed out
                // l.Log(Logger.Flag.Warning, "Getting reader lock timed out");
                return false;
            }

            if (response.OK == true && response.MessageData == null) {
                response.OK = false;
                response.Error = new Exception(String.Format("Something went wrong deserializing the message data"));
            }

            return true;
        }

        // should be called from a thread with a read lock held
        private void RequestReconnect() {
            try {
                var lc = rwl.UpgradeToWriterLock(lockTimeout);
                try {
                    // l.Info("Setting new need");
                    shouldReconnect.Set();
                } finally {
                    // Ensure that the lock is released.
                    rwl.DowngradeFromWriterLock(ref lc);
                }
            } catch (ApplicationException) {
                // lock timed out
                l.Log(Logger.Flag.Info, "Trying to upgrade lock timed out");
            }
        }
        
        private bool ConnectWithWriteLock() {
            if (!rwl.IsWriterLockHeld) {
                throw new RPCException("Somebody lied and isn't really holding a writer lock; this shouldn't happen");
            }
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

            if (networkStream != null) {
                networkStream.Close();
            }
            if (streamReader != null) {
                streamReader.Close();
            }
            if (streamWriter != null) {
                streamWriter.Close();
            }

            networkStream = sender.GetStream();
            streamWriter = new StreamWriter(sender.GetStream());
            streamReader = new StreamReader(sender.GetStream());

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
                throw new RPCException("Timed out on trying to get writer lock; this should never happen");
            }
        }
    }
}
