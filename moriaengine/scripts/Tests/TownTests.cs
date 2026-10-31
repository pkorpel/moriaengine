using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Regions;

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

		/// <summary>Go to Gondor's stone, a step off it.</summary>
		[SteamFunction]
		public static void TestRealmStone(Character self) {
			foreach (Thing thing in Thing.AllThings) {
				if (thing is RealmStone stone) {
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
				" uniform=" + Uniform.Has(self) + " uid=" + (uint) self.FlaggedUid + " str=" + self.Str + " vit=" + ((Player) self).Vit + " cape=0x" + ((cape != null) ? cape.Color.ToString("x", CultureInfo.InvariantCulture) : "none") + " end");
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
