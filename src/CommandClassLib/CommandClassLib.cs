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
        }
        public static void Test(List<string> args)
        {
            Console.WriteLine("Test!!!");
        }
        public static void Exit(List<string> args)
        {
            Environment.Exit(0);
        }
    }
}
