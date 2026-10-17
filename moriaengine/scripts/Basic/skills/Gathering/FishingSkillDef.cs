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
	/// Fishing (#328) - had no implementation at all before this (unlike Mining/Lumberjacking,
	/// which were already minimal shells). Minimal shell only, modeled directly on
	/// MiningSkillDef's shape - just enough to have a real On_Success/On_Fail hook for
	/// skill-gain-through-use (#140/#328) to attach to. The real resource-node system (material
	/// tiering, junk-fish/treasure-map sub-mechanic) is explicitly out of scope here - Mining's
	/// own equivalent full system is tracked separately (#332-338), nobody's asked for the same
	/// for Fishing yet.
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
				self.SysMessage(Loc<FishingLoc>.Get(self.Language).whereToFish);
				self.Target(SingletonScript<Targ_Fish>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
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

		private static ItemDef fishDef;
		private static ItemDef FishDef {
			get {
				if (fishDef == null) {
					fishDef = (ItemDef) ThingDef.GetByDefname("i_fish_raw_whole");
				}
				return fishDef;
			}
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			ItemDef def = FishDef;
			if (def != null) {
				def.Create(self.Backpack);
			}
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).success);
			SkillGainSystem.TryGainSkill(self, SkillName.Fishing); //#328
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).fail);
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
				return TargetResult.RestartTargetting;
			}
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).notWater);
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage(Loc<FishingLoc>.Get(self.Language).notWater);
			return TargetResult.RestartTargetting;
		}
	}

	public class FishingLoc : CompiledLocStringCollection<FishingLoc> {
		public string whereToFish = "Where do you want to fish?";
		public string notWater = "There is no fish to be found there.";
		public string fail = "You feel a tug, but your line comes up empty.";
		public string success = "You catch a fish.";
		public string abort = "You stop fishing.";
	}
}
