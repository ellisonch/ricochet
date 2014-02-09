using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    /// <summary>
    ///  Provides a ServiceStack.Text-based Serializer (with custom serialization
    ///  for Message types) for Ricochet.
    /// </summary>
    public class ServiceStackWithCustomMessageSerializer : Serializer {
        /// <summary>
        /// Serialization via ServiceStack.Text
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override byte[] Serialize<T>(T thing) {
            //string s = null;
            //if (typeof(T) == typeof(Query)) {
            //    Query query = (Query)thing;
            //    s = query.Handler + "|" + query.Dispatch + "|" + query.MessageData;
            //} else if (typeof(T) == typeof(Response)) {

            //} else {
            //    s = JsonSerializer.SerializeToString<T>(thing);
            //}
            //return Encoding.Default.GetBytes(s);

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
            //var s = streamReader.ReadLine();
            //if (s == null) {
            //    return null;
            //}
            //var pieces = s.Split(new char[] { '|' });
            //Query query = new Query() {
            //    Handler = pieces[0],
            //    Dispatch = Convert.ToInt32(pieces[1]),
            //    MessageData = Encoding.Default.GetBytes(pieces[2]),
            //};
            //return query;

            string s = Encoding.Default.GetString(thing);
            return JsonSerializer.DeserializeFromString<T>(s);
        }
    }
}
