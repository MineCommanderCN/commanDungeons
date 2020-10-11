#pragma once
#include"squidCore/squidCore_lib.hpp"
#include"cmdungeonsLib/cmdungeonsLib.hpp"
#pragma comment(lib,"squidCore.lib")
#pragma comment(lib,"cmdungeonsLib.lib")


cdl::character player;
#include<cstdlib>
#include<ctime>
#include<cmath>
const int EXIT_MAIN = 65536;
namespace cmdReg {
	int CREATEDELL_API_DU attack(const sqc::argsAry& args) {
		srand(time(0));
		//player.attack(enemy, rand() % 6 + 1, rand() % 6 + 1);
		std::cout << "================\n";
		//enemy.attack(player, rand() % 6 + 1, rand() % 6 + 1);

		return 0;
	}

	int CREATEDELL_API_DU loadSave(const sqc::argsAry& args) {
		sqc::cmdContainer tmp("save");
		tmp.run();
		player.attri.display_name = args[1];
		//TO DO: Something to load your save
		{
			std::string transbuf = cdl::get_trans("cmdungeons.msg.load_save.done");
			cdl::replace_substr(transbuf, "%s", player.attri.display_name);
			std::cout << transbuf << std::endl;
		}
		return 0;
	}

	int CREATEDELL_API_DU saveIn(const sqc::argsAry& args) {
		std::string savepath = "saves/" + player.attri.display_name + ".cmdgnsave";
		std::fstream saving(savepath.c_str(), std::ios::out);
		saving << "Here is nothing";	//TO DO: Something to save the game
		{
			std::string transbuf = cdl::get_trans("cmdungeons.msg.save.done");
			cdl::replace_substr(transbuf, "%s", player.attri.display_name);
			std::cout << transbuf << std::endl;
		}
		return 0;
	}

	int CREATEDELL_API_DU inputName(const sqc::argsAry& args) {
		player.attri.display_name = args[1];
		SetColorGreat; std::string transbuf = cdl::get_trans("cmdungeons.msg.rename"); ResetColor;
		cdl::replace_substr(transbuf, "%s", args[1]);
		std::cout << transbuf << std::endl;
		return 0;
	}

	int CREATEDELL_API_DU visitInfo(const sqc::argsAry& args) {
		if (args[1] == player.attri.display_name || args[1] == "me" || args[1] == "player") {
			{
				SetColorExellent;
				std::string transbuf = cdl::get_trans("cmdungeons.msg.info");
				cdl::replace_substr(transbuf, "%s", player.attri.display_name);
				std::cout << transbuf << std::endl;
				ResetColor;
			}
			SetColorWarning;
			std::cout << "LV" << player.attri.level << " " << player.attri.exp << "/" << pow(player.attri.level, 2) * 10 + 50
				<< std::endl << player.attri.gold << "G"
				<< "\nHP: " << player.attri.health << "/" << player.attri.max_health
				<< "\nATP: " << player.attri.attack_power
				<< "\nAMR: " << player.attri.armor << std::endl;
			ResetColor;
		}
		else if (/*args[1] == cdl::get_trans(enemy.get_attributes().display_name) ||*/ args[1] == "enemy") {
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.info");
				//if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, cdl::get_trans(enemy.get_attributes().display_name));
				std::cout << transbuf << std::endl;
			}
			/*std::cout << "LV" << enemy.get_attributes().level << "  " << enemy.get_attributes().gold << "G " << enemy.get_attributes().exp << "XP"
				<< "\nHP: " << enemy.get_attributes().health << "/" << enemy.get_attributes().max_health
				<< "\nATP: " << enemy.get_attributes().attack_power
				<< "\nARM: " << enemy.get_attributes().armor << std::endl;*/
		}
		else {
			SetColorError;
			std::string transbuf = cdl::get_trans("cmdungeons.msg.info.error");
			cdl::replace_substr(transbuf, "%s", args[1]);
			std::cout << transbuf << std::endl;
			ResetColor;
		}
		return 0;
	}

	int CREATEDELL_API_DU exitGame(const sqc::argsAry& args) {
		return EXIT_MAIN;
	}

	int CREATEDELL_API_DU directLuaScript(const sqc::argsAry& args) {	//Run a lua script in raw lua feature
		if (cdl::config_keymap["debug"] != "true") {
			SetColorError;
			std::cout << cdl::get_trans("cmdungeons.error.debug_only") << std::endl;
			ResetColor;
		}
		else 
			return luaL_dofile(lua_state, args[1].c_str());
		return 0;
	}

	void CREATEDELL_API_DU regist_cmd(void) {
		sqc::regcmd("attack", cmdReg::attack, 1, 1);
		sqc::regcmd("load", cmdReg::loadSave, 2, 2);
		sqc::regcmd("save", cmdReg::saveIn, 1, 1);
		sqc::regcmd("name", cmdReg::inputName, 2, 2);
		sqc::regcmd("info", cmdReg::visitInfo, 2, 2);
		sqc::regcmd("exit", cmdReg::exitGame, 1, 1);
		sqc::regcmd("lua-script-direct", cmdReg::directLuaScript, 2, 2);
	}
}