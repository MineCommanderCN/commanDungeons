#include "pch.h"

#define CREATEDELL_API_DU _declspec(dllexport)

#include"squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"
#include "cmdLib.hpp"
int CREATEDELL_API_DU attack(const lcmd& args) {
	return 0;
}

int CREATEDELL_API_DU createSave(const lcmd& args) {
	return 0;
}

int CREATEDELL_API_DU loadSave(const lcmd& args) {
	return 0;
}

int CREATEDELL_API_DU saveIn(const lcmd& args) {
	return 0;
}

int CREATEDELL_API_DU saveInPath(const lcmd& args) {
	return 0;
}

void CREATEDELL_API_DU regist_cmd(void) {
	sll::regcmd("attack", attack, 1, 1);
	sll::regcmd("newplr", createSave, 2, 2);
	sll::regcmd("load", loadSave, 2, 2);
	sll::regcmd("save", saveIn, 1, 1);
	sll::regcmd("saveas", saveInPath, 2, 2);
}