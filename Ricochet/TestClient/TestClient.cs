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
using NDesk.Options;

namespace TestClient {
    internal class TestClient {
        string mode = "realistic";
        string serializerName = "messagepack";
        bool show_help = false;
        
        public TestClient(string[] args) {
            OptionSet p = new OptionSet() {
                { "m|mode=", "Which mode to use.  Either 'realistic' or 'flood'.",
                   v => mode = v },
                { "s|serializer=", "Which serializer to use.  Either 'messagepack' or 'servicestack'.",
                   v => serializerName = v },
                //{ "r|repeat=", 
                //   "the number of {TIMES} to repeat the greeting.\n" + 
                //      "this must be an integer.",
                //    (int v) => repeat = v },
                //{ "v", "increase debug message verbosity",
                //   v => { if (v != null) ++verbosity; } },
                { "h|help",  "show this message and exit", 
                   v => show_help = v != null },
            };
            try {
                p.Parse(args);
            } catch (OptionException e) {
                Console.Write("greet: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `TestClient --help' for more information.");
                return;
            }
            if (show_help) {
                p.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }
        }

        static void Main(string[] args) {
            var tc = new TestClient(args);
            tc.Start();
        }

        private void Start() {
            Serializer serializer = GetSerializer();
            BenchClient<AQuery, AResponse> client = GetClient(serializer);
            client.Start();
        }

        private AQuery QueryGen(long num) {
            // string payload = payloads[mycount % numDistinctPayloads];

            const string payloadPrefix = "foo bar baz";
            //const string payloadPrefix = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";

            string payload = payloadPrefix + num;
            return new AQuery(payload);
        }

        private BenchClient<AQuery, AResponse> GetClient(Serializer serializer) {
            BenchClient<AQuery, AResponse> client = null;
            switch (mode) {
                case "realistic":
                    client = new BenchClientRealistic<AQuery, AResponse>(serializer, QueryGen, "double");
                    break;
                case "flood":
                    client = new BenchClientFlood<AQuery, AResponse>(serializer, QueryGen, "double");
                    break;
                default:
                    throw new Exception("Didn't expect your mode");
            }
            return client;
        }

        private Serializer GetSerializer() {
            Serializer serializer = null;
            switch (serializerName) {
                case "servicestack":
                    serializer = new ServiceStackWithCustomMessageSerializer();
                    break;
                case "messagepack":
                    serializer = new MessagePackWithCustomMessageSerializer();
                    break;
                default:
                    throw new Exception("Didn't expect your serializer");
            }
            return serializer;
        }
    }

}
