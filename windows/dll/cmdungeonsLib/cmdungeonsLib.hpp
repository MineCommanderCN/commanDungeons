#pragma once
#pragma warning(disable:4996)
#define CREATEDELL_API_DU _declspec(dllexport)

#if defined (_DEBUG)
#pragma comment( lib, "lua5.4.lib" ) // Lua Support
#else
#pragma comment( lib, "lua5.4.lib" ) // Lua Support
#endif

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
#include<iomanip>
#include "nlohmannJson.hpp"
//Console color font (on Windows)
#define ResetColor SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE)
#define SetColorWarning SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_INTENSITY | FOREGROUND_RED |FOREGROUND_GREEN)
#define SetColorFatal SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), BACKGROUND_RED | FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE)
#define SetColorError SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_RED)
#define SetColorGreat SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_GREEN)
#define SetColorExellent SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_GREEN | FOREGROUND_BLUE)
#include "lua/lua.hpp"
lua_State* lua_state;
namespace cdl {
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
	nlohmann::json translate_buffer;
	std::map<std::string, std::string> config_keymap;
	std::string CREATEDELL_API_DU get_trans(std::string key) {
		return (cdl::translate_buffer.count(key) == 1) ? utf8_to_ansi(cdl::translate_buffer[key]) : key;
	}
	bool CREATEDELL_API_DU replace_substr(std::string& raw, std::string from, std::string to) {
		if (raw.find(from) != std::string::npos) {
			raw.replace(raw.find(from), from.size(), to);
			return 0;
		}
		else return 1;
	}
	struct _T_attribute_modifier {
		double amount = 0.0;
		short operation = 0;
		std::string attribute_name;
	};
	struct _T_event_condition {
		std::string type;
		double _min, _max;
	};
	struct _T_effect_reg {
		bool debuf = false;
		std::string event_type;
		_T_attribute_modifier attribute_modifier;	//modify_attributes
		double amount;	//modify_health
		double level_multiplier = 1.0;	//modify_attributes, modify_health
	};
	struct _T_event {
		std::string type;
		short target = 0;	//0 -> self, 1 -> enemy
		std::string effect_name;	//give_effect
		long long time;	//give_effect
		long long level;	//give_effect
		double amount;	//modify_health
		bool active_immediately = true;	//give_effect
	};
	struct _T_skill {

	};

	struct _T_effect_data {
		long long time = 0, level = 0;
	};
	struct _T_item_data {
		std::string equipment;
		std::string use_event;
		std::vector<_T_attribute_modifier> equip_effects;
	};
	struct _T_loot_entry {
		std::string id;
		long long count_min = 0, count_max = 0;
		std::vector<_T_event_condition> conditions;
	};

	std::map<std::string, _T_effect_reg> effect_registry;
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
		std::map<std::string, long long> inventory;
		std::vector<_T_loot_entry> loot_table;
		std::map<std::string, _T_effect_data> effects;
		long long health = 1, exp = 0, level = 0, gold = 0;
		std::string display_name, id;
		std::map<std::string, double> attributeBases;
		struct _T_character_skills {
			_T_skill *attack, *defence;
		} skills;

		CREATEDELL_API_DU character(
			std::string _id,
			std::string _display_name,
			long long _health,
			long long _atk_power,
			long long _armor,
			long long _xp,
			long long _gold,
			long long _level) {
			
			attributeBases["generic:max_health"] = _health;
			health = _health;
			attributeBases["generic:attack_power"] = _atk_power;
			attributeBases["generic:armor"] = _armor;
			attributeBases["generic:luck"] = 1;
			display_name = _display_name;
			id = _id;
			skills.attack = NULL;
			skills.defence = NULL;
		}
		CREATEDELL_API_DU character(void) {
			attributeBases["generic:max_health"] = 1;
			health = 1;
			attributeBases["generic:attack_power"] = 0;
			attributeBases["generic:armor"] = 0;
			attributeBases["generic:luck"] = 1;
			display_name = "";
			id = "null";
			skills.attack = NULL;
			skills.defence = NULL;
		}

		double CREATEDELL_API_DU getAttribute(std::string attributeName) {
			double buf = attributeBases[attributeName];
			if (effects.count(attributeName) == 1) {
				if (effect_registry[attributeName].attribute_modifier.attribute_name == attributeName) {
					switch (effect_registry[attributeName].attribute_modifier.operation) {
					case 0:

					}
				}
			}
			
			
		}


		void CREATEDELL_API_DU attack(character& target, int aq, int dq) {
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.attack");
				cdl::replace_substr(transbuf,"%s", cdl::get_trans(display_name));
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(target.display_name));
				std::cout << transbuf << std::endl;
			}
			double apers = 0.2 * aq;
			double dpers = 0.12 * dq;
			if (aq == 6) apers *= 1.25;
			if (dq == 6) dpers *= 1.1;
			long long dd = long long(attribute("generic:attack_power") * apers);
			long long db = long long(target.attribute("generic:armor") * dpers);
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.roll.aq");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(display_name));
				cdl::replace_substr(transbuf, "%d", cdl::atob<long long, std::string>(aq));
				cdl::replace_substr(transbuf, "%d", cdl::atob<long long, std::string>(dd));
				std::cout << transbuf << std::endl;
			}
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.roll.dq");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(target.display_name));
				cdl::replace_substr(transbuf, "%d", cdl::atob<long long, std::string>(dq));
				cdl::replace_substr(transbuf, "%d", cdl::atob<long long, std::string>(db));
				std::cout << transbuf << std::endl;
			}
			target.dealt_damage(dd - db);
		}
		bool CREATEDELL_API_DU is_death(void) {
			if (health <= 0) return true;
			else return false;
		}
		void CREATEDELL_API_DU dealt_damage(long long damage) {
			if (damage <= 0) return;
			health -= damage;
			if (is_death())
				attri.health = 0;
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.took_damage");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(display_name));
				cdl::replace_substr(transbuf, "%d", cdl::atob<long long, std::string>(damage));
				cdl::replace_substr(transbuf, "%d", cdl::atob<long long, std::string>(health));
				cdl::replace_substr(transbuf, "%d", cdl::atob<long long, std::string>(attribute(max_health)));
				std::cout << transbuf << std::endl;
			}
			if (is_death())
			{
				std::string transbuf = cdl::get_trans("cmdungeons.msg.death");
				cdl::replace_substr(transbuf, "%s", cdl::get_trans(display_name));
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


	const int MAX_NUM = 2147483647;
	void getSubFiles(std::string path, std::vector<std::string>& files) {	//Gets all file names in the given path
		long   hFile = 0;
		struct _finddata_t fileinfo;
		std::string p;
		if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1)
		{
			do
			{
				if ((fileinfo.attrib & _A_SUBDIR))
				{

				}
				else
				{
					files.push_back(p.assign(fileinfo.name));
				}
			} while (_findnext(hFile, &fileinfo) == 0);
			_findclose(hFile);
		}
	}
	void getSubDir(std::string path, std::vector<std::string>& files) {	//Gets all sub forlder in a given path
		long hFile = 0;
		struct _finddata_t fileinfo;
		std::string p;
		if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1) {
			do {
				if ((fileinfo.attrib & _A_SUBDIR)) {
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
			if ((*ii < 'A' || *ii > 'Z') && (*ii < 'a' || *ii > 'z') && (*ii < '0' || *ii > '9') && *ii != '_')
				return false;
		}
		return true;
	}
}