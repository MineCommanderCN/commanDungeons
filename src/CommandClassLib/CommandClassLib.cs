using System;
using System.Collections.Generic;
using CmdungeonsLib;
using SquidCsharp;
using Jurassic;
using Newtonsoft.Json;
using System.Linq;
using System.IO;

namespace CommandClassLib
{
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

            GlobalData.debugCommandStates.RegCommand
                ("GetTranslateString", 2, 65536, Debug_GetTranslateString);
            GlobalData.debugCommandStates.RegCommand
                ("Dice", 2, 2, Debug_Dice);
            GlobalData.debugCommandStates.RegCommand
                ("Effect", 4, 4, Debug_Effect);
            GlobalData.debugCommandStates.RegCommand
                ("Runjs", 2, 2, Debug_Runjs);
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

            JsonFormat.Config config = JsonConvert.DeserializeObject<JsonFormat.Config>(File.ReadAllText("config.json"));
            GlobalData.config.language = config.language;
            GlobalData.config.packsPath = config.packs_path;

            if (!quiet)
            {
                Console.WriteLine("Uninstalling data...");
            }
            GlobalData.datapackInfo.Clear();
            GlobalData.regData = new GlobalData.DataFormat();
            GlobalData.squidCoreMain.commandRegistry.Clear();
            GlobalData.translates.Clear();
            GlobalData.debugCommandStates.commandRegistry.Clear();

            if (!quiet)
            {
                Console.WriteLine("Sacnning for datapacks...");
            }
            List<DirectoryInfo> enabledPackDirs = new List<DirectoryInfo>();
            DirectoryInfo[] pendingPackDirs = new DirectoryInfo(GlobalData.config.packsPath).GetDirectories();
            foreach (var item in pendingPackDirs)
            {
                if (File.Exists(item.FullName + "/registry.json"))
                {
                    enabledPackDirs.Add(item);
                }
            }

            if (!quiet)
            {
                Console.WriteLine("Start Loading...");
            }
            if (enabledPackDirs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Warning] No datapacks enabled! Please check your 'packs_path' folder.");
                Console.ResetColor();
            }
            enabledPackDirs.Reverse();

            foreach (var packDirInfo in enabledPackDirs)
            {
                //Load registry.json
                JsonFormat.PackRegistry packRegistryJson = new JsonFormat.PackRegistry();
                EntryFormat.DatapackRegistry datapackRegistry = new EntryFormat.DatapackRegistry();
                try
                {
                    packRegistryJson = JsonConvert.DeserializeObject<JsonFormat.PackRegistry>
                                        (File.ReadAllText(new FileInfo(packDirInfo.FullName + "/registry.json").FullName));
                    datapackRegistry = new EntryFormat.DatapackRegistry
                    {
                        fileFormat = packRegistryJson.file_format,
                        creator = packRegistryJson.creator,
                        descriptionTransKey = packRegistryJson.description,
                        packVersion = packRegistryJson.pack_version
                    };
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Error] Could not load registry info file from pack '{0}': {1}", packDirInfo.FullName, e.Message);
                    Console.ResetColor();
                }
                if (GlobalData.datapackInfo.ContainsKey(packRegistryJson.pack_name))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Warning] Pack '{0}' is already exist, and another pack from folder '{1}' is trying to override it.",
                        packRegistryJson.pack_name, packDirInfo.FullName);
                    Console.ResetColor();
                }
                GlobalData.datapackInfo[packRegistryJson.pack_name] = datapackRegistry;

                //Load languages
                if (Directory.Exists(packDirInfo.FullName + "/language"))
                {
                    DirectoryInfo languageDirInfo = new DirectoryInfo(packDirInfo.FullName + "/language");

                    FileInfo[] languageFileInfo = languageDirInfo.GetFiles("*.json");
                    foreach (var language in languageFileInfo)
                    {
                        try
                        {
                            string lang = Path.GetFileNameWithoutExtension(language.FullName);
                            Dictionary<string, string> langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>
                                                        (File.ReadAllText(language.FullName));
                            if (GlobalData.translates.ContainsKey(lang))
                            {
                                GlobalData.translates[lang] = Tools.MergeDictionary(GlobalData.translates[lang], langDict);
                            }
                            else
                            {
                                GlobalData.translates.Add(lang, langDict);
                            }
                            if (!quiet)
                            {
                                Console.WriteLine("Loaded language \"{0}\" from pack \"{1}\"", lang, packDirInfo.FullName);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[Error] Could not load language '{0}' from pack '{1}': {2}",
                                Path.GetFileNameWithoutExtension(language.FullName), packDirInfo.FullName, e.Message);
                            Console.ResetColor();
                        }
                    }
                }
            }




            RegistCommand();
            JsMethods.InitScriptEngine();
        }
        public static void Cmd_load(string[] args)
        {

        }
        public static void Cmd_attack(string[] args)
        {

        }
        public static void Cmd_test(string[] args) { }
        public static void Cmd_exit(string[] args)
        {
            Environment.Exit(0);
        }
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
            if (GlobalData.debugModeOn)
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
                            GlobalData.debugCommandStates.Run(strInput);
                        }
                    }
                }
                else
                {
                    GlobalData.debugCommandStates.Run(args.Skip(1).Take(args.Length).ToArray());
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
        public static void Debug_Runjs(string[] args)
        {
            GlobalData.scriptEngine.ExecuteFile(args[1]);
        }
    }
}
