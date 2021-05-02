using CmdungeonsLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NbtLib;

namespace CommanDungeons
{
    public class CommandMethods
    {
        public static void RegistCommand()
        {
            GlobalData.Data.squidCoreMain.RegCommand
                ("test", 2, 2, new List<string> { "", "wrd" }, Cmd_test);
            GlobalData.Data.squidCoreMain.RegCommand
                ("exit", 1, 1, Cmd_exit);
            GlobalData.Data.squidCoreMain.RegCommand
                ("packinfo", 1, 2, Cmd_packinfo);
            GlobalData.Data.squidCoreMain.RegCommand
                ("commands", 1, 1, Cmd_commands);
            GlobalData.Data.squidCoreMain.RegCommand
                ("reload", 1, 2, Cmd_reload);
            GlobalData.Data.squidCoreMain.RegCommand
                ("debug", 1, 65536, Cmd_debug);
            GlobalData.Data.squidCoreMain.RegCommand
                ("load", 1, 2, Cmd_load);
            GlobalData.Data.squidCoreMain.RegCommand
                ("attack", 1, 2, Cmd_attack);
            GlobalData.Data.squidCoreMain.Link
                ("atk", "attack");
            GlobalData.Data.squidCoreMain.RegCommand
                ("attribute", 2, 3, Cmd_attribute);
            GlobalData.Data.squidCoreMain.Link
                ("atb", "attribute");
            GlobalData.Data.squidCoreMain.RegCommand
                ("ground", 1, 1, Cmd_ground);
            GlobalData.Data.squidCoreMain.Link
                ("grd", "ground");
            GlobalData.Data.squidCoreMain.RegCommand
                ("inventory", 1, 1, Cmd_inventory);
            GlobalData.Data.squidCoreMain.Link
                ("inv", "inventory");
            GlobalData.Data.squidCoreMain.RegCommand
                ("goto", 2, 2, Cmd_goto);
            GlobalData.Data.squidCoreMain.RegCommand
                ("name", 2, 2, Cmd_name);
            GlobalData.Data.squidCoreMain.RegCommand
                ("info", 1, 2, Cmd_info);
            GlobalData.Data.squidCoreMain.RegCommand
                ("effect", 1, 2, Cmd_effect);
            GlobalData.Data.squidCoreMain.RegCommand
                ("home", 1, 1, Cmd_home);
            GlobalData.Data.squidCoreMain.RegCommand
                ("pass", 1, 1, Cmd_pass);
            GlobalData.Data.squidCoreMain.RegCommand
                ("pick", 2, 2, Cmd_pick);

            GlobalData.Data.debugCommandStates.RegCommand
                ("GetTranslateString", 2, 65536, Debug_GetTranslateString);
            GlobalData.Data.debugCommandStates.RegCommand
                ("Dice", 2, 2, Debug_Dice);
            GlobalData.Data.debugCommandStates.RegCommand
                ("Effect", 4, 4, Debug_Effect);
            GlobalData.Data.debugCommandStates.RegCommand
                ("Runjs", 2, 2, Debug_Runjs);
            GlobalData.Data.debugCommandStates.RegCommand
                ("Give", 3, 4, Debug_Give);
        }
        public static void Cmd_commands(string[] args)
        {
            foreach (var elem in GlobalData.Data.squidCoreMain.commandRegistry)
            {
                Tools.OutputLine(string.Format("{0}  [{1}-{2}]", elem.Key, elem.Value.argcMin, elem.Value.argcMax));
            }
        }
        public static void Cmd_reload(string[] args)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();
            bool quiet = false;

            if (args.Contains("quiet"))
            {
                quiet = true;
            }

            if (!quiet)
            {
                Tools.OutputLine("Reloading config...", Tools.MessageType.Log, GlobalData.Data.LogFileStream);
            }

            JsonFormat.Config config = JsonConvert.DeserializeObject<JsonFormat.Config>(File.ReadAllText("config.json"));
            GlobalData.Data.config.language = config.language;
            GlobalData.Data.config.packsPath = config.packs_path;

            if (!quiet)
            {
                Tools.OutputLine("Uninstalling data...", Tools.MessageType.Log, GlobalData.Data.LogFileStream);
            }
            GlobalData.Data.datapackInfo.Clear();
            GlobalData.Data.regData = new GlobalData.RegistryData();
            GlobalData.Data.squidCoreMain.commandRegistry.Clear();
            GlobalData.Data.translates.Clear();
            GlobalData.Data.debugCommandStates.commandRegistry.Clear();

            if (!quiet)
            {
                Tools.OutputLine("Sacnning for datapacks...", Tools.MessageType.Log, GlobalData.Data.LogFileStream);
            }
            List<DirectoryInfo> enabledPackDirs = new();
            DirectoryInfo[] pendingPackDirs = new DirectoryInfo(GlobalData.Data.config.packsPath).GetDirectories();
            foreach (var item in pendingPackDirs)
            {
                if (File.Exists(item.FullName + "/registry.json"))
                {
                    enabledPackDirs.Add(item);
                }
            }

