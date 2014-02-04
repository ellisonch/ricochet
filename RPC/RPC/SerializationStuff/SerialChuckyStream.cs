using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    class SerialChuckyStream : Serial {
        internal override void WriteQuery(NetworkStream networkWriter, StreamWriter streamWriter, Query query) {
            string msgString = query.Handler + "|" + query.Dispatch + "|" + query.MessageType + "|" + query.MessageData;

            streamWriter.WriteLine(msgString);
            streamWriter.Flush();
        }

        internal override Query ReadQuery(NetworkStream networkReader, StreamReader streamReader) {
            var s = streamReader.ReadLine();
            if (s == null) {
                return null;
            }
            var pieces = s.Split(new char[] { '|' });
            Query query = new Query() {
                Handler = pieces[0],
                Dispatch = Convert.ToInt32(pieces[1]),
                MessageType = Type.GetType(pieces[2]),
                MessageData = pieces[3],
            };
            return query;
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
