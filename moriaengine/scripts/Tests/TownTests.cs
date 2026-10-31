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

		/// <summary>The tax period is over.</summary>
		[SteamFunction]
		public static void TestTownTaxDue(Character self) {
			Town.ForCity(City).taxDueAt = Globals.TimeInSeconds - 1;
			Console.WriteLine("TOWN_DUE: done");
		}

		[SteamFunction]
		public static void TestTownSetMayor(Character self) {
			Town.ForCity(City).mayor = self;
			Console.WriteLine("TOWN_MAYOR: done");
		}

		[SteamFunction]
		public static void TestTownForSave(Character self) {
			Town town = Town.ForStone(FindStone(City), City);
			town.AddCitizen(self);
			town.mayor = self;
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
