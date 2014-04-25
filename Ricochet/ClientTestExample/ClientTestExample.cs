using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientTestExample;
using ClientTestHelper;
using Ricochet;

namespace ClientTestExample {
    class ClientTestExample : TestClient<AQuery, AResponse> {
        static void Main(string[] args) {
            var tc = new ClientTestExample(args);
            tc.Start();
        }

        const int distinctPayloads = 50;

        private List<AQuery> payloads = new List<AQuery>(distinctPayloads);

        public ClientTestExample(string[] args) : base(args, "double", new MessagePackWithCustomMessageSerializer()) {
            for (int i = 0; i < distinctPayloads; i++) {
                const string payloadPrefix = "foo bar baz";
                string payload = payloadPrefix + i;
                payloads.Add(new AQuery(payload));
            }
        }        

        protected override AQuery QueryGen(long num) {
            return payloads[(int)(num % (long)distinctPayloads)];
        }
    }
}
