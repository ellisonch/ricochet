using Common.Logging;
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
        private static ILog l = LogManager.GetCurrentClassLogger();

        private readonly TaskCompletionSource<Response> tcs;
        private readonly Stopwatch SW;
        public readonly int id;
        private readonly int timeout;

        public SignaledResponse(int id, Stopwatch stopwatch, int timeout) {
            this.id = id;
            this.SW = stopwatch;
            this.timeout = timeout;

            tcs = new TaskCompletionSource<Response>();
        }
        // static int count = 0;
        public void SetResponse(Response response) {
            if (!tcs.TrySetResult(response)) {
                l.WarnFormat("Couldn't set response for {0}", response.Dispatch);
            } else {
                //count++;
                //if (count % 100000 == 0) {
                //    l.WarnFormat("Count Threadid: {0}", Thread.CurrentThread.ManagedThreadId);
                //    l.WarnFormat("Done {0} queries", count);
                //}
            }

        }

        public async Task<Response> GetResponse() {
            int remainingTime = (int)(timeout - SW.ElapsedMilliseconds);
            if (remainingTime > timeout) {
                remainingTime = 0;
            }
            if (remainingTime < 0) {
                remainingTime = 0;
            }
            // l.DebugFormat("Remaining time: {0}", remainingTime);
            var cts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(remainingTime, cts.Token);
            var t = await Task.WhenAny(tcs.Task, timeoutTask);
            if (t == tcs.Task) {
                cts.Cancel();
                return await tcs.Task;
            } else {
                tcs.SetCanceled();
                return Response.Timeout(id);
            }
            // return tcs.Task;

        }
    }
}
