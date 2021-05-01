using NbtLib;
using Newtonsoft.Json;
using SquidCsharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CmdungeonsLib
{
    public struct Config
    {
        public string packsPath;
        public string language;
    }

    /// <summary>
    /// A stack of items.
    /// </summary>
    public class ItemStack
    {
        private string _id;
        private int _count;
        private NbtCompoundTag _externalData;

        /// <summary>
        /// The registry ID of the item.
        /// </summary>
        public string ID
        {
            get { return _id; }
            set { _id = value; }
        }
        /// <summary>
        /// The Count of the item. Must be over 0.
        /// </summary>
        public int Count
        {
            get { return _count; }
            set
            {
                if (value < 0)
                {
                    throw new Exception("Value is under 0!");
                }
                else
                {
                    _count = value;
                }
            }
        }
        public NbtCompoundTag ExternalData
        {
            get { return _externalData; }
            set { _externalData = value; }
        }

        public ItemStack(string id, int count)
        {
            ID = id;
            Count = count;
            ExternalData = new NbtCompoundTag();
        }
        public ItemStack(string id, int count, NbtCompoundTag externalData)
        {
            ID = id;
            Count = count;
            ExternalData = externalData;
        }

        /// <summary>
        /// Get a <see cref="RegInfo.Item"/> object including the registry info of the item.
        /// </summary>
        public RegInfo.Item RegInfo
        {
            get
            {
                if (GlobalData.Data.regData.items.ContainsKey(ID))
                {
                    return GlobalData.Data.regData.items[ID];
                }
                else
                {
                    throw new ApplicationException("Unknown item '" + ID + "'.");
                }
            }
        }
        public override string ToString()
        {
            return ToString(16);
        }
        public string ToString(int spaces)
        {
            string translatedName = Tools.GetTranslateString("item." + ID + ".name");
            if (translatedName.Length >= spaces)
            {
                return translatedName + " x" + Count.ToString();
            }
            else
            {
                return translatedName + new string(' ', spaces - translatedName.Length) + "x" + Count.ToString();
            }
        }
    }

    /// <summary>
    /// An array of <see cref="ItemStack"/>s with more powerful and suitable methods.
    /// </summary>
    public class ItemList : List<ItemStack>
    {
        /// <summary>
        /// Add an <see cref="ItemStack"/> to the end of the list. Before do it, try to merge the stack into other stacks.
        /// </summary>
        /// <param name="item"></param>
        public new void Add(ItemStack item)
        {
            base.Add(new ItemStack(item.ID, 0, item.ExternalData));
            foreach (var targetStack in this)
            {
                if (item.ID == targetStack.ID && item.ExternalData == targetStack.ExternalData && targetStack.Count < targetStack.RegInfo.maxStack)
                {
                    if (item.Count + targetStack.Count > targetStack.RegInfo.maxStack)
                    {
                        item.Count -= targetStack.RegInfo.maxStack - targetStack.Count;
                        targetStack.Count = targetStack.RegInfo.maxStack;
                        Add(item);
                    }
                    else if (item.ExternalData == targetStack.ExternalData)
                    {
                        targetStack.Count += item.Count;
                        CleanUp();
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// Add a collection of <see cref="ItemStack"/>s into the list.
        /// </summary>
        /// <param name="collection"></param>
        public new void AddRange(IEnumerable<ItemStack> collection)
        {
            base.AddRange(collection);
        }
        /// <summary>
        /// Delete all 0-counted items in the list.
        /// </summary>
        public void CleanUp()
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i].Count == 0)
                {
                    base.RemoveAt(i);
                    i--;
                }
            }
        }
        /*
        /// <summary>
        /// Try to merge all item stacks, then sort them by their IDs.
        /// </summary>
        public new void Sort()
        {
            //Still WIP...
        }
        */

        public override string ToString()
        {
            return ToString(16);
        }
        public string ToString(int spaces)
        {
            string result = "";
            foreach (var item in this)
            {
                result += item.ToString(spaces) + "\n";
            }
            return result;
        }
    }

    public class RegInfo
    {
        public class EffectEvent
        {
            public string name;
            public int time = 0;
            public int level = 0;
        }
        public class AttributeModifier
        {
            public enum Operations
            {
                // Priority of three operations:
                // 
                // e.g. x = base + a1

                DirectlyAdd = 0,
                //  Just simply add the amount onto the base. (Default option)
                //  e.g. x = base + a1 + a2+ ...
                OverlayingMultiply = 1,
                //  Add all "overlaying_multiply" amounts operation and a 1 together,
                //  then multiply the base with this number.
                //  e.g. x = base * (1 + a1 + a2 + ...)
                DirectlyMultiply = 2
                //  Multiply the base with the amount. Easy!
                //  e.g. x = base * a1 * a2 * ...
            }
            public string name; //Name of the attribute
            public Operations operation = Operations.DirectlyAdd;
            public double amount = 0.0;

            public string OperationString
            {
                get
                {
                    return operation.ToString();
                }
                set
                {
                    operation = (Operations)Enum.Parse(typeof(Operations), value);
                }
            }
        }
        public class Item
        {
            public enum Type
            {
                normal = 0,
                //  Normal item (default option)
                consumable = 1,
                //  Disappear after use
                equipment = 2
                //  Move onto the equipment slot after use
            }
            public Type type = Type.normal;
            public Dictionary<string, List<AttributeModifier>> slotEffects = new Dictionary<string, List<AttributeModifier>>();
            //(Only for 'equipment' item) Available equipment slot IDs, and the attribute modifiers on each slot.
            public int maxStack = 1;
            //The max count of a stack of the item.
            public List<EffectEvent> useEvents = new List<EffectEvent>();
        }
        public class Effect
        {
            public bool debuff = false; //If true, the effect will be highlighted when display
            public List<AttributeModifier> modifiers = new List<AttributeModifier>();
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
            public List<ItemStack> items = new List<ItemStack>();
        }
        public class Level
        {
            /// <summary>
            /// If true, the enemies will appear randomly, instead of being in set order.
            /// </summary>
            public bool random = false;

            /// <summary>
            /// If true, the level will loop again when all enemies have been defeated.
            /// The level won't be end unless the player exit manually.
            /// Also, the level won't be able to show in finishedLevels.
            /// </summary>
            public bool looping = false;

            public List<LevelRoom> rooms = new List<LevelRoom>();
        }
    }
    public class Effect
    {
        public string id;
        public int level = 0;
        public int time = 0;

        /// <summary>
        /// Merge a patch from buffer, for overlaying effects
        /// </summary>
        /// <param name="buffer"></param>
        public void Patch(Effect buffer)
        {
            time = (buffer.time > this.time) ? buffer.time : this.time;
            level = (buffer.level > this.level) ? buffer.level : this.level; //Select the bigger number
        }
        public RegInfo.Effect GetRegInfo()
        {
            if (GlobalData.Data.regData.effects.ContainsKey(this.id))
            {
                return GlobalData.Data.regData.effects[id];
            }
            else
            {
                throw new ApplicationException("Unknown effect '" + this.id + "'.");
            }
        }
    }


    public class SoldItem : ItemStack
    {
        public int Price { get; set; }

        public SoldItem(string id, int count, int price) : base(id, count)
        {
            Price = price;
        }
    }
    public class Entity
    {
        public struct EntityUpdateResults
        {
            public bool death;
        }
        public string id;   //Player's entity id is 'generic:player'
        public double health = 1.0;
        public int level = 0;
        public int lifetick = 0;
        public Dictionary<string, double> attribute_bases = new Dictionary<string, double>();
        public List<Effect> effects = new List<Effect>();
        public Dictionary<string, ItemStack> equipment = new Dictionary<string, ItemStack>();

        public double GetAttribute(string attribute_name)
        {
            if (!attribute_bases.ContainsKey(attribute_name))
            {
                return 0;
            }

            double tmp_op0 = attribute_bases[attribute_name];
            double tmp_op1 = 1;
            double tmp_op2 = 1;
            List<RegInfo.AttributeModifier> modifiers = new List<RegInfo.AttributeModifier>();
            //Get all modifiers from effects and equipment
            foreach (var effect in this.effects)
            {
                modifiers.AddRange(effect.GetRegInfo().modifiers);
            }
            foreach (var item in this.equipment)
            {
                modifiers.AddRange(item.Value.RegInfo.slotEffects[item.Key]);
            }
            foreach (var elem in modifiers)
            {
                if (elem.name == attribute_name)
                {
                    switch (elem.operation)
                    {
                        case RegInfo.AttributeModifier.Operations.DirectlyAdd:
                            tmp_op0 += elem.amount;
                            break;
                        case RegInfo.AttributeModifier.Operations.OverlayingMultiply:
                            tmp_op1 += elem.amount;
                            break;
                        case RegInfo.AttributeModifier.Operations.DirectlyMultiply:
                            tmp_op2 *= elem.amount;
                            break;
                        default:
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
        public void Update(out EntityUpdateResults result)
        {
            EntityUpdateResults r;
            if (this.health <= 0)
            {
                r.death = true;
                result = r;
                return;
            }
            else
            {
                r.death = false;
            }
            if (health > this.GetAttribute("generic:max_health"))
            {
                health = GetAttribute("generic:max_health");
            }
            result = r;
        }
        public void NextTurn(out EntityUpdateResults result)
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
            Update(out result);
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
                for (int i = 1; i < 7; i++)
                {
                    if (i <= 3)
                        w[i] = (i + 2) * (l + 1) / (Math.Pow(l + 1, 2) + i * l);
                    else
                        w[i] = 2 - w[7 - i];
                }
            }
            if (l < 0)
            {
                for (int i = 6; i > 0; i--)
                {
                    if (i >= 4)
                        w[i] = (i + 2) * (l + 1) / (Math.Pow(l + 1, 2) + i * l);
                    else
                        w[i] = 2 - w[7 - i];
                }
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
    public class Player : Entity
    {
        public string player_name = "Player";
        public int gold = 0;
        public int xp = 0;
        public ItemList inventory = new();
        public string location = "";
        public int room_index = -1;
        public List<string> finished_levels = new List<string>();
    }
    public class Room
    {
        public class BasicRoom
        {
            public List<ItemStack> droppedItems = new List<ItemStack>();
        }
        public class BattleRoom : BasicRoom
        {
            public List<Entity> enemies = new List<Entity>();
        }
        public class StoreRoom : BasicRoom
        {
            public List<SoldItem> shelf = new List<SoldItem>();
        }
    }

    public class SaveData
    {
        public SaveData()
        {
            player.id = "generic:player";
            player.health = 20.0;
            player.attribute_bases.Add("generic:max_health", 20.0);
            player.attribute_bases.Add("generic:attack_power", 3.0);
            player.attribute_bases.Add("generic:armor", 1.0);
            player.attribute_bases.Add("generic:luck", 0.0);
            player.attribute_bases.Add("player:inventory_capacity", 20.0);
        }
        public Player player = new Player();
        public Dictionary<string, List<Room.BasicRoom>> map = new Dictionary<string, List<Room.BasicRoom>>();
    }

    public class DatapackRegistry
    {
        public int fileFormat;
        public string descriptionTransKey;
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
    }

    public class GlobalData
    {
        private static GlobalData _instance;
        public static GlobalData Data
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlobalData();
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public FileStream LogFileStream;
        public string Version { get; private set; }
        public int[] SupportedPackFormat { get; private set; }
        public string[] EntryCategories { get; private set; }

        public bool debugModeOn;
        public List<string> disabledPacks;
        public bool safeModeOn;
        public SquidCoreStates squidCoreMain;
        public SquidCoreStates debugCommandStates;
        public Config config;
        public Dictionary<string, DatapackRegistry> datapackInfo;
        public class RegistryData
        {
            public Dictionary<string, RegInfo.Item> items;
            public Dictionary<string, RegInfo.Effect> effects;
            public Dictionary<string, RegInfo.Enemy> enemies;
            public Dictionary<string, RegInfo.Level> levels;
            public RegistryData()
            {
                items = new Dictionary<string, RegInfo.Item>();
                effects = new Dictionary<string, RegInfo.Effect>();
                enemies = new Dictionary<string, RegInfo.Enemy>();
                levels = new Dictionary<string, RegInfo.Level>();
            }
        }
        public RegistryData regData;
        public Dictionary<string, Dictionary<string, string>> translates;
        public SaveData save;
        public string memorySavesPath;

        public Jurassic.ScriptEngine scriptEngine;

        private GlobalData()
        {
            Directory.CreateDirectory("logs");
            LogFileStream = new FileStream("logs/" + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HHmmss") + ".log", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            Version = "v.devbuild_20210430";
            SupportedPackFormat = new int[] { 1 };
            EntryCategories = new string[4] { "item", "effect", "enemy", "level" };
            debugModeOn = false;
            disabledPacks = new List<string>();
            safeModeOn = false;
            squidCoreMain = new SquidCoreStates();
            debugCommandStates = new SquidCoreStates();
            datapackInfo = new Dictionary<string, DatapackRegistry>();
            regData = new RegistryData();
            translates = new Dictionary<string, Dictionary<string, string>>();
            save = new SaveData();
            memorySavesPath = "";
            scriptEngine = new Jurassic.ScriptEngine();
        }
    }

    public static class Tools
    {
        public enum MessageType
        {
            Log = 0,
            Warning = 1,
            Error = 2,
            Fatal = 3,
            Info = 4,
            Debug = 5
        }
        public static void OutputLine(string content)
        {
            OutputLine(content, MessageType.Log);
        }
        public static void OutputLine(string content, FileStream logFileStream)
        {
            OutputLine(content, MessageType.Log, logFileStream);
        }
        public static void OutputLine(string content, MessageType type)
        {
            ConsoleColor foregroundColor = ConsoleColor.White, backgroundColor = ConsoleColor.Black;
            switch (type)
            {
                case MessageType.Log:
                    break;
                case MessageType.Warning:
                    content = "[Warning] " + content;
                    foregroundColor = ConsoleColor.Yellow;
                    break;
                case MessageType.Error:
                    content = "[Error] " + content;
                    foregroundColor = ConsoleColor.Red;
                    break;
                case MessageType.Fatal:
                    content = "[fatal] " + content;
                    foregroundColor = ConsoleColor.White;
                    backgroundColor = ConsoleColor.DarkRed;
                    break;
                case MessageType.Info:
                    content = "[Info] " + content;
                    break;
                case MessageType.Debug:
                    content = "[Debug] " + content;
                    foregroundColor = ConsoleColor.Blue;
                    break;
            }
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.WriteLine(content);
            Console.ResetColor();
        }
        public static void OutputLine(string content, MessageType type, FileStream logFileStream)
        {
            OutputLine(content, type);
            LogLine(content, type, logFileStream);
        }
        public static void OutputLine(string content, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            OutputLine(content, MessageType.Log);
            Console.ResetColor();
        }
        public static void OutputLine(string content, ConsoleColor foregroundColor, ConsoleColor backgroundColor, FileStream logFileStream)
        {
            OutputLine(content, foregroundColor, backgroundColor);
            LogLine(content, MessageType.Log, logFileStream);
        }
        public static void LogLine(string content, MessageType type, FileStream logFileStream)
        {
            switch (type)
            {
                case MessageType.Log:
                    content = "[Log] " + content;
                    break;
                case MessageType.Warning:
                    content = "[Warning] " + content;
                    break;
                case MessageType.Error:
                    content = "[Error] " + content;
                    break;
                case MessageType.Fatal:
                    content = "[fatal] " + content;
                    break;
                case MessageType.Info:
                    content = "[Info] " + content;
                    break;
                case MessageType.Debug:
                    content = "[Debug] " + content;
                    break;
            }
            logFileStream.Write(Encoding.UTF8.GetBytes("[" + DateTime.Now.ToLocalTime().ToString("HH:mm:ss") + "]" + content + "\n"));
            logFileStream.Flush();
        }
        public static string GetTranslateString(string key)
        {
            if (GlobalData.Data.translates.ContainsKey(GlobalData.Data.config.language))
            {
                if (GlobalData.Data.translates[GlobalData.Data.config.language].ContainsKey(key))
                {
                    return GlobalData.Data.translates[GlobalData.Data.config.language][key];
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
         */
        {
            string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
            bool hasColon = false;
            foreach (var ch in str)
            {
                if (ch == ':')
                {
                    if (hasColon)
                    {
                        return false;
                    }
                    else
                    {
                        hasColon = true;
                    }
                }
                else if (ch == '/')
                {
                    if (hasColon)
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (!validChars.Contains(ch))
                {
                    return false;
                }
            }
            if (str.EndsWith('/'))
            {
                return false;
            }
            else
            {
                return true;
            }
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
}

