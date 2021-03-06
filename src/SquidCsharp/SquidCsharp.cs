﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SquidCsharp
{
    public class UnknownCommandException : ApplicationException
    {
        public string command;
        public UnknownCommandException(string message, string command) : base(message)
        {
            this.command = command;
        }
    }
    public class RegistingException : ApplicationException
    {
        public int argcMin, argcMax;
        public RegistingException(string message, int argcMin, int argcMax) : base(message)
        {
            this.argcMin = argcMin;
            this.argcMax = argcMax;
        }
    }
    public class PatternCountOutOfRangeException : ApplicationException
    {
        public int argcMin, argcMax, patternCount;
        public PatternCountOutOfRangeException(string message, int argcMin, int argcMax, int patternCount) : base(message)
        {
            this.argcMin = argcMin;
            this.argcMax = argcMax;
            this.patternCount = patternCount;
        }
    }
    public class ArgumentCountOutOfRangeException : ApplicationException
    {

        public int argCount, argcMin, argcMax;
        public ArgumentCountOutOfRangeException(string message, int argCount, int argcMin, int argcMax) : base(message)
        {
            this.argCount = argCount;
            this.argcMin = argcMin;
            this.argcMax = argcMax;
        }
    }
    public class RegexCheckFailedException : ApplicationException
    {
        public string arg;
        public int index;
        public string pattern;
        public RegexCheckFailedException(string message, string arg, int index, string pattern) : base(message)
        {
            this.arg = arg;
            this.index = index;
            this.pattern = pattern;
        }
    }
    public class LinkingSourceNotFoundException : ApplicationException
    {
        public string source;
        public LinkingSourceNotFoundException(string message, string sourceCommand) : base(message)
        {
            source = sourceCommand;
        }
    }

    public delegate void FuncDele(string[] args);
    public static class SquidCsharpLib
    {
        public struct CommandInfo
        {
            public FuncDele commandMethod;
            public int argcMin, argcMax;
            public List<string> argPatterns;  //Regular expression check for arguments at corresponding positions
                                              //对于相应位置的参数的正则表达式检查
        }
        public static string[] Convert(string buf)
        {
            buf.Trim();
            buf += "\n";
            List<string> tmp = new List<string>();
            string strtmp = "";
            int state = 0;  /*  state==0 scanning blank chars (spqce)
                            state==1 scanning strings between two spaces
                            state==2 scanning strings between two quotes
                            */
            int _counter = 0;
            foreach (char elem in buf)
            {
                if (elem == '\n')
                {
                    if (strtmp.Length != 0) tmp.Add(strtmp);
                    break;
                }

                switch (state)
                {
                    case 0:
                        switch (elem)
                        {
                            case ' ':
                                break;
                            case '"':
                                state = 2;
                                break;
                            default:
                                state = 1;
                                strtmp += elem;
                                break;
                        }
                        break;

                    case 1:
                        if (elem == ' ')
                        {
                            if (strtmp.Length != 0) tmp.Add(strtmp);
                            strtmp = "";
                            state = 0;
                        }
                        else
                        {
                            strtmp += elem;
                        }
                        break;

                    case 2:
                        if (elem == '"')
                        {
                            if (buf[_counter + 1] != ' ' && buf[_counter + 1] != '\n')
                            {
                                strtmp = "\"" + strtmp;
                                strtmp += elem;
                                state = 1;
                            }
                            else
                            {
                                if (strtmp.Length != 0) tmp.Add(strtmp);
                                strtmp = "";
                                state = 0;
                            }
                        }
                        else
                        {
                            strtmp += elem;
                        }
                        break;

                    default:
                        break;
                }
                _counter++;

            }
            return tmp.ToArray();
        }
    }
    public class SquidCoreStates
    {
        public readonly Dictionary<string, SquidCsharpLib.CommandInfo> commandRegistry
            = new Dictionary<string, SquidCsharpLib.CommandInfo>();
        //Dictionary of command info
        //命令信息辞典



        public void RegCommand(
            string rootCommand,
            int argcMin,
            int argcMax,
            FuncDele commandMethod)
        //Registry a command
        //注册一个命令
        {
            SquidCsharpLib.CommandInfo tmp;
            tmp.argcMin = argcMin;
            tmp.argcMax = argcMax;
            tmp.commandMethod = commandMethod;
            tmp.argPatterns = new List<string>();
            for (int i = 0; i < argcMax; i++)
            {
                tmp.argPatterns.Add("");
            }

            if (argcMin > argcMax)
            {
                throw new RegistingException("Minimum count of arguments (" + argcMin + ") is bigger than maximum count (" + argcMax + ")",
                    argcMin, argcMax);
            }
            else if (argcMin <= 0 || argcMax <= 0)
            {
                throw new RegistingException("Count of arguments (" + argcMin + " and " + argcMax + ") must greater than 0",
                    argcMin, argcMax);
            }

            commandRegistry.Add(rootCommand, tmp);
        }

        public void RegCommand(
            string rootCommand,
            int argcMin,
            int argcMax,
            List<string> argPatterns,
            FuncDele commandMethod)
        //Registry a command
        //注册一个命令
        {
            SquidCsharpLib.CommandInfo tmp;
            tmp.argcMin = argcMin;
            tmp.argcMax = argcMax;
            tmp.commandMethod = commandMethod;
            tmp.argPatterns = argPatterns;
            if (argcMin > argcMax)
            {
                throw new RegistingException("Minimum count of arguments (" + argcMin + ") is bigger than maximum count (" + argcMax + ")",
                    argcMin, argcMax);
            }
            else if (argcMin <= 0 || argcMax <= 0)
            {
                throw new RegistingException("Count of arguments (" + argcMin + " and " + argcMax + ") must greater than 0",
                    argcMin, argcMax);
            }
            else if (argPatterns.Count < argcMin || argPatterns.Count > argcMax)
            {
                throw new PatternCountOutOfRangeException("Count of patterns (" + argPatterns.Count + ") is out of range [" + argcMin + "," + argcMax + "]",
                    argcMin, argcMax, argPatterns.Count);
            }

            commandRegistry.Add(rootCommand, tmp);
        }
        public void Link(string link, string command)
        {
            if (!commandRegistry.ContainsKey(command))
            {
                throw new LinkingSourceNotFoundException("Source command \"" + command + "\" not found", command);
            }
            commandRegistry.Add(link, commandRegistry[command]);
        }

        private void p_Run(string[] argList)
        {
            if (argList.Length == 0)
            {
                return;
            }
            if (commandRegistry.ContainsKey(argList[0]))
            {
                if (argList.Length < commandRegistry[argList[0]].argcMin || argList.Length > commandRegistry[argList[0]].argcMax)
                {
                    throw new ArgumentCountOutOfRangeException("Count of arguments was out of range [" + commandRegistry[argList[0]].argcMin
                            + "," + commandRegistry[argList[0]].argcMax + "]", argList.Length,
                            commandRegistry[argList[0]].argcMin, commandRegistry[argList[0]].argcMax);
                }

                int _counter = 0;
                foreach (string elem in argList)
                {
                    if (!Regex.IsMatch(elem, commandRegistry[argList[0]].argPatterns[_counter]))
                    {
                        throw new RegexCheckFailedException("Argument \"" + elem + "\"(at [" + _counter + "]) could not match the regular expression \""
                            + commandRegistry[argList[0]].argPatterns[_counter] + "\"",
                            elem, _counter, commandRegistry[argList[0]].argPatterns[_counter]);
                    }
                    _counter++;
                }
                commandRegistry[argList[0]].commandMethod(argList);
                //return;
            }
            else
            {
                throw new UnknownCommandException("Unknown Command", argList[0]);
            }
        }
        public void Run(string[] argList)
        {
            p_Run(argList);
        }
        public void Run(string command)
        {
            p_Run(SquidCsharpLib.Convert(command));
        }
    }
}
