using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - server-side helpers for Tests/Protocol/CraftingRulesTests.cs.</summary>
	public static class CraftingRulesTests {
		[SteamFunction]
		public static void TestCraftDifficulty(Character self) {
			Console.WriteLine("CRAFT_DIFFICULTY: dagger=" + CraftingSkillDef.MakeDifficulty((ItemDef) ThingDef.GetByDefname("i_dagger_copper"))
				+ " boots=" + CraftingSkillDef.MakeDifficulty((ItemDef) ThingDef.GetByDefname("i_boots_calf")));
		}

		private static int CountItems(CraftmenuCategory category) {
			int items = 0;
			foreach (ICraftmenuElement element in category.Contents) {
				CraftmenuCategory section = element as CraftmenuCategory;
				items += (section == null) ? 1 : CountItems(section);
			}
			return items;
		}

		/// <summary>All the menus, and how the Blacksmith's reads.</summary>
		[SteamFunction]
		public static void TestCraftMenus(Character self) {
			string counts = "";
			foreach (KeyValuePair<SkillName, CraftmenuCategory> menu in CraftmenuContents.MainCategories) {
				counts += " " + menu.Key + "=" + CountItems(menu.Value);
			}
			CraftmenuCategory blacksmith = CraftmenuContents.MainCategories[SkillName.Blacksmith];
			Console.WriteLine("CRAFT_MENUS:" + counts + " first=" + blacksmith.Contents[0].Name + " last=" + blacksmith.Contents[blacksmith.Contents.Count - 1].Name);
		}

		/// <summary>An ordinary player good enough for copper, not for mithril, with what a dagger takes.</summary>
		[SteamFunction]
		public static void TestCraftSmith(Character self) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Blacksmith, 300);
			self.SetRealSkillValue((int) SkillName.Tinkering, 300);
			Item ingots = (Item) ((ItemDef) ThingDef.GetByDefname("i_ingot_copper")).Create(self.Backpack);
			ingots.Amount = 40;
			CraftmenuCategory menu = CraftmenuContents.MainCategories[SkillName.Blacksmith];
			string shown = "";
			foreach (ICraftmenuElement element in menu.Contents) {
				CraftmenuCategory section = element as CraftmenuCategory;
				if ((section != null) && section.HasMakeable(self)) {
					shown += section.Name + ";";
				}
			}
			Console.WriteLine("CRAFT_SMITH: shown=" + shown + " gm=" + self.IsGM);
		}

		[SteamFunction]
		public static void TestCraftOpenMenu(Character self) {
			self.SelectSkill(SkillName.Blacksmith);
			Console.WriteLine("CRAFT_OPENED:");
		}

		[SteamFunction]
		public static void TestCraftDaggers(Character self) {
			SimpleQueue<CraftingSelection> queue = new SimpleQueue<CraftingSelection>();
			queue.Enqueue(new CraftingSelection((ItemDef) ThingDef.GetByDefname("i_dagger_copper"), 5));
			CraftingProcessPlugin.StartCrafting(self, new CraftingOrder((CraftingSkillDef) SkillDef.GetBySkillName(SkillName.Blacksmith), queue));
			Console.WriteLine("CRAFT_STARTED:");
		}

		[SteamFunction]
		public static void TestCraftMade(Character self) {
			int daggers = 0, ingots = 0;
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == ThingDef.GetByDefname("i_dagger_copper")) {
					daggers += item.Amount;
				} else if (item.Def == ThingDef.GetByDefname("i_ingot_copper")) {
					ingots += item.Amount;
				}
			}
			Console.WriteLine("CRAFT_MADE: daggers=" + daggers + " ingots=" + ingots);
		}

		/// <summary>What failing at a given loss takes from 19 ingots of a real recipe.</summary>
		[SteamFunction]
		public static void TestCraftFailLoss(Character self, ScriptArgs sa) {
			int percent = int.Parse(sa.Args.Trim('"'));
			ItemDef recipe = (ItemDef) ThingDef.GetByDefname("i_dagger_copper");
			Item ingots = (Item) ((ItemDef) ThingDef.GetByDefname("i_ingot_copper")).Create(self.Backpack);
			ingots.Amount = 19;
			recipe.Resources.ConsumeSomeResources(self, ResourcesLocality.Backpack, percent);
			Console.WriteLine("CRAFT_LOSS: percent=" + percent + " left=" + (ingots.IsDeleted ? 0 : ingots.Amount));
			if (!ingots.IsDeleted) {
				ingots.Delete();
			}
		}
	}
}
