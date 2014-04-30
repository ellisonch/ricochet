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

        private readonly ManualResetEvent barrier;
        private readonly TaskCompletionSource<Response> tcs;
        // private SemaphoreSlim barrier2 = new SemaphoreSlim(0, 1);

        private readonly bool isAsync;
        public readonly Stopwatch SW;
        private readonly int id;

        private Response response;

        // private static CancellationTokenSource ct = new CancellationTokenSource(2000);

        public SignaledResponse(int id, Stopwatch stopwatch, bool isAsync) {
            this.id = id;
            this.SW = stopwatch;
            this.isAsync = isAsync;

            if (isAsync) {
                tcs = new TaskCompletionSource<Response>();

                //int remainingTime = (int)(Client.HardQueryTimeout - SW.ElapsedMilliseconds);
                //if (remainingTime > Client.HardQueryTimeout) {
                //    remainingTime = 0;
                //}
                //if (remainingTime < 0) {
                //    remainingTime = 0;
                //}
                
                //ct.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);
            } else {
                barrier = new ManualResetEvent(false);
            }
        }
        public void Set(Response response) {
            this.response = response;
            if (isAsync) {
                tcs.SetResult(response);
            } else {
                barrier.Set();
            }
        }

        public Response Get(int remainingTime) {
            Response res;
            bool canProceed = barrier.WaitOne(remainingTime);
            if (!canProceed) { // if timeout...
                l.WarnFormat("Hard timeout reached");
                res = Response.Timeout(id);
            } else {
                res = response;
            }
            return res;
        }

        public async Task<Response> GetAsync(int remainingTime) {
            // Timer timer = new Timer((x) => {}, null, 2000, -1);
            // timer.
            var cts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(remainingTime, cts.Token);
            // var timeoutTask = Task.Delay(remainingTime);
            var t = await Task.WhenAny(tcs.Task, timeoutTask);

            // return await tcs.Task;

            if (t == tcs.Task) {
                cts.Cancel();
                return await tcs.Task;
            } else {
                tcs.SetCanceled();
                return Response.Timeout(id);
            }
        }
    }
}
