using RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPCTestLib {
    public class AQuery {
        public string msg { get; set; }

        public AQuery(string payload) {
            this.msg = payload;
        }
    }
}
