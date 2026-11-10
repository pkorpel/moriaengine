using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#892 - server-side helpers for Tests/Protocol/TownTests.cs.</summary>
	public static class TownTests {
		private const string City = "Minas_Tirith";

		private static TownStone FindStone(string city) {
			foreach (Thing thing in Thing.AllThings) {
				TownStone stone = thing as TownStone;
				if ((stone != null) && (RegionEventSystem.AreaOf(stone.Region).City == city)) {
					return stone;
				}
			}
			return null;
		}

		[SteamFunction]
		public static void TestTownStones(Character self) {
			foreach (string city in TownSetup.GondorCities) {
				TownStone stone = FindStone(city);
				Console.WriteLine("TOWN_STONE: " + city + " at=" + ((stone != null) ? stone.P().ToString() : "none") +
					" area=" + ((stone != null) ? RegionEventSystem.AreaOf(stone.Region).Defname : "none") + " end");
			}
			Console.WriteLine("TOWN_STONES: done");
		}

		/// <summary>A plain Gondor player beside Minas Tirith's stone, with a level, a rank and gold.</summary>
		[SteamFunction]
		public static void TestTownPrepare(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			TownStone stone = FindStone(City);
			bool qualified = sa.Args.Trim().Trim('"') == "1";
			player.Realm = Realm.Gondor;
			player.Level = qualified ? TownSettings.instance.citizenMinLevel : 1;
			player.SkillRank = (SkillRank) (qualified ? TownSettings.instance.citizenMinRank : 0);
			player.P(stone.P());
			player.Account.PLevel = 1;
			if (qualified) {
				GoldUtils.Grant(player, 300000);
				Reputation.Set(player, VenueRegistry.CityAt(stone), ReputationSettings.instance.respectedFrom); //#1243 - citizenship wants a respected name
			}
			Console.WriteLine("TOWN_READY: stone=" + (uint) stone.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestTownReport(Character self) {
			Town town = Town.ForCity(City);
			if (town == null) {
				Console.WriteLine("TOWN_REPORT: none end");
				return;
			}
			TownCitizen citizen = town.FindCitizen(self);
			Console.WriteLine("TOWN_REPORT: citizens=" + town.citizens.Count + " citizen=" + (citizen != null) + " paid=" + ((citizen != null) && citizen.paidTax) +
				" autoPay=" + ((citizen != null) && citizen.autoPay) + " mayor=" + ((town.mayor != null) ? town.mayor.Name.Replace(' ', '_') : "none") +
				" candidates=" + town.candidates.Count + " votes=" + string.Join("+", town.candidates.ConvertAll(c => c.votes)) + " voters=" + town.voters.Count +
				" banned=" + town.banned.Count + " suggested=" + town.suggestedBans.Count + " rank=" + town.GetRank(self) + " uid=" + (uint) self.FlaggedUid +
				" tax=" + town.tax + " treasury=" + town.treasury + " gold=" + self.Gold + " chest=" + (town.taxChest != null) + " news=" + town.news.Count +
				" homeHere=" + object.Equals(((Player) self).Home, TownSetup.CityArea(City).P) + " end");
		}

		/// <summary>What a Gondor guard of the city makes of me, standing where I am, and a mask's worth of a new name.</summary>
		[SteamFunction]
		public static void TestTownGuardCheck(Character self) {
			NPC guard = (NPC) ThingDef.GetByDefname("c_vguard_gondor_hala_pesak").Create(TownSetup.CityArea(City).P);
			guard.AggroMode = AggroMode.GuardsRealm;
			bool protectedNow = GuardSpeech.IsProtectedBy(guard, self);
			string oldName = self.Name;
			bool attacks = guard.ShouldAggroOn(self);
			self.Name = "Maska";
			bool attacksMasked = guard.ShouldAggroOn(self);
			self.Name = oldName;
			guard.Delete();
			Console.WriteLine("TOWN_GUARD: attacks=" + attacks + " masked=" + attacksMasked + " protected=" + protectedNow + " end");
		}

		[SteamFunction]
		public static void TestTownCriminal(Character self) {
			Notoriety.FlagCriminal(self);
			NPC guard = (NPC) ThingDef.GetByDefname("c_vguard_gondor_hala_pesak").Create(TownSetup.CityArea(City).P);
			Console.WriteLine("TOWN_CRIMINAL: protected=" + GuardSpeech.IsProtectedBy(guard, self) + " end");
			guard.Delete();
		}

		/// <summary>Back to GM: the tests of this class share an account, and TestTownPrepare demotes it.</summary>
		[SteamFunction]
		public static void TestTownBecomeGm(Character self) {
			self.Account.PLevel = self.Account.MaxPLevel;
			Console.WriteLine("TOWN_GM: done");
		}

		/// <summary>Go to Gondor's stone, a step off it.</summary>
		[SteamFunction]
		public static void TestRealmStone(Character self) {
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is RealmStone stone) && (stone == RealmStone.CapitalStone(Realm.Gondor))) { //#904 - every city has one now
					self.P(stone.P());
					Console.WriteLine("REALM_STONE: uid=" + (uint) stone.FlaggedUid + " end");
					return;
				}
			}
			Console.WriteLine("REALM_STONE: uid=0 end");
		}

		private static string Clean(string text) {
			return (text == null) ? "none" : text.Replace(' ', '_');
		}

		[SteamFunction]
		public static void TestRealmReport(Character self) {
			RealmOffice office = RealmOffice.For(Realm.Gondor);
			AbstractItem cape = self.FindLayer(20);
			Console.WriteLine("REALM_REPORT: holder=" + Clean(office?.holder?.Name) + " name=" + Clean(self.Name) + " badge=" + (PowerBadge.Find(self) != null) +
				" uniform=" + Uniform.Has(self) + " uid=" + (uint) self.FlaggedUid + " str=" + self.Str + " vit=" + ((Player) self).Vit + " cape=0x" + ((cape != null) ? cape.Color.ToString("x", CultureInfo.InvariantCulture) : "none") +
				" horse=" + ((self.Mount != null) ? ("0x" + self.Mount.Color.ToString("x", CultureInfo.InvariantCulture) + "/0x" + self.Mount.MountItem.ToString("x", CultureInfo.InvariantCulture)) : "none") + " end");
		}

		/// <summary>The General's six pieces, the horse among them.</summary>
		[SteamFunction]
		public static void TestRealmDress(Character self) {
			foreach (string defname in new[] { "i_robe", "i_cape", "i_leather_gloves", "i_shoes_plain", "i_hat_floppy" }) {
				self.TryEquip((Item) ThingDef.GetByDefname(defname).Create(self.Backpack));
			}
			Character horse = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(self.P());
			self.Mount = horse;
			Console.WriteLine("REALM_DRESSED: done");
		}

		/// <summary>#908 - the full uniform again, then off the horse: the horse goes back to its own color and look.</summary>
		[SteamFunction]
		public static void TestRealmDismountCheck(Character self) {
			self.TryEquip((Item) ThingDef.GetByDefname("i_leather_gloves").Create(self.Backpack));
			Uniform.Refresh(self);
			Character horse = (Character) self.Mount;
			bool dressed = (horse.Color == 0x481) && (horse.MountItem == Uniform.UniformHorseItem);
			self.Dismount();
			Uniform.Refresh(self);
			Console.WriteLine("REALM_DISMOUNT: dressed=" + dressed + " restored=" + ((horse.Color != 0x481) && (horse.MountItem == horse.TypeDef.MountItem)) +
				" uniform=" + Uniform.Has(self) + " end");
		}

		[SteamFunction]
		public static void TestRealmUndress(Character self) {
			self.FindLayer(7).Cont = self.Backpack;
			Console.WriteLine("REALM_DRESSED: done");
		}

		[SteamFunction]
		public static void TestRealmDismiss(Character self) {
			RealmOffice.For(Realm.Gondor).Dismiss();
			Console.WriteLine("REALM_DISMISSED: done");
		}

		private static NazgulShrine FindShrine(bool leading) {
			foreach (Thing thing in Thing.AllThings) {
				if (thing is NazgulShrine shrine) {
					FlaggedRegion area = RegionEventSystem.AreaOf(shrine.Region);
					if (leading ? (area.Defname == "a_Fela_Nazgul") : (area.City == TownSetup.MordorCapital)) {
						return shrine;
					}
				}
			}
			return null;
		}

		private static NazgulGate FindGate(bool wayOut) {
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is NazgulGate gate) && ((RegionEventSystem.AreaOf(gate.Region).Defname == "a_Fela_Nazgul") == wayOut)) {
					return gate;
				}
			}
			return null;
		}

		/// <summary>A plain player fit for a ring, at Barad-dur's shrine; "G" makes it one of Gondor, level 30 too.</summary>
		[SteamFunction]
		public static void TestNazgulPrepare(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			NazgulShrine shrine = FindShrine(false);
			player.Realm = (sa.Args.Trim().Trim('"') == "G") ? Realm.Gondor : Realm.Mordor;
			player.Level = RealmSettings.instance.ringMinLevel;
			player.SkillRank = SkillRank.Grandmaster;
			player.P(shrine.P());
			player.Account.PLevel = 1;
			Console.WriteLine("NAZGUL_READY: shrine=" + (uint) shrine.FlaggedUid + " uid=" + (uint) self.FlaggedUid + " end");
		}

		private static Player nazgulUnderTest;

		[SteamFunction]
		public static void TestNazgulReport(Character self) {
			nazgulUnderTest = (self.Realm == Realm.Mordor) ? (Player) self : nazgulUnderTest;
			PowerBadge ring = NazgulRings.RingOf(self);
			Town town = Town.ForCity(TownSetup.MordorCapital);
			RealmOffice office = RealmOffice.For(Realm.Mordor);
			NazgulGate gate = FindGate(false);
			Console.WriteLine("NAZGUL_REPORT: nazgul=" + (ring != null) + " first=" + NazgulRings.IsFirst(self) + " name=" + Clean(self.Name) +
				" ruler=" + Clean(town?.mayor?.Name) + " office=" + Clean(office?.holder?.Name) + " kills=" + ((ring != null) ? NazgulRings.KillsWith(ring, self) : -1) +
				" deaths=" + ((ring != null) ? NazgulRings.DeathsOn(ring) : -1) + " str=" + self.Str + " vit=" + ((Player) self).Vit + " uniform=" + Uniform.Has(self) +
				" at=" + self.P().ToString().Replace(' ', '_') + " area=" + RegionEventSystem.AreaOf(self.Region).Defname + " moreP=" + gate.MoreP.ToString().Replace(' ', '_') +
				" trial=" + (RegionEventSystem.AreaOf(self.Region).Defname == "a_Fela_Nazgul") + " gate=0x" + gate.Color.ToString("x", CultureInfo.InvariantCulture) +
				" lying=" + LyingReport(town?.stone) + " leadingLying=" + LyingReport(LostRings.LeadingShrine()) +
				" horse=" + ((self.Mount != null) ? ("0x" + self.Mount.Color.ToString("x", CultureInfo.InvariantCulture) + "/0x" + self.Mount.MountItem.ToString("x", CultureInfo.InvariantCulture)) : "none") + " end");
		}

		//#910 - the ring lying at the capital's shrine: its uid and fire columns, or none
		private static string LyingReport(Item shrine) {
			PowerBadge lying = LostRings.LyingAt(shrine);
			return (lying == null) ? "none" : (((uint) lying.FlaggedUid).ToString(CultureInfo.InvariantCulture) + "/" + LostRings.ColumnsOf(lying) +
				(lying.HasAttribute(ItemAttributes.attr_move_never) ? "/fixed" : "/loose"));
		}

		/// <summary>#909 - fame for .nazghulfell; "death" instead: a death of mine with no killer (the First keeps the ring).</summary>
		[SteamFunction]
		public static void TestNazgulFell(Character self, ScriptArgs sa) {
			string arg = sa.Args.Trim().Trim('"');
			if (arg == "death") {
				NazgulRings.OnDeath((Player) self, null);
			} else {
				self.Fame = short.Parse(arg, CultureInfo.InvariantCulture);
			}
			Console.WriteLine("NAZGUL_FELL: fame=" + self.Fame + " end");
		}

		/// <summary>I die `count` times by the Nazgul under test.</summary>
		[SteamFunction]
		public static void TestNazgulDie(Character self, ScriptArgs sa) {
			for (int i = int.Parse(sa.Args.Trim().Trim('"'), CultureInfo.InvariantCulture); i > 0; i--) {
				NazgulRings.OnDeath((Player) self, nazgulUnderTest);
			}
			Console.WriteLine("NAZGUL_DIED: done");
		}

		/// <summary>The ring worn long enough to be judged, and the wearer's record: "1" online and killing
		/// enough, "0" not.</summary>
		[SteamFunction]
		public static void TestNazgulJudge(Character self, ScriptArgs sa) {
			PowerBadge ring = NazgulRings.RingOf(self);
			bool good = sa.Args.Trim().Trim('"') == "1";
			ring.SetTag(TagKey.Acquire("_ringEquippedAt_"), Globals.TimeInSeconds - (2 * 24 * 60 * 60));
			ring.SetTag(TagKey.Acquire("_ringOnlineMinutes_"), good ? (12 * 60) : 1);
			NazgulRings.Check(ring, self);
			Console.WriteLine("NAZGUL_JUDGED: done");
		}

		[SteamFunction]
		public static void TestNazgulDress(Character self) {
			foreach (string defname in new[] { "i_robe", "i_cape", "i_leather_gloves", "i_shoes_plain" }) {
				self.TryEquip((Item) ThingDef.GetByDefname(defname).Create(self.Backpack));
			}
			self.Mount = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(self.P());
			Console.WriteLine("NAZGUL_DRESSED: done");
		}

		[SteamFunction]
		public static void TestNazgulStep(Character self, ScriptArgs sa) {
			FindGate(sa.Args.Trim().Trim('"') == "out").On_Step(self, false, MovementType.Walking);
			Console.WriteLine("NAZGUL_STEPPED: done");
		}

		[SteamFunction]
		public static void TestNazgulLeading(Character self) {
			NazgulShrine shrine = FindShrine(true);
			self.P(shrine.P());
			Console.WriteLine("NAZGUL_LEADING: shrine=" + (uint) shrine.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestNazgulDismiss(Character self) {
			//#910 - as a live server would be: someone has used the lord nazguls' stone in Barad-dur, which makes it the office's stone
			RealmOffice.ForStone(RealmStone.CapitalStone(Realm.Mordor), Realm.Mordor); //#904 - Barad-dur's, of the five
			RealmOffice.For(Realm.Mordor).Dismiss();
			Console.WriteLine("NAZGUL_DISMISSED: done");
		}

		/// <summary>Every timer of the realm offices running: a timer that can't be saved breaks the whole save.</summary>
		[SteamFunction]
		public static void TestRealmTimersForSave(Character self) {
			self.AddTimer(TimerKey.Acquire("_testRealmEffect_"), new RealmStatEffect(0)).DueInSeconds = 600;
			self.AddTimer(TimerKey.Acquire("_testUniform_"), new UniformRefreshTimer()).DueInSeconds = 600;
			FindGate(false).AddTimer(TimerKey.Acquire("_testGate_"), new NazgulGateReset()).DueInSeconds = 600;
			NazgulTrial.BeginForTest((Player) self);
			GuardForce force = Town.ForCity(City).Force;
			force.Treasury = 100000;
			force.Hire(self, force.HireableDefnames()[0]);
			Console.WriteLine("REALM_TIMERS: ready");
		}

		/// <summary>What every city can hire, and the realms.</summary>
		[SteamFunction]
		public static void TestGuardTables(Character self) {
			foreach (string city in TownSetup.GondorCities) {
				Console.WriteLine("GUARD_TABLE: " + city + " " + new GuardForce { city = city }.HireableDefnames().Count + " end");
			}
			foreach (string city in TownSetup.MordorCities) {
				Console.WriteLine("GUARD_TABLE: " + city + " " + new GuardForce { city = city }.HireableDefnames().Count + " end");
			}
			Console.WriteLine("GUARD_TABLE: king " + new GuardForce { city = TownSetup.GeneralCity, isKings = true }.HireableDefnames().Count + " end");
			Console.WriteLine("GUARD_TABLES: done");
		}

		[SteamFunction]
		public static void TestGuardReport(Character self) {
			GuardForce force = Town.ForCity(City).Force;
			HiredGuard guard = (force.guards.Count > 0) ? force.guards[0] : null;
			NPCAiTimer ai = (guard != null && guard.IsStanding) ? NPCAiTimer.GetFor(guard.npc) : null;
			Console.WriteLine("GUARD_REPORT: guards=" + force.guards.Count + " standing=" + ((guard != null) && guard.IsStanding) + " held=" + ((ai != null) && ai.isHeld) +
				" treasury=" + force.Treasury + " near=" + ((guard != null) && guard.IsStanding && (Point2D.GetSimpleDistance(guard.npc.X, guard.npc.Y, self.X, self.Y) <= 1)) +
				" fightsMe=" + ((ai != null) && (ai.Target == self)) + " posX=" + self.X + " posY=" + self.Y + " posZ=" + self.Z + " end");
		}

		[SteamFunction]
		public static void TestGuardTime(Character self, ScriptArgs sa) {
			GuardForce force = Town.ForCity(City).Force;
			string what = sa.Args.Trim().Trim('"');
			if (what == "die") {
				force.guards[0].npc.Delete();
				force.Check();
				force.guards[0].respawnAt = Globals.TimeInSeconds - 1;
				force.Check();
			} else {
				force.guards[0].paidUntil = Globals.TimeInSeconds - 1;
				force.Check();
			}
			Console.WriteLine("GUARD_TIME: done");
		}

		/// <summary>#906 - a severed head at the caller's feet: the character's with this uid, or a fresh NPC's ("npc": a Gondor
		/// horse, which every ban check would let through if an NPC's head counted, and which leaves the Facts after it alone).</summary>
		[SteamFunction]
		public static void TestTownHead(Character self, ScriptArgs sa) {
			string arg = sa.Args.Trim().Trim('"');
			Character owner;
			if (arg == "npc") {
				owner = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(self.P());
				owner.Realm = Realm.Gondor;
			} else {
				owner = (Character) Thing.UidGetCharacter(uint.Parse(arg, System.Globalization.CultureInfo.InvariantCulture));
			}
			Item head = (Item) ((ItemDef) ThingDef.GetByDefname("i_flesh_head")).Create(self.X, self.Y, self.Z, self.M);
			head.SetTag(CorpseCarving.HeadOfTK, owner);
			Console.WriteLine("TOWN_HEAD: uid=" + (uint) head.FlaggedUid + " end");
		}

		/// <summary>#904 - beside the realm stone of another city: its uid, and the names of that city's king's guards and the
		/// capital's, from Erebor's tables.</summary>
		[SteamFunction]
		public static void TestRealmCityStone(Character self, ScriptArgs sa) {
			string city = sa.Args.Trim().Trim('"');
			Item found = null;
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is RealmStone stone) && (RegionEventSystem.AreaOf(stone.Region)?.City == city)) {
					found = stone;
				}
			}
			self.Go(found.X + 1, found.Y, found.Z, found.M);
			string Guard(string c) => ((ThingDef) Constant.GetByName("Guard_king_" + c + "_1").Value).Name.Replace(' ', '_');
			Console.WriteLine("REALM_CITY_STONE: uid=" + (uint) found.FlaggedUid + " cityGuard=" + Guard(city) + " capitalGuard=" + Guard(TownSetup.GeneralCity) + " end");
		}

		/// <summary>#905 - a new name (as a mask gives) and a place far off, or "back": the old name and place.</summary>
		[SteamFunction]
		public static void TestTownDisguise(Character self, ScriptArgs sa) {
			string arg = sa.Args.Trim().Trim('"');
			if (arg == "back") {
				self.Name = (string) self.GetTag(TagKey.Acquire("_test_oldName_"));
				self.P((Point4D) self.GetTag(TagKey.Acquire("_test_oldP_")));
			} else {
				self.SetTag(TagKey.Acquire("_test_oldName_"), self.Name);
				self.SetTag(TagKey.Acquire("_test_oldP_"), new Point4D(self.X, self.Y, self.Z, self.M));
				self.Name = arg;
				self.Go(self.X + 120, self.Y);
			}
			Console.WriteLine("TOWN_DISGUISE: name=" + self.Name.Replace(' ', '_') + " end");
		}

		[SteamFunction]
		public static void TestTownBanNames(Character self) {
			Town town = Town.ForCity(City);
			Console.WriteLine("TOWN_BAN_NAMES: " + string.Join("|", town.banned.ConvertAll(b => b.name.Replace(' ', '_'))) + " end");
		}

		[SteamFunction]
		public static void TestTownReportUid(Character self) {
			Console.WriteLine("TOWN_UID: uid=" + (uint) self.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestTownCriminalNow(Character self) {
			Notoriety.FlagCriminal(self);
			Console.WriteLine("TOWN_CRIMINAL_NOW: done");
		}

		private static VarGate nazgulOnlyGate;

		/// <summary>#912 - a var gate beside me with the Nazgul-only rule (f_vg_nazgulOnly), leading 6 tiles off.</summary>
		[SteamFunction]
		public static void TestNazgulGateMake(Character self) {
			nazgulOnlyGate = (VarGate) ThingDef.GetByDefname("i_varGate").Create(self.X + 1, self.Y, self.Z, self.M);
			nazgulOnlyGate.MoreP = new Point4D(self.X + 6, self.Y, self.Z, self.M);
			nazgulOnlyGate.GoHome = false;
			nazgulOnlyGate.Flags |= VarGateFlags.RunFunctions;
			nazgulOnlyGate.Functions.Add(new VarGateEntry { name = "f_vg_nazgulOnly", checkOnly = true });
			Console.WriteLine("NAZGUL_GATE: uid=" + (uint) nazgulOnlyGate.FlaggedUid + " x=" + (self.X + 6) + " y=" + self.Y + " z=" + self.Z + " end");
		}

		/// <summary>#912 - onto the gate, by a step; where I end up, and where it leads.</summary>
		[SteamFunction]
		public static void TestNazgulGateStep(Character self) {
			self.Go(nazgulOnlyGate.X - 1, nazgulOnlyGate.Y, nazgulOnlyGate.Z, nazgulOnlyGate.M);
			self.Go(nazgulOnlyGate.X, nazgulOnlyGate.Y, nazgulOnlyGate.Z, nazgulOnlyGate.M); //onto it: a Go fires the step triggers
			Console.WriteLine("NAZGUL_GATE_STEP: at=" + self.X + "," + self.Y + " leads=" + nazgulOnlyGate.MoreP.X + "," + nazgulOnlyGate.MoreP.Y + " end");
		}

		/// <summary>Barad-dur's ring straight on: the quest has its own test.</summary>
		[SteamFunction]
		public static void TestNazgulGiveRing(Character self) {
			Town town = Town.ForStone(FindShrine(false), TownSetup.MordorCapital);
			if (town.mayor != null) {
				NazgulRings.TakeAway(NazgulRings.RingOf(town.mayor), town.mayor);
			}
			NazgulRings.Give(self, town);
			Console.WriteLine("NAZGUL_GIVEN: done");
		}

		[SteamFunction]
		public static void TestNazgulForSave(Character self) {
			Player player = (Player) self;
			player.Realm = Realm.Mordor;
			player.Level = RealmSettings.instance.ringMinLevel;
			Town town = Town.ForStone(FindShrine(false), TownSetup.MordorCapital);
			NazgulRings.Give(player, town);
			Console.WriteLine("NAZGUL_SAVED: ready");
		}

		[SteamFunction]
		public static void TestNazgulAfterRestart(Character self) {
			Town town = Town.ForCity(TownSetup.MordorCapital);
			Character ruler = town?.mayor;
			Console.WriteLine("NAZGUL_LOADED: ruler=" + Clean(ruler?.Name) + " ring=" + ((ruler != null) && NazgulRings.IsNazgul(ruler)) + " end");
		}

		/// <summary>The tax period is over.</summary>
		[SteamFunction]
		public static void TestTownTaxDue(Character self) {
			Town.ForCity(City).taxDueAt = Globals.TimeInSeconds - 1;
			Console.WriteLine("TOWN_DUE: done");
		}

		[SteamFunction]
		public static void TestTownElectionOver(Character self) {
			Town.ForCity(City).electionStart = Globals.TimeInSeconds - ((TownSettings.instance.electionDays * 24 * 60 * 60) + 1);
			Console.WriteLine("TOWN_DUE: done");
		}

		[SteamFunction]
		public static void TestTownTermOver(Character self) {
			Town.ForCity(City).mayorSince = Globals.TimeInSeconds - ((TownSettings.instance.mayorTermDays * 24 * 60 * 60) + 1);
			Console.WriteLine("TOWN_DUE: done");
		}

		[SteamFunction]
		public static void TestTownSetMayor(Character self) {
			Town.ForCity(City).SetMayor(self);
			Console.WriteLine("TOWN_MAYOR: done");
		}

		[SteamFunction]
		public static void TestTownForSave(Character self) {
			Town town = Town.ForStone(FindStone(City), City);
			town.AddCitizen(self);
			town.SetMayor(self);
			town.treasury = 777;
			town.news.Add("Otevreno");
			Console.WriteLine("TOWN_SAVED: ready");
		}

		[SteamFunction]
		public static void TestTownAfterRestart(Character self) {
			Town town = Town.ForCity(City);
			Console.WriteLine("TOWN_LOADED: found=" + (town != null) + " stone=" + (town?.stone is TownStone) + " citizens=" + town?.citizens.Count +
				" mayor=" + town?.mayor?.Name.Replace(' ', '_') + " treasury=" + town?.treasury + " news=" + town?.news.Count +
				" taxDue=" + (town?.taxDueAt > Globals.TimeInSeconds) + " end");
		}
	}
}
