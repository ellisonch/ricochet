﻿using Ricochet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientTestHelper {
    public class BenchClientRealistic<T1, T2> : BenchClient<T1, T2> {
        // per client settings
        readonly int meanRPS = 100;
        const int threadsPerClient = 100;

        int targetRatePerThread {
            get {
                return (int)(1000.0 / ((double)meanRPS / (double)threadsPerClient));
            }
        }
        const int lowWait = 1;
        int highWait {
            get {
                return targetRatePerThread + (targetRatePerThread - 1);
            }
        }



        const int numberOfClients = 1;

        static Random r = new Random();
        Serializer serializer;

        public BenchClientRealistic(string address, int port, Serializer s, Func<long, T1> fun, string name, int rate)
            : base(address, port, fun, name) {
            this.meanRPS = rate;
            this.serializer = s;
        }

        public override void Start() {
            Debug.Assert(lowWait >= 1, "lowWait isn't high enough; reduce spread, increase threads, or raise target");

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < numberOfClients; i++) {
                var t = new Thread(OneClient);
                threads.Add(t);
                t.Start();
            }
            Console.ReadLine();
        }

        private async void OneClient() {
            Client client = new Client(address, port, serializer);
            await client.WaitUntilConnected();
            warmup(client);

            if (reportServerStatsInterval != 0) {
                new Thread(ReportServerStats).Start(client);
                // Task.Run(async () => await ReportServerStats(client));
            }

            TestObject ch = new TestObject() {
                client = client
            };

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < threadsPerClient; i++) {
                var t = new Thread(OneClientThread);
                threads.Add(t);
                t.Start(ch);
            }
            if (reportClientStatsInterval != 0) {
                new Thread(ClientMonitor).Start(ch);
            }
            ch.barrier.Set();
        }

        void OneClientThread(object obj) {
            TestObject ch = (TestObject)obj;
            ch.barrier.WaitOne();
            Thread.Sleep(r.Next(0, 1000));
            Interlocked.Increment(ref ch.threadsStarted);

            while (true) {
                var mspc = r.Next(lowWait, highWait);
                Thread.Sleep((int)targetRatePerThread);

                Stopwatch sw = Stopwatch.StartNew();
                var res = doCall(ch.client, mspc);
                sw.Stop();
                double time = sw.Elapsed.TotalMilliseconds;
                ch.times.Add(time);

                if (!res) {
                    Interlocked.Increment(ref ch.failures);
                }

                Interlocked.Increment(ref ch.done);
            }
        }

        SortedDictionary<int, int> workDone = new SortedDictionary<int, int>();
        void ClientMonitor(object obj) {
            TestObject ch = (TestObject)obj;
            ch.barrier.WaitOne();
            Stopwatch sw = Stopwatch.StartNew();

            long done = 0;
            long pdone = 0;
            bool startedUp = false;
            long failures = 0;

            while (true) {
                Thread.Sleep(reportClientStatsInterval);

                var threads = Interlocked.Read(ref ch.threadsStarted);
                if (!startedUp) {
                    Interlocked.Exchange(ref ch.done, 0);
                    ch.times = new ConcurrentBag<double>();
                    if (threads == threadsPerClient) {
                        startedUp = true;
                        Console.WriteLine("Started up");
                    }
                    sw.Restart();
                    continue;
                }

                pdone = done;
                done = Interlocked.Read(ref ch.done);
                if (done == 0) { continue; }
                failures = Interlocked.Read(ref ch.failures);

                var overallTime = sw.Elapsed.TotalMilliseconds / 1000.0;
                var instTime = (double)reportClientStatsInterval / 1000.0;
                var instDone = (done - pdone);
                double irps = (double)(instDone) / instTime;
                double rps = (double)done / overallTime;

                var stats = new BasicStats(ch.times.ToArray());

                //var myTimes = ch.times.ToArray();
                //Array.Sort(myTimes);
                //var percentile50 = myTimes[(int)(myTimes.Length * 0.50)];
                //var percentile99 = myTimes[(int)(myTimes.Length * 0.99)];
                //var percentile999 = myTimes[(int)(myTimes.Length * 0.999)];
                //var max = myTimes.Max();

                //Console.Write("{0:0.000} => {1:0.000}", 
                //    irps, rps
                //);
                Console.Write("{0:0.00}", rps);
                Console.Write(" | {0}", stats.ToString());
                //Console.Write(" | {0:0.000}, {1:0.000}, {2:0.000}, {3}",
                //    percentile50, percentile99, percentile999, max
                //);
                Console.Write(" | total: {0}, fail: {1}",
                    done, failures
                );
                Console.WriteLine();

                //if (overallTime > 100.0) {
                //    StringBuilder sb = new StringBuilder();
                //    foreach (var item in ch.times) {
                //        sb.Append(String.Format("{0:0.000}\n", item));
                //    }
                //    using (StreamWriter outfile = new StreamWriter("times.tsv")) {
                //        outfile.Write(sb.ToString());
                //    }
                //    Console.WriteLine("wrote strings");
                //    return;
                //}

                // print out histogram
                //if (!workDone.ContainsKey((int)instDone)) {
                //    workDone[(int)instDone] = 0;
                //}
                //workDone[(int)instDone]++;
                //foreach (var item in workDone) {
                //    Console.WriteLine("{0}\t{1}", item.Key, item.Value);
                //}
                //Console.WriteLine("\n");
            }
        }
    }
}
