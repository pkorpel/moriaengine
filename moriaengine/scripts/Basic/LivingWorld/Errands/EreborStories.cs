using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - the stories of Erebor and of Dale with Esgaroth (design 9.3): the ravens' messenger service, an ancestral axe
	/// and an old grudge, a deep mining expedition (Mining), the con man selling "the Arkenstone"; the fish market's quota and the
	/// smuggling bargeman. Only Erebor's, or Dale's, people give them. Texts Czech without diacritics.</summary>
	public static class EreborStories {
		public const string Erebor = "Erebor", Dale = "Dol";
		public const string PerchDef = "i_errand_raven_perch", AxeOrcDef = "c_errand_axe_orc", AxeDef = "i_errand_ancestral_axe", VeinDef = "i_errand_deep_vein",
			ConManDef = "c_errand_con_man", FakeStoneDef = "i_errand_fake_arkenstone", BargemanDef = "c_errand_bargeman", ContrabandDef = "i_errand_contraband";

		public static void Bootstrap() {
			ErrandTemplates.Register(new RavenPostErrand());
			ErrandTemplates.Register(new AncestralAxeErrand());
			ErrandTemplates.Register(new DeepMiningErrand());
			ErrandTemplates.Register(new FalseArkenstoneErrand());
			ErrandTemplates.Register(new FishQuotaErrand());
			ErrandTemplates.Register(new BargemanErrand());
		}

		internal static bool WorksIn(ErrandContext c, string city, params string[] venues) {
			return CityFlavour.Is(c.City, city) && BasicErrands.IsWorker(c.GiverGem)
				&& ((venues.Length == 0) || (Array.IndexOf(venues, BasicErrands.VenueKey(c.GiverGem)) >= 0));
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class EreborStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(EreborStories.PerchDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(EreborStories.AxeDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(EreborStories.VeinDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(EreborStories.ConManDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(EreborStories.BargemanDef, SingletonScript<E_ErrandThingNpc>.Instance);
		}
	}

	/// <summary>The ravens of Erebor (any worker in Erebor): the message the raven brought must go on to Dale; take it to the ravens'
	/// perch in Dale's streets within 60 minutes, and tell the giver.</summary>
	internal sealed class RavenPostErrand : ErrandTemplate {
		public override string Key { get { return "ravenpost"; } }
		public override string Name { get { return "Havrani posta"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Minutes { get { return 60; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!EreborStories.WorksIn(c, EreborStories.Erebor)) {
				return false;
			}
			ErrandStep perch = Errands.Things(EreborStories.PerchDef, 1, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Dones vzkaz na havrani bidlo v Dolu", "Havran si vzkaz vzal a uz leti.", EreborStories.Dale);
			if (!Errands.CanPlace(e, perch)) {
				return false;
			}
			e.offer = "Stary havran Roac prinesl zpravu, ale je unaveny a dal neuleti. Zprava musi do Dolu. Dones ji na havrani bidlo, tamni havrani ji roznesou.";
			e.thanks = "Havrani to roznesli. Dekujeme, posle.";
			e.steps.Add(perch);
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Zprava je v Dolu.", "Dobre. Roac uz si odpocinul a pozdravuje te."));
			return true;
		}
	}

	/// <summary>An ancestral axe and an old grudge (a smithy or market worker in Erebor): an orc carries the axe of the giver's
	/// house; lay him on the road out, take the axe and bring it home.</summary>
	internal sealed class AncestralAxeErrand : ErrandTemplate {
		public override string Key { get { return "ancestralaxe"; } }
		public override string Name { get { return "Sekera predku"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!EreborStories.WorksIn(c, EreborStories.Erebor, "smithy", "market")) {
				return false;
			}
			ErrandStep orc = Errands.Things(EreborStories.AxeOrcDef, 1, ErrandThingAction.Kill, ErrandThingPlace.Outside, null, false,
				"Zabij skreta se sekerou predku za mestem", "Skret je mrtvy. Sekeru mel schovanou nekde u cesty.");
			ErrandStep axe = Errands.Things(EreborStories.AxeDef, 1, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Najdi sekeru predku u cesty", "Sekera je v tvych rukou. Je tezka jako stara krivda.");
			if (!Errands.CanPlace(e, orc)) {
				return false;
			}
			e.offer = "Muj praded padl v Moriji a jeho sekeru si vzal skret. Ted se ten skret, nebo jeho vnuk, potuluje u nasi cesty a sekeru nosi jako trofej. Vrat ji domu.";
			e.thanks = "Sekera je zpatky v rodu. Stara krivda je smyta.";
			e.steps.Add(orc);
			e.steps.Add(axe);
			e.steps.Add(Errands.Talk(c.GiverGem, "Prines sekeru " + c.GiverName, "Tady je sekera tveho rodu.", "Baruk Khazad! Pradede, uz mas klid."));
			return true;
		}
	}

	/// <summary>A deep mining expedition (a mine or smithy worker in Erebor, offered from Mining MiningNeeded): strike the three deep
	/// veins the foreman marked in the town's streets (below the skill a vein won't give), then report.</summary>
	internal sealed class DeepMiningErrand : ErrandTemplate {
		internal const int Veins = 3, MiningNeeded = 400; //40.0 skill

		public override string Key { get { return "deepmining"; } }
		public override string Name { get { return "Vyprava do hlubin"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!EreborStories.WorksIn(c, EreborStories.Erebor, "mine", "smithy") || (c.Player.GetSkill(SkillName.Mining) < MiningNeeded)) {
				return false;
			}
			ErrandStep veins = Errands.Things(EreborStories.VeinDef, Veins, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Prokopej " + Veins + " hluboke zily (Hornictvi " + (MiningNeeded / 10) + ")", "Vsechny zily jsou otevrene. Mithril to neni, ale dobre zelezo ano.");
			if (!Errands.CanPlace(e, veins)) {
				return false;
			}
			e.offer = "Predak oznacil hluboke zily, ale nasi kopaci jsou na jine stene. Umis s krumpacem? Prokopej je a hlas, co v nich je.";
			e.thanks = "Dobra prace, haviri. Hora se ti odvdeci.";
			e.steps.Add(veins);
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas " + c.GiverName + ", co jsi nasel", "Zily jsou otevrene, je v nich zelezo.", "Tak budou mit kovari zase co delat."));
			return true;
		}

		/// <summary>A vein gives only to a miner (the skill can fall meanwhile).</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (player.GetSkill(SkillName.Mining) < MiningNeeded) {
				player.SysMessage("Tahle zila je na tebe moc tvrda. (Hornictvi alespon " + (MiningNeeded / 10) + ")");
				return false;
			}
			player.SysMessage("Krumpac zazvoni a skala se rozestoupi.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>The false Arkenstone (a market or tavern worker in Erebor): a con man sells "the Arkenstone" in the streets; catch him
	/// (he runs) and bring the fake stone to the giver.</summary>
	internal sealed class FalseArkenstoneErrand : ErrandTemplate {
		public override string Key { get { return "falsearkenstone"; } }
		public override string Name { get { return "Falesny Arcikam"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!EreborStories.WorksIn(c, EreborStories.Erebor, "market", "tavern")) {
				return false;
			}
			ErrandStep conMan = Errands.Things(EreborStories.ConManDef, 1, ErrandThingAction.Catch, ErrandThingPlace.Streets, null, true,
				"Chyt podvodnika s 'Arcikamem' v ulicich", "Mas ho. 'Arcikam' je obycejny kus skla.");
			if (!Errands.CanPlace(e, conMan)) {
				return false;
			}
			e.offer = "Po meste chodi nejaky clovek a prodava 'Arcikam'! Uz mu na to skocili dva mladi trpaslici. Chyt ho, nez zmizi.";
			e.thanks = "Sklo za cenu drahokamu. Ten uz nikoho neoklame.";
			e.steps.Add(conMan);
			e.steps.Add(Errands.Deliver(c.GiverGem, EreborStories.FakeStoneDef, "Dones 'Arcikam' " + c.GiverName, "Tady je ten jeho 'Arcikam'.",
				"Sklo! A on za nej chtel celou truhlu zlata. Tohle si dame nad dvere, pro smich."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Podvodnik se vzdava a odevzda ti svuj 'Arcikam'.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>The fish market's quota (a harbour or market worker in Dale or Esgaroth): bring the giver ten raw fish for the market.</summary>
	internal sealed class FishQuotaErrand : ErrandTemplate {
		internal const int Fish = 10;

		public override string Key { get { return "fishquota"; } }
		public override string Name { get { return "Rybi kvota"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!EreborStories.WorksIn(c, EreborStories.Dale, "harbour", "market")) {
				return false;
			}
			e.offer = "Rada mesta urcila rybarum kvotu, a my jsme pozadu. Kdyby ses chopil prutu... nebo koupil ryby, me je to jedno. Deset ryb do vecera.";
			e.thanks = "Kvota je splnena. Rada bude spokojena.";
			e.steps.Add(Errands.Fetch(c.GiverGem, "i_fish_raw", Fish, "Prines " + c.GiverName + " " + Fish + " syrovych ryb", "Tady jsou ryby na trh.", "Ciste a cerstve. Do vecera je prodam."));
			return true;
		}
	}

	/// <summary>The smuggling bargeman (pramar) (a harbour or barracks worker in Dale or Esgaroth): catch the bargeman on the road out (he runs)
	/// and bring the giver his crate of contraband.</summary>
	internal sealed class BargemanErrand : ErrandTemplate {
		public override string Key { get { return "bargeman"; } }
		public override string Name { get { return "Paserak na pramu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!EreborStories.WorksIn(c, EreborStories.Dale, "harbour", "barracks")) {
				return false;
			}
			ErrandStep bargeman = Errands.Things(EreborStories.BargemanDef, 1, ErrandThingAction.Catch, ErrandThingPlace.Outside, null, true,
				"Chyt pasujiciho pramare za mestem", "Mas ho. Na pramu mel bednu bez cla.");
			if (!Errands.CanPlace(e, bargeman)) {
				return false;
			}
			e.offer = "Jeden pramar vozi zbozi bez cla, zrovna vyklada za mestem. Chyt ho a prines tu bednu. Mestska pokladna ti za to podekuje.";
			e.thanks = "Clo je zaplaceno, i kdyz pozde. Dobra prace.";
			e.steps.Add(bargeman);
			e.steps.Add(Errands.Deliver(c.GiverGem, EreborStories.ContrabandDef, "Dones bednu bez cla " + c.GiverName, "Tady je ta bedna.",
				"Vino z Dorwinionu, bez cla. To uz se nebude opakovat."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Pramar zvedne ruce. Bednu ti necha.");
			thing.Delete();
			return true;
		}
	}
}
