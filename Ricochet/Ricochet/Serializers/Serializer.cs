using System;
using System.Collections.Generic;
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
        /// Converts a thing of type query into a serialized byte array that can be
        /// decoded using DeserializeQuery(byte[]).
        /// </summary>
        /// <param name="thing">The thing to be serialized</param>
        public virtual byte[] SerializeQuery(Query thing) {
            return Serialize<Query>(thing);
        }

        /// <summary>
        /// Converts a thing of type response into a serialized byte array that can be
        /// decoded using DeserializeQuery(byte[]).
        /// </summary>
        /// <param name="thing">The thing to be serialized</param>
        public virtual byte[] SerializeResponse(Response thing) {
            return Serialize<Response>(thing);
        }

        /// <summary>
        /// Converts a byte array coming from SerializeQuery(T thing) into an 
        /// actual object of type T.
        /// </summary>
        /// <param name="thing">The thing to be serialized</param>
        public abstract T Deserialize<T>(byte[] thing);

        /// <summary>
        /// Converts a byte array coming from Serialize{T}(T thing) into an 
        /// actual object of type Query.
        /// </summary>
        /// <param name="thing">The thing to be serialized</param>
        public virtual Query DeserializeQuery(byte[] thing) {
            return Deserialize<Query>(thing);
        }

        /// <summary>
        /// Converts a byte array coming from Serialize{T}(T thing) into an 
        /// actual object of type Query.
        /// </summary>
        /// <param name="thing">The thing to be serialized</param>
        public virtual Response DeserializeResponse(byte[] thing) {
            return Deserialize<Response>(thing);
        }
    }
}
