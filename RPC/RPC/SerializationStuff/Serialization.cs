using ProtoBuf;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    internal class Serialization {
        // Serial S = new SerialProtobuf();
        static Dictionary<int, Serial> methods = new Dictionary<int, Serial>() {
            {0, new SerialProtobuf()},
            {1, new SerialChuckybuf()},
            {2, new SerialChuckyStream()},
            {3, new SerialServiceStack()},
            {4, new SerialServiceStackStream()}, // doesn't work
        };

        static int method = 3;
        // at 500000 iterations...
        // 0: 49222
        // 1: 51781
        // 2: 54999
        // 3: 51509

        internal static T DeserializeFromString<T>(string s) {
            return JsonSerializer.DeserializeFromString<T>(s);
        }
        internal static string SerializeToString<T>(T obj) {
            return JsonSerializer.SerializeToString<T>(obj);
        }



        internal static void WriteQuery(NetworkStream networkStream, StreamWriter streamWriter, Query query) {
            methods[method].WriteQuery(networkStream, streamWriter, query);
        }

        internal static Query ReadQuery(NetworkStream networkStream, StreamReader streamReader) {
            return methods[method].ReadQuery(networkStream, streamReader);
        }

        internal static void WriteResponse(NetworkStream networkStream, StreamWriter streamWriter, Response response) {
            methods[method].WriteResponse(networkStream, streamWriter, response);
        }
        
        internal static Response ReadResponse(NetworkStream networkStream, StreamReader streamReader) {
            return methods[method].ReadResponse(networkStream, streamReader);
        }
    }
}
