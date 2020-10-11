//include libs
#include "cmdLib/cmdLib.hpp"
#pragma comment(lib,"cmdLib.lib")
//include libs end

int main() {
	lua_state = luaL_newstate();
	static const luaL_Reg lualibs[] =
	{
		{ "base", luaopen_base },
		{ NULL, NULL}
	};
	const luaL_Reg* lib = lualibs;
	for (; lib->func != NULL; lib++)
	{
		luaL_requiref(lua_state, lib->name, lib->func, 1);
		lua_pop(lua_state, 1);
	}
	//Hard-coded system info translate
	cdl::translate_buffer["cmdungeons.fatal.missing_config"] = "FATAL ERROR: Missing file 'config.ini'. Please re-install the commanDungeons.\nYour game save will be kept.";
	cdl::translate_buffer["cmdungeons.fatal.config_damaged"] = "FATAL ERROR: File 'config.ini' can't be read or was damaged. Please try to re-install the commanDungeons.\nYour game save will be kept.";
	cdl::translate_buffer["cmdungeons.msg.loading_datapacks"] = "Loading the Data Packs...";
	cdl::translate_buffer["cmdungeons.msg.loading_config"] = "Loading the config...";
	cdl::translate_buffer["cmdungeons.fatal.no_vanilla_pack"] = "FATAL ERROR: Unable to load vanilla data pack, game can not start up.\nPlease try to re-install the commanDungeons. Your game save will be kept.";
	cdl::translate_buffer["cmdungeons.warning.debug_mode"] = "WARNING: DEBUG mode activated! This feature is for developers only.\nThe pack and script system will be UNSAFE because they are able to MODIFY YOUR SYSTEM now.\nSome debugging commands will also be enabled, which may CRASH the game or DESTROY your game saves if you use them incorrectly.\nIf you are not a developer or a pack creator, please TURN OFF the debug mode in the 'config.ini' file.";
	cdl::translate_buffer["cmdungeons.warning.pack_not_support_language"] = "WARNING: Data pack '%s' does not support your current language '%s', which may cause some content does not display normally.\nYou can try to contact the creator of the pack to solve the problem.";

	std::ifstream inBuff("translate_buffer.json");
	if (inBuff) inBuff >> cdl::translate_buffer;
	inBuff.close();
	ResetColor;

	std::cout << cdl::get_trans("cmdungeons.msg.loading_config") << std::endl;
	//Read config.ini
	std::ifstream loadcfg("config.ini");
	if (!loadcfg) {
		SetColorFatal;
		std::cout << cdl::get_trans("cmdungeons.fatal.missing_config") << std::endl;
		ResetColor;

		system("pause");
		return 0;
	}
	std::string fullcfgfile;
	{
		std::stringstream buf;
		buf << loadcfg.rdbuf();
		fullcfgfile = buf.str();
	}
	loadcfg.close();
	fullcfgfile += "\n";
	{
		std::string keybuf, vlvbuf;
		int state = 0;
		for (std::string::iterator ii = fullcfgfile.begin(); ii != fullcfgfile.end(); ii++) {
			if (state == 0 && *ii != '=')
				keybuf.push_back(*ii);
			else if (state == 0 && *ii == '=')
				state = 1;
			else if (state == 1 && *ii != '\n')
				vlvbuf.push_back(*ii);
			else if (state == 1 && *ii == '\n') {
				state = 0;
				for (int ii = 0; ii < vlvbuf.size(); ii++) {
					if (vlvbuf.substr(ii, 2) == "\\n")
						vlvbuf.replace(ii, 2, "\n");
				}
				cdl::config_keymap[keybuf] = vlvbuf;
				keybuf.clear(); vlvbuf.clear();
			}
		}
	}
	if (cdl::config_keymap.count("lang") == 0) {
		SetColorFatal; std::cout << cdl::get_trans("cmdungeons.fatal.config_damaged") << std::endl; ResetColor;
		system("pause");
		return 0;
	}

	//Read datapacks
	std::cout << cdl::get_trans("cmdungeons.msg.loading_datapacks") << std::endl;
	struct _T_Pack {
		std::string pack_name;
		nlohmann::json pack_info;
		std::map<std::string, nlohmann::json> lang_json;
		typedef std::map<std::string, nlohmann::json> subPaths;
		subPaths attributes, effects, items, levels, mobs, player_skills;
	};
	std::map<std::string, _T_Pack> packs;
	std::vector<std::string> packList;
	cdl::getSubDir(cdl::getPath() + "packs", packList);
	std::ifstream readInfo;
	for (std::vector<std::string>::iterator ii = packList.begin(); ii != packList.end(); ii++) {
		readInfo.open((*ii + "\\pack_info.meta").c_str());
		if (readInfo) {
			cdl::replace_substr(*ii, cdl::getPath() + "packs\\", "");
			readInfo >> packs[*ii].pack_info;
		}
		//std::cout << *ii << std::endl;
	}
	readInfo.close();

	if (cdl::config_keymap["debug"] != "true") {
		bool vanillaNotLoaded = true;
		for (std::map<std::string, _T_Pack>::iterator ii = packs.begin(); ii != packs.end(); ii++) {
			if (ii->first == "vanilla") vanillaNotLoaded = false;
		}

		if (vanillaNotLoaded) {
			SetColorFatal; std::cout << cdl::get_trans("cmdungeons.fatal.no_vanilla_pack") << std::endl; ResetColor;
			system("pause");
			return 0;
		}
	}
	else {
		SetColorWarning; std::cout << cdl::get_trans("cmdungeons.warning.debug_mode") << std::endl; ResetColor;
	}

	//Load translations
	for (std::map<std::string, _T_Pack>::iterator ii = packs.begin(); ii != packs.end(); ii++) {
		nlohmann::json language_tmp;
		std::ifstream readTmp((cdl::getPath() + "packs\\" + ii->first + "\\translate\\" + cdl::config_keymap["lang"] + ".json").c_str());
		if 
			(readTmp) readTmp >> language_tmp;
		else {
			std::string transbuf = cdl::get_trans("cmdungeons.warning.pack_not_support_language");
			cdl::replace_substr(transbuf, "%s", ii->first);
			cdl::replace_substr(transbuf, "%s", cdl::config_keymap["lang"]);
			std::cout << transbuf << std::endl;
		}
		cdl::translate_buffer.merge_patch(language_tmp);
		//std::cout << cdl::getPath() + "packs\\" + *ii + "\\translate\\" + cdl::config_keymap["lang"] + ".json" << std::endl;
	}


	std::ofstream outBuff("translate_buffer.json");
	outBuff << std::setw(4) << cdl::translate_buffer << std::endl;
	cmdReg::regist_cmd();
	player.setup("generic:player", "Player", 20, 2, 4, 0, 0, 0);
	SetColorGreat; std::cout << cdl::get_trans("cmdungeons.msg.loading.done") << std::endl; ResetColor;
	SetColorExellent;  std::cout << cdl::get_trans("cmdungeons.msg.welcome") << std::endl; ResetColor;

	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		sqc::cmdContainer cmc(input);
		switch (cmc.run()) {
		  case 0: break;
		  case sqc::EXIT_MAIN: return 0;
		  case sqc::ARGUMENT_ERROR: SetColorError; std::cout << cdl::get_trans("cmdungeons.error.argument_error") << std::endl; ResetColor; break;
		  case sqc::UNKNOWN_COMMAND: SetColorError; std::cout << cdl::get_trans("cmdungeons.error.unknown_command") << std::endl; ResetColor; break;
		  default: SetColorWarning; std::cout << cdl::get_trans("cmdungeons.warning.unexpected_error") << std::endl; ResetColor;
		}
	}
}