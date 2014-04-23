using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientTestExample;
using ClientTestHelper;
using Ricochet;
using Common.Logging;
using Common.Logging.Simple;

namespace ClientTestExample {
    class ClientTestExample : TestClient<AQuery, AResponse> {
        static void Main(string[] args) {
            // LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(LogLevel.Debug, true, false, false, "HH:mm:ss:fff");
            var tc = new ClientTestExample(args);
            tc.Start();
        }

        public ClientTestExample(string[] args) : base(args, "double", new MessagePackWithCustomMessageSerializer()) { }

        protected override AQuery QueryGen(long num) {
            // string payload = payloads[mycount % numDistinctPayloads];

            const string payloadPrefix = "foo bar baz";
            //const string payloadPrefix = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent faucibus odio sollicitudin porta condimentum. Maecenas non rutrum sapien, dictum tincidunt nibh. Donec lacinia mattis interdum. Quisque pellentesque, ligula non elementum vulputate, massa lacus mattis justo, at iaculis mi lorem vel neque. Aenean cursus vitae nulla non vehicula. Vestibulum venenatis urna ac turpis semper, sed molestie nibh convallis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras pharetra sodales ante dapibus malesuada. Morbi in lectus vulputate tortor elementum congue id quis sem. Duis eget commodo magna. Suspendisse luctus viverra pharetra. Nam lacinia eros id dictum posuere. Ut euismod, enim sit amet laoreet dictum, enim erat adipiscing eros, nec auctor nibh elit sit amet turpis. Morbi hendrerit nibh a urna congue, ac ultricies tellus vulputate. Integer ac velit venenatis, porttitor tellus eu, pretium sapien. Curabitur eget tincidunt odio, ut vehicula nisi. Praesent molestie diam nullam.";

            string payload = payloadPrefix + num;
            return new AQuery(payload);
        }
    }
}
