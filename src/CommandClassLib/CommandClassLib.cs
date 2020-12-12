using System;
using System.Collections.Generic;
using SquidCsharp;
using CmdungeonsLib;

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
                    Console.WriteLine(Tools.GetTranslateString("commands.packinfo.description"), StaticData.packsData[args[1]].registry.meta_info.description);
                }
            }
        }
    }
}
