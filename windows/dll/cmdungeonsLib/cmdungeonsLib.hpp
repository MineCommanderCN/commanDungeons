#pragma once
#define CREATEDELL_API_DU _declspec(dllexport)
#include<string>
#include<iostream>
#include<map>
#include<vector>
#include"squidCore/squidCore_lib.hpp"
#pragma comment(lib, "squidCore.lib")
#include<windows.h>
namespace cdl {
	struct _T_chrt_data {
		int max_health = 1, health = 1, armor = 0, attack_power = 0, exp = 0, level = 0, gold = 0, lucky = 1;
		std::string display_name, id;
		std::map<std::string, double> custom_attris;
	};
	struct _T_event_condition {
		struct _T_random_chance {
			bool avilable = false;
			float random_chance = 1;
		} random_chance;
		struct _T_attribute {
			short target = 0;	//0=self 1=enemy
			bool avilable = false;
			std::string attri_name;
			double attri_min = -262144.0, attri_max = 262144.0;
		};
		std::vector<_T_attribute> attribute;
		struct _T_items {
			bool avilable = false;
			std::string item_id;
			int itemcnt_min = -262144, itemcnt_max = 262144;
		};
		std::vector<_T_items> has_item;
	};
	struct _T_attri_modifier {
		std::string attri_id;
		short operation = 0;
		float amount = 0;
	};
	struct _T_effect_event {
		_T_attri_modifier modifier;
		bool multy_lvl = false, fixed = false;
		_T_event_condition conditions;
	};
	struct _T_adv_event {
		struct _T_give_effect {
			short target = 0;	//0=self 1=enemy
			std::string effect_name;
			int time = 1, level = 1;
			bool active_immediately = false;
		};
		std::vector<_T_give_effect> give_effect;
		struct _T_mod_attri {
			short target = 0;
			_T_attri_modifier modifier;
		};
		std::vector<_T_mod_attri> modify_attris;
		std::vector<_T_event_condition> conditions;
	};
	struct _T_effect_data {
		std::string id;
		int time = 0, level = 0;
	};
	struct _T_item_data {
		std::string equipment;
		_T_adv_event use_event;
		std::vector<_T_attri_modifier> equip_effects;
	};
	struct _T_loot_entry {
		std::string id;
		int count_min = 0, count_max = 0;
		std::vector<_T_event_condition> conditions;
	};

	std::map<std::string, _T_effect_event> effect_registry;
	std::map<std::string, _T_item_data> item_registry;
	class character {
	public:
		std::map<std::string, int> inventory;
		std::vector<_T_loot_entry> loot_table;
		_T_effect_data effects;
		_T_chrt_data attri;
		struct _T_skill {
			_T_adv_event attack, defence;
		} mob_skill;
		std::map<std::string, _T_adv_event> player_attack_skill;
		std::map<std::string, _T_adv_event> player_defence_skill;
		void CREATEDELL_API_DU setup(std::string id, std::string name, int nhealth, int narmor, int natp, int lvl, int money, int xp) {
			attri.id = id;
			attri.display_name = name;
			attri.health = nhealth;
			attri.max_health = nhealth;
			attri.armor = narmor;
			attri.attack_power = natp;
			attri.level = lvl;
			attri.gold = money;
			attri.exp = xp;
		}
		void CREATEDELL_API_DU attack(character& target, int aq, int dq) {
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.attack");
				sll::replace_substr(transbuf,"%s", sll::get_trans(attri.display_name));
				sll::replace_substr(transbuf, "%s", sll::get_trans(target.attri.display_name));
				std::cout << transbuf << std::endl;
			}
			float apers = 0.2 * aq;
			float dpers = 0.12 * dq;
			if (aq == 6) apers *= 1.25;
			if (dq == 6) dpers *= 1.1;
			int dd = int(attri.attack_power * apers);
			int db = int(target.attri.armor * dpers);
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.roll.aq");
				sll::replace_substr(transbuf, "%s", sll::get_trans(attri.display_name));
				sll::replace_substr(transbuf, "%d", sll::atob<int, std::string>(aq));
				sll::replace_substr(transbuf, "%d", sll::atob<int, std::string>(dd));
				std::cout << transbuf << std::endl;
			}
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.roll.dq");
				sll::replace_substr(transbuf, "%s", sll::get_trans(target.attri.display_name));
				sll::replace_substr(transbuf, "%d", sll::atob<int, std::string>(dq));
				sll::replace_substr(transbuf, "%d", sll::atob<int, std::string>(db));
				std::cout << transbuf << std::endl;
			}
			target.dealt_damage(dd - db);
		}
		bool CREATEDELL_API_DU is_death(void) {
			if (attri.health <= 0) return true;
			else return false;
		}
		void CREATEDELL_API_DU dealt_damage(int damage) {
			if (damage <= 0) return;
			attri.health -= damage;
			if (is_death())
				attri.health = 0;
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.took_damage");
				sll::replace_substr(transbuf, "%s", sll::get_trans(attri.display_name));
				sll::replace_substr(transbuf, "%d", sll::atob<int, std::string>(damage));
				sll::replace_substr(transbuf, "%d", sll::atob<int, std::string>(attri.health));
				sll::replace_substr(transbuf, "%d", sll::atob<int, std::string>(attri.max_health));
				std::cout << transbuf << std::endl;
			}
			if (is_death())
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.death");
				sll::replace_substr(transbuf, "%s", sll::get_trans(attri.display_name));
				std::cout << transbuf << std::endl;
			}
		}
	};

	struct __dungeon_level_data {
		std::vector<character> entries;
		bool looping = false;
		bool random = false;
	};

	std::map<std::string, __dungeon_level_data> dungeon_levels;
}
