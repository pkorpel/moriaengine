using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's systems/skills/lockpicking.scp. A lock is the container's type, t_container_locked, and how hard
	/// it is its tag.lockpick. A pick is used up per try, or wears from 3 tries to 2 to 1, and the answer comes 2 s later. It runs
	/// by its own timer, not as a skill stroke, so on Erebor nothing else in between stops it. The skill list has no button for it.</summary>
	[ViewableClass]
	public class LockpickSkillDef : SkillDef {
		public LockpickSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(500014); //That skill cannot be used directly.
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

	public class t_lockpick : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character clicker) {
			Player player = clicker as Player;
			if ((player != null) && !player.Flag_Dead) {
				player.Target(SingletonScript<Targ_Lockpick>.Instance);
			}
			return TriggerResult.Cancel;
		}
	}

	public class Targ_Lockpick : CompiledTargetDef {
		private const int Reach = 2, PickSound = 577;
		private const double SecondsToPick = 2;

		//one that's nearly worn out gets used first
		private static readonly string[] picks = { "i_lockpick", "i_lockpick_2", "i_lockpick_3" };

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<LockpickLoc>.Get(self.Language).whatToOpen);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			int pick = Array.FindIndex(picks, defname => ClothMaking.Count(self.Backpack, (ItemDef) ThingDef.GetByDefname(defname)) > 0);
			if (pick < 0) {
				return TargetResult.Done;
			}
			LockpickLoc texts = Loc<LockpickLoc>.Get(self.Language);
			if (self.GetTimer(LockpickTimer.Key) != null) {
				self.SysMessage(texts.takesPatience);
			} else if ((Point2D.GetSimpleDistance(self, targetted.TopPoint) > Reach) || !self.CanSeeLOS(targetted).Allow) {
				self.SysMessage(texts.tooFar);
			} else if (WorkLight.CheckWithMessage(self)) {
				if (CraftingSkillDef.IsOfType(targetted, "t_container_locked", "t_ship_hold_lock")) {
					this.Begin(self, targetted, pick);
				} else {
					self.SysMessage(texts.cantUseLockpick);
				}
			}
			return TargetResult.Done;
		}

		private void Begin(Player self, Item locked, int pick) {
			PlayerHunger.HurtIfStarving(self);
			self.Flag_Hidden = false;
			self.Flag_InvisByMagic = false;
			self.Sound(PickSound);
			IResourceListEntry missing;
			ResourcesListParser.Parse("1 " + picks[pick]).ConsumeResourcesOnce(self, ResourcesLocality.Backpack, out missing);
			if (pick > 0) {
				ThingDef.GetByDefname(picks[pick - 1]).Create(self.Backpack);
			}
			LockpickTimer timer = new LockpickTimer();
			timer.target = locked;
			self.AddTimer(LockpickTimer.Key, timer).DueInSeconds = SecondsToPick;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class LockpickTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_lockpicking_");

		private const int LeastRollOnHold = 300, MostRoll = 1000;
		private static readonly TagKey difficultyTK = TagKey.Acquire("lockpick");
		private static readonly TagKey pickedTK = TagKey.Acquire("lockpicked");

		[SaveableData, CopyableData]
		public Item target;

		[LoadingInitializer, DeepCopyImplementation]
		public LockpickTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if ((this.target == null) || this.target.IsDeleted) {
				return;
			}
			LockpickLoc texts = Loc<LockpickLoc>.Get(self.Language);
			int skill = self.GetSkill(SkillName.Lockpicking);
			bool gain = true;
			if (CraftingSkillDef.IsOfType(this.target, "t_ship_hold_lock")) {
				if (Globals.dice.Next(LeastRollOnHold, MostRoll + 1) < skill) {
					this.target.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_ship_hold");
					self.SysMessage(self.IsFemale ? texts.holdOpenedFemale : texts.holdOpenedMale);
				} else {
					self.SysMessage(texts.lockHeld);
					gain = false;
				}
			} else if (Globals.dice.Next(MostRoll + 1) < (skill - DifficultyOf(this.target))) {
				this.target.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_container");
				this.target.SetTag(pickedTK, 1);
				self.SysMessage(self.IsFemale ? texts.openedFemale : texts.openedMale);
			} else {
				self.SysMessage(texts.lockHeld);
			}
			if (gain) {
				SkillGainSystem.TryGainSkillCustomFormula(self, SkillName.Lockpicking, SkillAdvancementSettings.instance.lockpickingAdvRate);
			}
		}

		private static int DifficultyOf(Item locked) {
			object difficulty = locked.GetTag(difficultyTK);
			return (difficulty == null) ? 0 : Convert.ToInt32(difficulty, System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	internal class LockpickLoc : CompiledLocStringCollection<LockpickLoc> {
		public string whatToOpen = "Co chces odemknout?";
		public string takesPatience = "Tahle prace vyzaduje trpelivost!";
		public string tooFar = "Jsi moc daleko.";
		public string cantUseLockpick = "Na tohle nelze pouzit paklic.";
		public string lockHeld = "Zamek odolal.";
		public string openedMale = "Odemkl jsi zamek!";
		public string openedFemale = "Odemkla jsi zamek!";
		public string holdOpenedMale = "Odemkl jsi podpalubi!";
		public string holdOpenedFemale = "Odemkla jsi podpalubi!";
	}
}
