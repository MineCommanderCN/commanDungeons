#pragma once
#define CREATEDELL_API_DU _declspec(dllexport)
#include<string>
#include<iostream>
#include<map>
#include<vector>
#include"squidCore_lib.hpp"
namespace cdl {

	struct chrt_data {
		int max_health = 1, health = 1, armor = 0, attack_power = 0, exp = 0, level = 0, mobid = -1, gold = 0;
		std::string display_name;
		bool hasBeenSetup = false;
	};


	class character {
	private:
		chrt_data attri;
	public:
		void CREATEDELL_API_DU setup(int id, std::string name, int nhealth, int narmor, int natp, int lvl, int money, int xp) {
			if (attri.hasBeenSetup) return;
			attri.mobid = id;
			attri.display_name = name;
			attri.health = nhealth;
			attri.max_health = nhealth;
			attri.armor = narmor;
			attri.attack_power = natp;
			attri.level = lvl;
			attri.gold = money;
			attri.exp = xp;
			attri.hasBeenSetup = true;
		}
		void CREATEDELL_API_DU renew_attributes(int nhealth, int narmor, int nattack_power) {
			attri.health = attri.max_health = nhealth;
			attri.armor = narmor;
			attri.attack_power = nattack_power;
		}
		void CREATEDELL_API_DU rename(std::string str_name) {
			attri.display_name = str_name;
		}
		void CREATEDELL_API_DU attack(character& target, int aq, int dq) {
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.attack");
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, sll::get_trans(attri.display_name));
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, sll::get_trans(target.get_attributes().display_name));
				std::cout << transbuf << std::endl;
			}
			float apers = 0.2 * aq;
			float dpers = 0.12 * dq;
			if (aq == 6) apers *= 1.25;
			if (dq == 6) dpers *= 1.1;
			int dd = int(attri.attack_power * apers);
			int db = int(target.get_attributes().armor * dpers);
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.roll.aq");
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, sll::get_trans(attri.display_name));
				if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, sll::atob<int, std::string>(aq));
				if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, sll::atob<int, std::string>(dd));
				std::cout << transbuf << std::endl;
			}
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.roll.dq");
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, sll::get_trans(target.get_attributes().display_name));
				if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, sll::atob<int, std::string>(dq));
				if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, sll::atob<int, std::string>(db));
				std::cout << transbuf << std::endl;
			}
			target.dealt_damage(dd - db);
		}
		chrt_data CREATEDELL_API_DU get_attributes(void) {
			return attri;
		}
		bool CREATEDELL_API_DU is_death(void) {
			if (attri.health <= 0) return true;
			else return false;
		}
		void CREATEDELL_API_DU dealt_damage(int damage) {
			if (damage <= 0) return;
			attri.health = (attri.health - damage);
			if (is_death())
				attri.health = 0;
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.took_damage");
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, sll::get_trans(attri.display_name));
				if (transbuf.find("%d") != std::string::npos) transbuf.replace(transbuf.find("%d"), 2, sll::atob<int, std::string>(damage));
				std::cout << transbuf << std::endl;
			}
			if (is_death())
			{
				std::string transbuf = sll::get_trans("cmdungeons.msg.took_damage");
				if (transbuf.find("%s") != std::string::npos) transbuf.replace(transbuf.find("%s"), 2, sll::get_trans(attri.display_name));
				std::cout << transbuf << std::endl;
			}
		}
		void CREATEDELL_API_DU setid(int id) {
			attri.mobid = id;
		}
	};
	std::vector<chrt_data> enemy_info;
}
