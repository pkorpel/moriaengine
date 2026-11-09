using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - Fornost's stories (design 9.3): the barrow that wakes at night, with an old blade to bring back, and the Rangers'
	/// watch, reading tracks (Tracking). Only Fornost's people give them. Texts Czech without diacritics.</summary>
	public static class FornostStories {
		public const string Fornost = "Fornost";
		public const string WightDef = "c_errand_barrow_wight", BladeDef = "i_errand_barrow_blade", TracksDef = "i_errand_tracks";

		public static void Bootstrap() {
			ErrandTemplates.Register(new BarrowErrand());
			ErrandTemplates.Register(new RangerWatchErrand());
		}

		/// <summary>The Rangers and those who keep the old fortress: the barracks, the stable, the town hall.</summary>
		internal static bool IsRangerFolk(Item gem) {
			string venue = BasicErrands.VenueKey(gem);
			return BasicErrands.IsWorker(gem) && ((venue == "barracks") || (venue == "stable") || (venue == "townhall"));
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class FornostStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(FornostStories.BladeDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(FornostStories.TracksDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>The barrow that wakes at night (a Ranger in Fornost): come back after dark, lay the wight that rises from the barrow
	/// on the road out, take the old blade it guarded, and bring it to the giver.</summary>
	internal sealed class BarrowErrand : ErrandTemplate {
		internal const int FromHour = 22, ToHour = 4;

		public override string Key { get { return "barrow"; } }
		public override string Name { get { return "Probuzena mohyla"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Minutes { get { return 120; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, FornostStories.Fornost) || !FornostStories.IsRangerFolk(c.GiverGem)) {
				return false;
			}
			ErrandStep wight = Errands.Things(FornostStories.WightDef, 1, ErrandThingAction.Kill, ErrandThingPlace.Outside, null, false,
				"Uloz k spanku prizrak z mohyly za mestem", "Prizrak se rozplynul. Starou cepel odnesl nekam k ceste, najdi ji.");
			ErrandStep blade = Errands.Things(FornostStories.BladeDef, 1, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Najdi starou cepel u cesty za mestem", "Cepel je studena jako led. Nes ji domu.");
			if (!Errands.CanPlace(e, wight)) {
				return false; //no road out: the barrow would be in the town
			}
			e.offer = "Stara mohyla za mestem se v noci probouzi. Neco tam chodi a hlida cepel kralu Arnoru. Prijd po setmeni, uloz to k spanku a cepel mi prines.";
			e.thanks = "Cepel Arnoru je zpatky. Mohyla bude spat.";
			e.steps.Add(Errands.Wait(c.GiverGem, FromHour, ToHour, "Prijd za " + c.GiverName + " po setmeni (" + FromHour + ".-" + ToHour + ". hodina)",
				"Je noc. Jdu k mohyle.", "Dobre. Drz se cesty a nepoustej mec z ruky."));
			e.steps.Add(wight);
			e.steps.Add(blade);
			e.steps.Add(Errands.Talk(c.GiverGem, "Prines cepel " + c.GiverName, "Tady je cepel z mohyly.", "Je to ona. Pohrbime ji u kralu, kam patri."));
			return true;
		}
	}

	/// <summary>The Rangers' watch (a Ranger in Fornost, offered from Tracking TrackingNeeded): read the three tracks on the road out
	/// (still refused below it, the skill can fall meanwhile), each telling which way the orcs went, then tell the giver. The right
	/// way pays; a wrong one pays nothing.</summary>
	internal sealed class RangerWatchErrand : ErrandTemplate {
		internal const int Tracks = 3, TrackingNeeded = 300; //30.0 skill
		internal static readonly string[] Ways = { "na sever", "na vychod", "na zapad" };

		public override string Key { get { return "rangerwatch"; } }
		public override string Name { get { return "Hlidka hranicaru"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			if (!CityFlavour.Is(c.City, FornostStories.Fornost) || !BasicErrands.IsWorker(c.GiverGem) || ((venue != "barracks") && (venue != "stable"))) {
				return false;
			}
			if (c.Player.GetSkill((int) SkillName.Tracking) < TrackingNeeded) {
				return false; //not offered to one who can't read tracks: it would only cost the day's errand from this giver
			}
			ErrandStep tracks = Errands.Things(FornostStories.TracksDef, Tracks, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Precti " + Tracks + " stopy za mestem (Stopovani " + (TrackingNeeded / 10) + ")", "Vsechny stopy jsou prectene. Vrat se s hlasenim.");
			if (!Errands.CanPlace(e, tracks)) {
				return false;
			}
			int right = Globals.dice.Next(Ways.Length);
			string[] answers = new string[Ways.Length], replies = new string[Ways.Length];
			for (int i = 0; i < Ways.Length; i++) {
				answers[i] = "Sli " + Ways[i] + ".";
				replies[i] = (i == right) ? "Tak to je. Posleme za nimi hlidku, nez dojdou k vesnicim." : "Tam? To se mi nezda. Poslu tam nekoho, ale ty stopy jsi cetl spatne.";
			}
			e.offer = "V noci prosla kolem mesta skupina skretu. Precti jejich stopy za mestem a rekni mi, kam sli. Hranicar se nespoleha na stesti.";
			e.thanks = "Hlaseni prijato.";
			e.steps.Add(tracks);
			ErrandStep report = Errands.Choose(c.GiverGem, "Hlas " + c.GiverName + ", kam skreti sli", answers, replies);
			report.count = right; //the right way, kept with the step
			e.steps.Add(report);
			return true;
		}

		private static bool Right(Errand e) {
			ErrandStep report = e.steps[e.steps.Count - 1];
			return report.chosen == report.count;
		}

		public override int PayPercent(Errand e) {
			return Right(e) ? 100 : 0;
		}

		public override int GiverFriendship(Errand e, int baseFriend) {
			return Right(e) ? baseFriend : -1;
		}

		public override void OnDone(Player player, Errand e) {
			if (!Right(e)) {
				player.SysMessage("Stopy jsi precetl spatne. Skreti jsou pryc.", 0x21);
			}
		}

		/// <summary>A track is read only with enough Tracking (it stays otherwise); reading it tells the way.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (player.GetSkill((int) SkillName.Tracking) < TrackingNeeded) {
				player.SysMessage("Ty stopy neumis precist. (Stopovani alespon " + (TrackingNeeded / 10) + ")");
				return false;
			}
			player.SysMessage("Okovane boty, tezky krok. Skreti. Stopy vedou " + Ways[errand.steps[errand.steps.Count - 1].count] + ".");
			thing.Delete();
			return true;
		}
	}
}
