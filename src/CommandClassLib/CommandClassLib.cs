using System;
using System.Collections.Generic;
using CmdungeonsLib;
using SquidCsharp;
using Newtonsoft.Json;
using System.Linq;
using System.IO;

namespace CommandClassLib
{
    public class JsonInterface
    {
        public struct PackRegistry
        {

        }
        public struct Config
        {

        }
    }
    public class CommandClassLib
    {
        public static void RegistCommand()
        {
            GlobalData.squidCoreMain.RegCommand
                ("test", 2, 2, new List<string> { "", "wrd" }, Cmd_test);
            GlobalData.squidCoreMain.RegCommand
                ("exit", 1, 1, Cmd_exit);
            GlobalData.squidCoreMain.RegCommand
                ("packinfo", 1, 2, Cmd_packinfo);
            GlobalData.squidCoreMain.RegCommand
                ("commands", 1, 1, Cmd_commands);
            GlobalData.squidCoreMain.RegCommand
                ("reload", 1, 3, Cmd_reload);
            GlobalData.squidCoreMain.RegCommand
                ("debug", 1, 65536, Cmd_debug);
            GlobalData.squidCoreMain.RegCommand
                ("load", 1, 2, Cmd_load);
            GlobalData.squidCoreMain.RegCommand
                ("attack", 1, 2, Cmd_attack);
            GlobalData.squidCoreMain.Link
                ("atk", "attack");
            GlobalData.squidCoreMain.RegCommand
                ("attribute", 2, 3, Cmd_attribute);
            GlobalData.squidCoreMain.Link
                ("atb", "attribute");
            GlobalData.squidCoreMain.RegCommand
                ("ground", 1, 1, Cmd_ground);
            GlobalData.squidCoreMain.Link
                ("grd", "ground");
            GlobalData.squidCoreMain.RegCommand
                ("inventory", 1, 1, Cmd_inventory);
            GlobalData.squidCoreMain.Link
                ("inv", "inventory");
            GlobalData.squidCoreMain.RegCommand
                ("goto", 2, 2, Cmd_goto);
            GlobalData.squidCoreMain.RegCommand
                ("name", 2, 2, Cmd_name);
            GlobalData.squidCoreMain.RegCommand
                ("info", 1, 2, Cmd_info);
            GlobalData.squidCoreMain.RegCommand
                ("effect", 1, 2, Cmd_effect);
            GlobalData.squidCoreMain.RegCommand
                ("home", 1, 1, Cmd_home);
            GlobalData.squidCoreMain.RegCommand
                ("pass", 1, 1, Cmd_pass);
            GlobalData.squidCoreMain.RegCommand
                ("pick", 2, 2, Cmd_pick);

            GlobalData.debugStates.RegCommand
                ("GetTranslateString", 2, 65536, Debug_GetTranslateString);
            GlobalData.debugStates.RegCommand
                ("Dice", 2, 2, Debug_Dice);
            GlobalData.debugStates.RegCommand
                ("Effect", 4, 4, Debug_Effect);
        }
        public static void Cmd_commands(string[] args)
        {
            foreach (var elem in GlobalData.squidCoreMain.commandRegistry)
            {
                Console.WriteLine("{0}  [{1}-{2}]", elem.Key, elem.Value.argcMin, elem.Value.argcMax);
            }
        }
        public static void Cmd_reload(string[] args)
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

            GlobalData.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));



            if (!quiet)
            {
                Console.WriteLine("Uninstalling data...");
            }
            GlobalData.datapackInfo.Clear();
            GlobalData.regData = new GlobalData.DataFormat();
            GlobalData.squidCoreMain.commandRegistry.Clear();
            GlobalData.translates.Clear();
            GlobalData.debugStates.commandRegistry.Clear();

            if (!quiet)
            {
                Console.WriteLine("Start Loading...");
            }
            if (GlobalData.config.enabled_packs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Warning] No datapacks enabled! Please check your settings at \"config.json\" file.");
                Console.ResetColor();
            }
            List<string> packList = GlobalData.config.enabled_packs;
            packList.Reverse();
        }
        public static void Cmd_load(string[] args)
        {

        }
        public static void Cmd_attack(string[] args)
        {

        }
        public static void Cmd_test(string[] args) { }
        public static void Cmd_exit(string[] args) { }
        public static void Cmd_packinfo(string[] args) { }
        public static void Cmd_attribute(string[] args) { }
        public static void Cmd_ground(string[] args) { }
        public static void Cmd_inventory(string[] args) { }
        public static void Cmd_goto(string[] args) { }
        public static void Cmd_name(string[] args) { }
        public static void Cmd_info(string[] args) { }
        public static void Cmd_effect(string[] args) { }
        public static void Cmd_home(string[] args) { }
        public static void Cmd_pass(string[] args) { }
        public static void Cmd_pick(string[] args) { }

        public static void Cmd_debug(string[] args)
        {
            if (GlobalData.config.debug)
            {
                if (args.Length == 1)
                {
                    //Take over the input stream
                    for (; ; )
                    {
                        Console.Write("debug >> ");
                        string strInput = Console.ReadLine();
                        if (strInput == "")
                        {
                            break;
                        }
                        else
                        {
                            GlobalData.debugStates.Run(strInput);
                        }
                    }
                }
                else
                {
                    string[] N_args = new string[args.Length - 1];
                    args.CopyTo(N_args, 1);
                    GlobalData.debugStates.Run(N_args);
                }
            }
            else
            {
                Console.WriteLine(Tools.GetTranslateString("command.debug.unavailable"));
            }
        }

        public static void Debug_GetTranslateString(string[] args) { }
        public static void Debug_Dice(string[] args) { }
        public static void Debug_Effect(string[] args) { }
    }
}
