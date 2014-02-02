using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    public class Query : Message {
        static private int ticketNumber = 0;
        // static string cache = null;
        public Stopwatch SW = Stopwatch.StartNew();
        public string Handler { get; set; }

        public static Query CreateQuery<T>(string handler, T data) {
            return new Query {
                Handler = handler,
                Dispatch = Interlocked.Increment(ref ticketNumber),
                // Dispatch = 1,
                MessageType = typeof(T),
                MessageData = JsonSerializer.SerializeToString(data)
            };
        }
        // serializer doesn't use most specific type?
        public string Serialize() {
            //if (cache == null) {
            //    cache = JsonSerializer.SerializeToString(this);
            //}
            //return cache;
            //string ret = Handler + "|" + Dispatch + "|" + MessageType + "|" + MessageData;
            //return ret;
            return JsonSerializer.SerializeToString(this);
        }
    }

}
