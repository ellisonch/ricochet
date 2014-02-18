using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ricochet {
    /// <summary>
    /// A representation of some basic statistics for a data set
    /// </summary>
    public class BasicStats {
        /// <summary>
        /// The percentiles we give data for
        /// </summary>
        public static double[] percentileIndices = new double[] {
            0.25,
            0.50,
            0.75,
            0.90,
            0.99,
            0.999,
        };
        /// <summary>
        /// The values of the percentiles given in percentileIndicies
        /// </summary>
        public double[] Percentiles = new double[percentileIndices.Length];
        /// <summary>
        /// Mean of data
        /// </summary>
        public double Mean { get; set; }
        /// <summary>
        /// Min of data
        /// </summary>
        public double Min { get; set; }
        /// <summary>
        /// Max of data
        /// </summary>
        public double Max { get; set; }
        /// <summary>
        /// Standard deviation of data
        /// </summary>
        public double Stddev { get; set; }

        /// <summary>
        /// Don't use this constructor
        /// </summary>
        public BasicStats() {}

        /// <summary>
        /// should be passed something that can be sorted
        /// </summary>
        /// <param name="array"></param>
        public BasicStats(double[] array) {
            // var array = list.ToArray();
            // var array = list.Copy<double>();
            Array.Sort(array);
            for (int i = 0; i < percentileIndices.Length; i++) {
                Percentiles[i] = array[(int)(array.Length * percentileIndices[i])];
            }

            Mean = array.Average();
            Min = array.Min();
            Max = array.Max();
            double numer = array.Sum(time => (time - Mean) * (time - Mean));
            Stddev = Math.Sqrt(numer / array.Length);
        }

        /// <summary>
        /// ToString...
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("[{0:0.00}, ", Min));
            foreach (var p in Percentiles) {
                sb.Append(String.Format("{0:0.00}, ", p));
            }
            sb.Append(String.Format("{0:0.00}], ", Max));
            sb.Append(String.Format("{0:0.00}", Stddev));

            return sb.ToString();
        }
    }
}
