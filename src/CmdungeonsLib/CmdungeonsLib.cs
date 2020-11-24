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
        double health = 20.0;
        int gold = 0;
        int level = 0;
        int xp = 0;
    }
    public class EntryFormats
    {
        public class Skill
        {

        }
        public class Effect
        {
            public bool debuff = false;
            public class EventsFormat
            {
                public string attribute_name;
                public int operation = 0;
                public double amount = 0.0;
                public bool affect_by_level = true;
                public bool @fixed = true;
            }
            public List<EventsFormat> events = new List<EventsFormat>();
        }
        public class Item
        {
            public string equipment;
            public class UseEventFormat
            {
                public string to;
                public string effect_name;
                int time = 1;
                int level = 0;
                int waiting_round = 0;
            }
            public List<UseEventFormat> use_events = new List<UseEventFormat>();
            public class EquipEventFormat
            {
                public string attribute_name;
                int operation = 0;
                double amount = 0.0;
            }
            public List<EquipEventFormat> equip_events = new List<EquipEventFormat>();
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
        public static Config config = new Config();
        public static Dictionary<string, Datapack> packsData = new Dictionary<string, Datapack>();
    }
}
