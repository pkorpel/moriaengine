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
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Sphere's Cartography wants blank parchment and then opens sm_cartography, which Erebor's scripts don't
	/// have: the button did nothing else there.</summary>
	[ViewableClass]
	public class CartographySkillDef : SkillDef {
		public CartographySkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			foreach (AbstractItem item in self.Backpack) {
				if (CraftingSkillDef.IsOfType(item as Item, "t_map_blank")) {
					return TriggerResult.Cancel;
				}
			}
			self.SysMessage("You have no blank parchment to draw on");
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}

	/// <summary>#962 - Sphere's Begging: grovel, a roll against the target's Int, and nothing for it, players never got
	/// anything from it on Erebor either. No gain by use.</summary>
	[ViewableClass]
	public class BeggingSkillDef : SkillDef {
		private const int DifficultyScale = 10;

		public BeggingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			if (skillSeqArgs.Self is Player self) {
				self.SysMessage(Loc<SmallSkillsLoc>.Get(self.Language).BegFrom);
				self.Target(SingletonScript<Targ_Begging>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = skillSeqArgs.Target1 as Character;
			if ((target == null) || (target == self)) {
				return TriggerResult.Cancel;
			}
			self.SysMessage("You grovel at " + target.Name + "'s feet");
			skillSeqArgs.Success = this.CheckSuccess(self, target.Int * DifficultyScale);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}

	public class Targ_Begging : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}
	}

	/// <summary>#962 - Sphere's Spirit Speak: a roll against 0-89, then four minutes of the netherworld. Ghosts' speech isn't
	/// garbled here, so there's nothing yet for it to open.</summary>
	[ViewableClass]
	public class SpiritSpeakSkillDef : SkillDef {
		private const int MostDifficulty = 90, DifficultyScale = 10, Seconds = 240, Connected = 502444, ConnectSound = 0x24a;

		public SpiritSpeakSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Success = this.CheckSuccess(skillSeqArgs.Self, Globals.dice.Next(MostDifficulty) * DifficultyScale);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (self.GetTimer(SpiritContactTimer.Key) == null) {
				self.ClilocSysMessage(Connected);
				self.Sound(ConnectSound);
				self.AddTimer(SpiritContactTimer.Key, new SpiritContactTimer()).DueInSeconds = Seconds;
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.MessageTo(self, Loc<SmallSkillsLoc>.Get(self.Language).WordsMakeNoSense); //#1502 - a stroke: no Globals.Src
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}

	[SteamEngine.Persistence.SaveableClass, DeepCopyableClass]
	public class SpiritContactTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_spiritContact_");

		[SteamEngine.Persistence.LoadingInitializer, DeepCopyImplementation]
		public SpiritContactTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
		}
	}

	/// <summary>#962 - Sphere's Herding: double click a crook, pick an animal and a spot, and after a roll against about
	/// its Int it walks there. A GM's creature goes at once. Its button does nothing. No gain by use.</summary>
	[ViewableClass]
	public class HerdingSkillDef : SkillDef {
		private const int DifficultyScale = 10, Convinced = 502479;

		public HerdingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			return (skillSeqArgs.Target1 != null) ? TriggerResult.Continue : TriggerResult.Cancel;
		}

		private static bool CanHerd(Character self, Character target) {
			if (self.IsGM) {
				return true;
			}
			if (target is NPC) {
				return target.IsAnimal;
			}
			return false;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = skillSeqArgs.Target1 as Character;
			if ((target == null) || target.IsDeleted) {
				self.SysMessage("You lost your target!");
				return TriggerResult.Cancel;
			}
			if (!CanHerd(self, target)) {
				self.SysMessage("They look somewhat annoyed");
				return TriggerResult.Cancel;
			}
			int half = target.Int / 2;
			skillSeqArgs.Success = this.CheckSuccess(self, (half + Globals.dice.Next(half + 1)) * DifficultyScale);
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = skillSeqArgs.Target1 as Character;
			IPoint3D where = (IPoint3D) skillSeqArgs.Param1;
			if ((target != null) && !target.IsDeleted) {
				if (self.IsGM) {
					target.P(where.X, where.Y, where.Z);
				} else if (target is NPC npc) {
					npc.PetGoalPoint = new Point4D(where.X, where.Y, where.Z, self.M);
				}
				target.ClilocMessageTo(self, Convinced, ""); //#1502 - to the herder: a stroke has no Globals.Src
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}

	public class Targ_Herd : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<SmallSkillsLoc>.Get(self.Language).WhatToHerd);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage(Loc<SmallSkillsLoc>.Get(self.Language).WhereToHerd);
			self.Target(SingletonScript<Targ_HerdDestination>.Instance, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("Try just picking up inanimate objects");
			return TargetResult.Done;
		}
	}

	public class Targ_HerdDestination : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return Herd(self, targetted, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			return Herd(self, targetted, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return Herd(self, targetted, parameter);
		}

		private static TargetResult Herd(Player self, IPoint3D spot, object parameter) {
			SkillSequenceArgs herd = SkillSequenceArgs.Acquire(self, SkillName.Herding);
			herd.Target1 = (Character) parameter;
			herd.Param1 = new Point3D(spot.X, spot.Y, spot.Z);
			herd.PhaseSelect();
			return TargetResult.Done;
		}
	}

	internal class SmallSkillsLoc : CompiledLocStringCollection<SmallSkillsLoc> {
		public string BegFrom = "U koho chces zebrat?";
		public string WordsMakeNoSense = "Slova se zdaji nesrozumitelna.";
		public string WhatToHerd = "What would you like to herd?";
		public string WhereToHerd = "Where do you want them to go ?";
		public string CantLightHere = "You can't light the kindling in a container";
		public string FireFailed = "You fail to ignite the fire.";
	}
}
