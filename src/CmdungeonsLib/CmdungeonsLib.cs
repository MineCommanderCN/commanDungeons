using System;
using System.Collections.Generic;
using TinyJson;
using SquidCsharp;
using System.Text.RegularExpressions;

namespace CmdungeonsLib
{
    public struct Config
    {
        public string packs_path;
        public string language;
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
            public class AttributeModifier
            {
                public string name; //Name of attribute
                public int operation = 0;   //0 = directly add (base + a1 + a2 + ...)
                                            //1 = overlaying multiply (base * (1 + a1 + a2 + ...) )
                                            //2 = directly multiply (base * a1 * a2 * ...)
                public double amount = 0.0;
            }
            public class Item
            {
                public string equipment;
                //'none' (normal item), 'consumable' (disappear after use) or other strings (for equipment slot ID, move onto the slot after use)
                public List<EntryFormats.Reg.EffectEvent> use_events = new List<EntryFormats.Reg.EffectEvent>();
                //Trigge once when use the item (INVALID for 'none')
                public List<EntryFormats.Reg.AttributeModifier> attribute_modifiers = new List<EntryFormats.Reg.AttributeModifier>();
                //Modifiers on the attributes when equipmented
            }
            public class Effect
            {
                public bool debuff = false; //If true, the effect will be highlighted when display
                public List<EntryFormats.Reg.AttributeModifier> modifiers = new List<EntryFormats.Reg.AttributeModifier>();
                //Modifiers on attributes for the effect.
                //NOTE: You can use attribute_name = "generic.health" with operation = 0 to modify the HP!
                //NOTE: Final modified amount is (amount * effect level) !
            }
            public class Enemy
            {
                public int health = 1;
                public int level = 0;
                Dictionary<string, double> attributes = new Dictionary<string, double>();
                public class RewardsFormat
                {
                    public int gold = 0;
                    public int xp = 0;
                    public class ItemLoot
                    {
                        public string id;
                        public class CountRange
                        {
                            public int min;
                            public int max;
                            public int Roll()
                            {
                                Random rd = new Random();
                                return rd.Next(max - min) + min;
                            }
                        }
                        CountRange count = new CountRange();
                    }
                }
                public RewardsFormat rewards = new RewardsFormat();
            }
            public class Level
            {
                public bool random = false;
                //If true, the enemies will appear randomly, instead of be in set order.
                public bool looping = false;
                // If true, the level will loop again when all enemies have been defeated.
                // The level won't be end unless the player exit manually.
                // Also, the level won't be able to show in finished_levels.
                public List<string> entries = new List<string>();
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
            public class ItemStack
            {
                public string id;
                public int count;

                public EntryFormats.Reg.Item GetItemRegInfo()
                {
                    if (GlobalData.regData.items.ContainsKey(this.id))
                    {
                        return GlobalData.regData.items[id];
                    }
                    else
                    {
                        throw new ApplicationException("Unknown item '" + this.id + "'.");
                    }
                }
            }
            public class Entity
            {
                public string id;   //For player, its id is 'generic:player'.
                public double health = 1.0;
                public int level;
                public Dictionary<string, double> attribute_bases = new Dictionary<string, double>();
                public Dictionary<string, EntryFormats.Log.Effect> effects = new Dictionary<string, EntryFormats.Log.Effect>();
                public Dictionary<string, EntryFormats.Log.ItemStack> equipment = new Dictionary<string, EntryFormats.Log.ItemStack>();

                public double GetAttribute(string attribute_name)
                {
                    double tmp_op0 = 0;
                    double tmp_op1 = 1;
                    double tmp_op2 = 1;
                    List<EntryFormats.Reg.AttributeModifier> modifiers = new List<EntryFormats.Reg.AttributeModifier>();
                    //Get all modifiers from effects and equipment
                    //i'm lazy :|
                    foreach (KeyValuePair<string, EntryFormats.Log.Effect> effect in this.effects)
                    {

                    }

                    foreach (EntryFormats.Reg.AttributeModifier elem in modifiers)
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
                        return (this.attribute_bases[attribute_name] + tmp_op0) * tmp_op1 * tmp_op2;
                    }
                    return 0;
                }
            }


