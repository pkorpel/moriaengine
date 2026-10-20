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
	/// Fishing (#328 shell, now built out under #271) - modeled directly on MiningSkillDef /
	/// LumberjackingSkillDef's shape (On_Select -> target -> On_Stroke check -> On_Success/
	/// On_Fail). Full implementation now wires in: the shared anti-macro delay (#271, via the
	/// generic GatheringDelaySystem - same formula as Mining #336 / Lumberjacking #424), the
	/// fish-variety + junk-fish/treasure-map catch system (FishingCatchSystem), and a
	/// water-monster ambush (FishingAmbushSystem). Skill-gain-through-use (#140/#328) was already
	/// wired.
	///
	/// Deliberately NOT a skill-gated fish-tier ladder (unlike Mining's/Lumberjacking's per-tile
	/// ore/wood veins): moriaengine has no fish-tier content to gate on - see FishingCatchSystem's
	/// own doc comment for the full reasoning. The treasure-map payoff of the junk-fish mechanic
	/// is a placeholder pending the real treasure-map system (#530), mirroring how Mining shipped
	/// functional-now and deferred region-restriction to #338.
	///
	/// Simplification: targets any nearby land tile flagged TileFlag.Wet ("Water? Or mud?...
	/// probably the first" per that flag's own doc comment) - this engine has no per-tile water-
	/// body registry, same simplification basis as Mining's TileFlag.Impassable/Lumberjacking's
	/// TileFlag.Foliage.
	/// </summary>
	[ViewableClass]
	public class FishingSkillDef : SkillDef {
		private const int MaxReach = 2;

		public FishingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				//#370 - mandatory tool, same consistency decision as Mining's own pickaxe gate.
				if (!Equippable.IsHoldingToolFor(self, SkillName.Fishing)) {
					self.SysMessage(Loc<FishingLoc>.Get(self.Language).needFishingPole);
					return TriggerResult.Cancel;
				}
				self.SysMessage(Loc<FishingLoc>.Get(self.Language).whereToFish);
				self.Target(SingletonScript<Targ_Fish>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			//#271 - PhaseStart already set the generic curve-based delay just before this trigger
			//fired; overwrite it with the real anti-macro formula (flat 20s, reduced above skill
			//100.0%), same wiring as MiningSkillDef.On_Start (#336) / LumberjackingSkillDef (#424)
			//- the formula itself is shared/generic in GatheringDelaySystem, just needed this call
			//site.
			skillSeqArgs.DelayInSeconds = GatheringDelaySystem.GetDelaySeconds(skillSeqArgs.Self, SkillName.Fishing);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			IPoint3D targetted = skillSeqArgs.Target1;

			if (!IsValidWater(self, targetted)) {
				skillSeqArgs.PhaseAbort();
				return TriggerResult.Cancel;
			}

			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
			return TriggerResult.Continue;
		}

		internal static bool IsValidWater(Character self, IPoint3D targetted) {
			if ((targetted == null) || (Point2D.GetSimpleDistance(self, targetted) > MaxReach)) {
				return false;
			}
			if (!self.CanInteractWith(targetted).Allow) {
				return false;
			}
			int landTile = self.GetMap().GetTileId(targetted.X, targetted.Y);
			return TileData.HasFlag(TileData.GetTileFlags(landTile), TileFlag.Wet);
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			//#271 - fish-variety roll, with the fishing-unique junk-fish/treasure-map sub-mechanic.
			FishingCatchSystem.CatchResult result = FishingCatchSystem.Catch(self);
			if (result.ItemDef != null) {
				result.ItemDef.Create(self.Backpack);
			}

			switch (result.Kind) {
				case FishingCatchSystem.CatchKind.JunkCrateWithMap:
					if (result.BonusMapDef != null) {
						result.BonusMapDef.Create(self.Backpack);
					}
					self.SysMessage(Loc<FishingLoc>.Get(self.Language).junkCrateWithMap);
					break;
				case FishingCatchSystem.CatchKind.JunkCrate:
					self.SysMessage(Loc<FishingLoc>.Get(self.Language).junkCrate);
					break;
				default:
					self.SysMessage(Loc<FishingLoc>.Get(self.Language).success);
					break;
			}

			FishingAmbushSystem.TryAmbush(self.P()); //#271
			SkillGainSystem.TryGainSkill(self, SkillName.Fishing); //#328
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).fail);
			FishingAmbushSystem.TryAmbush(self.P()); //#271 - real fixresource rolls this on every gather action, not just success
			SkillGainSystem.TryGainSkill(self, SkillName.Fishing); //#328 - real Erebor gains on fail too
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).abort);
		}
	}

	public class Targ_Fish : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			if (!FishingSkillDef.IsValidWater(self, targetted)) {
				self.SysMessage(Loc<FishingLoc>.Get(self.Language).notWater);
				return TargetResult.Done; //#391 - show the message once and stop, don't loop
			}
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).notWater);
			return TargetResult.Done; //#391
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).notWater);
			return TargetResult.Done; //#391
		}
	}

	public class FishingLoc : CompiledLocStringCollection<FishingLoc> {
		public string whereToFish = "Where do you want to fish?";
		public string notWater = "There is no fish to be found there.";
		public string fail = "You feel a tug, but your line comes up empty.";
		public string success = "You catch a fish.";
		public string abort = "You stop fishing.";
		public string needFishingPole = "You must be holding a fishing pole to do that."; //#370
		public string junkCrate = "You haul up a soggy crate of junk."; //#271
		public string junkCrateWithMap = "You haul up a soggy crate of junk - and find a tattered map inside!"; //#271
	}
}
