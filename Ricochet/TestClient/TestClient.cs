using RPC;
using TestLib;
using System;
using System.Collections.Generic;
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
    //  TODO consider moving out the serialization stuff from the reader/writer
    // TODO client threads not being shut down when disposed
    class TestClient {
        // const double howUnreliable = 0.000005;
        // const double howUnreliable = 0.00001;
        const double howUnreliable = 0;
        public static Random r = new Random(0);

        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition) {
            while (condition()) yield return true;
        }
        const int reportEvery = 50000;

        long failures = 0;
        long done = 0;
        long count = 0;
        Stopwatch osw = Stopwatch.StartNew();
        Stopwatch sw = Stopwatch.StartNew();

        static int Main(string[] args) {
            TestClient tc = new TestClient();
            while (true) {
                tc.BenchOnce();
            }
            // return 0;
        }

        private int BenchOnce() {
            Client client = new Client("127.0.0.1", 11000, WhichSerializer.Serializer);
            client.WaitUntilConnected();

            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 8;
            Parallel.ForEach(IterateUntilFalse(() => { return true; }), po, (guard, loopstate) => {
                if (r.NextDouble() < howUnreliable) {
                    Console.WriteLine("Simulated network instability!");
                    client.Dispose();
                    loopstate.Stop();
                }
                var mycount = Interlocked.Increment(ref count);
                var payload = "foo bar baz" + mycount;
                //var payload = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";
                // payload += payload + payload + payload + mycount;

                var q = new AQuery(payload);
                // Query msg = Query.CreateQuery<AQuery>("double", q);
                long myfailures;

                AResponse ar = null;
                bool success;
                try {
                    success = client.TryCall<AQuery, AResponse>("double", q, out ar);
                } catch (ObjectDisposedException) {
                    success = false;
                }

                if (!success) {
                    // Console.WriteLine("Failure");
                    myfailures = Interlocked.Increment(ref failures);
                } else {
                    myfailures = Interlocked.Read(ref failures);
                    Debug.Assert(ar.res == payload + payload, String.Format("Something went wrong, {0} != {1}", ar.res, payload + payload));
                }

                var mydone = Interlocked.Increment(ref done);
                if (mydone % reportEvery == 0) {
                    double tps = (reportEvery / (sw.ElapsedMilliseconds / 1000.0));
                    double atps = (mydone / (osw.ElapsedMilliseconds / 1000.0));
                    double avg = sw.ElapsedMilliseconds / (double)reportEvery;
                    double aavg = osw.ElapsedMilliseconds / (double)mydone;
                    Console.WriteLine("{0:#,###.} => {4:#,###.} tps (avg time {3:0.000} => {5:0.0000} ms) (done: {1}, failures: {2})", tps, mydone, myfailures, avg, atps, aavg);
                    sw.Restart();
                }
            });
            return 0;
        }
    }
}
