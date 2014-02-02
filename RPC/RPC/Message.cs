using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    public class Message {
        public int Dispatch { get; set; }
        public Type MessageType { get; set; }
        public string MessageData { get; set; }
    }
}
