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
                public string operation = "";   
                //Supported values:
                //"directly_add" (base + a1 + a2 + ...),
                //"overlaying_multiply" (base * (1 + a1 + a2 + ...) ),
                //"directly_multiply" (base * a1 * a2 * ...).
                public double amount = 0.0;
            }
            public class Item
            {
                public string type;
                //'normal' (normal item), 'consumable' (disappear after use) or 'equipment' (move onto the equipment slot after use)
                public List<string> slots = new List<string>();
                //(Only for 'equipment' item) Available equipment slot IDs
                public int max_stack = 1;
                //The max count of a stack of the item
                public List<EntryFormats.Reg.EffectEvent> use_events = new List<EntryFormats.Reg.EffectEvent>();
                //(Invalid for 'normal') Trigge once when use the item
                public List<EntryFormats.Reg.AttributeModifier> attribute_modifiers = new List<EntryFormats.Reg.AttributeModifier>();
                //Modifiers on the attributes when equipmented
            }
            public class Effect
            {
                public bool debuff = false; //If true, the effect will be highlighted when display
                public List<EntryFormats.Reg.AttributeModifier> modifiers = new List<EntryFormats.Reg.AttributeModifier>();
                //Modifiers on attributes for the effect.
                //NOTE: You can use attribute_name = "generic.health" to modify the HP! (Operation is forcely set to "directly_add")
                //NOTE: Final modified amount is (amount * effect level) !
            }
            public class Enemy
            {
                public double health = 1.0;
                public int level = 0;
                public Dictionary<string, double> attributes = new Dictionary<string, double>();
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
                        public CountRange count = new CountRange();
                    }
                    public List<ItemLoot> items = new List<ItemLoot>();
                }
                public RewardsFormat rewards = new RewardsFormat();

                public class EquipmentFormat
                {
                    public string id;
                    public double random_chance = 1;
                }
                public Dictionary<string, EquipmentFormat> equipment = new Dictionary<string, EquipmentFormat>();
            }
            public class LevelRoom
            {
                public List<string> enemies = new List<string>();
                public List<EntryFormats.Log.ItemStack> items = new List<EntryFormats.Log.ItemStack>();
            }
            public class Level
            {
                public bool random = false;
                //If true, the enemies will appear randomly, instead of be in set order.
                public bool looping = false;
                // If true, the level will loop again when all enemies have been defeated.
                // The level won't be end unless the player exit manually.
                // Also, the level won't be able to show in finished_levels.
                public List<LevelRoom> rooms = new List<LevelRoom>();
            }
        }
        public class Log
        {
            public class Effect
            {
                public string id;
                public int level = 0;
                public int time = 0;

                public void Patch(EntryFormats.Log.Effect buf)  //Merge a patch from buf, for overlaying effects
                {
                    time = (buf.time > this.time) ? buf.time : this.time;
                    level = (buf.level > this.level) ? buf.level : this.level; //Select the bigger number
                }
                public EntryFormats.Reg.Effect GetRegInfo()
                {
                    if (GlobalData.regData.effects.ContainsKey(this.id))
                    {
                        return GlobalData.regData.effects[id];
                    }
                    else
                    {
                        throw new ApplicationException("Unknown effect '" + this.id + "'.");
                    }
                }
            }
            public class ItemStack
            {
                public string id;
                public int count;

                public Reg.Item GetRegInfo()
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
                public string id;   //Player's entity id is 'generic:player'
                public double health = 1.0;
                public int level = 0;
                public int lifetick = 0;
                public Dictionary<string, double> attribute_bases = new Dictionary<string, double>();
                public List<EntryFormats.Log.Effect> effects = new List<Effect>();
                public Dictionary<string, EntryFormats.Log.ItemStack> equipment = new Dictionary<string, ItemStack>();

                public double GetAttribute(string attribute_name)
                {
                    if (!attribute_bases.ContainsKey(attribute_name))
                    {
                        return 0;
                    }

                    double tmp_op0 = this.attribute_bases[attribute_name];
                    double tmp_op1 = 1;
                    double tmp_op2 = 1;
                    List<EntryFormats.Reg.AttributeModifier> modifiers = new List<Reg.AttributeModifier>();
                    //Get all modifiers from effects and equipment
                    foreach (var effect in this.effects)
                    {
                        modifiers.AddRange(effect.GetRegInfo().modifiers);
                    }
                    foreach (var item in this.equipment.Values)
                    {
                        modifiers.AddRange(item.GetRegInfo().attribute_modifiers);
                    }
                    foreach (var elem in modifiers)
                    {
                        if (elem.name == attribute_name)
                        {
                            switch (elem.operation)
                            {
                                case "directly_add":
                                    tmp_op0 += elem.amount;
                                    break;
                                case "overlaying_multiply":
                                    tmp_op1 += elem.amount;
                                    break;
                                case "directly_multiply":
                                    tmp_op2 *= elem.amount;
                                    break;
                                default:
                                    //TODO: Unknown operation: Maybe some log?
                                    break;
                            }
                        }
                    }
                    return tmp_op0 * tmp_op1 * tmp_op2;
                }
                public int Dice()
                //Roll a point 1~6 with entity's luck attribute.
                {
                    return RollPointWithLuck(GetAttribute("generic:luck"));
                }

                public void Attack(Entity target, int point,
                    out double damageDealted, out double damageBlocked)
                //The higher point gived, the more damage will dealt (1<=point<=6).
                {
                    if (point > 6 || point < 1)
                    {
                        throw new ApplicationException("Invalid dice point.");
                    }
                    damageDealted = this.GetAttribute("generic:attack_power") * 0.2 * point;
                    damageBlocked = target.GetAttribute("generic:armor") * 0.125 * (7 - point);
                    if (damageDealted - damageBlocked <= 0)  //All or even more damage has been blocked
                    {
                        return;
                        //...and nothing happend
                    }
                    else
                    {
                        target.health -= damageDealted - damageBlocked;
                        return;
                    }
                }
                public void Update(out bool death)
                {
                    if (this.health <= 0)
                    {
                        death = true;
                        return;
                    }
                    else
                    {
                        death = false;
                    }
                    if (health > this.GetAttribute("generic:max_health"))
                    {
                        health = GetAttribute("generic:max_health");
                    }
                }
                public void NextTurn(out bool death)
                {
                    lifetick++;
                    foreach (var effect in this.effects)
                    {
                        foreach (var modifier in effect.GetRegInfo().modifiers)
                        {
                            if (modifier.name == "generic:health")
                            {
                                this.health += modifier.amount * effect.level;
                            }
                        }
                        effect.time--;
                        if (effect.time == 0)
                        {
                            this.effects.Remove(effect);
                        }
                    }
                    Update(out death);
                }
                public static int RollPointWithLuck(double l)
                //Roll a point 1~6 with given luck.
                {
                    //l for luck.
                    //w[x] for the weight of rolling a point x (1 <= x <= 6).
                    //There is always w[x]+w[7-x]=2 (e.g if w[1]=0.6, w[6] must be 1.4).
                    double[] w = new double[7]; //w[0] is not for use
                    w[0] = 0;

                    //Calculate weight
                    if (l == 0)
                    {
                        w = new double[7] { 0, 1, 1, 1, 1, 1, 1 };
                    }
                    if (l > 0)
                    {
                        w[1] = (3 * l + 1) / (Math.Pow(l + 1, 2) + 1 * l);
                        w[2] = (4 * l + 1) / (Math.Pow(l + 1, 2) + 2 * l);
                        w[3] = (5 * l + 1) / (Math.Pow(l + 1, 2) + 3 * l);
                        w[4] = 2 - w[3];
                        w[5] = 2 - w[2];
                        w[6] = 2 - w[1];
                    }
                    if (l < 0)
                    {
                        l = -l;
                        w[6] = (3 * l + 2) / (Math.Pow(l + 1, 2) + 1 * l);
                        w[5] = (4 * l + 3) / (Math.Pow(l + 1, 2) + 2 * l);
                        w[4] = (5 * l + 4) / (Math.Pow(l + 1, 2) + 3 * l);
                        w[3] = 2 - w[4];
                        w[2] = 2 - w[5];
                        w[1] = 2 - w[6];
                    }

                    //Roll point with weight
                    Random rand = new Random();
                    double doubleRandNum = Math.Round(rand.NextDouble() * 6.0, 3);

                    for (int i = 1; i <= 6; i++)
                    {
                        if (doubleRandNum >= w[i])
                        {
                            doubleRandNum -= w[i];
                        }
                        else
                        {
                            return i;
                        }
                    }
                    return 1;
                }
            }

            public class Room
            {
                public List<EntryFormats.Log.Entity> entities = new List<EntryFormats.Log.Entity>();
                public List<EntryFormats.Log.ItemStack> dropped_items = new List<EntryFormats.Log.ItemStack>();
            }
            public class SaveData
            {
                public SaveData()
                {
                    player_entity.id = "generic:player";
                    player_entity.health = 20.0;
                    player_entity.level = 0;
                    player_entity.attribute_bases.Add("generic:max_health", 20.0);
                    player_entity.attribute_bases.Add("generic:attack_power", 3.0);
                    player_entity.attribute_bases.Add("generic:armor", 1.0);
                    player_entity.attribute_bases.Add("generic:luck", 0.0);
                    player_entity.attribute_bases.Add("player:inventory_capacity", 20.0);
                }
                public string name = "Player";
                public EntryFormats.Log.Entity player_entity = new Entity();
                public int gold = 0;
                public int xp = 0;
                public List<EntryFormats.Log.ItemStack> inventory = new List<EntryFormats.Log.ItemStack>();
                public string location = ""; //Empty = home, others = level id
                public Dictionary<string, List<EntryFormats.Log.Room>> map = new Dictionary<string, List<EntryFormats.Log.Room>>();
                public List<string> finished_levels = new List<string>();
                public int room_index = -1;
                //Levels with looping=true can NOT be finished!
            }
        }
        public class DatapackRegistry
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
        public static void StackItems(ref List<EntryFormats.Log.ItemStack> itemList, EntryFormats.Log.ItemStack stack,
            int maxCapacity, out EntryFormats.Log.ItemStack itemRemaining)
        {
            int maxStack = stack.GetRegInfo().max_stack;
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].id == stack.id && itemList[i].count < maxStack)
                {
                    itemList[i].count += stack.count;
                    if (itemList[i].count > maxStack)
                    {
                        stack.count = itemList[i].count - maxStack;
                        itemList[i].count = maxStack;
                    }
                    else
                    {
                        stack.count = 0;
                    }
                }
            }
            while (stack.count > 0 && itemList.Count < maxCapacity)
            {
                EntryFormats.Log.ItemStack stackTmp = new EntryFormats.Log.ItemStack();
                stackTmp.id = stack.id;
                stackTmp.count = (stack.count > maxStack) ? maxStack : stack.count;
                stack.count -= stackTmp.count;
                itemList.Add(stackTmp);
            }
            itemRemaining = stack;
        }
        public static void StackItems(ref List<EntryFormats.Log.ItemStack> itemList, EntryFormats.Log.ItemStack stack)
        {
            int maxStack = stack.GetRegInfo().max_stack;
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].id == stack.id && itemList[i].count < maxStack)
                {
                    itemList[i].count += stack.count;
                    if (itemList[i].count > maxStack)
                    {
                        stack.count = itemList[i].count - maxStack;
                        itemList[i].count = maxStack;
                    }
                    else
                    {
                        stack.count = 0;
                    }
                }
            }
            while (stack.count > 0)
            {
                EntryFormats.Log.ItemStack stackTmp = new EntryFormats.Log.ItemStack();
                stackTmp.id = stack.id;
                stackTmp.count = (stack.count > maxStack) ? maxStack : stack.count;
                stack.count -= stackTmp.count;
                itemList.Add(stackTmp);
            }
        }
    }
    public class GlobalData
    {
        public const string VERSION = "v.devbuild_20210123";
        public const int SUPPORTED_PACKFORMAT = 1;
        public readonly static string[] ENTRY_CATEGORIES = new string[4] { "item", "effect", "enemy", "level" };

        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
        public static SquidCoreStates debugStates = new SquidCoreStates();
        public static Config config = new Config();
        public static Dictionary<string, EntryFormats.DatapackRegistry> datapackInfo = new Dictionary<string, EntryFormats.DatapackRegistry>();
        public class DataFormat
        {
            public Dictionary<string, EntryFormats.Reg.Item> items = new Dictionary<string, EntryFormats.Reg.Item>();
            public Dictionary<string, EntryFormats.Reg.Effect> effects = new Dictionary<string, EntryFormats.Reg.Effect>();
            public Dictionary<string, EntryFormats.Reg.Enemy> enemies = new Dictionary<string, EntryFormats.Reg.Enemy>();
            public Dictionary<string, EntryFormats.Reg.Level> levels = new Dictionary<string, EntryFormats.Reg.Level>();
        }
        public static DataFormat regData = new DataFormat();
        public static Dictionary<string, Dictionary<string, string>> translates = new Dictionary<string, Dictionary<string, string>>();
        public static EntryFormats.Log.SaveData save = new EntryFormats.Log.SaveData();
        public static string memorySavesPath = "";
    }
}
