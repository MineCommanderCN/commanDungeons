#pragma once
#include<baselib/baselib.hpp>
#pragma comment(lib, "baselib.lib")

/*
*   SquidCore v0.1.4
*   Copyright (C) MineCommander 2020, use MIT License
*   Repo: https://github.com/MineCommanderCN/squidCore
*/
#pragma once
#define _CRT_SECURE_NO_WARNINGS
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
    const int MAXN = 2147483647;
    struct tCmdreg {
        Fp func;
        int argcMin;
        int argcMax;
    };
    std::map<std::string, tCmdreg> cmd_register;    //first -> root command, second -> command info
    template <class Ta, class Tb>
    Tb atob(const Ta& t) {    //strstream-based type conversion (such as std::string->float)
        std::stringstream temp;
        temp << t;
        Tb i;
        temp >> i;
        return i;
    }
    double str2num(std::string str) {    //A shortcut of atob<std::string, double>(), which is convenient for most use
        return atob<std::string, double>(str);
    }
    std::string num2str(double num) {    //A shortcut of atob<double, std::string>(), which is convenient for most use
        return atob<double, std::string>(num);
    }
    argsAry compile(std::string str) {
        str += "\n";
        argsAry tmp;
        std::string strtmp;
        int state = 0;
        for (std::string::iterator ii = str.begin(); ii != str.end(); ii++) {
            if (*ii == '\n') {
                tmp.push_back(strtmp);
                return tmp;
            }
            if (state == 0) {
                if (*ii == ' ') {

                }
                else if (*ii == '"') {
                    state = 2;
                }
                else {
                    state = 1;
                    strtmp.push_back(*ii);
                }
            }
            else if (state == 1) {
                if (*ii == ' ') {
                    tmp.push_back(strtmp);
                    strtmp.clear();
                    state = 0;
                }
                else {
                    strtmp.push_back(*ii);
                }
            }
            else if (state == 2) {
                if (*ii == '"') {
                    tmp.push_back(strtmp);
                    strtmp.clear();
                    state = 0;
                }
                else {
                    strtmp.push_back(*ii);
                }
            }
        }
        return tmp;
    }
    class t_ioClass {
    private:
        bool logWithTime = false;
        bool coloringLinuxConsole = false;
        std::string getSysTime(void) {
            time_t t = time(0);
            char tmp[16];
            strftime(tmp, sizeof(tmp), "[%H:%M:%S] ", localtime(&t));
            return tmp;
        }
    public:
        const short __INFO = 1;
        const short __WARNING = 2;
        const short __ERROR = 3;
        const short __FATAL_ERROR = 4;
        const short __DEBUG = 5;
        int print(std::string buf) {
            std::cout << ((logWithTime) ? getSysTime() + buf : buf) << std::endl;
            return 0;
        }
        int printWithPrefix(std::string buf, short type) {
            std::string prefix;
            if (coloringLinuxConsole) {
                switch (type) {
                case 5: prefix += "\033[34m"; break;
                case 4: prefix += "\033[41m\033[37m"; break;
                case 3: prefix += "\033[31m"; break;
                case 2: prefix += "\033[33m"; break;
                case 1: prefix += "\033[0m"; break;
                }
            }
            switch (type) {
            case 5: prefix += "[DEBUG]"; break;
            case 4: prefix += "[FATAL_ERROR]"; break;
            case 3: prefix += "[ERROR]"; break;
            case 2: prefix += "[WARNING]"; break;
            case 1: prefix += "[INFO]"; break;
            }
            std::cout << ((logWithTime) ? prefix + getSysTime() + buf : prefix + buf) << ((coloringLinuxConsole) ? "\033[0m" : "") << std::endl;
            return 0;
        }
        void logWithTheTime(bool option) {
            logWithTime = option;
        }
        void coloringOnLinux(bool option) {
            coloringLinuxConsole = option;
        }
    } io;
    class cmdContainer {
    private:
        argsAry args;
    public:
        cmdContainer operator=(const std::string& str) {
            this->args = compile(str);
            return this->args;
        }
        cmdContainer() {

        }
        cmdContainer(std::string str) {
            args = compile(str);
        }
        cmdContainer(argsAry argy) {
            args = argy;
        }
        int run() {
            if (cmd_register.count(args[0]) == 1) {
                if (args.size() >= cmd_register[args[0]].argcMin && args.size() <= cmd_register[args[0]].argcMax) {
                    return cmd_register[args[0]].func(args);
                }
                else {
                    std::stringstream buf;
                    buf << "Incorrect arguments count: needed ("
                        << cmd_register[args[0]].argcMin << "," << cmd_register[args[0]].argcMax
                        << ") but detected " << args.size();
                    io.printWithPrefix(buf.str(), io.__ERROR);
                    return 32767;
                }
            }
            else {
                std::stringstream buf;
                buf << "Unknown command '" << args[0] << "'";
                io.printWithPrefix(buf.str(), io.__ERROR);
                return 32768;
            }
        }
    };

    void regcmd(std::string cmdstr, //root command
        Fp cmdfp,           //function pointer
        int argcMin,        //min argument count
        int argcMax) {      //max argument count

        tCmdreg temp{ cmdfp,argcMin,argcMax };
        cmd_register[cmdstr] = temp;
    }
}
