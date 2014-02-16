using Ricochet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib {
    public class ClientHelper {
        const int numDistinctPayloads = 10;
        static string[] payloads = new string[numDistinctPayloads];

        const int numWarmupQueries = 5;
        const string payloadPrefix = smallPayload; // or bigPayload


            //for (int i = 0; i < numDistinctPayloads; i++) {
            //    payloads[i] = payloadPrefix + i;
            //}

        public static void warmup(Client client) {
            var q = new AQuery("xxx");
            AResponse ar = null;
            for (int i = 0; i < numWarmupQueries; i++) {
                client.TryCall<AQuery, AResponse>("double", q, out ar);
            }
        }

        public static bool doCall(Client client, long myCount) {
            // string payload = payloads[mycount % numDistinctPayloads];
            string payload = payloadPrefix + myCount;
            var q = new AQuery(payload);
            bool success = false;

            try {
                AResponse ar = null;
                if (client.TryCall<AQuery, AResponse>("double", q, out ar)) {
                    // Debug.Assert(ar.res == payload + payload, String.Format("Something went wrong, {0} != {1}", ar.res, payload + payload));
                    Debug.Assert(ar.res == payload + payload, "Something went wrong, strings didn't match");
                    success = true;
                }
            } catch (Exception e) {
                Console.WriteLine("Something really unexpected happened: {0}", e);
                success = false;
            }
            return success;
        }

        const string smallPayload = "foo bar baz";
        const string bigPayload = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";
    }
}
