using Ricochet;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestLib;
using System.Diagnostics;
using System.IO;

namespace TestClientRealistic {

    class TestClientRealistic {
        // per client settings
        const int meanRPS = 100;
        const int threadsPerClient = 100;
        const int clientReportInterval = 1000;

        const int targetRatePerThread = (int)(1000.0 / ((double)meanRPS / (double)threadsPerClient));
        const int lowWait = 1;
        const int highWait = targetRatePerThread + (targetRatePerThread - 1);

        const int numberOfClients = 1;

        static Random r = new Random();

        static void Main(string[] args) {
            Debug.Assert(lowWait >= 1, "lowWait isn't high enough; reduce spread, increase threads, or raise target");
            
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < numberOfClients; i++) {
                var t = new Thread(OneClient);
                threads.Add(t);
                t.Start();
            }
            Console.ReadLine();
        }
        
        static void OneClient() {
            Client client = new Client("127.0.0.1", 11000, WhichSerializer.Serializer);
            client.WaitUntilConnected();
            ClientHelper.warmup(client);

            TestObject ch = new TestObject() {
                client = client
            };
            
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < threadsPerClient; i++) {
                var t = new Thread(OneClientThread);
                threads.Add(t);
                t.Start(ch);
            }
            new Thread(ClientMonitor).Start(ch);
            ch.barrier.Set();
        }

        static void OneClientThread(object obj) {
            TestObject ch = (TestObject)obj;
            ch.barrier.WaitOne();
            Thread.Sleep(r.Next(0, 1000));
            Interlocked.Increment(ref ch.threadsStarted);
            
            while (true) {
                var mspc = r.Next(lowWait, highWait);
                Thread.Sleep((int)targetRatePerThread);

                Stopwatch sw = Stopwatch.StartNew();
                var res = ClientHelper.doCall(ch.client, mspc);
                sw.Stop();
                double time = sw.Elapsed.TotalMilliseconds;
                ch.times.Add(time);

                if (!res) {
                    Interlocked.Increment(ref ch.failures);
                }

                Interlocked.Increment(ref ch.done);
            }
        }

        static SortedDictionary<int, int> workDone = new SortedDictionary<int, int>();
        static void ClientMonitor(object obj) {
            TestObject ch = (TestObject)obj;
            ch.barrier.WaitOne();
            Stopwatch sw = Stopwatch.StartNew();

            long done = 0;
            long pdone = 0;
            bool startedUp = false;
            long failures = 0;

            while (true) {
                Thread.Sleep(clientReportInterval);
                
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
                var instTime = (double)clientReportInterval / 1000.0;
                var instDone = (done - pdone);
                double irps = (double)(instDone) / instTime;
                double rps = (double)done / overallTime;


                var myTimes = ch.times.ToArray();
                Array.Sort(myTimes);
                var percentile50 = myTimes[(int)(myTimes.Length * 0.50)];
                var percentile99 = myTimes[(int)(myTimes.Length * 0.99)];
                var percentile9999 = myTimes[(int)(myTimes.Length * 0.9999)];
                var max = myTimes.Max();

                Console.Write("{0:0.000} => {1:0.000}", 
                    irps, rps
                );
                Console.Write(" | {0:0.000}, {1:0.000}, {2:0.000}, {3}",
                    percentile50, percentile99, percentile9999, max
                );
                Console.Write(" | done: {0}, total: {1}, fail: {2}",
                    instDone, done, failures
                );
                Console.WriteLine();

                if (overallTime > 100.0) {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in myTimes) {
                        sb.Append(String.Format("{0:0.000}\n", item));
                    }
                    using (StreamWriter outfile = new StreamWriter("times.tsv")) {
                        outfile.Write(sb.ToString());
                    }
                    Console.WriteLine("wrote strings");
                    return;
                }

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
