using Common.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ricochet {
    internal class PendingRequests {
        private static readonly ILog l = LogManager.GetCurrentClassLogger();

        private ConcurrentDictionary<int, SignaledResponse> requests = new ConcurrentDictionary<int, SignaledResponse>();
        
        //internal async Task<Response> GetResponse(SignaledResponse sr) {
        //    Response res = await sr.Get();
        //    return res;
        //}

        internal void SetResponse(int dispatch, Response response) {
            SignaledResponse sr;
            if (requests.TryGetValue(dispatch, out sr)) {
                sr.SetResponse(response);
            }
            DeleteRequest(dispatch);
        }

        internal SignaledResponse AddRequest(Query query) {
            var sr = new SignaledResponse(query.Dispatch, query.SW, Client.HardQueryTimeout);
            requests[query.Dispatch] = sr;
            return sr;
        }

        internal void DeleteRequest(int ticket) {
            SignaledResponse junk;
            requests.TryRemove(ticket, out junk);
        }
    }
}
