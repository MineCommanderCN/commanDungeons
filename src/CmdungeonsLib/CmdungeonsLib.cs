using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SquidCsharp;

namespace CmdungeonsLib
{
    public struct Config
    {
        public string packsPath;
        public string language;
    }

    public class EntryFormat
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
                public enum Operations
                {
                    DirectlyAdd = 0,
                    //  Just simply add the amount onto the base. (Default option)
                    //  Mathematical expression: x = base + a1 + a2+ ...
                    OverlayingMultiply = 1,
                    //  Add all amounts that be with "overlaying_multiply" operation and a 1 together,
                    //  then multiply the base with this number.
                    //  Mathematical expression: x = base * (1 + a1 + a2 + ...)
                    DirectlyMultiply = 2
                    //  Multiply the base with the amount. Easy!
                    //  Mathematical expression: x = base * a1 * a2 * ...
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
                public Dictionary<string, List<AttributeModifier>> slot_effects = new Dictionary<string, List<AttributeModifier>>();
                //(Only for 'equipment' item) Available equipment slot IDs, and the attribute modifiers on each slot.
                public int max_stack = 1;
                //The max count of a stack of the item.
                public List<EffectEvent> use_events = new List<EffectEvent>();
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
                public List<EntryFormat.Log.ItemStack> items = new List<EntryFormat.Log.ItemStack>();
            }
            public class Level
            {
                public bool random = false;
                //  If true, the enemies will appear randomly, instead of be in set order.
                public bool looping = false;
                //  If true, the level will loop again when all enemies have been defeated.
                //  The level won't be end unless the player exit manually.
                //  Also, the level won't be able to show in finished_levels.
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

                public void Patch(Effect buffer)  //Merge a patch from buffer, for overlaying effects
                {
                    time = (buffer.time > this.time) ? buffer.time : this.time;
                    level = (buffer.level > this.level) ? buffer.level : this.level; //Select the bigger number
                }
                public Reg.Effect GetRegInfo()
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
            public class SoldItem : ItemStack
            {
                public int price = 0;
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
                    List<Reg.AttributeModifier> modifiers = new List<Reg.AttributeModifier>();
                    //Get all modifiers from effects and equipment
                    foreach (var effect in this.effects)
                    {
                        modifiers.AddRange(effect.GetRegInfo().modifiers);
                    }
                    foreach (var item in this.equipment)
                    {
                        modifiers.AddRange(item.Value.GetRegInfo().slot_effects[item.Key]);
                    }
                    foreach (var elem in modifiers)
                    {
                        if (elem.name == attribute_name)
                        {
                            switch (elem.operation)
                            {
                                case Reg.AttributeModifier.Operations.DirectlyAdd:
                                    tmp_op0 += elem.amount;
                                    break;
                                case Reg.AttributeModifier.Operations.OverlayingMultiply:
                                    tmp_op1 += elem.amount;
                                    break;
                                case Reg.AttributeModifier.Operations.DirectlyMultiply:
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
                public List<ItemStack> inventory = new List<ItemStack>();
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
                public class UnknownLocationException : ApplicationException
                {
                    public UnknownLocationException() : base()
                    {
                    }
                    public UnknownLocationException(string message) : base(message)
                    {
                    }
                }
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
                public Room.BasicRoom CurrentRoom
                {
                    get
                    {
                        if (!map.ContainsKey(player.location))
                        {
                            throw new UnknownLocationException("Unknown location.");
                        }
                        if (player.room_index < 0)
                        {
                            throw new UnknownLocationException("Unknown location.");
                        }
                        return map[player.location][player.room_index];
                    }
                    set
                    {
                        if (!map.ContainsKey(player.location))
                        {
                            throw new UnknownLocationException("Unknown location.");
                        }
                        if (player.room_index < 0)
                        {
                            throw new UnknownLocationException("Unknown location.");
                        }
                        map[player.location][player.room_index] = value;
                    }
                }
                public List<ItemStack> CurrentGround
                {
                    get
                    {
                        return CurrentRoom.droppedItems;
                    }
                    set
                    {
                        map[player.location][player.room_index].droppedItems = value;
                    }
                }
                public List<Entity> CurrentEntityList
                {
                    get
                    {
                        if (CurrentRoom is Room.BattleRoom)
                        {
                            return ((Room.BattleRoom)CurrentRoom).enemies;
                        }
                        else
                        {
                            return new List<Entity>();
                        }
                    }
                    set
                    {
                        if (CurrentRoom is Room.BattleRoom)
                        {
                            ((Room.BattleRoom)CurrentRoom).enemies = value;
                        }
                    }
                }
            }
        }
        public class DatapackRegistry
        {
            public int fileFormat;
            public string descriptionTransKey;
            public string creator;
            public string packVersion;
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
        public static void StackItems(ref List<EntryFormat.Log.ItemStack> itemList, EntryFormat.Log.ItemStack stack,
            int maxCapacity, out EntryFormat.Log.ItemStack itemRemaining)
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
                EntryFormat.Log.ItemStack stackTmp = new EntryFormat.Log.ItemStack
                {
                    id = stack.id,
                    count = (stack.count > maxStack) ? maxStack : stack.count
                };
                stack.count -= stackTmp.count;
                itemList.Add(stackTmp);
            }
            itemRemaining = stack;
        }
        public static void StackItems(ref List<EntryFormat.Log.ItemStack> itemList, EntryFormat.Log.ItemStack stack)
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
                EntryFormat.Log.ItemStack stackTmp = new EntryFormat.Log.ItemStack();
                stackTmp.id = stack.id;
                stackTmp.count = (stack.count > maxStack) ? maxStack : stack.count;
                stack.count -= stackTmp.count;
                itemList.Add(stackTmp);
            }
        }
        public static void UpdateItemList(ref List<EntryFormat.Log.ItemStack> itemList)
        {
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].count == 0)
                {
                    itemList.RemoveAt(i);
                    i--;
                    continue;
                }
            }
        }
    }
    public class GlobalData
    {
        public static string Version { get; } = "v.devbuild_20210123";
        public static int[] SupportedPackFormat { get; } = new int[] { 1 };
        public static string[] EntryCategories { get; } = new string[4] { "item", "effect", "enemy", "level" };

        public static bool debugModeOn = false;
        public static List<string> disabledPacks = new List<string>();
        public static bool safeModeOn = false;
        public static SquidCoreStates squidCoreMain = new SquidCoreStates();
        public static SquidCoreStates debugCommandStates = new SquidCoreStates();
        public static Config config = new Config();
        public static Dictionary<string, EntryFormat.DatapackRegistry> datapackInfo = new Dictionary<string, EntryFormat.DatapackRegistry>();
        public class DataFormat
        {
            public Dictionary<string, EntryFormat.Reg.Item> items = new Dictionary<string, EntryFormat.Reg.Item>();
            public Dictionary<string, EntryFormat.Reg.Effect> effects = new Dictionary<string, EntryFormat.Reg.Effect>();
            public Dictionary<string, EntryFormat.Reg.Enemy> enemies = new Dictionary<string, EntryFormat.Reg.Enemy>();
            public Dictionary<string, EntryFormat.Reg.Level> levels = new Dictionary<string, EntryFormat.Reg.Level>();
        }
        public static DataFormat regData = new DataFormat();
        public static Dictionary<string, Dictionary<string, string>> translates = new Dictionary<string, Dictionary<string, string>>();
        public static EntryFormat.Log.SaveData save = new EntryFormat.Log.SaveData();
        public static string memorySavesPath = "";

    }
}
