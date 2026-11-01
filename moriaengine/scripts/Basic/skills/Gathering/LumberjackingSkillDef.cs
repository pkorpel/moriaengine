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
	/// Lumberjacking (enhancements #21) - was a shell with only an @Fail message. Targets any
	/// world static flagged TileFlag.Foliage in tiledata.mul (bushes/tree leaves per that flag's
	/// own doc comment) rather than a hardcoded tree-static-ID whitelist, for the same reason as
	/// Mining: no per-tile/per-static resource registry exists in this engine to verify specific
	/// IDs against. No per-tree depletion/respawn system - same simplification as Mining.
	/// </summary>
	[ViewableClass]
	public class LumberjackingSkillDef : SkillDef {
		private const int MaxReach = 3;

		public LumberjackingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				//#370 - mandatory tool, same consistency decision as Mining's own pickaxe gate.
				if (!Equippable.IsHoldingToolFor(self, SkillName.Lumberjacking)) {
					self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).needHatchet);
					return TriggerResult.Cancel;
				}
				self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).whatToChop);
				self.Target(SingletonScript<Targ_Lumberjack>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			//#424 - PhaseStart already set the generic curve-based delay just before this
			//trigger fired; overwrite it with the real anti-macro formula (flat 20s, reduced
			//above skill 100.0%), same wiring as MiningSkillDef.On_Start (#336) - the formula
			//itself is shared/generic in GatheringDelaySystem, just needed this call site.
			skillSeqArgs.DelayInSeconds = GatheringDelaySystem.GetDelaySeconds(skillSeqArgs.Self, SkillName.Lumberjacking);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			IPoint3D targetted = skillSeqArgs.Target1;

			if (!IsValidTree(self, targetted)) {
				skillSeqArgs.PhaseAbort();
				return TriggerResult.Cancel;
			}

			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
			return TriggerResult.Continue;
		}

		internal static bool IsValidTree(Character self, IPoint3D targetted) {
			AbstractInternalItem s = targetted as AbstractInternalItem;
			if ((s == null) || (Point2D.GetSimpleDistance(self, s) > MaxReach)) {
				return false;
			}
			if (!self.CanInteractWith(s).Allow) {
				return false;
			}
			return TileData.HasFlag(s.Flags, TileFlag.Foliage) || GatherTiles.IsOfType(s, "t_tree");
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			IPoint3D targetted = skillSeqArgs.Target1;

			//#422 - real per-tile wood-tier vein system (lazy per-tile veins, weighted roll on
			//an untouched vein, skill-gated downgrade to copper, depletion/regen). Replaces the
			//old hardcoded i_log_lager placeholder, which was never the real default to begin
			//with - confirmed via #270's corrected comment that item is exclusively a labor-
			//camp/prisoner-punishment flavor item, not normal lumberjacking output.
			ItemDef def = LumberjackingVeinSystem.Chop(self, self.GetMap().M, targetted.X, targetted.Y);
			if (def != null) {
				def.Create(self.Backpack);
				self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).success);
			} else if (LumberjackingVeinSystem.IsEmptySpot(self.GetMap().M, targetted.X, targetted.Y)) {
				self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).notATree); //#813 - the region's roll was "nothing"
			} else {
				self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).veinExhausted);
			}
			LumberjackingAmbushSystem.TryAmbush(self.P(), WoodTiers.ByItemDef(def)); //#431
			SkillGainSystem.TryGainSkill(self, SkillName.Lumberjacking); //#328
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).fail);
			LumberjackingAmbushSystem.TryAmbush(self.P(), null); //#431 - real fixresource rolls this on every gather action, not just success
			SkillGainSystem.TryGainSkill(self, SkillName.Lumberjacking); //#328 - real Erebor gains on fail too
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).abort);
		}
	}

	public class Targ_Lumberjack : CompiledTargetDef {
		protected override TargetResult On_TargonStatic(Player self, AbstractInternalItem targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			if (!LumberjackingSkillDef.IsValidTree(self, targetted)) {
				self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).notATree);
				return TargetResult.Done; //#391 - show the message once and stop, don't loop
			}
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).notATree);
			return TargetResult.Done; //#391
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).notATree);
			return TargetResult.Done; //#391
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage(Loc<LumberjackingLoc>.Get(self.Language).notATree);
			return TargetResult.Done; //#391
		}
	}

	public class LumberjackingLoc : CompiledLocStringCollection<LumberjackingLoc> {
		public string whatToChop = "What do you want to chop?";
		public string notATree = "There is no wood to be gathered there.";
		public string fail = "You hack at the tree for a while, but fail to produce any useable wood.";
		public string success = "You chop some logs.";
		public string abort = "You stop chopping.";
		public string needHatchet = "You must be holding a hatchet to do that."; //#370
		public string veinExhausted = "This tree seems to have no more wood to give."; //#422
	}
}
