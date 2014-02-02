using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    public class QueryWithDestination {
        public Query Query;
        public BlockingQueue<Response> Destination;
        public QueryWithDestination(Query query, BlockingQueue<Response> destination) {
            this.Query = query;
            this.Destination = destination;
        }
    }
}
