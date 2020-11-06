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
            CmdungeonsLib.CmdungeonsLib.squidCoreMain.RegCommand("test", 2, 2, new List<string> { "", "wrd" }, Test);
        }
        public static int Test(List<string> args)
        {
            Console.WriteLine("Test!!!");
            return 0;
        }
    }
}
