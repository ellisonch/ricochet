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

        // protected static int ticketNumber = 0;

        //public static T DecodeMessage<T>(string msg) {
        //    var rpcmsg = JsonSerializer.DeserializeFromString<RPCMessage>(msg);
        //    return (T)JsonSerializer.DeserializeFromString(rpcmsg.MessageData, rpcmsg.MessageType);
        //}
    }
}
