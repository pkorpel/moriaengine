using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1416 - @killCredit: a fighter credited with a death (Erebor's trigger_kill, @playerKill on a player and
	/// @npckill on an NPC, with added_experience_bydeath).</summary>
	public class KillCreditArgs : ScriptArgs {
		public readonly Character victim;
		/// <summary>The fighter's share of the experience: what a player is given for it (a hypnotized one later), 0 where
		/// nothing is paid (a monster worth nothing, a player killed by their own realm, a craftsman, an immune or
		/// experience-less victim). An NPC fighter is given nothing whatever its share.</summary>
		public readonly int share;

		public KillCreditArgs(Character victim, int share)
			: base(victim, share) {
			this.victim = victim;
			this.share = share;
		}
	}

	/// <summary>#1416 - @gainExperience: a player about to gain (or lose) experience through LevelingSystem.AddExperience.
	/// Amount is what will be added, the inline bonuses (boosters, RP rank, the beacons) already in it; a handler may
	/// change it, adding to it a share of BaseAmount as those do, or cancel, and nothing is added. Both are negative for
	/// a loss (a hypnosis paying back less than nothing). Compiled handlers only: a scripted one sees the numbers in Argv
	/// and can't change Amount.</summary>
	public class GainExperienceArgs : ScriptArgs {
		private readonly int baseAmount;
		private int amount;

		public GainExperienceArgs(int baseAmount, int amount)
			: base(baseAmount, amount) {
			this.baseAmount = baseAmount;
			this.amount = amount;
		}

		/// <summary>The gain before any bonus: what bonuses take their percent of.</summary>
		public int BaseAmount {
			get {
				return this.baseAmount;
			}
		}

		public int Amount {
			get {
				return this.amount;
			}
			set {
				this.amount = value;
			}
		}
	}

	public static class LevelingTriggers {
		private static readonly TriggerKey killCreditTK = TriggerKey.Acquire("killCredit");
		private static readonly TriggerKey gainExperienceTK = TriggerKey.Acquire("gainExperience");

		/// <summary>@killCredit on a fighter (its trigger groups and plugins), player or NPC.</summary>
		public static void KillCredit(Character fighter, Character victim, int share) {
			if ((fighter == null) || fighter.IsDeleted) {
				return;
			}
			fighter.TryTrigger(killCreditTK, new KillCreditArgs(victim, share));
		}

		/// <summary>@gainExperience on the player; the amount to add, or null when a handler cancelled.</summary>
		public static int? GainExperience(Player player, int baseAmount, int amount) {
			GainExperienceArgs args = new GainExperienceArgs(baseAmount, amount);
			if (player.TryCancellableTrigger(gainExperienceTK, args) == TriggerResult.Cancel) {
				return null;
			}
			return args.Amount;
		}
	}
}
