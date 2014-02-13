using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    /// <summary>
    ///  Provides a ServiceStack.Text-based Serializer (with custom serialization
    ///  for Message types) for Ricochet.
    /// </summary>
    public class ServiceStackWithCustomMessageSerializer2 : ServiceStackSerializer {
        /// <summary>
        /// Serialize using goofy, custom method.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public override byte[] SerializeQuery(Query query) {
            // packet looks like:
            // Dispatch (4 bytes)
            // handler
            // message data (rest of array)
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(query.Dispatch);
                    writer.Write(query.Handler);
                    writer.Write(query.MessageData);
                }
                stream.Flush();
                return stream.GetBuffer();
            }
        }

        /// <summary>
        /// Deserialize using goofy, custom method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override Query DeserializeQuery(byte[] bytes) {
            using (MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length, false, true))
            using (BinaryReader reader = new BinaryReader(stream)) {
                int dispatch = reader.ReadInt32();
                string handler = reader.ReadString();
                byte[] messageData = reader.ReadBytes((int)(stream.Length - stream.Position));
                return new Query() {
                    Handler = handler,
                    Dispatch = dispatch,
                    MessageData = messageData,
                };
            }
        }

        /// <summary>
        /// Serialize using goofy, custom method.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public override byte[] SerializeResponse(Response response) {
            // packet looks like:
            // OK (1 byte)
            // Dispatch (4 bytes)
            // hasErrorMsg
            // errorMsg
            // message data (rest of array)
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    bool hasErrorMsg = response.ErrorMsg != null;

                    writer.Write(response.OK);
                    writer.Write(response.Dispatch);
                    writer.Write(hasErrorMsg);
                    if (hasErrorMsg) {
                        writer.Write(response.ErrorMsg);
                    }
                    writer.Write(response.MessageData);
                }
                stream.Flush();
                return stream.GetBuffer();
            }
        }

        /// <summary>
        /// Deserialize using goofy, custom method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public override Response DeserializeResponse(byte[] bytes) {
            using (MemoryStream stream = new MemoryStream(bytes, 0, bytes.Length, false, true))
            using (BinaryReader reader = new BinaryReader(stream)) {
                bool ok = reader.ReadBoolean();
                int dispatch = reader.ReadInt32();
                bool hasErrorMsg = reader.ReadBoolean();
                string errorMsg = null;
                if (hasErrorMsg) {
                    errorMsg = reader.ReadString();
                }
                byte[] messageData = reader.ReadBytes((int)(stream.Length - stream.Position));
                return new Response() {
                    OK = ok,
                    Dispatch = dispatch,
                    ErrorMsg = errorMsg,
                    MessageData = messageData,
                };
            }
        }
    }
}
