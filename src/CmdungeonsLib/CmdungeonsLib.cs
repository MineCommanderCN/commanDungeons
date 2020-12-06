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
    
    
    public class EntryFormats
    {
        public class Reg
        {
            public class EffectEvent
            {
                public string name;
                public int time = 0;
                public int level = 0;
            }
            public class AttributeModifiers
            {
                public string name;
                public int operation;
                public double amount;
            }
            public class Item
            {
                public string equipment;
                public List<EntryFormats.Reg.EffectEvent> use_events = new List<EntryFormats.Reg.EffectEvent>();  //trigge once when use the item
            }
            public class effects
            {
                public bool debuff = false;
                public List<EntryFormats.Reg.AttributeModifiers> modifiers = new List<EntryFormats.Reg.AttributeModifiers>();
            }
        }
        public class Log
        {
            
            public class Effect
            {
                public int level = 0;
                public int time = 0;

                public void Patch(EntryFormats.Log.Effect buf)
                {
                    time = (buf.time > time)? buf.time : time;
                    level = (buf.level > level)? buf.level : level; //select the bigger number
                }
            }
            public class Player
            {
                public double health = 20.0;
                public int gold = 0;
                public int level = 0;
                public int xp = 0;
                public Dictionary<string, EntryFormats.Log.Effect> effects = new Dictionary<string, EntryFormats.Log.Effect>();
            }
        }
        public class Datapack
        {
            public class RegistryFormat
            {
                public class MetaInfo
                {
                    public int file_format;
                    public string description;
                    public string @namespace;
                    public string creator;
                    public string pack_version;
                }
                public MetaInfo meta_info = new MetaInfo();
                public List<string> languages = new List<string>(); //enabled languages
                public Dictionary<string, List<string>> data = new Dictionary<string, List<string>>(); //enabled data files
                                                                                                       //key = category(item, effect, etc.)
                                                                                                       //value = entry name list
            }
            public RegistryFormat registry = new RegistryFormat();
            public class DataFormat
            {
                //public Dictionary<string, EntryFormats.Reg.Item> items;
                //public Dictionary<string, EntryFormats.Reg.Effect> effects;
                //public Dictionary<string, EntryFormats.Reg.Enemy> enemies;
                //public Dictionary<string, EntryFormats.Reg.Level> levels;
            }
            public DataFormat data = new DataFormat();
            public Dictionary<string, Dictionary<string, string>> translate;    //translate files
                                                                                //key = language name
                                                                                //value = translate dictionary
        }
    }
    
    public class Tools
    {
        public static string GetTranslateString(string key)
        {
            string tmp = key;
            foreach (EntryFormats.Datapack elem in StaticData.packsData.Values)
            {
                try 
                { 
                    tmp = elem.translate[StaticData.config.lang][key];
                }
                catch
                {
                    ;   //TODO: maybe some log?
                }
            }
            return tmp;
        }
    }
    public class StaticData
    {
        public const string VERSION = "v.devbuild_20201206";

        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
        public static Config config = new Config();
        public static Dictionary<string, EntryFormats.Datapack> packsData = new Dictionary<string, EntryFormats.Datapack>();
    }
}
