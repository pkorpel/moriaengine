using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1293 - the doing errand templates on the engine's own steps, on the server: each built for a fitting giver and
	/// played to its end with its choices' effects. Tests/Protocol/DoingErrandTests.cs asks a harbour worker for work first.</summary>
	public static class DoingErrandTests {
		private static readonly List<ErrandTemplate> removed = new List<ErrandTemplate>(); //put back when the chain ends

		private static void Report(string name, bool ok) {
			Console.WriteLine("DOINGERRANDS_CHECK: " + name + "=" + ok);
		}

		private static TagKey VenueKey(string type) {
			return TagKey.Acquire("_doingerrands_" + type + "_");
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

		/// <summary>A tavern, temple, harbour and smithy in Edoras, a tavern in Minas Tirith (for the courier), a mess hall in the war
		/// camp; only the stowaway offered; the hero beside the harbourmaster.</summary>
		[SteamFunction]
		public static void TestDoingErrandsSetup(Character self) {
			Player player = (Player) self;
			byte plevel = player.Account.PLevel;
			player.Account.PLevel = 7;
			try {
				Point4D edoras = WorldEvents.CentreOf("Edoras");
				string[] types = { "tavern", "temple", "harbour", "smithy" };
				List<int[]> spots = Spots(edoras, types.Length);
				for (int i = 0; i < types.Length; i++) {
					player.SetTag(VenueKey(types[i]), OpenAir("Edoras", types[i], spots[i], edoras.M));
				}
				Point4D tirith = WorldEvents.CentreOf("Minas_Tirith");
				player.SetTag(VenueKey("far"), OpenAir("Minas_Tirith", "tavern", Spots(tirith, 1)[0], tirith.M));
				Point4D camp = WorldEvents.CentreOf("Barad-dur");
				player.SetTag(VenueKey("camp"), OpenAir("Barad-dur", "messhall", Spots(camp, 1)[0], camp.M));
				foreach (ErrandTemplate template in new List<ErrandTemplate>(ErrandTemplates.All)) {
					removed.Add(template);
					ErrandTemplates.Unregister(template.Key);
				}
				ErrandTemplates.Register(new StowawayErrand());
				player.RemoveTag(Errands.LogTK);
				NPC master = Residents.BodyOf(Gem(player, "harbour", "harbourmaster"));
				player.Go(new Point4D(master.X, master.Y, master.Z, master.M));
				Console.WriteLine("DOINGERRANDS_SETUP: master=" + master.Uid);
			} finally {
				player.Account.PLevel = plevel;
			}
		}

		[SteamFunction]
		public static void TestDoingErrandsState(Character self) {
			int stowaway = Errands.Active((Player) self).FindAll(e => e.key == "stowaway").Count;
			Console.WriteLine("DOINGERRANDS_STATE: stowaway=" + stowaway);
		}

		private static Item Gem(Player player, string type, string role) {
			CityVenue venue = (CityVenue) player.GetTag(VenueKey(type));
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if ((role == null) || (Residents.RoleKeyOf(gem) == role)) {
					return gem;
				}
			}
			return null;
		}

		private static void SetFriendship(Player player, string residentId, int value) {
			Residents.AddFriendship(player, residentId, value - Residents.Friendship(player, residentId));
		}

		private static Errand Build(Player player, ErrandTemplate template, Item giver, string city = "Edoras") {
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

		private static bool Play(Player player, Errand errand, int choice) {
			Errands.Accept(player, errand);
			for (int guard = 0; (guard < 12) && !errand.IsDone; guard++) {
				ErrandStep step = errand.Current;
				if (step.Kind == ErrandStepKind.Fetch) {
					for (int i = 0; i < step.count; i++) {
						ThingDef.GetByDefname(step.itemDef).Create(player.Backpack);
					}
				}
				NPC body = Residents.BodyOf(Residents.ById(step.residentId));
				player.Go(new Point4D(body.X, body.Y, body.Z, body.M));
				if (!Errands.Do(player, errand, body, (step.Kind == ErrandStepKind.Choose) ? choice : -1)) {
					return false;
				}
			}
			return errand.IsDone;
		}

		private static bool Plain(Errand errand) {
			List<string> texts = new List<string> { errand.name, errand.offer, errand.thanks };
			foreach (ErrandStep step in errand.steps) {
				texts.AddRange(new[] { step.task, step.say, step.reply });
				texts.AddRange(step.choices);
				texts.AddRange(step.choiceReplies);
			}
			foreach (string text in texts) {
				foreach (char c in text ?? "") {
					if (c > 127) {
						return false;
					}
				}
			}
			return true;
		}

		[SteamFunction]
		public static void TestDoingErrandsChain(Character self) {
			Player player = (Player) self;
			AbstractAccount account = player.Account;
			byte plevel = account.PLevel;
			ushort spiritSpeak = (ushort) player.GetSkill((int) SkillName.SpiritSpeak);
			try {
				account.PLevel = 1; //a player pays for a bet; a GM wouldn't
				player.Realm = Realm.Gondor;
				DoingErrands.Bootstrap(); //the setup left only the stowaway: the finish looks a template up by key
				int baseSmall = ErrandSettings.instance.friendSmall, baseMedium = ErrandSettings.instance.friendMedium;
				GoldUtils.Grant(player, 2000);
				Item innkeeper = Gem(player, "tavern", "innkeeper"), priest = Gem(player, "temple", "priest"), master = Gem(player, "harbour", "harbourmaster");
				Item smith = Gem(player, "smithy", "smith"), campCook = Gem(player, "camp", null);
				List<Errand> built = new List<Errand>();

				//a commission: something made, brought to the giver
				Fresh(player);
				Errand commission = Build(player, new CommissionErrand(), smith);
				Report("commission", (commission != null) && (commission.steps[0].Kind == ErrandStepKind.Fetch) && Play(player, commission, 0));
				built.Add(commission);

				//the courier race: to the realm's other city; a short deadline; faster pays more
				Fresh(player);
				Errand courier = Build(player, new CourierErrand(), innkeeper);
				bool courierOk = (courier != null) && (Residents.WorkplaceOf(Residents.ById(courier.steps[0].residentId)).City == "Minas_Tirith");
				if (courierOk) {
					Errands.Accept(player, courier);
					CourierErrand template = new CourierErrand();
					courierOk &= ((courier.Expires - DateTime.UtcNow).TotalMinutes <= 30.5) && (template.PayPercent(courier) == CourierErrand.FastPercent);
					courier.expiresTicks = DateTime.UtcNow.AddMinutes(5).Ticks;
					courierOk &= template.PayPercent(courier) == 100; //slow: the usual pay
					Errands.GiveUp(player, courier);
					Fresh(player);
					courier = Build(player, new CourierErrand(), innkeeper);
					courierOk &= Play(player, courier, 0);
				}
				Report("courier", courierOk);
				built.Add(courier);

				//words for the dead: only for one who hears the dead, and only at night
				Fresh(player);
				player.SetRealSkillValue((int) SkillName.SpiritSpeak, 0);
				bool deaf = Build(player, new GhostWordsErrand(), priest) == null;
				player.SetRealSkillValue((int) SkillName.SpiritSpeak, 500);
				Errand ghost = Build(player, new GhostWordsErrand(), priest);
				bool ghostOk = deaf && (ghost != null) && (ghost.steps[0].Kind == ErrandStepKind.Wait) && (Build(player, new GhostWordsErrand(), innkeeper) == null);
				if (ghost != null) {
					VenueClock.Override = 12;
					Errands.Accept(player, ghost);
					NPC priestBody = Residents.BodyOf(priest);
					player.Go(new Point4D(priestBody.X, priestBody.Y, priestBody.Z, priestBody.M));
					ghostOk &= !Errands.Do(player, ghost, priestBody); //by day the dead are quiet
					Errands.GiveUp(player, ghost);
					Fresh(player);
					ghost = Build(player, new GhostWordsErrand(), priest);
					VenueClock.Override = 23;
					ghostOk &= Play(player, ghost, 0);
					VenueClock.Override = null;
				}
				Report("ghostwords", ghostOk);
				built.Add(ghost);

				//the brawl: split them, the innkeeper's thanks; bet, paid at the end, won double or lost, and the innkeeper pays nothing
				Fresh(player);
				SetFriendship(player, Residents.IdOf(innkeeper), 5);
				Errand brawl = Build(player, new BrawlErrand(), innkeeper);
				bool brawlOk = (brawl != null) && Play(player, brawl, 0) && (Residents.Friendship(player, Residents.IdOf(innkeeper)) == 5 + baseSmall + 1);
				long[] betGain = new long[2];
				for (int roll = 0; roll < 2; roll++) {
					Fresh(player);
					BrawlErrand.forcedRoll = (roll == 0) ? 0 : 99;
					Errand bet = Build(player, new BrawlErrand(), innkeeper);
					long before = GoldUtils.CountGoldOn(player);
					brawlOk &= (bet != null) && Play(player, bet, 1);
					betGain[roll] = GoldUtils.CountGoldOn(player) - before;
				}
				BrawlErrand.forcedRoll = null;
				Report("brawl", brawlOk && (betGain[0] == BrawlErrand.Bet) && (betGain[1] == -BrawlErrand.Bet) && (Build(player, new BrawlErrand(), priest) == null));
				built.Add(brawl);

				//the stowaway: handed over pleases the harbour; let go, the stowaway's friend, half the pay
				long[] gained = new long[2];
				bool stowOk = true;
				Errand stow = null;
				for (int answer = 0; answer < 2; answer++) {
					Fresh(player);
					SetFriendship(player, Residents.IdOf(master), 5);
					stow = Build(player, new StowawayErrand(), master);
					if (stow == null) {
						stowOk = false;
						break;
					}
					string stowaway = stow.steps[0].residentId;
					SetFriendship(player, stowaway, 5);
					long before = GoldUtils.CountGoldOn(player);
					stowOk &= Play(player, stow, answer);
					gained[answer] = GoldUtils.CountGoldOn(player) - before;
					stowOk &= (answer == 0) ? (Residents.Friendship(player, Residents.IdOf(master)) == 5 + baseMedium + 1)
						: ((Residents.Friendship(player, Residents.IdOf(master)) == 5 + baseMedium - 1) && (Residents.Friendship(player, stowaway) == 7));
				}
				Report("stowaway", stowOk && (gained[1] < gained[0]) && (Build(player, new StowawayErrand(), innkeeper) == null));
				built.Add(stow);

				//the war camp gives orders, not these; every text plain
				ErrandTemplate[] doing = { new CommissionErrand(), new CourierErrand(), new GhostWordsErrand(), new BrawlErrand(), new StowawayErrand() };
				Report("notInTheCamp", (campCook != null) && (Build(player, new LetterErrand(), campCook, "Barad-dur") != null)
					&& Array.TrueForAll(doing, t => Build(player, t, campCook, "Barad-dur") == null));
				Report("plainCzech", built.TrueForAll(e => (e != null) && Plain(e)));
			} finally {
				VenueClock.Override = null;
				BrawlErrand.forcedRoll = null;
				player.SetRealSkillValue((int) SkillName.SpiritSpeak, spiritSpeak);
				Fresh(player);
				account.PLevel = plevel;
				foreach (ErrandTemplate template in removed) {
					if (ErrandTemplates.Get(template.Key) == null) {
						ErrandTemplates.Register(template);
					}
				}
				Console.WriteLine("DOINGERRANDS_CHAIN_DONE");
			}
		}
	}
}
