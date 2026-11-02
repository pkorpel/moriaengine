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

	/// <summary>#962 - Erebor's t_trap_custom: a player's trap, set with a bomb, a poison bottle or another trap. Each works once
	/// and only on a first step here. Erebor blanks the poison's kind when setting one, so its poison never fires there;
	/// here it does, a question for the user.</summary>
	public class t_trap_custom : CompiledTriggerGroup {
		private const int MostPoisonWound = 25, MostSprungWound = 50;
		private static readonly TagKey lastTrapTK = TagKey.Acquire("past");
		internal static readonly TagKey bombTK = TagKey.Acquire("bomba");
		internal static readonly TagKey sprungTK = TagKey.Acquire("natazeno");
		internal static readonly TagKey poisonTK = TagKey.Acquire("jed_sila");
		internal static readonly TagKey poisonKindTK = TagKey.Acquire("jed_typ");

		private static void Wound(Character victim, int most) {
			DamageManager.CauseDamage(null, victim, DamageType.Physical, Globals.dice.Next(1, most + 1));
		}

		public void On_Step(Item self, Character stepping) {
			string here = self.P().ToString();
			if (here.Equals(stepping.GetTag(lastTrapTK) as string)) {
				return;
			}
			stepping.SetTag(lastTrapTK, here);
			if (self.GetTag(bombTK) is Potion bomb) {
				if (!bomb.IsDeleted && (PotionKinds.Find(bomb) is BombPotionKind kind)) {
					kind.Explode(bomb);
				}
				self.Delete();
			} else if (self.GetTag(poisonTK) is int strength) {
				PoisonKind kind2 = (PoisonKind) (int) self.GetTag(poisonKindTK);
				Poison.Apply(stepping, null, strength, PoisonEffectSettings.instance.SecondsFor(kind2), kind2);
				Wound(stepping, MostPoisonWound);
				self.RemoveTag(poisonTK);
			} else if (self.HasTag(sprungTK)) {
				Wound(stepping, MostSprungWound);
				self.RemoveTag(sprungTK);
			}
		}

		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				player.SysMessage(Loc<TrapLoc>.Get(player.Language).WhatToSet);
				player.Target(SingletonScript<Targ_Trap>.Instance, self);
			}
			return TriggerResult.Cancel;
		}
	}

	public class Targ_Trap : CompiledTargetDef {
		private const int MostRushWound = 50, SecondsToWait = 60;
		private static readonly TagKey waitUntilTK = TagKey.Acquire("mytimer");

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			Item trap = (Item) parameter;
			TrapLoc texts = Loc<TrapLoc>.Get(self.Language);
			if ((trap.GetTag(waitUntilTK) is double until) && (until > Globals.TimeInSeconds)) {
				self.SysMessage(self.IsFemale ? texts.DontRushFemale : texts.DontRushMale);
				DamageManager.CauseDamage(null, self, DamageType.Physical, Globals.dice.Next(MostRushWound + 1));
			}
			trap.SetTag(waitUntilTK, Globals.TimeInSeconds + SecondsToWait);
			if ((targetted is Potion bomb) && (PotionKinds.Find(bomb) is BombPotionKind)) {
				trap.SetTag(t_trap_custom.bombTK, bomb);
				bomb.P(trap);
				bomb.Flag_Invisible = true;
				self.SysMessage(self.IsFemale ? texts.ExplosionSetFemale : texts.ExplosionSetMale);
			} else if (targetted is PoisonPotion bottle) {
				trap.SetTag(t_trap_custom.poisonTK, bottle.PoisonPower);
				trap.SetTag(t_trap_custom.poisonKindTK, (int) bottle.Kind);
				bottle.Consume(1);
				self.SysMessage(self.IsFemale ? texts.PoisonedFemale : texts.PoisonedMale);
			} else if (CraftingSkillDef.IsOfType(targetted, "t_trap_custom")) {
				trap.SetTag(t_trap_custom.sprungTK, 1);
				self.SysMessage(self.IsFemale ? texts.SprungFemale : texts.SprungMale);
			}
			return TargetResult.Done;
		}
	}

	internal class TrapLoc : CompiledLocStringCollection<TrapLoc> {
		public string WhatToSet = "Co chces vlozit do pasti?";
		public string DontRushMale = "Nebud zbrkly !";
		public string DontRushFemale = "Nebud zbrkla !";
		public string ExplosionSetMale = "Nastrazil jsi explozi";
		public string ExplosionSetFemale = "Nastrazila jsi explozi";
		public string PoisonedMale = "Otravil jsi past.";
		public string PoisonedFemale = "Otravila jsi past.";
		public string SprungMale = "Natahl jsi past.";
		public string SprungFemale = "Natahla jsi past.";
	}
}
