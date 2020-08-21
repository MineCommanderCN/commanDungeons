#include "squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"
#include"cmdLib.hpp"

int main() {
	std::cout << "Loading the Translates..." << std::endl;
	std::ifstream loadtrans("translate/en_us.lang");
	if (!loadtrans) {
		std::cout << "FATAL ERROR: Missing file 'translate/en_us.lang'. Please re-install the commanDungeons.\nYour game save will be kept.\n";
		system("pause");
		return 0;
	}
	std::string fulltransfile;
	{
		std::stringstream buf;
		buf << loadtrans.rdbuf();
		fulltransfile = buf.str();
	}
	loadtrans.close();
	fulltransfile += "\n";
	{
		std::string keybuf, vlvbuf;
		int state = 0;
		for (std::string::iterator ii = fulltransfile.begin(); ii != fulltransfile.end(); ii++) {
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
				cdl::trans_str[keybuf] = vlvbuf;
				keybuf.clear(); vlvbuf.clear();
			}
		}
	}

	std::cout << sll::get_trans("cmdungeons.msg.loading.0") << std::endl;
	cmdReg::regist_cmd();
	std::cout << sll::get_trans("cmdungeons.msg.loading.1") << std::endl;

	std::ifstream loaddata("data/enemy_info.txt");
	if (!loaddata) {
		std::cout << "FATAL ERROR: Missing file 'data/enemy_info.txt'. Please re-install the commanDungeons.\nYour game save will be kept.\n";
		system("pause");
		return 0;
	}

	cdl::chrt_data buf;
	while (loaddata >> buf.display_name
		&& loaddata >> buf.mobid
		&& loaddata >> buf.level
		&& loaddata >> buf.health
		&& loaddata >> buf.armor
		&& loaddata >> buf.attack_power
		&& loaddata >> buf.exp
		&& loaddata >> buf.gold
		) cdl::enemy_info.push_back(buf);
	loaddata.close();


	player.setup(0, "Player", 20, 2, 4, 0, 0,0);
	enemy.setup(cdl::enemy_info[0].mobid,
		cdl::enemy_info[0].display_name,
		cdl::enemy_info[0].health,
		cdl::enemy_info[0].armor,
		cdl::enemy_info[0].attack_power,
		cdl::enemy_info[0].level,
		cdl::enemy_info[0].gold,
		cdl::enemy_info[0].exp
	);
	std::cout << sll::get_trans("cmdungeons.msg.loading.done") << std::endl;
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		if (sll::command.run(input) == EXIT_MAIN)
			return 0;
	}
}