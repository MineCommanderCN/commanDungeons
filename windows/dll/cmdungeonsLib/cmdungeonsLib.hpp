#pragma once
#pragma warning(disable:4996)
#define CREATEDELL_API_DU _declspec(dllexport)
#include<iostream>
#include<vector>
#include<map>
#include<ctime>
#include<fstream>
#include<string>
#include<sstream>
#include<windows.h>
#include<io.h>
#include<tchar.h>
#include "nlohmannJson.hpp"
//Console color font (on Windows)
#define ResetColor SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE)
#define SetColorWarning SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_INTENSITY | FOREGROUND_RED |FOREGROUND_GREEN)
#define SetColorFatal SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), BACKGROUND_RED | FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE)
#define SetColorError SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_RED)
#define SetColorGreat SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_GREEN)
#define SetColorExellent SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_GREEN | FOREGROUND_BLUE)
namespace cdl {
	nlohmann::json translate_buffer;
	std::map<std::string, std::string> config_keymap;
	std::string CREATEDELL_API_DU get_trans(std::string key) {
		return (cdl::translate_buffer.count(key) == 1) ? cdl::translate_buffer[key] : "";
	}
	bool CREATEDELL_API_DU replace_substr(std::string& raw, std::string from, std::string to) {
		if (raw.find(from) != std::string::npos) {
			raw.replace(raw.find(from), from.size(), to);
			return 0;
		}
		else return 1;
	}
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
	template <class Ta, class Tb>
	Tb atob(const Ta& t) {
		std::stringstream temp;
		temp << t;
		Tb i;
		temp >> i;
		return i;
	}
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
				std::string transbuf = cdl::get_trans("cmdungeons.msg.attack");
				cdl::replace_substr(transbuf,"%s", cdl::get_trans(attri.display_name));
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(target.attri.display_name));
				std::cout << transbuf << std::endl;
			}
			float apers = 0.2 * aq;
			float dpers = 0.12 * dq;
			if (aq == 6) apers *= 1.25;
			if (dq == 6) dpers *= 1.1;
			int dd = int(attri.attack_power * apers);
			int db = int(target.attri.armor * dpers);
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.roll.aq");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(attri.display_name));
				cdl::replace_substr(transbuf, "%d", cdl::atob<int, std::string>(aq));
				cdl::replace_substr(transbuf, "%d", cdl::atob<int, std::string>(dd));
				std::cout << transbuf << std::endl;
			}
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.roll.dq");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(target.attri.display_name));
				cdl::replace_substr(transbuf, "%d", cdl::atob<int, std::string>(dq));
				cdl::replace_substr(transbuf, "%d", cdl::atob<int, std::string>(db));
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
				std::string transbuf = cdl::get_trans("cmdungeons.msg.took_damage");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(attri.display_name));
				cdl::replace_substr(transbuf, "%d", cdl::atob<int, std::string>(damage));
				cdl::replace_substr(transbuf, "%d", cdl::atob<int, std::string>(attri.health));
				cdl::replace_substr(transbuf, "%d", cdl::atob<int, std::string>(attri.max_health));
				std::cout << transbuf << std::endl;
			}
			if (is_death())
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.death");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(attri.display_name));
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
	std::string CREATEDELL_API_DU utf8_to_ansi(std::string strUTF8) {	//方法来源：https://blog.csdn.net/yuanwow/article/details/98469297
		UINT nLen = MultiByteToWideChar(CP_UTF8, NULL, strUTF8.c_str(), -1, NULL, NULL);
		WCHAR* wszBuffer = new WCHAR[nLen + 1];
		nLen = MultiByteToWideChar(CP_UTF8, NULL, strUTF8.c_str(), -1, wszBuffer, nLen);
		wszBuffer[nLen] = 0;
		nLen = WideCharToMultiByte(936, NULL, wszBuffer, -1, NULL, NULL, NULL, NULL);
		CHAR* szBuffer = new CHAR[nLen + 1];
		nLen = WideCharToMultiByte(936, NULL, wszBuffer, -1, szBuffer, nLen, NULL, NULL);
		szBuffer[nLen] = 0;
		strUTF8 = szBuffer;
		delete[]szBuffer;
		delete[]wszBuffer;
		return strUTF8;
	}
	
	
	const int MAX_NUM = 2147483647;
	void getFilesAll(std::string path, std::vector<std::string>& files) {	//Gets all file names in the given path (include its sub path)
		long hFile = 0;
		struct _finddata_t fileinfo;
		std::string p;
		if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1) {
			do {
				if ((fileinfo.attrib & _A_SUBDIR)) {
					if (strcmp(fileinfo.name, ".") != 0 && strcmp(fileinfo.name, "..") != 0) {
						getFilesAll(p.assign(path).append("\\").append(fileinfo.name), files);
					}
				}
				else {
					files.push_back(p.assign(path).append("\\").append(fileinfo.name));
				}
			} while (_findnext(hFile, &fileinfo) == 0);
			_findclose(hFile);
		}
	}
	std::string WstringToString(const std::wstring str)	//just wstring to string
	{
		unsigned len = str.size() * 4;
		setlocale(LC_CTYPE, "");
		char* p = new char[len];
		wcstombs(p, str.c_str(), len);
		std::string str1(p);
		delete[] p;
		return str1;
	}
	std::string getPath(void) {	//The path that program running in
		TCHAR szFilePath[MAX_PATH + 1] = { 0 };
		GetModuleFileName(NULL, szFilePath, MAX_PATH);
		(_tcsrchr(szFilePath, _T('\\')))[1] = 0;
		std::wstring str_url = szFilePath;
		return WstringToString(str_url);
	}
	bool valid_datastr(std::string str) {	//Is it a valid name for a item/effect/attribute/enemy/level...?
		if (str.empty()) return false;
		for (std::string::iterator ii = str.begin(); ii != str.end(); ii++) {
			if ((*ii < 'a' || *ii>'z') && (*ii < '0' || *ii>'9') && *ii != '_')
				return false;
		}
		return true;
	}
}
