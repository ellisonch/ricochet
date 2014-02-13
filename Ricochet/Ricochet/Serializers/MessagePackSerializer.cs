
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Ricochet {
    /// <summary>
    ///  Provides a ServiceStack.Text-based Serializer for Ricochet.
    /// </summary>
    public class MessagePackSerializer : Serializer {
        /// <summary>
        /// Serialization via MessagePack.cli
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override byte[] Serialize<T>(T thing) {
            var serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>();
            return serializer.PackSingleObject(thing);
        }

        /// <summary>
        /// Deserialization via MessagePack.cli
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override T Deserialize<T>(byte[] thing) {
            var serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>();
            return serializer.UnpackSingleObject(thing);
        }
    }
}
