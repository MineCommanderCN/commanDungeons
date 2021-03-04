using System;
using CommandClassLib;
using CmdungeonsLib;
using System.IO;
using Jurassic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandClassLib
{
    class JsMethods
    {
        public static void InitScriptEngine()
        {
            GlobalData.scriptEngine.EnableExposedClrTypes = true;
            GlobalData.scriptEngine.SetGlobalValue("console", new Jurassic.Library.FirebugConsole(GlobalData.scriptEngine));
            GlobalData.scriptEngine.SetGlobalValue("game", GlobalData.save);
        }
    }
}
