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
	/// Mining (enhancements #21) - was a shell with only an @Fail message, no success path at
	/// all. Modeled on LockpickSkillDef's shape (target -> stroke check -> success/fail), since
	/// that's the simplest existing example of a single-check, non-combat skill.
	///
	/// Simplification: real UO identifies specific "ore vein" map tiles; this engine has no such
	/// per-tile vein registry (only generic land-tile flags from tiledata.mul). Rather than guess
	/// at a vein-tile-ID whitelist with no way to verify it, mining works on any nearby impassable
	/// land tile (TileFlag.Impassable - mountains/rock per tiledata.mul's own flag naming), which
	/// is the closest real signal this engine has for "rocky terrain." No per-tile depletion/
	/// respawn system - the skill's own stroke delay is the only throttle, same simplification
	/// basis as Lumberjacking.
	/// </summary>
	[ViewableClass]
	public class MiningSkillDef : SkillDef {
		private const int MaxReach = 2;

		public MiningSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.SysMessage(Loc<MiningLoc>.Get(self.Language).whereToMine);
				self.Target(SingletonScript<Targ_Mine>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			IPoint3D targetted = skillSeqArgs.Target1;

			if (!IsValidVein(self, targetted)) {
				skillSeqArgs.PhaseAbort();
				return TriggerResult.Cancel;
			}

			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));
			return TriggerResult.Continue;
		}

		internal static bool IsValidVein(Character self, IPoint3D targetted) {
			if ((targetted == null) || (Point2D.GetSimpleDistance(self, targetted) > MaxReach)) {
				return false;
			}
			if (!self.CanInteractWith(targetted).Allow) {
				return false;
			}
			int landTile = self.GetMap().GetTileId(targetted.X, targetted.Y);
			return TileData.HasFlag(TileData.GetTileFlags(landTile), TileFlag.Impassable);
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			IPoint3D targetted = skillSeqArgs.Target1;

			//#333 - real per-vein ore-tier system (lazy per-tile veins, weighted roll on an
			//untouched vein, skill-gated downgrade to copper, depletion/regen)
			ItemDef def = MiningVeinSystem.Mine(self, (byte) self.GetMap().M, targetted.X, targetted.Y);
			if (def != null) {
				def.Create(self.Backpack);
				self.SysMessage(Loc<MiningLoc>.Get(self.Language).success);
			} else {
				self.SysMessage(Loc<MiningLoc>.Get(self.Language).veinExhausted);
			}
			SkillGainSystem.TryGainSkill(self, SkillName.Mining); //#328
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<MiningLoc>.Get(self.Language).fail);
			SkillGainSystem.TryGainSkill(self, SkillName.Mining); //#328 - real Erebor gains on fail too
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<MiningLoc>.Get(self.Language).abort);
		}
	}

	public class Targ_Mine : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			if (!MiningSkillDef.IsValidVein(self, targetted)) {
				self.SysMessage(Loc<MiningLoc>.Get(self.Language).notAVein);
				return TargetResult.RestartTargetting;
			}
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage(Loc<MiningLoc>.Get(self.Language).notAVein);
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage(Loc<MiningLoc>.Get(self.Language).notAVein);
			return TargetResult.RestartTargetting;
		}
	}

	public class MiningLoc : CompiledLocStringCollection<MiningLoc> {
		public string whereToMine = "Where do you want to mine?";
		public string notAVein = "There is no ore to be found there.";
		public string fail = "You loosen some rocks but fail to find any useable ore.";
		public string success = "You dig some ore.";
		public string abort = "You stop mining.";
		public string veinExhausted = "This vein seems to be exhausted."; //#333
	}
}
