using RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPCTestLib {
    public class AResponse {
        public string res { get; set; }

        public AResponse(string res) {
            this.res = res;
        }
    }
}
