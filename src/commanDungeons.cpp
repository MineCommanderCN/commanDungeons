#include "squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"
#include"cmdLib.hpp"
int main() {
	cmdReg::regist_cmd();
	
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		sll::command.run(input);
	}
	std::string name = cdl::enemy.get_attributes().display_name;
}