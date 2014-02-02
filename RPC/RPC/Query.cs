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

        public Stopwatch SW = Stopwatch.StartNew();
        public string Handler { get; set; }

        // TODO: should we give a dispatch number here, or later?
        public static Query CreateQuery<T>(string handler, T data) {
            return new Query {
                // Dispatch = Interlocked.Increment(ref ticketNumber),
                Handler = handler,
                Dispatch = Interlocked.Increment(ref ticketNumber),
                MessageType = typeof(T),
                MessageData = JsonSerializer.SerializeToString(data)
            };
        }
        // serializer doesn't use most specific type?
        public string Serialize() {
            return JsonSerializer.SerializeToString(this);
        }
    }

}
