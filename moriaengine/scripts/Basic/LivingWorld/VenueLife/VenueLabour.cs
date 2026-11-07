using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1260 - paid work at a venue for stamina (#1258's bellows, generalised): labourStamina taken, labourWage paid, a rest
	/// of labourMinutes real minutes, at most labourPerDay turns a real day for each kind of work, and friendship +1 with the
	/// resident of the day's first turn. Per player and kind: one cap for all of the world's smithies.</summary>
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

		internal static TagKey DayKey(string kind) {
			return TagKey.Acquire("_labour_" + kind + "_day_");
		}

		private static TagKey CountKey(string kind) {
			return TagKey.Acquire("_labour_" + kind + "_count_");
		}

		/// <summary>True when the turn was worked and paid.</summary>
		public static bool Work(Player player, NPC npc, Item gem, string kind, Lines lines) {
			long now = Errands.Now.Ticks;
			if ((player.GetTag(AtKey(kind)) is long at) && ((now - at) < Settings.labourMinutes * TimeSpan.TicksPerMinute)) {
				npc.Say(lines.Rest);
				return false;
			}
			bool newDay = !((player.GetTag(DayKey(kind)) is long day) && ((now - day) < TimeSpan.TicksPerDay));
			int today = newDay ? 0 : ((player.GetTag(CountKey(kind)) is int c) ? c : 0);
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
			player.SetTag(CountKey(kind), today + 1);
			if (newDay) {
				player.SetTag(DayKey(kind), now);
				string id = Residents.IdOf(gem);
				if (id != null) {
					Residents.AddFriendship(player, id, 1);
				}
			}
			GoldUtils.Grant(player, (uint) Math.Max(0, Settings.labourWage));
			npc.Say(ErrandContext.Pick(lines.Thanks));
			return true;
		}

		/// <summary>Tests: the rest, the day's count and the day for a kind of work.</summary>
		public static void Forget(Player player, string kind) {
			player.RemoveTag(AtKey(kind));
			player.RemoveTag(DayKey(kind));
			player.RemoveTag(CountKey(kind));
		}
	}
}
