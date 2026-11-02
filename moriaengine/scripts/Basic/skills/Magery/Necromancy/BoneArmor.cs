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
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's necroarmor and i_bone_armor_timer, its own spell in place of Sphere's bones on a corpse: a
	/// second's wait, then Protection at full power and bone pieces on the free layers for {180 320} s.</summary>
	public static class BoneArmor {
		private const int SpellId = 67, BookMana = 10, ScrollMana = 5, LeastSkill = 400;
		private const int FailLow = 300, FailHigh = 700, LeastSeconds = 180, MostSeconds = 320;
		private static readonly TimerKey castKey = TimerKey.Acquire("_boneArmorCast_");
		private static readonly TimerKey wornKey = TimerKey.Acquire("_boneArmorWorn_");
		private static readonly string[] pieces = { "i_necro_arms", "i_necro_chest", "i_necro_gloves", "i_necro_helmet", "i_necro_leggings" };
		private static readonly int[] layers = { 19, 13, 7, 6, 4 };

		internal static bool IsBoneArmorScroll(SpellScroll scroll) {
			return scroll.SpellId == SpellId;
		}

		private static bool IsNecromancer(Player self) {
			if (Professions.HasProfession(self, Professions.Necromant)) {
				return true;
			}
			self.RedMessage(Loc<BoneArmorLoc>.Get(self.Language).NotForYou);
			return false;
		}

		private static bool BookHoldsIt(Player self) {
			if ((self.FindLayer(1) is SpellBook held) && held.HasSpell(SpellId)) {
				return true;
			}
			foreach (Item item in self.Backpack) {
				if ((item is SpellBook book) && book.HasSpell(SpellId)) {
					return true;
				}
			}
			return false;
		}

		[SteamFunction]
		public static void NecroArmor(Player self) {
			if (IsNecromancer(self)) {
				BoneArmorLoc texts = Loc<BoneArmorLoc>.Get(self.Language);
				if (!BookHoldsIt(self)) {
					self.SysMessage(texts.NotInBook);
				} else if (self.GetSkill(SkillName.Magery) < LeastSkill) {
					self.SysMessage(texts.BeyondYou);
				} else if (self.Mana < BookMana) {
					self.SysMessage(texts.NoMana);
				} else {
					self.Mana = (short) (self.Mana - BookMana);
					Begin(self);
				}
			}
		}

		internal static void UseScroll(Player self, SpellScroll scroll) {
			BoneArmorLoc texts = Loc<BoneArmorLoc>.Get(self.Language);
			if (scroll.TopObj() != self) {
				self.SysMessage(texts.MustCarry);
			} else if (IsNecromancer(self)) {
				if (self.Mana < ScrollMana) {
					self.SysMessage(texts.NoMana);
				} else {
					scroll.Consume(1);
					self.Mana = (short) (self.Mana - ScrollMana);
					Begin(self);
				}
			}
		}

		private static void Begin(Player self) {
			self.AddTimer(castKey, new BoneArmorCastTimer()).DueInSeconds = 1;
		}

		internal static void Finish(Character self) {
			if ((self.CurrentSkill != null) || (self.GetSkill(SkillName.Magery) < Globals.dice.Next(FailLow, FailHigh + 1))) {
				MagerySkillDef.Fizzle(self);
				return;
			}
			SpellDef.GetByDefname("s_protection").ApplyEffectChar(self, self, 1000, EffectFlag.FromSpellBook | EffectFlag.BeneficialEffect);
			int seconds = Globals.dice.Next(LeastSeconds, MostSeconds + 1);
			self.Sound(577);
			EffectFactory.StationaryEffect(self, 0x373A, 1, 16);
			for (int i = 0; i < pieces.Length; i++) {
				ItemDef def = (ItemDef) ThingDef.GetByDefname(pieces[i]);
				AbstractItem worn = self.FindLayer(layers[i]);
				if ((worn == null) || (worn.Def == def)) {
					worn?.Delete();
					Item piece = (Item) def.Create(self.Backpack);
					self.TryEquip(piece);
					piece.AddTimer(wornKey, new BoneArmorFadeTimer()).DueInSeconds = seconds;
				}
			}
		}
	}

	/// <summary>i_bone_armor_timer</summary>
	[SaveableClass, DeepCopyableClass]
	public class BoneArmorCastTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public BoneArmorCastTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			BoneArmor.Finish((Character) cont);
		}
	}

	/// <summary>The piece's own Sphere timer ran out.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BoneArmorFadeTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public BoneArmorFadeTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			cont.Delete();
		}
	}

	internal class BoneArmorLoc : CompiledLocStringCollection<BoneArmorLoc> {
		public string NotForYou = "Nesmis pouzivat toto kouzlo.";
		public string NotInBook = "The spell is not in your spellbook";
		public string BeyondYou = "It is beyond your ability";
		public string NoMana = "You lack sufficient mana for this spell.";
		public string MustCarry = "Magic items must be on your person to activate.";
	}
}
