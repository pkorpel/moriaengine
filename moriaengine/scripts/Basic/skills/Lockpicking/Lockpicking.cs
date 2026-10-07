using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class LockpickSkillDef : SkillDef {
		public LockpickSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_Lockpick>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item targetted = (Item) skillSeqArgs.Target1;
			if (self.CanReachWithMessage(targetted)) {
				skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));	// TODO pridat succes v zavislosti na obtiznosti zamku
				return TriggerResult.Continue;
				// odemknuti predmetu
			}
			skillSeqArgs.PhaseAbort();
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item targetted = (Item) skillSeqArgs.Target1;

			self.SysMessage(self.IsFemale ? Loc<LockpickLoc>.Get(self.Language).successWoman : Loc<LockpickLoc>.Get(self.Language).success);

			//TODO

		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			skillSeqArgs.Self.SysMessage(Loc<LockpickLoc>.Get(self.Language).fail);
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			skillSeqArgs.Self.SysMessage(Loc<LockpickLoc>.Get(self.Language).abort);
		}
	}


	public class t_lockpick : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			//TODO? use resource system for consuming lockpicks
			DenyResult canPickup = clicker.CanPickup(self);
			if (canPickup.Allow) {
				clicker.SelectSkill(SkillSequenceArgs.Acquire(clicker, SkillName.Lockpicking, self));
				//StartLockpick(clicker, self);
			} else {
				foreach (Item i in clicker.Backpack.EnumShallow()) {
					if ((i.Type == this) && (clicker.CanPickup(i).Allow)) {
						clicker.SelectSkill(SkillSequenceArgs.Acquire(clicker, SkillName.Lockpicking, self));
						return;
					}
				}
			}
			canPickup.SendDenyMessage(clicker);
		}
	}


	public class Targ_Lockpick : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<LockpickLoc>.Get(self.Language).whatToUseLockOn);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;

			//nevidi na cil
			if (self.CanReachWithMessage(targetted)) {
				if (!ItemLockPlugin.IsLocked(targetted)) {
					skillSeq.Target1 = targetted;
					skillSeq.PhaseStart();
					return TargetResult.Done;
				}
			}
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage(Loc<LockpickLoc>.Get(self.Language).cantUseLockpick);
			return TargetResult.RestartTargetting;
		}
	}

	public class LockpickLoc : CompiledLocStringCollection<LockpickLoc> {
		public string cantUseLockpick = "Na tohle nelze paklíè použít.";
		public string whatToUseLockOn = "Na co chceš použít paklíè?";
		public string abort = "Odemykání pøerušeno.";
		public string fail = "Nepodaøilo se ti odemknout zámek";
		public string success = "Úspìšnì jsi odemkl zámek";
		public string successWoman = "Úspìšnì jsi odemkla zámek";
	}
}