using Ricochet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientTestExample {
    public class AResponse {
        public string res { get; set; }

        public AResponse() { } // needed for MessagePack?
        public AResponse(string res) {
            this.res = res;
        }
    }
}
