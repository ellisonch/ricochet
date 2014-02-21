using Ricochet;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestLib;
using System.Diagnostics;
using System.IO;

namespace TestClientRealistic {
    class TestClientRealistic {
        // per client settings
        const int meanRPS = 100;
        const int threadsPerClient = 100;
        const int clientReportInterval = 1000;

        const int targetRatePerThread = (int)(1000.0 / ((double)meanRPS / (double)threadsPerClient));
        const int lowWait = 1;
        const int highWait = targetRatePerThread + (targetRatePerThread - 1);

        const int numberOfClients = 1;

        static Random r = new Random();

        static void Main(string[] args) {
            
        }
    }
}
