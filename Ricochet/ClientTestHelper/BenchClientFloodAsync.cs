using Ricochet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientTestHelper {
    // TODO: consider interface with async
    // serialization based on interface
    // consider auto registering public methods/etc
    // x/y
    // TODO need to start using volatile in places
    public class BenchClientFloodAsync<T1, T2> : BenchClient<T1, T2> {
        // play with these
        // const bool reportServer = true;
        // const int numThreads = 16;

        static ConcurrentBag<long> times = new ConcurrentBag<long>();
        // static ConcurrentDictionary<int, double> timeSums = new ConcurrentDictionary<int, double>();

        static long failures = 0;
        static long count = 0;

        //static ManualResetEvent threadsReady = new ManualResetEvent(false);
        //static volatile int numThreadsReady = 0;

        static Client client;

        // static ConcurrentDictionary<int, Thread> threads = new ConcurrentDictionary<int, Thread>();
        readonly Serializer serializer;

        public BenchClientFloodAsync(string address, int port, Serializer serializer, Func<long, T1> fun, string name)
            : base(address, port, fun, name) {
            this.serializer = serializer;
        }
        public async override void Start() {
            client = new Client(address, port, serializer);
            client.WaitUntilConnected();

            if (reportServerStatsInterval != 0) {
                new Thread(ReportServerStats).Start(client);
            }
            if (reportClientStatsInterval != 0) {
                ReportClientStatsHeader();
                new Thread(ReportClientStats).Start(client);
            }


            for (int i = 0; i < 200; i++) {
                Task t = OneWorkerGroup(8);
            }
            // return Task.Factory.StartNew(
            await Task.Factory.StartNew(() => { });
        }

        private async Task OneWorkerGroup(int num) {
            var tasks = new List<Task>();

            while (true) {
                while (tasks.Count < num) {
                    Task t = ClientWorker();
                    tasks.Add(t);
                }
                var finished = await Task.WhenAny(tasks);
                tasks.Remove(finished);
            }
        }

        private async Task ClientWorker() {
            var mycount = Interlocked.Increment(ref count);

            Stopwatch mysw = Stopwatch.StartNew();
            bool success = await doCallAsync(client, mycount, false);
            mysw.Stop();

            if (success) {
                times.Add(mysw.ElapsedTicks);
                // timeSums[myThreadNum] += mysw.ElapsedMilliseconds;
            } else {
                Interlocked.Increment(ref failures);
            }
        }



        private void ReportClientStatsHeader() {
            Console.WriteLine("\nThe game is to maximize throughput (responses per second (rps)),\nwhile keeping individual response times low.\n");

            Console.WriteLine("\nThe output first shows stuff about instantaneous numbers (across the most recent {0} seconds).", reportClientStatsInterval / 1000.0);
            Console.Write("Avg (inst) response time (ms)");
            Console.Write(", (Stddev");
            Console.Write(", 99 Pctile");
            Console.Write(", 99.9 Pctile");
            Console.Write(", Max)");
            Console.Write(", Requests per second");
            Console.WriteLine("\n");

            Console.WriteLine("Next come overall numbers from the duration of the program.");
            Console.Write("Min");
            Console.Write(", Avg");
            Console.Write(", Max");
            Console.WriteLine("\n");

            Console.WriteLine("Finally are general stats.");
            Console.Write("Total requests processed, total failures");
            Console.WriteLine("\n");

        }
        static double ticksPerMS = (Stopwatch.Frequency / 1000.0);
        static List<double> arls = new List<double>();
        private void ReportClientStats(object obj) {
            Client client = (Client)obj;
            while (client.IsAlive) {
                System.Threading.Thread.Sleep(reportClientStatsInterval);

                var myFailures = Interlocked.Read(ref failures);
                var myTimes = times.ToArray();
                if (myTimes.Length == 0) {
                    Console.WriteLine("No successes this round ({0} failures)", myFailures);
                    continue;
                }
                var theTotal = Interlocked.Read(ref count);

                Array.Sort(myTimes);
                var percentile99 = myTimes[(int)(myTimes.Length * 0.99)] / ticksPerMS;
                var percentile999 = myTimes[(int)(myTimes.Length * 0.999)] / ticksPerMS;
                var myCount = myTimes.Count();


                times = new ConcurrentBag<long>();
                double avg = myTimes.Average() / ticksPerMS;
                double max = myTimes.Max() / ticksPerMS;
                double numer = myTimes.Sum(time => ((time / ticksPerMS) - avg) * ((time / ticksPerMS) - avg));
                double stddev = Math.Sqrt(numer / myCount);

                arls.Add(avg);

                Console.Write("{0:0.000} ({1:0.0}, {2:00.0}, {3:00.0}, {4:#,###.}), {5:#,###.} rps",
                    avg,
                    stddev,
                    percentile99,
                    percentile999,
                    max,
                    myTimes.Length / (reportClientStatsInterval / 1000.0)
                );
                Console.Write(" | {0:0.00} {1:0.00} {2:0.00}", arls.Min(), arls.Average(), arls.Max());
                Console.Write(" | done: {0:#,###.}; fail: {1}",
                    theTotal,
                    myFailures
                );
                Console.WriteLine();
            }
        }
    }
}
