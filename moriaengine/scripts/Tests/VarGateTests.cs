using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#920 - server-side helpers for Tests/Protocol/VarGateTests.cs: each rule walked with a
	/// real step onto a real gate.</summary>
	public static class VarGateTests {
		private const string Gold = "i_gold", Dagger = "i_dagger", BookDef = "i_book_lg_pub";
		private static int fails;
		private static VarGate gate;
		private static Point4D start;
		private static Point4D onGate;
		private static Point4D target;
		private static Point4D elsewhere;
		private static int hookRuns;
		private static VarGate hookGate;
		private static bool hookDenies;

		private static void Check(string name, bool ok, string detail = "") {
			if (!ok) {
				fails++;
				Console.WriteLine("VARGATE_FAIL: " + name + " " + detail);
			}
		}

		private static Point4D Spot(Character self, int x, int y) {
			int[] at = WorldSpots.NearestStandable(self.GetMap(), x, y, 3);
			return new Point4D(at[0], at[1], at[2], self.M);
		}

		private static void Fresh(Character self) {
			if ((gate != null) && !gate.IsDeleted) {
				gate.Delete();
			}
			gate = (VarGate) ThingDef.GetByDefname("i_varGate").Create(onGate.X, onGate.Y, onGate.Z, onGate.M);
			gate.MoreP = target;
			self.Go(start);
		}

		/// <summary>Where a real step east from the start ended: passed, back, on, or a place.</summary>
		private static string Walk(Character ch) {
			ch.Go(start);
			ch.Direction = Direction.East;
			ch.WalkRunOrFly(Direction.East, false, false);
			return Where(ch);
		}

		private static string Where(Character ch) {
			if ((ch.X == target.X) && (ch.Y == target.Y)) {
				return "passed";
			}
			if ((ch.X == start.X) && (ch.Y == start.Y)) {
				return "back";
			}
			if ((ch.X == onGate.X) && (ch.Y == onGate.Y)) {
				return "on";
			}
			return ch.X + "," + ch.Y;
		}

		private static void WalkIs(Character ch, string name, string expected) {
			string where = Walk(ch);
			Check(name, where == expected, "walked " + where + ", expected " + expected);
		}

		private static void Clear(Character self, string defname) {
			foreach (Item item in VarGate.Carried(self)) {
				if (!item.IsDeleted && VarGate.Matches(item, defname, false)) {
					item.Delete();
				}
			}
		}

		private static Item Give(Character self, string defname, int amount) {
			Item item = (Item) ThingDef.GetByDefname(defname).Create(self.Backpack);
			if (amount > 1) {
				item.Amount = amount;
			}
			return item;
		}

		private static VarGateEntry Entry(string name, int amount, int kind = 0, bool optional = false, bool checkOnly = false) {
			VarGateEntry entry = new VarGateEntry();
			entry.name = name;
			entry.amount = amount;
			entry.kind = kind;
			entry.optional = optional;
			entry.checkOnly = checkOnly;
			return entry;
		}

		/// <summary>Stands me on open ground as a plain player of level 1 first.</summary>
		[SteamFunction]
		public static void TestVarGateRules(Character self) {
			Player player = (Player) self;
			fails = 0;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			self.Account.PLevel = 1;
			self.Realm = Realm.Gondor;
			start = new Point4D(self.X, self.Y, self.Z, self.M);
			self.Direction = Direction.East;
			self.WalkRunOrFly(Direction.East, false, false);
			onGate = new Point4D(self.X, self.Y, self.Z, self.M);
			Check("open ground", (onGate.X == (start.X + 1)) && (onGate.Y == start.Y), onGate.ToString());
			target = Spot(self, start.X, start.Y + 5);
			elsewhere = Spot(self, start.X + 5, start.Y + 5);

			Fresh(self);
			Check("the converted def makes a VarGate that can't be moved", gate.Flag_NonMovable);
			Check("a gate doesn't decay", !ItemDecaySystem.HasActiveTimer(gate));
			gate.MoreP = null;
			WalkIs(self, "no target", "on");
			gate.MoreP = target;
			WalkIs(self, "plain gate", "passed");

			self.Account.PLevel = 4;
			gate.Flags = VarGateFlags.RequiresLevel;
			gate.reqMinLevel = 100;
			gate.reqMaxLevel = 0;
			WalkIs(self, "a GM passes anything", "passed");
			self.Account.PLevel = 1;

			Who(self);
			Levels(player);
			Guards(self);
			Removals(self);
			Additions(self);
			Functions(self);
			PvpKit(player);
			RemovesGear(self);
			Experience(player);
			Destinations(player);
			TimeSeal(self);
			Duping(self);

			gate.Delete();
			Console.WriteLine("VARGATE_RULES: fails=" + fails + " end");
		}

		private static void Who(Character self) {
			Fresh(self);
			self.Go(elsewhere);
			Character npc = (Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(start);
			WalkIs(npc, "NPC, not allowed", "on");
			gate.Flags = VarGateFlags.NpcGoThrough;
			WalkIs(npc, "NPC, allowed", "passed");
			npc.Delete();

			Fresh(self);
			self.Go(start);
			self.CauseDeath(null);
			WalkIs(self, "ghost, not allowed", "on");
			gate.Flags = VarGateFlags.GhostGoThrough;
			WalkIs(self, "ghost, allowed", "passed");
			self.Resurrect();
		}

		private static void Levels(Player player) {
			Fresh(player);
			gate.Flags = VarGateFlags.RequiresLevel;
			gate.reqMinLevel = 5;
			gate.reqMaxLevel = 10;
			player.Level = 4;
			WalkIs(player, "under the range", "on");
			player.Level = 5;
			WalkIs(player, "at its bottom", "passed");
			player.Level = 10;
			WalkIs(player, "at its top", "passed");
			player.Level = 11;
			WalkIs(player, "over the range", "on");
			gate.reqMaxLevel = 0;
			player.Level = 5;
			WalkIs(player, "only a minimum: it must be exceeded", "on");
			player.Level = 6;
			WalkIs(player, "only a minimum, above", "passed");
			player.Level = 1;
		}

		private static void Guards(Character self) {
			Fresh(self);
			TagKey count = TagKey.Acquire("spawnedCount");
			Item gem = Give(self, Dagger, 1);
			gem.SetTag(count, 1);
			gate.Flags = VarGateFlags.Guarded;
			gate.Guards.Add(gem);
			WalkIs(self, "guard alive", "on");
			gem.SetTag(count, 0);
			WalkIs(self, "guard dead", "passed");
			gem.Delete();
		}

		private static void Removals(Character self) {
			Clear(self, Gold);
			Fresh(self);
			gate.Flags = VarGateFlags.RemoveItems;
			gate.Removals.Add(Entry(Gold, 3));
			Give(self, Gold, 2);
			WalkIs(self, "too few to give", "back");
			Check("nothing taken from who was refused", VarGate.CountOf(self, Gold, false) == 2);
			Give(self, Gold, 3);
			WalkIs(self, "enough to give", "passed");
			Check("taken what the line says", VarGate.CountOf(self, Gold, false) == 2, VarGate.CountOf(self, Gold, false).ToString());

			gate.Removals[0] = Entry(Gold, 0);
			WalkIs(self, "all of it, some carried", "passed");
			Check("all of it taken", VarGate.CountOf(self, Gold, false) == 0);

			gate.Removals[0] = Entry(Gold, 5, 0, true);
			WalkIs(self, "optional, none carried", "passed");
			Give(self, Gold, 2);
			WalkIs(self, "optional, some carried", "passed");
			Check("optional, taken", VarGate.CountOf(self, Gold, false) == 0);

			gate.Removals[0] = Entry(Gold, 2, 0, false, true);
			Give(self, Gold, 3);
			WalkIs(self, "check only, more than allowed", "back");
			Clear(self, Gold);
			Give(self, Gold, 2);
			WalkIs(self, "check only, within it", "passed");
			Check("check only takes nothing", VarGate.CountOf(self, Gold, false) == 2);
			Clear(self, Gold);

			Item bandage = Give(self, "i_bandage", 1);
			string type = VarGate.TypeOf(bandage);
			Check("a bandage has its type", type.Length > 0, type);
			gate.Removals[0] = Entry(type, 1, VarGateEntry.RemoveType);
			WalkIs(self, "by type", "passed");
			Check("taken by type", bandage.IsDeleted);
			WalkIs(self, "by type, none", "back");

			gate.Removals[0] = Entry("e_spawned_char", 0, VarGateEntry.RemoveEvent);
			WalkIs(self, "event missing", "back");
			TriggerGroup group = TriggerGroup.GetByDefname("e_spawned_char");
			self.AddTriggerGroup(group);
			WalkIs(self, "event there", "passed");
			Check("event taken", !self.HasTriggerGroup(group));
		}

		private static void Additions(Character self) {
			Clear(self, Gold);
			Fresh(self);
			gate.Flags = VarGateFlags.AddItems;
			gate.Additions.Add(Entry(Gold, 7));
			WalkIs(self, "adding", "passed");
			Check("added", VarGate.CountOf(self, Gold, false) == 7, VarGate.CountOf(self, Gold, false).ToString());
			gate.Flags |= VarGateFlags.AddItemTimeLimit;
			gate.addItemSeconds = 1000;
			WalkIs(self, "adding under a time limit", "passed");
			Check("added once", VarGate.CountOf(self, Gold, false) == 14, VarGate.CountOf(self, Gold, false).ToString());
			WalkIs(self, "again within the limit", "passed");
			Check("not added twice", VarGate.CountOf(self, Gold, false) == 14, VarGate.CountOf(self, Gold, false).ToString());
			Clear(self, Gold);

			Fresh(self);
			gate.Flags = VarGateFlags.AddItems;
			gate.Additions.Add(Entry(Gold, 4, 0, false, true));
			WalkIs(self, "add's check only, too few", "back");
			Give(self, Gold, 4);
			WalkIs(self, "add's check only, enough", "passed");
			Check("add's check only gives nothing", VarGate.CountOf(self, Gold, false) == 4);
			Clear(self, Gold);

			gate.Additions[0] = Entry("e_spawned_char", 1, VarGateEntry.AddEvent);
			TriggerGroup group = TriggerGroup.GetByDefname("e_spawned_char");
			WalkIs(self, "adding an event", "passed");
			Check("event added", self.HasTriggerGroup(group));
			self.RemoveTriggerGroup(group);
		}

		[SteamFunction]
		public static bool TestVarGateHook(Character self, ScriptArgs sa) {
			hookRuns++;
			hookGate = sa.Argv[0] as VarGate;
			return hookDenies;
		}

		private static void Functions(Character self) {
			Fresh(self);
			gate.Flags = VarGateFlags.RunFunctions;
			gate.Functions.Add(Entry("TestVarGateHook", 0, 0, false, true));
			hookRuns = 0;
			hookDenies = true;
			WalkIs(self, "a function says no", "back");
			Check("it's told the gate", hookGate == gate);
			hookDenies = false;
			WalkIs(self, "a function says yes", "passed");
			Check("run once each", hookRuns == 2, hookRuns.ToString());

			gate.Functions[0] = Entry("TestVarGateHook", 0);
			hookDenies = true;
			WalkIs(self, "one that can't stop anyone", "passed");
			Check("it still runs", hookRuns == 3, hookRuns.ToString());

			gate.Functions[0] = Entry("no_such_function_here", 0, 0, false, true);
			WalkIs(self, "a function that's gone", "passed");

			gate.Functions[0] = Entry("f_vg_mountDeny", 0, 0, false, true);
			Character horse = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(self.P());
			self.Mount = horse;
			WalkIs(self, "f_vg_mountDeny, riding", "back");
			self.Dismount();
			horse.Delete();
			WalkIs(self, "f_vg_mountDeny, on foot", "passed");

			gate.Functions[0] = Entry("f_vg_bookDeny", 0, 0, false, true);
			Item book = Give(self, BookDef, 1);
			WalkIs(self, "f_vg_bookDeny, carrying", "back");
			book.Delete();
			WalkIs(self, "f_vg_bookDeny, not carrying", "passed");
		}

		private static Item InPack(Character self, string defname) {
			foreach (AbstractItem item in self.Backpack) {
				if (!item.IsDeleted && VarGate.Matches((Item) item, defname, false)) {
					return (Item) item;
				}
			}
			return null;
		}

		/// <summary>#1116 step 7 - the PvP event kit, as a gate's function line with its argument.</summary>
		private static void PvpKit(Player self) {
			ProfessionDef profession = self.Profession;
			self.Profession = Professions.Warrior;
			short str = self.Str;
			self.Str = 200; //a warrior's whole iron kit, or the walker is too loaded to step (#779)
			foreach (AbstractItem item in new List<AbstractItem>(self.Backpack)) {
				item.Delete();
			}
			Item own = Give(self, Dagger, 1);
			Item fixedOne = Give(self, "i_bandage", 3);
			fixedOne.Flag_NonMovable = true;

			Fresh(self);
			gate.Flags = VarGateFlags.RunFunctions;
			gate.Functions.Add(Entry("f_varGate_pvp_equip(iron)", 0));
			WalkIs(self, "the PvP kit", "passed");
			Item bag = null;
			foreach (AbstractItem item in self.Bank) {
				if (item.Name == PvpEventKit.BankBagName) {
					bag = (Item) item;
				}
			}
			Check("own things in the bank's tournament bag", (bag != null) && (own.Cont == bag) && (bag.Color == PvpEventKit.BankBagColor));
			Check("a fixed one stays", fixedOne.Cont == self.Backpack);
			Item plate = InPack(self, "i_iron_platemail");
			Check("warrior's iron plate, marked", (plate != null) && (plate.Name == PvpEventKit.Marker));
			Check("warrior's iron katana", InPack(self, "i_katana_iron") != null);
			Item bandages = null;
			foreach (AbstractItem item in self.Backpack) {
				if ((item != fixedOne) && VarGate.Matches((Item) item, "i_bandage", false)) {
					bandages = (Item) item;
				}
			}
			Check("40 bandages", (bandages != null) && (bandages.Amount == 40), (bandages == null) ? "none" : bandages.Amount.ToString());
			SpellBook book = InPack(self, "i_spellbook_pvp") as SpellBook;
			Check("the PvP spellbook, flamestrike and reveal in it, the necro spells too", (book != null) && book.HasSpell(51) && book.HasSpell(48) && !book.HasSpell(2) && (book.necroSpells == 0x3f));

			gate.Functions[0] = Entry("f_varGate_pvp_clearequip", 0);
			WalkIs(self, "clearing the kit", "passed");
			int left = 0;
			foreach (AbstractItem item in self.Backpack) {
				left++;
			}
			Check("only the fixed one is left", (left == 1) && (fixedOne.Cont == self.Backpack), left.ToString());

			gate.Functions[0] = Entry("f_varGate_pvp_equip tin", 0);
			WalkIs(self, "a metal with no plate", "passed");
			Check("gives nothing", InPack(self, "i_spellbook_pvp") == null);

			gate.Functions[0] = Entry("vg_pvp_denyMagic", 0, 0, false, true);
			MagicBag pouch = (MagicBag) ThingDef.GetByDefname("i_mi_pouch_superhigh").Create(self.Backpack);
			Item magic = MagicItems.Unpack(pouch, self);
			MagicItems.Finish(magic);
			WalkIs(self, "vg_pvp_denyMagic, carrying a magic item", "back");
			magic.Delete();
			WalkIs(self, "vg_pvp_denyMagic, without", "passed");
			fixedOne.Delete();
			bag?.Delete();
			if (profession != null) {
				self.Profession = profession;
			}
			self.Str = str;
		}

		private static void RemovesGear(Character self) {
			Fresh(self);
			gate.Flags = VarGateFlags.RemoveCursor;
			Item held = Give(self, Dagger, 1);
			self.PickupItem(held, 1);
			WalkIs(self, "holding something", "passed");
			Check("the cursor's gone", held.IsDeleted);

			Fresh(self);
			gate.Flags = VarGateFlags.RemovePvpTools;
			Item bandage = Give(self, "i_bandage", 5);
			Item potion = Give(self, "i_potion_HealLess", 1);
			Item robe = Give(self, "i_robe", 1);
			Item bag = Give(self, "i_bag", 1);
			Item dagger = Give(self, Dagger, 1);
			WalkIs(self, "PvP tools", "passed");
			Check("bandage gone", bandage.IsDeleted);
			Check("potion gone", potion.IsDeleted);
			Check("robe gone", robe.IsDeleted);
			Check("bag gone", bag.IsDeleted);
			Check("a weapon stays", !dagger.IsDeleted);
			dagger.Delete();

			Fresh(self);
			gate.Flags = VarGateFlags.RemoveMount;
			Character horse = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(self.P());
			self.Mount = horse;
			WalkIs(self, "riding", "passed");
			Check("off the horse", self.Mount == null);
			Check("the horse is gone", horse.IsDeleted);

			Fresh(self);
			gate.Flags = VarGateFlags.RemoveBank;
			Item banked = (Item) ThingDef.GetByDefname(Dagger).Create(self.Bank);
			WalkIs(self, "a bank", "passed");
			Check("the bank is empty", banked.IsDeleted);
		}

		private static void ExpIs(Player player, int level, int expected) {
			player.Level = level;
			int got = gate.ExperienceFor(player);
			Check("exp at level " + level, got == expected, "got " + got + ", expected " + expected);
		}

		private static void Experience(Player player) {
			Fresh(player);
			gate.Flags = VarGateFlags.ExpGate;
			gate.expAmount = 100;
			gate.expMinLevel = 10;
			gate.expMaxLevel = 20;
			gate.expDecayLow = 4;
			gate.expDecayHigh = 5;
			ExpIs(player, 5, 0);
			ExpIs(player, 6, 20);
			ExpIs(player, 8, 60);
			ExpIs(player, 10, 100);
			ExpIs(player, 20, 100);
			ExpIs(player, 22, 64);
			ExpIs(player, 25, 16);
			ExpIs(player, 26, 0);
			gate.expMaxLevel = 0;
			ExpIs(player, 1, 100);

			player.Level = 1;
			gate.expMinutes = 30;
			int before = player.Experience;
			WalkIs(player, "exp gate", "passed");
			Check("exp given", player.Experience > before, before + " -> " + player.Experience);
			int after = player.Experience;
			WalkIs(player, "exp gate again", "passed");
			Check("exp waits for its time", player.Experience == after);
		}

		private static void Destinations(Player player) {
			Point4D savedHome = player.Home;
			Point4D home = Spot(player, start.X + 5, start.Y);
			player.Home = home;

			Fresh(player);
			gate.GoHome = true;
			Walk(player);
			Check("gohome", (player.X == home.X) && (player.Y == home.Y), Where(player));

			Fresh(player);
			gate.Flags = VarGateFlags.RecallPort;
			gate.MoreP = null;
			Walk(player);
			Check("recall without a rune goes home", (player.X == home.X) && (player.Y == home.Y), Where(player));
			Item rune = Give(player, "i_rune_base", 1);
			rune.MoreP = elsewhere;
			Walk(player);
			Check("recall by the rune", (player.X == elsewhere.X) && (player.Y == elsewhere.Y), Where(player));
			rune.Delete();

			Fresh(player);
			gate.Flags = VarGateFlags.Sideways;
			WalkIs(player, "sideways", "passed");
			player.Home = savedHome;
		}

		private static void TimeSeal(Character self) {
			Fresh(self);
			gate.Flags = VarGateFlags.HasPlugins;
			gate.hasTimeSeal = true;
			gate.sealDelay = 1000;
			gate.sealLength = 1000;
			gate.sealMaxPlayers = 2;
			WalkIs(self, "first through", "passed");
			WalkIs(self, "second through", "passed");
			WalkIs(self, "over the number", "back");
			self.Realm = Realm.Mordor;
			WalkIs(self, "another realm has its own seal", "passed");
			self.Realm = Realm.Gondor;
			gate.ResetTimeSeal();
			WalkIs(self, "reset", "passed");

			gate.sealMaxPlayers = 0;
			gate.ResetTimeSeal();
			gate.sealLength = 1000;
			gate.sealDelay = 0;
			WalkIs(self, "no delay: the first shuts it", "passed");
			WalkIs(self, "shut", "back");
			gate.ResetTimeSeal();

			gate.sealDelay = 1000;
			gate.sealSameRealm = true;
			WalkIs(self, "same realm, first", "passed");
			WalkIs(self, "same realm, second", "passed");
			self.Realm = Realm.Mordor;
			WalkIs(self, "same realm, the other realm", "back");
			self.Realm = Realm.Gondor;
		}

		private static void Duping(Character self) {
			Fresh(self);
			gate.Removals.Add(Entry(Gold, 1));
			VarGate copy = (VarGate) gate.Dupe();
			copy.Removals.Add(Entry(Dagger, 1));
			Check("a copied gate has its own lists", (gate.Removals.Count == 1) && (copy.Removals.Count == 2), gate.Removals.Count + "/" + copy.Removals.Count);
			copy.Delete();
		}

		/// <summary>A gate beside me for the dialogs, and something to link as a guard.</summary>
		[SteamFunction]
		public static void TestVarGateForDialog(Character self) {
			gate = (VarGate) ThingDef.GetByDefname("i_varGate").Create(self.X + 2, self.Y, self.Z, self.M);
			Item gem = (Item) ThingDef.GetByDefname(Dagger).Create(self.X + 1, self.Y, self.Z, self.M);
			gem.SetTag(TagKey.Acquire("spawnedCount"), 0);
			Item notGem = (Item) ThingDef.GetByDefname(Dagger).Create(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("VARGATE_PLACED: gate=" + (uint) gate.FlaggedUid + " gem=" + (uint) gem.FlaggedUid + " notGem=" + (uint) notGem.FlaggedUid + " end");
		}

		private static string Lines(List<VarGateEntry> list) {
			List<string> lines = new List<string>();
			foreach (VarGateEntry entry in list) {
				lines.Add(entry.name + ":" + entry.amount + ":" + entry.kind + ":" + entry.optional + ":" + entry.checkOnly);
			}
			return string.Join("|", lines);
		}

		private static void Report(string marker) {
			Console.WriteLine(marker + " flags=" + (int) gate.Flags + " moreP=" + (gate.MoreP != null) + " exp=" + gate.expAmount + "," + gate.expMinutes +
				" expLevel=" + gate.expMinLevel + "," + gate.expMaxLevel + " decay=" + gate.expDecayLow + "," + gate.expDecayHigh +
				" level=" + gate.reqMinLevel + "," + gate.reqMaxLevel + " seconds=" + gate.addItemSeconds + " remove=" + Lines(gate.Removals) +
				" add=" + Lines(gate.Additions) + " functions=" + Lines(gate.Functions) + " guards=" + gate.Guards.Count +
				" seal=" + gate.hasTimeSeal + "," + gate.sealLength + "," + gate.sealDelay + "," + gate.sealMaxPlayers + "," + gate.sealSameRealm + " end");
		}

		[SteamFunction]
		public static void TestVarGateReport(Character self) {
			Report("VARGATE_REPORT:");
		}

		/// <summary>A gate with everything in use, for a save.</summary>
		[SteamFunction]
		public static void TestVarGateForSave(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			self.Account.PLevel = 1;
			Point4D from = new Point4D(self.X, self.Y, self.Z, self.M);
			self.Direction = Direction.East;
			self.WalkRunOrFly(Direction.East, false, false);
			Item gem = (Item) ThingDef.GetByDefname(Dagger).Create(from.X, from.Y + 1, from.Z, from.M);
			gate = (VarGate) ThingDef.GetByDefname("i_varGate").Create(self.X, self.Y, self.Z, self.M);
			gate.SetTag(TagKey.Acquire("_vargate_test_"), 1);
			gate.MoreP = Spot(self, from.X, from.Y + 5);
			gate.Flags = VarGateFlags.ExpGate | VarGateFlags.AddItems | VarGateFlags.HasPlugins | VarGateFlags.Guarded | VarGateFlags.AddItemTimeLimit;
			gate.expAmount = 500;
			gate.expMinutes = 30;
			gate.addItemSeconds = 1000;
			gate.Removals.Add(Entry(Gold, 3, 0, true, true));
			gate.Additions.Add(Entry(Dagger, 1));
			gate.Functions.Add(Entry("f_vg_mountDeny", 0, 0, false, true));
			gate.Guards.Add(gem);
			gate.hasTimeSeal = true;
			gate.sealLength = 30;
			gate.sealDelay = 1000;
			gate.sealMaxPlayers = 2;
			gate.sealSameRealm = true;
			gate.GoHome = false;
			self.Go(from);
			self.Direction = Direction.East;
			self.WalkRunOrFly(Direction.East, false, false);
			Console.WriteLine("VARGATE_SAVED: where=" + self.X + "," + self.Y + " end");
		}

		[SteamFunction]
		public static void TestVarGateAfterRestart(Character self) {
			gate = null;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is VarGate found) && found.HasTag(TagKey.Acquire("_vargate_test_"))) {
					gate = found;
				}
			}
			if (gate == null) {
				Console.WriteLine("VARGATE_LOADED: none end");
				return;
			}
			Report("VARGATE_LOADED:");
			Console.WriteLine("VARGATE_LOADED_STATE: seals=" + ((gate.seals != null) ? gate.seals.Count : 0) + " records=" + ((gate.records != null) ? gate.records.Count : 0) +
				" nonMovable=" + gate.Flag_NonMovable + " guardSurvived=" + ((gate.Guards.Count == 1) && (gate.Guards[0] != null)) + " end");
		}
	}
}
