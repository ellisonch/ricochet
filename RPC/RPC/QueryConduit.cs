using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    public class QueryConduit {
        Logger l = new Logger(Logger.Flag.Default);

        protected Queue<Query> outgoingQueries = new Queue<Query>();
        // protected Semaphore outgoingCount = new Semaphore(0, maxQueueSize);
        protected PendingRequests pendingRequests = new PendingRequests();

        Thread readerThread;
        Thread writerThread;

        
        readonly Connection connection;

        public QueryConduit(string hostname, int port) {
            this.connection = new Connection(hostname, port);

            readerThread = new Thread(this.ReadResponses);
            readerThread.Start();

            writerThread = new Thread(this.WriteQueries);
            writerThread.Start();

            // Ping();
        }

    }
}
