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

using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	public static class PlayerCreation {
		public static readonly Point4D startingPosition = new Point4D(5233, 31, 20, 0);

		private static PlayerDef c_woman;
		public static PlayerDef FemaleDef {
			get {
				if (c_woman == null) {
					c_woman = (PlayerDef) ThingDef.GetByDefname("c_woman");
				}
				return c_woman;
			}
		}

		private static PlayerDef c_man;
		public static PlayerDef MaleDef {
			get {
				if (c_man == null) {
					c_man = (PlayerDef) ThingDef.GetByDefname("c_man");
				}
				return c_man;
			}
		}

		private static WearableDef i_skirt_long;
		public static WearableDef SkirtDef {
			get {
				if (i_skirt_long == null) {
					i_skirt_long = (WearableDef) ThingDef.GetByDefname("i_skirt_long");
				}
				return i_skirt_long;
			}
		}

		private static WearableDef i_pants_long;
		public static WearableDef PantsDef {
			get {
				if (i_pants_long == null) {
					i_pants_long = (WearableDef) ThingDef.GetByDefname("i_pants_long");
				}
				return i_pants_long;
			}
		}

		private static WearableDef i_shirt_plain;
		public static WearableDef ShirtDef {
			get {
				if (i_shirt_plain == null) {
					i_shirt_plain = (WearableDef) ThingDef.GetByDefname("i_shirt_plain");
				}
				return i_shirt_plain;
			}
		}

		[SteamFunction("f_createPlayerCharacter")]
		public static Character CreatePlayerCharacter(Globals globals, CreateCharacterInPacket.CreateCharArguments argo) {
			//we are supposed to create a character and return it

			Character ch;
			Thing pants;

			if (argo.IsFemale) {
				ch = (Character) FemaleDef.Create(startingPosition);
				pants = ch.NewEquip(SkirtDef);
			} else {
				ch = (Character) MaleDef.Create(startingPosition);
				pants = ch.NewEquip(PantsDef);
			}

			ch.Name = argo.Charname;
			//ch.gender = argo.gender
			ch.Color = argo.SkinColor;

			//make hair and beard
			if (argo.HairStyle > 0) {
				Thing hair = ch.NewEquip(ThingDef.FindItemDef(argo.HairStyle));
				hair.Color = argo.HairColor;
			}
			if ((argo.FacialHair > 0) && (!argo.IsFemale)) {
				Thing facial = ch.NewEquip(ThingDef.FindItemDef(argo.FacialHair));
				facial.Color = argo.FacialHairColor;
			}

			//make shirt and pants
			Thing shirt = ch.NewEquip(ShirtDef);
			shirt.Color = argo.ShirtColor;

			pants.Color = argo.PantsColor;

			if (argo.StartStr < 10) {
				ch.Str = 10;
			}
			if (argo.StartDex < 10) {
				ch.Dex = 10;
			}
			if (argo.StartInt < 10) {
				ch.Int = 10;
			}

			if (argo.StartStr > 60) {
				ch.Str = 60;
			}
			if (argo.StartDex > 60) {
				ch.Dex = 60;
			}
			if (argo.StartInt > 60) {
				ch.Int = 60;
			}

			//TODO? check the skill values
			ch.SetRealSkillValue(argo.SkillId1, argo.SkillValue1);
			ch.SetRealSkillValue(argo.SkillId2, argo.SkillValue2);
			ch.SetRealSkillValue(argo.SkillId3, argo.SkillValue3);

			//add whatever you wish here :)



			return ch;
		}
	}
}