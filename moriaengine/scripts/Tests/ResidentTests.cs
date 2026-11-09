using System;
using System.Collections.Generic;
using System.IO;
using SteamEngine.Common;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1242 - residents checked on the server; Tests/Protocol/ResidentTests.cs drives these and .resident through a client.</summary>
	public static class ResidentTests {
		private static readonly TriggerKey spawnNowTK = TriggerKey.Acquire("spawnNow");
		private static readonly TimerKey spawnTimerTK = TimerKey.Acquire("SpawnTimer");

		private static void Report(string name, bool ok) {
			Console.WriteLine("RESIDENT_CHECK: " + name + "=" + ok);
		}

		private static void Clean() {
			VenueClock.Override = null;
			foreach (Item gem in new List<Item>(Residents.AllGems)) {
				NPC body = Residents.BodyOf(gem);
				if (body != null) {
					body.Delete();
				}
				gem.Delete();
			}
			foreach (CityVenue venue in new List<CityVenue>(VenueRegistry.All)) {
				venue.Delete();
			}
			Residents.Reset();
			VenueRegistry.Reset();
		}

		/// <summary>The two biggest rooms of Edoras, as found by the venue scan.</summary>
		private static List<FoundRoom> EdorasRooms(Character self) {
			return RoomScan.Candidates("Edoras", self.MovementSettings);
		}

		private static CityVenue VenueOf(FoundRoom room, string typeKey, string title) {
			VenueDraft draft = VenueDraft.OfRoom(room, room.M, "Edoras");
			draft.SetType(VenueType.Get(typeKey).IndexOf());
			draft.Title = title;
			string problem;
			return draft.Save(out problem);
		}

		/// <summary>An open-air venue of the given type around a city area's middle.</summary>
		private static CityVenue OpenAirIn(string regionDefname, string city, string typeKey) {
			StaticRegion area = StaticRegion.GetByDefname(regionDefname);
			ImmutableRectangle rect = area.Rectangles[0];
			Map map = Map.GetMap(area.Mapplane);
			int[] spot = WorldSpots.NearestStandable(map, rect.MinX + (rect.Width / 2), rect.MinY + (rect.Height / 2), 20);
			VenueDraft draft = new VenueDraft();
			draft.SetType(VenueType.Get(typeKey).IndexOf());
			draft.M = (byte) area.Mapplane;
			draft.City = city;
			draft.Rects.Add(new ImmutableRectangle(spot[0] - 3, spot[1] - 3, spot[0] + 3, spot[1] + 3));
			string problem;
			return draft.Save(out problem);
		}

		private static Item Role(CityVenue venue, string roleKey) {
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if (Residents.RoleKeyOf(gem) == roleKey) {
					return gem;
				}
			}
			return null;
		}

		private static bool HomePointIn(NPC npc, CityVenue venue) {
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			return (ai != null) && ai.hasHome && venue.Contains(ai.homeX, ai.homeY, ai.homeZ);
		}

		[SteamFunction]
		public static void TestResidentFill(Character self) {
			Clean();
			List<FoundRoom> rooms = EdorasRooms(self);
			Report("edorasHasTwoRooms", rooms.Count >= 2);
			if (rooms.Count < 2) {
				Console.WriteLine("RESIDENT_FILL_DONE");
				return;
			}
			self.Go(new Point4D(rooms[0].Middle[0], rooms[0].Middle[1], rooms[0].Middle[2], rooms[0].M));
			CityVenue tavern = VenueOf(rooms[0], "tavern", "U Bileho kone");
			CityVenue house = VenueOf(rooms[1], "house", "Dum u brany");
			VenueClock.Override = 12; //midday: the innkeeper works, the evening patrons are home
			int added = Residents.FillRoles(tavern);
			Report("aTavernGetsItsSevenPeople", added == 7);
			Report("fillingAgainAddsNobody", Residents.FillRoles(tavern) == 0);
			List<Item> gems = Residents.OfVenue(tavern.VenueId);
			HashSet<string> names = new HashSet<string>();
			bool allNamed = true, allBodies = true, bodiesCarryTheName = true, allHaveHome = true, idsFollowTheRole = true;
			foreach (Item gem in gems) {
				NPC body = Residents.BodyOf(gem);
				allNamed &= Residents.NameOf(gem).Length > 0;
				names.Add(Residents.NameOf(gem));
				allBodies &= body != null;
				bodiesCarryTheName &= (body != null) && (body.Name == Residents.NameOf(gem)) && !string.IsNullOrEmpty(body.Title);
				allHaveHome &= Residents.HomeIdOf(gem) == house.VenueId;
				idsFollowTheRole &= Residents.IdOf(gem).StartsWith(tavern.VenueId + "/" + Residents.RoleKeyOf(gem) + "/", StringComparison.Ordinal);
				Console.WriteLine("RESIDENT: " + Residents.IdOf(gem) + " " + Residents.NameOf(gem) + " " + string.Join(",", Residents.TraitsOf(gem)) +
					" at=" + ((body == null) ? "-" : body.P().ToString()));
			}
			Report("everyoneHasAName", allNamed);
			Report("namesDifferInTheCity", names.Count == gems.Count);
			Report("everyoneHasABody", allBodies);
			Report("theBodyCarriesTheNameAndRole", bodiesCarryTheName);
			Report("theCitysHouseIsHome", allHaveHome);
			Report("idsFollowTheRole", idsFollowTheRole);
			Report("residentsOfEdorasAreGondors", (Residents.BodyOf(gems[0]) != null) && (Residents.BodyOf(gems[0]).Realm == Realm.Gondor));
			Report("humansInEdoras", (Residents.BodyOf(gems[0]) != null) && Residents.BodyOf(gems[0]).Def.PrettyDefname.StartsWith("c_resident_", StringComparison.Ordinal) &&
				(Residents.BodyOf(gems[0]).Def.PrettyDefname != "c_resident_orc"));
			Report("theyAreCalm", (Residents.BodyOf(gems[0]) != null) && (Residents.BodyOf(gems[0]).AggroMode == AggroMode.Calm));

			Item innkeeper = Role(tavern, "innkeeper"), patron = Role(tavern, "patron"), cook = Role(tavern, "cook");
			Report("atMiddayTheInnkeeperIsAtWork", tavern.Contains(Residents.BodyOf(innkeeper)));
			Report("andThePatronAtHome", house.Contains(Residents.BodyOf(patron)));
			VenueClock.Override = 20; //evening: the patrons come to the tavern
			Residents.SetTraits(cook, "morous,lakomy"); //a sociable cook would go to the tavern too: fixed traits, both ways checked
			ResidentSchedule.ApplyAll();
			Report("inTheEveningThePatronHeadsForTheTavern", HomePointIn(Residents.BodyOf(patron), tavern));
			//with a player near, the walk is real: the patron, at home since midday, gets a goal point that keeps it awake and stepping
			IPoint3D goal = Residents.BodyOf(patron).PetGoalPoint;
			Report("andWalksThereWhileWatched", !tavern.Contains(Residents.BodyOf(patron)) && (goal != null) && tavern.Contains(goal.X, goal.Y, goal.Z));
			Report("andTheCookGoesHome", HomePointIn(Residents.BodyOf(cook), house));
			Residents.SetTraits(cook, "spolecensky,morous");
			ResidentSchedule.ApplyAll();
			Report("aSociableCookGoesToTheTavernInstead", HomePointIn(Residents.BodyOf(cook), tavern));

			VenueClock.Override = 2; //night: everyone home but those always at work
			ResidentSchedule.ApplyAll();
			Report("atNightTheInnkeeperIsHomeToo", HomePointIn(Residents.BodyOf(innkeeper), house));
			Report("theLeashIsSmall", NPCAiTimer.GetFor(Residents.BodyOf(innkeeper)).WanderLeash == 2);
			int[] first = ResidentSchedule.SpotIn(tavern, Residents.IdOf(innkeeper));
			ResidentSchedule.ForgetSpots();
			int[] again = ResidentSchedule.SpotIn(tavern, Residents.IdOf(innkeeper));
			Report("eachHasAFixedSpot", (first != null) && (again != null) && (first[0] == again[0]) && (first[1] == again[1]) && (first[2] == again[2]));
			HashSet<long> spread = new HashSet<long>();
			foreach (Item gem in gems) {
				int[] spot = ResidentSchedule.SpotIn(tavern, Residents.IdOf(gem));
				spread.Add(((long) spot[0] << 16) | (uint) spot[1]);
			}
			Report("theySpreadOverTheRoom", spread.Count >= 3);

			//what a resident thinks of you lives on you
			string id = Residents.IdOf(innkeeper);
			//a saved tag's name is read back by \w+: the key must hold word characters only, or a restart loses it
			Report("theFriendshipKeyLoadsBack", System.Text.RegularExpressions.Regex.IsMatch(Residents.FriendshipKey(id).Name, @"^\w+$") &&
				(Residents.FriendshipKey(id) != Residents.FriendshipKey("venue/Edoras/1/innkeeper/2")));
			Report("aStrangerIsZero", Residents.Friendship(self, id) == 0);
			Residents.AddFriendship(self, id, 3);
			Report("helpRaisesIt", Residents.Friendship(self, id) == 3);
			Residents.AddFriendship(self, id, 20);
			Report("itStopsAtTen", Residents.Friendship(self, id) == Residents.MaxFriendship);
			Residents.AddFriendship(self, id, -50);
			Report("andAtZero", Residents.Friendship(self, id) == 0);

			//dead and back: the same person
			NPC body1 = Residents.BodyOf(innkeeper);
			string name = Residents.NameOf(innkeeper);
			body1.Delete();
			Report("deadHasNoBody", Residents.BodyOf(innkeeper) == null);
			if (innkeeper.GetTimer(spawnTimerTK) != null) {
				innkeeper.DeleteTimer(spawnTimerTK);
			}
			innkeeper.TryTrigger(spawnNowTK, new ScriptArgs());
			NPC body2 = Residents.BodyOf(innkeeper);
			Report("theGemBringsTheSamePersonBack", (body2 != null) && (body2 != body1) && (body2.Name == name));
			Report("theSchedulerRuns", Globals.Instance.HasTimer(TimerKey.Acquire("_resident_scheduler_")));
			VenueClock.Override = null;
			Console.WriteLine("RESIDENT_FILL_DONE");
		}

		[SteamFunction]
		public static void TestResidentExport(Character self) {
			List<Item> before = new List<Item>(Residents.AllGems);
			Report("thereAreResidents", before.Count == 7);
			Dictionary<string, string> who = new Dictionary<string, string>();
			foreach (Item gem in before) {
				who[Residents.IdOf(gem)] = Residents.NameOf(gem) + "|" + Residents.RoleKeyOf(gem) + "|" + Residents.HomeIdOf(gem) + "|" + string.Join(",", Residents.TraitsOf(gem)) + "|" + Residents.IsFemale(gem);
			}
			string path = Path.Combine(Path.GetTempPath(), "moria-residents-" + Guid.NewGuid().ToString("N") + ".txt");
			VenueFile.PathOverride = path;
			try {
				VenueFile.Export(path);
				string tavernId = Residents.VenueIdOf(before[0]);
				VenueRegistry.ById(tavernId).Delete(); //its people go with it (CityVenue.On_Destroy)
				Report("removingTheVenueRemovesItsPeople", Residents.OfVenue(tavernId).Count == 0);
				Report("nobodyLeft", Residents.AllGems.Count == 0);
				new VenueSetup().Run(self);
				bool same = Residents.AllGems.Count == 7;
				foreach (Item gem in Residents.AllGems) {
					string was;
					same &= who.TryGetValue(Residents.IdOf(gem), out was) &&
						(was == Residents.NameOf(gem) + "|" + Residents.RoleKeyOf(gem) + "|" + Residents.HomeIdOf(gem) + "|" + string.Join(",", Residents.TraitsOf(gem)) + "|" + Residents.IsFemale(gem));
				}
				Report("setupBringsBackTheSamePeople", same);
				bool named = true;
				foreach (Item gem in Residents.AllGems) {
					NPC body = Residents.BodyOf(gem);
					named &= (body != null) && (body.Name == Residents.NameOf(gem));
				}
				Report("withBodiesNamedAtOnce", named);
				new VenueSetup().Run(self);
				Report("aSecondRunAddsNobody", Residents.AllGems.Count == 7);
			} finally {
				VenueFile.PathOverride = null;
				File.Delete(path);
				Console.WriteLine("RESIDENT_EXPORT_DONE");
			}
		}

		[SteamFunction]
		public static void TestResidentRaces(Character self) {
			CityVenue messhall = OpenAirIn("a_Barad_dur", "Barad-dur", "messhall");
			CityVenue inn = OpenAirIn("a_Hobitin", "Hobitin", "tavern");
			Report("bothVenuesExist", (messhall != null) && (inn != null));
			if ((messhall == null) || (inn == null)) {
				Console.WriteLine("RESIDENT_RACES_DONE");
				return;
			}
			Residents.FillRoles(messhall);
			Residents.FillRoles(inn);
			bool orcs = true, mordor = true;
			foreach (Item gem in Residents.OfVenue(messhall.VenueId)) {
				NPC body = Residents.BodyOf(gem);
				orcs &= (body != null) && (body.Def.PrettyDefname == "c_resident_orc") && !Residents.IsFemale(gem);
				mordor &= (body != null) && (body.Realm == Realm.Mordor);
			}
			Report("baradDursFolkAreOrcs", orcs);
			Report("ofMordor", mordor);
			List<string> hobbitNames = new List<string> { "Bilbo", "Bungo", "Drogo", "Folco", "Fredegar", "Hamfast", "Halfred", "Hob", "Isembard", "Largo", "Lotho", "Milo", "Mungo",
				"Odo", "Otho", "Paladin", "Posco", "Ponto", "Robin", "Sancho", "Tolman", "Wilcome", "Wiseman", "Angelica", "Belba", "Bell", "Daisy", "Dora", "Elanor", "Esmeralda", "Lily",
				"Lobelia", "Marigold", "Mentha", "Mimosa", "Pansy", "Pearl", "Peony", "Poppy", "Primula", "Rose", "Ruby", "Tanta" };
			bool hobbits = true;
			foreach (Item gem in Residents.OfVenue(inn.VenueId)) {
				hobbits &= hobbitNames.Contains(Residents.NameOf(gem));
			}
			Report("hobitinHasHobbitNames", hobbits);
			//a venue whose type changes keeps only the new type's people
			VenueDraft draft = VenueDraft.For(inn);
			draft.SetType(VenueType.Get("bakery").IndexOf());
			string problem;
			draft.Save(out problem);
			int stray = Residents.RemoveStrayRoles(inn);
			Report("aNewTypeSendsTheOldRolesAway", (stray == 7) && (Residents.OfVenue(inn.VenueId).Count == 0) && (Residents.FillRoles(inn) == 1));
			Console.WriteLine("RESIDENT_RACES_DONE");
		}

		/// <summary>The innkeeper of Edoras's tavern, put beside the GM for the .resident test.</summary>
		[SteamFunction]
		public static void TestResidentBeside(Character self) {
			foreach (Item gem in Residents.AllGems) {
				if (Residents.RoleKeyOf(gem) == "innkeeper") {
					NPC body = Residents.BodyOf(gem);
					if ((body != null) && string.Equals(Residents.WorkplaceOf(gem).City, "Edoras", StringComparison.Ordinal)) {
						self.Go(new Point4D(body.X, body.Y, body.Z, body.M));
						Console.WriteLine("RESIDENT_BESIDE uid=" + (uint) body.FlaggedUid + " name=" + body.Name.Replace(' ', '_'));
						return;
					}
				}
			}
			Console.WriteLine("RESIDENT_BESIDE none");
		}

		/// <summary>#1315 - a healing venue's healer and herbalist are healers (a patient isn't); a gem a GM moved inside the
		/// venue is the resident's spot, for this body and the next; the NPC's .info Home reads it and moves it (inside the
		/// venue only), and an NPC that isn't a resident takes any home.</summary>
		[SteamFunction]
		public static void TestResidentHomes(Character self) {
			Player player = (Player) self;
			try {
				Clean();
				CityVenue lecebna = OpenAirIn("a_Edoras", "Edoras", "healing");
				Residents.FillRoles(lecebna);
				Item healer = null, herbalist = null, patient = null;
				foreach (Item gem in Residents.OfVenue(lecebna.VenueId)) {
					switch (Residents.RoleKeyOf(gem)) {
						case "healer": healer = gem; break;
						case "herbalist": herbalist = gem; break;
						case "patient": patient = gem; break;
					}
				}
				AbilityDef healerDef = MonsterSpecialAttacks.HealerDef;
				Report("theHealersHeal", (healer != null) && (herbalist != null) && Residents.BodyOf(healer).HasAbility(healerDef, out _) && Residents.BodyOf(herbalist).HasAbility(healerDef, out _));
				Report("aPatientDoesnt", (patient != null) && !Residents.BodyOf(patient).HasAbility(healerDef, out _));
				Report("aGemOnItsOwnSpotIsntMoved", (patient != null) && (ResidentSchedule.MovedSpot(patient) == null) && (ResidentSchedule.MovedSpot(healer) == null));
				NPC healerBody = Residents.BodyOf(healer);
				NPCAiTimer.Freeze(healerBody, 60);
				player.Go(new Point4D(healerBody.X + 1, healerBody.Y, healerBody.Z, healerBody.M));
				player.CauseDeath(null);
				player.SetTag(TagKey.Acquire("_diedAt_"), Globals.TimeInSeconds - 10); //past #1070's 5 s before a ghost may rise
				bool dead = player.Flag_Dead;
				healerBody.Trigger_Click(player); //a 3.0.6m client's click
				Report("aGhostClickingTheHealerLives", dead && !player.Flag_Dead);
				if (!player.Flag_Dead) {
					player.CauseDeath(null);
				}
				player.SetTag(TagKey.Acquire("_diedAt_"), Globals.TimeInSeconds - 10);
				dead = player.Flag_Dead;
				healerBody.Trigger_AosClick(player); //#1122 - TazUO's (an AOS-tooltip client) goes this way
				Report("anAosClientsClickToo", dead && !player.Flag_Dead);
				if (player.Flag_Dead) {
					player.Resurrect();
				}

				//the gem moved to another spot of the venue: the home goes there, also for the next body
				int[] was = new[] { healer.X, healer.Y, healer.Z };
				int[] moved = null;
				for (int i = 0; (moved == null) && (i < 50); i++) {
					int[] spot = ResidentSchedule.SpotIn(lecebna, "moved/" + i);
					if ((spot != null) && (Point2D.GetSimpleDistance(spot[0], spot[1], was[0], was[1]) >= 2)) {
						moved = spot;
					}
				}
				healer.P(moved[0], moved[1], moved[2], lecebna.M);
				ResidentSchedule.ApplyAll();
				NPCAiTimer ai = NPCAiTimer.GetFor(healerBody);
				Report("aMovedGemMovesTheHome", (ai.homeX == moved[0]) && (ai.homeY == moved[1]));
				healerBody.Delete();
				if (healer.GetTimer(spawnTimerTK) != null) {
					healer.DeleteTimer(spawnTimerTK);
				}
				healer.TryTrigger(spawnNowTK, new ScriptArgs());
				NPC next = Residents.BodyOf(healer);
				NPCAiTimer nextAi = (next == null) ? null : NPCAiTimer.GetFor(next);
				Report("theNextBodyKeepsIt", (nextAi != null) && (nextAi.homeX == moved[0]) && (nextAi.homeY == moved[1]) && (next.X == moved[0]) && (next.Y == moved[1])
					&& next.HasAbility(healerDef, out _));

				//.info's Home: read, moved inside the venue (the gem goes too), refused outside it
				Report("infoShowsTheHome", (string) Dialogs.NPCDescriptor.GetHome(next) == moved[0] + "," + moved[1] + "," + moved[2] + "," + lecebna.M);
				Dialogs.NPCDescriptor.SetHome(next, was[0] + "," + was[1] + "," + was[2]);
				Report("infoMovesAResidentsHome", (nextAi.homeX == was[0]) && (nextAi.homeY == was[1]) && (healer.X == was[0]) && (healer.Y == was[1]));
				bool refused = false;
				try {
					Dialogs.NPCDescriptor.SetHome(next, (lecebna.X + 40) + "," + lecebna.Y + "," + lecebna.Z);
				} catch (SEException) {
					refused = true;
				}
				bool leashRefused = false;
				try {
					Dialogs.NPCDescriptor.SetLeash(next, 9);
				} catch (SEException) {
					leashRefused = true;
				}
				Report("butOnlyInsideItsVenue", refused && (nextAi.homeX == was[0]) && (healer.X == was[0]));
				Report("andNotItsLeash", leashRefused);
				//the moved spot goes into .venues export, and .SetupWorld puts the resident back on it
				healer.P(moved[0], moved[1], moved[2], lecebna.M);
				string healerId = Residents.IdOf(healer), lecebnaId = lecebna.VenueId;
				string path = Path.Combine(Path.GetTempPath(), "moria-residents-" + Guid.NewGuid().ToString("N") + ".txt");
				VenueFile.PathOverride = path;
				try {
					VenueFile.Export(path);
					bool written = File.ReadAllText(path).Contains("|" + moved[0] + "," + moved[1] + "," + moved[2] + "\n");
					lecebna.Delete();
					new VenueSetup().Run(self);
					Item back = Residents.ById(healerId);
					NPCAiTimer backAi = ((back == null) || (Residents.BodyOf(back) == null)) ? null : NPCAiTimer.GetFor(Residents.BodyOf(back));
					Report("theMovedSpotSurvivesARebuild", written && (back != null) && (back.X == moved[0]) && (back.Y == moved[1]) && (backAi != null) && (backAi.homeX == moved[0])
						&& (VenueRegistry.ById(lecebnaId) != null));
				} finally {
					VenueFile.PathOverride = null;
					File.Delete(path);
				}
				NPC sheep = (NPC) ((NPCDef) ThingDef.GetByDefname("c_rabbit")).Create(player.X, player.Y, player.Z, player.M);
				NPCAiTimer sheepAi = NPCAiTimer.GetFor(sheep);
				if (sheepAi != null) {
					Dialogs.NPCDescriptor.SetHome(sheep, (player.X + 30) + "," + player.Y + "," + player.Z);
				}
				Report("anyOtherNpcTakesAnyHome", (sheepAi != null) && sheepAi.hasHome && (sheepAi.homeX == player.X + 30) && (sheepAi.homeY == player.Y));
				sheep.Delete();

				//a count lowered: a setup line beyond it isn't brought back; a new type starts from its own counts
				CityVenue again = VenueRegistry.ById(lecebnaId);
				Residents.SetCount(again, Array.Find(again.VenueType.Roles, r => r.Key == "patient"), 0);
				bool fits = !Residents.Fits(again, "patient", lecebnaId + "/patient/1") && Residents.Fits(again, "healer", lecebnaId + "/healer/1");
				bool counted = Residents.CountsText(again) == "patient=0";
				VenueDraft retyped = VenueDraft.For(again);
				retyped.SetType(VenueType.Get("tavern").IndexOf());
				bool saved = retyped.Save(out string problem) != null;
				retyped = VenueDraft.For(again);
				retyped.SetType(VenueType.Get("healing").IndexOf()); //and back: the old patient count mustn't come back with it
				saved &= retyped.Save(out problem) != null;
				Report("countsFollowTheTypeAndTheSetup", fits && counted && saved && (Residents.CountsText(again) == ""));
			} finally {
				Clean();
				Console.WriteLine("RESIDENT_HOMES_DONE");
			}
		}

		[SteamFunction]
		public static void TestResidentRenamed(Character self) {
			bool found = false;
			foreach (Item gem in Residents.AllGems) {
				found |= (Residents.NameOf(gem) == "Hamund") && (Residents.BodyOf(gem) != null) && (Residents.BodyOf(gem).Name == "Hamund");
			}
			Report("theDialogRenamedHim", found);
			Console.WriteLine("RESIDENT_RENAME_DONE");
		}
	}
}
