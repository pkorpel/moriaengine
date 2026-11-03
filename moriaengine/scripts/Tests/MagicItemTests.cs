using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - magic items: bags, rolls, identification, names, wearing.</summary>
	public static class MagicItemTests {
		[SteamFunction]
		public static void TestMagicItems(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;

			MagicBag pouch = (MagicBag) ((ItemDef) ThingDef.GetByDefname("i_mi_pouch_superhigh")).Create(player.Backpack);
			Item made = MagicItems.Unpack(pouch, player);
			Report("aBagUnpacks", pouch.IsDeleted && (made.TopObj() == player) && MagicItems.IsUnidentified(made) && (made.Name == MagicItems.UnidentifiedName));
			Report("aPouchHasOneProperty", MagicItems.Properties(made).Count == 1);
			Report("unidentifiedCantBeWorn", MagicItems.RefusesEquip(made, player));
			MagicItems.Finish(made);
			Report("identifiedItHasAName", !MagicItems.IsUnidentified(made) && MagicItems.IsMagic(made) && (made.Name != MagicItems.UnidentifiedName));
			made.Delete();

			bool allMade = true, allInTable = true, metalsHigh = true;
			for (int i = 0; i < 300; i++) {
				MagicBag bag = (MagicBag) ((ItemDef) ThingDef.GetByDefname((i % 2 == 0) ? "i_mi_bag" : "i_mi_bag_superHigh")).Create(player.Backpack);
				Item item = MagicItems.Unpack(bag, player);
				allMade &= (item != null);
				if (item == null) {
					continue;
				}
				foreach (int n in MagicItems.Properties(item)) { //a staff mixes the staff and the weapon tables
					bool found = false;
					foreach (int[][] table in MagicItemTables.Rolls.Values) {
						foreach (int[] row in table) {
							found |= (row[0] == n) && (MagicItems.Value(item, n) >= 1) && (MagicItems.Value(item, n) <= row[2]);
						}
					}
					allInTable &= found;
				}
				allInTable &= (MagicItems.Properties(item).Count >= 1) && (MagicItems.Properties(item).Count <= 2);
				string defname = item.Def.Defname.ToLowerInvariant();
				if ((i % 2 == 1) && ((item is Weapon) || (item is ColoredArmor)) && (defname.Contains("copper") || defname.Contains("iron") || defname.Contains("verite"))) {
					metalsHigh = false;
				}
				item.Delete();
			}
			Report("everyRollMakesAnItem", allMade);
			Report("everyPropertyIsFromItsTable", allInTable);
			Report("aBlackBagIsValoriteOrBetter", metalsHigh);

			Item ring = MagicItems.MagicItem(player, new ScriptArgs("i_ring_gold 60 5 25 10 70 5 134 7"));
			Report("theInfoLines", MagicItems.InfoLines(ring).Contains("+5 k sile") && MagicItems.InfoLines(ring).Contains("+10% ke skillu Magery") && MagicItems.InfoLines(ring).Contains("+7 % uspesnosti blokace uderu zblizka"));
			short str = player.Str;
			int magery = player.GetSkill(SkillName.Magery);
			double regen = player.HitsRegenSpeed;
			ring.Cont = player;
			Report("wornItWorks", (player.Str == str + 5) && (player.GetSkill(SkillName.Magery) == magery + 100) &&
				(Math.Abs(player.HitsRegenSpeed - regen - 0.5) < 0.001) && (MagicItems.WearerValue(player, 134) == 7) && MagicItems.IsUnderMagic(player));
			ring.Cont = player.Backpack;
			Report("takenOffItStops", (player.Str == str) && (player.GetSkill(SkillName.Magery) == magery) &&
				(Math.Abs(player.HitsRegenSpeed - regen) < 0.001) && (MagicItems.WearerValue(player, 134) == 0) && !MagicItems.IsUnderMagic(player));
			ring.Delete();

			Item kryss = MagicItems.MagicItem(player, new ScriptArgs("i_kryss_copper 60 3"));
			Report("namedByItsGraphic", (kryss.Name == "Kryss of Titan") || (kryss.Name == "Titans Kryss"));
			kryss.Delete();
			Console.WriteLine("MAGIC_ITEMS: kryss=" + ThingDef.GetByModel(((ItemDef) ThingDef.GetByDefname("i_kryss_copper")).Model).Name);
			Console.WriteLine("MAGIC_DONE");
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("MAGIC_ITEM: " + name + "=" + ok);
		}
	}
}
