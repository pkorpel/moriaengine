using System;
using System.Collections.Generic;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1260 - a kind of stay in a venue (#1258's vigil, generalised): how long, what the resident says at the start, and
	/// the reward at the end.</summary>
	public sealed class StayKind {
		public readonly string Key, Start;
		public readonly Func<int> Seconds;
		public readonly Action<Player, CityVenue, string> Reward; //the player, the venue, the resident's id (may be null)

		public StayKind(string key, Func<int> seconds, string start, Action<Player, CityVenue, string> reward) {
			this.Key = key;
			this.Seconds = seconds;
			this.Start = start;
			this.Reward = reward;
		}
	}

	/// <summary>#1260 - staying in a venue for a while: one stay at a time per player, each kind once a real day; leaving the
	/// venue, dying or logging out breaks it with no reward. The temple's vigil, the market stall, the night sentry.</summary>
	public static class VenueStay {
		private static readonly Dictionary<string, StayKind> kinds = new Dictionary<string, StayKind>(StringComparer.Ordinal);

		public static void Register(StayKind kind) {
			kinds[kind.Key] = kind;
		}

		public static StayKind Get(string key) {
			return (key != null) && kinds.TryGetValue(key, out StayKind kind) ? kind : null;
		}

		internal static TagKey DoneKey(string kind) {
			return TagKey.Acquire("_stay_" + kind + "_done_");
		}

		public static bool IsStaying(Player player) {
			return player.GetTimer(VenueStayTimer.Key) != null;
		}

		public static bool DoneToday(Player player, string kind) {
			return (player.GetTag(DoneKey(kind)) is long at) && ((Errands.Now.Ticks - at) < TimeSpan.TicksPerDay);
		}

		/// <summary>The resident's line starting a stay in their workplace; false (and told why) when it can't start.</summary>
		public static bool Begin(Player player, NPC npc, Item gem, string kindKey) {
			StayKind kind = Get(kindKey);
			CityVenue venue = Residents.WorkplaceOf(gem);
			if ((kind == null) || (venue == null)) {
				return false;
			}
			if (IsStaying(player)) {
				npc.Say("Uz mas co delat. Nejdriv dokonci to.");
				return false;
			}
			if (DoneToday(player, kindKey)) {
				npc.Say("Dnes uz to mas za sebou. Prijd zitra.");
				return false;
			}
			if (VenueRegistry.At(player) != venue) {
				npc.Say("Pojd dovnitr, tady to bude.");
				return false;
			}
			VenueStayTimer timer = new VenueStayTimer { kind = kindKey, venueId = venue.VenueId, residentId = Residents.IdOf(gem), left = Math.Max(1, kind.Seconds()) };
			player.AddTimer(VenueStayTimer.Key, timer);
			timer.DueInSeconds = VenueStayTimer.StepSeconds;
			timer.PeriodInSeconds = VenueStayTimer.StepSeconds;
			npc.Say(kind.Start);
			return true;
		}
	}

	/// <summary>#1260 - a stay under way: every StepSeconds the player must still be in the venue, alive and online; at the end
	/// its kind's reward, and the kind is done for the day.</summary>
	[SaveableClass, DeepCopyableClass]
	public class VenueStayTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_venue_stay_");
		public const int StepSeconds = 10;

		[SaveableData, CopyableData]
		public string kind, venueId, residentId;

		[SaveableData, CopyableData]
		public int left;

		[LoadingInitializer, DeepCopyImplementation]
		public VenueStayTimer() {
		}

		/// <summary>The stay after this many seconds; false once it has ended, either way.</summary>
		public bool Step(Player player, int seconds) {
			CityVenue venue = VenueRegistry.ById(this.venueId);
			StayKind stay = VenueStay.Get(this.kind);
			if ((player == null) || (venue == null) || (stay == null)) {
				return false;
			}
			if (player.Flag_Disconnected || player.Flag_Dead || (VenueRegistry.At(player) != venue)) {
				player.SysMessage("Preruseno: uz nejsi na svem miste.");
				return false;
			}
			this.left -= seconds;
			if (this.left > 0) {
				return true;
			}
			player.SetTag(VenueStay.DoneKey(this.kind), Errands.Now.Ticks);
			stay.Reward(player, venue, this.residentId);
			return false;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.Step(cont as Player, StepSeconds)) {
				this.Delete();
			}
		}
	}
}
