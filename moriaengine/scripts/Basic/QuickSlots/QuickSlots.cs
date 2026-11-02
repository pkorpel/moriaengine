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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's equip macros (questy/enrico.scp, healing.scp): sixteen slots, each remembering an item or a
	/// character. .setequipN picks, .equipN uses what's there, .healN bandages who's there, .samhealN with a shaman's
	/// bloody bandage.</summary>
	public static class QuickSlots {
		private const string Bandage = "i_bandage", BloodyBandage = "i_bandage_bloody";
		private static readonly TagKey[] slotTKs = CreateKeys();

		private static TagKey[] CreateKeys() {
			TagKey[] keys = new TagKey[16];
			for (int i = 0; i < keys.Length; i++) {
				keys[i] = TagKey.Acquire("equip" + i);
			}
			return keys;
		}

		private static void Remember(Character self, int slot) {
			if (self is Player player) {
				player.SysMessage(Loc<QuickSlotsLoc>.Get(player.Language).Aim);
				player.Target(SingletonScript<Targ_QuickSlot>.Instance, slot);
			}
		}

		internal static void Store(Character self, int slot, Thing what) {
			self.SetTag(slotTKs[slot], what);
			QuickSlotsLoc texts = Loc<QuickSlotsLoc>.Get(self.Language);
			self.SysMessage((what is Character) ? texts.CharacterStored : texts.ItemStored, Bandaging.ClassMessageColor);
		}

		private static void Use(Character self, int slot) {
			QuickSlotsLoc texts = Loc<QuickSlotsLoc>.Get(self.Language);
			Thing what = self.GetTag(slotTKs[slot]) as Thing;
			if (what == null) {
				self.RedMessage(texts.NothingStored);
			} else if ((what is Item item) && CraftingSkillDef.IsOfType(item, "t_housemenu", "t_vendormenu", "t_fieldmenu")) {
				item.Trigger_DClick(self);
			} else if ((what.TopObj() == self) && self.CanReach(what).Allow) {
				what.Trigger_DClick(self);
			} else {
				self.RedMessage(texts.MustCarry);
			}
		}

		//f_heal_value_base
		private static void Heal(Character self, int slot, bool bloody) {
			QuickSlotsLoc texts = Loc<QuickSlotsLoc>.Get(self.Language);
			if (bloody && !Professions.HasProfession(self, Professions.Shaman)) {
				self.RedMessage(texts.OnlyShamans);
			} else if (self.Flag_Dead) {
				self.RedMessage(texts.GhostsDontHeal);
			} else if (ParalyzeEffectPluginDef.IsParalyzed(self)) {
				self.RedMessage(texts.Paralyzed);
			} else {
				Thing what = self.GetTag(slotTKs[slot]) as Thing;
				if (what == null) {
					self.RedMessage(string.Format(texts.SlotEmpty, slot));
				} else if (!(what is Character)) {
					self.RedMessage(string.Format(texts.SlotIsAnItem, slot));
				} else {
					ScriptedSkills.Stop(self);
					SkillSequenceArgs.AbortSkill(self);
					Bandaging.OnCharacter(self, (Character) what, bloody);
				}
			}
		}

		[SteamFunction]
		public static void SetEquip0(Character self) { Remember(self, 0); }
		[SteamFunction]
		public static void Equip0(Character self) { Use(self, 0); }
		[SteamFunction]
		public static void Heal0(Character self) { Heal(self, 0, false); }
		[SteamFunction]
		public static void SamHeal0(Character self) { Heal(self, 0, true); }
		[SteamFunction]
		public static void SetEquip1(Character self) { Remember(self, 1); }
		[SteamFunction]
		public static void Equip1(Character self) { Use(self, 1); }
		[SteamFunction]
		public static void Heal1(Character self) { Heal(self, 1, false); }
		[SteamFunction]
		public static void SamHeal1(Character self) { Heal(self, 1, true); }
		[SteamFunction]
		public static void SetEquip2(Character self) { Remember(self, 2); }
		[SteamFunction]
		public static void Equip2(Character self) { Use(self, 2); }
		[SteamFunction]
		public static void Heal2(Character self) { Heal(self, 2, false); }
		[SteamFunction]
		public static void SamHeal2(Character self) { Heal(self, 2, true); }
		[SteamFunction]
		public static void SetEquip3(Character self) { Remember(self, 3); }
		[SteamFunction]
		public static void Equip3(Character self) { Use(self, 3); }
		[SteamFunction]
		public static void Heal3(Character self) { Heal(self, 3, false); }
		[SteamFunction]
		public static void SamHeal3(Character self) { Heal(self, 3, true); }
		[SteamFunction]
		public static void SetEquip4(Character self) { Remember(self, 4); }
		[SteamFunction]
		public static void Equip4(Character self) { Use(self, 4); }
		[SteamFunction]
		public static void Heal4(Character self) { Heal(self, 4, false); }
		[SteamFunction]
		public static void SamHeal4(Character self) { Heal(self, 4, true); }
		[SteamFunction]
		public static void SetEquip5(Character self) { Remember(self, 5); }
		[SteamFunction]
		public static void Equip5(Character self) { Use(self, 5); }
		[SteamFunction]
		public static void Heal5(Character self) { Heal(self, 5, false); }
		[SteamFunction]
		public static void SamHeal5(Character self) { Heal(self, 5, true); }
		[SteamFunction]
		public static void SetEquip6(Character self) { Remember(self, 6); }
		[SteamFunction]
		public static void Equip6(Character self) { Use(self, 6); }
		[SteamFunction]
		public static void Heal6(Character self) { Heal(self, 6, false); }
		[SteamFunction]
		public static void SamHeal6(Character self) { Heal(self, 6, true); }
		[SteamFunction]
		public static void SetEquip7(Character self) { Remember(self, 7); }
		[SteamFunction]
		public static void Equip7(Character self) { Use(self, 7); }
		[SteamFunction]
		public static void Heal7(Character self) { Heal(self, 7, false); }
		[SteamFunction]
		public static void SamHeal7(Character self) { Heal(self, 7, true); }
		[SteamFunction]
		public static void SetEquip8(Character self) { Remember(self, 8); }
		[SteamFunction]
		public static void Equip8(Character self) { Use(self, 8); }
		[SteamFunction]
		public static void Heal8(Character self) { Heal(self, 8, false); }
		[SteamFunction]
		public static void SamHeal8(Character self) { Heal(self, 8, true); }
		[SteamFunction]
		public static void SetEquip9(Character self) { Remember(self, 9); }
		[SteamFunction]
		public static void Equip9(Character self) { Use(self, 9); }
		[SteamFunction]
		public static void Heal9(Character self) { Heal(self, 9, false); }
		[SteamFunction]
		public static void SamHeal9(Character self) { Heal(self, 9, true); }
		[SteamFunction]
		public static void SetEquip10(Character self) { Remember(self, 10); }
		[SteamFunction]
		public static void Equip10(Character self) { Use(self, 10); }
		[SteamFunction]
		public static void Heal10(Character self) { Heal(self, 10, false); }
		[SteamFunction]
		public static void SamHeal10(Character self) { Heal(self, 10, true); }
		[SteamFunction]
		public static void SetEquip11(Character self) { Remember(self, 11); }
		[SteamFunction]
		public static void Equip11(Character self) { Use(self, 11); }
		[SteamFunction]
		public static void Heal11(Character self) { Heal(self, 11, false); }
		[SteamFunction]
		public static void SamHeal11(Character self) { Heal(self, 11, true); }
		[SteamFunction]
		public static void SetEquip12(Character self) { Remember(self, 12); }
		[SteamFunction]
		public static void Equip12(Character self) { Use(self, 12); }
		[SteamFunction]
		public static void Heal12(Character self) { Heal(self, 12, false); }
		[SteamFunction]
		public static void SamHeal12(Character self) { Heal(self, 12, true); }
		[SteamFunction]
		public static void SetEquip13(Character self) { Remember(self, 13); }
		[SteamFunction]
		public static void Equip13(Character self) { Use(self, 13); }
		[SteamFunction]
		public static void Heal13(Character self) { Heal(self, 13, false); }
		[SteamFunction]
		public static void SamHeal13(Character self) { Heal(self, 13, true); }
		[SteamFunction]
		public static void SetEquip14(Character self) { Remember(self, 14); }
		[SteamFunction]
		public static void Equip14(Character self) { Use(self, 14); }
		[SteamFunction]
		public static void Heal14(Character self) { Heal(self, 14, false); }
		[SteamFunction]
		public static void SamHeal14(Character self) { Heal(self, 14, true); }
		[SteamFunction]
		public static void SetEquip15(Character self) { Remember(self, 15); }
		[SteamFunction]
		public static void Equip15(Character self) { Use(self, 15); }
		[SteamFunction]
		public static void Heal15(Character self) { Heal(self, 15, false); }
		[SteamFunction]
		public static void SamHeal15(Character self) { Heal(self, 15, true); }
	}

	public class Targ_QuickSlot : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			QuickSlots.Store(self, (int) parameter, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			QuickSlots.Store(self, (int) parameter, targetted);
			return TargetResult.Done;
		}
	}

	internal class QuickSlotsLoc : CompiledLocStringCollection<QuickSlotsLoc> {
		public string Aim = "Zamer vec nebo tvora.";
		public string CharacterStored = "Charakter byl uspesne zaznamenan.";
		public string ItemStored = "Predmet byl uspesne zaznamenan.";
		public string NothingStored = "Neni zaznamenan zadny predmet.";
		public string MustCarry = "Predmet musis mit u sebe nebo v otevrene bance.";
		public string OnlyShamans = "Pouze saman vi jak vyuzit krvave bandaze k leceni";
		public string GhostsDontHeal = "Jako duch nic nevylecis.";
		public string Paralyzed = "V paralyze nemuzes lecit.";
		public string SlotEmpty = "Nemas nastaveny equip{0}.";
		public string SlotIsAnItem = "Cil ulozeny jako equip{0} je predmet!";
	}
}
