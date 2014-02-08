using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    class SerialChuckybuf : Serial {
        internal override void WriteQuery(NetworkStream networkWriter, StreamWriter streamWriter, Query query) {
            string msgString = query.Handler + "|" + query.Dispatch + "|" + query.MessageType + "|" + query.MessageData;
            byte[] msg = Encoding.ASCII.GetBytes(msgString);

            char len = (char)(msg.Length - 1);
            if (len > 255) {
                Console.Write("msg is {0} bytes long", msg.Length);
                throw new RPCException("Can't handle packets larger than 256");
            }
            // Console.WriteLine("Writing length of {0}", (int)len);
            networkWriter.WriteByte((byte)len);
            networkWriter.Write(msg, 0, msg.Length);
            networkWriter.Flush();
        }

        internal override Query ReadQuery(NetworkStream networkReader, StreamReader streamReader) {
            var len = networkReader.ReadByte(); // reader.Read();
            // l.Log(Logger.Flag.Warning, "Looking to read {0} bytes", len+1);
            if (len < 0) {
                throw new RPCException("End of input stream reached");
            }

            byte[] bytes = readn(networkReader, len + 1);

            if (bytes == null || bytes.Length == 0) {
                throw new RPCException("Unable to deserialize empty string");
            }
            //MemoryStream afterStream = new MemoryStream(bytes);
            //return Serializer.Deserialize<Query>(afterStream);
            string s = System.Text.Encoding.Default.GetString(bytes);
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
            var s = JsonSerializer.SerializeToString<Response>(response);
            streamWriter.WriteLine(s);
            streamWriter.Flush();
        }

        internal override Response ReadResponse(NetworkStream networkReader, StreamReader streamReader) {
            string s = streamReader.ReadLine();
            if (s == null) {
                return null;
            }
            Response response = JsonSerializer.DeserializeFromString<Response>(s);
            if (response == null) {
                // TODO should probably kill connection here
                l.Log(Logger.Flag.Warning, "Failed to deserialize response.  Something's really messed up");
            }
            return response;
        }
    }
}
