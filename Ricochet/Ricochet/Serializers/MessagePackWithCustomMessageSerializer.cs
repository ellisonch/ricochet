
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
    public class MessagePackWithCustomMessageSerializer : MessagePackSerializer {
        /// <summary>
        /// Serialize using goofy, custom method.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override byte[] SerializeQuery(Query query) {
            return CustomMessageSerializerHelper.SerializeQuery(query);
        }

        /// <summary>
        /// Deserialize using goofy, custom method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override Query DeserializeQuery(byte[] bytes) {
            return CustomMessageSerializerHelper.DeserializeQuery(bytes);
        }

        /// <summary>
        /// Serialize using goofy, custom method.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public override byte[] SerializeResponse(Response response) {
            return CustomMessageSerializerHelper.SerializeResponse(response);
        }

        /// <summary>
        /// Deserialize using goofy, custom method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override Response DeserializeResponse(byte[] bytes) {
            return CustomMessageSerializerHelper.DeserializeResponse(bytes);
        }
    }
}
