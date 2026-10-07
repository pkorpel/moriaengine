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
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	public abstract class BandageSkillDef : SkillDef {
		public BandageSkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}


		public static void SelectBandageSkill(Player self) {
			self.Target(SingletonScript<Targ_SelectBandageTarget>.Instance);
		}

		public static void SelectBandageSkill(Player self, Item bandage) {
			self.Target(SingletonScript<Targ_SelectBandageTarget>.Instance, bandage);
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item bandage = AcquireBandage(self, skillSeqArgs.Tool);
			if (bandage == null) {
				return TriggerResult.Cancel;
			}
			skillSeqArgs.Tool = bandage;


			Thing target = skillSeqArgs.Target1 as Thing;
			//není cíl
			if (target == null) {
				return TriggerResult.Cancel;
			}

			//nevidi na cil
			if (Point2D.GetSimpleDistance(self, target) > Math.Min(6, self.VisionRange)) {
				self.ClilocSysMessage(3000268);	//That is too far away.
				return TriggerResult.Cancel;
			}
			if (!self.CanInteractWithMessage(target)) {
				return TriggerResult.Cancel;
			}

			return TriggerResult.Continue;
		}


		private TimerKey healingTimerKey = TimerKey.Acquire("_healing_timer_");

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			//abort previous healing/veterinary, if needed
			SkillSequenceArgs.SkillStrokeTimer timer = (SkillSequenceArgs.SkillStrokeTimer) self.RemoveTimer(this.healingTimerKey);
			if (timer != null) {
				timer.skillSeqArgs.PhaseAbort();
				timer.skillSeqArgs = null;
				timer.Delete();
			}

			skillSeqArgs.DelayInSeconds = this.GetDelayForChar(self);
			if (skillSeqArgs.DelaySpan < TimeSpan.Zero) {
				skillSeqArgs.PhaseStroke();
			} else {
				self.AddTimer(this.healingTimerKey, new SkillSequenceArgs.SkillStrokeTimer(skillSeqArgs)).DueInSpan = skillSeqArgs.DelaySpan;
			}

			return TriggerResult.Cancel; //cancel normal operation, we use separate timer here
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			Thing target = (Thing) skillSeqArgs.Target1;
			//nevidi na cil
			if (Point2D.GetSimpleDistance(self, target) > Math.Min(6, self.VisionRange)) {
				self.ClilocSysMessage(3000268);	//That is too far away.
				return TriggerResult.Cancel;
			}
			if (!self.CanInteractWithMessage(target)) {
				return TriggerResult.Cancel;
			}

			return TriggerResult.Continue;
		}

		private static ItemDef i_bandage_bloody;
		public static ItemDef BloodyBandageDef {
			get {
				if (i_bandage_bloody == null) {
					i_bandage_bloody = (ItemDef) ThingDef.GetByDefname("i_bandage_bloody");
				}
				return i_bandage_bloody;
			}
		}

		private static ItemDef i_bandage;
		public static ItemDef CleanBandageDef {
			get {
				if (i_bandage == null) {
					i_bandage = (ItemDef) ThingDef.GetByDefname("i_bandage");
				}
				return i_bandage;
			}
		}

		public static Item AcquireBandage(Character self, Item uncheckedBandage) {
			if (uncheckedBandage != null) {
				DenyResult result = self.CanPickup(uncheckedBandage);
				if (!result.Allow) {
					uncheckedBandage = self.Backpack.FindByTypeShallow(SingletonScript<t_bandage>.Instance);
					if ((uncheckedBandage == null) || (!self.CanPickup(uncheckedBandage).Allow)) {
						result.SendDenyMessage(self);
						return null;
					}
				}
			}
			if (uncheckedBandage == null) {
				uncheckedBandage = self.Backpack.FindByTypeShallow(SingletonScript<t_bandage>.Instance);
				if ((uncheckedBandage == null) || (!self.CanPickup(uncheckedBandage).Allow)) {
					self.SysMessage(Loc<BandageSkillLoc>.Get(self.Language).YouHaveNoBandages);
					return null;
				}
			}

			if ((uncheckedBandage.Type == SingletonScript<t_bandage>.Instance) || //clean bandage
				((uncheckedBandage.Type == SingletonScript<t_bandage_blood>.Instance) && Professions.HasProfession(self, Professions.Shaman))) { //bloody bandage shaman only
				return uncheckedBandage;
			}

			return null;
		}

		public static void AddBloodyBandage(Character self) {
			Item bloodybandage = self.Backpack.FindByTypeShallow(SingletonScript<t_bandage_blood>.Instance);
			if (bloodybandage == null) {
				BloodyBandageDef.Create(self.Backpack);
			} else {
				bloodybandage.Amount += 1;
			}
		}

		public static void AddCleanBandage(Character self) {
			Item cleanBandage = self.Backpack.FindByTypeShallow(SingletonScript<t_bandage>.Instance);
			if (cleanBandage == null) {
				CleanBandageDef.Create(self.Backpack);
			} else {
				cleanBandage.Amount += 1;
			}
		}

	}

	public class t_bandage_blood : CompiledTriggerGroup {

		public void On_DClick(Item self, Player clicker) {
			if (Professions.HasProfession(clicker, Professions.Shaman)) {
				BandageSkillDef.SelectBandageSkill(clicker, self);
			} else {
				Globals.SrcWriteLine("Bandage cleaning not yet implemented");
			}
		}
	}

	public class t_bandage : CompiledTriggerGroup {

		public void On_DClick(Item self, Player clicker) {
			BandageSkillDef.SelectBandageSkill(clicker, self);
		}
	}


	public class Targ_SelectBandageTarget : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.WriteLine(Loc<BandageSkillLoc>.Get(self.Language).SelectHealingTarget);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			SkillName skill;
			if (CharModelInfo.IsHumanModel(targetted.Model)) {
				skill = SkillName.Healing;
			} else {
				skill = SkillName.Veterinary;
			}

			SkillSequenceArgs skillSeq = SkillSequenceArgs.Acquire(self, skill, targetted, null, (Item) parameter, null, null); //tool = parameter = bandage
			skillSeq.PhaseSelect();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			Corpse corpse = targetted as Corpse;
			if (corpse != null) {
				SkillName skill;
				if (CharModelInfo.IsHumanModel(corpse.CharDef.Model)) {
					skill = SkillName.Healing;
				} else {
					skill = SkillName.Veterinary;
				}
				SkillSequenceArgs skillSeq = SkillSequenceArgs.Acquire(self, skill, targetted, null, (Item) parameter, null, null); //tool = parameter = bandage
				skillSeq.PhaseSelect();
				return TargetResult.Done;
			}
			self.WriteLine(Loc<BandageSkillLoc>.Get(self.Language).CantHealItems);
			return TargetResult.RestartTargetting;
		}
	}

	public class BandageSkillLoc : CompiledLocStringCollection<BandageSkillLoc> {
		internal readonly string SelectHealingTarget = "Koho chceš léèit?";
		internal readonly string CantHealItems = "Pøedmìty nelze léèit.";
		internal readonly string YouHaveNoBandages = "Nemáš u sebe bandáže!";
	}
}