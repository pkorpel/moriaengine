using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1090 - Erebor's player boosters (questy/System/boosters.scp): more experience for a while. Six kinds, +25% or
	/// +50% for 1, 2 or 4 hours. A player keeps unused ones as counts, which never expire, and runs one at a time from the
	/// journal's Boostery tab; the bonus is the percent of each experience gain (Erebor's addexp). Erebor's sources were
	/// conquest (ConquestRewards, #1116) and the gq058/mq057 quests, not ported; a GM's .AddBooster gives one too.</summary>
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
		/// <summary>The stored boosters, in the order they were first had: (kind, count). #1471 - kept on BoosterPlugin.</summary>
		internal static List<KeyValuePair<int, int>> Stored(Player player) {
			List<KeyValuePair<int, int>> stored = new List<KeyValuePair<int, int>>();
			BoosterPlugin plugin = BoosterPlugin.Of(player);
			if ((plugin != null) && (plugin.stored is string text)) {
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
				BoosterPlugin plugin = BoosterPlugin.Of(player);
				if (plugin != null) {
					plugin.stored = null;
					plugin.DropIfEmpty();
				}
			} else {
				BoosterPlugin.Ensure(player).stored = string.Join(",", parts);
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

		/// <summary>The running booster (BoosterPlugin.running, its time the plugin's Timer), or null.</summary>
		internal static BoosterPlugin Active(Player player) {
			BoosterPlugin plugin = BoosterPlugin.Of(player);
			return ((plugin != null) && plugin.IsRunning) ? plugin : null;
		}

		/// <summary>For tests and GMs: every booster of a player gone, stored, running or paused.</summary>
		internal static void Clear(Player player) {
			player.DeletePlugin(BoosterPlugin.Key);
		}

		/// <summary>playerBooster_use: refused, silently, while one runs; else it starts and the row loses one.</summary>
		internal static bool Use(Player player, int row) {
			List<KeyValuePair<int, int>> stored = Stored(player);
			BoosterPlugin plugin = BoosterPlugin.Of(player);
			if ((Active(player) != null) || ((plugin != null) && (plugin.pausedLeft > 0)) || (row < 0) || (row >= stored.Count)) {
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
			BoosterPlugin plugin = BoosterPlugin.Ensure(player);
			plugin.running = kind;
			plugin.pausedLeft = 0;
			plugin.Timer = seconds;
		}

		/// <summary>Sphere doesn't tick an offline character, so a booster's hours count only while its player plays
		/// (BoosterPlugin's @logout).</summary>
		internal static void Pause(Player player) {
			BoosterPlugin active = Active(player);
			if (active != null) {
				active.pausedLeft = Math.Max(1, (int) active.Timer);
				active.TimerObject.Delete();
			}
		}

		/// <summary>BoosterPlugin's @login.</summary>
		internal static void Resume(Player player) {
			BoosterPlugin plugin = BoosterPlugin.Of(player);
			if ((plugin != null) && (plugin.running >= 0) && (plugin.pausedLeft > 0) && (Active(player) == null)) {
				Start(player, plugin.running, plugin.pausedLeft);
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

		/// <summary>A GM gives one, beside conquest's (gq058/mq057 aren't ported). .AddBooster kind (0-5), then the player.</summary>
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
}
