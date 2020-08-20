#pragma once
#ifdef CREATEDELL_API_DU
#else                                                                            
#define CREATEDELL_API_DU _declspec(dllimport)
#endif

namespace cdl {
	class character {
		private:
		  int max_health, health, armor, attack_power, exp, level, mobid;
		  std::string display_name;
		public:
		  CREATEDELL_API_DU character(int id,std::string str_name, int nhealth, int narmor, int nattack_power, int nlevel);
		  void CREATEDELL_API_DU renew_attributes(int nhealth, int narmor, int nattack_power);
		  void CREATEDELL_API_DU rename(std::string str_name);
		  template <class T>
		  T CREATEDELL_API_DU get(std::string attribute);
	};
	
	cdl::character enemy(1,"test1", 10, 0, 6, 0);
	
}