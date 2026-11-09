using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1317 - the stories of Erebor and Dale on the server: the false Arkenstone (a client takes it, catches the con man and
	/// brings the stone, Tests/Protocol/EreborStoryTests.cs), then the ravens, the axe, the deep veins, the fish and the bargeman here.</summary>
	public static class EreborStoryTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>();

		private static void Report(string name, bool ok) {
			Console.WriteLine("EREBOR_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string key) {
			return TagKey.Acquire("_erebortest_" + key + "_");
		}

		private static CityVenue OpenAir(string city, string type, int[] spot, byte m) {
			VenueDraft draft = new VenueDraft();
			draft.SetType(VenueType.Get(type).IndexOf());
			draft.M = m;
			draft.City = city;
			draft.Rects.Add(new ImmutableRectangle(spot[0] - 2, spot[1] - 2, spot[0] + 2, spot[1] + 2));
			CityVenue venue = draft.Save(out string problem);
			if (venue == null) {
				throw new SEException(city + "'s " + type + " wasn't saved: " + problem);
			}
			Residents.FillRoles(venue);
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				NPCAiTimer.Freeze(Residents.BodyOf(gem), 900);
			}
			return venue;
		}

		private static List<int[]> Spots(Point4D p, int count) {
			List<int[]> spots = new List<int[]>();
			foreach (int[] tile in WorldEvents.Reachable(Map.GetMap(p.M), new[] { p.X, p.Y, p.Z }, 25)) {
				if ((WorldSpots.Distance(tile, p.X, p.Y) >= 3) && spots.TrueForAll(o => WorldSpots.Distance(tile, o[0], o[1]) >= 6)) {
					spots.Add(tile);
				}
			}
			if (spots.Count < count) {
				throw new SEException("Only " + spots.Count + " spots for test venues near " + p + ".");
			}
			return spots;
		}

		private static Item Gem(Player player, string key, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(key));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if ((role == null) || (Residents.RoleKeyOf(gem) == role)) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>A market and a smithy in Erebor, a harbour and a barracks in Dale, a market in Edoras; only the false Arkenstone
		/// offered; the hero beside a merchant of Erebor.</summary>
		[SteamFunction]
		public static void TestEreborSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D erebor = WorldEvents.CentreOf("Erebor"), dale = WorldEvents.CentreOf("Dol"), edoras = WorldEvents.CentreOf("Edoras");
				List<int[]> e = Spots(erebor, 3), d = Spots(dale, 2);
				player.SetTag(VenueKey("market"), OpenAir("Erebor", "market", e[0], erebor.M));
				player.SetTag(VenueKey("smithy"), OpenAir("Erebor", "smithy", e[1], erebor.M));
				player.SetTag(VenueKey("mine"), OpenAir("Erebor", "mine", e[2], erebor.M));
				player.SetTag(VenueKey("harbour"), OpenAir("Dol", "harbour", d[0], dale.M));
				player.SetTag(VenueKey("barracks"), OpenAir("Dol", "barracks", d[1], dale.M));
				player.SetTag(VenueKey("edoras"), OpenAir("Edoras", "market", Spots(edoras, 1)[0], edoras.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new FalseArkenstoneErrand());
				player.RemoveTag(Errands.LogTK);
				NPC merchant = Residents.BodyOf(Gem(player, "market", "merchant"));
				player.Go(new Point4D(merchant.X, merchant.Y, merchant.Z, merchant.M)); //#1324: Go keeps one in the halls under the mountain
				Console.WriteLine("EREBOR_SETUP: merchant=" + merchant.Uid + " gold=" + player.Gold + " talk=" + Errands.CanTalk(player, merchant)
					+ " at=" + merchant.X + "," + merchant.Y + "," + merchant.Z + " me=" + player.X + "," + player.Y + "," + player.Z + " realm=" + player.Realm);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		/// <summary>The hero beside the con man, held still; his uid and the errand's id (0s if none).</summary>
		[SteamFunction]
		public static void TestEreborBesideConMan(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "falsearkenstone");
			Thing conMan = (errand == null) ? null : Errands.ThingsOf(player, errand).Find(t => (t is NPC) && !t.IsDeleted);
			if (conMan != null) {
				Beside(player, conMan);
			}
			Console.WriteLine("EREBOR_CONMAN: uid=" + ((conMan == null) ? 0 : conMan.Uid) + " errand=" + ((errand == null) ? 0 : errand.id));
		}

		[SteamFunction]
		public static void TestEreborBesideGiver(Character self) {
			Player player = (Player) self;
			Errand errand = Errands.Active(player).Find(e => e.key == "falsearkenstone");
			NPC giver = (errand == null) ? null : Residents.BodyOf(Residents.ById(errand.giverId));
			if (giver != null) {
				player.Go(new Point4D(giver.X, giver.Y, giver.Z, giver.M)); //#1324: Go keeps one in the halls under the mountain
			}
			Item stone = (errand == null) ? null : errand.Current.placed;
			Report("theConManIsCaughtWithTheStone", (errand != null) && (errand.index == 1) && (stone != null) && !stone.IsDeleted && (stone.TopObj() == player));
			Console.WriteLine("EREBOR_GIVER: uid=" + ((giver == null) ? 0 : giver.Uid));
		}

		[SteamFunction]
		public static void TestEreborState(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			long gold = long.Parse(sa.Args.Trim().Trim('"'));
			Report("theStoneIsBrought", !Errands.Active(player).Exists(e => e.key == "falsearkenstone") && (player.Gold > gold));
			Console.WriteLine("EREBOR_STATE_DONE");
		}

		private static Errand Build(Player player, ErrandTemplate template, Item giver, string city) {
			Errand errand = new Errand { key = template.Key, name = template.Name, giverId = Residents.IdOf(giver), city = city, tier = (int) template.Tier };
			return template.TryBuild(new ErrandContext(player, giver, city), errand) ? errand : null;
		}

		private static void Fresh(Player player) {
			ErrandLog log = Errands.Log(player);
			foreach (Errand left in new List<Errand>(log.active)) {
				Errands.GiveUp(player, left);
			}
			log.day = -1;
			log.lastKey = "";
			log.streak = 0;
		}

		private static void Beside(Player player, Thing thing) {
			if (thing is NPC npc) {
				NPCAiTimer.Freeze(npc, 60);
			}
			player.Go(new Point4D(thing.X, thing.Y, thing.Z, thing.M)); //#1324: Go keeps one in the halls under the mountain
		}

		private static bool Talk(Player player, Errand errand, int choice = -1) {
			Item gem = (errand.Current == null) ? null : Residents.ById(errand.Current.residentId);
			if (gem == null) {
				return false; //a step without a resident (things): nobody to talk to
			}
			NPC body = Residents.BodyOf(gem);
			player.Go(new Point4D(body.X, body.Y, body.Z, body.M)); //#1324: Go keeps one in the halls under the mountain
			return Errands.Do(player, errand, body, choice);
		}

		[SteamFunction]
		public static void TestEreborChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			try {
				account.PLevel = 1;
				Chain(player);
			} finally {
				account.PLevel = plevel;
				foreach (ErrandTemplate template in removed) {
					ErrandTemplates.Register(template);
				}
				removed.Clear();
				Console.WriteLine("EREBOR_CHAIN_DONE");
			}
		}

		//the things of the current Things step used one by one; true when the step is done
		private static bool UseAll(Player player, Errand errand) {
			int index = errand.index;
			List<Thing> things = Errands.ThingsOf(player, errand);
			foreach (Thing thing in things) {
				if (!thing.IsDeleted) {
					Beside(player, thing);
					Errands.UseThing(thing, player);
				}
			}
			return (errand.index == index + 1) && things.TrueForAll(t => t.IsDeleted); //each goes as it's used
		}

		//a Catch step's NPCs caught one by one; true when the step is done
		private static bool CatchAll(Player player, Errand errand) {
			int index = errand.index;
			foreach (Thing thing in Errands.ThingsOf(player, errand)) {
				Beside(player, thing);
				Errands.UseThing(thing, player);
			}
			return errand.index == index + 1;
		}

		private static void Chain(Player player) {
			ErrandTemplates.Register(new RavenPostErrand());
			ErrandTemplates.Register(new AncestralAxeErrand());
			ErrandTemplates.Register(new DeepMiningErrand());
			ErrandTemplates.Register(new FishQuotaErrand());
			ErrandTemplates.Register(new BargemanErrand());
			Item merchant = Gem(player, "market", "merchant"), smith = Gem(player, "smithy", "smith"), harbour = Gem(player, "harbour", "harbourmaster");
			Item daleGuard = Gem(player, "barracks", null), edorasMerchant = Gem(player, "edoras", "merchant");
			player.SetRealSkillValue((int) SkillName.Mining, 500);
			Report("onlyTheirOwnCities", (Build(player, new RavenPostErrand(), edorasMerchant, "Edoras") == null) && (Build(player, new AncestralAxeErrand(), edorasMerchant, "Edoras") == null)
				&& (Build(player, new FalseArkenstoneErrand(), edorasMerchant, "Edoras") == null) && (Build(player, new FishQuotaErrand(), edorasMerchant, "Edoras") == null)
				&& (Build(player, new FishQuotaErrand(), merchant, "Erebor") == null) && (Build(player, new RavenPostErrand(), harbour, "Dol") == null)
				&& (Build(player, new DeepMiningErrand(), merchant, "Erebor") == null) && (Build(player, new BargemanErrand(), harbour, "Dol") != null)
				&& (Build(player, new AncestralAxeErrand(), Gem(player, "mine", "foreman"), "Erebor") == null) && (Build(player, new FalseArkenstoneErrand(), Gem(player, "mine", "foreman"), "Erebor") == null)
				&& (Build(player, new BargemanErrand(), smith, "Erebor") == null) && (Build(player, new DeepMiningErrand(), Gem(player, "mine", "foreman"), "Erebor") != null)
				&& (Build(player, new FishQuotaErrand(), harbour, "Dol") != null) && (Build(player, new DeepMiningErrand(), smith, "Erebor") != null));

			//the ravens: the perch stands in Dale's streets; the message left there; told
			Fresh(player);
			Errand ravens = Build(player, new RavenPostErrand(), merchant, "Erebor");
			bool flown = false;
			if ((ravens != null) && Errands.Accept(player, ravens)) {
				List<Thing> perch = Errands.ThingsOf(player, ravens);
				Point4D dale = WorldEvents.CentreOf("Dol");
				flown = (perch.Count == 1) && (Point2D.GetSimpleDistance(perch[0], dale) <= Errands.ThingSpread) && UseAll(player, ravens) && Talk(player, ravens) && !Errands.Active(player).Contains(ravens);
			}
			Report("theRavensCarryItToDale", flown);

			//the axe: the orc laid, the axe taken, home
			Fresh(player);
			Errand axe = Build(player, new AncestralAxeErrand(), smith, "Erebor");
			bool home = false;
			if ((axe != null) && Errands.Accept(player, axe)) {
				List<Thing> orc = Errands.ThingsOf(player, axe);
				if ((orc.Count == 1) && (orc[0] is NPC)) {
					((NPC) orc[0]).CauseDeath(player);
				}
				home = (axe.index == 1) && UseAll(player, axe) && Talk(player, axe) && !Errands.Active(player).Contains(axe);
			}
			Report("theAncestralAxeComesHome", home);

			//the deep veins: not offered below Mining 40; below it a vein won't give; then struck
			Fresh(player);
			player.SetRealSkillValue((int) SkillName.Mining, 100);
			bool notOffered = Build(player, new DeepMiningErrand(), smith, "Erebor") == null;
			player.SetRealSkillValue((int) SkillName.Mining, 500);
			Errand mining = Build(player, new DeepMiningErrand(), smith, "Erebor");
			bool struck = false;
			if ((mining != null) && Errands.Accept(player, mining)) {
				List<Thing> veins = Errands.ThingsOf(player, mining);
				player.SetRealSkillValue((int) SkillName.Mining, 100);
				Beside(player, veins[0]);
				Errands.UseThing(veins[0], player);
				bool refused = !veins[0].IsDeleted && (mining.index == 0);
				player.SetRealSkillValue((int) SkillName.Mining, 500);
				struck = refused && UseAll(player, mining) && Talk(player, mining) && !Errands.Active(player).Contains(mining);
			}
			Report("theDeepVeinsNeedAMiner", notOffered && struck);

			//the fish quota: ten raw fish taken
			Fresh(player);
			Errand fish = Build(player, new FishQuotaErrand(), harbour, "Dol");
			bool quota = false;
			if ((fish != null) && Errands.Accept(player, fish)) {
				Item catchOf = (Item) ((ItemDef) ThingDef.GetByDefname("i_fish_raw")).Create(player.Backpack);
				catchOf.Amount = FishQuotaErrand.Fish;
				quota = Talk(player, fish) && !Errands.Active(player).Contains(fish) && catchOf.IsDeleted;
			}
			Report("theFishQuotaIsMet", quota);

			//the bargeman: caught on the road out, his crate brought
			Fresh(player);
			Errand barge = Build(player, new BargemanErrand(), daleGuard, "Dol");
			bool smuggled = false;
			if ((barge != null) && Errands.Accept(player, barge)) {
				smuggled = CatchAll(player, barge) && (barge.Current.placed != null) && Talk(player, barge) && !Errands.Active(player).Contains(barge);
			}
			Report("theBargemanIsCaughtWithHisCrate", smuggled);

			//Erebor is one area of two parts 1600 tiles apart: a market in the halls deep in the mountain gets its things there, not at the gate
			Fresh(player);
			Point4D gate = WorldEvents.CentreOf("Erebor");
			int[] deep = WorldSpots.NearestStandable(Map.GetMap(gate.M), 4808, 2088, 40);
			bool inTheHalls = false;
			if (deep != null) {
				byte plevel = player.Account.PLevel;
				player.Account.PLevel = 7;
				CityVenue hallMarket = OpenAir("Erebor", "market", deep, gate.M);
				player.Account.PLevel = plevel;
				Item deepMerchant = null;
				foreach (Item gem in Residents.OfVenue(hallMarket.VenueId)) {
					deepMerchant = (Residents.RoleKeyOf(gem) == "merchant") ? gem : deepMerchant;
				}
				Errand con = Build(player, new FalseArkenstoneErrand(), deepMerchant, "Erebor");
				if ((con != null) && Errands.Accept(player, con)) {
					List<Thing> conMan = Errands.ThingsOf(player, con);
					inTheHalls = (conMan.Count == 1) && (Point2D.GetSimpleDistance(conMan[0], hallMarket) < 40) && (Point2D.GetSimpleDistance(conMan[0], gate) > 1000);
					Errands.GiveUp(player, con);
				}
			}
			Report("aTwoPartTownsThingsAreInTheGiversPart", inTheHalls);
		}
	}
}
