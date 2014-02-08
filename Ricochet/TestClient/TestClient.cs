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
    // TODO something like 64% of time is taken up serializing/deserializing json

    // TODO: consider interface with async
    // serialization based on interface
    // consider auto registering public methods/etc
    // x/y
    //  TODO consider moving out the serialization stuff from the reader/writer
    class TestClient {
        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition) {
            while (condition()) yield return true;
        }
        const int reportEvery = 50000;

        static int Main(string[] args) {
            Client client = new Client("127.0.0.1", 11000);
            client.WaitUntilConnected();

            long failures = 0;
            long done = 0;
            long count = 0;

            var osw = Stopwatch.StartNew();
            var sw = Stopwatch.StartNew();
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 12;
            Parallel.ForEach(IterateUntilFalse(() => { return true; }), po, guard => {
                var mycount = Interlocked.Increment(ref count);
                var payload = "foo bar baz" + mycount;
                //var payload = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";
                // payload += payload + payload + payload + mycount;

                var q = new AQuery(payload);
                // Query msg = Query.CreateQuery<AQuery>("double", q);
                long myfailures;
                AResponse ar;
                // Console.WriteLine(payload);

                // var ar = client.Call(q);

                // var ar = Server.Double2(q, q2, q3);

                // var ar = client.TryDouble2<AQuery, AResponse>(q);
                // public Double(){
                //     return Client.TryCall
                //     else throw exception
                // }
                if (!client.TryCall<AQuery, AResponse>("double", q, out ar)) {
                    // Console.WriteLine("Failure");
                    myfailures = Interlocked.Increment(ref failures);
                } else {
                    myfailures = Interlocked.Read(ref failures);
                    if (ar.res != payload + payload) {
                        Console.WriteLine("Something went wrong, {0} != {1}", ar.res, payload + payload);
                        Environment.Exit(1);
                    }
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
