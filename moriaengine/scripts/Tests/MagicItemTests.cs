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

		[SteamFunction]
		public static void TestMagicCombat(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC rabbit = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			rabbit.MaxHits = 30000;
			rabbit.Hits = 30000;
			NPCDef def = (NPCDef) rabbit.Def;
			string type = def.MonsterType;
			def.MonsterType = "animal,60";
			Item ring = MagicItems.MagicItem(player, new ScriptArgs("i_ring_gold 104 20 111 10 121 100"));
			ring.Cont = player;
			Report("aSlayerCountsByTheMonstersPercent", (MagicCombat.SlayerBonuses(player, rabbit)[3] == 12) && (Math.Abs(MagicCombat.ApplySlayers(player, rabbit, 100) - 112) < 0.01));
			def.MonsterType = "humanoid";
			Report("notAgainstAnotherKind", MagicCombat.ApplySlayers(player, rabbit, 100) == 100);
			def.MonsterType = type;
			player.Hits = 10;
			MagicCombat.AfterSwing(player, rabbit, 100);
			Report("leechHealsByTheFormula", player.Hits == 10 + MagicCombat.Leech(100, 20, ClassAbilitySettings.instance));
			Report("freezingParalyses", ParalyzeEffectPluginDef.IsParalyzed(rabbit));
			ring.Delete();
			player.Hits = player.MaxHits;

			Item shield = MagicItems.MagicItem(player, new ScriptArgs("i_ring_gold 136 100"));
			shield.Cont = player;
			SpellDef arrow = SpellDef.GetByDefname("s_magic_arrow");
			int hits = player.Hits;
			arrow.Trigger_EffectChar(player, SpellEffectArgs.Acquire(rabbit, player, player, arrow, 500, EffectFlag.HarmfulEffect));
			Report("aBlockStopsAHarmfulSpell", player.Hits == hits);
			shield.Delete();
			Item mirror = MagicItems.MagicItem(player, new ScriptArgs("i_ring_gold 133 100"));
			mirror.Cont = player;
			int rabbitHits = rabbit.Hits;
			arrow.Trigger_EffectChar(player, SpellEffectArgs.Acquire(rabbit, player, player, arrow, 500, EffectFlag.HarmfulEffect));
			Report("aReflexSendsItBack", (player.Hits == hits) && (rabbit.Hits < rabbitHits));
			mirror.Delete();
			rabbit.Delete();
			Console.WriteLine("MAGIC_COMBAT_DONE");
		}

		[SteamFunction]
		public static void TestMagicExchanger(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			NPC vetesnik = (NPC) ((NPCDef) ThingDef.GetByDefname("c_vetesnik")).Create(self.X + 1, self.Y, self.Z, self.M);
			Report("theVetesnikIsAnExchanger", MagicItemShop.IsExchanger(vetesnik) && !MagicItemShop.IsMordor(vetesnik));
			foreach (AbstractItem old in new System.Collections.Generic.List<AbstractItem>(player.Backpack)) {
				old.Delete();
			}
			MagicBag bag = (MagicBag) ((ItemDef) ThingDef.GetByDefname("i_mi_pouch")).Create(player.Backpack);
			Item unknown = MagicItems.Unpack(bag, player);
			((Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(player.Backpack)).Amount = 6000;
			MagicItemShop.Identify(vetesnik, player, unknown);
			Report("identifyingCostsAndReveals", !MagicItems.IsUnidentified(unknown) && MagicItems.IsMagic(unknown) && (player.Gold == 1000));
			MagicItems.SetValue(unknown, 60, 1);
			MagicItems.SetValue(unknown, 61, 1);
			Report("tooFewToExchange", !MagicItemShop.Exchange(vetesnik, player) && !unknown.IsDeleted);
			Item rich = MagicItems.MagicItem(player, new ScriptArgs("i_ring_gold 1 1 5 1 17 1 26 1 60 1 61 1 62 1 70 1"));
			Item crafted = MagicItems.MagicItem(player, new ScriptArgs("i_ring_silver 1 1 5 1 17 1 26 1 60 1 61 1 62 1 70 1"));
			CraftingSkillDef.SetCrafter(crafted, player);
			Report("tenPropertiesBuyABag", MagicItemShop.Exchange(vetesnik, player) && unknown.IsDeleted && rich.IsDeleted && !crafted.IsDeleted &&
				(ShamanAbilities.FindInPack(player, "i_mi_bag") != null));
			crafted.Delete();

			ClassAbilities.SetPoints(player, "crushing", 8);
			Item staff = (Item) ((ItemDef) ThingDef.GetByDefname("i_staff_quarter_copper")).Create(player.Backpack);
			Report("aCrushingPathStaff", MagicItemShop.ApplyPath(player, staff) && (MagicItems.Value(staff, 46) == 8) && (MagicItems.Value(staff, 25) == 2));
			ClassAbilities.SetPoints(player, "sharps", 5);
			Item kryss = (Item) ((ItemDef) ThingDef.GetByDefname("i_kryss_copper")).Create(player.Backpack);
			Report("aSharpsPathKryss", MagicItemShop.ApplyPath(player, kryss) && (MagicItems.Value(kryss, 42) == 5));
			ClassAbilities.SetPoints(player, "crushing", 0);
			Item mace = (Item) ((ItemDef) ThingDef.GetByDefname("i_mace_copper")).Create(player.Backpack);
			Report("noPathNoMace", !MagicItemShop.ApplyPath(player, mace));
			staff.Delete();
			kryss.Delete();
			mace.Delete();
			Console.WriteLine("MAGIC_EXCHANGER_READY uid=" + vetesnik.Uid);
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("MAGIC_ITEM: " + name + "=" + ok);
		}
	}
}
