using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ricochet {
    internal class CircularArrayList<T> : IList<T> {
        private T[] _array;
        private int _maxcap; // maximum length of the underlying array

        private int _len = 0; // actual number of elements in list
        private int _head = 0; // where the next insert would go
        private int _start = 0; // where the first value is/would go

        private object _lock = new Object();

        public CircularArrayList(int startSize, int maxSize) {
            _maxcap = maxSize;
            this._array = new T[startSize];
        }

        private bool isFull() {
            lock (_lock) {
                return (_head == _start) && (_len != 0);
            }
        }

        private void tryAndGrowArray() {
            lock (_lock) {
                if (_array.Length >= _maxcap) {
                    return;
                }
                int newLength = Math.Min(_array.Length * 2, _maxcap);
                var newArray = new T[newLength];
                CopyTo(newArray, 0);
                _start = 0;
                _head = _array.Length;
                _len = _array.Length;
                _array = newArray;
            }
        }


        public bool Contains(T item) {
            throw new NotImplementedException();
        }

        public int IndexOf(T item) {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item) {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }

        public T this[int index] {
            get {
                lock (_lock) {
                    var realPos = (_start + index) % _array.Length;
                    return _array[realPos];
                }
            }
            set {
                throw new NotImplementedException();
            }
        }

        public void Add(T item) {
            lock (_lock) {
                // if the list is full
                if (isFull()) {
                    // grow the array if possible
                    tryAndGrowArray();

                    // if the list is still full
                    if (isFull()) {
                        // lose first element
                        _start = (_start + 1) % _array.Length;
                        _len--;
                    }
                }

                _array[_head] = item;
                _head = (_head + 1) % _array.Length;
                _len++;
            }
        }

        public void Clear() {
            lock (_lock) {
                _len = 0;
                _start = 0;
                _head = 0;
            }
        }

        public void CopyTo(T[] array, int arrayIndex) {
            lock (_lock) {
                if (array == null) {
                    throw new ArgumentNullException();
                }
                if (arrayIndex < 0) {
                    throw new ArgumentOutOfRangeException();
                }
                if (_len > array.Length - arrayIndex) {
                    throw new ArgumentException("Array not large enough");
                }
                for (int i = 0; i < _len; i++) {
                    array[arrayIndex + i] = this[i];
                }
            }
        }
        public T[] ToArray() {
            lock (_lock) {
                var myLen = _len;
                var array = new T[myLen];

                // try {
                    CopyTo(array, 0);
                //} catch (Exception e) {
                //    Console.WriteLine(":(");
                //}
                return array;
            }
        }

        public int Count {
            get {
                return _len;
            }
        }

        public bool IsReadOnly {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(T item) {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator() {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}
