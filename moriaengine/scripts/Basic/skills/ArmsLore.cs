using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class ArmsLoreSkillDef : SkillDef {
		public ArmsLoreSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.Target(SingletonScript<Targ_ArmsLore>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (self.CanReachWithMessage((Item) skillSeqArgs.Target1)) {
				skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
				return TriggerResult.Continue;
			}
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage("Arms lore SUKCEEES");// kontrolni hlaska, pozdeji odstranit!
			Destroyable targetted = (Destroyable) skillSeqArgs.Target1;

			self.SysMessage("Armor už umíme, doplníme. Vypisujeme testovaci hlasku: " + targetted.Name + " model je: " + targetted.Model);

			//Destroyable = zbran (Weapon) nebo brneni/obleceni (Wearable)
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Arms lore se nepovedlo.");
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Arms lore pøerušeno.");
		}
	}

	public class Targ_ArmsLore : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Co chceš prohlédnout?");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("Zamìøuj pouze zbranì a zbroje!");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (!self.CanReachWithMessage(targetted)) {
				return TargetResult.RestartTargetting;
			}

			if (targetted is Destroyable) {
				SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
				skillSeq.Target1 = targetted;
				skillSeq.PhaseStart();
				return TargetResult.Done;
			}
			self.SysMessage("Zamìøuj pouze zbranì a zbroje!");
			return TargetResult.RestartTargetting;
		}
	}
}