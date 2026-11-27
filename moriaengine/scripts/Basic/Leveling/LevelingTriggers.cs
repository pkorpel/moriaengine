using System;
using System.Globalization;
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
	/// a loss (a hypnosis paying back less than nothing). Argv is (BaseAmount, Amount): #1558 - a scripted handler changes
	/// Amount by setting argv[1].</summary>
	public class GainExperienceArgs : ScriptArgs {
		private readonly int baseAmount, originalAmount;

		public GainExperienceArgs(int baseAmount, int amount)
			: base(baseAmount, amount) {
			this.baseAmount = baseAmount;
			this.originalAmount = amount;
		}

		/// <summary>The gain before any bonus: what bonuses take their percent of.</summary>
		public int BaseAmount {
			get {
				return this.baseAmount;
			}
		}

		/// <summary>A script that set argv[1] to something that isn't a number leaves the gain unchanged.</summary>
		public int Amount {
			get {
				try {
					return Convert.ToInt32(this.Argv[1], CultureInfo.InvariantCulture);
				} catch (Exception e) when ((e is FormatException) || (e is InvalidCastException) || (e is OverflowException)) {
					return this.originalAmount;
				}
			}
			set {
				this.Argv[1] = value;
			}
		}
	}
}
