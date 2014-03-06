using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Common.Logging;

namespace Ricochet {
    /// <summary>
    ///  Provides a ServiceStack.Text-based Serializer for Ricochet.
    /// </summary>
    public class MessagePackSerializer : Serializer {
        private readonly ILog l = LogManager.GetCurrentClassLogger();

        ConcurrentDictionary<Type, MsgPack.Serialization.IMessagePackSingleObjectSerializer> serializers = new ConcurrentDictionary<Type, MsgPack.Serialization.IMessagePackSingleObjectSerializer>();

        /// <summary>
        /// Serialization via MessagePack.cli
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thing"></param>
        /// <returns></returns>
        public override byte[] Serialize<T>(T thing) {
            Type t = typeof(T);
            MsgPack.Serialization.IMessagePackSingleObjectSerializer serializer;
            if (!serializers.TryGetValue(t, out serializer)) {
                // l.WarnFormat("Creating serializer for {0}", typeof(T));
                serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>();
                serializers.TryAdd(t, serializer);
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
            Type t = typeof(T);
            MsgPack.Serialization.IMessagePackSingleObjectSerializer serializer;
            if (!serializers.TryGetValue(t, out serializer)) {
                // l.WarnFormat("Creating serializer for {0}", typeof(T));
                serializer = MsgPack.Serialization.MessagePackSerializer.Create<T>();
                serializers.TryAdd(t, serializer);
            }
            return (T)serializer.UnpackSingleObject(thing);
        }
    }
}
