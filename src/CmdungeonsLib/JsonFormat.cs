using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmdungeonsLib
{
    public class JsonFormat
    {
        public struct Config
        {
            public string packs_path;
            public string language;
        }
        public struct PackRegistry
        {
            public string pack_name;
            public int file_format;
            public string description;
            public string creator;
            public string pack_version;
        }
    }
}
