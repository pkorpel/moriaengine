using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1023 - Erebor's t_item_in_corpse (sphereevents.scp) on a player's body, the user's call: its limits, with
	/// #918's realm rule for the crime (Notoriety.OnLooting) instead of Erebor's guild one. Nothing can be taken for the
	/// first seconds after the death; each pick holds the looter still a moment, picks adding up (i_looter_immobilizer); one
	/// item per 0.8 s ("Ne tak rychle!", the clock running again on a refused pick too); and below Grandmaster nobody fresh
	/// from a death of their own (i_checkstats_timer) may take from another's body. GMs and monsters' bodies are free.
	/// The numbers are on .settings (Notoriety).</summary>
	public static class CorpseLooting {
		private static readonly TagKey nextLootTK = TagKey.Acquire("_nextLoot_");
		private static readonly TagKey holdUntilTK = TagKey.Acquire("_lootHoldUntil_");

		/// <summary>Why `looter` can't take from `body` now, or null.</summary>
		public static string Refusal(Character looter, Corpse body) {
			if (looter.IsGM || !(body.Owner is Player owner)) {
				return null;
			}
			NotorietySettings settings = NotorietySettings.instance;
			if (body.SecondsDead < settings.lootWaitSeconds) {
				return "Z tohoto tela jeste nemuzes nic brat.";
			}
			double now = Globals.TimeInSeconds;
			looter.SetTag(holdUntilTK, System.Math.Max(now, HoldUntil(looter)) + settings.lootHoldSeconds);
			if ((looter != owner) && (looter is Player player) && (player.SkillRank < SkillRank.Grandmaster) && LevelingSystem.IsRecentlyDead(player)) {
				return "Jeste nemuzes obirat cizi mrtvoly.";
			}
			bool tooSoon = (looter.GetTag(nextLootTK) is double next) && (now < next);
			looter.SetTag(nextLootTK, now + settings.lootRateSeconds);
			return tooSoon ? "Ne tak rychle!" : null;
		}

		private static double HoldUntil(Character ch) {
			return (ch.GetTag(holdUntilTK) is double until) ? until : 0;
		}

		/// <summary>Character.CheckCanMove: still held by a pick.</summary>
		internal static bool CheckCanMove(Character ch) {
			return HoldUntil(ch) <= Globals.TimeInSeconds;
		}
	}
}
