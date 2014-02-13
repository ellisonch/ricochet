using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;

namespace Ricochet {
    internal class MessageReadStream : IDisposable {
        private readonly ILog l = LogManager.GetCurrentClassLogger();
        Stream stream;

        bool disposed = false;
        //byte[] buf = new byte[8192];
        //int bufOffset = 0;
        //int bufLength = 0;

        internal MessageReadStream(Stream stream) {
            this.stream = new BufferedStream(stream);
        }

        /// <summary>
        /// Reads a serialized version of type T from the Stream.
        /// 
        /// Assumes this thread has exclusive access to the Stream.
        /// </summary>
        internal byte[] ReadFromStream() {
            byte[] lenBytes = read4();
            int len = BitConverter.ToInt32(lenBytes, 0);
            // Console.WriteLine("Read length {0}", len);
            byte[] bytes = readn(len);
            return bytes;
        }

        private byte[] buffer4 = new byte[4];
        private byte[] read4() {
            return readnHelper(4, buffer4);
        }

        private byte[] readn(int len) {
            return readnHelper(len, new byte[len]);
        }

        private byte[] readnHelper(int len, byte[] buffer) {
            Debug.Assert(buffer.Length == len, "Length of buffer should be same as len");
            int remaining = len;
            int done = 0;
            do {
                int got = stream.Read(buffer, done, remaining);
                if (got == 0) {
                    l.WarnFormat("Y stream.Read returned 0 bytes");
                    throw new Exception("Stream was closed out from under us");
                }
                done += got;
                remaining -= got;
            } while (remaining > 0);
            Debug.Assert(done == len, "The number of bytes received was not the same as the number of bytes asked for");
            Debug.Assert(remaining == 0, "The number of bytes remaining was not 0");
            return buffer;
        }

        //private int read(byte[] buffer, int offset, int count) {
        //    // Debug.Assert(count > 0, "Wasn't asked for any bytes");
        //    if (bufOffset == bufLength) {
        //        renewBuffer();
        //    }
        //    // Debug.Assert(bufLength > bufOffset, "buflength <= bufOffset");
        //    int numToCopy = Math.Min((bufLength - bufOffset), count);
        //    // Debug.Assert(numToCopy > 0, "Not copying any bytes");
        //    // Debug.Assert(bufOffset + numToCopy < buf.Length, "Buffer not long enough");
        //    Array.Copy(buf, bufOffset, buffer, offset, numToCopy);
        //    bufOffset += numToCopy;
        //    return numToCopy;
        //}

        //private void renewBuffer() {
        //    // Debug.Assert(buf.Length > 0, "buf.Length not > 0");
        //    bufLength = stream.Read(buf, 0, buf.Length);
        //    if (bufLength == 0) {
        //        l.WarnFormat("X stream.Read returned 0 bytes");
        //        throw new Exception("Stream was closed out from under us");
        //    }
        //    bufOffset = 0;
        //}

        public void Dispose() {
            if (disposed) { return; }
            if (stream != null) {
                try { stream.Dispose(); } catch (Exception) { }
            }
            disposed = true;
        }
    }
}
