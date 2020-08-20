#include "squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"
#include"cmdLib.hpp"
int main() {
	regist_cmd();
	
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		sll::command.run(input);
	}
	cdl::enemy.get<std::string>("name");
}