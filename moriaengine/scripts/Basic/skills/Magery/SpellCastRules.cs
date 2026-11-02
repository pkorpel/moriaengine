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

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's @spellcast: what may be cast at what, before a spell starts. Fizzles and quiet refusals both
	/// stop it. The realm and conquest checks of recall and gate wait for those systems.</summary>
	internal static class SpellCastRules {
		private const string NoLight = "s_nightsight";
		private static readonly string[] MysticSelfOnly = { "s_heal", "s_cunning", "s_cure", "s_harm", "s_protection", "s_bless", "s_greater_heal", "s_magic_reflection", "s_dispel" };
		private const int WeakStaff = 3, MiddleStaff = 4, StrongStaff = 5; //material_oak, _teak, _mahagon

		private static bool Is(SpellDef spell, params string[] defnames) {
			return System.Array.Exists(defnames, defname => SpellDef.GetByDefname(defname) == spell);
		}

		/// <summary>f_customspell_delayed's end of a cast: hunger, then f_circle_fizzle, which caps skill at 39.2 over the
		/// difficulty and throws against up to 40.0 over it.</summary>
		internal static bool CustomCastHolds(Character self, SpellDef spell) {
			if (self.IsGM) {
				return true;
			}
			if (PlayerHunger.DistractsFrom(self)) {
				return false;
			}
			int difficulty = spell.Difficulty;
			int skill = System.Math.Min(self.GetSkill(SkillName.Magery), difficulty + 392);
			return skill > Globals.dice.Next(difficulty, difficulty + 401);
		}

		/// <summary>False when it doesn't go on. fizzle says whether that shows as a fizzle.</summary>
		internal static bool Allow(Character self, SpellDef spell, IPoint3D target, out bool fizzle) {
			fizzle = true;
			if (!(self is Player)) {
				return true; //e_allplayers
			}
			MageryRulesLoc texts = Loc<MageryRulesLoc>.Get(self.Language);
			if (spell.IsCustom) { //customspellcast knows nothing of @spellcast
				fizzle = false;
				if (Is(spell, "s_mana_shield") && (target != self)) {
					self.SysMessage(texts.OnlyAtYourself);
					return false;
				}
				return true;
			}
			if (!self.IsGM && !Is(spell, NoLight) && !WorkLight.CheckWithMessage(self)) {
				return false;
			}
			Character targetChar = target as Character;
			Item targetItem = target as Item;
			if (Professions.HasProfession(self, Professions.Mystic) && (target != self) && Is(spell, MysticSelfOnly)) {
				self.RedMessage(texts.OnlyOnYourselfMystic);
				return false;
			}
			if (Is(spell, "s_reactive_armor")) {
				if (Professions.HasProfession(self, Professions.Priest) || Professions.HasProfession(self, Professions.Shaman)) {
					if ((targetChar == null) || !targetChar.IsPlayer) {
						self.RedMessage(texts.OnlyOnPlayers);
						return false;
					}
				} else if (targetChar != self) {
					self.RedMessage(texts.OnlyOnYourself);
					return false;
				}
			}
			if (Is(spell, "s_bless", "s_curse") && (targetItem != null)) {
				self.RedMessage(texts.NotOnItems);
				return false;
			}
			if (Is(spell, "s_teleport") && !self.IsGM) {
				if (targetItem != null) {
					self.RedMessage(texts.NotOntoItems);
					return false;
				}
				if (targetChar is NPC) {
					self.RedMessage(texts.NotOntoNpcs);
					return false;
				}
			}
			if (target is Thing thing) {
				if (thing != self) {
					if ((targetItem != null) ? !CraftingSkillDef.IsOfType(targetItem, "t_rune") : !((targetChar != null) && targetChar.IsGM)) {
						HidingSkillDef.Reveal(self);
					}
				}
				if (Is(spell, "s_fire_field", "s_wall_of_stone", "s_poison_field", "s_paralyzation_field", "s_energy_field") && (thing.TopObj() == self)) {
					self.SysMessage(texts.Cheat);
					fizzle = false;
					return false;
				}
			}
			int staff = StaffNeeded(self, spell);
			if ((staff > 0) && !HasStaff(self, staff)) {
				self.SysMessage(texts.BetterStaff);
				fizzle = false;
				return false;
			}
			if (Is(spell, "s_mark") && (self.M > 0)) {
				fizzle = false;
				return false;
			}
			return true;
		}

		private static int StaffNeeded(Character self, SpellDef spell) {
			if (!Professions.HasProfession(self, Professions.Mage) && !Professions.HasProfession(self, Professions.Necromant)) {
				return 0;
			}
			if (Is(spell, "s_blade_spirits")) {
				return WeakStaff;
			}
			if (Is(spell, "s_energy_vortex", "s_summon_elem_water", "s_summon_elem_earth", "s_summon_elem_fire", "s_summon_elem_air")) {
				return MiddleStaff;
			}
			return Is(spell, "s_summon_daemon") ? StrongStaff : 0;
		}

		private static bool HasStaff(Character self, int material) {
			if (self.IsGM) {
				return true;
			}
			AbstractItem held = self.FindLayer(LayerNames.Hand2);
			Constant staff = (held == null) ? null : Constant.GetByName("staff_" + held.Def.PrettyDefname);
			return (staff != null) && (System.Convert.ToInt32(staff.Value, System.Globalization.CultureInfo.InvariantCulture) >= material);
		}

		/// <summary>Any spell but an arrow or a harm on yourself drops the shield, and half your Int of mana with it.</summary>
		internal static void DropManaShield(Character self, SpellDef spell, IPoint3D target) {
			if (self.HasPlugin(DamageManager.manaShieldKey) && !(Is(spell, "s_magic_arrow", "s_harm") && (target == self))) {
				self.DeletePlugin(DamageManager.manaShieldKey);
				self.Mana = (short) System.Math.Max(self.Mana - (self.Int / 2), 0);
			}
		}
	}

	internal class MageryRulesLoc : CompiledLocStringCollection<MageryRulesLoc> {
		public string OnlyOnPlayers = "Toto kouzlo smis kouzlit jen na hrace.";
		public string OnlyOnYourself = "Toto kouzlo smis kouzlit jen na sebe.";
		public string OnlyAtYourself = "Kouzlo lze zamerit pouze na sebe.";
		public string OnlyOnYourselfMystic = "Toto kouzlo muzes pouzit jen na sebe.";
		public string NotOnItems = "Nesmis kouzlit na predmet.";
		public string NotOntoItems = "Nelze se premistovat na predmety.";
		public string NotOntoNpcs = "Nelze se premistovat na NPC.";
		public string Cheat = "Jsem hnusny buger...";
		public string BetterStaff = "Na toto kouzlo nejspis budes potrebovat lepsi hul.";
		public string LackSkill = "You lack the skill to cast this spell.";
	}
}
