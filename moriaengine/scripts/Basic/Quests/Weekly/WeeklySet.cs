using System.Collections.Generic;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1090 - one task of a weekly set: Erebor's quest_* tags of one slot of i_memory_weekly_quest.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class WeeklyTask {
		[SaveableData, CopyableData]
		public int Pool, Id;

		/// <summary>The trigger it listens to, one of WeeklyQuests' kinds.</summary>
		[SaveableData, CopyableData]
		public string Kind;

		[SaveableData, CopyableData]
		public int Total, Done;

		[SaveableData, CopyableData]
		public string Description;

		/// <summary>What counts: a defname, a typedef, a skill, a damage type or a number, by kind.</summary>
		[SaveableData, CopyableData]
		public string Subject;

		[SaveableData, CopyableData]
		public int Icon, MoveX, MoveY;

		[SaveableData, CopyableData]
		public int RewGold, RewXP, RewMI;

		/// <summary>KillStreak: the kills of the running streak.</summary>
		[SaveableData, CopyableData]
		public int Streak;

		[LoadingInitializer, DeepCopyImplementation]
		public WeeklyTask() {
		}

		public bool IsDone {
			get { return this.Done >= this.Total; }
		}
	}

	/// <summary>#1090 - a player's weekly set, Erebor's i_memory_weekly_quest: its tasks, when it ends (absolute time here,
	/// Erebor's serv.time had to be fixed up by a tool whenever the server clock moved), and the reward for all of it.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class WeeklySet {
		[SaveableData, CopyableData]
		public List<WeeklyTask> Tasks = new List<WeeklyTask>();

		[SaveableData, CopyableData]
		public long ReceivedTicks, ExpiresTicks;

		[SaveableData, CopyableData]
		public int PlayerLevel;

		[SaveableData, CopyableData]
		public int RewGold, RewXP, RewMI;

		/// <summary>KillStreak_time: until when the next kill continues a streak.</summary>
		[SaveableData, CopyableData]
		public long StreakUntilTicks;

		[LoadingInitializer, DeepCopyImplementation]
		public WeeklySet() {
		}

		public bool AllDone {
			get { return this.Tasks.TrueForAll(task => task.IsDone); }
		}
	}
}
