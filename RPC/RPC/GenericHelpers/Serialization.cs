using ProtoBuf;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    internal class Serialization {
        internal static T DeserializeFromString<T>(string s) {
            return JsonSerializer.DeserializeFromString<T>(s);
        }
        internal static string SerializeToString<T>(T obj) {
            return JsonSerializer.SerializeToString<T>(obj);
        }

        internal static byte[] SerializeQuery(Query obj) {
            //MemoryStream msTestString = new MemoryStream();
            //Serializer.Serialize(msTestString, obj);
            //return msTestString.ToArray();

            string ret = obj.Handler + "|" + obj.Dispatch + "|" + obj.MessageType + "|" + obj.MessageData;
            return Encoding.ASCII.GetBytes(ret);
        }
        internal static Query DeserializeQuery(byte[] bytes) {
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
        
        internal static string SerializeResponse(Response obj) {
            return JsonSerializer.SerializeToString<Response>(obj);
        }
        internal static Response DeserializeResponse(string s) {
            return JsonSerializer.DeserializeFromString<Response>(s);
        }

    }
}
