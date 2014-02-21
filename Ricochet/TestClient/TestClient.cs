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
    internal abstract class TestClient<T1, T2> {
        string mode = "realistic";
        string serializerName = "messagepack";
        bool show_help = false;
        readonly string requestName;

        public TestClient(string[] args, string requestName) {
            this.requestName = requestName;
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
        
        protected void Start() {
            Serializer serializer = GetSerializer();
            BenchClient<T1, T2> client = GetClient(serializer);
            client.Start();
        }

        protected abstract T1 QueryGen(long num);

        private BenchClient<T1, T2> GetClient(Serializer serializer) {
            BenchClient<T1, T2> client = null;
            switch (mode) {
                case "realistic":
                    client = new BenchClientRealistic<T1, T2>(serializer, QueryGen, requestName);
                    break;
                case "flood":
                    client = new BenchClientFlood<T1, T2>(serializer, QueryGen, requestName);
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
