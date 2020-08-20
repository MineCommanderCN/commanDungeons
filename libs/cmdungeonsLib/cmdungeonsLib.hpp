#pragma once
#ifdef CREATEDELL_API_DU
#else                                                                            
#define CREATEDELL_API_DU _declspec(dllimport)
#endif



namespace cdl {
	struct chrt_data {
		int max_health, health, armor, attack_power, exp, level, mobid, gold;
		std::string display_name;
	};
	class character {
		private:
			chrt_data attri;
		public:
			CREATEDELL_API_DU character(int id, std::string str_name, int nhealth, int narmor, int nattack_power, int nlevel, int money);
		  void CREATEDELL_API_DU renew_attributes(int nhealth, int narmor, int nattack_power);
		  void CREATEDELL_API_DU rename(std::string str_name);
		  void CREATEDELL_API_DU attack(character target,int aq,int dq);
		  chrt_data CREATEDELL_API_DU get_attributes(void);
		  bool CREATEDELL_API_DU is_death(void);
		  int CREATEDELL_API_DU dealt_damage(int damage);
	};
	cdl::character enemy(1, "test1", 1000, 0, 6, 0, 5);
	cdl::character player(0, "You", 1000, 0, 6, 0, 0);
}