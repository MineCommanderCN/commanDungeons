using System;
using System.Collections.Generic;
using TinyJson;
using SquidCsharp;

namespace CmdungeonsLib
{
    
    public struct Config
    {
        public string lang;
        public bool debug;
        public List<string> enabled_packs;
    }
    
    public class Player
    {
        double health;
        int gold, level, xp;
    }
    public class EntryFormats
    {
        public class Effect
        {

        }
        public class Item
        {

        }
        public class Enemy
        {

        }
        public class Level
        {

        }
    }
    public class Datapack
    {
        public struct registryFormat
        {
            public struct meta_info
            {
                public int file_format;
                public string description;
                public string @namespace;
                public string creator;
                public string pack_version;
            }
            public List<string> languages;
            public Dictionary<string, List<string>> data;
        }
        public registryFormat registry;
        public struct dataFormat
        {
            public Dictionary<string, EntryFormats.Item> items;
            public Dictionary<string, EntryFormats.Effect> effects;
            public Dictionary<string, EntryFormats.Enemy> enemies;
            public Dictionary<string, EntryFormats.Level> levels;
        }
        public  dataFormat data;
        public Dictionary<string, Dictionary<string, string>> translate;
    }
    public class StaticData
    {

        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
        public static Config config;
        public static Dictionary<string, Datapack> packsData;
    }
}
