﻿#define _CRT_SECURE_NO_WARNINGS
#include "squidCore/squidCore_lib.hpp"
#include "cmdungeonsLib/cmdungeonsLib.hpp"
#include "cmdLib/cmdLib.hpp"
#pragma comment(lib,"squidCore.lib")
#pragma comment(lib,"cmdungeonsLib.lib")
#pragma comment(lib,"cmdLib.lib")
#include<io.h>
#include<tchar.h>
#include<windows.h>
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

int main() {
	//Hard-coded system info translate
	cdl::translate_buffer["cmdungeons.fatal.missing_config"] = "FATAL ERROR: Missing file 'config.ini'. Please re-install the commanDungeons.\nYour game save will be kept.\n";
	cdl::translate_buffer["cmdungeons.fatal.config_damaged"] = "FATAL ERROR: File 'config.ini' can't be read or was damaged. Please try to re-install the commanDungeons.\nYour game save will be kept.\n";
	cdl::translate_buffer["cmdungeons.msg.loading_datapacks"] = "Loading the Data Packs...";
	cdl::translate_buffer["cmdungeons.fatal.no_vanilla_pack"] = "FATAL ERROR: Unable to load vanilla data pack, game can not start up.\nPlease try to re-install the commanDungeons. Your game save will be kept.\n";
	cdl::translate_buffer["cmdungeons.warning.debug_mode"] = "WARNING: DEBUG mode activated!\nThe game will not check the vanilla data pack and the invaild packs will be FORCED ENABLE.\nSome debugging commands will also be enabled, which may crash the game or destroy your game saves if you use them.\nIf you are not a developer or a pack creator, please TURN OFF the debug mode in the 'config.ini' file.\n";


	ResetColor;
	//Read config.ini
	std::ifstream loadcfg("config.ini");
	if (!loadcfg) {
		SetColorFatal;
		std::cout << sll::get_trans("cmdungeons.fatal.missing_config");
		ResetColor;
		system("pause");
		return 0;
	}
	std::string fullcfgfile;

	{
		std::stringstream buf;
		buf << loadcfg.rdbuf();
		fullcfgfile = buf.str();
	}
	loadcfg.close();
	fullcfgfile += "\n";
	{
		std::string keybuf, vlvbuf;
		int state = 0;
		for (std::string::iterator ii = fullcfgfile.begin(); ii != fullcfgfile.end(); ii++) {
			if (state == 0 && *ii != '=')
				keybuf.push_back(*ii);
			else if (state == 0 && *ii == '=')
				state = 1;
			else if (state == 1 && *ii != '\n')
				vlvbuf.push_back(*ii);
			else if (state == 1 && *ii == '\n') {
				state = 0;
				for (int ii = 0; ii < vlvbuf.size(); ii++) {
					if (vlvbuf.substr(ii, 2) == "\\n")
						vlvbuf.replace(ii, 2, "\n");
				}
				cdl::config_keymap[keybuf] = vlvbuf;
				keybuf.clear(); vlvbuf.clear();
			}
		}
	}
	if (cdl::config_keymap.count("lang") == 0) {
		SetColorFatal; std::cout << sll::get_trans("cmdungeons.fatal.config_damaged"); ResetColor;
		system("pause");
		return 0;
	}

	//Read datapacks
	std::cout << sll::get_trans("cmdungeons.msg.loading_datapacks") << std::endl;
	struct _T_Pack {
		std::string pack_name;
		nlohmann::json pack_info;
		std::map<std::string, nlohmann::json> lang_json;
		typedef std::map<std::string, nlohmann::json> subPaths;
		subPaths attributes, effects, items, levels, mobs, player_skills;
	};
	std::map<std::string, _T_Pack> packs;

	std::vector<std::string> filepath_tmp;
	std::vector<std::string> packList;
	getFilesAll(getPath() + "\packs", filepath_tmp);
	for (std::vector<std::string>::iterator ii = filepath_tmp.begin(); ii != filepath_tmp.end(); ii++) {//get all enabled pack names first
		std::string packname_tmp;
		if (ii->find("\\pack_info.meta") != std::string::npos) packname_tmp = *ii;
		sll::replace_substr(packname_tmp, "\\pack_info.meta", "");
		for (int it = packname_tmp.size() - 1; it > 0; it--) {
			if (packname_tmp[it] == '\\') packname_tmp.erase(0, it + 1);
		}
		packList.push_back(packname_tmp);
	}


	if (cdl::config_keymap["debug"] != "true") {
		bool vanillaNotLoaded = true;
		for (std::map<std::string, _T_Pack>::iterator ii = packs.begin(); ii != packs.end(); ii++) {
			if (ii->first == "vanilla") vanillaNotLoaded = false;
		}

		if (vanillaNotLoaded) {
			SetColorFatal; std::cout << sll::get_trans("cmdungeons.fatal.no_vanilla_pack"); ResetColor;
			system("pause");
			return 0;
		}
	}
	else {
		std::fstream test("_DEBUG_MODE_NO_WARNING");
		if (!test) {
			SetColorWarning; std::cout << sll::get_trans("cmdungeons.warning.debug_mode"); ResetColor;
		}
	}




	cmdReg::regist_cmd();
	player.setup("generic:player", "Player", 20, 2, 4, 0, 0, 0);
	SetColorGreat; std::cout << sll::get_trans("cmdungeons.msg.loading.done") << std::endl; ResetColor;
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		if (sll::command.run(input) == EXIT_MAIN)
			return 0;
	}
}