using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1260 - paid work at a venue for stamina (#1258's bellows, generalised): labourStamina taken, labourWage paid, a rest
	/// of labourMinutes real minutes for each kind of work, at most labourPerDay turns a real day of all kinds together (a player
	/// can't go round four kinds for four days' wages), and friendship +1 with the resident of the day's first turn.</summary>
	public static class VenueLabour {
		/// <summary>What the resident says: resting, too tired, the day's turns done, and thanks (one at random).</summary>
		public sealed class Lines {
			public string Rest, Tired, Done;
			public string[] Thanks;
		}

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		internal static TagKey AtKey(string kind) {
			return TagKey.Acquire("_labour_" + kind + "_at_");
		}

		internal static readonly TagKey DayTK = TagKey.Acquire("_labour_day_");
		private static readonly TagKey countTK = TagKey.Acquire("_labour_count_");

		/// <summary>True when the turn was worked and paid.</summary>
		public static bool Work(Player player, NPC npc, Item gem, string kind, Lines lines) {
			long now = Errands.Now.Ticks;
			if ((player.GetTag(AtKey(kind)) is long at) && ((now - at) < Settings.labourMinutes * TimeSpan.TicksPerMinute)) {
				npc.Say(lines.Rest);
				return false;
			}
			bool newDay = !((player.GetTag(DayTK) is long day) && ((now - day) < TimeSpan.TicksPerDay));
			int today = newDay ? 0 : ((player.GetTag(countTK) is int c) ? c : 0);
			if (today >= Settings.labourPerDay) {
				npc.Say(lines.Done);
				return false;
			}
			if (player.Stam < Settings.labourStamina) {
				npc.Say(lines.Tired);
				return false;
			}
			player.Stam = (short) (player.Stam - Settings.labourStamina);
			player.SetTag(AtKey(kind), now);
			player.SetTag(countTK, today + 1);
			if (newDay) {
				player.SetTag(DayTK, now);
				string id = Residents.IdOf(gem);
				if (id != null) {
					Residents.AddFriendship(player, id, 1);
				}
			}
			GoldUtils.Grant(player, (uint) Math.Max(0, Settings.labourWage));
			npc.Say(ErrandContext.Pick(lines.Thanks));
			return true;
		}

		/// <summary>Tests: a kind's rest, and the day's count of all labour.</summary>
		public static void Forget(Player player, string kind) {
			player.RemoveTag(AtKey(kind));
			player.RemoveTag(DayTK);
			player.RemoveTag(countTK);
		}
	}
}
