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
    class TestClient {
        private static IEnumerable<bool> IterateUntilFalse(Func<bool> condition) {
            while (condition()) yield return true;
        }

        static int Main(string[] args) {
            
            // const int numToDo = 10;
            // const int numToDo = 1000;

            // Thread.Sleep(1000);
            Client client = new Client("127.0.0.1", 11000);

            // Console.WriteLine("Press enter to send requests.");
            // Console.ReadLine();

            long failures = 0;
            long done = 0;

            var sw = Stopwatch.StartNew();
            // int i = 0;
            // while (true) {
            // Parallel.For(0, numToDo, i => {
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = 8;
            Parallel.ForEach(IterateUntilFalse(() => { return true; }), po, i => {
                // for (int i = 0; i < numToDo; i++) {
                // Thread.Sleep(1000);
                var payload = "foo bar baz" + i;
                var q = new AQuery(payload);
                Query msg = Query.CreateQuery<AQuery>("double", q);
                Response res = client.Call(msg);
                // this should be tucked in
                long myfailures;
                if (res.OK) {
                    myfailures = Interlocked.Read(ref failures);
                    AResponse ar = JsonSerializer.DeserializeFromString<AResponse>(res.MessageData);
                    if (ar.res != payload + payload) {
                        Console.WriteLine("Something went wrong, {0} != {1}", ar.res, payload + payload);
                        Environment.Exit(1);
                    }
                } else {
                    myfailures = Interlocked.Increment(ref failures);
                }
                var mydone = Interlocked.Increment(ref done);
                if (mydone % 10000 == 0) {
                    double tps = (mydone / (sw.ElapsedMilliseconds / 1000.0));
                    double avg = sw.ElapsedMilliseconds / (double)mydone;
                    Console.WriteLine("{0:0,###.} tps (avg time {3:0.000 ms}) (done: {1}, failures: {2})", tps, mydone, myfailures, avg);
                    // sw.Reset();
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
