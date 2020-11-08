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
            CmdungeonsLib.CmdungeonsLib.squidCoreMain.RegCommand
                ("test", 2, 2, new List<string> { "", "wrd" }, Test);
            CmdungeonsLib.CmdungeonsLib.squidCoreMain.RegCommand
                ("exit", 1, 1, Exit);
        }
        public static int Test(List<string> args)
        {
            Console.WriteLine("Test!!!");
            return 0;
        }
        public static int Exit(List<string> args)
        {
            Environment.Exit(0);
            return 0;
        }
    }
}
