using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ricochet {
    static class TimingHelper {
        static ConcurrentDictionary<string, CircularArrayList<double>> stats = new ConcurrentDictionary<string, CircularArrayList<double>>();

        public static void Add(string s, double v) {
            if (!stats.ContainsKey(s)) {
                stats[s] = new CircularArrayList<double>(200000, 200000);
            }
            stats[s].Add(v);
        }
        public static void Add(string s, Stopwatch sw) {
            Add(s, sw.Elapsed.TotalMilliseconds);
        }

        public static CircularArrayList<double> Get(string s) {
            CircularArrayList<double> ret = null;
            if (!stats.TryGetValue(s, out ret)) {
                ret = new CircularArrayList<double>(1, 1);
            }
            return ret;
        }

        public static Dictionary<string, BasicStats> Summary() {
            var ret = new Dictionary<string, BasicStats>();
            foreach (var item in stats) {
                ret.Add(item.Key, new BasicStats(item.Value.ToArray()));
            }
            return ret;
        }
        // public 
        // static CircularArrayList<double> workQueueTimes = new CircularArrayList<double>(20000, 20000);

        internal static void Reset() {
            stats = new ConcurrentDictionary<string, CircularArrayList<double>>();
        }
    }
}
