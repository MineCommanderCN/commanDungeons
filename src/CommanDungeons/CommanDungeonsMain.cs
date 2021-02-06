using System;
using System.Collections.Generic;
using CmdungeonsLib;
using SquidCsharp;

namespace CommanDungeonsMain
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INFO] Starting up...");
            Console.ResetColor();
            try
            {
                CommandClassLib.CommandClassLib.Cmd_reload(new List<string>()); //First load
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("[Fatal] Game crashed!\n{0}\nPress any key to exit...", e.Message);
                Console.ResetColor();
                Console.WriteLine();
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INFO] All done!");
            Console.ResetColor();

            Console.WriteLine(Tools.GetTranslateString("generic.welcome"), GlobalData.VERSION);
            for (; ; )
            {
                Console.Write(">> ");
                string strInput = Console.ReadLine();
                if (strInput == "")
                {
                    continue;
                }
                if (!GlobalData.squidCoreMain.commandRegistry.ContainsKey(
                    SquidCsharpLib.Convert(strInput)[0]))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.error.unknown_command"));
                    Console.ResetColor();
                    continue;
                }
                try
                {
                    GlobalData.squidCoreMain.Run(strInput);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.exception_caught"), e.Message);
                    Console.ResetColor();
                }
            }

        }
    }
}
