using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    // TODO can maybe be replaced with BlockingCollection<T>
    internal class BlockingQueue<T> {
        protected LinkedList<T> queue = new LinkedList<T>();
        private readonly int maxSize;
        bool closing;

        public BlockingQueue(int maxSize) {
            this.maxSize = maxSize;
        }
        public bool EnqueAtFront(T item) {
            lock (queue) {
                if (queue.Count >= maxSize) {
                    return false;
                }
                queue.AddFirst(item);
                Monitor.Pulse(queue);
            }
            return true;
        }

        public bool EnqueueIfRoom(T item) {
            lock (queue) {
                if (queue.Count >= maxSize) {
                    return false;
                }
                // queue.Enqueue(item);
                queue.AddLast(item);
                Monitor.Pulse(queue);
            }
            return true;
        }

        public bool TryDequeue(out T value) {
            lock (queue) {
                while (queue.Count == 0) {
                    if (closing) {
                        value = default(T);
                        return false;
                    }
                    Monitor.Wait(queue);
                }
                // T item = queue.Dequeue();
                value = queue.First();
                queue.RemoveFirst();
                return true;
            }
        }

        internal void Close() {
            lock (queue) {
                closing = true;
                Monitor.PulseAll(queue);
            }
        }
    }
}
