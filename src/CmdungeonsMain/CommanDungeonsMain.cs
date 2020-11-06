using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CmdungeonsLib;
using CommandClassLib;
using Newtonsoft.Json;
using SquidCsharp;

namespace CommanDungeonsMain
{
    class Program
    {
        static void Main(string[] args)
        {
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
                    Console.WriteLine("Exception Caught! Error Message: {0}", e.Message);
                }
            }
            
        }
    }
}
