using Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ricochet {
    // TODO should worry about cleaning up thread
    // TODO i think i saw this mess up once; need reporting for weird cases.  i think getting write lock was timing out
    // TODO i think sometimes it's still trying to reconnect twice
    // TODO possible upon Disposal, that it reconnects
    internal sealed class StableConnection : IDisposable {
        const int lockTimeout = 50;
        const int testConnectionTimer = 500;
        const int connectRetryTimer = 500;
        private bool disposed = false;

        private readonly string hostname;
        private readonly int port;

        private readonly ILog l = LogManager.GetCurrentClassLogger();

        Serializer serializer;
        private TcpClient connection;
        private MessageReadStream readStream;
        private MessageWriteStream writeStream;

        Thread reconnectThread;

        public StableConnection(string hostname, int port, Serializer serializer) {
            this.hostname = hostname;
            this.port = port;
            this.serializer = serializer;

            reconnectThread = new Thread(this.MonitorConnection);
            reconnectThread.Start();
        }

        private void MonitorConnection() {
            while (!disposed) {
                if (connection != null && connection.Connected) {
                    System.Threading.Thread.Sleep(testConnectionTimer);
                    continue;
                }
                while (!Connect()) {
                    // l.Info("Couldn't reconnect, sleeping...");
                    System.Threading.Thread.Sleep(connectRetryTimer);
                }
            }
            // l.Log(Logger.Flag.Warning, "Connection closing");
        }

        internal bool Write(Query query) {
            try {
                if (disposed) { return false; }
                if (connection == null) { return false; }
                // NetworkInstability();
                byte[] bytes = serializer.SerializeQuery(query);
                writeStream.WriteToStream(bytes);
            } catch (Exception e) {
                l.InfoFormat("Error writing: {0}", e.Message);
                // RequestReconnect();
                return false;
            }
            return true;
        }

        public static Random r = new Random(0);
        private void NetworkInstability() {
            if (r.NextDouble() < 0.00001) {
                l.WarnFormat("Network Instability!");
                this.connection.Close();
            }
        }

        internal bool Read(out Response response) {
            response = null;
            try {
                if (disposed) { return false; }
                if (connection == null) { return false; }
                byte[] bytes = readStream.ReadFromStream();
                response = serializer.DeserializeResponse(bytes);
                    
                if (response == null) {
                    // RequestReconnect();
                    return false;
                }
            } catch (Exception e) {
                l.InfoFormat("Error reading: {0}", e.Message);
                // RequestReconnect();
                return false;
            }

            Debug.Assert(response.MessageData != null, "MessageData should never be null");

            return true;
        }
                
        private bool Connect() {
            if (connection != null) {
                try { connection.Close(); } catch (Exception) { }
            }
            connection = null;
            TcpClient myConnection = new TcpClient();
            //sender.ReceiveBufferSize = 1024 * 32;
            //sender.SendBufferSize = 1024 * 32;
            l.InfoFormat("Connecting to {0}:{1}...", hostname, port);
            // await sender.ConnectAsync(hostname, port);
            try {
                myConnection.Connect(hostname, port);
            } catch (SocketException e) {
                l.InfoFormat("Couldn't connect: {0}", e.Message);
                return false;
            }

            if (readStream != null) {
                try { readStream.Dispose(); } catch (Exception) { }
            }
            if (writeStream != null) {
                try { writeStream.Dispose(); } catch (Exception) { }
            }

            writeStream = new MessageWriteStream(myConnection.GetStream());
            readStream = new MessageReadStream(myConnection.GetStream());

            connection = myConnection;

            l.InfoFormat("Connected to {0}:{1}", hostname, port);
            return true;
        }

        public void Dispose() {
            disposed = true;
            if (writeStream != null) {
                try { writeStream.Dispose(); } catch (Exception) { }
                writeStream = null;
            }
            if (readStream != null) {
                try { readStream.Dispose(); } catch (Exception) { }
                readStream = null;
            }
            if (connection != null) {
                try {connection.Close(); } catch (Exception) {}
                connection = null;
            }
        }
    }
}
