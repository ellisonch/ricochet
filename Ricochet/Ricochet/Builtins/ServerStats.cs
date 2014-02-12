using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    /// <summary>
    /// Class representing operating stats for a server.
    /// </summary>
    public class ServerStats {
        /// <summary>
        /// The length of the work queue.  I.e., incoming queries.
        /// </summary>
        public int WorkQueueLength { get; set; }
        private List<ClientStats> _clients = new List<ClientStats>();
        /// <summary>
        /// A list of stats about the clients the server is connected to.
        /// </summary>
        public List<ClientStats> Clients {
            get { return _clients; }
            set { _clients = value; }
        }

        internal ServerStats(int workQueueLength) {
            this.WorkQueueLength = workQueueLength;
        }

        internal void AddClient(ClientStats cs) {
            Clients.Add(cs);
        }
    }
}
