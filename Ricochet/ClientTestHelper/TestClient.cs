using Ricochet;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NDesk.Options;

namespace ClientTestHelper {
    public abstract class TestClient<T1, T2> {
        string mode = "realistic";
        int rate = 100;
        // string serializerName = "messagepack";
        readonly Serializer serializer;
        bool show_help = false;
        readonly string requestName;
        string address = "127.0.0.1";
        int port = 11000;

        int clientReportInterval = 2000;
        int serverReportInterval = 5000;

        public TestClient(string[] args, string requestName, Serializer serializer) {
            this.requestName = requestName;
            this.serializer = serializer;
            OptionSet p = new OptionSet() {
                { "m|mode=", "Which mode to use.  Either 'realistic' or 'flood'.  \nDefault is " + mode + ".",
                   v => mode = v },
                { "rate=", "For realistic mode, the target rate in rps.  \nDefault is " + rate + ".",
                   (int v) => rate = v },
                //{ "s|serializer=", "Which serializer to use.  'messagepack' is the only option for now.  \nDefault is " + serializerName + ".",
                //   v => serializerName = v },
                { "cri|clientReportInterval=", 
                   "Frequency (in ms) client stats should be reported.  \nSet to 0 to disable.  \nDefault is " + clientReportInterval + ".",
                    (int v) => clientReportInterval = v },
                { "sri|serverReportInterval=", 
                   "Frequency (in ms) server stats should be reported.  \nSet to 0 to disable.  \nDefault is " + serverReportInterval + ".",
                    (int v) => serverReportInterval = v },
                { "h|host=", 
                   "Address to connect to.  \nDefault is " + address + ".",
                    (string v) => address = v },
                { "p|port=", 
                   "Port to use.  \nDefault is " + port + ".",
                    (int v) => port = v },
                //{ "v", "increase debug message verbosity",
                //   v => { if (v != null) ++verbosity; } },
                { "help",  "Show this message and exit.", 
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
            // Serializer serializer = GetSerializer();
            BenchClient<T1, T2> client = GetClient(serializer);

            client.reportClientStatsInterval = clientReportInterval;
            client.reportServerStatsInterval = serverReportInterval;

            client.Start();
        }

        protected abstract T1 QueryGen(long num);

        private BenchClient<T1, T2> GetClient(Serializer serializer) {
            BenchClient<T1, T2> client = null;
            switch (mode) {
                case "realistic":
                    client = new BenchClientRealistic<T1, T2>(address, port, serializer, QueryGen, requestName, rate);
                    break;
                case "flood":
                    client = new BenchClientFlood<T1, T2>(address, port, serializer, QueryGen, requestName);
                    break;
                default:
                    throw new Exception("Didn't expect your mode");
            }
            return client;
        }

        //private Serializer GetSerializer() {
        //    Serializer serializer = null;
        //    switch (serializerName) {
        //        //case "servicestack":
        //        //    serializer = new ServiceStackWithCustomMessageSerializer();
        //        //    break;
        //        case "messagepack":
        //            serializer = new MessagePackWithCustomMessageSerializer();
        //            break;
        //        default:
        //            throw new Exception("Didn't expect your serializer");
        //    }
        //    return serializer;
        //}
    }

}
