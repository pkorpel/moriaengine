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

using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's [function animate]: the corpse of a monster gets up as the caster's pet, as far as the staff
	/// reaches, for as long as there's mana to keep it. Its death leaves nothing.</summary>
	[ViewableClass]
	public class AnimateDeadSpellDef : SpellDef {
		private static readonly TagKey isAnimatedTK = TagKey.Acquire("isAnimated");
		private static readonly TimerKey upkeepKey = TimerKey.Acquire("_animatedUpkeep_");
		private static readonly TimerKey endKey = TimerKey.Acquire("_animatedEnd_");

		//strop_spruce to strop_elven, the exp a staff of that material holds
		private static readonly int[,] staffTiers = { { 500, 1 }, { 1000, 2 }, { 1500, 3 }, { 2500, 4 }, { 4000, 5 }, { 6000, 6 }, { 9000, 7 } };

		public AnimateDeadSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static bool IsAnimated(Character ch) {
			return ch.GetTag(isAnimatedTK) != null;
		}

		internal static int Ability(Character ch, string tag) {
			return (ch.GetTag(TagKey.Acquire(tag)) is int points) ? points : 0;
		}

		private static bool StaffHolds(Player self, int exp) {
			int ability = Ability(self, "ra_ucinnost");
			int cap = (ability <= 0) ? 100 : ((ability < 11) ? ((ability * 2) + 100) : (ability + 110));
			for (int i = 0; i < staffTiers.GetLength(0); i++) {
				if (exp <= ((staffTiers[i, 0] * cap) / 100)) {
					return SpellCastRules.HasStaff(self, staffTiers[i, 1]);
				}
			}
			return true; //past the last tier Erebor's chain just ends
		}

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectChar(target, spellEffectArgs);
			if (spellEffectArgs.Caster is Player self) {
				self.RedMessage(Loc<AnimateDeadLoc>.Get(self.Language).NotABody);
			}
		}

		protected override void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectItem(target, spellEffectArgs);
			if (spellEffectArgs.Caster is Player self) {
				Raise(self, target as Corpse);
			}
		}

		private static void Raise(Player self, Corpse corpse) {
			AnimateDeadLoc texts = Loc<AnimateDeadLoc>.Get(self.Language);
			NPCDef def = corpse?.CharDef as NPCDef;
			if (def == null) {
				self.RedMessage(texts.NotABody);
				return;
			}
			if (corpse.Owner != null) { //a player's
				return;
			}
			if (def.NoAnimate) {
				MagerySkillDef.Fizzle(self);
				self.RedMessage(texts.CantRaise);
				return;
			}
			NPC raised;
			bool wasResurrected = MonsterLoot.CreatingResurrected;
			MonsterLoot.CreatingResurrected = true;
			try {
				raised = (NPC) def.Create((IPoint4D) corpse.TopPoint);
			} finally {
				MonsterLoot.CreatingResurrected = wasResurrected;
			}
			int exp = raised.ExperienceReward;
			if (!StaffHolds(self, exp)) {
				raised.Delete();
				MagerySkillDef.Fizzle(self);
				self.SysMessage(texts.StaffTooWeak);
				return;
			}
			foreach (Item item in new List<Item>(ItemsIn(corpse))) {
				if (!item.Flag_Newbied) {
					item.P(corpse.TopPoint);
				}
			}
			StripGear(raised);
			raised.SetTag(isAnimatedTK, 1);
			raised.Karma = 0;
			raised.ControlMaster = self;
			NecroAbilities.StrengthenUndead(raised, self); //#1017
			raised.Flag_WarMode = false;

			AnimatedUpkeepTimer upkeep = new AnimatedUpkeepTimer();
			upkeep.caster = self;
			raised.AddTimer(upkeepKey, upkeep);
			upkeep.PeriodInSeconds = 1;
			upkeep.DueInSeconds = 1;

			AnimatedEndTimer end = new AnimatedEndTimer();
			end.caster = self;
			raised.AddTimer(endKey, end);
			end.DueInSeconds = Lifetime(self);
			corpse.Delete();
		}

		private static IEnumerable<Item> ItemsIn(Container container) {
			foreach (Item item in container) {
				yield return item;
			}
		}

		private static void StripGear(NPC raised) {
			Container pack = raised.Backpack;
			foreach (Item item in new List<Item>(ItemsIn(pack))) {
				if (!item.Flag_Newbied) {
					item.Delete();
				}
			}
			Item hair = raised.Hair, beard = raised.Beard;
			foreach (Thing equipped in new List<Thing>(raised.VisibleEquip)) {
				Item item = (Item) equipped;
				if (!item.Flag_Newbied && (item != pack) && (item != hair) && (item != beard)) {
					item.Delete();
				}
			}
		}

		private static int Lifetime(Player self) {
			int seconds = (self.Level / 2) + 50;
			int ability = Ability(self, "ra_vydrz");
			if (ability > 0) {
				seconds = (seconds * ((ability < 11) ? ((ability * 3) + 100) : (ability + 120))) / 100;
			}
			return seconds;
		}

		/// <summary>i_rune_vzboureni's price of a second.</summary>
		internal static int UpkeepOf(int exp, Character caster) {
			int mana = (exp / NecromancySettings.instance.animateManaDrain) + 1;
			int ability = Ability(caster, "ra_mana_narocnost");
			if (ability > 0) {
				mana = (mana * ((ability < 11) ? (100 - (ability * 2)) : (90 - ability))) / 100;
			}
			return mana;
		}
	}

	/// <summary>i_rune_vzboureni</summary>
	[SaveableClass, DeepCopyableClass]
	public class AnimatedUpkeepTimer : BoundTimer {
		[SaveableData, CopyableData]
		public Character caster;

		[LoadingInitializer, DeepCopyImplementation]
		public AnimatedUpkeepTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC raised = (NPC) cont;
			if ((this.caster == null) || this.caster.IsDeleted) {
				raised.Delete();
				return;
			}
			int mana = AnimateDeadSpellDef.UpkeepOf(raised.ExperienceReward, this.caster);
			if (this.caster.Mana > mana) {
				this.caster.Mana = (short) (this.caster.Mana - mana);
			} else {
				this.caster.SysMessage(Loc<AnimateDeadLoc>.Get(this.caster.Language).LostHold);
				raised.Delete();
			}
		}
	}

	/// <summary>i_npcanimate_remove</summary>
	[SaveableClass, DeepCopyableClass]
	public class AnimatedEndTimer : BoundTimer {
		[SaveableData, CopyableData]
		public Character caster;

		[LoadingInitializer, DeepCopyImplementation]
		public AnimatedEndTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((this.caster != null) && !this.caster.IsDeleted) {
				this.caster.SysMessage(Loc<AnimateDeadLoc>.Get(this.caster.Language).Dissolved);
			}
			cont.Delete();
		}
	}

	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class NecromancySettings : SettingsMetaCategory {
		[SavedMember]
		public static NecromancySettings instance = new NecromancySettings();

		[LoadingInitializer]
		public NecromancySettings() {
		}

		/// <summary>nastaveni_ability_animatemanadrain: a raised monster costs a mana a second per this much of its exp.</summary>
		[SaveableData]
		public int animateManaDrain = 100;
	}

	internal class AnimateDeadLoc : CompiledLocStringCollection<AnimateDeadLoc> {
		public string NotABody = "Toto neni telo.";
		public string CantRaise = "S timto telem nelze dale pracovat.";
		public string StaffTooWeak = "Na toto monstrum nestaci tvoje hul.";
		public string LostHold = "Uz si nedokazal udrzet monstrum na zivu.";
		public string Dissolved = "Monstrum se rozplynulo.";
	}
}
