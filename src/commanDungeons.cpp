#include "squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"

#include"cmdLib.hpp"

int main() {
	player.setup(0, "Player", 20, 2, 4, 0, 0);
	enemy.setup(1, "test1", 10, 1, 2, 0, 5);
	std::cout << "Loading Commands..." << std::endl;
	cmdReg::regist_cmd();
	std::cout << "Loading the Translates..." << std::endl;
	//TO DO: load the translate
	std::cout << "Done!\n\nWelcome to CommanDungeons!\nType in \"commands\" or \"help\" for the list of all commands." << std::endl;
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		if (sll::command.run(input) == EXIT_MAIN)
			return 0;
	}
}