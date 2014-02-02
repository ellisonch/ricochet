using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    public class BlockingQueue<T> {
        // protected Queue<T> queue = new Queue<T>();
        protected LinkedList<T> queue = new LinkedList<T>();
        private readonly int maxSize;

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
        //public bool TryCallingFunOnElement(Func<T, bool> fun) {
        //    bool ret;
        //    lock (queue) {
        //        while (queue.Count == 0) {
        //            Monitor.Wait(queue);
        //        }
        //        T query = queue.Peek();
        //        ret = fun(query);
        //        if (ret) {
        //            queue.Dequeue();
        //        }
        //    }
        //    return ret;
        //}

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

        public T Dequeue() {
            lock (queue) {
                while (queue.Count == 0) {
                    Monitor.Wait(queue);
                }
                // T item = queue.Dequeue();
                T item = queue.First();
                queue.RemoveFirst();
                return item;
            }
        }
    }
}
