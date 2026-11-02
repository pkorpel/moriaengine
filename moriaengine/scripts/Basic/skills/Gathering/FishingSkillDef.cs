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
	/// Since #813 the catch comes from the water spot's region resources (the archive's w_fish /
	/// w_jf tiers, skill-gated, per-spot pools) - see FishingCatchSystem. The treasure-map payoff
	/// of junk fish is a placeholder pending the real treasure-map system (#530).
	///
	/// Simplification: targets any nearby land tile flagged TileFlag.Wet ("Water? Or mud?...
	/// probably the first" per that flag's own doc comment) - this engine has no per-tile water-
	/// body registry, same simplification basis as Mining's TileFlag.Impassable/Lumberjacking's
	/// TileFlag.Foliage.
	/// </summary>
	[ViewableClass]
	public class FishingSkillDef : SkillDef {
		private const int MaxReach = 3, SplashSound = 0x27;

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
			Character self = skillSeqArgs.Self;
			if (!Gathering.Begin(self, skillSeqArgs.Target1, SkillName.Fishing)) {
				return TriggerResult.Cancel;
			}
			self.Sound(SplashSound);
			AnimCalculator.PerformAttackAnim(self);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			IPoint3D targetted = skillSeqArgs.Target1;

			if (!IsValidWater(self, targetted)) {
				skillSeqArgs.PhaseAbort();
				return TriggerResult.Cancel;
			}

			int difficulty = FishingCatchSystem.RollDifficulty(self.GetMap().M, targetted.X, targetted.Y);
			skillSeqArgs.Param2 = difficulty;
			skillSeqArgs.Success = (difficulty <= 0) || this.CheckSuccess(self, difficulty);
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

			//#271/#813 - what this water spot holds, from its region's t_water region type
			IPoint3D target = skillSeqArgs.Target1;
			FishingCatchSystem.CatchResult result = FishingCatchSystem.Catch(self, self.GetMap().M, target.X, target.Y);
			if (result.ItemDef != null) {
				result.ItemDef.Create(self.X, self.Y, self.Z, self.M); //Sphere's, at your feet
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
				case FishingCatchSystem.CatchKind.Nothing:
					self.SysMessage(Loc<FishingLoc>.Get(self.Language).nothingHere); //#813
					break;
				default:
					self.SysMessage(string.Format(Loc<GatheringLoc>.Get(self.Language).PulledOut, result.ItemDef?.Name));
					break;
			}

			FishingAmbushSystem.TryAmbush(self.P()); //#271
			SkillGainSystem.TryGainSkill(self, SkillName.Fishing, System.Convert.ToInt32(skillSeqArgs.Param2)); //#328
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.Message(Loc<GatheringLoc>.Get(self.Language).FishingFail);
			FishingAmbushSystem.TryAmbush(self.P()); //#271 - real fixresource rolls this on every gather action, not just success
			SkillGainSystem.TryGainSkill(self, SkillName.Fishing, -System.Convert.ToInt32(skillSeqArgs.Param2)); //#328 - real Erebor gains on fail too
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
		public string nothingHere = "The fish don't seem to be biting here."; //#813 - the spot's region roll was "nothing", or it's fished out
	}
}
