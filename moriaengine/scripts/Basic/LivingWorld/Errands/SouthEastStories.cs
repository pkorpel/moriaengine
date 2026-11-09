using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - the stories of Umbar, Harad and Rhun (design 9.3; Harad's caravan and Umbar's contraband are #1282's): preparing
	/// a corsair raid, a mumak handler's runaway calf, the spice caravan robbed in the desert, and horse trading with the Easterlings.
	/// Only those cities' people give them. Texts Czech without diacritics.</summary>
	public static class SouthEastStories {
		public const string Umbar = "Umbar", Harad = "Harad", Rhun = "Rhun";
		public const string CalfDef = "c_errand_mumak_calf", DesertBanditDef = "c_errand_desert_bandit", SpiceDef = "i_errand_spice_crate", WildHorseDef = "c_errand_wild_horse";

		public static void Bootstrap() {
			ErrandTemplates.Register(new CorsairRaidErrand());
			ErrandTemplates.Register(new MumakCalfErrand());
			ErrandTemplates.Register(new SpiceCaravanErrand());
			ErrandTemplates.Register(new HorseTradeErrand());
		}

		/// <summary>A crewman for the raid: from the giver's own town (Umbar's city holds the oases too), the corsairs' haunts first,
		/// then any worker.</summary>
		internal static Item Crew(ErrandContext c, Item not) {
			FlaggedRegion town = Errands.TownOf(c.GiverGem, c.City);
			Predicate<Item> sameTown = gem => (gem != not) && BasicErrands.IsWorker(gem) && (Errands.TownOf(gem, c.City) == town);
			return c.PickOther(gem => sameTown(gem) && (Array.IndexOf(new[] { "corsairden", "harbour", "tavern" }, BasicErrands.VenueKey(gem)) >= 0)) ?? c.PickOther(sameTown);
		}

		internal static bool WorksIn(ErrandContext c, string city, params string[] venues) {
			return CityFlavour.Is(c.City, city) && BasicErrands.IsWorker(c.GiverGem)
				&& ((venues.Length == 0) || (Array.IndexOf(venues, BasicErrands.VenueKey(c.GiverGem)) >= 0));
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class SouthEastStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(SouthEastStories.CalfDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(SouthEastStories.SpiceDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(SouthEastStories.WildHorseDef, SingletonScript<E_ErrandThingNpc>.Instance);
		}
	}

	/// <summary>Preparing a corsair raid (a corsair den or harbour worker in Umbar): call two of the crew to the ship, bring the captain
	/// arrows for the bows, and report.</summary>
	internal sealed class CorsairRaidErrand : ErrandTemplate {
		internal const int Arrows = 30;

		public override string Key { get { return "corsairraid"; } }
		public override string Name { get { return "Priprava najezdu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!SouthEastStories.WorksIn(c, SouthEastStories.Umbar, "corsairden", "harbour")) {
				return false;
			}
			Item first = SouthEastStories.Crew(c, null);
			Item second = (first == null) ? null : SouthEastStories.Crew(c, first);
			if (second == null) {
				return false;
			}
			e.offer = "Za tri noci vyplouvame na pobrezi Gondoru. Sezen mi posadku a sipy, at mame cim pozdravit jejich straze.";
			e.thanks = "Lod je pripravena. Gondor se probudi do ohne.";
			foreach (Item crew in new[] { first, second }) {
				e.steps.Add(Errands.Talk(crew, "Svolej na lod: " + ErrandContext.Who(crew), "Kapitan te chce na lodi. Za tri noci se pluje.",
					ErrandContext.Pick("Konecne! Uz mi dochazi stribro.", "Budu tam. A muj nuz taky.", "Na Gondor? To si nenecham ujit.")));
			}
			e.steps.Add(Errands.Fetch(c.GiverGem, "i_arrow", Arrows, "Prines " + c.GiverName + " " + Arrows + " sipu", "Tady jsou sipy.", "Ostre. Gondorsti to pociti."));
			return true;
		}
	}

	/// <summary>A mumak handler's runaway calf (a caravanserai or stable worker in Harad): catch the young mumak on the road out (it runs)
	/// and tell the giver.</summary>
	internal sealed class MumakCalfErrand : ErrandTemplate {
		public override string Key { get { return "mumakcalf"; } }
		public override string Name { get { return "Utekle mumaci mlade"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!SouthEastStories.WorksIn(c, SouthEastStories.Harad, "caravanserai", "stable", "kennels")) { //kennels: Harad's mumak handlers
				return false;
			}
			ErrandStep calf = Errands.Things(SouthEastStories.CalfDef, 1, ErrandThingAction.Catch, ErrandThingPlace.Outside, null, true,
				"Chyt utekle mumaci mlade za mestem", "Mlade se dalo chytit. Uz klusa k mamince.");
			if (!Errands.CanPlace(e, calf)) {
				return false;
			}
			e.offer = "Mlade mumaka se splasilo a utika do pouste. Je mensi nez dum, ale vetsi nez ty. Chyt ho, nez ho najdou lvi.";
			e.thanks = "Mlade je u matky. Dekuji, cizince.";
			e.steps.Add(calf);
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas " + c.GiverName + ", ze mlade je zpet", "Mlade je zpatky.", "Dobre. Uz ho nikdy nepustim z oci."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Mlade zatroubi a necha se odvest.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>The spice caravan (a caravanserai or market worker in Harad): bandits robbed it in the desert; lay the two on the road
	/// out, pick up the two spice crates they dropped, and report.</summary>
	internal sealed class SpiceCaravanErrand : ErrandTemplate {
		internal const int Bandits = 2, Crates = 2;

		public override string Key { get { return "spicecaravan"; } }
		public override string Name { get { return "Karavana s koreninami"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!SouthEastStories.WorksIn(c, SouthEastStories.Harad, "caravanserai", "market")) {
				return false;
			}
			ErrandStep bandits = Errands.Things(SouthEastStories.DesertBanditDef, Bandits, ErrandThingAction.Kill, ErrandThingPlace.Outside, null, false,
				"Zabij " + Bandits + " poustni lupice za mestem", "Lupici jsou mrtvi. Bedny s koreninami lezi u cesty.");
			ErrandStep crates = Errands.Things(SouthEastStories.SpiceDef, Crates, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Posbirej " + Crates + " bedny koreni u cesty", "Mas obe bedny. Voni az sem.");
			if (!Errands.CanPlace(e, bandits)) {
				return false;
			}
			e.offer = "Lupici prepadli nasi karavanu v pousti a ukradli koreni. Dve bedny! Ziskej je zpet a odmena je tvoje.";
			e.thanks = "Korenim voni zase cely trh. Dobra prace.";
			e.steps.Add(bandits);
			e.steps.Add(crates);
			e.steps.Add(Errands.Talk(c.GiverGem, "Prines koreni " + c.GiverName, "Tady jsou bedny.", "Safran, skorice, pepr... vsechno tu je."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Bedna je tvoje. Voni po skorici.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>Horse trading with the Easterlings (a stable or kennels worker in Rhun): catch two of the wild horses on the steppe on
	/// the road out (they run), and the trader buys them.</summary>
	internal sealed class HorseTradeErrand : ErrandTemplate {
		internal const int Horses = 2;

		public override string Key { get { return "horsetrade"; } }
		public override string Name { get { return "Obchod s konmi"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!SouthEastStories.WorksIn(c, SouthEastStories.Rhun, "stable", "kennels")) {
				return false;
			}
			ErrandStep horses = Errands.Things(SouthEastStories.WildHorseDef, Horses, ErrandThingAction.Catch, ErrandThingPlace.Outside, null, true,
				"Chyt " + Horses + " divoke kone na stepi za mestem", "Mas oba. Nejsou krotci, ale jsou rychli.");
			if (!Errands.CanPlace(e, horses)) {
				return false;
			}
			e.offer = "Na stepi se pasou divoci kone. Chyt mi dva a koupim je. Vychodnane plati dobre, kdyz kun stoji za to.";
			e.thanks = "Dobri kone. Obchod je obchod.";
			e.steps.Add(horses);
			e.steps.Add(Errands.Talk(c.GiverGem, "Prodej kone " + c.GiverName, "Mam pro tebe dva kone.", "Ukaz zuby... Dobre. Beru je."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Kun se zazmita a necha se odvest k obchodnikovi.");
			thing.Delete();
			return true;
		}
	}
}
