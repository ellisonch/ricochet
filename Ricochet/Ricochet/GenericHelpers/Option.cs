using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    /// <summary>
    /// Represents either a Some or None type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Option<T> {
        /// <summary>
        /// True if Value is set to something, false if not
        /// </summary>
        public bool OK { get; set; }
        /// <summary>
        /// Actual value of Option, if OK is true
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Creates a new None value
        /// </summary>
        /// <returns></returns>
        public static Option<T> None() {
            return new Option<T>() {
                OK = false,
                Value = default(T),
            };
        }

        /// <summary>
        /// Creates a new Some value
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Option<T> Some(T v) {
            return new Option<T>() {
                OK = true,
                Value = v,
            };
        }
    }
}
