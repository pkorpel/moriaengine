using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class AnatomySkillDef : SkillDef {
		public AnatomySkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_Anatomy>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (self.CanInteractWithMessage(skillSeqArgs.Target1)) {
				skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
				return TriggerResult.Continue;
			}
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			GameState stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				//TODO: Hlasky pro ruzne intervaly STR/STAM
				stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).ASuccess1);
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			GameState stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).AFailed);
			}
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			GameState stateSelf = skillSeqArgs.Self.GameState;
			if (stateSelf != null) {
				stateSelf.WriteLine(Loc<AnatomyLoc>.Get(stateSelf.Language).ACanceled);
			}
		}
	}

	public class Targ_Anatomy : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.WriteLine(Loc<AnatomyLoc>.Get(self.Language).TargetWho);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!self.CanInteractWithMessage(targetted)) {
				return TargetResult.RestartTargetting;
			}

			if (targetted.IsHuman) {
				SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
				skillSeq.Target1 = targetted;
				skillSeq.PhaseStart();
				return TargetResult.Done;
			}
			self.WriteLine(Loc<AnatomyLoc>.Get(self.Language).TargetOnlyHuman);
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.WriteLine(Loc<AnatomyLoc>.Get(self.Language).TargetOnlyHuman);
			return TargetResult.RestartTargetting;
		}
	}

	public class AnatomyLoc : CompiledLocStringCollection<AnatomyLoc> {
		internal readonly string TargetWho = "Koho chceš zkoumat?";
		internal readonly string TargetOnlyHuman = "Zamìøuj pouze osoby!";
		internal readonly string ACanceled = "Anatomie byla pøerušena.";
		internal readonly string AFailed = "Tvé zkoumání se nezdaøilo.";
		internal readonly string ASuccess1 = "Anatomie se ti povedla.";
	}
}