#include "pch.h"

#define CREATEDELL_API_DU _declspec(dllexport)

#include"squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"
#include<cstdlib>
#include<ctime>
#include "cmdLib.hpp"

int CREATEDELL_API_DU cmdReg::attack(const lcmd& args) {
	srand(time(0));
	cdl::player.attack(cdl::enemy, rand() % 6 + 1, rand() % 6 + 1);
	std::cout << "================\n";
	cdl::enemy.attack(cdl::player, rand() % 6 + 1, rand() % 6 + 1);
	return 0;
}

int CREATEDELL_API_DU cmdReg::loadSave(const lcmd& args) {
	return 0;
}

int CREATEDELL_API_DU cmdReg::saveIn(const lcmd& args) {
	return 0;
}

int CREATEDELL_API_DU cmdReg::saveInPath(const lcmd& args) {
	return 0;
}

int CREATEDELL_API_DU cmdReg::inputName(const lcmd& args) {
	return 0;
}


void CREATEDELL_API_DU cmdReg::regist_cmd(void) {
	sll::regcmd("attack", cmdReg::attack, 1, 1);
	sll::regcmd("load", cmdReg::loadSave, 2, 2);
	sll::regcmd("save", cmdReg::saveIn, 1, 1);
	sll::regcmd("saveas", cmdReg::saveInPath, 2, 2);
	sll::regcmd("name", cmdReg::inputName, 2, 2);
}