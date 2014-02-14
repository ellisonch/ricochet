using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ricochet {
    interface IBoundedQueue<T> {
        int Count {get;}
        bool EnqueueIfRoom(T item);
        bool TryDequeue(out T value);
        void Close();
    }
}
