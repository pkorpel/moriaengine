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

	public interface IPoisonableItem {
		int PoisoningDifficulty { get; }
		double PoisoningEfficiency { get; }
	}

	/// <summary>#962 - Erebor's f_poisoning: the skill's button only says how, the bottle is double clicked. It goes on a weapon
	/// that has poisoning numbers (nastaveni_weappoison) or on arrows that have a poisoned kind. Nobody gains by use.</summary>
	[ViewableClass]
	public class PoisoningSkillDef : SkillDef {
		public PoisoningSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<PoisoningLoc>.Get(self.Language).UseTheBottle, Bandaging.ClassMessageColor);
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>f_poisoning.</summary>
		public static void UseBottle(Player self, PoisonPotion bottle) {
			if (WorkLight.CheckWithMessage(self)) {
				SkillSequenceArgs.AbortSkill(self);
				ScriptedSkills.Stop(self);
				PoisoningTimer timer = new PoisoningTimer();
				timer.bottle = bottle;
				self.AddTimer(PoisoningTimer.Key, timer).DueInSeconds = PoisoningTimer.SecondsForTheCursor;
				self.SysMessage(Loc<PoisoningLoc>.Get(self.Language).WhatToPoison);
				self.Target(SingletonScript<Targ_Poisoning>.Instance, timer);
			}
		}

		/// <summary>stopscriptedskills.</summary>
		internal static void Spoil(Character self) {
			PoisoningTimer timer = self.GetTimer(PoisoningTimer.Key) as PoisoningTimer;
			if (timer != null) {
				timer.works = false;
				timer.DueInSeconds = 0;
			}
		}

		internal static ItemDef PoisonedKindOf(Item arrows) {
			return ThingDef.GetByDefname(arrows.Def.PrettyDefname + "_poison") as ItemDef;
		}
	}

	public class Targ_Poisoning : CompiledTargetDef {
		private const int MostAmount = 50, RollOfArrows = 1001, RollOfWeapons = 101;

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			PoisoningTimer timer = (PoisoningTimer) parameter;
			if (timer.IsDeleted) {
				return TargetResult.Done;
			}
			PoisoningLoc texts = Loc<PoisoningLoc>.Get(self.Language);
			Projectile arrows = targetted as Projectile;
			Weapon weapon = targetted as Weapon;
			if ((arrows != null) && (PoisonedItemPlugin.GetPoisonPlugin(arrows) != null)) {
				self.SysMessage(texts.ArrowsAlreadyPoisoned);
				return TargetResult.Done;
			}
			if (targetted.TopObj() != self) {
				self.SysMessage(texts.MustHoldIt);
			} else if (targetted.Amount > MostAmount) {
				self.SysMessage(texts.TooMany);
			} else if ((arrows != null) ? (PoisoningSkillDef.PoisonedKindOf(arrows) == null) : ((weapon == null) || (weapon.PoisoningDifficulty == 0))) {
				self.SysMessage(texts.CantPoison);
			} else {
				int skill = self.GetSkill(SkillName.Poisoning);
				timer.target = targetted;
				timer.applying = true;
				timer.works = (arrows != null) ? (skill > Globals.dice.Next(RollOfArrows)) : (skill > (weapon.PoisoningDifficulty + Globals.dice.Next(RollOfWeapons)));
				SkillDef def = SkillDef.GetBySkillName(SkillName.Poisoning);
				timer.DueInSeconds = def.GetDelayForValue((ushort) skill);
				return TargetResult.Done;
			}
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}
	}

	/// <summary>i_potion_poison_timer.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PoisoningTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_poisoning_");

		internal const double SecondsForTheCursor = 20;

		private const int RanOutRoll = 11, RanOutOn = 6, SpoiltRoll = 1401, SelfPoisonRoll = 1001, SelfPoisonBase = 1200, SelfPoisonShare = 12;

		[SaveableData, CopyableData]
		public PoisonPotion bottle;

		[SaveableData, CopyableData]
		public Item target;

		[SaveableData, CopyableData]
		public bool applying;

		[SaveableData, CopyableData]
		public bool works;

		[LoadingInitializer, DeepCopyImplementation]
		public PoisoningTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if (!this.applying || (this.bottle == null) || this.bottle.IsDeleted || (this.target == null) || this.target.IsDeleted) {
				return;
			}
			PoisoningLoc texts = Loc<PoisoningLoc>.Get(self.Language);
			bool arrows = this.target is Projectile;
			if (this.works) {
				if (arrows) {
					this.PoisonArrows(self);
				} else {
					this.PoisonWeapon();
				}
				self.SysMessage(self.IsFemale ? texts.AppliedFemale : texts.AppliedMale);
				if (Globals.dice.Next(RanOutRoll) == RanOutOn) {
					self.SysMessage(texts.RanOut);
					this.bottle.Consume(1);
				}
			} else {
				self.SysMessage(texts.Failed);
				if (((SelfPoisonBase - self.GetSkill(SkillName.Poisoning)) / SelfPoisonShare) > Globals.dice.Next(SelfPoisonRoll)) {
					self.SysMessage(self.IsFemale ? texts.PoisonedYourselfFemale : texts.PoisonedYourselfMale);
					Poison.Apply(self, self, this.bottle.PoisonPower, PoisonEffectSettings.instance.SecondsFor(this.bottle.Kind), this.bottle.Kind);
				}
			}
			if (!arrows && !this.bottle.IsDeleted && (self.GetSkill(SkillName.Poisoning) < Globals.dice.Next(SpoiltRoll))) {
				self.SysMessage(self.IsFemale ? texts.SpoiltFemale : texts.SpoiltMale);
				this.bottle.Consume(1);
			}
		}

		private void PoisonArrows(Character self) {
			Item arrows = this.target;
			Item poisoned = (Item) PoisoningSkillDef.PoisonedKindOf(arrows).Create(arrows.Cont as Container ?? self.Backpack);
			poisoned.Amount = arrows.Amount;
			poisoned.Color = this.bottle.Color;
			arrows.Delete();
			PoisonedItemPlugin.Bind(poisoned, this.bottle, (this.bottle.PoisonPower * PoisonEffectSettings.instance.percentOnArrows) / 100, 0);
		}

		private void PoisonWeapon() {
			Weapon weapon = (Weapon) this.target;
			if (weapon.Def.PrettyDefname == "i_kudla") {
				weapon.Color = this.bottle.Color;
			}
			PoisonedItemPlugin.Bind(weapon, this.bottle, (int) (this.bottle.PoisonPower * weapon.PoisoningEfficiency), weapon.PoisonCapacity);
		}
	}

	public class PoisoningLoc : CompiledLocStringCollection<PoisoningLoc> {
		public string UseTheBottle = "Jed se aplikuje poklikanim na ampulku s jedem.";
		public string WhatToPoison = "Co chces otravit?";
		public string ArrowsAlreadyPoisoned = "Tyhle sipky uz jsou otravene.";
		public string MustHoldIt = "Vec musis mit u sebe.";
		public string TooMany = "Na takove mnozstvi jedna lahvicka nestaci.";
		public string CantPoison = "Na toto nemuzes aplikovat jed.";
		public string AppliedMale = "Uspesne jsi aplikoval jed.";
		public string AppliedFemale = "Uspesne jsi aplikovala jed.";
		public string RanOut = "Dosel jed.";
		public string Failed = "Nepovedlo se ti aplikovat jed.";
		public string PoisonedYourselfMale = "Pri manipulaci jsi se priotravil pouzitym jedem.";
		public string PoisonedYourselfFemale = "Pri manipulaci jsi se priotravila pouzitym jedem.";
		public string SpoiltMale = "Pri manipulaci jsi znehodnotil lektvar.";
		public string SpoiltFemale = "Pri manipulaci jsi znehodnotila lektvar.";
	}
}
