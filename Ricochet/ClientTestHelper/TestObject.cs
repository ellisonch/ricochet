using Ricochet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientTestHelper {
    internal class TestObject {
        public Client client { get; set; }
        public long done = 0;
        public long failures = 0;
        public long threadsStarted = 0;
        public ManualResetEvent barrier = new ManualResetEvent(false);
        public int waitMax = 0;
        public ConcurrentBag<double> times = new ConcurrentBag<double>();
    }
}
