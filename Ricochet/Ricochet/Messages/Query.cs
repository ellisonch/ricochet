using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ricochet {
    /// <summary>
    /// The actual package that is sent to a server representing an RPC call.
    /// </summary>
    public class Query : Message {
        static private int ticketNumber = 0;
        internal Stopwatch SW = Stopwatch.StartNew();

        /// <summary>
        /// Name of function to which this query should be passed.
        /// </summary>
        public string Handler { get; set; }

        internal static Query CreateQuery<T>(string handler, T data, Serializer serializer) {
            return new Query {
                Handler = handler,
                Dispatch = Interlocked.Increment(ref ticketNumber),
                MessageData = serializer.Serialize<T>(data)
            };
        }
        // serializer doesn't use most specific type?
        //internal byte[] Serialize() {
        //    return Serialization.SerializeQuery(this);
        //}
    }

}
