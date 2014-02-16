﻿using Ricochet;
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
    // TODO need to start using volatile in places
    class TestClient {
        // play with these
        const bool reportServer = false;
        const bool reportClient = true;
        const int reportServerStatsTimer = 5000;
        const int reportClientStatsTimer = 5000;
        const int numThreads = 500;



        static ConcurrentBag<long> times = new ConcurrentBag<long>();
        static ConcurrentDictionary<int, double> timeSums = new ConcurrentDictionary<int, double>();

        static long failures = 0;
        static long count = 0;

        static ManualResetEvent threadsReady = new ManualResetEvent(false);
        static volatile int numThreadsReady = 0;

        static Client client;

        static ConcurrentDictionary<int, Thread> threads = new ConcurrentDictionary<int, Thread>();

        static int Main(string[] args) {
            TestClient tc = new TestClient();

            client = new Client("127.0.0.1", 11000, WhichSerializer.Serializer);
            client.WaitUntilConnected();

            if (reportServer) {
#pragma warning disable 0162
                new Thread(ReportServerStats).Start(client);
#pragma warning restore 0162
            }
            if (reportClient) {
#pragma warning disable 0162
                ReportClientStatsHeader();
                new Thread(ReportClientStats).Start(client);
#pragma warning restore 0162
            }

            for (int i = 0; i < numThreads; i++) {
                timeSums[i] = 0.0;
                Thread t = new Thread(ClientWorker);
                threads[i] = t;
                t.Start(i);
            }
            foreach (var thread in threads.Values) {
                thread.Join();
            }
            return 0;
        }

        private static void ClientWorker(object obj) {
            int myThreadNum = (int)obj;
            numThreadsReady++;

            // this waits until all the threads have been created and are here
            while (threadsReady.WaitOne(1)) {
                if (numThreadsReady == numThreads) {
                    threadsReady.Set();
                }
            }
            ClientHelper.warmup(client);

            while (true) {
                var mycount = Interlocked.Increment(ref count);

                Stopwatch mysw = Stopwatch.StartNew();
                bool success = ClientHelper.doCall(client, mycount);
                mysw.Stop();

                long myfailures;
                if (success) {
                    times.Add(mysw.ElapsedTicks);
                    timeSums[myThreadNum] += mysw.ElapsedMilliseconds;
                } else {
                    myfailures = Interlocked.Increment(ref failures);
                }
            }
        }



        private static void ReportClientStatsHeader() {
            Console.WriteLine("\nThe game is to maximize throughput (responses per second (rps)),\nwhile keeping individual response times low.\n");

            Console.WriteLine("\nThe output first shows stuff about instantaneous numbers (across the most recent {0} seconds).", reportClientStatsTimer/1000.0);
            Console.Write("Avg (inst) response time (ms)");
            Console.Write(", (Stddev");
            Console.Write(", 99 Pctile");
            Console.Write(", 99.99 Pctile");
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
        private static void ReportClientStats(object obj) {
            Client client = (Client)obj;
            while (client.IsAlive) {
                System.Threading.Thread.Sleep(reportClientStatsTimer);

                var myTimes = times.ToArray();
                if (myTimes.Length == 0) {
                    continue;
                }
                var theTotal = Interlocked.Read(ref count);
                
                Array.Sort(myTimes);
                var percentile99 = myTimes[(int)(myTimes.Length * 0.99)] / ticksPerMS;
                var percentile9999 = myTimes[(int)(myTimes.Length * 0.9999)] / ticksPerMS;
                var myCount = myTimes.Count();
                var myFailures = Interlocked.Read(ref failures);
                
                times = new ConcurrentBag<long>();
                double avg = myTimes.Average() / ticksPerMS;
                double max = myTimes.Max() / ticksPerMS;
                double numer = myTimes.Sum(time => ((time / ticksPerMS) - avg) * ((time / ticksPerMS) - avg));
                double stddev = Math.Sqrt(numer / myCount);

                arls.Add(avg);

                Console.Write("{0:0.000} ({2:0.00}, {3:00.00}, {4:00.00}, {1:#,000.}), {5:#,###.} rps",
                    avg,
                    max,
                    stddev,
                    percentile99,
                    percentile9999,
                    myTimes.Length / (reportClientStatsTimer / 1000.0)
                );
                Console.Write(" | {0:0.000} {1:0.000} {2:0.000}", arls.Min(), arls.Average(), arls.Max());
                Console.Write(" | done: {0:#,###.}; fail: {1}",
                    theTotal,
                    myFailures
                );
                Console.WriteLine();
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
    }
}
