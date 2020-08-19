#define _CRT_SECURE_NO_WARNINGS
#include<iostream>
#include<vector>
#include<map>
#include<ctime>
#include<fstream>
#include<string>
#include<sstream>

typedef std::vector<std::string> lcmd;
typedef int(*Fp)(const lcmd &args);
namespace sll {
    const int EXIT_MAIN = 65536;
    const int MAXN = 2147483647;
    struct tCmdreg {
        std::string rootcmd;
        Fp func;
        int argcMin;
        int argcMax;
    };
    std::vector<tCmdreg> cmd_register;
    std::string getSysTimeData(void) {
        time_t t = time(0);
        char tmp[64];
        strftime(tmp, sizeof(tmp), "%Y%m%d-%H%M%S", localtime(&t));
        return tmp;
    }
    std::string getSysTime(void) {
        time_t t = time(0);
        char tmp[64];
        strftime(tmp, sizeof(tmp), "%H:%M:%S", localtime(&t));
        return tmp;
    }
    template <class Ta, class Tb>
    Tb atob(const Ta& t) {
        std::stringstream temp;
        temp << t;
        Tb i;
        temp >> i;
        return i;
    }
    class tcSqLCmd {
    public:
        int run(std::string command) {  //支持解析多行文本
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

                            std::cout << "Incorrect parameters count: Detected " << i->size() << " but needed [" << cf->argcMin << "," << cf->argcMax << "]" << std::endl;

                        }
                    }
                }
                if (!i->empty() && !scfl) std::cout << "Unknown command '" << (*i)[0] << "'\n";
            }
            return 0;
        }
    }   command;
    void regcmd(std::string cmdstr, //根命令字符串
                Fp cmdfp,           //函数指针
                int argcMin,        //需要的最少参数数量
                int argcMax) {      //需要的最大参数数量
                                    //若参数数量不处于[argcMin,argcMax]中将会报错。
        tCmdreg temp{ cmdstr,cmdfp,argcMin,argcMax };
        cmd_register.push_back(temp);
    }
}
