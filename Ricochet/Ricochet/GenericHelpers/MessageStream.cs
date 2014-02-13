using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;

namespace RPC {
    internal class MessageStream {
        private readonly ILog l = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Writes bytes to the Stream.
        /// 
        /// Assumes this thread has exclusive access to the Stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytes"></param>
        internal static void WriteToStream(Stream stream, byte[] bytes) {
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
        /// <param name="stream"></param>
        internal static byte[] ReadFromStream(Stream stream) {
            byte[] lenBytes = readn(stream, 4);
            int len = BitConverter.ToInt32(lenBytes, 0);
            // Console.WriteLine("Read length {0}", len);
            byte[] bytes = readn(stream, len);
            return bytes;
        }

        private static byte[] readn(Stream stream, int len) {
            byte[] buffer = new byte[len];
            int remaining = len;
            int done = 0;
            do {
                int got = stream.Read(buffer, done, remaining);
                if (got == 0) {
                    // l.Log(Logger.Flag.Warning, "stream.Read returned 0 bytes");
                    throw new Exception("Stream was closed out from under us");
                }
                done += got;
                remaining -= got;
            } while (remaining > 0);
            Debug.Assert(done == len, "The number of bytes received was not the same as the number of bytes asked for");
            Debug.Assert(remaining == 0, "The number of bytes remaining was not 0");
            return buffer;
        }
    }
}
