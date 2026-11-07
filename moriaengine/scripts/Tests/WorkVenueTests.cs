using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1258 - the armoury, the smithy, the temple and the Houses of Healing checked on the server;
	/// Tests/Protocol/WorkVenueTests.cs drives these and works the bellows through a client. Edoras gets one of each and a barracks.</summary>
	public static class WorkVenueTests {
		private static readonly string[] types = { "armoury", "smithy", "temple", "healing", "barracks" };

		private static void Report(string name, bool ok) {
			Console.WriteLine("WORKVENUE_CHECK: " + name + "=" + ok);
		}

		private static CityVenue Venue(Player player, string type) {
			return (CityVenue) player.GetTag(TagKey.Acquire("_workvenue_" + type + "_"));
		}

		private static Item Gem(Player player, string type, string role) {
			return Theatre.GemOf(Venue(player, type), role);
		}

		private static NPC Body(Player player, string type, string role) {
			return Residents.BodyOf(Gem(player, type, role));
		}

		private static void Inside(CityVenue venue, Item gem) {
			int[] spot = ResidentSchedule.SpotIn(venue, Residents.IdOf(gem));
			Residents.BodyOf(gem).Go(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], venue.M));
		}

		private static void Near(Player player, NPC npc) {
			player.Go(new Point4D(npc.X, npc.Y, npc.Z, npc.M));
		}

		private static List<string> Offers(Player player, string type, string role) {
			Item gem = Gem(player, type, role);
			List<string> keys = new List<string>();
			foreach (VenueAction action in VenueActions.For(player, Residents.BodyOf(gem), gem)) {
				keys.Add(action.Key);
			}
			return keys;
		}

		private static void Do(Player player, string key, string type, string role) {
			Item gem = Gem(player, type, role);
			VenueActions.Get(key).Do(player, Residents.BodyOf(gem), gem);
		}

		private static int Friend(Player player, string type, string role) {
			return Residents.Friendship(player, Residents.IdOf(Gem(player, type, role)));
		}

		private static void Give(Player player, string defname, int amount) {
			Item item = (Item) ((ItemDef) ThingDef.GetByDefname(defname)).Create(player.Backpack);
			item.Amount = amount;
		}

		private static int Count(Player player, string defname) {
			return Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname(defname));
		}

		[SteamFunction]
		public static void TestWorkVenueSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				VenueClock.Override = 12;
				List<FoundRoom> rooms = RoomScan.Candidates("Edoras", player.MovementSettings);
				for (int i = 0; i < types.Length; i++) {
					VenueDraft draft = VenueDraft.OfRoom(rooms[i], rooms[i].M, "Edoras");
					draft.SetType(VenueType.Get(types[i]).IndexOf());
					string problem;
					CityVenue venue = draft.Save(out problem);
					if (venue == null) {
						throw new SEException("venue " + types[i] + " wasn't saved: " + problem);
					}
					Residents.FillRoles(venue);
					foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
						NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
						Inside(venue, gem);
					}
					player.SetTag(TagKey.Acquire("_workvenue_" + types[i] + "_"), venue);
				}
				player.Stam = 50;
				NPC smith = Body(player, "smithy", "smith");
				Near(player, smith);
				Console.WriteLine("WORKVENUE_SETUP: smith=" + smith.Uid + " bellows=" + VenueActions.ButtonOf(VenueActions.Get("smithy_bellows")));
			} finally {
				player.Account.PLevel = plevel;
				Console.WriteLine("WORKVENUE_SETUP_DONE");
			}
		}

		[SteamFunction]
		public static void TestWorkVenueAfterClient(Character self) {
			Player player = (Player) self;
			try {
				Report("theClientWorkedTheBellows", player.GetTag(WorkVenues.BellowsAtTK) != null);
				player.RemoveTag(WorkVenues.BellowsAtTK);
				player.RemoveTag(WorkVenues.BellowsDayTK);
			} finally {
				Console.WriteLine("WORKVENUE_CLIENT_DONE");
			}
		}

		[SteamFunction]
		public static void TestWorkVenueRules(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			try {
				player.Account.PLevel = 1;
				GoldUtils.Grant(player, 50000);
				Smithy(player);
				Temple(player);
				Healing(player);
				Errand(player);
			} finally {
				player.Account.PLevel = plevel;
				Errands.NowOverride = null;
				VenueClock.Override = 12;
				Console.WriteLine("WORKVENUE_RULES_DONE");
			}
		}

		private static void Smithy(Player player) {
			Near(player, Body(player, "smithy", "smith"));
			List<string> smith = Offers(player, "smithy", "smith"), apprentice = Offers(player, "smithy", "apprentice");
			Report("theSmithyOffers", smith.Contains("smithy_bellows") && smith.Contains("smithy_ore") && apprentice.Contains("smithy_bellows") && !apprentice.Contains("smithy_ore"));

			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;
			player.Stam = 50;
			int f0 = Friend(player, "smithy", "smith");
			long g0 = GoldUtils.CountGoldOn(player);
			Do(player, "smithy_bellows", "smithy", "smith");
			bool worked = (player.Stam == 50 - VenueLifeSettings.instance.bellowsStamina) && (GoldUtils.CountGoldOn(player) - g0 == VenueLifeSettings.instance.bellowsWage) &&
				(Friend(player, "smithy", "smith") == Math.Min(Residents.MaxFriendship, f0 + 1));
			long g1 = GoldUtils.CountGoldOn(player);
			Do(player, "smithy_bellows", "smithy", "smith");
			bool rests = GoldUtils.CountGoldOn(player) == g1;
			Errands.NowOverride = start.AddMinutes(VenueLifeSettings.instance.bellowsMinutes + 1);
			Do(player, "smithy_bellows", "smithy", "smith");
			bool again = (GoldUtils.CountGoldOn(player) - g1 == VenueLifeSettings.instance.bellowsWage) && (Friend(player, "smithy", "smith") == Math.Min(Residents.MaxFriendship, f0 + 1));
			Errands.NowOverride = start.AddMinutes(2 * (VenueLifeSettings.instance.bellowsMinutes + 1));
			player.Stam = 3;
			long g2 = GoldUtils.CountGoldOn(player);
			Do(player, "smithy_bellows", "smithy", "smith");
			Report("theBellowsTakeStaminaAndRest", worked && rests && again && (GoldUtils.CountGoldOn(player) == g2) && (player.Stam == 3));
			player.Stam = 50;

			Errands.NowOverride = start;
			VenueGoods.ForgetDay(player, "smithore");
			Give(player, "i_ore_iron", 5);
			long g3 = GoldUtils.CountGoldOn(player);
			Do(player, "smithy_ore", "smithy", "smith");
			bool first = (Count(player, "i_ore_iron") == 0) && (GoldUtils.CountGoldOn(player) - g3 == 5 * VenueLifeSettings.instance.oreWage);
			Give(player, "i_ore_iron", 30);
			Do(player, "smithy_ore", "smithy", "smith");
			int left = Count(player, "i_ore_iron");
			Do(player, "smithy_ore", "smithy", "smith");
			Report("theSmithBuysOreUpToADay", first && (left == 30 - (VenueLifeSettings.instance.orePerDay - 5)) && (Count(player, "i_ore_iron") == left));
			Errands.NowOverride = null;
		}

		private static void Temple(Player player) {
			NPC priest = Body(player, "temple", "priest");
			Near(player, priest);
			Report("theTempleOffers", Offers(player, "temple", "priest").Contains("temple_offering") && Offers(player, "temple", "acolyte").Contains("temple_offering") &&
				!Offers(player, "temple", "pilgrim").Contains("temple_offering"));

			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;
			int k0 = player.Karma, f0 = Friend(player, "temple", "priest");
			long g0 = GoldUtils.CountGoldOn(player);
			Do(player, "temple_offering", "temple", "priest");
			bool given = (g0 - GoldUtils.CountGoldOn(player) == VenueLifeSettings.instance.offeringPrice) && (player.Karma == Math.Min(short.MaxValue, k0 + VenueLifeSettings.instance.offeringKarma)) &&
				(Friend(player, "temple", "priest") == Math.Min(Residents.MaxFriendship, f0 + 1));
			long g1 = GoldUtils.CountGoldOn(player);
			Do(player, "temple_offering", "temple", "priest");
			bool once = GoldUtils.CountGoldOn(player) == g1;
			Errands.NowOverride = start.AddHours(25);
			Do(player, "temple_offering", "temple", "priest");
			Report("anOfferingOnceADay", given && once && (g1 - GoldUtils.CountGoldOn(player) == VenueLifeSettings.instance.offeringPrice));

			//the vigil: at night, inside, to its end
			Errands.NowOverride = start;
			VenueClock.Override = 12;
			bool notByDay = !Offers(player, "temple", "priest").Contains("temple_vigil");
			VenueClock.Override = 23;
			bool byNight = Offers(player, "temple", "priest").Contains("temple_vigil");
			int k1 = player.Karma;
			Do(player, "temple_vigil", "temple", "priest");
			VigilTimer vigil = player.GetTimer(VigilTimer.Key) as VigilTimer;
			bool started = (vigil != null) && !Offers(player, "temple", "priest").Contains("temple_vigil");
			int half = VenueLifeSettings.instance.vigilSeconds / 2;
			bool goesOn = (vigil != null) && vigil.Step(player, half);
			bool ends = (vigil != null) && !vigil.Step(player, VenueLifeSettings.instance.vigilSeconds - half);
			player.DeleteTimer(VigilTimer.Key);
			Report("aVigilAtNightToItsEnd", notByDay && byNight && started && goesOn && ends && (player.Karma == Math.Min(short.MaxValue, k1 + VenueLifeSettings.instance.vigilKarma)) &&
				(player.GetTag(WorkVenues.VigilDoneTK) != null));
			Do(player, "temple_vigil", "temple", "priest");
			bool onceADay = player.GetTimer(VigilTimer.Key) == null;
			Errands.NowOverride = start.AddHours(25);
			Do(player, "temple_vigil", "temple", "priest");
			VigilTimer second = player.GetTimer(VigilTimer.Key) as VigilTimer;
			int k2 = player.Karma;
			player.Go(new Point4D((ushort) (player.X + 40), player.Y, player.Z, player.M));
			bool broken = (second != null) && !second.Step(player, VenueLifeSettings.instance.vigilSeconds);
			player.DeleteTimer(VigilTimer.Key);
			Report("leavingBreaksTheVigil", onceADay && broken && (player.Karma == k2));
			VenueClock.Override = 12;
			Errands.NowOverride = null;
		}

		private static void Healing(Player player) {
			Item patient = Gem(player, "healing", "patient");
			NPC body = Residents.BodyOf(patient);
			Near(player, body);
			Report("patientsWantDressing", Offers(player, "healing", "patient").Contains("healing_bandage") && !Offers(player, "healing", "healer").Contains("healing_bandage"));
			DateTime start = DateTime.UtcNow;
			Errands.NowOverride = start;
			player.SetRealSkillValue((int) SkillName.Healing, 0);
			Give(player, "i_bandage", 2);
			Do(player, "healing_bandage", "healing", "patient");
			bool unskilled = Count(player, "i_bandage") == 2;
			player.SetRealSkillValue((int) SkillName.Healing, 500);
			int f0 = Friend(player, "healing", "patient");
			long g0 = GoldUtils.CountGoldOn(player);
			Do(player, "healing_bandage", "healing", "patient");
			bool dressed = (Count(player, "i_bandage") == 1) && (GoldUtils.CountGoldOn(player) - g0 == VenueLifeSettings.instance.bandageWage) &&
				(Friend(player, "healing", "patient") == Math.Min(Residents.MaxFriendship, f0 + 1));
			Do(player, "healing_bandage", "healing", "patient");
			bool fresh = Count(player, "i_bandage") == 1;
			Errands.Consume(player.Backpack, (ItemDef) ThingDef.GetByDefname("i_bandage"), 1);
			Errands.NowOverride = start.AddHours(25);
			long g1 = GoldUtils.CountGoldOn(player);
			Do(player, "healing_bandage", "healing", "patient");
			Report("dressingNeedsSkillABandageAndADay", unskilled && dressed && fresh && (GoldUtils.CountGoldOn(player) == g1));
			Errands.NowOverride = null;
		}

		private static void FreshLog(Player player) {
			ErrandLog log = Errands.Log(player);
			foreach (Errand left in new List<Errand>(log.active)) {
				Errands.GiveUp(player, left);
			}
			log.day = -1;
			log.lastKey = "";
			log.streak = 0;
		}

		private static Errand Build(Player player, string key, Item giver) {
			ErrandTemplate template = ErrandTemplates.Get(key);
			Errand errand = new Errand { key = key, name = template.Name, giverId = Residents.IdOf(giver), city = "Edoras", tier = (int) template.Tier };
			return template.TryBuild(new ErrandContext(player, giver, "Edoras"), errand) ? errand : null;
		}

		private static bool Step(Player player, Errand errand, int choice = -1) {
			Item gem = Residents.ById(errand.Current.residentId);
			Near(player, Residents.BodyOf(gem));
			return Errands.Do(player, errand, Residents.BodyOf(gem), choice);
		}

		private static void Errand(Player player) {
			Item quartermaster = Gem(player, "armoury", "quartermaster"), smith = Gem(player, "smithy", "smith");
			Item pilgrim = Gem(player, "temple", "pilgrim"), healer = Gem(player, "healing", "healer");
			Report("eachAskedOnlyByItsRole", (Build(player, "requisition", Gem(player, "armoury", "armourer")) == null) && (Build(player, "blade", quartermaster) == null) &&
				(Build(player, "pilgrim", Gem(player, "temple", "priest")) == null) && (Build(player, "fever", Gem(player, "healing", "patient")) == null));

			//requisition: two iron weapons
			FreshLog(player);
			Errand req = Build(player, "requisition", quartermaster);
			Errands.Accept(player, req);
			ErrandStep fetch = req.steps[0];
			Give(player, fetch.itemDef, 1);
			bool short1 = !Step(player, req);
			Give(player, fetch.itemDef, 1);
			Report("aRequisitionWantsTwoIronWeapons", (fetch.Kind == ErrandStepKind.Fetch) && (fetch.count == 2) && fetch.itemDef.EndsWith("_iron", StringComparison.Ordinal) &&
				short1 && Step(player, req) && req.IsDone);

			//the stock theft: quiet makes a friend of the thief; turning in pays a bigger share (compared as shares: the first errand's
			//experience can raise the level, and the level raises all pay)
			long[] gained = new long[2];
			int[] share = new int[2];
			int thiefFriend = 0, thiefBefore = 0;
			for (int choice = 0; choice < 2; choice++) {
				FreshLog(player);
				Errand theft = Build(player, "stocktheft", quartermaster);
				Errands.Accept(player, theft);
				string thiefId = theft.steps[1].residentId;
				if (choice == 0) {
					thiefBefore = Residents.Friendship(player, thiefId);
				}
				long g = GoldUtils.CountGoldOn(player);
				Step(player, theft);
				Step(player, theft, choice);
				Step(player, theft);
				gained[choice] = theft.IsDone ? GoldUtils.CountGoldOn(player) - g : -1;
				share[choice] = ErrandTemplates.Get("stocktheft").PayPercent(theft);
				if (choice == 0) {
					thiefFriend = Residents.Friendship(player, thiefId);
				}
			}
			Report("theTheftEndsTwoWays", (gained[0] > 0) && (gained[1] > 0) && (share[1] > share[0]) && (thiefFriend == Math.Min(Residents.MaxFriendship, thiefBefore + 2)));

			//the repaired blade goes to the barracks
			FreshLog(player);
			Errand blade = Build(player, "blade", smith);
			Errands.Accept(player, blade);
			Item owner = (blade == null) ? null : Residents.ById(blade.steps[0].residentId);
			bool toBarracks = (owner != null) && (Residents.WorkplaceOf(owner) == Venue(player, "barracks")) && (Errands.CountIn(player.Backpack, (ItemDef) ThingDef.GetByDefname("i_sword_long_iron")) == 0);
			Report("theBladeGoesToTheBarracks", toBarracks && Step(player, blade) && blade.IsDone);

			//the pilgrim: the right way pays more than a wrong one
			long[] paid = new long[2];
			bool threeWays = true;
			for (int round = 0; round < 2; round++) {
				FreshLog(player);
				Errand way = Build(player, "pilgrim", pilgrim);
				Errands.Accept(player, way);
				ErrandStep ask = way.steps[0];
				threeWays &= ask.choices.Count == 3;
				int pick = (round == 0) ? ask.count : (ask.count + 1) % 3;
				long g = GoldUtils.CountGoldOn(player);
				Step(player, way, pick);
				paid[round] = way.IsDone ? GoldUtils.CountGoldOn(player) - g : -1;
			}
			Report("thePilgrimsRightWayPaysMore", threeWays && (paid[0] > paid[1]) && (paid[1] >= 0));

			//the fever: sit with the patient, come back at night, tell the healer
			FreshLog(player);
			Errand fever = Build(player, "fever", healer);
			Errands.Accept(player, fever);
			bool sat = Step(player, fever);
			VenueClock.Override = 12;
			bool notByDay = !Step(player, fever);
			VenueClock.Override = 23;
			bool atNight = Step(player, fever);
			Report("aFeverNightToItsEnd", sat && notByDay && atNight && Step(player, fever) && fever.IsDone);
			VenueClock.Override = 12;
			FreshLog(player);
		}
	}
}
