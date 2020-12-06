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
                ("test", 2, 2, new List<string> { "", "wrd" }, Test);
            StaticData.squidCoreMain.RegCommand
                ("exit", 1, 1, Exit);
            StaticData.squidCoreMain.RegCommand
                ("packinfo", 1, 2, PackCheck);
        }
        public static void Test(List<string> args)
        {
            Console.WriteLine("Test!!!");
        }
        public static void Exit(List<string> args)
        {
            Environment.Exit(0);
        }
        public static void PackCheck(List<string> args)
        {
            if (args.Count == 1)
            {
                foreach (KeyValuePair<string, EntryFormats.Datapack> elem in StaticData.packsData)
                {
                    Console.WriteLine("");
                    //
                }
            }
            else
            {

            }
        }
    }
}
