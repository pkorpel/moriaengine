using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1070 - Erebor's magic item counters (tags on its global object #0400075A4) and GM tools: copyitem,
	/// showmonstertypes, getidentifygold. Erebor read the counters in a GM dialog of Aldaron's; here .magicitemstats
	/// lists them. Its pouch counters were always 1 (they added to the pouch's own empty tag); these count.</summary>
	[HasSavedMembers]
	public static class MagicItemGmTools {
		[SavedMember]
		private static int identifyGold;

		[SavedMember]
		private static int exchangedItems;

		[SavedMember]
		private static int pouches, highPouches, superHighPouches, bags, highBags, superHighBags;

		internal static void CountIdentified(Player player, int price) {
			if (!player.IsGM) {
				identifyGold += price;
			}
		}

		internal static int BagsCounted {
			get { return bags; }
		}

		internal static void CountExchanged(int items) {
			exchangedItems += items;
		}

		/// <summary>padnute_male_zelene and the rest, at a bag's @create.</summary>
		internal static void CountBag(Item bag) {
			switch (bag.Def.PrettyDefname.ToLowerInvariant()) {
				case "i_mi_pouch":
					pouches++;
					break;
				case "i_mi_pouch_high":
					highPouches++;
					break;
				case "i_mi_pouch_superhigh":
					superHighPouches++;
					break;
				case "i_mi_bag":
					bags++;
					break;
				case "i_mi_bag_high":
					highBags++;
					break;
				case "i_mi_bag_superhigh":
					superHighBags++;
					break;
			}
		}

		[SteamFunction]
		public static void GetIdentifyGold(Character self) {
			if (self.IsGM) {
				self.SysMessage("Za identifikaci itemu bylo doposud zaplaceno " + identifyGold + " zlata.");
			}
		}

		[SteamFunction]
		public static void MagicItemStats(Character self) {
			self.SysMessage("Za identifikaci zaplaceno: " + identifyGold + " zlata, vymeneno predmetu: " + exchangedItems + ".");
			self.SysMessage("Male: zelene " + pouches + ", hnede " + highPouches + ", cierne " + superHighPouches + ".");
			self.SysMessage("Velke: zelene " + bags + ", hnede " + highBags + ", cierne " + superHighBags + ".");
		}

		[SteamFunction]
		public static void CopyItem(Player self) {
			self.Target(SingletonScript<Targ_CopyMagicItem>.Instance);
		}

		[SteamFunction]
		public static void ShowMonsterTypes(Player self) {
			self.Target(SingletonScript<Targ_ShowMonsterTypes>.Instance);
		}

		/// <summary>copyitem's @targon_item: a new item of the same def with every property, its name, identified.</summary>
		internal static Item Copy(Player self, Item original) {
			Item copy = (Item) original.Def.Create(self.Backpack);
			foreach (int n in MagicItems.Properties(original)) {
				MagicItems.SetValue(copy, n, MagicItems.Value(original, n));
			}
			if (MagicItems.Properties(copy).Count > 0) {
				MagicItems.Finish(copy);
			}
			copy.Name = original.Name;
			Logger.WriteInfo(true, "Copy Of Magic Item: created item uid=" + copy.Uid + " (" + copy.Amount + " " + copy.Name + ")");
			return copy;
		}
	}

	public class Targ_CopyMagicItem : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			MagicItemGmTools.Copy(self, targetted);
			return TargetResult.Done;
		}
	}

	public class Targ_ShowMonsterTypes : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			NPCDef def = targetted.Def as NPCDef;
			self.SysMessage(def?.MonsterType ?? "");
			self.SysMessage(def?.MonsterType2 ?? "");
			self.SysMessage(def?.MonsterType3 ?? "");
			return TargetResult.Done;
		}
	}
}
