using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    /// <summary>
    /// Stats about a client.
    /// </summary>
    public class ClientStats {
        /// <summary>
        /// The length of the completed queue holding completed responses, ready to be sent back to the client.
        /// </summary>
        public int OutgoingQueueLength { get; set; }
        /// <summary>
        /// The total number of queries received over the wire.
        /// </summary>
        public long IncomingTotal { get; set; }
        /// <summary>
        /// The total number of responses returned over the wire.
        /// </summary>
        public long OutgoingTotal { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ClientStats() { }
    }
}
