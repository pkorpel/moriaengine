using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - the woodland realm's stories (design 9.3): the feast whose lights vanish, the spiders' nests, the barrel run down
	/// the river (timed), and wine from the east for the king (a trip to Rhun across the realms). Only the woodland realm's people
	/// give them, in Thranduil's own town (Lorien is the same city on the map, but none of these are its stories). Texts Czech without diacritics.</summary>
	public static class WoodlandStories {
		public const string Woodland = "Kralovstvi_lesnich_elfu", Rhun = "Rhun", LorienArea = "a_Lorien";
		public const string LightDef = "i_errand_elf_light", SpiderDef = "c_errand_mirkwood_spider", BarrelDef = "i_errand_river_barrel", CaskDef = "i_errand_east_cask";

		public static void Bootstrap() {
			ErrandTemplates.Register(new VanishingFeastErrand());
			ErrandTemplates.Register(new SpiderNestsErrand());
			ErrandTemplates.Register(new BarrelRunErrand());
			ErrandTemplates.Register(new EastWineErrand());
		}

		/// <summary>A worker of Thranduil's own town: Lorien shares the city on the map, but has no king, no river and no lake town.</summary>
		internal static bool IsWoodlandWorker(ErrandContext c, params string[] venues) {
			FlaggedRegion town = Errands.TownOf(c.GiverGem, c.City);
			return CityFlavour.Is(c.City, Woodland) && BasicErrands.IsWorker(c.GiverGem) && !((town != null) && string.Equals(town.Defname, LorienArea, StringComparison.OrdinalIgnoreCase))
				&& ((venues.Length == 0) || (Array.IndexOf(venues, BasicErrands.VenueKey(c.GiverGem)) >= 0));
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class WoodlandStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(WoodlandStories.LightDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(WoodlandStories.BarrelDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(WoodlandStories.CaskDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>The feast whose lights vanish (a tavern or library worker): follow the three lights of the elves' feast on the road out,
	/// each going out as you reach it, and tell the giver what you saw.</summary>
	internal sealed class VanishingFeastErrand : ErrandTemplate {
		internal const int Lights = 3;

		public override string Key { get { return "elffeast"; } }
		public override string Name { get { return "Mizici hostina"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!WoodlandStories.IsWoodlandWorker(c, "tavern", "library")) {
				return false;
			}
			ErrandStep lights = Errands.Things(WoodlandStories.LightDef, Lights, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Jdi za " + Lights + " svetly hostiny v lese", "Posledni svetlo zhaslo. Zbyl jen smich a vune vina.");
			if (!Errands.CanPlace(e, lights)) {
				return false;
			}
			e.offer = "Za mestem v lese hoduji nasi a nikdo nesmi rusit. Ale kral chce vedet, kdo vsechno prisel. Jdi za svetly. Jen se nediv, kdyz zmizi.";
			e.thanks = "Tak tak. Kral bude spokojen.";
			e.steps.Add(lights);
			e.steps.Add(Errands.Talk(c.GiverGem, "Povez " + c.GiverName + ", co jsi videl", "Svetla mizela, sotva jsem k nim dosel.", "Tak se slavi u nas. Kdo prijde neohlasen, uvidi jen tmu."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Svetlo zhasne. Kdesi v lese se nekdo zasmeje.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>The spiders' nests (any worker): lay three of the great spiders on the road out, then report.</summary>
	internal sealed class SpiderNestsErrand : ErrandTemplate {
		internal const int Spiders = 3;

		public override string Key { get { return "spidernests"; } }
		public override string Name { get { return "Pavouci hnizda"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!WoodlandStories.IsWoodlandWorker(c)) {
				return false;
			}
			ErrandStep spiders = Errands.Things(WoodlandStories.SpiderDef, Spiders, ErrandThingAction.Kill, ErrandThingPlace.Outside, null, false,
				"Zabij " + Spiders + " obri pavouky za mestem", "Pavouci jsou mrtvi a jejich site visi prazdne.");
			if (!Errands.CanPlace(e, spiders)) {
				return false;
			}
			e.offer = "Pavouci se zase prisunuli az k ceste. Vcera chytili dva nase lovce do siti. Vycisti cestu, nez se vrati jejich mladata.";
			e.thanks = "Cesta je volna. Kral ti dekuje.";
			e.steps.Add(spiders);
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas " + c.GiverName + ", ze cesta je volna", "Pavouci jsou pryc.", "Dobre. Ale vrati se. Vzdycky se vrati."));
			return true;
		}
	}

	/// <summary>The barrel run (a harbour, tavern or brewery worker): the empty barrels float down the river to the lake town; push off
	/// the three stuck on the road out within 10 minutes, and report.</summary>
	internal sealed class BarrelRunErrand : ErrandTemplate {
		internal const int Barrels = 3;

		public override string Key { get { return "barrelrun"; } }
		public override string Name { get { return "Beh v sudech"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }
		public override int Minutes { get { return 10; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!WoodlandStories.IsWoodlandWorker(c, "harbour", "tavern", "brewery")) {
				return false;
			}
			ErrandStep barrels = Errands.Things(WoodlandStories.BarrelDef, Barrels, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Dobehni k " + Barrels + " sudum na rece za mestem", "Vsechny sudy plavou. Jako tenkrat trpaslici!");
			if (!Errands.CanPlace(e, barrels)) {
				return false;
			}
			e.offer = "Prazdne sudy plavou po rece do jezerniho mesta. Ale zasekly se u brehu. Postrc je, a rychle, nez je reka vezme jinam. Mas deset minut.";
			e.thanks = "Sudy jsou na ceste. Rychly jako trpaslik v sudu!";
			e.steps.Add(barrels);
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas " + c.GiverName + ", ze sudy plavou", "Sudy jsou na rece.", "Vyborne. Jezerni mesto dostane sve sudy vcas."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Sud sklouzne do vody a reka si ho odnasi.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>Wine from the east for the king (a tavern worker): fetch the cask of eastern wine waiting in Rhun's streets, across the
	/// realms, within 90 minutes, and bring word to the giver.</summary>
	internal sealed class EastWineErrand : ErrandTemplate {
		public override string Key { get { return "eastwine"; } }
		public override string Name { get { return "Vino z vychodu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Large; } }
		public override int Minutes { get { return 90; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!WoodlandStories.IsWoodlandWorker(c, "tavern") || (WorldEvents.CentreOf(WoodlandStories.Rhun) == null)) {
				return false;
			}
			ErrandStep cask = Errands.Things(WoodlandStories.CaskDef, 1, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Vyzvedni sud vina v Rhunu a dej ho na cestu ke krali", "Sud je na voze. Do lesa dorazi s kupci.", WoodlandStories.Rhun);
			if (!Errands.CanPlace(e, cask)) {
				return false;
			}
			e.offer = "Kral miluje vino z vychodu, a to se dovazi az z Rhunu. Kupci tam na nekoho cekaji se sudem. Cesta je dlouha a nebezpecna, ale kral plati dobre.";
			e.thanks = "Kral bude mit sve vino. Ten sud jsi vyprostil z Rhunu jako hrdina.";
			e.steps.Add(cask);
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Sud z Rhunu je na ceste.", "Kral uz se tesi. A ty si zaslouzis pohar z prvniho sudu."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Kupci nalozi sud na vuz a vyrazi k lesu.");
			thing.Delete();
			return true;
		}
	}
}