            public class SavesData
            {
                public string name;
                public int gold = 0;
                public int xp = 0;
                public List<EntryFormats.Log.ItemStack> inventory = new List<EntryFormats.Log.ItemStack>();
                public string location; //Empty = home, others = level id
                public EntryFormats.Log.Entity challanging_enemy = new EntryFormats.Log.Entity();
                public List<string> finished_levels = new List<string>();
                //Levels with looping=true can NOT be finished!
            }
        }
        public class Datapack
        {
            public class MetaInfo
            {
                public int file_format;
                public string description;
                public string creator;
                public string pack_version;
            }
            public MetaInfo meta_info = new MetaInfo();
            public List<string> languages = new List<string>(); //Enabled languages
            public Dictionary<string, List<string>> data = new Dictionary<string, List<string>>(); //Enabled data files
                                                                                                   //Key = Category(items, effects, etc.)
        }
    }

    public class Tools
    {
        public static string GetTranslateString(string key)
        {
            if (GlobalData.translates.ContainsKey(GlobalData.config.language))
            {
                if (GlobalData.translates[GlobalData.config.language].ContainsKey(key))
                {
                    return GlobalData.translates[GlobalData.config.language][key];
                }
                else
                {
                    return key;
                }
            }
            else
            {
                return key;
            }
        }
        public static bool IsValidName(string str)
        /*
         *   Judge the str if is a valid name for an entry.
         *   "A valid name for an entry" must:
         *     - Only contains lowercase or uppercase letters (a-z, A-Z), numbers (0-9), underscores (_) and only one colon (:);
         *     - Both sides of colon is not empty.
         *     
         *   Here is the magic pattern: [A-Za-z0-9_]+:{1}[A-Za-z0-9_]+
         *   If the pattern matches the whole origin string, it will be a valid name.
         *   
         *   TODO: Support forward slash (/) for the entries in subfolders.
         */
        {
            if (RegexMatchedStrings(str, @"[A-Za-z0-9_]+:{1}[A-Za-z0-9_]+") == str)
            {
                return true;
            }
            return false;
        }
        public static string RegexMatchedStrings(string input, string pattern)
        //Find all substrings that match the pattern in the input, and connect them.
        {
            MatchCollection mc = Regex.Matches(input, pattern);
            string[] matches = new string[mc.Count];
            string result = "";
            for (int i = 0; i < mc.Count; i++)
            {
                matches[i] = mc[i].Groups[0].Value;
            }
            foreach (string s in matches)
            {
                result = result + s;
            }
            return result;
        }
        public static Dictionary<GT_Key, GT_Value> MergeDictionary<GT_Key, GT_Value>(
            Dictionary<GT_Key, GT_Value> first, Dictionary<GT_Key, GT_Value> second)
        {
            if (first == null)
            {
                first = new Dictionary<GT_Key, GT_Value>();
            }
            if (second == null)
            {
                return first;
            }

            foreach (var item in second)
            {
                if (!first.ContainsKey(item.Key))
                {
                    first.Add(item.Key, item.Value);
                }
                else
                {
                    first[item.Key] = second[item.Key];
                }
            }

            return first;
        }
    }
    public class GlobalData
    {
        public const string VERSION = "v.devbuild_20201206";
        public const int SUPPORTED_PACKFORMAT = 1;
        public readonly static string[] ENTRY_CATEGORIES = new string[4] { "item", "effect", "enemy", "level" };

        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
        public static SquidCoreStates debugStates = new SquidCoreStates();
        public static Config config = new Config();
        public static Dictionary<string, EntryFormats.Datapack> datapackInfo = new Dictionary<string, EntryFormats.Datapack>();
        public class DataFormat
        {
            public Dictionary<string, EntryFormats.Reg.Item> items = new Dictionary<string, EntryFormats.Reg.Item>();
            public Dictionary<string, EntryFormats.Reg.Effect> effects = new Dictionary<string, EntryFormats.Reg.Effect>();
            public Dictionary<string, EntryFormats.Reg.Enemy> enemies = new Dictionary<string, EntryFormats.Reg.Enemy>();
            public Dictionary<string, EntryFormats.Reg.Level> levels = new Dictionary<string, EntryFormats.Reg.Level>();
        }
        public static DataFormat regData = new DataFormat();
        public static Dictionary<string, Dictionary<string, string>> translates = new Dictionary<string, Dictionary<string, string>>();

        public static EntryFormats.Log.Entity playerEntity = new EntryFormats.Log.Entity();
        public static EntryFormats.Log.SavesData saves = new EntryFormats.Log.SavesData();
    }
}
