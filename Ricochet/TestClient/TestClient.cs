using Ricochet;
using TestLib;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient {
    // TODO: consider interface with async
    // serialization based on interface
    // consider auto registering public methods/etc
    // x/y
    class TestClient {
        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition) {
            while (condition()) yield return true;
        }
        const int reportServerStatsTimer = 5000;
        const int reportClientStatsTimer = 5000;
        const int numThreads = 16;

        static ConcurrentBag<long> times = new ConcurrentBag<long>();
        static ConcurrentDictionary<int, double> timeSums = new ConcurrentDictionary<int, double>();

        static long failures = 0;
        static long count = 0;

        static Client client;

        static int Main(string[] args) {
            TestClient tc = new TestClient();

            string junk = smallPayload;
            junk = bigPayload;

            client = new Client("127.0.0.1", 11000, WhichSerializer.Serializer);
            client.WaitUntilConnected();

            // new Thread(ReportServerStats).Start(client);
            new Thread(ReportClientStats).Start(client);

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < numThreads; i++) {
                timeSums[i] = 0.0;
                Thread t = new Thread(ClientWorker);
                threads.Add(t);
                t.Start(i);
            }
            foreach (var thread in threads) {
                thread.Join();
            }
            return 0;
        }

        private static void ClientWorker(object obj) {
            int myThreadNum = (int)obj;
            while (true) {
                var mycount = Interlocked.Increment(ref count);
                var payload = smallPayload + mycount;
                //var payload = bigPayload + mycount;

                var q = new AQuery(payload);
                long myfailures;

                Stopwatch mysw = Stopwatch.StartNew();
                AResponse ar = null;
                bool success;
                try {
                    success = client.TryCall<AQuery, AResponse>("double", q, out ar);
                } catch (ObjectDisposedException) {
                    success = false;
                }
                mysw.Stop();

                if (!success) {
                    myfailures = Interlocked.Increment(ref failures);
                } else {
                    times.Add(mysw.ElapsedTicks);
                    timeSums[myThreadNum] += mysw.ElapsedMilliseconds;
                    myfailures = Interlocked.Read(ref failures);
                    Debug.Assert(ar.res == payload + payload, String.Format("Something went wrong, {0} != {1}", ar.res, payload + payload));
                }
            }
        }

        static double ticksPerMS = (Stopwatch.Frequency / 1000.0);
        private static void ReportClientStats(object obj) {
            Client client = (Client)obj;
            while (client.IsAlive) {
                var myCount = Interlocked.Read(ref count);
                var myFailures = Interlocked.Read(ref failures);
                var myTimes = times;
                times = new ConcurrentBag<long>();
                if (myTimes.Count > 0) {
                    double avg = myTimes.Average() / ticksPerMS;
                    double max = myTimes.Max() / ticksPerMS;
                    double numer = myTimes.Sum(time => ((time / ticksPerMS) - avg) * ((time / ticksPerMS) - avg));
                    double stddev = Math.Sqrt(numer / myTimes.Count());

                    Console.WriteLine("{0:0.000} arl ({1:#,#00.} mrl, {2:0.000} stdrl); {3:#,###.} rps (done: {4}, failures: {5})",
                        avg,
                        max,
                        stddev,
                        myTimes.Count / (reportClientStatsTimer / 1000.0),
                        myCount,
                        myFailures
                    );

                    //foreach (var time in myTimes) {
                    //    Console.WriteLine(time / ticksPerMS);
                    //}
                }
                System.Threading.Thread.Sleep(reportClientStatsTimer);
            }
        }

        private static void ReportServerStats(object obj) {
            Client client = (Client)obj;
            while (client.IsAlive) {
                System.Threading.Thread.Sleep(reportServerStatsTimer);
                bool success;
                ServerStats ss = null;
                try {
                    success = client.TryCall<bool, ServerStats>("_getStats", true, out ss);
                } catch (ObjectDisposedException) {
                    success = false;
                }
                if (!success) { continue; }
                // Console.WriteLine("My outgoing queue length: {0}", client.)
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine("Server queue length: {0}", ss.WorkQueueLength);
                Console.WriteLine("Server worker threads: {0}", ss.ActiveWorkerThreads);
                Console.WriteLine("Server completion port threads: {0}", ss.ActiveCompletionPortThreads);
                foreach (var cs in ss.Clients) {
                    Console.WriteLine("A client:");
                    Console.WriteLine("  Client outgoing queue length: {0}", cs.OutgoingQueueLength);
                    Console.WriteLine("  Client total queries: {0}", cs.IncomingTotal);
                    Console.WriteLine("  Client total responses: {0}", cs.OutgoingTotal);
                }
                Console.WriteLine("----------------------------------------------");
            }
        }
        private static string smallPayload = "foo bar baz";
        private static string bigPayload = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";
    }
}
