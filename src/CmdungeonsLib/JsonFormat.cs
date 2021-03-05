using System;
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
            public string pack_name;
            public int file_format;
            public Hashtable meta_data = new Hashtable();
        }
    }
}
