/*
*   SquidCore v0.1.5
*   Copyright (C) MineCommander 2020, use MIT License
*   Repo: https://github.com/MineCommanderCN/squidCore
*/

/*
If you want to use the macro to build squidCore as a .dll file, please make sure:
  1. You are using Visual Studio as the IDE;
  2. Your operating system is Windows;
  3. Using the "Dynamic link library" template to generate the project.
Then, remove the comment of the following code:
*/
      #define SQUIDCORE_EXPORT_AS_DLL true

#ifdef SQUIDCORE_EXPORT_AS_DLL
    #define USE_DLL _declspec(dllexport)
#else
    #define USE_DLL
#endif
#pragma warning(disable:4996)
#pragma once
#include<iostream>
#include<vector>
#include<map>
#include<ctime>
#include<fstream>
#include<string>
#include<sstream>

namespace sqc {
    typedef std::vector<std::string> argsAry;
    typedef int(*Fp)(const argsAry& args);
    const int EXIT_MAIN = 65536;
    const int UNKNOWN_COMMAND = 65537;
    const int ARGUMENT_ERROR = 65538;
    const int MAXN = 2147483647;
    struct tCmdreg {
        Fp func;
        int argcMin;
        int argcMax;
    };
    std::map<std::string, tCmdreg> cmd_register;    //first -> root command, second -> command info
    template <class Ta, class Tb>
    Tb USE_DLL atob(const Ta& t) {    //strstream-based type conversion (such as std::string->float)
        std::stringstream temp;
        temp << t;
        Tb i;
        temp >> i;
        return i;
    }
    double USE_DLL str2num(std::string str) {    //A shortcut of atob<std::string, double>(), which is convenient for most use
        return atob<std::string, double>(str);
    }
    std::string USE_DLL num2str(double num) {    //A shortcut of atob<double, std::string>(), which is convenient for most use
        return atob<double, std::string>(num);
    }
    argsAry USE_DLL compile(std::string str) {
        str += "\n";
        argsAry tmp;
        std::string strtmp;
        int state = 0;  /*  state==0 scanning blank chars (spqce)
                            state==1 scanning strings between two spaces
                            state==2 scanning strings between two quotes
                        */
        for (std::string::iterator ii = str.begin(); ii != str.end(); ii++) {
            if (*ii == '\n') {
                tmp.push_back(strtmp);
                return tmp;
            }

            switch (state) {
            case 0:
                switch (*ii) {
                case ' ': break;
                case '"': state = 2; break;
                default: state = 1; strtmp.push_back(*ii);
                } break;
            case 1:
                if (*ii == ' ') {
                    tmp.push_back(strtmp);
                    strtmp.clear();
                    state = 0;
                }
                else {
                    strtmp.push_back(*ii);
                } break;
            case 2:
                if (*ii == '"') {
                    strtmp = "\"" + strtmp;
                    strtmp.push_back(*ii);
                    state = 1;
                }
                else {
                    strtmp.push_back(*ii);
                } break;
            default:
                throw "squidCore: Unexpected error at command parsing";
            }

        }
        return tmp;
    }
    class cmdContainer {
    private:
        argsAry args;
    public:
        cmdContainer USE_DLL operator=(const std::string& str) {
            this->args = compile(str);
            return this->args;
        }
        USE_DLL cmdContainer() {

        }
        USE_DLL cmdContainer(std::string str) {
            args = compile(str);
        }
        USE_DLL cmdContainer(argsAry argy) {
            args = argy;
        }
        int USE_DLL run() {
            if (cmd_register.count(args[0]) == 1) { //if command exist
                if (args.size() >= cmd_register[args[0]].argcMin && args.size() <= cmd_register[args[0]].argcMax) { //passed the arguments check
                    return cmd_register[args[0]].func(args);    //run the command
                }
                else {  //can not pass the check
                    return ARGUMENT_ERROR;
                }
            }
            else {  //command not exist
                return UNKNOWN_COMMAND;
            }
        }
    };

    void USE_DLL regcmd(std::string cmdstr, //root command
        Fp cmdfp,           //function pointer
        int argcMin,        //min argument count
        int argcMax) {      //max argument count

        tCmdreg temp{ cmdfp,argcMin,argcMax };
        cmd_register[cmdstr] = temp;
    }
}
