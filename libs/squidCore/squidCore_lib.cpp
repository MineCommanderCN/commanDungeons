
#include "pch.h"

#define CREATEDELL_API_DU _declspec(dllexport)

#include "squidCore_lib.hpp"

std::string CREATEDELL_API_DU sll::getSysTimeData(void) {
    time_t t = time(0);
    char tmp[64];
    strftime(tmp, sizeof(tmp), "%Y%m%d-%H%M%S", localtime(&t));
    return tmp;
}
std::string CREATEDELL_API_DU sll::getSysTime(void) {
    time_t t = time(0);
    char tmp[64];
    strftime(tmp, sizeof(tmp), "%H:%M:%S", localtime(&t));
    return tmp;
}
template <class Ta, class Tb>
Tb CREATEDELL_API_DU sll::atob(const Ta& t) {
    std::stringstream temp;
    temp << t;
    Tb i;
    temp >> i;
    return i;
}
int CREATEDELL_API_DU sll::tcSqLCmd::run(std::string command) {  //支持解析多行文本
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
void CREATEDELL_API_DU sll::regcmd(std::string cmdstr, Fp cmdfp, int argcMin, int argcMax) {
    tCmdreg temp{ cmdstr,cmdfp,argcMin,argcMax };
    cmd_register.push_back(temp);
}