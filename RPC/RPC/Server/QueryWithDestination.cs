using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    internal class QueryWithDestination {
        internal Query Query;
        internal BlockingQueue<Response> Destination;
        internal QueryWithDestination(Query query, BlockingQueue<Response> destination) {
            this.Query = query;
            this.Destination = destination;
        }
    }
}
