using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    public class BlockingQueue<T> {
        protected Queue<T> queue = new Queue<T>();
        private readonly int maxSize;

        public BlockingQueue(int maxSize) {
            this.maxSize = maxSize;
        }

        public void TryCallingFunOnElement(Func<T, bool> fun) {
            lock (queue) {
                while (queue.Count == 0) {
                    Monitor.Wait(queue);
                }
                T query = queue.Peek();
                if (fun(query)) {
                    queue.Dequeue();
                }
            }
        }

        public bool EnqueueIfRoom(T item) {
            lock (queue) {
                if (queue.Count >= maxSize) {
                    return false;
                }
                queue.Enqueue(item);
                Monitor.Pulse(queue);
            }
            return true;
        }

        public T Dequeue() {
            lock (queue) {
                while (queue.Count == 0) {
                    Monitor.Wait(queue);
                }
                T item = queue.Dequeue();
                return item;
            }
        }
    }
}
