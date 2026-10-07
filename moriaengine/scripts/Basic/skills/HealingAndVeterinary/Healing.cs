using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class HealingSkillDef : BandageSkillDef {

		public HealingSkillDef(string defname, string filename, int line)
			: base(defname, filename, line) {
		}

		//public static SkillDef defHealing = SkillDef.ById(SkillName.Healing);

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = target as Character;
			if (targetChar != null) {
				//pokud ma cil maximum zivotu,a nekrvaci
				if ((targetChar.Hits >= targetChar.MaxHits) && (!BleedingEffectPluginDef.IsBleeding(targetChar))) {
					if (target == self) {
						self.WriteLine(Loc<HealingLoc>.Get(self.Language).YoureAtMaxHitpoints);
					} else {
						self.WriteLine(string.Format(CultureInfo.InvariantCulture,
							Loc<HealingLoc>.Get(self.Language).TargetIsAtMaxHitpoints, target.Name));
					}
					return TriggerResult.Cancel;
				}

				if (!CharModelInfo.IsHumanModel(targetChar.Model)) {
					self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingSkillNotApplicable);
					return TriggerResult.Cancel;
				}

			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}

			return base.On_Select(skillSeqArgs); //BandageSkillDef implementation, common for both Healing and Veterinary
		}


		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = target as Character;

			if (targetChar != null) {
				if (targetChar == self) {
					self.WriteLine(Loc<HealingLoc>.Get(self.Language).YoureStartingToHealYourself);
				} else {
					self.WriteLine(string.Format(CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(self.Language).YoureStartingToHealTarget, targetChar.Name));
				}
				skillSeqArgs.Tool.Consume(1);
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}

			return base.On_Start(skillSeqArgs); //BandageSkillDef implementation. Starts using separate timer
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(200));

			return base.On_Stroke(skillSeqArgs); //BandageSkillDef implementation. Similar checks as in @select
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = target as Character;
			if (targetChar != null) {
				// TODO? Vytvorit vzorec pro maximum a minimum pridanych HP

				if (BleedingEffectPluginDef.IsBleeding(targetChar)) {
					BleedingEffectPluginDef.StopBleeding(targetChar);

					self.WriteLine(Loc<HealingLoc>.Get(self.Language).SuccessfullyStoppedBleeding);
				} else {
					targetChar.Hits += (short) ScriptUtil.EvalRangePermille(self.GetSkill(SkillName.Healing) + self.GetSkill(SkillName.Anatomy), this.Effect);

					if (targetChar.Hits > targetChar.MaxHits) {
						targetChar.Hits = targetChar.MaxHits;
					}

					self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingSucceeded);
				}
				//tell the target hes been healed?


				Item usedBandages = skillSeqArgs.Tool;
				if ((usedBandages != null) && (usedBandages.Type == SingletonScript<t_bandage_blood>.Instance)) {
					AddCleanBandage(self);
				} else {
					AddBloodyBandage(self);
				}
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Thing target = skillSeqArgs.Target1 as Thing;

			Character targetChar = skillSeqArgs.Target1 as Character;
			if (targetChar != null) {
				self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingFailed);

				if (targetChar != self) {
					targetChar.WriteLine(string.Format(CultureInfo.InvariantCulture,
						Loc<HealingLoc>.Get(targetChar.Language).HealerFailedToHealYou, self.Name));
				}
			} else {
				throw new NotImplementedException("Resurrecting not implemented");
			}
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.WriteLine(Loc<HealingLoc>.Get(self.Language).HealingAborted);
		}
	}

	public class HealingLoc : CompiledLocStringCollection<HealingLoc> {
		internal readonly string YoureAtMaxHitpoints = "Jsi zcela zdráv!";
		internal readonly string TargetIsAtMaxHitpoints = "{0} je zcela zdráv!";
		internal readonly string YoureStartingToHealTarget = "Zaèínáš léèit {0}.";
		internal readonly string YoureStartingToHealYourself = "Zaèínáš se léèit.";
		internal readonly string HealingSucceeded = "Léèení se ti povedlo!";
		internal readonly string HealingFailed = "Léèení se ti nezdaøilo!";
		internal readonly string HealerFailedToHealYou = "{0} selhal pøi pokusu tì ošetøit!";
		internal readonly string HealingAborted = "Léèení pøerušeno";

		internal readonly string SuccessfullyStoppedBleeding = "Povedlo se ti zastavit krvácení";

		internal readonly string HealingSkillNotApplicable = "Nelze léèit skillem Healing";
	}
}