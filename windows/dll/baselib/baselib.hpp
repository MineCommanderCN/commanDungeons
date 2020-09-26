#pragma once
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
#define ResetColor SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE)
#define SetColorWarning SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_INTENSITY | FOREGROUND_RED |FOREGROUND_GREEN)
#define SetColorFatal SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), BACKGROUND_RED | FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE)
#define SetColorError SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_RED)
#define SetColorGreat SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_GREEN)
#define SetColorExellent SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), FOREGROUND_GREEN | FOREGROUND_BLUE)

namespace cdl {
    nlohmann::json translate_buffer;
    std::map<std::string, std::string> config_keymap;
}
namespace base {
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
