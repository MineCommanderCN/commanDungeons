#pragma once
#ifdef CREATEDELL_API_DU
#else                                                                            
#define CREATEDELL_API_DU _declspec(dllimport)
#endif   

#include<iostream>
#include<vector>
#include<map>
#include<ctime>
#include<fstream>
#include<string>
#include<sstream>

typedef std::vector<std::string> lcmd;
typedef int(*Fp)(const lcmd& args);
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
    std::string CREATEDELL_API_DU getSysTimeData(void);
    std::string CREATEDELL_API_DU getSysTime(void);
    template <class Ta, class Tb>
    Tb CREATEDELL_API_DU atob(const Ta& t);
    class tcSqLCmd {
    public:
        int CREATEDELL_API_DU run(std::string command);
    }   command;
    void CREATEDELL_API_DU regcmd(std::string cmdstr, //�������ַ���
        Fp cmdfp,           //����ָ��
        int argcMin,        //��Ҫ�����ٲ�������
        int argcMax);       //��Ҫ������������
                            //����������������[argcMin,argcMax]�н��ᱨ��
}

