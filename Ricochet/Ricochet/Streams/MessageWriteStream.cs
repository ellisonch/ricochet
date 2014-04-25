using Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    class MessageWriteStream : IDisposable {
        private static readonly ILog l = LogManager.GetCurrentClassLogger();
        readonly Stream stream;

        bool disposed = false;

        internal MessageWriteStream(Stream stream) {
            // this.stream = stream;
            this.stream = new BufferedStream(stream);
        }

        /// <summary>
        /// Writes bytes to the Stream.
        /// 
        /// Assumes this thread has exclusive access to the Stream.
        /// </summary>
        /// <param name="bytes"></param>
        internal void WriteToStream(byte[] bytes) {
            int len = bytes.Length;
            // Console.WriteLine("Writing length {0}", len);
            byte[] lenBytes = BitConverter.GetBytes(len);
            Debug.Assert(lenBytes.Length == 4, "Header should be 4 bytes");
            stream.Write(lenBytes, 0, lenBytes.Length);
            stream.Write(bytes, 0, bytes.Length);

            //byte[] allBytes = new byte[bytes.Length + 4];
            //lenBytes.CopyTo(allBytes, 0);
            //bytes.CopyTo(allBytes, 4);
            // stream.Write(allBytes, 0, allBytes.Length);
                        
            stream.Flush();
        }

        public void Dispose() {
            if (disposed) { return; }
            if (stream != null) {
                try { stream.Dispose(); } catch (Exception) { }
            }
            disposed = true;
        }
    }
}
