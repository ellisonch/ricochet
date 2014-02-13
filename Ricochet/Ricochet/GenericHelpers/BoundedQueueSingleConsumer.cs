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
    internal class BoundedQueueSingleConsumer<T> {
        private readonly ILog l = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Time to wait for a signal that something has been put into the queue
        /// before checking it manually.
        /// </summary>
        const int dequeueWaitTimeout = 1;

        ManualResetEvent barrier = new ManualResetEvent(false);
        private ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        // private BlockingCollection<T> queue = new BlockingCollection<T>(new ConcurrentQueue<T>());
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
            // queue.Add(item);
            queue.Enqueue(item);
            // Console.WriteLine("Item Enqueued");
            //Debug.Assert(queue.Count > 0, "Count should be positive before set (1)");
            barrier.Set();
            return true;
        }

        public bool TryDequeue(out T value) {
            value = default(T);
            if (closed) { return false; }
            //try {
            //    value = queue.Take();
            //} catch (Exception) {
            //    return false;
            //}
            // l.WarnFormat("Queue length is {0}", queue.Count);
            // l.WarnFormat("Current thread is {0}", Thread.CurrentThread.GetHashCode());
            int oldCount = queue.Count;
            while (oldCount == 0) {
                while (!barrier.WaitOne(dequeueWaitTimeout)) {
                    oldCount = queue.Count;
                    if (oldCount > 0) {
                        barrier.Set();
                    }
                }
                oldCount = queue.Count;
            }
            // Debug.Assert(oldCount > 0, "Count should be greater than 0");
            // l.WarnFormat("Queue length is {0}", count);
            if (!queue.TryDequeue(out value)) {
                throw new Exception("Tried to dequeue, but nothing was there");
            }
            // int newCount = queue.Count();
            // Debug.Assert(newCount >= oldCount - 1, String.Format("Expected count is wrong: {0}, {1}", oldCount, newCount));
            if (oldCount - 1 == 0) {
                barrier.Reset();
            }
            //if (oldCount - 1 >= 1) {
            //    Debug.Assert(queue.Count > 0, "Count should be positive before set (2)");
            //    barrier.Set();
            //}

            //while (queue.Count == 0) {
            //    // l.WarnFormat("Having to wait :(");
            //    System.Threading.Thread.Sleep(dequeueWaitTimeout);
            //}
            
            return true;
        }

        internal void Close() {
            closed = true;
        }
    }
}
