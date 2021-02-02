using System;
using System.Collections.Generic;
using SquidCsharp;
using CmdungeonsLib;
using System.IO;
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
            GlobalData.squidCoreMain.RegCommand
                ("attribute", 2, 2, Cmd_attribute);
            GlobalData.squidCoreMain.RegCommand
                ("ground", 1, 1, Cmd_ground);


            GlobalData.debugStates.RegCommand
                ("GetTranslateString", 2, 65536, Debug_GetTranslateString);
            GlobalData.debugStates.RegCommand
                ("Dice", 2, 2, Debug_Dice);
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
                    Console.WriteLine(Tools.GetTranslateString("command.load.successful"), GlobalData.save.name);
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
            List<EntryFormats.Log.Entity> targetList =
                GlobalData.save.map[GlobalData.save.location][GlobalData.save.room_index].entities;

            int targetIndex = 0;
            if (args.Count == 2)
            {
                targetIndex = Convert.ToInt32(args[1]);
            }
            double dmgDealted = 0, dmgBlocked = 0;
            bool selfDead = false, targetDead = false;
            int point = GlobalData.save.player_entity.Dice();

            void Attack(string selfName, string targetName,
                ref EntryFormats.Log.Entity self, ref EntryFormats.Log.Entity target)
            {
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
                    targetName, target.GetAttribute("generic:max_health"), target.health);

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

            EntryFormats.Log.Entity enemyTmp = targetList[targetIndex];
            Attack(Tools.GetTranslateString("generic.target.you"),
                Tools.GetTranslateString(
                    "enemy." + targetList[targetIndex].id + ".name"),
                ref GlobalData.save.player_entity,
                ref enemyTmp
                );
            targetList[targetIndex] = enemyTmp;

            targetList[targetIndex].NextTurn(out targetDead);
            if (targetDead)
            {
                Console.WriteLine("菜");
                foreach (var item in
                    GlobalData.regData.enemies[GlobalData.save.map[GlobalData.save.location][GlobalData.save.room_index]
                    .entities[targetIndex].id
                    ].rewards.items)
                {
                    EntryFormats.Log.ItemStack itemTmp = new EntryFormats.Log.ItemStack();
                    itemTmp.id = item.id;
                    itemTmp.count = item.count.Roll();
                    GlobalData.save.map[GlobalData.save.location][GlobalData.save.room_index].dropped_items.Add(itemTmp);
                }

                targetList.Remove(targetList[targetIndex]);
            }
            GlobalData.save.map[GlobalData.save.location][GlobalData.save.room_index].entities = targetList;
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
            Console.WriteLine(GlobalData.save.player_entity.GetAttribute(args[1]));
        }
        public static void Cmd_ground(List<string> args)
        {
            foreach (var item in GlobalData.save.map[GlobalData.save.location][GlobalData.save.room_index].dropped_items)
            {
                Console.Write(Tools.GetTranslateString("item." + item.id + ".name"));
                Console.WriteLine("  x" + item.count);
            }
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
    }
}
