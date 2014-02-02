using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    class SignaledResponse {
        public ManualResetEvent Barrier = new ManualResetEvent(false);
        public Response Response;
        public Stopwatch SW;

        public SignaledResponse(Stopwatch stopwatch) {
            this.SW = stopwatch;
        }
    }
}
