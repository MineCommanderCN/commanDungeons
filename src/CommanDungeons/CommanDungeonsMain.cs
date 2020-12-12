using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CmdungeonsLib;
using CommandClassLib;
using TinyJson;
using SquidCsharp;

namespace CommanDungeonsMain
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                StaticData.config = File.ReadAllText("config.json").FromJson<Config>();

            }
            catch(Exception e)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("[Fatal] Can not read \"config.json\" file: {0}\nPress any key to exit...",e.Message);
                Console.ResetColor();
                Console.ReadKey();
                Environment.Exit(0);
            }
            if (StaticData.config.enabled_packs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Warning] No datapacks enabled! Please check your settings at \"config.json\" file.");
                Console.ResetColor();
            }

            int _counter = 0;
            foreach(string elem in StaticData.config.enabled_packs)
            {
                EntryFormats.Datapack tmp = new EntryFormats.Datapack();
                tmp.translate = new Dictionary<string, Dictionary<string, string>>();

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Loading pack \"{0}\"...", elem);
                Console.ResetColor();
                try
                {
                    tmp.registry = File.ReadAllText(StaticData.config.packs_path + "/" + elem + "/registry.json").FromJson<EntryFormats.Datapack.RegistryFormat>();
                }
                catch(Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[Error] Can not read pack \"{0}\": {1}", elem,e.Message);
                    Console.ResetColor();
                    continue;
                }

                int _tmp_counter = 0;

                //load languages
                foreach(string elem2 in tmp.registry.languages)
                {
                    try
                    {
                        tmp.translate[elem2] = File.ReadAllText(StaticData.config.packs_path + "/" + elem + "/translate/" + elem2 + ".json").FromJson<Dictionary<string, string>>();
                    }
                    catch(Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Error] Can not load language \"{0}\" from pack \"{1}\": {2}", elem2, elem, e.Message);
                        Console.ResetColor();
                        continue;
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[OK] Loaded language \"{0}\" from pack \"{1}\"", elem2, elem);
                    Console.ResetColor();

                    _tmp_counter++;
                }
                //load languages end


                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK] Loaded pack \"{0}\" successfully.", elem);
                Console.ResetColor();
                StaticData.packsData.Add(elem, tmp);

                _counter++;
            }
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INFO] Enabled language: '{0}'",StaticData.config.lang);
            Console.ResetColor();






            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("All done, initializing...");
            Console.ResetColor();
            CommandClassLib.CommandClassLib.RegistCommand();

            Console.WriteLine(Tools.GetTranslateString("generic.welcome"), StaticData.VERSION);
            while (true)
            {
                Console.Write(">>");
                string strInput = Console.ReadLine();
                SquidCoreStates.CommandContainer commandContainer = new SquidCoreStates.CommandContainer(strInput);
                try
                {
                    commandContainer.Run();
                }
                catch(SquidCoreStates.CommandContainer.SquidCoreRunException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Tools.GetTranslateString("generic.exception_caught"), e.Message);
                    Console.ResetColor();
                }
            }
            
        }
    }
}
