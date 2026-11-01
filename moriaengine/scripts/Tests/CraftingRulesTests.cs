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

		private static void CollectUncoloured(CraftmenuCategory category, List<string> uncoloured, ref int coloured) {
			foreach (ICraftmenuElement element in category.Contents) {
				CraftmenuCategory section = element as CraftmenuCategory;
				if (section != null) {
					CollectUncoloured(section, uncoloured, ref coloured);
				} else {
					ItemDef def = ((CraftmenuItem) element).itemDef;
					IObjectWithMaterial material = def as IObjectWithMaterial;
					bool hasMaterial = (material != null) && (material.Material != Material.None);
					Item made;
					try {
						made = (Item) def.Create(self_backpack);
					} catch (SEException e) {
						string reason = e.Message.Length > 90 ? e.Message.Substring(e.Message.Length - 90) : e.Message;
						uncreatable.Add(def.PrettyDefname + " " + reason.Replace("/workspace/moriaengine/moriaengine/./scripts/converted/", ""));
						continue;
					}
					if (made.Color != 0) {
						coloured++;
					} else if (hasMaterial || System.Text.RegularExpressions.Regex.IsMatch(def.PrettyDefname, "_(verite|valorite|obsidian|adamantinum|mithril|copper|iron)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
						uncoloured.Add(def.PrettyDefname + ((material == null) ? "(no material)" : "(" + material.Material + "/" + material.MaterialType + ")"));
					}
					made.Delete();
				}
			}
		}

		private static Container self_backpack;
		private static readonly List<string> uncreatable = new List<string>();

		/// <summary>Every item of the metal and wood menus, made: what comes out with no colour though its material has one.</summary>
		[SteamFunction]
		public static void TestCraftColours(Character self) {
			self_backpack = self.Backpack;
			List<string> uncoloured = new List<string>();
			int coloured = 0;
			foreach (SkillName skill in new[] { SkillName.Blacksmith, SkillName.Fletching, SkillName.Carpentry, SkillName.Tailoring, SkillName.Tinkering, SkillName.Alchemy, SkillName.Cooking, SkillName.Inscribe }) {
				CollectUncoloured(CraftmenuContents.MainCategories[skill], uncoloured, ref coloured);
			}
			Console.WriteLine("CRAFT_UNCREATABLE: " + uncreatable.Count + " " + string.Join(" | ", uncreatable.GetRange(0, Math.Min(14, uncreatable.Count))));
			Console.WriteLine("CRAFT_COLOURS: coloured=" + coloured + " uncoloured=" + uncoloured.Count + " " + string.Join(" ", uncoloured.GetRange(0, Math.Min(36, uncoloured.Count))));
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

		/// <summary>A bundle in the pack: its uid, and later how much loose it became.</summary>
		[SteamFunction]
		public static void TestBundleMake(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim('"').Split(' ');
			Item bundle = (Item) ((ItemDef) ThingDef.GetByDefname(args[0])).Create(self.Backpack);
			bundle.Amount = int.Parse(args[1]);
			CraftingSkillDef.SetCrafter(bundle, self);
			Console.WriteLine("BUNDLE_MADE: uid=" + (uint) bundle.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid);
		}

		[SteamFunction]
		public static void TestBundleReport(Character self, ScriptArgs sa) {
			ItemDef def = (ItemDef) ThingDef.GetByDefname(sa.Args.Trim('"'));
			int loose = 0;
			bool signed = false;
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == def) {
					loose += item.Amount;
					signed = CraftingSkillDef.GetCrafter((Item) item) == self;
				}
			}
			Console.WriteLine("BUNDLE_LOOSE: amount=" + loose + " signed=" + signed);
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
