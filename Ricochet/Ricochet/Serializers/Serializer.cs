using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    /// <summary>
    /// This class can be instantiated to provide a serialization mechanism for 
    /// Ricochet.
    /// </summary>
    public abstract class Serializer {
        /// <summary>
        /// Converts a thing of type T into a serialized byte array that can be
        /// decoded using Deserialize{T}(byte[]).
        /// </summary>
        /// <typeparam name="T">The type of the thing to be serialized</typeparam>
        /// <param name="thing">The thing to be serialized</param>
        public abstract byte[] Serialize<T>(T thing);

        /// <summary>
        /// Converts a byte array coming from Serialize{T}(T thing) into an 
        /// actual object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the thing to be serialized</typeparam>
        /// <param name="thing">The thing to be serialized</param>
        public abstract T Deserialize<T>(byte[] thing);

        /// <summary>
        /// Writes a serialized version of thing on the Stream.
        /// 
        /// Assumes this thread has exclusive access to the Stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <param name="thing"></param>
        public void WriteToStream<T>(Stream stream, T thing) {
            byte[] bytes = Serialize<T>(thing);
            int len = bytes.Length;
            // Console.WriteLine("Writing length {0}", len);
            byte[] lenBytes = BitConverter.GetBytes(len);
            Debug.Assert(lenBytes.Length == 4, "Header should be 4 bytes");

            stream.Write(lenBytes, 0, lenBytes.Length);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        /// <summary>
        /// Reads a serialized version of type T from the Stream.
        /// 
        /// Assumes this thread has exclusive access to the Stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        public T ReadFromStream<T>(Stream stream) {
            byte[] lenBytes = readn(stream, 4);
            int len = BitConverter.ToInt32(lenBytes, 0);
            // Console.WriteLine("Read length {0}", len);
            byte[] bytes = readn(stream, len);

            T thing = Deserialize<T>(bytes);
            return thing;
        }


        private static byte[] readn(Stream stream, int len) {
            byte[] buffer = new byte[len];
            int remaining = len;
            int done = 0;
            do {
                int got = stream.Read(buffer, done, remaining);
                done += got;
                remaining -= got;
            } while (remaining > 0);
            if (done != len) {
                throw new RPCException(String.Format("Wanted {0}, got {1} bytes", len, done));
            }
            if (remaining != 0) {
                throw new RPCException(String.Format("{0} bytes remaining", remaining));
            }
            return buffer;
        }
    }
}
