using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ricochet {
    /// <summary>
    /// Any kind of packet sent to or from an RPC server.
    /// </summary>
    public class Message {
        /// <summary>
        /// The ticket number of the query.  Uniquely identifies a query
        /// for a client.
        /// </summary>
        public int Dispatch { get; set; }

        /// <summary>
        /// The actual payload
        /// </summary>
        public byte[] MessageData { get; set; }
    }
}
