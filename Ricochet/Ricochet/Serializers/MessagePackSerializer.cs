
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
        ConcurrentDictionary<Type, MsgPack.Serialization.IMessagePackSingleObjectSerializer> serializers = new ConcurrentDictionary<Type, MsgPack.Serialization.IMessagePackSingleObjectSerializer>();

        /// <summary>
        /// Serialization via MessagePack.cli
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override byte[] Serialize<T>(T thing) {
            MsgPack.Serialization.IMessagePackSingleObjectSerializer serializer;
            if (!serializers.TryGetValue(typeof(T), out serializer)) {
                serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>();
                serializers.TryAdd(typeof(T), serializer);
            }
            return serializer.PackSingleObject(thing);
        }

        /// <summary>
        /// Deserialization via MessagePack.cli
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override T Deserialize<T>(byte[] thing) {
            MsgPack.Serialization.IMessagePackSingleObjectSerializer serializer;
            if (!serializers.TryGetValue(typeof(T), out serializer)) {
                serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>();
                serializers.TryAdd(typeof(T), serializer);
            }
            return (T)serializer.UnpackSingleObject(thing);
        }
    }
}
