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
            StaticData.squidCoreMain.RegCommand
                ("test", 2, 2, new List<string> { "", "wrd" }, Cmd_test);
            StaticData.squidCoreMain.RegCommand
                ("exit", 1, 1, Cmd_exit);
            StaticData.squidCoreMain.RegCommand
                ("packinfo", 1, 2, Cmd_packinfo);
            StaticData.squidCoreMain.RegCommand
                ("commands", 1, 1, Cmd_commands);
            StaticData.squidCoreMain.RegCommand
                ("reload", 1, 3, Cmd_reload);
            StaticData.squidCoreMain.RegCommand
                ("debug", 1, 65536, Cmd_debug);

            StaticData.debugStates.RegCommand
                ("GetTranslateString", 2, 65536, Debug_GetTranslateString);
        }
        public static void Cmd_commands(List<string> args)
        {
            foreach (KeyValuePair<string, SquidCsharpLib.CommandInfo> elem in StaticData.squidCoreMain.commandRegistry)
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
            StaticData.config = File.ReadAllText("config.json").FromJson<Config>();

            if (!quiet)
            {
                Console.WriteLine("Uninstalling data...");
            }
            StaticData.packsData.Clear();
            StaticData.squidCoreMain.commandRegistry.Clear();
            StaticData.debugStates.commandRegistry.Clear();

            if (!quiet)
            {
                Console.WriteLine("Start Loading...");
            }
            if (StaticData.config.enabled_packs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Warning] No datapacks enabled! Please check your settings at \"config.json\" file.");
                Console.ResetColor();
            }

            int _counter = 0;
            foreach (string elem in StaticData.config.enabled_packs)
            {
                int errorCount = 0, warningCount = 0, entryCount = 0;
                EntryFormats.Datapack tmp = new EntryFormats.Datapack();
                tmp.translate = new Dictionary<string, Dictionary<string, string>>();
                tmp.data = new EntryFormats.Datapack.DataFormat();
                if (!quiet)
                {
                    Console.WriteLine("Loading pack \"{0}\"...", elem);
                }

                //Load registry.json
                try
                {
                    tmp.registry = File.ReadAllText(StaticData.config.packs_path + "/" + elem + "/registry.json").FromJson<EntryFormats.Datapack.RegistryFormat>();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Error] Can not read pack \"{0}\": {1}", elem, e.Message);
                    Console.ResetColor();
                    continue;
                }

                //Load languages
                if (tmp.registry.languages.Count != 0)
                {
                    foreach (string elem2 in tmp.registry.languages)
                    {
                        entryCount++;
                        try
                        {
                            tmp.translate[elem2] = File.ReadAllText(StaticData.config.packs_path + "/" + elem + "/translate/" + elem2 + ".json").FromJson<Dictionary<string, string>>();
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[Error] Can not load language \"{0}\" from pack \"{1}\": {2}", elem2, elem, e.Message);
                            Console.ResetColor();
                            continue;
                        }
                        if (!quiet)
                        {
                            Console.WriteLine("Loaded language \"{0}\" from pack \"{1}\"", elem2, elem);
                        }
                    }
                }
                //Load languages end

                //Load data
                if (tmp.registry.data.ContainsKey("items"))
                {
                    foreach (string elem2 in tmp.registry.data["items"])
                    {
                        entryCount++;
                        try
                        {
                            if(!Tools.IsValidName(elem2))
                            {
                                throw new ApplicationException("Invalid entry name '" + elem2 + "'.");
                            }
                            string[] namepair = elem2.Split(':', 2);
                            tmp.data.items[elem2] = File.ReadAllText(StaticData.config.packs_path + "/" + elem + "/data/" + namepair[0] + "/items/" + namepair[1] + ".json").FromJson<EntryFormats.Reg.Item>();
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[Error] Can not load item entry \"{0}\" from pack \"{1}\": {2}", elem2, elem, e.Message);
                            Console.ResetColor();
                            continue;
                        }
                        if (!quiet)
                        {
                            Console.WriteLine("Loaded item entry \"{0}\" from pack \"{1}\"", elem2, elem);
                        }
                    }
                }

                if (tmp.registry.data.ContainsKey("effects"))
                {
                    foreach (string elem2 in tmp.registry.data["effects"])
                    {
                        entryCount++;
                        try
                        {
                            if (!Tools.IsValidName(elem2))
                            {
                                throw new ApplicationException("Invalid entry name '" + elem2 + "'.");
                            }
                            string[] namepair = elem2.Split(':', 2);
                            tmp.data.effects[elem2] = File.ReadAllText(StaticData.config.packs_path + "/" + elem + "/data/" + namepair[0] + "/effects/" + namepair[1] + ".json").FromJson<EntryFormats.Reg.Effect>();
                        }
                        catch (Exception e)
                        {
                            errorCount++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[Error] Can not load effect entry \"{0}\" from pack \"{1}\": {2}", elem2, elem, e.Message);
                            Console.ResetColor();
                            continue;
                        }
                        if (!quiet)
                        {
                            Console.WriteLine("Loaded effect entry \"{0}\" from pack \"{1}\"", elem2, elem);
                        }
                    }

                }
                //Load data end
                if (entryCount - warningCount - errorCount == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Warning] There is nothing to do with the pack \"{0}\", {1} entries detected.", elem, entryCount);
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[OK] Loaded pack \"{0}\": {1} entries, {2} warnings, {3} errors.", elem, entryCount, warningCount, errorCount);
                    Console.ResetColor();
                }
                StaticData.packsData.Add(elem, tmp);

                _counter++;
            }
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INFO] Enabled language: '{0}'", StaticData.config.lang);
            Console.ResetColor();
            RegistCommand();
            Console.WriteLine(Tools.GetTranslateString("generic.load_done"));
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
                Console.WriteLine(Tools.GetTranslateString("commands.packinfo.pack_count"), StaticData.packsData.Count);
                foreach (KeyValuePair<string, EntryFormats.Datapack> elem in StaticData.packsData)
                {
                    Console.WriteLine(elem.Key);
                }
            }
            else
            {
                if (StaticData.packsData.ContainsKey(args[1]))
                {
                    Console.WriteLine(Tools.GetTranslateString("commands.packinfo.pack_info"), args[1]);
                    Console.WriteLine(
                        Tools.GetTranslateString("commands.packinfo.creator"), 
                        StaticData.packsData[args[1]].registry.meta_info.creator);
                    Console.Write(Tools.GetTranslateString("commands.packinfo.version") + "    ",
                        StaticData.packsData[args[1]].registry.meta_info.file_format);

                    if (StaticData.packsData[args[1]].registry.meta_info.file_format == StaticData.SUPPORTED_PACKFORMAT)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(Tools.GetTranslateString("commands.packinfo.version.compatible") + "\n");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(Tools.GetTranslateString("commands.packinfo.version.uncompatible") + "\n");
                        Console.ResetColor();
                    }
                    Console.WriteLine(Tools.GetTranslateString("commands.packinfo.description"),
                        Tools.GetTranslateString(StaticData.packsData[args[1]].registry.meta_info.description));

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Error] " + Tools.GetTranslateString("commands.packinfo.unknown_pack"), args[1]);
                    Console.ResetColor();
                }
            }
        }
        public static void Cmd_debug(List<string> args)
        {
            if (StaticData.config.debug)
            {
                args.Remove("debug");
                StaticData.debugStates.Run(args);
            }
            else
            {
                Console.WriteLine(Tools.GetTranslateString("commands.debug.unavailable"));
            }
        }

        public static void Debug_GetTranslateString(List<string> args)
        {
            List<string> transArgs = args;
            if (transArgs.Count > 2)
            {
                transArgs.Remove(transArgs[0]);
                transArgs.Remove(transArgs[0]);
                if (transArgs.Count == 0)
                {
                    Console.WriteLine(Tools.GetTranslateString(args[1]));
                }
                else
                {
                    Console.WriteLine(Tools.GetTranslateString(args[1]), transArgs);
                }
            }
            else
            {
                Console.WriteLine(Tools.GetTranslateString(args[1]));
                return;
            }
        }
    }
}
