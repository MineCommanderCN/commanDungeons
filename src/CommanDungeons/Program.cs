using System;
using System.Linq;
using System.Collections.Generic;
using CmdungeonsLib;
using SquidCsharp;
using System.IO;

namespace CommanDungeons
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--debug") || args.Contains("-d"))
            {
                GlobalData.Data.debugModeOn = true;
            }
            if (args.Contains("--disable-datapack"))
            {
                for (int argIndex = args.ToList().IndexOf("--disable-datapack") + 1;
                    argIndex < args.Length; argIndex++)
                {
                    if (args[argIndex].StartsWith('-')) break;
                    GlobalData.Data.disabledPacks.Add(args[argIndex]);
                }
            }
            if (args.Contains("-D"))
            {
                for (int argIndex = args.ToList().IndexOf("-D") + 1;
                    argIndex < args.Length; argIndex++)
                {
                    if (args[argIndex].StartsWith('-')) break;
                    GlobalData.Data.disabledPacks.Add(args[argIndex]);
                }
            }
            if (args.Contains("--safemode") || args.Contains("-s"))
            {
                GlobalData.Data.safeModeOn = true;
            }


            Tools.OutputLine("Starting up...", Tools.MessageType.Info, GlobalData.Data.LogFileStream);
            try
            {
                CommandMethods.Cmd_reload(new string[1]); //First load
            }
            catch (Exception e)
            {
                Tools.OutputLine(string.Format("Game crashed!\n{0}\nPress any key to exit...", e.Message), Tools.MessageType.Fatal, GlobalData.Data.LogFileStream);
                Console.ReadKey();
                Environment.Exit(0);
            }

            Tools.OutputLine(string.Format(Tools.GetTranslateString("generic.welcome"), GlobalData.Data.Version));
            for (; ; )
            {
                Console.Write(">> ");
                string strInput = Console.ReadLine();
                if (strInput == "")
                {
                    continue;
                }
                try
                {
                    Tools.LogLine("Run the command '" + strInput + "'.", Tools.MessageType.Log, GlobalData.Data.LogFileStream);
                    GlobalData.Data.squidCoreMain.Run(strInput);
                }
                catch (UnknownCommandException)
                {
                    Tools.OutputLine(Tools.GetTranslateString("generic.error.unknown_command"), Tools.MessageType.Error, GlobalData.Data.LogFileStream);
                }
                catch (ArgumentCountOutOfRangeException e)
                {
                    Tools.OutputLine(string.Format(Tools.GetTranslateString("generic.error.args_count_out_of_range"), e.argCount, e.argcMin, e.argcMax), Tools.MessageType.Error, GlobalData.Data.LogFileStream);
                }
                catch (RegexCheckFailedException e)
                {
                    Tools.OutputLine(string.Format(Tools.GetTranslateString("generic.error.regex_check_failed"), e.index, e.arg, e.pattern), Tools.MessageType.Error, GlobalData.Data.LogFileStream);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.error.unexpected_error"), e.ToString());
                    Console.ResetColor();
                }
            }

        }
    }
}
