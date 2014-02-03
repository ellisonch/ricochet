using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPC {
    // TODO should be replaced with real logger
    internal class Logger {
        [FlagsAttribute]
        public enum Flag : int {
            None = 0,
            Debug = 1,
            Info = 2,
            Warning = 4,
            Error = 8,
            Default = Warning | Error,
            All = Debug | Info | Warning | Error,
        }

        Flag flags = Flag.None;

        public Logger(Flag flags) {
            this.flags = flags;
        }

        public void Log(Flag f, string s) {
            this.Log(f, s, null);
        }
        public void Log(Flag f, string s, params object[] p) {
            if (flags.HasFlag(f)) {
                Console.WriteLine(s, p);
            }
        }
    }
}
