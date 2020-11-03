using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CmdungeonsLib;
using SquidCsharp;

namespace commanDungeons_rebuilt
{
    class Program
    {
        static void Main(string[] args)
        {
            SquidCoreStates squidCoreMain = new SquidCoreStates();
            Console.WriteLine("Hello World!");
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
