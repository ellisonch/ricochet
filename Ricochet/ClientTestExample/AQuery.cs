﻿using Ricochet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientTestExample {
    public class AQuery {
        public string msg { get; set; }

        public AQuery() { } // needed for MessagePack?
        public AQuery(string payload) {
            this.msg = payload;
        }
    }
}
