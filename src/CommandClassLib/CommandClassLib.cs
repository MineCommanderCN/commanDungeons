﻿using System;
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
                ("reload", 1, 1, Cmd_reload);
        }

        public static void Cmd_commands(List<string> args)
        {
            foreach(KeyValuePair<string,SquidCsharpLib.CommandInfo> elem in SquidCoreStates.commandRegistry)
            {
                Console.WriteLine("{0}  [{1}-{2}]", elem.Key, elem.Value.argcMin, elem.Value.argcMax);
            }
        }
        public static void Cmd_reload(List<string> args)
        {

            StaticData.config = File.ReadAllText("config.json").FromJson<Config>();

            StaticData.packsData.Clear();
            SquidCoreStates.commandRegistry.Clear();

            if (StaticData.config.enabled_packs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Warning] No datapacks enabled! Please check your settings at \"config.json\" file.");
                Console.ResetColor();
            }

            int _counter = 0;
            foreach (string elem in StaticData.config.enabled_packs)
            {
                EntryFormats.Datapack tmp = new EntryFormats.Datapack();
                tmp.translate = new Dictionary<string, Dictionary<string, string>>();

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Loading pack \"{0}\"...", elem);
                Console.ResetColor();
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

                int _tmp_counter = 0;

                //Load languages
                foreach (string elem2 in tmp.registry.languages)
                {
                    try
                    {
                        tmp.translate[elem2] = File.ReadAllText(StaticData.config.packs_path + "/" + elem + "/translate/" + elem2 + ".json").FromJson<Dictionary<string, string>>();
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Error] Can not load language \"{0}\" from pack \"{1}\": {2}", elem2, elem, e.Message);
                        Console.ResetColor();
                        continue;
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[OK] Loaded language \"{0}\" from pack \"{1}\"", elem2, elem);
                    Console.ResetColor();

                    _tmp_counter++;
                }
                //Load languages end


                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK] Loaded pack \"{0}\" successfully.", elem);
                Console.ResetColor();
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
                if(StaticData.packsData.ContainsKey(args[1]))
                {
                    Console.WriteLine(Tools.GetTranslateString("commands.packinfo.pack_info"), args[1]);
                    Console.WriteLine(Tools.GetTranslateString("commands.packinfo.creator"), StaticData.packsData[args[1]].registry.meta_info.creator);
                    Console.WriteLine(Tools.GetTranslateString("commands.packinfo.namespace"), StaticData.packsData[args[1]].registry.meta_info.@namespace);
                    Console.Write(Tools.GetTranslateString("commands.packinfo.version") + "    ", StaticData.packsData[args[1]].registry.meta_info.file_format);
                    if(StaticData.packsData[args[1]].registry.meta_info.file_format == StaticData.SUPPORTED_PACKFORMAT)
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
                    string _tmp_descrip = Tools.GetTranslateString(StaticData.packsData[args[1]].registry.meta_info.@namespace + ".info.description");
                    if(_tmp_descrip == StaticData.packsData[args[1]].registry.meta_info.@namespace + ".info.description")   //When translated description dose not exit
                    {
                        Console.WriteLine(Tools.GetTranslateString("commands.packinfo.description"), StaticData.packsData[args[1]].registry.meta_info.description);
                        //Get the description in the meta info
                    }
                    else
                    {
                        Console.WriteLine(Tools.GetTranslateString("commands.packinfo.description"), _tmp_descrip);
                    }
                }
            }
        }
    }
}