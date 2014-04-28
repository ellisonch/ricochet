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

        public SignaledResponse(int id, Stopwatch stopwatch, bool isAsync) {
            this.id = id;
            this.SW = stopwatch;
            this.isAsync = isAsync;

            if (isAsync) {
                tcs = new TaskCompletionSource<Response>();
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
        }
    }
}
