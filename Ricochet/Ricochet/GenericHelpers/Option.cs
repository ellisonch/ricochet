using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    public class Option<T> {
        public bool OK { get; set; }
        public T Value { get; set; }

        // static Option<T> none = 
        public static Option<T> None() {
            return new Option<T>() {
                OK = false,
                Value = default(T),
            };
        }
        public static Option<T> Some(T v) {
            return new Option<T>() {
                OK = true,
                Value = v,
            };
        }
    }
}
