﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmdungeonsLib
{
    public class JsonFormat
    {
        public class Config
        {
            public string packs_path;
            public string language;
        }
        public class PackRegistry
        {
            public int file_format;
            public Dictionary<string, object> metadata = new Dictionary<string, object>();
        }
    }
}
