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
			self.TryEquip((Item) ((ItemDef) ThingDef.GetByDefname("i_hammer_smith")).Create(self.Backpack));
			PlaceForge(self, 1);
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

		private static Item forge;

		private static void PlaceForge(Character self, int distance) {
			if ((forge != null) && !forge.IsDeleted) {
				forge.Delete();
			}
			forge = null;
			if (distance > 0) {
				forge = (Item) ((ItemDef) ThingDef.GetByDefname("i_forge")).Create(self.Backpack);
				forge.P((ushort) (self.X + distance), self.Y, self.Z, self.M);
			}
		}

		/// <summary>A plain smith with ingots and a hammer, in the pack, no forge.</summary>
		[SteamFunction]
		public static void TestStageSetup(Character self) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Blacksmith, 300);
			self.SetRealSkillValue((int) SkillName.Tinkering, 300);
			PlaceForge(self, 0);
			Item ingots = (Item) ((ItemDef) ThingDef.GetByDefname("i_ingot_copper")).Create(self.Backpack);
			ingots.Amount = 40;
			Item hammer = (Item) ((ItemDef) ThingDef.GetByDefname("i_hammer_smith")).Create(self.Backpack);
			Console.WriteLine("STAGE_SETUP: ingots=" + (uint) ingots.FlaggedUid + " hammer=" + (uint) hammer.FlaggedUid + " pack=" + (uint) self.Backpack.FlaggedUid);
		}

		[SteamFunction]
		public static void TestStageForge(Character self, ScriptArgs sa) {
			PlaceForge(self, int.Parse(sa.Args.Trim('"')));
			Console.WriteLine("STAGE_FORGE:");
		}

		/// <summary>How many strokes each skill takes over many rolls, and its delay at no skill and at full.</summary>
		[SteamFunction]
		public static void TestStageStrokes(Character self) {
			string report = "";
			foreach (SkillName skill in new[] { SkillName.Blacksmith, SkillName.Carpentry, SkillName.Fletching, SkillName.Tailoring, SkillName.Tinkering, SkillName.Alchemy }) {
				CraftingSkillDef def = (CraftingSkillDef) SkillDef.GetBySkillName(skill);
				int least = int.MaxValue, most = 0;
				for (int i = 0; i < 2000; i++) {
					int strokes = def.RollStrokes((ItemDef) ThingDef.GetByDefname("i_potion_Agility"));
					least = Math.Min(least, strokes);
					most = Math.Max(most, strokes);
				}
				report += " " + skill + "=" + least + "-" + most + "@" + def.GetDelayForValue(0).ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" + def.GetDelayForValue(1000).ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			Console.WriteLine("STAGE_STROKES:" + report);
		}

		[SteamFunction]
		public static void TestStageMake(Character self) {
			SimpleQueue<CraftingSelection> queue = new SimpleQueue<CraftingSelection>();
			queue.Enqueue(new CraftingSelection((ItemDef) ThingDef.GetByDefname("i_dagger_copper"), 1));
			CraftingProcessPlugin.StartCrafting(self, new CraftingOrder((CraftingSkillDef) SkillDef.GetBySkillName(SkillName.Blacksmith), queue));
			Console.WriteLine("STAGE_STARTED:");
		}

		/// <summary>The quality bands each skill reaches over many rolls, and a band's range.</summary>
		[SteamFunction]
		public static void TestStageQuality(Character self) {
			string report = "";
			foreach (int skill in new[] { 0, 500, 1000 }) {
				int least = int.MaxValue, most = -1, worst = int.MaxValue, best = 0;
				for (int i = 0; i < 5000; i++) {
					int band = CraftingSkillDef.RollQualityBand(skill, Globals.dice.Next);
					int quality = CraftingSkillDef.RollQuality(band, Globals.dice.Next);
					least = Math.Min(least, band);
					most = Math.Max(most, band);
					worst = Math.Min(worst, quality);
					best = Math.Max(best, quality);
				}
				report += " " + skill + "=" + least + "-" + most + "q" + worst + "-" + best;
			}
			Console.WriteLine("STAGE_QUALITY:" + report);
		}

		/// <summary>Makes one thing as a GM would, with everything to hand, and says what came of it.</summary>
		[SteamFunction]
		public static void TestStageMade(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim('"').Split(' ');
			ItemDef def = (ItemDef) ThingDef.GetByDefname(args[0]);
			int quality = 0, count = 0;
			bool signed = false;
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == def) {
					count += item.Amount;
					quality = CraftingSkillDef.GetQuality((Item) item);
					signed = CraftingSkillDef.GetCrafter((Item) item) == self;
				}
			}
			Console.WriteLine("STAGE_MADE: count=" + count + " quality=" + quality + " signed=" + signed);
		}

		[SteamFunction]
		public static void TestStageGm(Character self, ScriptArgs sa) {
			self.Account.PLevel = 7;
			SimpleQueue<CraftingSelection> queue = new SimpleQueue<CraftingSelection>();
			string[] args = sa.Args.Trim('"').Split(' ');
			queue.Enqueue(new CraftingSelection((ItemDef) ThingDef.GetByDefname(args[0]), 1));
			CraftingProcessPlugin.StartCrafting(self, new CraftingOrder((CraftingSkillDef) SkillDef.GetBySkillName((SkillName) Enum.Parse(typeof(SkillName), args[1])), queue));
			Console.WriteLine("STAGE_STARTED:");
		}

		private static void Give(Character self, string defname, int amount) {
			Item item = (Item) ((ItemDef) ThingDef.GetByDefname(defname)).Create(self.Backpack);
			item.Amount = amount;
		}

		/// <summary>A plain thief able to brew Agility, with all it takes. Who may brew it, and a queue to start.</summary>
		[SteamFunction]
		public static void TestStageBrew(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Profession = ProfessionDef.GetByDefname("profession_thief");
			player.SetRealSkillValue((int) SkillName.Alchemy, 1000);
			player.SetRealSkillValue((int) SkillName.TasteID, 1000);
			Give(player, "i_mortar_pestle", 1);
			Give(player, "i_bottle_empty", 2);
			Give(player, "i_reag_blood_moss", 4);
			Give(player, "i_reag_worm_heart", 2);
			Give(player, "i_reag_obsidian", 2);
			ItemDef agility = (ItemDef) ThingDef.GetByDefname("i_potion_Agility");
			bool thief = CraftingSkillDef.CanBeMade(agility, player);
			player.Profession = ProfessionDef.GetByDefname("profession_mage");
			bool mage = CraftingSkillDef.CanBeMade(agility, player);
			player.Profession = ProfessionDef.GetByDefname("profession_thief");
			SimpleQueue<CraftingSelection> queue = new SimpleQueue<CraftingSelection>();
			queue.Enqueue(new CraftingSelection(agility, 1));
			CraftingProcessPlugin.StartCrafting(player, new CraftingOrder((CraftingSkillDef) SkillDef.GetBySkillName(SkillName.Alchemy), queue));
			Console.WriteLine("STAGE_BREW: thief=" + thief + " mage=" + mage);
		}

		[SteamFunction]
		public static void TestStageCount(Character self) {
			string report = "";
			foreach (string defname in new[] { "i_reag_blood_moss", "i_reag_worm_heart", "i_reag_obsidian", "i_bottle_empty", "i_potion_Agility", "i_scroll_blank", "i_scroll_magic_arrow" }) {
				int count = 0;
				foreach (AbstractItem item in self.Backpack) {
					if (item.Def == ThingDef.GetByDefname(defname)) {
						count += item.Amount;
					}
				}
				report += " " + defname + "=" + count;
			}
			Console.WriteLine("STAGE_COUNT:" + report);
		}

		/// <summary>A mage ready to scribe Magic Arrow, and a thief who may not.</summary>
		[SteamFunction]
		public static void TestStageScribe(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Profession = ProfessionDef.GetByDefname("profession_mage");
			player.SetRealSkillValue((int) SkillName.Inscribe, 1000);
			player.SetRealSkillValue((int) SkillName.Magery, 1000);
			SpellDef spell = (SpellDef) SpellDef.GetByDefname("s_magic_arrow");
			SpellBook book = (SpellBook) ((ItemDef) ThingDef.GetByDefname("i_spellbook_3")).Create(player.Backpack);
			book.AddSpell(spell);
			SpellScrollDef scroll = spell.ScrollItem;
			Give(player, "i_scroll_blank", 20);
			player.MaxMana = 100;
			player.Mana = 3;
			bool noMana = CraftingSkillDef.CanBeMade(scroll, player);
			player.Mana = 50;
			bool mage = CraftingSkillDef.CanBeMade(scroll, player);
			player.Profession = ProfessionDef.GetByDefname("profession_thief");
			bool thief = CraftingSkillDef.CanBeMade(scroll, player);
			player.Profession = ProfessionDef.GetByDefname("profession_mage");
			SimpleQueue<CraftingSelection> queue = new SimpleQueue<CraftingSelection>();
			queue.Enqueue(new CraftingSelection(scroll, 1));
			CraftingProcessPlugin.StartCrafting(player, new CraftingOrder((CraftingSkillDef) SkillDef.GetBySkillName(SkillName.Inscribe), queue));
			int noRecipe = 0;
			foreach (SpellDef one in SpellDef.AllSpellDefs) {
				if ((one.ScrollItem != null) && (one.ScrollItem.Resources == null) && !one.ScrollItem.PrettyDefname.StartsWith("i_scroll_test_", StringComparison.Ordinal)) {
					noRecipe++;
				}
			}
			Console.WriteLine("STAGE_SCRIBE: noMana=" + noMana + " mage=" + mage + " thief=" + thief + " mana=" + player.Mana + " noRecipe=" + noRecipe);
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
