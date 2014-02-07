using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    class SerialServiceStackStream : Serial {
        internal override void WriteQuery(NetworkStream networkWriter, StreamWriter streamWriter, Query query) {
            JsonSerializer.SerializeToStream<Query>(query, networkWriter);
            // networkWriter.Flush();
        }

        internal override Query ReadQuery(NetworkStream networkReader, StreamReader streamReader) {
            Console.WriteLine("y");
            Query query = JsonSerializer.DeserializeFromStream<Query>(networkReader);
            Console.WriteLine("x: {0}", query);
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
