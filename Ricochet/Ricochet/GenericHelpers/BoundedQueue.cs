using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPC {
    // Based on Marc Gravell's code from http://stackoverflow.com/questions/530211/creating-a-blocking-queuet-in-net
    // Used here with permission
    // Changes to the original code made by Chucky Ellison
    internal class BoundedQueue<T> {
        Logger l = new Logger(Logger.Flag.Default);
        protected LinkedList<T> queue = new LinkedList<T>();
        private readonly int maxSize;
        bool closed;

        public BoundedQueue(int maxSize) {
            this.maxSize = maxSize;
        }
        public bool EnqueAtFront(T item) {
            lock (queue) {
                if (closed) { return false; }
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
                if (closed) { return false; }
                if (queue.Count >= maxSize) {
                    l.Log(Logger.Flag.Warning, "Reached maximum queue size!  Item dropped.");
                    return false;
                }
                queue.AddLast(item);
                Monitor.Pulse(queue);
            }
            return true;
        }

        public bool TryDequeue(out T value) {
            lock (queue) { // TODO seems to be lots of contention for this lock (at least on server)
                while (queue.Count == 0) {
                    if (closed) {
                        value = default(T);
                        return false;
                    }
                    Monitor.Wait(queue);
                }
                value = queue.First();
                queue.RemoveFirst();
                return true;
            }
        }

        internal void Close() {
            lock (queue) {
                closed = true;
                Monitor.PulseAll(queue);
            }
        }
    }
}
