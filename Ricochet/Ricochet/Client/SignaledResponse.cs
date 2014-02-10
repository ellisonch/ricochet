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
        private ManualResetEvent barrier = new ManualResetEvent(false);
        private SemaphoreSlim barrier2 = new SemaphoreSlim(0, 1);

        public Response Response;
        public Stopwatch SW;

        public SignaledResponse(Stopwatch stopwatch) {
            this.SW = stopwatch;
        }
        public void Set() {
            barrier.Set();
            // barrier.Release();
        }
        public bool WaitUntil(int timeout) {
            return barrier.WaitOne(timeout);
            // return barrier.Wait(timeout);
        }
        public async Task<bool> WaitUntilAsync(int timeout) {
            return await barrier2.WaitAsync(timeout);
        }
    }
}
