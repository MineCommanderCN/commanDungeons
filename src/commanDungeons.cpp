#include "squidCore_lib.hpp"

namespace cdl {
	class character {
	private:
		int max_hp, hp, amr, atp, xp = 0, lvl;
		std::string name;
	public:
		character(std::string display_name, int health, int armor, int attack_power, int level);
		void renew_attributes(int health, int armor, int attack_power) {
			hp = max_hp = health;
			amr = armor;
			atp = attack_power;
		}

	};
	character::character(std::string display_name, int health, int armor, int attack_power, int level)
	{
		name = display_name;
		hp = max_hp = health;
		amr = armor;
		atp = attack_power;
		lvl = level;
	}

}



int attack(const lcmd& args) {
	return 0;
}

int createSave(const lcmd& args) {
	return 0;
}

int loadSave(const lcmd& args) {
	return 0;
}

int saveIn(const lcmd& args) {
	return 0;
}

int saveInPath(const lcmd& args) {
	return 0;
}



void regist_cmd(void) {
	sll::regcmd("attack",attack,1,1);
	sll::regcmd("newplr", createSave, 2, 2);
	sll::regcmd("load", loadSave, 2, 2);
	sll::regcmd("save", saveIn, 1, 1);
	sll::regcmd("saveas", saveInPath, 2, 2);
}


int main() {
	regist_cmd();
	cdl::character enemy("test1", 10, 0, 6, 0);
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		sll::command.run(input);
	}
}
