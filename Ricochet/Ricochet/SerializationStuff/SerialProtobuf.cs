using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    class SerialProtobuf : Serial {
        internal override void WriteQuery(NetworkStream networkWriter, StreamWriter streamWriter, Query query) {
            Serializer.SerializeWithLengthPrefix<Query>(networkWriter, query, PrefixStyle.Base128, 0);
            networkWriter.Flush();
        }

        internal override Query ReadQuery(NetworkStream networkReader, StreamReader streamReader) {
            return ProtoBuf.Serializer.DeserializeWithLengthPrefix<Query>(networkReader, ProtoBuf.PrefixStyle.Base128, 0);
        }

        internal override void WriteResponse(NetworkStream networkWriter, StreamWriter streamWriter, Response response) {
            Serializer.SerializeWithLengthPrefix<Response>(networkWriter, response, PrefixStyle.Base128, 0);
            networkWriter.Flush();
        }

        internal override Response ReadResponse(NetworkStream networkReader, StreamReader streamReader) {
            Response response = ProtoBuf.Serializer.DeserializeWithLengthPrefix<Response>(networkReader, ProtoBuf.PrefixStyle.Base128, 0);
            // Console.WriteLine("x: {0}", response.MessageData);
            return response;
        }
    }
}
