using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RPC {
    /// <summary>
    /// The actual package that is returned from a server representing an RPC 
    /// result.
    /// </summary>
    internal class Response : Message {
        public bool OK { get; set; }
        public Exception Error { get; set; }

        public Response(Exception e) {
            OK = false;
            Error = e;
        }
        public Response() { }

        public static Response CreateResponse<T>(Query query, T data) {
            return new Response {
                OK = true,
                Dispatch = query.Dispatch,
                MessageType = typeof(T),
                MessageData = Serialization.SerializeToString<T>(data)
            };
        }

        public string Serialize() {
            return Serialization.SerializeToString<Response>(this);
        }

        internal static Response Failure() {
            return new Response() {
                OK = false,
                Error = new Exception("Failed to get a result, maybe the connection died")
            };
        }

        internal static Response Timeout(int ticket) {
            return new Response() {
                OK = false,
                Error = new Exception("Timeout"),
                Dispatch = ticket,
            };
        }
    }
}
