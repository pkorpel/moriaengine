using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - the stories of Dol Amroth, Pelargir and Linhir (design 9.3; all one city on the map): the swan knights'
	/// tourney, corsair spies on the docks, and the lighthouse whose keeper fell ill. Only Dol Amroth's people give them. Texts
	/// Czech without diacritics.</summary>
	public static class DolAmrothStories {
		public const string DolAmroth = "Dol_Amroth";
		public const string QuintainDef = "i_errand_quintain", SpyDef = "c_errand_corsair_spy", LetterDef = "i_errand_corsair_letter", LightDef = "i_errand_lighthouse_fire";

		public static void Bootstrap() {
			ErrandTemplates.Register(new SwanTourneyErrand());
			ErrandTemplates.Register(new CorsairSpiesErrand());
			ErrandTemplates.Register(new LighthouseErrand());
		}

		internal static bool WorksAt(Item gem, params string[] venues) {
			return BasicErrands.IsWorker(gem) && (Array.IndexOf(venues, BasicErrands.VenueKey(gem)) >= 0);
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class DolAmrothStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(DolAmrothStories.QuintainDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(DolAmrothStories.SpyDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(DolAmrothStories.LightDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>The swan knights' tourney (a barracks or armoury worker in Dol Amroth): ride at the three quintains in the streets,
	/// mounted and armed (on foot or bare-handed a run doesn't count), then report to the giver.</summary>
	internal sealed class SwanTourneyErrand : ErrandTemplate {
		internal const int Quintains = 3;

		public override string Key { get { return "swantourney"; } }
		public override string Name { get { return "Turnaj labutich rytiru"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, DolAmrothStories.DolAmroth) || !DolAmrothStories.WorksAt(c.GiverGem, "barracks", "armoury")) {
				return false;
			}
			ErrandStep quintains = Errands.Things(DolAmrothStories.QuintainDef, Quintains, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Najed na koni se zbrani na " + Quintains + " terce v ulicich", "Vsechny terce padly. Labuti rytiri tleskaji.");
			if (!Errands.CanPlace(e, quintains)) {
				return false;
			}
			e.offer = "Labuti rytiri porada turnaj a hledaji jezdce do klani. Ukaz, co umis: na koni a se zbrani srazis tri terce v ulicich.";
			e.thanks = "Knize by byl hrdy. Dobra jizda.";
			e.steps.Add(quintains);
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas se u " + c.GiverName + " po klani", "Terce lezi.", "Videl jsem. Takovou ruku by labuti rytiri brali."));
			return true;
		}

		/// <summary>A run counts only from the saddle, with a weapon in hand.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (!player.Flag_Riding || !((player.FindLayer(LayerNames.Hand1) is Weapon) || (player.FindLayer(LayerNames.Hand2) is Weapon))) { //the hands, not the cached combat values
				player.SysMessage("Na terc se najizdi na koni a se zbrani v ruce.");
				return false;
			}
			player.SysMessage("Uder! Terc se zatoci a padne.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>Corsair spies on the docks (a harbour or barracks worker in Dol Amroth): catch the two spies in the streets, who run,
	/// and bring the giver the letter for Umbar one of them carried.</summary>
	internal sealed class CorsairSpiesErrand : ErrandTemplate {
		internal const int Spies = 2;

		public override string Key { get { return "corsairspies"; } }
		public override string Name { get { return "Spehove korzaru"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, DolAmrothStories.DolAmroth) || !DolAmrothStories.WorksAt(c.GiverGem, "harbour", "barracks")) {
				return false;
			}
			ErrandStep spies = Errands.Things(DolAmrothStories.SpyDef, Spies, ErrandThingAction.Catch, ErrandThingPlace.Streets, null, true,
				"Chyt " + Spies + " spehy korzaru v ulicich", "Mas je. Jeden mel u sebe dopis pro Umbar.");
			if (!Errands.CanPlace(e, spies)) {
				return false;
			}
			e.offer = "V pristavu se motaji cizinci a ptaji se, kolik lodi kotvi a kdy vyplouvaji. Spehove korzaru! Chyt je, nez utecou na more.";
			e.thanks = "Umbar se tentokrat nic nedozvi.";
			e.steps.Add(spies);
			e.steps.Add(Errands.Deliver(c.GiverGem, DolAmrothStories.LetterDef, "Dones " + c.GiverName + " dopis spehu", "Tohle mel jeden z nich u sebe.",
				"Seznam nasich lodi... a kdy vyplouvaji. To uz jim neprojde."));
			return true;
		}

		/// <summary>A spy caught is led off to the harbour watch.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Spehoun se vzdava. Straz si ho odvede.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>The lighthouse keeper fell ill (a harbour worker in Dol Amroth): come in the evening, light the lighthouse fire on the
	/// road out along the shore, and bring the giver ginseng for the keeper.</summary>
	internal sealed class LighthouseErrand : ErrandTemplate {
		internal const int FromHour = 18, ToHour = 23, Ginseng = 5;

		public override string Key { get { return "lighthouse"; } }
		public override string Name { get { return "Majak bez strazce"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Minutes { get { return 240; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, DolAmrothStories.DolAmroth) || !DolAmrothStories.WorksAt(c.GiverGem, "harbour")) {
				return false;
			}
			ErrandStep light = Errands.Things(DolAmrothStories.LightDef, 1, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Zapal svetlo majaku za mestem", "Svetlo majaku plane. Lode dnes v noci trefi domu.");
			if (!Errands.CanPlace(e, light)) {
				return false;
			}
			e.offer = "Strazce majaku lezi v horecce a dnes vecer nema kdo rozsvitit. Bez svetla se lode rozbiji o utesy. Prijd vecer, zapal ho, a prines mu zensen.";
			e.thanks = "Majak svitil celou noc. Strazce ti posila diky.";
			e.steps.Add(Errands.Wait(c.GiverGem, FromHour, ToHour, "Prijd vecer za " + c.GiverName + " (" + FromHour + ".-" + ToHour + ". hodina)",
				"Je vecer, jdu k majaku.", "Spechej, slunce uz zapada."));
			e.steps.Add(light);
			e.steps.Add(Errands.Fetch(c.GiverGem, "i_reag_ginseng", Ginseng, "Prines " + c.GiverName + " " + Ginseng + " korenu zensenu pro strazce",
				"Tady je zensen.", "Uvarim mu z nej caj. Do rana mu bude lip."));
			return true;
		}
	}
}
