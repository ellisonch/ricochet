using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPC {
    /// <summary>
    /// Any kind of packet sent to or from an RPC server.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(4, typeof(Query))]
    internal class Message {
        /// <summary>
        /// The ticket number of the query.  Uniquely identifies a query
        /// for a client.
        /// </summary>
        [ProtoMember(1)]
        public int Dispatch { get; set; }

        /// <summary>
        /// The type of the payload
        /// </summary>
        [ProtoMember(2)]
        public Type MessageType { get; set; }

        /// <summary>
        /// The actual payload
        /// </summary>
        [ProtoMember(3)]
        public string MessageData { get; set; }
    }
}
