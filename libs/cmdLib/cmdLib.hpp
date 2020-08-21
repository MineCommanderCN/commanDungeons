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
		sll::command.run("save");
		player.rename(args[1]);
		//TO DO: Something to load your save
		{
			std::string transbuf = sll::get_trans("cmdungeons.msg.load.done");
			sll::replace_substr(transbuf, "%s", player.get_attributes().display_name);
			std::cout << transbuf << std::endl;
		}
		return 0;
	}

	int CREATEDELL_API_DU saveIn(const lcmd& args) {
		char* pathvar;
		pathvar = getenv("APPDATA");
		std::string savepath = pathvar;
		savepath += "\\commanDungeons\\saves\\" + player.get_attributes().display_name + ".cmdgnsave";
		std::ofstream saving(savepath.c_str());
		saving << "Here is nothing";	//TO DO: Something to save the game
		{
			std::string transbuf = sll::get_trans("cmdungeons.msg.save.done");
			sll::replace_substr(transbuf, "%s", player.get_attributes().display_name);
			std::cout << transbuf << std::endl;
		}
		return 0;
	}

	int CREATEDELL_API_DU inputName(const lcmd& args) {
		player.rename(args[1]);
		std::cout << "Your name has been changed to: " << args[1] << std::endl;
		return 0;
	}

	int CREATEDELL_API_DU visitInfo(const lcmd& args) {
		if (args[1] == player.get_attributes().display_name || args[1] == "me" || args[1] == "player") {
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.info");
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, player.get_attributes().display_name);
				std::cout << transbuf << std::endl;
			}
			std::cout << "LV" << player.get_attributes().level << " " << player.get_attributes().exp << "/" << pow(player.get_attributes().level, 2) * 10 + 50
				<< std::endl << player.get_attributes().gold << "G"
				<< "\nHP: " << player.get_attributes().health << "/" << player.get_attributes().max_health
				<< "\nATP: " << player.get_attributes().attack_power
				<< "\nAMR: " << player.get_attributes().armor << std::endl;
		}
		else if (args[1] == sll::get_trans(enemy.get_attributes().display_name) || args[1] == "enemy") {
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.info");
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, sll::get_trans(enemy.get_attributes().display_name));
				std::cout << transbuf << std::endl;
			}
			std::cout << "LV" << enemy.get_attributes().level << "  " << enemy.get_attributes().gold << "G " << enemy.get_attributes().exp << "XP"
				<< "\nHP: " << enemy.get_attributes().health << "/" << enemy.get_attributes().max_health
				<< "\nATP: " << enemy.get_attributes().attack_power
				<< "\nARM: " << enemy.get_attributes().armor << std::endl;
		}
		else
			std::cout << sll::get_trans("cmdungeons.msg.info.error") << std::endl;
		return 0;
	}

	int CREATEDELL_API_DU exitGame(const lcmd& args) {
		return EXIT_MAIN;
	}

	void CREATEDELL_API_DU regist_cmd(void) {
		sll::regcmd("attack", cmdReg::attack, 1, 1);
		sll::regcmd("load", cmdReg::loadSave, 2, 2);
		sll::regcmd("save", cmdReg::saveIn, 1, 1);
		sll::regcmd("name", cmdReg::inputName, 2, 2);
		sll::regcmd("info", cmdReg::visitInfo, 2, 2);
		sll::regcmd("exit", cmdReg::exitGame, 1, 1);
	}
}