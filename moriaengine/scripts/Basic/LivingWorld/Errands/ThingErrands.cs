using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1293 - errand templates with things in the world, on the Things step (design 9.1: 4 the runaway, 13 the lamplighter,
	/// 14 the cellar, 16 stop thief, 28 the missing shipment, 29 press gang). Texts Czech without diacritics. Press gang is the war
	/// camp's (an order there); the rest aren't the camp's.</summary>
	public static class ThingErrands {
		public const string CatDef = "c_errand_cat", RatDef = "c_errand_rat", ThiefDef = "c_errand_thief", DrunkDef = "c_errand_drunk_orc";
		public const string LampDef = "i_errand_lamp", CartDef = "i_errand_cart", GoodsDef = "i_cloth";
		public const int LitLamp = 0x0A12; //a lit torch
		internal static readonly TagKey MovedTK = TagKey.Acquire("_errand_moved_"), LitTK = TagKey.Acquire("_errand_lit_");

		public static void Bootstrap() {
			ErrandTemplates.Register(new RunawayErrand());
			ErrandTemplates.Register(new LamplighterErrand());
			ErrandTemplates.Register(new CellarErrand());
			ErrandTemplates.Register(new ThiefErrand());
			ErrandTemplates.Register(new ShipmentErrand());
			ErrandTemplates.Register(new PressGangErrand());
		}

		internal static bool IsCamp(ErrandContext c) {
			return WarCamp.IsCamp(c.City);
		}

		/// <summary>A venue of the city other than these, at random, or null.</summary>
		internal static CityVenue OtherVenue(string city, params CityVenue[] not) {
			List<CityVenue> venues = VenueRegistry.InCity(city);
			venues.RemoveAll(v => Array.IndexOf(not, v) >= 0);
			return (venues.Count == 0) ? null : venues[Globals.dice.Next(venues.Count)];
		}
	}

	/// <summary>Attaches the things' double clicks to their defs.</summary>
	public static class ThingErrandsInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(ThingErrands.LampDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(ThingErrands.CartDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(ThingErrands.CatDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(ThingErrands.ThiefDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(ThingErrands.DrunkDef, SingletonScript<E_ErrandThingNpc>.Instance);
		}
	}

	/// <summary>4. The runaway: the giver's cat hides in another venue and slips away once when found, to yet another.</summary>
	internal sealed class RunawayErrand : ErrandTemplate {
		public override string Key { get { return "runaway"; } }
		public override string Name { get { return "Uteklo"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (ThingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			CityVenue hideout = ThingErrands.OtherVenue(c.City, Residents.WorkplaceOf(c.GiverGem));
			if (hideout == null) {
				return false;
			}
			e.offer = "Moje kocka zase utekla! Naposledy ji videli v: " + hideout.Label + ". Prines mi ji, prosim.";
			e.thanks = "Ty jedna tulacko! Dekuji, ze jsi ji nasel.";
			e.steps.Add(Errands.Things(ThingErrands.CatDef, 1, ErrandThingAction.Catch, ErrandThingPlace.Venue, hideout, true,
				"Chyt kocku " + c.GiverName + " (" + hideout.Label + ")", "Mas ji. Prede ti v naruci."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat kocku " + c.GiverName, "Tady je tvoje kocka.", "Ach, ty moje mazlicku!"));
			return true;
		}

		/// <summary>The first catch, she slips through the fingers to another venue; the second holds.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if ((thing.GetTag(ThingErrands.MovedTK) == null) && (thing is NPC cat)) {
				CityVenue here = VenueRegistry.At(cat);
				CityVenue next = ThingErrands.OtherVenue(errand.city, here, VenueRegistry.ById(errand.steps[0].venueId));
				int[] spot = (next == null) ? null : ResidentSchedule.SpotIn(next, "runaway/" + errand.id);
				if (spot != null) {
					cat.SetTag(ThingErrands.MovedTK, 1);
					cat.Go(new Point4D((ushort) spot[0], (ushort) spot[1], (sbyte) spot[2], next.M));
					NPCAiTimer ai = NPCAiTimer.GetFor(cat);
					if (ai != null) {
						ai.hasHome = false; //her home is where she hides now, or she'd walk back to the first venue
					}
					player.SysMessage("Kocka ti proklouzla mezi prsty! Zahledl jsi ji, jak utika k: " + next.Label + ".");
					return false;
				}
			}
			thing.Delete();
			return true;
		}
	}

	/// <summary>13. The lamplighter: light the lamps in the streets before the dark (a short deadline).</summary>
	internal sealed class LamplighterErrand : ErrandTemplate {
		internal const int Lamps = 4;

		public override string Key { get { return "lamplighter"; } }
		public override string Name { get { return "Lampar"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }
		public override int Minutes { get { return 15; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (ThingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			e.offer = "Lampar onemocnel a brzy bude tma. Rozsvitis lampy v ulicich kolem namesti? Jsou " + Lamps + ".";
			e.thanks = "Mesto sviti. Dekuji.";
			e.steps.Add(Errands.Things(ThingErrands.LampDef, Lamps, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Rozsvit " + Lamps + " lampy v ulicich kolem namesti", "Vsechny lampy sviti."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Lampy sviti.", "Vidim. Pekne."));
			return true;
		}

		/// <summary>A lamp lit stays lit (it goes with the errand); a lit one again counts nothing.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (thing.GetTag(ThingErrands.LitTK) != null) {
				player.SysMessage("Tahle lampa uz sviti.");
				return false;
			}
			thing.SetTag(ThingErrands.LitTK, 1);
			if (thing is Item lamp) {
				lamp.Model = ThingErrands.LitLamp;
			}
			return true;
		}
	}

	/// <summary>14. The cellar: rats in the giver's venue; kill them.</summary>
	internal sealed class CellarErrand : ErrandTemplate {
		internal const int Rats = 3;
		private static readonly string[] venues = { "tavern", "brewery", "bakery", "market", "house" };

		public override string Key { get { return "cellar"; } }
		public override string Name { get { return "Krysy ve sklepe"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			CityVenue work = Residents.WorkplaceOf(c.GiverGem);
			if (ThingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem) || (work == null) || (Array.IndexOf(venues, BasicErrands.VenueKey(c.GiverGem)) < 0)) {
				return false;
			}
			e.offer = "Ve sklepe mam krysy. Zerou mi zasoby. Vyhubis je?";
			e.thanks = "Konecne klid. Dekuji.";
			e.steps.Add(Errands.Things(ThingErrands.RatDef, Rats, ErrandThingAction.Kill, ErrandThingPlace.Venue, work, false,
				"Vyhub krysy: " + work.Label, "Posledni krysa je pryc."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Krysy jsou pryc.", "Vyborne!"));
			return true;
		}
	}

	/// <summary>16. Stop, thief: a pickpocket robs the giver and runs; he tires after a few runs; catch him and bring the purse back.</summary>
	internal sealed class ThiefErrand : ErrandTemplate {
		public override string Key { get { return "thief"; } }
		public override string Name { get { return "Zlodej"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (ThingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			e.offer = "Zlodej! Ukradl mi mesec a utika k namesti! Chyt ho!";
			e.thanks = "Muj mesec! Ty jsi hrdina.";
			e.steps.Add(Errands.Things(ThingErrands.ThiefDef, 1, ErrandThingAction.Catch, ErrandThingPlace.Streets, null, true,
				"Chyt zlodeje, ktery okradl " + c.GiverName, "Mas ho! Mesec je zpatky."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat mesec " + c.GiverName, "Tady je tvuj mesec.", "Dekuji! Uz jsem s nim nepocital" + BasicErrands.Sex(c.GiverGem, "", "a") + "."));
			return true;
		}
	}

	/// <summary>28. The missing shipment: goods didn't arrive; find the overturned cart on the road out of the city and bring back
	/// what's left.</summary>
	internal sealed class ShipmentErrand : ErrandTemplate {
		private static readonly string[] venues = { "market", "caravanserai", "harbour", "tailor" };

		public override string Key { get { return "shipment"; } }
		public override string Name { get { return "Ztracena zasilka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (ThingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem) || (Array.IndexOf(venues, BasicErrands.VenueKey(c.GiverGem)) < 0)) {
				return false;
			}
			e.offer = "Moje zbozi nedorazilo. Vozka mel prijet uz vcera. Podivej se na cestu za mestem, prosim.";
			e.thanks = "Aspon neco se zachranilo. Dekuji.";
			e.steps.Add(Errands.Things(ThingErrands.CartDef, 1, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Najdi prevraceny vuz na ceste za mestem", "Mezi troskami jsi nasel balik latky."));
			e.steps.Add(Errands.Deliver(c.GiverGem, ThingErrands.GoodsDef, "Dones zbozi " + c.GiverName, "Tohle zbylo z vozu.", "Moje latka! Dekuji."));
			return true;
		}
	}

	/// <summary>29. Press gang (Mordor's war camp): drag the drunk orcs from the mess hall to the barracks before the captain sees.</summary>
	internal sealed class PressGangErrand : ErrandTemplate {
		internal const int Drunks = 3;

		public override string Key { get { return "pressgang"; } }
		public override string Name { get { return "Verbovani"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			if (!ThingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem) || ((venue != "messhall") && (venue != "barracks"))) {
				return false;
			}
			CityVenue mess = (venue == "messhall") ? Residents.WorkplaceOf(c.GiverGem) : VenueRegistry.InCity(c.City).Find(v => (v.VenueType != null) && (v.VenueType.Key == "messhall"));
			if (mess == null) {
				return false;
			}
			e.offer = "V jidelne se zase valeji opilci. Odtahni je do kasaren, nez si toho vsimne kapitan.";
			e.thanks = "Dobre. Kapitan nic nevi.";
			e.steps.Add(Errands.Things(ThingErrands.DrunkDef, Drunks, ErrandThingAction.Catch, ErrandThingPlace.Venue, mess, false,
				"Odtahni " + Drunks + " opilce z jidelny do kasaren", "Vsichni opilci jsou v kasarnach."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas se u " + c.GiverName, "Opilci jsou v kasarnach.", "Dobre."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Odtahl jsi opilce do kasaren.");
			thing.Delete();
			return true;
		}
	}
}
