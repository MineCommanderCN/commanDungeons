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
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("[Fatal] Can not read \"config.json\" file: {0}",e.Message);
                Console.ResetColor();
                return;
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
                Datapack tmp = new Datapack();
                tmp.translate = new Dictionary<string, Dictionary<string, string>>();

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Loading pack \"{0}\"...", elem);
                Console.ResetColor();
                try
                {
                    tmp.registry = File.ReadAllText("packs/" + elem + "/registry.json").FromJson<Datapack.registryFormat>();
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
                        tmp.translate[elem2] = File.ReadAllText("packs/" + elem + "/translate/" + elem2 + ".json").FromJson<Dictionary<string, string>>();
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









            CommandClassLib.CommandClassLib.RegistCommand();

            Console.WriteLine("CommanDungeons Version dev.20201106\nSquidCsharp demo");
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
                    Console.WriteLine("Exception Caught! Error Message: {0}", e.Message);
                    Console.ResetColor();
                }
            }
            
        }
    }
}
