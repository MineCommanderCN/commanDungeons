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
                CommandClassLib.CommandClassLib.Cmd_reload(new List<string>()); //First load
            }
            catch(Exception e)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("[Fatal] {0}\nPress any key to exit...", e.Message);
                Console.ResetColor();
                Console.ReadKey();
                Environment.Exit(0);
            }

            Console.WriteLine("All done, initializing...");
            
            Console.WriteLine(Tools.GetTranslateString("generic.welcome"), StaticData.VERSION);
            for (; ; )
            {
                Console.Write(">> ");
                string strInput = Console.ReadLine();
                try
                {
                    StaticData.squidCoreMain.Run(strInput);
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
