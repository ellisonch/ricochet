using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    public class QueuedQuery {
        public Stopwatch SW = Stopwatch.StartNew();
        public Query Query;
        public QueuedQuery(Query q) {
            Query = q;
        }
    }
}
