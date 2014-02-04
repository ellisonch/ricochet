using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    internal abstract class Serial {
        protected static Logger l = new Logger(Logger.Flag.Default);

        internal abstract void WriteQuery(NetworkStream networkStream, StreamWriter streamWriter, Query query);
        internal abstract Query ReadQuery(NetworkStream networkStream, StreamReader streamReader);

        internal abstract void WriteResponse(NetworkStream networkStream, StreamWriter streamWriter, Response response);
        internal abstract Response ReadResponse(NetworkStream networkStream, StreamReader streamReader);

        internal static byte[] readn(NetworkStream networkReader, int len) {
            byte[] buffer = new byte[len];
            int remaining = len;
            int done = 0;
            do {
                int got = networkReader.Read(buffer, done, remaining);
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
