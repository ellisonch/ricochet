using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    public class PendingRequests {
        private ConcurrentDictionary<int, SignaledResponse> requests = new ConcurrentDictionary<int, SignaledResponse>();
        Logger l = new Logger(Logger.Flag.Default);
        // private readonly int queryTimeout = 1000; // in ms

        internal Response Get(int ticket) {
            var sr = requests[ticket];
            // l.Info("Waiting for barrier {0}...", ticket);

            Response res;

            int remainingTime = (int)(Client.HardQueryTimeout - sr.SW.ElapsedMilliseconds);
            if (remainingTime > Client.HardQueryTimeout) {
                remainingTime = 0;
            }
            if (remainingTime < 0) {
                remainingTime = 0;
            }
            //if (remainingTime != Client.HardQueryTimeout) {
            //    l.Info("Remaining time {0}", remainingTime);
            //}
            if (!sr.Barrier.WaitOne(remainingTime)) { // if timeout...
                // l.Info("Hard timeout reached");
                res = Response.Timeout(ticket);
            } else {
                res = sr.Response;
            }

            Delete(ticket);

            // barrier.Wait();

            //if (!outstandingRequestBarriers.TryRemove(ticket, out barrier)) {
            //    throw new RPCException(String.Format("Couldn't find barrier for ticket {0}", ticket));
            //}
            return res;
            //Response res;
            //if (!results.TryRemove(ticket, out res)) {
            //    throw new RPCException(String.Format("Couldn't find result for ticket {0}", ticket));
            //}
            // l.Warning("{0}, {1}", results.Count, outstandingRequestBarriers.Count);
        }

        internal void Set(int dispatch, Response response) {
            SignaledResponse sr;
            if (requests.TryGetValue(dispatch, out sr)) {
                sr.Response = response;
                sr.Barrier.Set();
            }
            // this.results.TryRemove(dispatch, out response);
        }

        internal void Add(Query query) {
            requests[query.Dispatch] = new SignaledResponse(query.SW);
        }

        internal void Delete(int ticket) {
            SignaledResponse junk;
            requests.TryRemove(ticket, out junk);
        }
    }
}
