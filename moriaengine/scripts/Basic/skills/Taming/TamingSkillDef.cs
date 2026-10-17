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

	/// <summary>
	/// Taming (enhancements #21) - previously only a SkillName enum entry and an @Fail-only
	/// shell, no logic at all. Taming a valid NPC (NPCDef.TamingDifficulty &gt;= 0, nothing
	/// else's pet already) sets Character.ControlMaster, which NPCAiTimer's new Following state
	/// and NPC.ShouldAggroOn (tamed pets don't pick their own fights) both key off of.
	///
	/// Deliberately out of scope here: pet commands (guard/stay/kill/release), a pet-ownership
	/// transfer/abandon flow, and a pet list dialog - this pass is "taming produces a pet that
	/// follows you around and doesn't fight on its own," not the full command suite. Natural
	/// follow-ups once this is in use.
	/// </summary>
	[ViewableClass]
	public class TamingSkillDef : SkillDef {
		private const int MaxReach = 3;

		public TamingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.SysMessage(Loc<TamingLoc>.Get(self.Language).whatToTame);
				self.Target(SingletonScript<Targ_Tame>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			NPC targetted = skillSeqArgs.Target1 as NPC;

			if (!IsValidTameTarget(self, targetted)) {
				skillSeqArgs.PhaseAbort();
				return TriggerResult.Cancel;
			}

			double difficulty = ((NPCDef) targetted.Def).TamingDifficulty;
			skillSeqArgs.Success = this.CheckSuccess(self, (int) difficulty);
			return TriggerResult.Continue;
		}

		internal static bool IsValidTameTarget(Character self, NPC targetted) {
			if ((targetted == null) || !targetted.IsAliveAndValid) {
				return false;
			}
			if (Point2D.GetSimpleDistance(self, targetted) > MaxReach) {
				return false;
			}
			if (!self.CanInteractWith(targetted).Allow) {
				return false;
			}
			if (targetted.ControlMaster != null) {
				return false; //already somebody's pet
			}
			return ((NPCDef) targetted.Def).TamingDifficulty >= 0;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			NPC targetted = (NPC) skillSeqArgs.Target1;

			targetted.ControlMaster = self;
			targetted.Flag_WarMode = false;
			self.SysMessage(Loc<TamingLoc>.Get(self.Language).success);
			SkillGainSystem.TryGainSkillCustomFormula(self, SkillName.AnimalTaming, SkillAdvancementSettings.instance.tamingAdvRate); //#329
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<TamingLoc>.Get(self.Language).fail);
			//#329 - real Erebor source (taming.scp) calls SKILLGAIN on both outcomes, not just
			//success as #329's own issue text summarized - matching the real mechanic here since
			//every other skill in this pass (Lockpicking included) also gains on both outcomes.
			SkillGainSystem.TryGainSkillCustomFormula(self, SkillName.AnimalTaming, SkillAdvancementSettings.instance.tamingAdvRate);
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<TamingLoc>.Get(self.Language).abort);
		}
	}

	public class Targ_Tame : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			NPC npc = targetted as NPC;
			if (!TamingSkillDef.IsValidTameTarget(self, npc)) {
				self.SysMessage(Loc<TamingLoc>.Get(self.Language).cantTameThat);
				return TargetResult.RestartTargetting;
			}
			skillSeq.Target1 = npc;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage(Loc<TamingLoc>.Get(self.Language).cantTameThat);
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage(Loc<TamingLoc>.Get(self.Language).cantTameThat);
			return TargetResult.RestartTargetting;
		}
	}

	public class TamingLoc : CompiledLocStringCollection<TamingLoc> {
		public string whatToTame = "What would you like to tame?";
		public string cantTameThat = "You can't tame that.";
		public string fail = "You fail to tame the creature.";
		public string success = "You have tamed the creature.";
		public string abort = "You stop trying to tame the creature.";
	}
}
