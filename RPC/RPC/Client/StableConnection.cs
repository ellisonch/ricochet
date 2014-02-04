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
        // private StreamWriter writer;
        NetworkStream writer;
        private StreamReader reader;
        private TcpClient sender;

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
            byte[] msg;
            try {
                msg = query.Serialize();
            } catch (Exception e) {
                l.Log(Logger.Flag.Info, "Error serializing: {0}", e);
                throw;
            }

            if (msg.Length > 256) {
                Console.Write("msg is {0} bytes long", msg.Length);
                throw new RPCException("Can't handle packets larger than 256");
            }
            try {
                rwl.AcquireReaderLock(lockTimeout);
                try {
                    if (!connected) { return false; }
                    // char len = (char)(msg.Length-1);
                    // Console.WriteLine("Writing length of {0}", (int)len);
                    // writer.WriteByte((byte)len);
                    writer.Write(msg, 0, msg.Length);
                    // writer.Write(new char[]{(char)255});
                    // writer.Write((char)255);
                    // writer.WriteLine(msg);
                    writer.Flush();
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
            string s = null;
            try {
                rwl.AcquireReaderLock(lockTimeout);
                try {
                    if (!connected) { return false; }
                    s = reader.ReadLine();
                    if (s == null) {
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

            response = Serialization.DeserializeResponse(s);
            if (response == null) {
                // TODO should probably kill connection here
                l.Log(Logger.Flag.Warning, "Failed to deserialize response.  Something's really messed up");
            }

            if (response.OK == true && response.MessageData == null) {
                response.OK = false;
                response.Error = new Exception(String.Format("Something went wrong deserializing the message data of length {0}", s.Length));
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

            if (writer != null) {
                writer.Close();
            }
            if (reader != null) {
                reader.Close();
            }

            // writer = new StreamWriter(sender.GetStream());
            writer = sender.GetStream();
            reader = new StreamReader(sender.GetStream());

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
