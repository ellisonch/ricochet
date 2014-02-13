using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    internal class QueryWithDestination {
        private byte[] bytes;
        private Serializer serializer;

        internal Query Query {
            get {
                Query query = serializer.DeserializeQuery(bytes);
                if (bytes == null) {
                    throw new RPCException("Error deserializing query");
                }
                return query;
            }
        }
        internal BoundedQueue<byte[]> Destination;
        internal QueryWithDestination(byte[] bytes, BoundedQueue<byte[]> destination, Serializer serializer) {
            this.bytes = bytes;
            this.Destination = destination;
            this.serializer = serializer;
        }
    }
}