            if (!quiet)
            {
                Tools.OutputLine("Start Loading...", Tools.MessageType.Log, GlobalData.Data.LogFileStream);
            }
            if (enabledPackDirs.Count == 0)
            {
                Tools.OutputLine("No datapacks enabled! Please check your 'packs_path' folder.", Tools.MessageType.Warning, GlobalData.Data.LogFileStream);
            }
            enabledPackDirs.Reverse();

            foreach (var packDirInfo in enabledPackDirs)
            {
                //Load registry.json
                JsonFormat.PackRegistry packRegistryJson = new();
                DatapackRegistry datapackRegistry = new();
                try
                {
                    packRegistryJson = JsonConvert.DeserializeObject<JsonFormat.PackRegistry>
                                        (File.ReadAllText(new FileInfo(packDirInfo.FullName + "/registry.json").FullName));
                    datapackRegistry = new DatapackRegistry
                    {
                        fileFormat = packRegistryJson.file_format,
                        author = packRegistryJson.author,
                        description = packRegistryJson.description,
                        weblinks = packRegistryJson.weblinks
                    };
                }
                catch (Exception e)
                {
                    Tools.OutputLine(string.Format("Could not load registry info file from pack '{0}': {1}", packDirInfo.FullName, e.Message), Tools.MessageType.Error, GlobalData.Data.LogFileStream);
                }

                //Load languages
                if (Directory.Exists(packDirInfo.FullName + "/language"))
                {
                    DirectoryInfo languageDirInfo = new(packDirInfo.FullName + "/language");

                    FileInfo[] languageFileInfo = languageDirInfo.GetFiles("*.json");
                    foreach (var language in languageFileInfo)
                    {
                        try
                        {
                            string lang = Path.GetFileNameWithoutExtension(language.FullName);
                            Dictionary<string, string> langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>
                                                        (File.ReadAllText(language.FullName));
                            if (GlobalData.Data.translates.ContainsKey(lang))
                            {
                                GlobalData.Data.translates[lang] = Tools.MergeDictionary(GlobalData.Data.translates[lang], langDict);
                            }
                            else
                            {
                                GlobalData.Data.translates.Add(lang, langDict);
                            }
                            if (!quiet)
                            {
                                Tools.OutputLine(string.Format("Loaded language \"{0}\" from pack \"{1}\"", lang, packDirInfo.FullName), Tools.MessageType.Log, GlobalData.Data.LogFileStream);
                            }
                        }
                        catch (Exception e)
                        {
                            Tools.OutputLine(string.Format("Could not load language '{0}' from pack '{1}': {2}",
                                Path.GetFileNameWithoutExtension(language.FullName), packDirInfo.FullName, e.Message), Tools.MessageType.Error, GlobalData.Data.LogFileStream);
                        }
                    }
                }

                //Load data - items
            }




            RegistCommand();
            JsMethods.InitScriptEngine();

            stopwatch.Stop();
            Tools.OutputLine("Done! Used " + (double)stopwatch.ElapsedMilliseconds / 1000.0 + "s.", Tools.MessageType.Info, GlobalData.Data.LogFileStream);
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
            GlobalData.Data.LogFileStream.Close();
            GlobalData.Data.LogFileStream.Dispose();
            Environment.Exit(0);
        }
        public static void Cmd_packinfo(string[] args) { }
        public static void Cmd_attribute(string[] args) { }
        public static void Cmd_ground(string[] args) { }
        public static void Cmd_inventory(string[] args)
        {
            Tools.OutputLine(GlobalData.Data.save.player.inventory.ToString(), Tools.MessageType.Log, GlobalData.Data.LogFileStream);
        }
        public static void Cmd_goto(string[] args) { }
        public static void Cmd_name(string[] args) { }
        public static void Cmd_info(string[] args) { }
        public static void Cmd_effect(string[] args) { }
        public static void Cmd_home(string[] args) { }
        public static void Cmd_pass(string[] args) { }
        public static void Cmd_pick(string[] args) { }

        public static void Cmd_debug(string[] args)
        {
            if (GlobalData.Data.debugModeOn)
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
                            GlobalData.Data.debugCommandStates.Run(strInput);
                        }
                    }
                }
                else
                {
                    GlobalData.Data.debugCommandStates.Run(args.Skip(1).Take(args.Length).ToArray());
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
            GlobalData.Data.scriptEngine.ExecuteFile(args[1]);
        }
        public static void Debug_Give(string[] args)
        {
            ItemStack itemStack;
            if (args.Length == 3)
            {
                itemStack = new(args[1], Convert.ToInt32(args[2]));
            }
            else
            {
                object obj = JsonConvert.DeserializeObject(args[3]);
                itemStack = new(args[1], Convert.ToInt32(args[2]), new NbtSerializer().SerializeObjectToTag(obj));
            }
            GlobalData.Data.save.player.inventory.Add(itemStack);
            Tools.OutputLine(string.Format("Gave player {0} x{1}.", args[1], args[2]), Tools.MessageType.Debug, GlobalData.Data.LogFileStream);
        }
    }
}
