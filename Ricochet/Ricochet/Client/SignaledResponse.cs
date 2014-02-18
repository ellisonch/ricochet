using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ricochet {
    class SignaledResponse {
        private ManualResetEvent barrier = new ManualResetEvent(false);
        // private SemaphoreSlim barrier2 = new SemaphoreSlim(0, 1);
        private TaskCompletionSource<Response> tcs;

        public Response Response;
        public Stopwatch SW;

        public SignaledResponse(Stopwatch stopwatch, TaskCompletionSource<Response> tcs) {
            this.SW = stopwatch;
            this.tcs = tcs;
        }
        public void Set() {
            // barrier.Set();
            tcs.TrySetResult(Response);
            // FIXME ASDFASDF
            // barrier.Release();
        }
        public bool WaitUntil(int timeout) {
            return tcs.Task.Wait(timeout);
            // return barrier.WaitOne(timeout);
            // return barrier.Wait(timeout);
        }

        public async Task<Response> WaitResponse(int timeout) {
            var t = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            if (t == tcs.Task) {
                return await tcs.Task;
            } else {
                return null;
            }
        }
        //public async Task<bool> WaitUntilAsync(int timeout) {
        //    return await barrier2.WaitAsync(timeout);
        //}
    }
}