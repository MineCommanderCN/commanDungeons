using System;
using System.Collections.Generic;
using System.IO;
using CmdungeonsLib;
using SquidCsharp;
using TinyJson;

namespace CommandClassLib
{
    public class CommandClassLib
    {
        public static void RegistCommand()
        {
            GlobalData.squidCoreMain.RegCommand
                ("test", 2, 2, new List<string> { "", "wrd" }, Cmd_test);
            GlobalData.squidCoreMain.RegCommand
                ("exit", 1, 1, Cmd_exit);
            GlobalData.squidCoreMain.RegCommand
                ("packinfo", 1, 2, Cmd_packinfo);
            GlobalData.squidCoreMain.RegCommand
                ("commands", 1, 1, Cmd_commands);
            GlobalData.squidCoreMain.RegCommand
                ("reload", 1, 3, Cmd_reload);
            GlobalData.squidCoreMain.RegCommand
                ("debug", 1, 65536, Cmd_debug);
            GlobalData.squidCoreMain.RegCommand
                ("load", 1, 2, Cmd_load);
            GlobalData.squidCoreMain.RegCommand
                ("attack", 1, 2, Cmd_attack);
            GlobalData.squidCoreMain.Link
                ("atk", "attack");
            GlobalData.squidCoreMain.RegCommand
                ("attribute", 2, 3, Cmd_attribute);
            GlobalData.squidCoreMain.Link
                ("atb", "attribute");
            GlobalData.squidCoreMain.RegCommand
                ("ground", 1, 1, Cmd_ground);
            GlobalData.squidCoreMain.Link
                ("grd", "ground");
            GlobalData.squidCoreMain.RegCommand
                ("inventory", 1, 1, Cmd_inventory);
            GlobalData.squidCoreMain.Link
                ("inv", "inventory");
            GlobalData.squidCoreMain.RegCommand
                ("goto", 2, 2, Cmd_goto);
            GlobalData.squidCoreMain.RegCommand
                ("name", 2, 2, Cmd_name);
            GlobalData.squidCoreMain.RegCommand
                ("info", 1, 2, Cmd_info);
            GlobalData.squidCoreMain.RegCommand
                ("effect", 1, 2, Cmd_effect);
            GlobalData.squidCoreMain.RegCommand
                ("home", 1, 1, Cmd_home);
            GlobalData.squidCoreMain.RegCommand
                ("pass", 1, 1, Cmd_pass);
            GlobalData.squidCoreMain.RegCommand
                ("pick", 2, 2, Cmd_pick);

            GlobalData.debugStates.RegCommand
                ("GetTranslateString", 2, 65536, Debug_GetTranslateString);
            GlobalData.debugStates.RegCommand
                ("Dice", 2, 2, Debug_Dice);
            GlobalData.debugStates.RegCommand
                ("Effect", 4, 4, Debug_Effect);
        }
        static void TurnRound()
        {
            int point;
            bool playerDead = false, targetDead = false;
            void Attack(string selfName, string targetName,
                ref EntryFormats.Log.Entity self, ref EntryFormats.Log.Entity target)
            {
                double dmgDealted, dmgBlocked;
                Console.WriteLine(Tools.GetTranslateString("command.attack.act"), selfName, targetName);

                self.Attack(target, point, out dmgDealted, out dmgBlocked);

                Console.Write(Tools.GetTranslateString("command.attack.info"), point);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(Math.Round(dmgDealted, 1));
                Console.ResetColor();

                Console.Write(" - ");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(Math.Round(dmgBlocked, 1));
                Console.ResetColor();
                Console.WriteLine();

                target.health = (target.health > 0) ? target.health : 0;
                Console.Write(Tools.GetTranslateString("command.attack.result"),
                     targetName, target.health, target.GetAttribute("generic:max_health"));

                if (dmgDealted - dmgBlocked > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  (-{0:F1})", dmgDealted - dmgBlocked);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine();
                }
            }
            void PlayerDead()
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(Tools.GetTranslateString("generic.message.player_dead"),
                    Tools.GetTranslateString("level." + GlobalData.save.player.location + ".name"),
                    GlobalData.save.player.room_index);
                Console.ResetColor();
                foreach (var item in GlobalData.save.player.inventory)
                {
                    Tools.StackItems(ref GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index]
                        .dropped_items, item);
                }
                GlobalData.save.player.inventory.Clear();
                foreach (var equipment in GlobalData.save.player.equipment.Values)
                {
                    Tools.StackItems(ref GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index]
                        .dropped_items, equipment);
                }
                GlobalData.save.player.equipment.Clear();
                GlobalData.save.player.health =
                    GlobalData.save.player.GetAttribute("generic:max_health") / 2;
                GlobalData.save.player.location = "";
                GlobalData.save.player.room_index = -1;
            }
            var targetList = GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].entities;
            for (int i = 0; i < targetList.Count; i++)
            {
                var enemyTmp = targetList[i];
                enemyTmp.NextTurn(out targetDead);
                if (targetDead)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(Tools.GetTranslateString("command.attack.defeated"),
                        Tools.GetTranslateString("enemy." + targetList[i].id + ".name"));
                    Console.ResetColor();
                    var rewards = GlobalData.regData.enemies[targetList[i].id].rewards;
                    foreach (var item in rewards.items)
                    {
                        EntryFormats.Log.ItemStack itemTmp = new EntryFormats.Log.ItemStack();
                        itemTmp.id = item.id;
                        itemTmp.count = item.count.Roll();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(Tools.GetTranslateString("generic.message.looted_item"),
                            Tools.GetTranslateString("item." + itemTmp.id + ".name"), itemTmp.count);
                        Console.ResetColor();
                        Tools.StackItems(ref GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].dropped_items, itemTmp);
                    }
                    GlobalData.save.xp += rewards.xp;
                    GlobalData.save.gold += rewards.gold;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(Tools.GetTranslateString("player.message.get_xp"), rewards.xp);
                    Console.WriteLine(Tools.GetTranslateString("player.message.get_gold"), rewards.gold);
                    Console.ResetColor();

                    targetList.Remove(targetList[i]);
                    i--;
                    continue;
                }
                point = enemyTmp.Dice();
                Attack(Tools.GetTranslateString("enemy." + enemyTmp.id + ".name"),
                    Tools.GetTranslateString("generic.target.you"),
                    ref enemyTmp, ref GlobalData.save.player);
                GlobalData.save.player.Update(out playerDead);
                targetList[i] = enemyTmp;
                if (playerDead)
                {
                    GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].entities = targetList;
                    PlayerDead();
                    return;
                }
            }

            GlobalData.save.player.NextTurn(out playerDead);
            if (playerDead)
            {
                GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].entities = targetList;
                PlayerDead();
                return;
            }
            GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].entities = targetList;
        }
        public static void Cmd_commands(List<string> args)
        {
            foreach (KeyValuePair<string, SquidCsharpLib.CommandInfo> elem in GlobalData.squidCoreMain.commandRegistry)
            {
                Console.WriteLine("{0}  [{1}-{2}]", elem.Key, elem.Value.argcMin, elem.Value.argcMax);
            }
        }
        public static void Cmd_reload(List<string> args)
        {
            bool quiet = false, nolog = false;
            if (!nolog)
            {
                //DO log!
            }

            if (args.Contains("quiet"))
            {
                quiet = true;
            }
            if (args.Contains("nolog"))
            {
                nolog = true;
            }

            if (!quiet)
            {
                Console.WriteLine("Reloading config...");
            }
            GlobalData.config = File.ReadAllText("config.json").FromJson<Config>();

            if (!quiet)
            {
                Console.WriteLine("Uninstalling data...");
            }
            GlobalData.datapackInfo.Clear();
            GlobalData.regData = new GlobalData.DataFormat();
            GlobalData.squidCoreMain.commandRegistry.Clear();
            GlobalData.translates.Clear();
            GlobalData.debugStates.commandRegistry.Clear();

            if (!quiet)
            {
                Console.WriteLine("Start Loading...");
            }
            if (GlobalData.config.enabled_packs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Warning] No datapacks enabled! Please check your settings at \"config.json\" file.");
                Console.ResetColor();
            }
            List<string> packList = GlobalData.config.enabled_packs;
            packList.Reverse();
            foreach (string packName in packList)
            {
                int errorCount = 0, warningCount = 0, entryCount = 0;
                EntryFormats.DatapackRegistry tmpPackInfo = new EntryFormats.DatapackRegistry();
                GlobalData.DataFormat tmpRegData = new GlobalData.DataFormat();
                if (!quiet)
                {
                    Console.WriteLine("Loading pack \"{0}\"...", packName);
                }

                //Load registry.json
                try
                {
                    tmpPackInfo = File.ReadAllText(GlobalData.config.packs_path + "/" + packName + "/registry.json").FromJson<EntryFormats.DatapackRegistry>();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Error] Can not read pack \"{0}\": {1}", packName, e.Message);
                    Console.ResetColor();
                    continue;
                }

                //Load languages
                if (tmpPackInfo.languages.Count != 0)
                {
                    foreach (string lang in tmpPackInfo.languages)
                    {
                        entryCount++;
                        try
                        {
                            GlobalData.translates[lang] = File.ReadAllText(GlobalData.config.packs_path + "/" + packName + "/translate/" + lang + ".json").FromJson<Dictionary<string, string>>();
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[Error] Can not load language \"{0}\" from pack \"{1}\": {2}", lang, packName, e.Message);
                            Console.ResetColor();
                            continue;
                        }
                        if (!quiet)
                        {
                            Console.WriteLine("Loaded language \"{0}\" from pack \"{1}\"", lang, packName);
                        }
                    }
                }
                //Load languages end


                //Load data
                foreach (string category in GlobalData.ENTRY_CATEGORIES)
                {
                    if (tmpPackInfo.data.ContainsKey(category))
                    {
                        foreach (string entryName in tmpPackInfo.data[category])
                        {
                            entryCount++;
                            try
                            {
                                if (!Tools.IsValidName(entryName))
                                {
                                    throw new ApplicationException("Invalid entry name '" + entryName + "'.");
                                }
                                string[] namepair = entryName.Split(':', 2);

                                switch (category)
                                {
                                    case "item":
                                        tmpRegData.items[entryName] = File.ReadAllText(
                                            GlobalData.config.packs_path + "/" + packName + "/data/"
                                            + namepair[0] + "/item/" + namepair[1] + ".json")
                                            .FromJson<EntryFormats.Reg.Item>();
                                        break;

                                    case "effect":
                                        tmpRegData.effects[entryName] = File.ReadAllText(
                                            GlobalData.config.packs_path + "/" + packName + "/data/"
                                            + namepair[0] + "/effect/" + namepair[1] + ".json")
                                            .FromJson<EntryFormats.Reg.Effect>();
                                        break;

                                    case "enemy":
                                        tmpRegData.enemies[entryName] = File.ReadAllText(
                                            GlobalData.config.packs_path + "/" + packName + "/data/"
                                            + namepair[0] + "/enemy/" + namepair[1] + ".json")
                                            .FromJson<EntryFormats.Reg.Enemy>();
                                        break;

                                    case "level":
                                        tmpRegData.levels[entryName] = File.ReadAllText(
                                            GlobalData.config.packs_path + "/" + packName + "/data/"
                                            + namepair[0] + "/level/" + namepair[1] + ".json")
                                            .FromJson<EntryFormats.Reg.Level>();
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                errorCount++;
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("[Error] Can not load " + category + " entry \"{0}\" from pack \"{1}\": {2}", entryName, packName, e.Message);
                                Console.ResetColor();
                                continue;
                            }
                            if (!quiet)
                            {
                                Console.WriteLine("Loaded " + category + " entry \"{0}\" from pack \"{1}\"", entryName, packName);
                            }
                        }
                    }
                }
                //Load data end


                if (entryCount - warningCount - errorCount == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Warning] There is nothing to do with the pack \"{0}\", {1} entries detected.",
                        packName, entryCount);
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[OK] Loaded pack \"{0}\": {1} entries, {2} warnings, {3} errors.",
                        packName, entryCount, warningCount, errorCount);
                    Console.ResetColor();
                }
                GlobalData.datapackInfo.Add(packName, tmpPackInfo);

                GlobalData.regData.items = Tools.MergeDictionary<string, EntryFormats.Reg.Item>(
                    GlobalData.regData.items, tmpRegData.items);
                GlobalData.regData.effects = Tools.MergeDictionary<string, EntryFormats.Reg.Effect>(
                    GlobalData.regData.effects, tmpRegData.effects);
                GlobalData.regData.enemies = Tools.MergeDictionary<string, EntryFormats.Reg.Enemy>(
                    GlobalData.regData.enemies, tmpRegData.enemies);
                GlobalData.regData.levels = Tools.MergeDictionary<string, EntryFormats.Reg.Level>(
                    GlobalData.regData.levels, tmpRegData.levels);
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INFO] Enabled language: '{0}'", GlobalData.config.language);
            Console.ResetColor();
            RegistCommand();
            Console.WriteLine(Tools.GetTranslateString("generic.load_done"));
        }
        public static void Cmd_load(List<string> args)
        {
            void Load()
            {
                string saves_file;
                try
                {
                    saves_file = File.ReadAllText(GlobalData.memorySavesPath);
                    GlobalData.save = saves_file.FromJson<EntryFormats.Log.SaveData>();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(Tools.GetTranslateString("command.load.successful"), GlobalData.save.player.player_name);
                    Console.ResetColor();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("command.load.error.load_failed"), e.Message);
                    Console.ResetColor();
                    GlobalData.memorySavesPath = "";
                    return;
                }
            }

            if (args.Count == 1)
            {
                if (GlobalData.memorySavesPath.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(Tools.GetTranslateString("command.load.no_memory_path"));
                    Console.ResetColor();
                }
                else
                {
                    Load();
                }
            }
            else
            {
                GlobalData.memorySavesPath = args[1];
                Load();
            }
        }
        public static void Cmd_attack(List<string> args)
        {
            int point;
            void Attack(string selfName, string targetName,
                ref EntryFormats.Log.Entity self, ref EntryFormats.Log.Entity target)
            {
                Console.WriteLine(Tools.GetTranslateString("command.attack.act"), selfName, targetName);

                self.Attack(target, point, out double dmgDealted, out double dmgBlocked);

                Console.Write(Tools.GetTranslateString("command.attack.info"), point);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(Math.Round(dmgDealted, 1));
                Console.ResetColor();

                Console.Write(" - ");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(Math.Round(dmgBlocked, 1));
                Console.ResetColor();
                Console.WriteLine();

                target.health = (target.health > 0) ? target.health : 0;
                Console.Write(Tools.GetTranslateString("command.attack.result"),
                     targetName, target.health, target.GetAttribute("generic:max_health"));

                if (dmgDealted - dmgBlocked > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  (-{0:F1})", dmgDealted - dmgBlocked);
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine();
                }
            }

            if (GlobalData.save.player.room_index < 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.attack.no_target"));
                Console.ResetColor();
                return;
            }
            var targetList = GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].enemies;
            if (targetList.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.attack.no_target"));
                Console.ResetColor();
                return;
            }
            int targetIndex = (args.Count == 2) ? Convert.ToInt32(args[1]) : 0;
            try
            {
                var testTmp = targetList[targetIndex];
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Tools.GetTranslateString("command.attack.error.failed"));
                Console.ResetColor();
                return;
            }
            var enemyTmp = targetList[targetIndex];
            point = GlobalData.save.player.Dice();
            Attack(Tools.GetTranslateString("generic.target.you"), Tools.GetTranslateString("enemy." + enemyTmp.id + ".name"),
                    ref GlobalData.save.player.ToEntity, ref enemyTmp);
            targetList[targetIndex] = enemyTmp;
            GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].entities = targetList;
            TurnRound();
        }
        public static void Cmd_test(List<string> args)
        {
            Console.WriteLine(Tools.GetTranslateString("test.hello_world"));
        }
        public static void Cmd_exit(List<string> args)
        {
            Environment.Exit(0);
        }
        public static void Cmd_packinfo(List<string> args)
        {
            if (args.Count == 1)
            {
                Console.WriteLine(Tools.GetTranslateString("command.packinfo.pack_count"), GlobalData.datapackInfo.Count);
                foreach (KeyValuePair<string, EntryFormats.DatapackRegistry> elem in GlobalData.datapackInfo)
                {
                    Console.WriteLine(elem.Key);
                }
            }
            else
            {
                if (GlobalData.datapackInfo.ContainsKey(args[1]))
                {
                    EntryFormats.DatapackRegistry packInfoTemp = GlobalData.datapackInfo[args[1]];
                    Console.WriteLine(Tools.GetTranslateString("command.packinfo.pack_info"), args[1]);
                    Console.WriteLine(
                        Tools.GetTranslateString("command.packinfo.creator"),
                        packInfoTemp.meta_info.creator);
                    Console.Write(Tools.GetTranslateString("command.packinfo.version") + "    ",
                        packInfoTemp.meta_info.file_format);

                    if (packInfoTemp.meta_info.file_format == GlobalData.SUPPORTED_PACKFORMAT)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(Tools.GetTranslateString("command.packinfo.version.compatible") + "\n");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(Tools.GetTranslateString("command.packinfo.version.uncompatible") + "\n");
                        Console.ResetColor();
                    }
                    Console.WriteLine(Tools.GetTranslateString("command.packinfo.description"),
                        Tools.GetTranslateString(packInfoTemp.meta_info.description));

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("command.packinfo.error.unknown_pack"), args[1]);
                    Console.ResetColor();
                }
            }
        }
        public static void Cmd_attribute(List<string> args)
        {
            if (args.Count == 3)
            {
                try
                {
                    var testTmp = GlobalData.CurrentRoom.entities[Convert.ToInt32(args[2])];
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("command.attribute.error.failed"));
                    Console.ResetColor();
                    return;
                }
            }
            EntryFormats.Log.Entity entity = (args.Count == 2) ?
                GlobalData.save.player :
                GlobalData.CurrentRoom.entities[Convert.ToInt32(args[2])];
            Console.WriteLine(Tools.GetTranslateString("command.attribute.message"),
                Tools.GetTranslateString("attribute." + args[1] + ".name"), entity.GetAttribute(args[1]));
        }
        public static void Cmd_ground(List<string> args)
        {
            if (!GlobalData.save.map.ContainsKey(GlobalData.save.player.location))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.ground.nothing"));
                Console.ResetColor();
                return;
            }
            if (GlobalData.save.player.room_index < 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.ground.nothing"));
                Console.ResetColor();
                return;
            }
            foreach (var item in GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index].dropped_items)
            {
                Console.Write(Tools.GetTranslateString("item." + item.id + ".name"));
                Console.WriteLine("  x" + item.count);
            }
        }
        public static void Cmd_inventory(List<string> args)
        {
            Console.WriteLine(Tools.GetTranslateString("command.inventory.capacity"),
                GlobalData.save.player.inventory.Count,
                Convert.ToInt32(Math.Floor(GlobalData.save.player.GetAttribute("player:inventory_capacity"))));
            foreach (var item in GlobalData.save.player.inventory)
            {
                Console.WriteLine(Tools.GetTranslateString("item." + item.id + ".name") + "  x" + item.count);
            }
        }
        public static void Cmd_goto(List<string> args)
        {
            if (!GlobalData.regData.levels.ContainsKey(args[1]))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Tools.GetTranslateString("command.goto.error.unknown_level"));
                Console.ResetColor();
                return;
            }

            void InitLevel()
            {
                EntryFormats.Reg.Level levelRegData = GlobalData.regData.levels[args[1]];
                List<EntryFormats.Log.Room> tmpRoomList = new List<EntryFormats.Log.Room>();
                EntryFormats.Log.Room tmpRoom = new EntryFormats.Log.Room();
                foreach (var roomRegData in levelRegData.rooms)
                {
                    foreach (var enemyID in roomRegData.enemies)
                    {
                        EntryFormats.Log.Entity enemyEntity = new EntryFormats.Log.Entity();
                        enemyEntity.health = GlobalData.regData.enemies[enemyID].health;
                        enemyEntity.id = enemyID;
                        enemyEntity.level = GlobalData.regData.enemies[enemyID].level;
                        enemyEntity.attribute_bases = GlobalData.regData.enemies[enemyID].attributes;
                        foreach (var equipmentItem in GlobalData.regData.enemies[enemyID].equipment)
                        {
                            Random random = new Random();
                            if (random.NextDouble() > equipmentItem.Value.random_chance)
                            {
                                continue;
                            }
                            EntryFormats.Log.ItemStack itemTmp = new EntryFormats.Log.ItemStack();
                            itemTmp.id = equipmentItem.Value.id;
                            itemTmp.count = 1;
                            enemyEntity.equipment.Add(equipmentItem.Key, itemTmp);
                        }
                        tmpRoom.entities.Add(enemyEntity);
                    }
                    tmpRoom.dropped_items = roomRegData.items;
                    tmpRoomList.Add(tmpRoom);
                }
                GlobalData.save.map[args[1]] = tmpRoomList;
            }

            if (GlobalData.save.map.ContainsKey(args[1]))
            {
                GlobalData.save.player.location = args[1];
                GlobalData.save.player.room_index = 0;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(Tools.GetTranslateString("command.goto.message"),
                    Tools.GetTranslateString("level." + args[1] + ".name"));
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.goto.init"),
                    Tools.GetTranslateString("level." + args[1] + ".name"));
                Console.ResetColor();
                InitLevel();
                GlobalData.save.player.location = args[1];
                GlobalData.save.player.room_index = 0;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(Tools.GetTranslateString("command.goto.message"),
                    Tools.GetTranslateString("level." + args[1] + ".name"));
                Console.ResetColor();
            }
        }
        public static void Cmd_name(List<string> args)
        {
            GlobalData.save.name = args[1];
            Console.WriteLine(Tools.GetTranslateString("command.name.message"), args[1]);
        }
        public static void Cmd_info(List<string> args)
        {
            if (args.Count == 2)
            {
                try
                {
                    var testTmp = GlobalData.CurrentRoom.entities[Convert.ToInt32(args[1])];
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("command.attribute.error.failed"));
                    Console.ResetColor();
                    return;
                }
            }
            EntryFormats.Log.Entity entity = (args.Count == 1) ?
                GlobalData.save.player :
                GlobalData.CurrentRoom.entities[Convert.ToInt32(args[1])];
            Console.WriteLine(
                Tools.GetTranslateString("command.info.message"),
                (args.Count == 1) ? GlobalData.save.name : Tools.GetTranslateString("enemy." + entity.id + ".name"),
                entity.health,
                entity.GetAttribute("generic:max_health"),
                entity.level,
                entity.GetAttribute("generic:attack_power"),
                entity.GetAttribute("generic:armor"),
                entity.effects.Count);
        }
        public static void Cmd_effect(List<string> args)
        {
            if (args.Count == 2)
            {
                try
                {
                    var testTmp = GlobalData.CurrentRoom.entities[Convert.ToInt32(args[1])];
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("command.attribute.error.failed"));
                    Console.ResetColor();
                    return;
                }
            }
            EntryFormats.Log.Entity entity = (args.Count == 1) ?
                GlobalData.save.player :
                GlobalData.CurrentRoom.entities[Convert.ToInt32(args[1])];

            Console.WriteLine(Tools.GetTranslateString("command.effect.count"), entity.effects.Count);
            foreach (var effect in entity.effects)
            {
                if (effect.GetRegInfo().debuff)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.WriteLine(Tools.GetTranslateString("command.effect.message"),
                    Tools.GetTranslateString("effect." + effect.id + ".name"),
                    effect.level, effect.time);
                Console.ResetColor();
            }
        }
        public static void Cmd_home(List<string> args)
        {
            if (GlobalData.save.player.location == "")
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Tools.GetTranslateString("command.home.message"));
            Console.ResetColor();
            TurnRound();
            GlobalData.save.player.room_index = -1;
            GlobalData.save.player.location = "";
        }
        public static void Cmd_pass(List<string> args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Tools.GetTranslateString("command.pass.message"));
            Console.ResetColor();
            TurnRound();
        }
        public static void Cmd_pick(List<string> args)
        {
            //TODO: Max count
            if (!GlobalData.save.map.ContainsKey(GlobalData.save.player.location))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.pick.nothing_to_do"));
                Console.ResetColor();
                return;
            }
            if (GlobalData.save.player.room_index < 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.ground.nothing_to_do"));
                Console.ResetColor();
                return;
            }
            if (GlobalData.save.player.inventory.Count >=
                Convert.ToInt32(Math.Floor(GlobalData.save.player.GetAttribute("player:inventory_capacity"))))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Tools.GetTranslateString("command.ground.nothing_to_do"));
                Console.ResetColor();
                return;
            }
            var itemList = GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index]
                .dropped_items;

            for (int i = 0; i < itemList.Count; i++)
            {
                if (args[1] != itemList[i].id && args[i] != "all")
                {
                    continue;
                }

                int pickedCount = itemList[i].count;
                Tools.StackItems(ref GlobalData.save.player.inventory, itemList[i],
                    Convert.ToInt32(Math.Floor(GlobalData.save.player.GetAttribute("player:inventory_capacity"))),
                    out var remaining);
                pickedCount -= remaining.count;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Tools.GetTranslateString("generic.message.picked_item"),
                    Tools.GetTranslateString("item." + itemList[i].id + ".name"), pickedCount);
                Console.ResetColor();
                itemList[i] = remaining;
                if (remaining.count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.message.inventory_full"));
                    Console.ResetColor();
                    break;
                }
            }
            Tools.UpdateItemList(ref itemList);
            GlobalData.save.map[GlobalData.save.player.location][GlobalData.save.player.room_index]
               .dropped_items = itemList;
        }

        public static void Cmd_debug(List<string> args)
        {
            if (GlobalData.config.debug)
            {
                if (args.Count == 1)
                {
                    //Take over the input stream
                    for (; ; )
                    {
                        Console.Write("debug >> ");
                        string strInput = Console.ReadLine();
                        if (strInput == "")
                        {
                            break;
                        }
                        else
                        {
                            GlobalData.debugStates.Run(strInput);
                        }
                    }
                }
                else
                {
                    args.Remove("debug");
                    GlobalData.debugStates.Run(args);
                }
            }
            else
            {
                Console.WriteLine(Tools.GetTranslateString("command.debug.unavailable"));
            }
        }

        public static void Debug_GetTranslateString(List<string> args)
        {
            if (args.Count > 2)
            {
                string[] transArgs = new string[args.Count - 2];
                for (int i = 2; i < args.Count; i++)
                {
                    transArgs[i - 2] = args[i];
                }
                Console.WriteLine(Tools.GetTranslateString(args[1]), transArgs);
            }
            else
            {
                Console.WriteLine(Tools.GetTranslateString(args[1]));
            }
        }
        public static void Debug_Dice(List<string> args)
        {
            double luck = Convert.ToDouble(args[1]);
            Console.WriteLine(EntryFormats.Log.Entity.RollPointWithLuck(luck));
        }
        public static void Debug_Effect(List<string> args)
        {
            EntryFormats.Log.Effect effectTmp = new EntryFormats.Log.Effect();
            effectTmp.id = args[1];
            effectTmp.time = Convert.ToInt32(args[2]);
            effectTmp.level = Convert.ToInt32(args[3]);
            GlobalData.save.player.effects.Add(effectTmp);
        }
    }
}
