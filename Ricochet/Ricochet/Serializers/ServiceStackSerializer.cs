using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    /// <summary>
    ///  Provides a ServiceStack.Text-based Serializer for Ricochet.
    /// </summary>
    public class ServiceStackSerializer : Serializer {
        /// <summary>
        /// Serialization via ServiceStack.Text
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override byte[] Serialize<T>(T thing) {
            string s = JsonSerializer.SerializeToString<T>(thing);
            return Encoding.Default.GetBytes(s);
        }

        /// <summary>
        /// Deserialization via ServiceStack.Text
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override T Deserialize<T>(byte[] thing) {
            string s = Encoding.Default.GetString(thing);
            // Console.WriteLine(s);
            return JsonSerializer.DeserializeFromString<T>(s);
        }
    }
}
