using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#925 - Erebor's magic mounts (jezditka.scp's magic_horse, the <c>&lt;name&gt;_mif</c> tables and mount_equip).
	/// A mount whose chardef has tag(magic_horse,name) makes its mount item a magic item: its slayers, blocks, Hiding
	/// and Stealth go on the rider while riding. On top of that the rider's mount knowledge (MountKnowledge.Bonus) adds
	/// that share of the slayers again, Erebor's i_memory_taming_bonus. Both come off at the dismount.</summary>
	public static class MountPowers {
		private static readonly TagKey appliedTK = TagKey.Acquire("_mountPowers_");

		/// <summary>The _mif functions, written once from the archive: property number and value.</summary>
		private static readonly Dictionary<string, int[,]> tables = new Dictionary<string, int[,]>(StringComparer.OrdinalIgnoreCase) {
			{ "roan", new[,] { { 101, 3 }, { 102, 3 }, { 103, 3 } } },
			{ "wimmimate", new[,] { { 101, 7 }, { 104, 7 }, { 103, 7 } } },
			{ "red_roan", new[,] { { 101, 9 }, { 105, 9 }, { 103, 9 } } },
			{ "chocolate", new[,] { { 101, 11 }, { 102, 11 }, { 103, 11 } } },
			{ "pamamino", new[,] { { 101, 13 }, { 102, 13 }, { 103, 13 } } },
			{ "zostrich", new[,] { { 101, 10 }, { 102, 10 }, { 103, 10 }, { 134, 5 } } },
			{ "sky", new[,] { { 101, 15 }, { 102, 15 }, { 103, 15 } } },
			{ "grey", new[,] { { 101, 17 }, { 102, 17 }, { 103, 17 } } },
			{ "sky_grey", new[,] { { 101, 19 }, { 102, 19 }, { 103, 19 } } },
			{ "crimson", new[,] { { 101, 25 }, { 102, 25 }, { 103, 25 } } },
			{ "black", new[,] { { 101, 20 }, { 104, 20 }, { 103, 20 }, { 21, 5 }, { 47, 10 } } },
			{ "polar_bear", new[,] { { 104, 20 }, { 105, 20 } } },
			{ "zostrich_green", new[,] { { 101, 20 }, { 102, 20 }, { 103, 20 }, { 134, 8 }, { 135, 8 } } },
			{ "zostrich_red", new[,] { { 101, 25 }, { 102, 25 }, { 103, 25 }, { 134, 10 }, { 135, 10 } } },
			{ "zostrich_send", new[,] { { 101, 25 }, { 102, 25 }, { 103, 25 }, { 134, 10 }, { 135, 10 } } },
			{ "zostrich_brown", new[,] { { 101, 25 }, { 102, 25 }, { 103, 25 }, { 134, 10 }, { 135, 10 } } },
			{ "ridgeback", new[,] { { 104, 10 }, { 105, 10 }, { 103, 10 }, { 134, 2 }, { 135, 4 } } },
			{ "ridgeback_savage", new[,] { { 104, 15 }, { 105, 15 }, { 103, 15 }, { 134, 3 }, { 135, 8 } } }
		};

		/// <summary>What a mount gives a rider: the mount's own values, then the slayers again at the rider's bonus.</summary>
		internal static List<KeyValuePair<int, int>> PowersFor(Character rider, Character mount) {
			List<KeyValuePair<int, int>> powers = new List<KeyValuePair<int, int>>();
			string name = (mount.Def as NPCDef)?.MagicHorse;
			if (string.IsNullOrEmpty(name) || !tables.TryGetValue(name, out int[,] table)) {
				return powers;
			}
			int bonus = (rider is Player) ? MountKnowledge.Bonus(rider) : 0;
			for (int i = 0; i < table.GetLength(0); i++) {
				int n = table[i, 0], value = table[i, 1];
				if ((n >= 101) && (n <= 105)) {
					value += (value * bonus) / 1000;
				}
				powers.Add(new KeyValuePair<int, int>(n, value));
			}
			return powers;
		}

		internal static void OnMount(Character rider, Character mount) {
			OnDismount(rider);
			List<KeyValuePair<int, int>> powers = PowersFor(rider, mount);
			if (powers.Count > 0) {
				MagicItems.ApplyValues(rider, powers, 1);
				rider.SetTag(appliedTK, Serialize(powers));
			}
		}

		internal static void OnDismount(Character rider) {
			if (rider.GetTag(appliedTK) is string applied) {
				MagicItems.ApplyValues(rider, Deserialize(applied), -1);
				rider.RemoveTag(appliedTK);
			}
		}

		/// <summary>For .boosts: "Demoni 10, Draci 10..." of what's on the rider now.</summary>
		internal static string Describe(Character rider) {
			if (!(rider.GetTag(appliedTK) is string applied)) {
				return null;
			}
			List<string> parts = new List<string>();
			foreach (KeyValuePair<int, int> power in Deserialize(applied)) {
				parts.Add(MagicItemTables.Info(power.Key, power.Value));
			}
			return string.Join("; ", parts);
		}

		private static string Serialize(List<KeyValuePair<int, int>> powers) {
			List<string> parts = new List<string>();
			foreach (KeyValuePair<int, int> power in powers) {
				parts.Add(power.Key + ":" + power.Value);
			}
			return string.Join(",", parts);
		}

		private static List<KeyValuePair<int, int>> Deserialize(string text) {
			List<KeyValuePair<int, int>> powers = new List<KeyValuePair<int, int>>();
			foreach (string part in text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
				string[] pair = part.Split(':');
				powers.Add(new KeyValuePair<int, int>(int.Parse(pair[0]), int.Parse(pair[1])));
			}
			return powers;
		}
	}
}
