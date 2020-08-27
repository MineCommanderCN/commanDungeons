#include "squidCore_lib.hpp"
#include "cmdungeonsLib.hpp"
#include "cmdLib.hpp"
#include "nlohmannJson.hpp"
#include<io.h>
#include<tchar.h>
#include<windows.h>
void getFilesAll(std::string path, std::vector<std::string>& files) {
	//文件句柄
	long hFile = 0;
	//文件信息
	struct _finddata_t fileinfo;
	std::string p;
	if ((hFile = _findfirst(p.assign(path).append("\\*").c_str(), &fileinfo)) != -1) {
		do {
			if ((fileinfo.attrib & _A_SUBDIR)) {
				if (strcmp(fileinfo.name, ".") != 0 && strcmp(fileinfo.name, "..") != 0) {
					//files.push_back(p.assign(path).append("\\").append(fileinfo.name));
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
std::string WstringToString(const std::wstring str)
{
	unsigned len = str.size() * 4;
	setlocale(LC_CTYPE, "");
	char* p = new char[len];
	wcstombs(p, str.c_str(), len);
	std::string str1(p);
	delete[] p;
	return str1;
}
std::string getPath(void) {
	TCHAR szFilePath[MAX_PATH + 1] = { 0 };
	GetModuleFileName(NULL, szFilePath, MAX_PATH);
	(_tcsrchr(szFilePath, _T('\\')))[1] = 0;
	std::wstring str_url = szFilePath;
	return WstringToString(str_url);
}

bool valid_datastr(std::string str) {
	if (str.empty()) return false;
	for (std::string::iterator ii = str.begin(); ii != str.end(); ii++) {
		if ((*ii < 'a' || *ii>'z') && (*ii < '0' || *ii>'9') && *ii != '_')
			return false;
	}
	return true;
}

int main() {
	std::cout << "Loading Config..." << std::endl;
	std::ifstream loadcfg("config.ini");
	if (!loadcfg) {
		std::cout << "FATAL ERROR: Missing file 'config.ini'. Please re-install the commanDungeons.\nYour game save will be kept.\n";
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
	if (cdl::config_keymap.count("lang")==0) {
		std::cout << "FATAL ERROR: File 'config.ini' can't be read or was damaged. Please try to re-install the commanDungeons.\nYour game save will be kept.\n";
		system("pause");
		return 0;
	}


	std::cout << "Loading the Data Packs..." << std::endl;
	struct _T_transPack {
		std::string pack_name;
		nlohmann::json pack_info;
		nlohmann::json lang_json;
	};
	std::vector<_T_transPack> translates;
	std::vector<std::string> packPaths;
	getFilesAll(getPath()+"packs\\translate", packPaths);

	for (std::vector<std::string>::iterator ii = packPaths.begin(); ii != packPaths.end(); ii++) {
		sll::replace_substr(*ii, getPath() + "packs\\translate\\", "");
		if (ii->find("pack_info.meta") != std::string::npos) {
			_T_transPack buffer;
			sll::replace_substr(*ii, "\\pack_info.meta", "");
			buffer.pack_name = *ii;
			std::cout << "Loaded pack [" << *ii << "](translate)" << std::endl;
			std::ifstream readInfo(("packs/translate/" + *ii + "/pack_info.meta").c_str());
			readInfo >> buffer.pack_info;
			if (!buffer.pack_info["file_format"].empty() && buffer.pack_info["file_format"] != 1) 
				std::cout << "(!) This pack was designed for the legacy version of the game\n";
			if (buffer.pack_info["description"].empty()
				|| buffer.pack_info["file_format"].empty()
				|| buffer.pack_info["creator"].empty()
				|| buffer.pack_info["pack_version"].empty()) {
				std::cout << "(x) Invalid pack\n";
			}
			else 
				translates.push_back(buffer);
		}
	}
	for (std::vector<std::string>::iterator ii = packPaths.begin(); ii != packPaths.end(); ii++) {

		for (std::vector<_T_transPack>::iterator ia = translates.begin(); ia != translates.end(); ia++) {
			if (ii->find(ia->pack_name) == 0 && ii->find("\\files\\") == ia->pack_name.size()) {
				std::ifstream readLang(("packs/translate/" + *ii).c_str());
				sll::replace_substr(*ii, ia->pack_name + "\\files\\", "");
				sll::replace_substr(*ii, ".json", "");
				if (*ii == cdl::config_keymap["lang"]) readLang >> ia->lang_json;
				cdl::translate_json.merge_patch(ia->lang_json);
			}
		}
	}
	struct _T_dataPack {
		std::string pack_name;
		nlohmann::json pack_info;
		typedef std::map<std::string, nlohmann::json> subPaths;
		subPaths attributes, effects, items, levels, mobs, player_skills;
	};
	std::vector<_T_dataPack> datapacks;
	packPaths.clear();
	getFilesAll(getPath() + "packs\\data", packPaths);
	for (std::vector<std::string>::iterator ii = packPaths.begin(); ii != packPaths.end(); ii++) {
		sll::replace_substr(*ii, getPath() + "packs\\data\\", "");
		if (ii->find("pack_info.meta") != std::string::npos) {
			_T_dataPack buffer;
			sll::replace_substr(*ii, "\\pack_info.meta", "");
			buffer.pack_name = *ii;
			std::cout << "Loaded pack [" << *ii << "](data)" << std::endl;
			std::ifstream readInfo(("packs/data/" + *ii + "/pack_info.meta").c_str());
			readInfo >> buffer.pack_info;
			if (!buffer.pack_info["file_format"].empty() && buffer.pack_info["file_format"] != 1)
				std::cout << "(!) This pack was designed for the legacy version of the game\n";
			if (buffer.pack_info["description"].empty()
				|| buffer.pack_info["file_format"].empty()
				|| buffer.pack_info["creator"].empty()
				|| buffer.pack_info["pack_version"].empty()
				|| !valid_datastr(buffer.pack_info["namespace"])) {
				std::cout << "(x) Invalid pack\n";
			}
			else 
				datapacks.push_back(buffer);
		}
	}

	if (cdl::config_keymap["debug"] != "true") {
		bool vanillaNotLoaded_trans = false, vanillaNotLoaded_data = false;
		for (std::vector<_T_transPack>::iterator ii = translates.begin(); ii != translates.end(); ii++) {
			if (ii->pack_name == "vanilla") vanillaNotLoaded_trans = true;
		}
		for (std::vector<_T_dataPack>::iterator ii = datapacks.begin(); ii != datapacks.end(); ii++) {
			if (ii->pack_name == "vanilla") vanillaNotLoaded_data = true;
		}

		if (!vanillaNotLoaded_trans || !vanillaNotLoaded_data) {
			std::cout << "FATAL ERROR: Unable to load vanilla data pack, game can not start up.\nPlease try to re-install the commanDungeons. Your game save will be kept.\n";
			system("pause");
			return 0;
		}
	}
	else {
		std::cout << "WARNING: DEBUG mode activated!\nThe game will not check the vanilla data pack and the invaild packs will be FORCED ENABLE.\nSome debugging commands will also be enabled, which may crash the game or destroy your game saves if you use them.\nIf you are not a developer or a pack creator, please TURN OFF the debug mode in the 'config.ini' file!\n";
	}
	cmdReg::regist_cmd();
	player.setup("generic:player", "Player", 20, 2, 4, 0, 0,0);
	std::cout << sll::get_trans("cmdungeons.msg.loading.done") << std::endl;
	while (1) {
		std::string input;
		std::cout << ">> ";
		std::getline(std::cin, input);
		if (sll::command.run(input) == EXIT_MAIN)
			return 0;
	}
}