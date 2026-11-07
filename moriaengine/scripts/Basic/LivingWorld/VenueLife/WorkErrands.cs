using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1258 - the errands of the armoury, the smithy, the temple and the Houses of Healing (design 9.2).</summary>
	public static class WorkErrands {
		public static void Bootstrap() {
			ErrandTemplates.Register(new RequisitionErrand());
			ErrandTemplates.Register(new StockTheftErrand());
			ErrandTemplates.Register(new RepairedBladeErrand());
			ErrandTemplates.Register(new PilgrimWayErrand());
			ErrandTemplates.Register(new FeverErrand());
		}

		internal static Item SameVenue(Item gem, string role) {
			CityVenue venue = Residents.WorkplaceOf(gem);
			return (venue == null) ? null : Theatre.GemOf(venue, role);
		}
	}

	/// <summary>The quartermaster's requisition: two iron weapons the smiths make.</summary>
	internal sealed class RequisitionErrand : ErrandTemplate {
		public override string Key { get { return "requisition"; } }
		public override string Name { get { return "Rekvizice"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		private static readonly string[][] goods = {
			new[] { "i_kryss_iron", "zelezne kryssy" }, new[] { "i_dagger_iron", "zelezne dyky" },
			new[] { "i_sword_long_iron", "zelezne dlouhe mece" }, new[] { "i_spear_iron", "zelezna kopi" }
		};

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (Residents.RoleKeyOf(c.GiverGem) != "quartermaster") {
				return false;
			}
			string[] good = goods[Globals.dice.Next(goods.Length)];
			if (!(ThingDef.GetByDefname(good[0]) is ItemDef)) {
				return false;
			}
			const int count = 2;
			e.offer = "Straz potrebuje novou vyzbroj a sklad je prazdny. Prines mi " + count + " " + good[1] + ". Kovari je umi udelat.";
			e.thanks = "Dobra prace. Zapisu to do knih.";
			e.steps.Add(Errands.Fetch(c.GiverGem, good[0], count, "Prines " + c.GiverName + " " + count + " " + good[1], "Tady je vyzbroj pro straz.",
				"Pekny kus prace."));
			return true;
		}
	}

	/// <summary>The stock doesn't add up: the armourer says who had the key, the player lets the thief return it quietly (a new
	/// friend) or turns them in (more pay), then reports back.</summary>
	internal sealed class StockTheftErrand : ErrandTemplate {
		public override string Key { get { return "stocktheft"; } }
		public override string Name { get { return "Kradez ze skladu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (Residents.RoleKeyOf(c.GiverGem) != "quartermaster") {
				return false;
			}
			Item armourer = WorkErrands.SameVenue(c.GiverGem, "armourer");
			CityVenue armoury = Residents.WorkplaceOf(c.GiverGem);
			Item suspect = c.PickOther(gem => (Residents.WorkplaceOf(gem) != armoury) && BasicErrands.IsWorker(gem) &&
				(Residents.RoleKeyOf(gem) != "captain") && (Residents.RoleKeyOf(gem) != "guard")); //someone who'd borrow a key, not the guard who'd arrest them
			if ((armourer == null) || (suspect == null)) {
				return false;
			}
			string thief = Residents.NameOf(suspect);
			string ask = BasicErrands.Sex(armourer, "zbrojire ", "zbrojirky ") + Residents.NameOf(armourer);
			e.offer = "Pri inventure mi chybi tri mece. Nekdo kradl. Zeptej se " + ask + ", kdo mel klic.";
			e.thanks = "Diky. Aspon vim, na cem jsem.";
			e.steps.Add(Errands.Talk(armourer, "Zeptej se " + ask + ", kdo mel klic od skladu", "Kdo mel vcera klic od skladu?",
				"Klic si vcera pujcil" + BasicErrands.Sex(suspect, "", "a") + " " + ErrandContext.Who(suspect) + "."));
			e.steps.Add(Errands.Choose(suspect, "Promluv si s: " + ErrandContext.Who(suspect),
				new[] { "Vrat ty mece a nikdo se nic nedozvi.", "Pujdes se mnou pred kapitana!" },
				new[] { "Dobre... vratim je. Diky, ze mlcis.", "Ne! Byla to jen chvilkova slabost..." }));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se k " + c.GiverName, "Vim, kdo to byl: " + thief + ".", "Postaram se o to."));
			return true;
		}

		private static bool TurnedIn(Errand e) {
			return (e.steps.Count > 1) && (e.steps[1].chosen == 1);
		}

		public override int PayPercent(Errand e) {
			return TurnedIn(e) ? 125 : 100;
		}

		public override void OnDone(Player player, Errand e) {
			if (!TurnedIn(e) && (e.steps.Count > 1)) {
				Residents.AddFriendship(player, e.steps[1].residentId, 2); //a thief let off quietly doesn't forget it
			}
		}
	}

	/// <summary>The smith's repaired iron long sword, taken to the city's barracks captain or a guard.</summary>
	internal sealed class RepairedBladeErrand : ErrandTemplate {
		public override string Key { get { return "blade"; } }
		public override string Name { get { return "Opraveny mec"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if ((Residents.RoleKeyOf(c.GiverGem) != "smith") || !(ThingDef.GetByDefname("i_sword_long_iron") is ItemDef)) {
				return false;
			}
			Item owner = c.PickOther(gem => (Residents.RoleKeyOf(gem) == "captain") || (Residents.RoleKeyOf(gem) == "guard"));
			if (owner == null) {
				return false;
			}
			e.offer = BasicErrands.Sex(c.GiverGem, "Opravil", "Opravila") + " jsem mec pro: " + ErrandContext.Who(owner) + ". Odneses " + BasicErrands.Sex(owner, "mu", "ji") + " ho?";
			e.thanks = "Diky. Ten mec ted vydrzi dlouho.";
			e.steps.Add(Errands.Deliver(owner, "i_sword_long_iron", "Odnes opraveny mec: " + ErrandContext.Who(owner), "Kovar posila tvuj opraveny mec.",
				"Konecne! Bez nej jsem si pripadal" + BasicErrands.Sex(owner, " nahy", "a naha") + "."));
			return true;
		}
	}

	/// <summary>A pilgrim asks the way: the right one of three venues. A wrong answer still ends it, for a quarter of the pay. The
	/// right answer's place is kept in the choose step's count, which a choose step doesn't otherwise use.</summary>
	internal sealed class PilgrimWayErrand : ErrandTemplate {
		public override string Key { get { return "pilgrim"; } }
		public override string Name { get { return "Poutnik"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		//places that would answer the ask as well: never offered as a wrong way
		private static readonly Dictionary<string, string[]> alsoRight = new Dictionary<string, string[]>(StringComparer.Ordinal) {
			{ "tavern", new[] { "bakery", "market" } }, { "market", new[] { "bakery", "tavern" } }, { "bakery", new[] { "market", "tavern" } },
			{ "barracks", new[] { "armoury" } }, { "armoury", new[] { "barracks" } }, { "smithy", new[] { "tailor" } }, { "stable", new[] { "farm" } },
		};

		internal static bool AlsoRight(string asked, string other) {
			return (asked == other) || (alsoRight.TryGetValue(asked, out string[] also) && (Array.IndexOf(also, other) >= 0));
		}

		private static readonly Dictionary<string, string> asks = new Dictionary<string, string>(StringComparer.Ordinal) {
			{ "tavern", "Kde se tu da najist a vyspat?" },
			{ "bathhouse", "Po dlouhe ceste potrebuji koupel. Kam mam jit?" },
			{ "healing", "Boli me nohy. Kde najdu lecitele?" },
			{ "smithy", "Utrhla se mi prezka u batohu. Kdo ji spravi?" },
			{ "armoury", "Prej tu nekde vydavaji zbrane strazi. Kde?" },
			{ "market", "Potrebuji jidlo na cestu. Kde nakoupim?" },
			{ "theatre", "Kde se tu hraje divadlo?" },
			{ "stable", "Kde si pujcim kone?" },
			{ "bakery", "Kde koupim cerstvy chleb?" },
			{ "library", "Hledam stare knihy o poutnich mistech." },
			{ "barracks", "Kde najdu kapitana straze?" },
		};

		public override bool TryBuild(ErrandContext c, Errand e) {
			CityVenue here = Residents.WorkplaceOf(c.GiverGem);
			if ((Residents.RoleKeyOf(c.GiverGem) != "pilgrim") || (here == null)) {
				return false;
			}
			List<CityVenue> askable = new List<CityVenue>(), others = new List<CityVenue>();
			foreach (CityVenue venue in VenueRegistry.InCity(here.City)) {
				if ((venue == here) || (venue.VenueType == null) || (venue.VenueType.Key == "house")) {
					continue;
				}
				others.Add(venue);
				if (asks.ContainsKey(venue.VenueType.Key)) {
					askable.Add(venue);
				}
			}
			if (askable.Count == 0) {
				return false;
			}
			CityVenue right = askable[Globals.dice.Next(askable.Count)];
			others.RemoveAll(v => (v == right) || AlsoRight(right.VenueType.Key, v.VenueType.Key));
			if (others.Count < 2) {
				return false;
			}
			List<string> choices = new List<string>();
			for (int i = 0; i < 2; i++) {
				int at = Globals.dice.Next(others.Count);
				choices.Add(others[at].Label);
				others.RemoveAt(at);
			}
			int rightAt = Globals.dice.Next(3);
			choices.Insert(rightAt, right.Label);
			string[] replies = new string[3];
			for (int i = 0; i < 3; i++) {
				replies[i] = (i == rightAt) ? "Diky, tam pujdu." : "Tam? To asi nebude ono... ale diky.";
			}
			e.offer = asks[right.VenueType.Key];
			e.thanks = "";
			ErrandStep ask = Errands.Choose(c.GiverGem, "Ukaz cestu: " + c.GiverName, choices.ToArray(), replies);
			ask.count = rightAt;
			e.steps.Add(ask);
			return true;
		}

		public override int PayPercent(Errand e) {
			ErrandStep ask = e.steps[0];
			return (ask.chosen == ask.count) ? 100 : 25;
		}
	}

	/// <summary>The healer's fevered patient: sit with them, come back to them at night, then tell the healer.</summary>
	internal sealed class FeverErrand : ErrandTemplate {
		public override string Key { get { return "fever"; } }
		public override string Name { get { return "Horecka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (Residents.RoleKeyOf(c.GiverGem) != "healer") {
				return false;
			}
			Item patient = WorkErrands.SameVenue(c.GiverGem, "patient");
			if (patient == null) {
				return false;
			}
			string name = Residents.NameOf(patient), him = BasicErrands.Sex(patient, "nemu", "ni");
			e.offer = name + " ma horecku a ja mam dalsi pacienty. Posedis u " + BasicErrands.Sex(patient, "nej", "ni") + " do noci?";
			e.thanks = "Diky. Bez tebe bych to nezvlad" + BasicErrands.Sex(c.GiverGem, "l", "la") + ".";
			e.steps.Add(Errands.Talk(patient, "Sedni si k: " + name, "Jsem tady. Vydrz.", "Dekuju... je mi zima."));
			e.steps.Add(Errands.Wait(patient, 22, 6, "Prijd k " + him + " zase v noci (22 az 6)", "Noc je tu. Jak ti je?", "Horecka povolila. Dekuju za spolecnost."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Rekni " + c.GiverName + ", ze horecka povolila", "Horecka povolila.", "To je dobra zprava."));
			return true;
		}
	}
}
