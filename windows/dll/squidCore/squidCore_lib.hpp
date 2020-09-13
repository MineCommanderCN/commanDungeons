#pragma once
#define CREATEDELL_API_DU _declspec(dllexport)
#include<iostream>
#include<vector>
#include<map>
#include<ctime>
#include<fstream>
#include<string>
#include<sstream>
#include<windows.h>
#include "nlohmannJson.hpp"

namespace cdl {
    nlohmann::json translate_json;
    std::map<std::string, std::string> config_keymap;
}
typedef std::vector<std::string> lcmd;
typedef int(*Fp)(const lcmd& args);
namespace sll {
    std::string CREATEDELL_API_DU utf8_to_ansi(std::string strUTF8) {	//方法来源：https://blog.csdn.net/yuanwow/article/details/98469297
        UINT nLen = MultiByteToWideChar(CP_UTF8, NULL, strUTF8.c_str(), -1, NULL, NULL);
        WCHAR* wszBuffer = new WCHAR[nLen + 1];
        nLen = MultiByteToWideChar(CP_UTF8, NULL, strUTF8.c_str(), -1, wszBuffer, nLen);
        wszBuffer[nLen] = 0;
        nLen = WideCharToMultiByte(936, NULL, wszBuffer, -1, NULL, NULL, NULL, NULL);
        CHAR* szBuffer = new CHAR[nLen + 1];
        nLen = WideCharToMultiByte(936, NULL, wszBuffer, -1, szBuffer, nLen, NULL, NULL);
        szBuffer[nLen] = 0;
        strUTF8 = szBuffer;
        delete[]szBuffer;
        delete[]wszBuffer;
        return strUTF8;
    }
    std::string CREATEDELL_API_DU get_trans(std::string key) {
        if (cdl::translate_json.count(key) == 1) return utf8_to_ansi(cdl::translate_json[key]);
        else return "WARNING: Missing translation key '" + key + "'";
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
    bool CREATEDELL_API_DU replace_substr(std::string& raw, std::string from, std::string to) {
        if (raw.find(from) != std::string::npos) {
            raw.replace(raw.find(from), from.size(), to);
            return 0;
        }
        else return 1;
    }
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
                            sll::replace_substr(transbuf, "%d", atob<int, std::string>(i->size()));
                            sll::replace_substr(transbuf, "%d", atob<int, std::string>(cf->argcMin));
                            sll::replace_substr(transbuf, "%d", atob<int, std::string>(cf->argcMax));
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
