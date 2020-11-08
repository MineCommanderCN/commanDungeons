using System;
using System.Collections.Generic;
using TinyJson;
using SquidCsharp;

namespace CmdungeonsLib
{
    
    public class Config
    {
        string lang;
        bool debug;
        
    }
    public class Enemy
    {

    }
    public class Player
    {
        double health;
        int gold, level, xp;
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
    public class CmdungeonsLib
    {

        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
        public static Config config;
        
    }
}
