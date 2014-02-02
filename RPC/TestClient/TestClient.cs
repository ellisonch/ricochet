using RPC;
using RPCTestLib;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClient {
    // TODO something like 64% of time is taken up serializing/deserializing json
    class TestClient {
        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition) {
            while (condition()) yield return true;
        }
        const int reportEvery = 10000;

        static int Main(string[] args) {
            
            // const int numToDo = 10;
            // const int numToDo = 1000;

            // Thread.Sleep(1000);
            Client client = new Client("127.0.0.1", 11000);

            // Console.WriteLine("Press enter to send requests.");
            // Console.ReadLine();

            long failures = 0;
            long done = 0;
            long count = 0;

            var sw = Stopwatch.StartNew();
            // int i = 0;
            // while (true) {
            // Parallel.For(0, numToDo, i => {
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 4;
            Parallel.ForEach(IterateUntilFalse(() => { return true; }), po, guard => {
                var mycount = Interlocked.Increment(ref count);
                // for (int i = 0; i < numToDo; i++) {
                // Thread.Sleep(1000);
                var payload = "foo bar baz" + mycount;
                // var payload = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";
                // payload += payload + payload + payload + i;

                var q = new AQuery(payload);
                // Query msg = Query.CreateQuery<AQuery>("double", q);
                long myfailures;
                AResponse ar;
                // Console.WriteLine(payload);
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
                // this should be tucked in
                
                
                var mydone = Interlocked.Increment(ref done);
                if (mydone % reportEvery == 0) {
                    double tps = (reportEvery / (sw.ElapsedMilliseconds / 1000.0));
                    double avg = sw.ElapsedMilliseconds / (double)reportEvery;
                    Console.WriteLine("{0:#,###.} tps (avg time {3:0.000 ms}) (done: {1}, failures: {2})", tps, mydone, myfailures, avg);
                    sw.Restart();
                }
            });

            // client.Disconnect();

            //{
            //    RPCQuery msg = RPCQuery.CreateQuery<GetStarsQuery>("getStars", new GetStarsQuery() { UserId = 268746123 });
            //    RPCResponse res = client.SyncCall(msg);
            //    Console.WriteLine("returned {0}", res);
            //}

            // Console.WriteLine("Press enter to exit.");
            // Console.ReadLine();
            return 0;
        }
    }
}
