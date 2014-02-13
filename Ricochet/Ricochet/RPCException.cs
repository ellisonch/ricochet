using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    /// <summary>
    /// Class for RPC Exceptions
    /// </summary>
    public class RPCException : Exception {
        /// <summary>
        /// Default RPC exception
        /// </summary>
        public RPCException() {
        }

        /// <summary>
        /// RPC Exception with a message
        /// </summary>
        public RPCException(string message)
            : base(message) {
        }

        /// <summary>
        /// RPC Exception with internal exception
        /// </summary>
        public RPCException(Exception e)
            : base("RPCException thrown\n", e) {
        }

        /// <summary>
        /// RPC Exception with message and internal exception
        /// </summary>
        public RPCException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}
