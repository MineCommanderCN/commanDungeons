#include "squidCore_lib.hpp"
#include "cmdungeonsLib.hpp"
#include "cmdLib.hpp"
#include "nlohmannJson.hpp"
int main() {
	std::cout << "Loading Config..." << std::endl;
	std::ifstream loadcfg("config.ini");
	if (!loadcfg) {
		std::cout << "FATAL ERROR: Missing file 'config.ini'. Please re-install the commanDungeons.\nYour game save will be kept.\n";
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
	if (cdl::config_keymap.count("lang")==0) {
		std::cout << "FATAL ERROR: File 'config.ini' can't be read or was damaged. Please re-install the commanDungeons.\nYour game save will be kept.\n";
		system("pause");
		return 0;
	}


	std::cout << "Loading the Translates..." << std::endl;
	std::ifstream loadtrans(("translate/" + cdl::config_keymap["lang"] + ".json").c_str());
	if (!loadtrans) {
		std::cout << "FATAL ERROR: Missing file 'translate/" + cdl::config_keymap["lang"] + ".json'. Please check out your game config or re-install the commanDungeons.\nYour game save will be kept.\n";
		system("pause");
		return 0;
	}
	
	loadtrans >> cdl::translate_json;
	loadtrans.close();
	

	std::cout << sll::get_trans("cmdungeons.msg.loading.0") << std::endl;
	cmdReg::regist_cmd();
	std::cout << sll::get_trans("cmdungeons.msg.loading.1") << std::endl;

	std::ifstream loaddata("data/enemy_info.json");
	if (!loaddata) {
		std::cout << "FATAL ERROR: Missing file 'data/enemy_info.json'. Please re-install the commanDungeons.\nYour game save will be kept.\n";
		system("pause");
		return 0;
	}
	
	nlohmann::json enemydata_json;
	loaddata >> enemydata_json;
	loaddata.close();


	
	int version = enemydata_json["file_format"];
	if (version != 1) {
		std::cout << "WARNING: The file 'data/enmy_info.json' you using was designed for the legacy version of commanDungeons.\nThe game will still try to read the file, which may cause many unexpected issue." << std::endl;
	}


	player.setup(0, "Player", 20, 2, 4, 0, 0,0);
	std::cout << sll::get_trans("cmdungeons.msg.loading.done") << std::endl;
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		if (sll::command.run(input) == EXIT_MAIN)
			return 0;
	}
}