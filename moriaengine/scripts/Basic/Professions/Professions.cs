/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

namespace SteamEngine.CompiledScripts {

	//utility class for scripts dealing with moria professions
	public static class Professions {

		public static bool HasProfession(Character ch, ProfessionDef prof) {
			if (prof != null) {
				Player asPlayer = ch as Player;
				if (asPlayer != null) {
					if (asPlayer.Profession == prof) {
						return true;
					}
				}
			}
			return false;
		}


        static ProfessionDef profession_mage;
        public static ProfessionDef Mage {
            get {
                if (profession_mage == null) {
                    profession_mage = ProfessionDef.GetByDefname("profession_mage");
                }
                return profession_mage;
            }
        }

        static ProfessionDef profession_thief;
        public static ProfessionDef Thief {
            get {
                if (profession_thief == null) {
                    profession_thief = ProfessionDef.GetByDefname("profession_thief");
                }
                return profession_thief;
            }
        }
        
        static ProfessionDef profession_shaman;
        public static ProfessionDef Shaman {
            get {
                if (profession_shaman == null) {
                    profession_shaman = ProfessionDef.GetByDefname("profession_shaman");
                }
                return profession_shaman;
            }
        }

        static ProfessionDef profession_warrior;
        public static ProfessionDef Warrior {
            get {
                if (profession_warrior == null) {
                    profession_warrior = ProfessionDef.GetByDefname("profession_warrior");
                }
                return profession_warrior;
            }
        }

        static ProfessionDef profession_necromant;
        public static ProfessionDef Necromant {
            get {
                if (profession_necromant == null) {
                    profession_necromant = ProfessionDef.GetByDefname("profession_necromant");
                }
                return profession_necromant;
            }
        }

        static ProfessionDef profession_priest;
        public static ProfessionDef Priest {
            get {
                if (profession_priest == null) {
                    profession_priest = ProfessionDef.GetByDefname("profession_priest");
                }
                return profession_priest;
            }
        }

        static ProfessionDef profession_ranger;
        public static ProfessionDef Ranger {
            get {
                if (profession_ranger == null) {
                    profession_ranger = ProfessionDef.GetByDefname("profession_ranger");
                }
                return profession_ranger;
            }
        }

        static ProfessionDef profession_mystic;
        public static ProfessionDef Mystic {
            get {
                if (profession_mystic == null) {
                    profession_mystic = ProfessionDef.GetByDefname("profession_mystic");
                }
                return profession_mystic;
            }
        }
	}
}