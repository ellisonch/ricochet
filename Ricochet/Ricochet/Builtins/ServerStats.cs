using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    /// <summary>
    /// Class representing operating stats for a server.
    /// </summary>
    public class ServerStats {
        /// <summary>
        /// The length of the work queue.  I.e., incoming queries.
        /// </summary>
        public int WorkQueueLength { get; set; }
        /// <summary>
        /// Number of outstanding worker threads in the thread pool
        /// </summary>
        public int ActiveWorkerThreads { get; set; }
        /// <summary>
        /// Number of outstanding completion port threads in the thread pool
        /// </summary>
        public int ActiveCompletionPortThreads { get; set; }

        /// <summary>
        /// Work Queue times
        /// </summary>
        public Dictionary<string, BasicStats> Timers { get; set; }

        private List<ClientStats> _clients = new List<ClientStats>();

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerStats() { }

        /// <summary>
        /// A list of stats about the clients the server is connected to.
        /// </summary>
        public List<ClientStats> Clients {
            get { return _clients; }
            set { _clients = value; }
        }
        
        internal void AddClient(ClientStats cs) {
            Clients.Add(cs);
        }
    }
}
