using ServiceStack.Text;
using System;
using System.Collections.Generic;
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

    }
}
