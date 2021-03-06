﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    /// <summary>
    ///  Helper for custom serialization of Message types for Ricochet.
    /// </summary>
    public static class CustomMessageSerializerHelper {
        /// <summary>
        /// Serialize using goofy, custom method.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static byte[] SerializeQuery(Query query) {
            // packet looks like:
            // Dispatch (4 bytes)
            // Length of handler (4 bytes)
            // handler
            // message data (rest of array)


            int handlerBytesLen = Encoding.Unicode.GetByteCount(query.Handler);
            int len = 4 + 4 + handlerBytesLen + query.MessageData.Length;
            byte[] bytes = new byte[len];
            
            // BitConverter.GetBytes(query.Dispatch).CopyTo(bytes, 0);
            // intHolder[0] = query.Dispatch;
            // Buffer.BlockCopy(query.Dispatch, 0, bytes, 0, 4);
            FastIntToBytes(bytes, 0, query.Dispatch);
            // BitConverter.GetBytes(handlerBytesLen).CopyTo(bytes, 4);
            // intHolder[0] = handlerBytesLen;
            // Buffer.BlockCopy(intHolder, 0, bytes, 4, 4);
            FastIntToBytes(bytes, 4, handlerBytesLen);

            Encoding.Unicode.GetBytes(query.Handler, 0, query.Handler.Length, bytes, 8);
            query.MessageData.CopyTo(bytes, 8 + handlerBytesLen);
            return bytes;
        }

        // from http://stackoverflow.com/questions/2036718/c-whats-the-fastest-way-of-reading-and-writing-binary by Marc Gravell
        // used here with permission
        static void FastIntToBytes(byte[] target, int index, int value) {
            target[index++] = (byte)value;
            target[index++] = (byte)(value >> 8);
            target[index++] = (byte)(value >> 16);
            target[index] = (byte)(value >> 24);
        }

        /// <summary>
        /// Deserialize using goofy, custom method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Query DeserializeQuery(byte[] bytes) {
            int dispatch = BitConverter.ToInt32(bytes, 0);
            int handlerLen = BitConverter.ToInt32(bytes, 4);
            string handler = Encoding.Unicode.GetString(bytes, 8, handlerLen);
            int messageDataLen = bytes.Length - 4 - 4 - handlerLen;
            byte[] messageData = new byte[messageDataLen];
            Array.Copy(bytes, 4 + 4 + handlerLen, messageData, 0, messageDataLen);
            Query query = new Query() {
                Handler = handler,
                Dispatch = dispatch,
                MessageData = messageData,
            };
            // Console.WriteLine("dispatch: {0}, handler: {1}", dispatch, handler);
            return query;
        }

        static byte[] zeroLenByteArray = new byte[0];
        /// <summary>
        /// Serialize using goofy, custom method.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static byte[] SerializeResponse(Response response) {
            // packet looks like:
            // OK (1 byte)
            // Dispatch (4 bytes)
            // Length of error (4 bytes)
            // errorMsg
            // message data (rest of array)
            byte[] errorBytes;
            if (response.ErrorMsg == null) {
                errorBytes = zeroLenByteArray;
            } else {
                errorBytes = Encoding.Unicode.GetBytes(response.ErrorMsg);
            }
            byte[] messageData;
            if (response.MessageData == null) {
                messageData = zeroLenByteArray;
            } else {
                messageData = response.MessageData;
            }

            int len = 9 + errorBytes.Length + messageData.Length;
            byte[] bytes = new byte[len];

            BitConverter.GetBytes(response.OK).CopyTo(bytes, 0);
            BitConverter.GetBytes(response.Dispatch).CopyTo(bytes, 1);
            BitConverter.GetBytes(errorBytes.Length).CopyTo(bytes, 5);
            errorBytes.CopyTo(bytes, 9);
            messageData.CopyTo(bytes, 9 + errorBytes.Length);
            return bytes;
        }

        /// <summary>
        /// Deserialize using goofy, custom method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Response DeserializeResponse(byte[] bytes) {
            bool ok = BitConverter.ToBoolean(bytes, 0);
            int dispatch = BitConverter.ToInt32(bytes, 1);
            int errorLen = BitConverter.ToInt32(bytes, 5);
            string errorMsg = Encoding.Unicode.GetString(bytes, 9, errorLen);
            int messageDataLen = bytes.Length - 9 - errorLen;
            byte[] messageData = new byte[messageDataLen];
            Array.Copy(bytes, 9 + errorLen, messageData, 0, messageDataLen);
            Response response = new Response() {
                OK = ok,
                Dispatch = dispatch,
                ErrorMsg = errorMsg,
                MessageData = messageData,
            };
            // Console.WriteLine("dispatch: {0}, handler: {1}", dispatch, handler);
            return response;
        }
    }
}
