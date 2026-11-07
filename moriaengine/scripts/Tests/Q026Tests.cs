using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1272 - q026 (the tomb curse) on the server: the wraith's curse and its stronger blows, the curse's tick, cure potions
	/// failing, Eriol, then (after Tests/Protocol/Q026Tests.cs's client has talked to Andor) Unas, the temple's tapestry, door and books,
	/// Dol Guldur's tile, wall and box, Unas's recipe, Roxana's goods and price, and the immunity.</summary>
	public static class Q026Tests {
		private static readonly TagKey npcsTK = TagKey.Acquire("_q026test_npcs_");
		private static readonly TagKey itemsTK = TagKey.Acquire("_q026test_items_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("Q026_CHECK: " + name + "=" + ok);
		}

		private static NPC Npc(Player player, string def, int dx, int dy) {
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname(def)).Create(player.X + dx, player.Y + dy, player.Z, player.M);
			NPCAiTimer.Freeze(npc, 900);
			return npc;
		}

		private static Item Fixture(Player player, string def, int dx) {
			return (Item) ThingDef.GetByDefname(def).Create(player.X + dx, player.Y + 40, player.Z, player.M);
		}

		/// <summary>Andor beside the player, everyone else out of earshot; the curse, the potions and Eriol up to stage 3.</summary>
		[SteamFunction]
		public static void TestQ026Setup(Character self) {
			Player player = (Player) self;
			NPC andor = Npc(player, Q026.AndorDef, 1, 0);
			NPC eriol = Npc(player, Q006.EriolDef, 30, 0);
			NPC unas = Npc(player, Q006.UnasDef, 34, 0);
			NPC roxana = Npc(player, Q026.RoxanaDef, 38, 0);
			NPC wraith = Npc(player, "c_hcn_wraith_level_1", 42, 0);
			NPC rabbit = Npc(player, "c_rabbit", 46, 0);
			player.SetTag(npcsTK, new[] { andor, eriol, unas, roxana, wraith, rabbit });
			Q026.SetStage(player, 0);
			Q026.ForgetGoods(player);
			player.DeleteTimer(Q026CurseTimer.Key);
			player.Level = 200;
			player.Experience = 0;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //wraiths leave GMs alone
			try {
				Cursed(player, wraith, rabbit, eriol);
			} finally {
				player.Account.PLevel = plevel;
			}
			Console.WriteLine("Q026_SETUP_DONE: andor=" + andor.Uid);
		}

		private static void Cursed(Player player, NPC wraith, NPC rabbit, NPC eriol) {
			//the first landed blow curses, through the real swing; later ones on a cursed hero are stronger, never weaker; other monsters do nothing
			Point4D at = new Point4D(wraith.X, wraith.Y, wraith.Z, wraith.M);
			wraith.P(player.X + 1, player.Y, player.Z);
			player.MaxHits = 30000;
			player.Hits = 30000;
			int swings = 0;
			for (; (swings < 200) && (Q026.GetStage(player) == 0); swings++) {
				DamageManager.ProcessSwing(wraith, player);
			}
			wraith.P(at);
			bool cursed = (Q026.GetStage(player) == 1) && (player.GetTimer(Q026CurseTimer.Key) != null) && Q026.Journal.IsStarted(player);
			Console.WriteLine("Q026_SWINGS: " + swings);
			int stronger = 0;
			bool neverWeaker = true;
			for (int i = 0; i < 100; i++) {
				double blow = Q026.WraithBlow(wraith, player, 30);
				neverWeaker &= (blow >= 30) && (blow <= 50);
				stronger += (blow > 30) ? 1 : 0;
			}
			bool rabbitPlain = Q026.WraithBlow(rabbit, player, 30) == 30;
			Report("theWraithCurses", cursed && neverWeaker && (stronger > 40) && (stronger < 95) && rabbitPlain);

			//the curse's tick: a food point while there's food, then 25 damage
			PlayerHunger.SetFood(player, 10);
			Q026.CurseTick(player);
			bool ate = PlayerHunger.FoodOf(player) == 9;
			PlayerHunger.SetFood(player, 4);
			player.MaxHits = 200;
			player.Hits = 200;
			Q026.CurseTick(player);
			Report("theCurseTicks", ate && (PlayerHunger.FoodOf(player) == 4) && (player.Hits == 200 - Q026.CurseDamage));
			player.Hits = player.MaxHits;
			PlayerHunger.SetFood(player, 10);

			//a cure potion only shows it's no poison; Eriol sends the hero to a cleric
			Potion potion = (Potion) ThingDef.GetByDefname("i_potion_Cure").Create(player.Backpack);
			PotionKinds.Find(potion).Drink(player, potion);
			bool potionFailed = Q026.GetStage(player) == 2;
			D_Q006Eriol dEriol = SingletonScript<D_Q006Eriol>.Instance;
			bool eriolHears = dEriol.BuildFor(eriol, player).HasResponse("q026_healer");
			dEriol.Press("q026_healer", eriol, player);
			Report("thePotionThenEriol", potionFailed && eriolHears && (Q026.GetStage(player) == 3));
		}

		[SteamFunction]
		public static void TestQ026Button(Character self, ScriptArgs sa) {
			Console.WriteLine("Q026_BUTTON: id=" + SingletonScript<D_Q026Andor>.Instance.ExitButtonId(sa.Args.Trim().Trim('"')));
		}

		[SteamFunction]
		public static void TestQ026State(Character self) {
			Console.WriteLine("Q026_STATE: stage=" + Q026.GetStage((Player) self));
		}

		private static int ExpGained(Player player, Action act) {
			int before = player.Experience;
			act();
			return player.Experience - before;
		}

		[SteamFunction]
		public static void TestQ026Chain(Character self) {
			Player player = (Player) self;
			NPC[] npcs = (NPC[]) player.GetTag(npcsTK);
			NPC unas = npcs[2], roxana = npcs[3], wraith = npcs[4];
			D_Q006Unas dUnas = SingletonScript<D_Q006Unas>.Instance;
			D_Q026Roxana dRoxana = SingletonScript<D_Q026Roxana>.Instance;
			E_Q026Fixture fixture = SingletonScript<E_Q026Fixture>.Instance;
			Item tapestry = Fixture(player, Q026.TapestryDef, 0), books = Fixture(player, Q026.BookPileDef, 3), door = Fixture(player, Q026.DoorDef, 6);
			Item alarm = Fixture(player, Q026.AlarmDef, 9), wall = Fixture(player, Q026.WallDef, 12), box = Fixture(player, Q026.BoxDef, 15);
			player.SetTag(itemsTK, new[] { tapestry, books, door, alarm, wall, box });
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 1; //a player: GMs pay nothing, and fixtures only describe themselves to them
			try {
				Report("theClientToldAndor", Q026.GetStage(player) == 4);

				//Unas sends the hero to the temple; its tapestry only then
				fixture.On_DClick(tapestry, player);
				bool tapestryWaits = Q026.GetStage(player) == 4;
				bool unasHears = dUnas.BuildFor(unas, player).HasResponse("q026_unasTemple");
				dUnas.Press("q026_unasTemple", unas, player);
				int exp = ExpGained(player, () => fixture.On_DClick(tapestry, player));
				Report("unasThenTheTapestry", tapestryWaits && unasHears && (Q026.GetStage(player) == 7) && (exp == Q026.TapestryExp));

				//without the key: the door won't move, its force throws the hero back, the books and the box hide nothing
				int z = door.Z;
				fixture.On_DClick(door, player);
				bool doorShut = (door.Z == z) && (door.GetTag(Q026.OpenTK) == null);
				player.Go(door.X, door.Y, door.Z, door.M);
				int hits = player.Hits, y = player.Y;
				fixture.On_Step(door, player);
				bool thrownBack = (player.Y == y + 1) && (player.Hits == hits - Q026.DoorDamage);
				player.Hits = player.MaxHits;
				player.Go(books.X, books.Y + 1, books.Z, books.M);
				Q026.SearchBooks(books, player, true);
				fixture.On_DClick(box, player);
				int wallX = wall.X;
				fixture.On_DClick(wall, player);
				bool wallStays = (wall.X == wallX) && (wall.GetTag(Q026.OpenTK) == null);
				Report("noKeyNoWay", doorShut && thrownBack && wallStays && (Q026.GetStage(player) == 7));

				//Dol Guldur: the tile, the wall pushed aside and back, the box's key
				fixture.On_Step(alarm, player);
				bool tile = Q026.GetStage(player) == 8;
				int wallModel = wall.Model;
				fixture.On_DClick(wall, player);
				bool open = (wall.X == wallX - 1) && (wall.Model == 0x0242) && (wall.GetTimer(Q026CloseTimer.Key) != null);
				fixture.On_DClick(wall, player);
				bool closed = (wall.X == wallX) && (wall.Model == wallModel) && (wall.GetTimer(Q026CloseTimer.Key) == null);
				fixture.On_DClick(box, player);
				Report("dolGuldur", tile && open && closed && (Q026.GetStage(player) == 9));

				//with the key: the door slides, the force lets the hero by, a lucky search finds the book
				fixture.On_DClick(door, player);
				bool slid = (Q026.GetStage(player) == 10) && (door.Z == z - 30) && (door.GetTimer(Q026CloseTimer.Key) != null);
				player.Go(door.X, door.Y, door.Z, door.M);
				y = player.Y;
				fixture.On_Step(door, player);
				bool letBy = (player.Y == y) && (player.Hits == player.MaxHits);
				fixture.On_DClick(door, player);
				bool backUp = door.Z == z;
				player.Go(books.X, books.Y + 5, books.Z, books.M);
				Q026.SearchBooks(books, player, true);
				bool tooFar = Q026.GetStage(player) == 10;
				player.Go(books.X, books.Y + 1, books.Z, books.M);
				Q026.SearchBooks(books, player, false);
				bool unlucky = Q026.GetStage(player) == 10;
				Q026.SearchBooks(books, player, true);
				Report("theDoorAndTheBook", slid && letBy && backUp && tooFar && unlucky && (Q026.GetStage(player) == 11));

				//Unas reads the book, Roxana wants her goods brought bit by bit, then her price
				bool recipe = dUnas.BuildFor(unas, player).HasResponse("q026_unasRecipe");
				dUnas.Press("q026_unasRecipe", unas, player);
				dRoxana.Press("q026_roxanaAccept", roxana, player);
				bool listed = (Q026.GetStage(player) == 13) && (Q026.Remaining(player, 0) == 20) && Q026.Journal.GetText(player).Contains("4500 smrkovych");
				Q006.Give(player, Q026.Goods[0], 10);
				dRoxana.Press("q026_roxanaGoods", roxana, player);
				bool partly = (Q026.Given(player, 0) == 10) && (Q006.Count(player, Q026.Goods[0]) == 0);
				Q006.Give(player, Q026.Goods[0], 15);
				dRoxana.Press("q026_roxanaGoods", roxana, player);
				bool neverMore = (Q026.Given(player, 0) == 20) && (Q006.Count(player, Q026.Goods[0]) == 5);
				bool notYet = !dRoxana.BuildFor(roxana, player).HasResponse("q026_noMoney") && dRoxana.BuildFor(roxana, player).HasResponse("q026_roxanaGoods");
				for (int i = 1; i < Q026.Goods.Length; i++) {
					Q006.Give(player, Q026.Goods[i], Q026.GoodsNeeded[i]);
				}
				dRoxana.Press("q026_roxanaGoods", roxana, player);
				Report("roxanasGoods", recipe && listed && partly && neverMore && notYet && Q026.AllGoodsGiven(player));

				bool noGoldNoCure = dRoxana.BuildFor(roxana, player).HasResponse("q026_noMoney") && !dRoxana.BuildFor(roxana, player).HasResponse("q026_getCure");
				dRoxana.Press("q026_getCure", roxana, player);
				bool refused = Q026.GetStage(player) == 13;
				((Item) ThingDef.GetByDefname("i_gold").Create(player.Bank)).Amount = Q026.CureGold;
				long gold = player.Gold;
				bool offered = dRoxana.BuildFor(roxana, player).HasResponse("q026_getCure");
				dRoxana.Press("q026_getCure", roxana, player);
				Report("theCure", noGoldNoCure && refused && offered && (Q026.GetStage(player) == Q026.Done) && (player.Gold == gold - Q026.CureGold)
					&& (player.GetTimer(Q026CurseTimer.Key) == null) && Q026.Journal.IsDone(player));
				Report("immuneForGood", Q026.WraithBlow(wraith, player, 30) == 30 && (Q026.GetStage(player) == Q026.Done));

				bool plain = true;
				for (int stage = 1; stage <= Q026.Done; stage++) {
					Q026.SetStage(player, stage);
					string text = Q026.GetJournalText(player);
					plain &= (text != null) && Q009Tests.Plain(text);
				}
				Q026.SetStage(player, Q026.Done);
				Report("journalTextsArePlain", plain);
			} finally {
				player.Account.PLevel = plevel;
			}
			Console.WriteLine("Q026_CHAIN_DONE: stage=" + Q026.GetStage(player));
		}
	}
}
