using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's conquest rewards (dobyvani-palantir.scp's f_dobyvani_bonus_gain_point, f_dobyvani_GiveBonus):
	/// while a war runs, a player's double click on its palantir registers them in it, and PvP fights add points (2 for each
	/// fighter of a death that counts, Erebor's f_countfight; the killing blow's 1 only registers, as on Erebor). When the war
	/// ends, taken or defended, each registered player, the winners with a point more, gets a booster: +25% for 2 h from 7
	/// points, for 1 h from 3. Craftsmen take no part ("Jako Kraft se nemuzes aktivne zucastnit Dobyvani.").
	/// Fixed from Erebor: each running war keeps its own participants (Erebor had one for both realms); the craftsman's refusal
	/// is said only while a war runs (Erebor said it on every PvP point); a war lord's cancel of a running war gives the
	/// defenders their bonus (Erebor skipped it, see Conquest).</summary>
	public static class ConquestRewards {
		public const int SmallBooster = 0, BigBooster = 1, SmallPoints = 3, BigPoints = 7;

		/// <summary>f_dobyvani_bonus_gain_point in every running war.</summary>
		public static void GainPoint(Player player, int points) {
			foreach (Realm realm in new[] { Realm.Gondor, Realm.Mordor }) {
				WarFront front = Conquest.Front(realm);
				if (front.IsRunning) {
					GainPoint(player, points, front);
				}
			}
		}

		/// <summary>In one war: the first registers with 1 point, more than 1 adds them.</summary>
		public static void GainPoint(Player player, int points, WarFront front) {
			if (!front.IsRunning) {
				return;
			}
			if (Professions.HasProfession(player, Professions.Craftsman)) {
				player.RedMessage("Jako Kraft se nemuzes aktivne zucastnit Dobyvani.");
				return;
			}
			WarParticipant entry = front.participants.Find(p => p.player == player);
			if (entry == null) {
				entry = new WarParticipant { player = player, points = 1 };
				front.participants.Add(entry);
				player.SysMessage("Tva ucast v Dobyvani byla zaznamenana.");
				Logger.WriteInfo(true, "Conquest: " + player.Name + " takes part in the war on " + front.declaredOn);
			}
			if (points > 1) {
				entry.points += points;
			}
		}

		/// <summary>f_dobyvani_GiveBonus: at a war's end, its participants' boosters; the winners have a point more.</summary>
		public static void GiveBonus(WarFront front, Realm winner) {
			foreach (WarParticipant entry in front.participants) {
				Player player = entry.player;
				if ((player == null) || player.IsDeleted || player.IsGM) {
					continue;
				}
				int points = entry.points + ((player.Realm == winner) ? 1 : 0);
				int kind = (points >= BigPoints) ? BigBooster : ((points >= SmallPoints) ? SmallBooster : -1);
				if (kind < 0) {
					Logger.WriteInfo(true, "Conquest: " + player.Name + " didn't get enough points (" + points + ") for a reward");
					continue;
				}
				Boosters.Add(player, kind);
				player.DelayedMessage("Ziskal jsi za ucast v dobyvani odmenu booster " + Boosters.Kinds[kind].Description);
				Logger.WriteInfo(true, "Conquest: " + player.Name + " got the booster " + Boosters.Kinds[kind].Description + " for " + points + " points");
			}
			front.participants.Clear();
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class WarParticipant {
		[SaveableData, CopyableData]
		public Player player;

		[SaveableData, CopyableData]
		public int points;

		[LoadingInitializer, DeepCopyImplementation]
		public WarParticipant() {
		}
	}
}
