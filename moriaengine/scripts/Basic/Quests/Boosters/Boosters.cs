using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1090 - Erebor's player boosters (questy/System/boosters.scp): more experience for a while. Six kinds, +25% or
	/// +50% for 1, 2 or 4 hours. A player keeps unused ones as counts, which never expire, and runs one at a time from the
	/// journal's Boostery tab; the bonus is the percent of each experience gain (Erebor's addexp). Erebor's sources were
	/// conquest and the gq058/mq057 quests, none of them ported, so for now only a GM's .AddBooster gives one.</summary>
	public static class Boosters {
		internal sealed class Kind {
			internal readonly string Description;
			internal readonly int Percent, Hours;

			internal Kind(int percent, int hours) {
				this.Percent = percent;
				this.Hours = hours;
				this.Description = "+" + percent + "% zkusenosti na " + hours + "h";
			}
		}

		internal static readonly Kind[] Kinds = { new Kind(25, 1), new Kind(25, 2), new Kind(25, 4), new Kind(50, 1), new Kind(50, 2), new Kind(50, 4) };
		public const int Icon = 0x3300;
		private static readonly TagKey storedTK = TagKey.Acquire("_boosters_");
		private static readonly TagKey pausedTK = TagKey.Acquire("_boosterPaused_");

		/// <summary>The stored boosters, in the order they were first had: (kind, count).</summary>
		internal static List<KeyValuePair<int, int>> Stored(Player player) {
			List<KeyValuePair<int, int>> stored = new List<KeyValuePair<int, int>>();
			if (player.GetTag(storedTK) is string text) {
				foreach (string part in text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
					string[] pair = part.Split(':');
					stored.Add(new KeyValuePair<int, int>(int.Parse(pair[0], CultureInfo.InvariantCulture), int.Parse(pair[1], CultureInfo.InvariantCulture)));
				}
			}
			return stored;
		}

		private static void SetStored(Player player, List<KeyValuePair<int, int>> stored) {
			List<string> parts = new List<string>();
			foreach (KeyValuePair<int, int> row in stored) {
				parts.Add(row.Key.ToString(CultureInfo.InvariantCulture) + ":" + row.Value.ToString(CultureInfo.InvariantCulture));
			}
			if (parts.Count == 0) {
				player.RemoveTag(storedTK);
			} else {
				player.SetTag(storedTK, string.Join(",", parts));
			}
		}

		/// <summary>playerBooster_add: one more of a kind, on its own row or the row of its kind.</summary>
		public static void Add(Player player, int kind) {
			if ((kind < 0) || (kind >= Kinds.Length)) {
				return;
			}
			List<KeyValuePair<int, int>> stored = Stored(player);
			int at = stored.FindIndex(row => row.Key == kind);
			if (at < 0) {
				stored.Add(new KeyValuePair<int, int>(kind, 1));
			} else {
				stored[at] = new KeyValuePair<int, int>(kind, stored[at].Value + 1);
			}
			SetStored(player, stored);
		}

		internal static BoosterTimer Active(Player player) {
			return player.GetTimer(BoosterTimer.Key) as BoosterTimer;
		}

		/// <summary>playerBooster_use: refused, silently, while one runs; else it starts and the row loses one.</summary>
		internal static bool Use(Player player, int row) {
			List<KeyValuePair<int, int>> stored = Stored(player);
			if ((Active(player) != null) || (player.GetTag(pausedTK) != null) || (row < 0) || (row >= stored.Count)) {
				return false;
			}
			int kind = stored[row].Key;
			Start(player, kind, Kinds[kind].Hours * 3600);
			if (stored[row].Value > 1) {
				stored[row] = new KeyValuePair<int, int>(kind, stored[row].Value - 1);
			} else {
				stored.RemoveAt(row);
			}
			SetStored(player, stored);
			Logger.WriteInfo(true, "Booster: " + player.Name + " (" + player.Account?.Name + ") pouzil Booster " + Kinds[kind].Description + " na pozici " + player.P() +
				" (" + player.Region?.Name + ")");
			return true;
		}

		private static void Start(Player player, int kind, double seconds) {
			BoosterTimer timer = new BoosterTimer();
			timer.kind = kind;
			player.AddTimer(BoosterTimer.Key, timer).DueInSeconds = seconds;
		}

		/// <summary>funkce.scp's addexp: the running booster's percent of a gain, never of a loss.</summary>
		internal static int Bonus(Player player, int amount) {
			BoosterTimer active = Active(player);
			return ((active == null) || (amount <= 0)) ? 0 : (amount * Kinds[active.kind].Percent) / 100;
		}

		/// <summary>Sphere doesn't tick an offline character, so a booster's hours count only while its player plays.</summary>
		internal static void Pause(Player player) {
			BoosterTimer active = Active(player);
			if (active != null) {
				player.SetTag(pausedTK, active.kind.ToString(CultureInfo.InvariantCulture) + ":" +
					Math.Max(1, (int) active.DueInSeconds).ToString(CultureInfo.InvariantCulture));
				player.DeleteTimer(BoosterTimer.Key);
			}
		}

		internal static void Resume(Player player) {
			if ((player.GetTag(pausedTK) is string paused) && (Active(player) == null)) {
				string[] pair = paused.Split(':');
				player.RemoveTag(pausedTK);
				Start(player, int.Parse(pair[0], CultureInfo.InvariantCulture), int.Parse(pair[1], CultureInfo.InvariantCulture));
			}
		}

		/// <summary>Erebor's dsToRTime(..., 1): "1 dni 2 hod", "2 hod 5 min" or "5 min 3 sec".</summary>
		internal static string TimeText(double seconds) {
			TimeSpan left = TimeSpan.FromSeconds(Math.Max(0, seconds));
			if (left.Days > 0) {
				return left.Days + " dni " + left.Hours + " hod";
			}
			if (left.Hours > 0) {
				return left.Hours + " hod " + left.Minutes + " min";
			}
			return left.Minutes + " min " + left.Seconds + " sec";
		}

		/// <summary>No player source is ported (conquest, gq058/mq057): a GM gives one. .AddBooster kind (0-5), then the player.</summary>
		[SteamFunction]
		public static void AddBooster(Character self, ScriptArgs sa) {
			Player gm = self as Player;
			if (gm == null) {
				return;
			}
			string arg = (sa == null) ? "" : (sa.Args ?? "").Trim('"', ' ');
			if (!int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int kind) || (kind < 0) || (kind >= Kinds.Length)) {
				for (int i = 0; i < Kinds.Length; i++) {
					gm.SysMessage(".AddBooster " + i + ": " + Kinds[i].Description);
				}
				return;
			}
			gm.SysMessage("Komu dat booster " + Kinds[kind].Description + "?");
			gm.Target(SingletonScript<Targ_AddBooster>.Instance, kind);
		}
	}

	public class Targ_AddBooster : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (targetted is Player player) {
				int kind = (int) parameter;
				Boosters.Add(player, kind);
				self.SysMessage(player.Name + " dostal booster " + Boosters.Kinds[kind].Description + ".");
			}
			return TargetResult.Done;
		}
	}

	/// <summary>i_booster: the running one. Its end: "Tvuj bonus do zkusenosti vyprsel!".</summary>
	[SaveableClass, DeepCopyableClass]
	public class BoosterTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_booster_");

		[SaveableData, CopyableData]
		public int kind;

		[LoadingInitializer, DeepCopyImplementation]
		public BoosterTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character ch) {
				ch.SysMessage("Tvuj bonus do zkusenosti vyprsel!", ClassAbilities.ClassMessageHue);
			}
		}
	}
}
