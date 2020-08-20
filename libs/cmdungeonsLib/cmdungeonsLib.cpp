#include "pch.h"

#define CREATEDELL_API_DU _declspec(dllexport)

#include<string>
#include"squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"

const int ERROR_CODE = -65536;

void CREATEDELL_API_DU cdl::character::renew_attributes(int nhealth, int narmor, int nattack_power) {
	attri.health = attri.max_health = nhealth;
	attri.armor = narmor;
	attri.attack_power = nattack_power;
}
void CREATEDELL_API_DU cdl::character::rename(std::string str_name) {
	attri.display_name = str_name;
}
CREATEDELL_API_DU cdl::character::character(int id,std::string str_name, int nhealth, int narmor, int nattack_power, int nlevel,int money)
{
	attri.mobid = id;
	attri.display_name = str_name;
	attri.health = attri.max_health = nhealth;
	attri.armor = narmor;
	attri.attack_power = nattack_power;
	attri.level = nlevel;
	attri.exp = 0;
	attri.gold = money;
}
cdl::chrt_data CREATEDELL_API_DU cdl::character::get_attributes(void) {
	return attri;
}

void CREATEDELL_API_DU cdl::character::attack(cdl::character target, int aq,int dq) {
	std::cout << attri.display_name << " Attacked the " << target.attri.display_name << "!\n";
	float apers = 0.2 * aq;
	float dpers = 0.15 * dq;
	if (aq == 6) apers *= 1.25;
	if (dq == 6) dpers *= 1.25;
	int dd = int(attri.attack_power * apers - target.get_attributes().armor * dpers);
	std::cout << attri.display_name << " Rolled a " << aq << " AQ.\n";
	std::cout << target.attri.display_name << " Rolled a " << dq << " DQ.\n";
	std::cout << attri.display_name << " Dealted " << int(attri.attack_power * apers) << "HP of damage to " << target.attri.display_name << "!\n";
	std::cout << target.attri.display_name << " Blocked " << int(target.get_attributes().armor * dpers) << "HP of damage.\n";
	target.dealt_damage(dd);
}
bool CREATEDELL_API_DU cdl::character::is_death(void) {
	if (attri.health <= 0) return true;
	else return false;
}
int CREATEDELL_API_DU cdl::character::dealt_damage(int damage) {
	if (damage <= 0) return ERROR_CODE;
	attri.health -= damage;
	std::cout << attri.display_name << " Suffered " << damage << "HP of damage: " << attri.health << "/" << attri.max_health << std::endl;
	return attri.health;
}