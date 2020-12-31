using System;
using System.Collections.Generic;
using TinyJson;
using SquidCsharp;

namespace CmdungeonsLib
{
    public struct Config
    {
        public string packs_path;
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
                public string name; //Name of attribute
                public int operation = 0;   //0 = directly add (base + a1 + a2 + ...)
                                            //1 = overlaying multiply (base * (1 + a1 + a2 + ...) )
                                            //2 = directly multiply (base * a1 * a2 * ...)
                public double amount = 0.0;
            }
            public class Item
            {
                public string equipment;    //'none' (could not be used), 'consumable' (disappear after use) or other strings (for equipment slot ID, move onto the slot)
                public List<EntryFormats.Reg.EffectEvent> use_events = new List<EntryFormats.Reg.EffectEvent>();  //Trigge once when use the item (INVALID for 'none')
                public List<EntryFormats.Reg.AttributeModifiers> attribute_modifiers = new List<EntryFormats.Reg.AttributeModifiers>(); //Modifiers on the attributes when equipmented
            }
            public class Effects
            {
                public bool debuff = false; //If true, the effect will be highlighted when display
                public List<EntryFormats.Reg.AttributeModifiers> modifiers = new List<EntryFormats.Reg.AttributeModifiers>();
                //Modifiers on attributes for the effect
                //NOTE: You can use attribute_name = "generic.health" with operation = 0 to modify the HP!
                //NOTE: Final modified amount is (amount * effect level) !
            }
        }
        public class Log
        {
            public class Effect
            {
                public int level = 0;
                public int time = 0;

                public void Patch(EntryFormats.Log.Effect buf)  //Merge a patch from buf, for overlaying effects
                {
                    time = (buf.time > this.time) ? buf.time : this.time;
                    level = (buf.level > this.level) ? buf.level : this.level; //Select the bigger number
                }
            }
            public class Attribute
            {
                public double @base = 0;
                public List<EntryFormats.Reg.AttributeModifiers> modifiers = new List<EntryFormats.Reg.AttributeModifiers>();
                public double GetValue()
                {
                    double tmp_op0 = 0;
                    double tmp_op1 = 1;
                    double tmp_op2 = 1;
                    foreach (EntryFormats.Reg.AttributeModifiers elem in this.modifiers)
                    {
                        switch (elem.operation)
                        {
                            case 0:
                                tmp_op0 += elem.amount;
                                break;
                            case 1:
                                tmp_op1 += elem.amount;
                                break;
                            case 2:
                                tmp_op2 *= elem.amount;
                                break;
                            default:
                                //TODO: Unknown operation: Maybe some log?
                                break;
                        }
                        return (this.@base + tmp_op0) * tmp_op1 * tmp_op2;
                    }
                    return 0;
                }
            }
            public class Player
            {
                public double health = 20.0;
                public int gold = 0;
                public int level = 0;
                public int xp = 0;
                public Dictionary<string, EntryFormats.Log.Attribute> attributes = new Dictionary<string, EntryFormats.Log.Attribute>();
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
                public List<string> languages = new List<string>(); //Enabled languages
                public Dictionary<string, List<string>> data = new Dictionary<string, List<string>>(); //Enabled data files
                                                                                                       //Key = Category(items, effects, etc.)
                                                                                                       //Value = Entry name list
            }
            public RegistryFormat registry = new RegistryFormat();
            public class DataFormat
            {
                public Dictionary<string, EntryFormats.Reg.Item> items = new Dictionary<string, Reg.Item>();
                //public Dictionary<string, EntryFormats.Reg.Effect> effects;
                //public Dictionary<string, EntryFormats.Reg.Enemy> enemies;
                //public Dictionary<string, EntryFormats.Reg.Level> levels;
            }
            public DataFormat data = new DataFormat();
            public Dictionary<string, Dictionary<string, string>> translate;    //Translate files
                                                                                //Key = Language name
                                                                                //Value = Translate dictionary
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
                    ;   //TODO: Unknown translate string: Maybe some log?
                }
            }
            return tmp;
        }
    }
    public class StaticData
    {
        public const string VERSION = "v.devbuild_20201206";
        public const int SUPPORTED_PACKFORMAT = 1;

        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
        public static Config config = new Config();
        public static Dictionary<string, EntryFormats.Datapack> packsData = new Dictionary<string, EntryFormats.Datapack>();
    }
}
