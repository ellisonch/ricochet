using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    internal class QueuedQuery {
        internal Stopwatch SW = Stopwatch.StartNew();
        internal Query Query;
        internal QueuedQuery(Query q) {
            Query = q;
        }
    }
}
