using System;
using System.Linq;
using System.Collections.Generic;
using CmdungeonsLib;
using SquidCsharp;

namespace CommanDungeonsMain
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--debug") || args.Contains("-d"))
            {
                GlobalData.debugModeOn = true;
            }
            if (args.Contains("--disable-datapack"))
            {
                for (int argIndex = args.ToList().IndexOf("--disable-datapack") + 1;
                    argIndex < args.Length; argIndex++)
                {
                    if (args[argIndex].StartsWith('-')) break;
                    GlobalData.disabledPacks.Add(args[argIndex]);
                }
            }
            if (args.Contains("-D"))
            {
                for (int argIndex = args.ToList().IndexOf("-D") + 1;
                    argIndex < args.Length; argIndex++)
                {
                    if (args[argIndex].StartsWith('-')) break;
                    GlobalData.disabledPacks.Add(args[argIndex]);
                }
            }
            if (args.Contains("--safemode") || args.Contains("-s"))
            {
                GlobalData.safeModeOn = true;
            }


            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INFO] Starting up...");
            Console.ResetColor();
            try
            {
                CommandClassLib.CommandClassLib.Cmd_reload(new string[1]); //First load
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

            Console.WriteLine(Tools.GetTranslateString("generic.welcome"), GlobalData.Version);
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
                catch (UnknownCommandException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.unknown_command"), e.command);
                    Console.ResetColor();
                }
                catch (ArgumentCountOutOfRangeException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.args_count_out_of_range"), e.argcMin, e.argcMax);
                    Console.ResetColor();
                }
                catch (RegexCheckFailedException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.regex_check_failed"), e.arg, e.index, e.pattern);
                    Console.ResetColor();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.unexpected_error"), e.ToString());
                    Console.ResetColor();
                }
            }

        }
    }
}
