using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1090 - the user's rule: a special quest item (a key, letter, egg, trophy) is a tag on the player, never an item in
	/// the game. A quest registers its item's defname here. A monster whose loot names one creates no item: it carries the item as
	/// a mark, which goes to its corpse, and the first player to open the corpse takes it, as lifting it out was on Erebor.</summary>
	public static class QuestItems {
		private sealed class Entry {
			internal string Name;
			internal Action<Player> Taken;
		}

		private static readonly Dictionary<string, Entry> registry = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
		private static readonly TagKey carriedTK = TagKey.Acquire("_questLoot_");

		/// <summary>name is what a player reads on taking it ("oplodnene vejce hadi kralovny"); taken runs after a player has.</summary>
		public static void Register(string defname, string name, Action<Player> taken = null) {
			registry[defname] = new Entry { Name = name, Taken = taken };
		}

		public static bool IsQuestItem(string defname) {
			return (defname != null) && registry.ContainsKey(defname);
		}

		private static TagKey TagOf(string defname) {
			return TagKey.Acquire("_questItem_" + defname.ToLowerInvariant() + "_");
		}

		public static bool Has(Character ch, string defname) {
			return ch.GetTag(TagOf(defname)) != null;
		}

		public static void Give(Character ch, string defname) {
			ch.SetTag(TagOf(defname), 1);
		}

		/// <summary>Takes it away; false if the player didn't have it.</summary>
		public static bool Take(Character ch, string defname) {
			if (!Has(ch, defname)) {
				return false;
			}
			ch.RemoveTag(TagOf(defname));
			return true;
		}

		/// <summary>An NPC's loot line names a quest item: the NPC carries it instead of an item.</summary>
		internal static void Carry(Thing holder, string defname) {
			List<string> carried = Carried(holder);
			carried.Add(defname);
			holder.SetTag(carriedTK, string.Join(",", carried));
		}

		internal static List<string> Carried(Thing holder) {
			string carried = holder.GetTag(carriedTK) as string;
			return new List<string>(string.IsNullOrEmpty(carried) ? new string[0] : carried.Split(','));
		}

		/// <summary>Corpse.InitFromChar: what the dead carried lies in the corpse now.</summary>
		internal static void PassToCorpse(Thing dead, Thing corpse) {
			if (dead.GetTag(carriedTK) is string carried) {
				corpse.SetTag(carriedTK, carried);
				dead.RemoveTag(carriedTK);
			}
		}

		/// <summary>Corpse.On_ContainerOpen: the first living player to open it takes what lies in it.</summary>
		internal static void TakeFromCorpse(Thing corpse, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			List<string> carried = Carried(corpse);
			if (carried.Count == 0) {
				return;
			}
			corpse.RemoveTag(carriedTK);
			foreach (string defname in carried) {
				Entry entry;
				registry.TryGetValue(defname, out entry);
				Give(player, defname);
				player.SysMessage((player.IsFemale ? "Sebrala" : "Sebral") + " jsi " + ((entry == null) ? defname : entry.Name) + ".");
				entry?.Taken?.Invoke(player);
			}
		}

		/// <summary>The defname a loot line ("i_x", "i_x,2", "{i_x 1 i_y 1}") names, if it's one plain name.</summary>
		internal static string LootDefname(string lootLine) {
			string line = (lootLine ?? "").Trim();
			int end = line.IndexOfAny(new[] { ',', ' ' });
			return (end < 0) ? line : line.Substring(0, end);
		}
	}
}
