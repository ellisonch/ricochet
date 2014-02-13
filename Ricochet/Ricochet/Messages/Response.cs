using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ricochet {
    /// <summary>
    /// The actual package that is returned from a server representing an RPC 
    /// result.
    /// </summary>
    public class Response : Message {
        /// <summary>
        /// Whether or not the response was succesful.  If true, client should
        /// be able to deserialize the MessageData.  If false, ErrorMsg should be set.
        /// </summary>
        public bool OK { get; set; }

        /// <summary>
        /// Human readable error message if the call failed.
        /// </summary>
        public string ErrorMsg { get; set; }

        /// <summary>
        /// Create a new, successful response.
        /// </summary>
        /// <typeparam name="T">Type of embedded data</typeparam>
        /// <param name="query">Query used to generate this response</param>
        /// <param name="data">Embedded, return data</param>
        /// <param name="serializer">Serializer to use to serialize the embedded data</param>
        /// <returns></returns>
        public static Response CreateResponse<T>(Query query, T data, Serializer serializer) {
            return new Response {
                OK = true,
                Dispatch = query.Dispatch,
                MessageData = serializer.Serialize<T>(data)
            };
        }

        internal static Response Failure(string message) {
            return new Response() {
                OK = false,
                ErrorMsg = message
            };
        }

        internal static Response Timeout(int ticket) {
            return new Response() {
                OK = false,
                ErrorMsg = "Timeout",
                Dispatch = ticket,
            };
        }
    }
}
