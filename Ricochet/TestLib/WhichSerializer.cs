using RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib {
    public class WhichSerializer {
        public static Serializer Serializer = new ServiceStackSerializer();
        // public static Serializer Serializer = new ServiceStackWithCustomMessageSerializer();
    }
}
