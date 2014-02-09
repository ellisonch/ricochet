using MsgPack.Serialization;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPC {
    class MessagePack : Serial {
        internal override void WriteQuery(System.Net.Sockets.NetworkStream networkStream, System.IO.StreamWriter streamWriter, Query query) {
            var serializer = MessagePackSerializer.Create<Query>();
            serializer.Pack(networkStream, query);
        }

        internal override Query ReadQuery(System.Net.Sockets.NetworkStream networkStream, System.IO.StreamReader streamReader) {
            var serializer = MessagePackSerializer.Create<Query>();
            return serializer.Unpack(networkStream);
        }

        internal override void WriteResponse(System.Net.Sockets.NetworkStream networkStream, System.IO.StreamWriter streamWriter, Response response) {
            var serializer = MessagePackSerializer.Create<Response>();
            serializer.Pack(networkStream, response);
        }

        internal override Response ReadResponse(System.Net.Sockets.NetworkStream networkStream, System.IO.StreamReader streamReader) {
            var serializer = MessagePackSerializer.Create<Response>();
            return serializer.Unpack(networkStream);
        }
    }
}
