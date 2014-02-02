using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    public class RPCException : Exception {
        public RPCException() {
        }

        public RPCException(string message)
            : base(message) {
        }

        public RPCException(Exception e)
            : base("RPCException thrown\n", e) {
        }

        public RPCException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}
