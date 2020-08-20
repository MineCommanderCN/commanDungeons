#include "pch.h"

#define CREATEDELL_API_DU _declspec(dllexport)

#include<string>
#include"squidCore_lib.hpp"
#include"cmdungeonsLib.hpp"

void CREATEDELL_API_DU cdl::character::renew_attributes(int nhealth, int narmor, int nattack_power) {
	health = max_health = nhealth;
	armor = narmor;
	attack_power = nattack_power;
}
void CREATEDELL_API_DU cdl::character::rename(std::string str_name) {
	display_name = str_name;
}
CREATEDELL_API_DU cdl::character::character(int id,std::string str_name, int nhealth, int narmor, int nattack_power, int nlevel)
{
	mobid = id;
	display_name = str_name;
	health = max_health = nhealth;
	armor = narmor;
	attack_power = nattack_power;
	level = nlevel;
	exp = 0;
}

template <class T>
T CREATEDELL_API_DU cdl::character::get(std::string attribute) {
	switch (attribute) {
	case "health":return health; break;
	case "hp":return health; break;
	case "max_hp":return max_health; break;
	case "max_health":return max_health; break;
	case "armor":return armor; break;
	case "amr":return armor; break;
	case "attack_power":return attack_power; break;
	case "atp":return attack_power; break;
	case "xp":return exp; break;
	case "exp":return exp; break;
	case "lvl":return level; break;
	case "level":return level; break;
	case "name":return display_name; break;
	case "mobid":return mobid; break;
	case "id":return mobid; break;
	default:return 0;
	}
}