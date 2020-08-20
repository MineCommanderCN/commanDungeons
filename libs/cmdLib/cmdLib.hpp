#pragma once

#ifdef CREATEDELL_API_DU
#else                                                                            
#define CREATEDELL_API_DU _declspec(dllimport)
#endif
namespace cmdReg {
	int CREATEDELL_API_DU attack(const lcmd& args);

	int CREATEDELL_API_DU loadSave(const lcmd& args);

	int CREATEDELL_API_DU saveIn(const lcmd& args);

	int CREATEDELL_API_DU saveInPath(const lcmd& args);

	int CREATEDELL_API_DU inputName(const lcmd& args);

	void CREATEDELL_API_DU regist_cmd(void);
}
