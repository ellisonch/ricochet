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
        public void SetResponse(Response response) {
            tcs.SetResult(response);
        }

        public async Task<Response> GetResponse() {
            int remainingTime = (int)(timeout - SW.ElapsedMilliseconds);
            if (remainingTime > timeout) {
                remainingTime = 0;
            }
            if (remainingTime < 0) {
                remainingTime = 0;
            }

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
