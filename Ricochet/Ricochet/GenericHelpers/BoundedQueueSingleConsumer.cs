using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ricochet {
    /// <summary>
    /// The actual bound is maxSize + number of writer threads
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class BoundedQueueSingleConsumer<T> : IBoundedQueue<T> {
        private static readonly ILog l = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Time to wait for a signal that something has been put into the queue
        /// before checking it manually.
        /// </summary>
        const int dequeueWaitTimeout = 10;

        ManualResetEvent barrier = new ManualResetEvent(false);
        private ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        private readonly int maxSize;
        bool closed = false;
        
        public int Count {
            get { return queue.Count; }
        }

        public BoundedQueueSingleConsumer(int maxSize) {
            this.maxSize = maxSize;
        }

        public bool EnqueueIfRoom(T item) {
            if (closed) { return false; }
            if (queue.Count >= maxSize) {
                l.WarnFormat("Reached maximum queue size!  Item dropped.");
                return false;
            }
            queue.Enqueue(item);
            // since there's no lock, this space allows for the possibility that
            // a reader pulled the item we just put on off, so this set isn't
            // guaranteed to be real the next time the reader reads
            barrier.Set();
            return true;
        }

        public bool TryDequeue(out T value) {
            value = default(T);
            if (closed) { return false; }

            int oldCount = queue.Count;
            while (oldCount == 0) {
                while (!barrier.WaitOne(dequeueWaitTimeout)) {
                    if (closed) { return false; }
                    oldCount = queue.Count;
                    if (oldCount > 0) {
                        barrier.Set();
                    }
                }
                oldCount = queue.Count;
            }
            Debug.Assert(oldCount > 0, "Count should be greater than 0");
            var dequeueSuccess = queue.TryDequeue(out value);
            Debug.Assert(dequeueSuccess, "We thought there was something in the queue, but we were wrong");
            if (oldCount - 1 == 0) {
                barrier.Reset();
            }
            
            return true;
        }

        public void Close() {
            closed = true;
        }
    }
}
