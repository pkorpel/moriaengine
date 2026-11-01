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
	[ViewableClass]
	/// <summary>#962 - raw food on a fire, as Sphere's Skill_Cooking: never through the menu, nothing on Erebor opens that one.</summary>
	public class Targ_CookOn : CompiledTargetDef {
		private static readonly string[] fires = { "t_fire", "t_forge", "t_campfire" };

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			return Cook(self, CraftingSkillDef.IsOfType(targetted as Item, fires), (Item) parameter);
		}

		protected override TargetResult On_TargonStatic(Player self, AbstractInternalItem targetted, object parameter) {
			return Cook(self, System.Array.Exists(fires, fire => GatherTiles.IsOfType(targetted, fire)), (Item) parameter);
		}

		private static TargetResult Cook(Player self, bool isFire, Item raw) {
			if (!isFire) {
				self.SysMessage(Loc<CraftSkillsLoc>.Get(self.Language).CookOnFire);
			} else if (!raw.IsDeleted) {
				SkillSequenceArgs.Acquire(self, SkillName.Cooking, raw).PhaseSelect();
			}
			return TargetResult.Done;
		}
	}

	public class CookingSkillDef : CraftingSkillDef {
		private const int MostDifficulty = 500;
		private static readonly TagKey cookedTK = TagKey.Acquire("cooked");

		private static ItemDef CookedOf(Item raw) {
			return ThingDef.GetByDefname(raw.Def.GetTag(cookedTK) as string) as ItemDef;
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			return (skillSeqArgs.Tool != null) ? TriggerResult.Continue : base.On_Select(skillSeqArgs);
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			int difficulty = Globals.dice.Next(MostDifficulty);
			skillSeqArgs.Param2 = difficulty;
			skillSeqArgs.Success = (difficulty <= 0) || this.CheckSuccess(skillSeqArgs.Self, difficulty);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item raw = skillSeqArgs.Tool;
			if ((raw != null) && !raw.IsDeleted) {
				ItemDef cooked = CookedOf(raw);
				if (cooked != null) {
					cooked.Create(self.Backpack);
					self.SysMessage(Loc<CraftSkillsLoc>.Get(self.Language).SmellsGood);
					ConsumeOne(raw);
					SkillGainSystem.TryGainSkill(self, SkillName.Cooking, System.Convert.ToInt32(skillSeqArgs.Param2));
				}
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Item raw = skillSeqArgs.Tool;
			if ((raw != null) && !raw.IsDeleted) {
				self.SysMessage(this.FailMessage(self));
				ConsumeOne(raw);
				SkillGainSystem.TryGainSkill(self, SkillName.Cooking, -System.Convert.ToInt32(skillSeqArgs.Param2));
			}
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}

		private static void ConsumeOne(Item raw) {
			if (raw.Amount > 1) {
				raw.Amount--;
			} else {
				raw.Delete();
			}
		}

		protected override string FailMessage(Character self) {
			return Loc<CraftSkillsLoc>.Get(self.Language).FailedFood;
		}

		public CookingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		//TODO - some sounds?
	}
}