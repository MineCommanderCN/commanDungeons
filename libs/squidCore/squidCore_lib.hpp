#pragma once
#define CREATEDELL_API_DU _declspec(dllexport)
#include<iostream>
#include<vector>
#include<map>
#include<ctime>
#include<fstream>
#include<string>
#include<sstream>
namespace cdl {
    std::map<std::string, std::string> trans_str;
}
typedef std::vector<std::string> lcmd;
typedef int(*Fp)(const lcmd& args);
namespace sll {
    std::string CREATEDELL_API_DU get_trans(std::string key) {
        if (cdl::trans_str.count(key) == 1) return cdl::trans_str[key];
        else return key;
    }
    const int EXIT_MAIN = 65536;
    const int MAXN = 2147483647;
    struct tCmdreg {
        std::string rootcmd;
        Fp func;
        int argcMin;
        int argcMax;
    };
    std::vector<tCmdreg> cmd_register;
    std::string CREATEDELL_API_DU getSysTimeData(void) {
        time_t t = time(0);
        char tmp[64];
        strftime(tmp, sizeof(tmp), "%Y%m%d-%H%M%S", localtime(&t));
        return tmp;
    }
    std::string CREATEDELL_API_DU getSysTime(void) {
        time_t t = time(0);
        char tmp[64];
        strftime(tmp, sizeof(tmp), "%H:%M:%S", localtime(&t));
        return tmp;
    }
    template <class Ta, class Tb>
    Tb CREATEDELL_API_DU atob(const Ta& t) {
        std::stringstream temp;
        temp << t;
        Tb i;
        temp >> i;
        return i;
    }
    class tcSqLCmd {
    public:
        int CREATEDELL_API_DU run(std::string command) {
            command += "\n";
            std::vector<lcmd> cmdlines;
            lcmd ltemp;
            std::string strbuf;
            char state = 0;
            for (std::string::iterator li = command.begin(); li != command.end(); li++) {
                if (*li == '\n') {
                    if (!strbuf.empty()) {
                        ltemp.push_back(strbuf);
                        strbuf.clear();
                    }
                    state = 0;
                    if (!ltemp.empty() && ltemp[0].at(0) != '#') {
                        cmdlines.push_back(ltemp);
                        ltemp.clear();
                    }
                }
                else if (state == 0) {
                    if (*li == ' ')
                        if (!strbuf.empty()) {
                            ltemp.push_back(strbuf);
                            strbuf.clear();
                        }
                        else;
                    else if (*li == '"') {
                        if (!strbuf.empty()) {
                            ltemp.push_back(strbuf);
                            strbuf.clear();
                        }
                        state = 1;
                    }
                    else strbuf.push_back(*li);
                }
                else if (state == 1) {
                    if (*li == '"') {
                        if (!strbuf.empty()) {
                            ltemp.push_back(strbuf);
                            strbuf.clear();
                        }
                        state = 0;
                    }
                    else strbuf.push_back(*li);
                }
            }

            for (std::vector<lcmd>::iterator i = cmdlines.begin(); i != cmdlines.end(); i++) {
                bool scfl = false;

                for (std::vector<tCmdreg>::iterator cf = cmd_register.begin(); cf != cmd_register.end(); cf++) {
                    if (cf->rootcmd == (*i)[0]) {
                        scfl = true;
                        if (i->size() >= cf->argcMin && i->size() <= cf->argcMax) {
                            if (cf->func(*i) == EXIT_MAIN)
                                return EXIT_MAIN;
                        }
                        else {
                            std::string transbuf = get_trans("squidcore.error.incorrect_parameters_count");
                            if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, atob<int, std::string>(i->size()));
                            if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, atob<int, std::string>(cf->argcMin));
                            if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, atob<int, std::string>(cf->argcMax));
                            std::cout << transbuf << std::endl;
                        }
                    }
                }
                if (!i->empty() && !scfl) {
                    std::string transbuf = get_trans("squidcore.error.unknown_command");
                    if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, (*i)[0]);
                    std::cout << transbuf << std::endl;
                }
            }
            return 0;
        }
    }   command;
    void CREATEDELL_API_DU regcmd(std::string cmdstr, //根命令字符串
        Fp cmdfp,           //函数指针
        int argcMin,        //需要的最少参数数量
        int argcMax) {      //需要的最大参数数量
                            //若参数数量不处于[argcMin,argcMax]中将会报错。
        tCmdreg temp{ cmdstr,cmdfp,argcMin,argcMax };
        cmd_register.push_back(temp);
    }
}
