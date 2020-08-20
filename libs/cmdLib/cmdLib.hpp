#pragma once
#define CREATEDELL_API_DU _declspec(dllexport)
#include"squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"
cdl::character player;
cdl::character enemy;
#include<cstdlib>
#include<ctime>
#include<cmath>
const int EXIT_MAIN = 65536;
namespace cmdReg {
	int CREATEDELL_API_DU attack(const lcmd& args) {
		srand(time(0));
		player.attack(enemy, rand() % 6 + 1, rand() % 6 + 1);
		std::cout << "================\n";
		enemy.attack(player, rand() % 6 + 1, rand() % 6 + 1);
		
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

	int CREATEDELL_API_DU inputName(const lcmd& args) {
		player.rename(args[1]);
		std::cout << "Your name has been changed to: " << args[1] << std::endl;
		return 0;
	}

	int CREATEDELL_API_DU visitInfo(const lcmd& args) {
		if (args[1] == player.get_attributes().display_name || args[1] == "me" || args[1] == "player")
			std::cout << player.get_attributes().display_name << "'s Info:" << std::endl
			<< "LV" << player.get_attributes().level << " " << player.get_attributes().exp << "/" << pow(player.get_attributes().level, 2) * 10 + 50
			<< "\nGold: " << player.get_attributes().gold
			<< "\nHP: " << player.get_attributes().health << "/" << player.get_attributes().max_health
			<< "\nAttack Power: " << player.get_attributes().attack_power
			<< "\nArmor: " << player.get_attributes().armor << std::endl;
		else if (args[1] == enemy.get_attributes().display_name || args[1] == "enemy")
			std::cout << enemy.get_attributes().display_name << "'s Info:" << std::endl
			<< "LV" << enemy.get_attributes().level << "  " << "Gold reward: " << enemy.get_attributes().gold
			<< "\nHP: " << enemy.get_attributes().health << "/" << enemy.get_attributes().max_health
			<< "\nAttack Power: " << enemy.get_attributes().attack_power
			<< "\nArmor: " << enemy.get_attributes().armor << std::endl;
		else
			std::cout << "Unknown target\n";
		return 0;
	}

	int CREATEDELL_API_DU exitGame(const lcmd& args) {
		return EXIT_MAIN;
	}

	void CREATEDELL_API_DU regist_cmd(void) {
		sll::regcmd("attack", cmdReg::attack, 1, 1);
		sll::regcmd("load", cmdReg::loadSave, 2, 2);
		sll::regcmd("save", cmdReg::saveIn, 1, 1);
		sll::regcmd("saveas", cmdReg::saveInPath, 2, 2);
		sll::regcmd("name", cmdReg::inputName, 2, 2);
		sll::regcmd("info", cmdReg::visitInfo, 2, 2);
		sll::regcmd("exit", cmdReg::exitGame, 1, 1);
	}
}
