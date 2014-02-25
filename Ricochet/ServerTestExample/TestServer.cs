﻿using Ricochet;
using ClientTestExample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerTestExample {
    class TestServer {
        delegate string ReverseDel(string s);
        static void Main(string[] args) {
//            GC.GetTotalMemory(true);


//using System.Diagnostics;

//Process currentProc = Process.GetCurrentProcess();
//Once you have a reference to the current process, you can determine its memory usage by reading the PrivateMemorySize64 property.

//long memoryUsed = currentProc.PrivateMemorySize64;



            var server = new Server(IPAddress.Any, 11000, WhichSerializer.Serializer);

            server.Register<AQuery, AResponse>("reverse", Reverse);
            server.Register<AQuery, AResponse>("double", Double);
            try {
                server.Start();
            } catch (RPCException e) {
                Console.WriteLine(" Couldn't start server... \n{0}", e.ToString());
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        public static AResponse Reverse(AQuery s) {
            char[] arr = s.msg.ToCharArray();
            
            Array.Reverse(arr);
            var res = new string(arr);
            return new AResponse(res);
        }

        public static Random r = new Random();
        public static AResponse Double(AQuery s) {
            // simulate some queries being really slow
            //if (r.NextDouble() < 0.01) {
            //    System.Threading.Thread.Sleep(1000);
            //}
            // throw new Exception("asdf");
            var res = s.msg + s.msg;
            // Console.WriteLine(res);
            return new AResponse(res);
        }
    }
}
