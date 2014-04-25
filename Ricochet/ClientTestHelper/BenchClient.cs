using Ricochet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientTestHelper {
    public abstract class BenchClient<T1, T2> {
        public abstract void Start();

        protected readonly string address;
        protected readonly int port;

        readonly Func<long, T1> requestGen;
        readonly string requestName;

        const int numDistinctPayloads = 10;
        static string[] payloads = new string[numDistinctPayloads];

        const int numWarmupQueries = 5;
        const string payloadPrefix = smallPayload; // or bigPayload

        public int reportServerStatsInterval = 5000;
        public int reportClientStatsInterval = 2000;

        //for (int i = 0; i < numDistinctPayloads; i++) {
        //    payloads[i] = payloadPrefix + i;
        //}

        public BenchClient(string address, int port, Func<long, T1> requestGen, string requestName) {
            this.address = address;
            this.port = port;
            this.requestGen = requestGen;
            this.requestName = requestName;
        }

        protected void warmup(Client client) {
            // var q = new AQuery("xxx");
            // AResponse ar = null;
            // Console.WriteLine("Warming up...");
            for (int i = 0; i < numWarmupQueries; i++) {
                // client.TryCall<AQuery, AResponse>("double", q, out ar);
                client.Ping();
            }
            // Console.WriteLine("Warmed up.");
        }

        protected bool doCall(Client client, long myCount) {
            T1 request = requestGen(myCount);
            Option<T2> result;
            try {
                result = client.TryCall<T1, T2>(requestName, request);
            } catch (Exception e) {
                Console.WriteLine("Something really unexpected happened: {0}", e);
                return false;
            }
            return result.OK;
        }

        protected bool doCallThrowAway(Client client, long myCount) {
            T1 request = requestGen(myCount);
            bool result;
            try {
                result = client.TryCallThrowAway<T1, T2>(requestName, request);
            } catch (Exception e) {
                Console.WriteLine("Something really unexpected happened: {0}", e);
                return false;
            }
            return result;
        }

        protected async Task<bool> doCallAsync(Client client, long myCount) {
            T1 request = requestGen(myCount);
            Option<T2> result;
            try {
                result = await client.TryCallAsync<T1, T2>(requestName, request);
            } catch (Exception e) {
                Console.WriteLine("Something really unexpected happened: {0}", e);
                return false;
            }
            return result.OK;
        }

        protected async Task<bool> doCallAsyncThrowAway(Client client, long myCount) {
            T1 request = requestGen(myCount);
            bool result;
            try {
                result = await client.TryCallAsyncThrowAway<T1, T2>(requestName, request);
            } catch (Exception e) {
                Console.WriteLine("Something really unexpected happened: {0}", e);
                return false;
            }
            return result;
        }

        protected void ReportServerStats(object obj) {
            Client client = (Client)obj;
            while (client.IsAlive) {
                System.Threading.Thread.Sleep(reportServerStatsInterval);
                // bool success;
                var result = client.TryCall<bool, ServerStats>("_getStats", true);
                if (!result.OK) { continue; }
                ServerStats ss = result.Value;
                // Console.WriteLine("My outgoing queue length: {0}", client.)
                Console.WriteLine("----------------------------------------------");
                foreach (var item in ss.Timers) {
                    Console.WriteLine("{0,-20}: {1}", item.Key, item.Value);
                }
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

        const string smallPayload = "foo bar baz";
        const string bigPayload = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";
    }
}
