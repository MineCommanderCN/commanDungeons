using System;
using CmdungeonsLib;
using System.IO;
using Jurassic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommanDungeons
{
    class JsMethods
    {
        public static void InitScriptEngine()
        {
            GlobalData.Data.scriptEngine.EnableExposedClrTypes = true;
            GlobalData.Data.scriptEngine.SetGlobalValue("console", new Jurassic.Library.FirebugConsole(GlobalData.Data.scriptEngine));
            GlobalData.Data.scriptEngine.SetGlobalValue("game", GlobalData.Data.save);
        }
    }
}
