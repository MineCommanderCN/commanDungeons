using System;
using SquidCsharp;

namespace CmdungeonsLib
{
    public class CmdungeonsLib
    {
        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
    }

    public class Datapack
    {
        public struct packInfo
        {
            int file_format;
            string description;
            string @namespace;
            string creator;
            string pack_version;
        }
    }
}
