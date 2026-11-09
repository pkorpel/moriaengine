using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - Edoras's stories (design 9.3, the horse thieves are step 30's): the muster of an eored, riding out to its
	/// banners on horseback, and a song for the hall (Musicianship). Only Edoras's people give them. Texts Czech without diacritics.</summary>
	public static class EdorasStories {
		public const string Edoras = "Edoras";
		public const string BannerDef = "i_errand_eored_banner", StoolDef = "i_errand_hall_stool";

		public static void Bootstrap() {
			ErrandTemplates.Register(new EoredMusterErrand());
			ErrandTemplates.Register(new HallSongErrand());
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class EdorasStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(EdorasStories.BannerDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(EdorasStories.StoolDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>The muster of an eored (a stable or barracks worker in Edoras): ride to the eored's three banners on the road out
	/// within 20 minutes, mounted at each (on foot a banner doesn't count), and report.</summary>
	internal sealed class EoredMusterErrand : ErrandTemplate {
		internal const int Banners = 3;

		public override string Key { get { return "eored"; } }
		public override string Name { get { return "Sraz eoredu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Minutes { get { return 20; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			if (!CityFlavour.Is(c.City, EdorasStories.Edoras) || !BasicErrands.IsWorker(c.GiverGem) || ((venue != "stable") && (venue != "barracks"))) {
				return false;
			}
			ErrandStep banners = Errands.Things(EdorasStories.BannerDef, Banners, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Objed na koni " + Banners + " korouhve eoredu za mestem", "Eored je svolan. Vrat se s hlasenim.");
			if (!Errands.CanPlace(e, banners)) {
				return false; //no road out: the banners would stand in the town
			}
			e.offer = "Eored se sjizdi! Objed na koni jeho korouhve na ceste za mestem a dej jezdcum znameni. Do dvaceti minut, jezdec Rohanu nemeska.";
			e.thanks = "Eored je pripraven. Jezdis jako Rohir.";
			e.steps.Add(banners);
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas " + c.GiverName + ", ze eored je svolan", "Vsechny korouhve vedi.", "Dobre. Kopyta zahrmi do rana."));
			return true;
		}

		/// <summary>A banner counts only from the saddle.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (!player.Flag_Riding) {
				player.SysMessage("Jezdci eoredu cekaji jezdce, ne pesaka. Nasedni na kone.");
				return false;
			}
			player.SysMessage("Jezdci u korouhve zvedaji kopi. Eored vi.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>A song for the hall (a tavern or town hall worker in Edoras, offered from Musicianship MusicNeeded): learn the old
	/// verse from another resident, then play it in the giver's venue at the bard's stool (an instrument in the pack, Erebor's
	/// Musicianship roll; a poor try can be played again), and hear the giver's thanks.</summary>
	internal sealed class HallSongErrand : ErrandTemplate {
		internal const int MusicNeeded = 300; //30.0 skill
		internal const double TrySeconds = 3; //a try at the stool, then a pause: the tavern hears each one
		internal static readonly TagKey TriedAtTK = TagKey.Acquire("_hallsong_tried_");

		public override string Key { get { return "hallsong"; } }
		public override string Name { get { return "Pisen pro sin"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			CityVenue hall = Residents.WorkplaceOf(c.GiverGem);
			if (!CityFlavour.Is(c.City, EdorasStories.Edoras) || !BasicErrands.IsWorker(c.GiverGem) || ((venue != "tavern") && (venue != "townhall")) || (hall == null)) {
				return false;
			}
			if (c.Player.GetSkill(SkillName.Musicianship) < MusicNeeded) {
				return false; //nobody asks a song of one who can't play
			}
			Item elder = c.PickOther();
			if (elder == null) {
				return false;
			}
			ErrandStep play = Errands.Things(EdorasStories.StoolDef, 1, ErrandThingAction.Use, ErrandThingPlace.Venue, hall, false,
				"Zahraj starou pisen v: " + hall.Label + " (Hudba " + (MusicNeeded / 10) + ", nastroj)", "Sin ztichl a pak se rozjasal. Pisen se povedla.");
			if (!Errands.CanPlace(e, play)) {
				return false;
			}
			e.offer = "Dnes vecer je hostina a nikdo neumi starou pisen o Eorlovi. " + ErrandContext.Who(elder) + " si ji jeste pamatuje. Nauc se ji a zahraj ji tady.";
			e.thanks = "Takovou pisen sin dlouho neslysel.";
			e.steps.Add(Errands.Talk(elder, "Nauc se starou pisen od " + ErrandContext.Who(elder), "Naucis me pisen o Eorlovi?",
				"Hm, kdysi jsem ji zpival... 'Kde je kun a jezdec? Kde je roh, co zaznival?' Tak. Zapamatuj si to."));
			e.steps.Add(play);
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Zahral jsem pisen.", "Slysel jsem. Zitra ji bude zpivat cele mesto."));
			return true;
		}

		/// <summary>The song needs an instrument and Erebor's Musicianship roll; a poor try leaves the stool to try again.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			Musical instrument = Bard.FindInstrument(player);
			if (instrument == null) {
				player.SysMessage("Bez nastroje se pisen hrat neda.");
				return false;
			}
			if ((player.GetTag(TriedAtTK) is double tried) && (Globals.TimeInSeconds - tried < TrySeconds)) {
				player.SysMessage("Jeste ladis nastroj.");
				return false;
			}
			player.SetTag(TriedAtTK, Globals.TimeInSeconds);
			if (!Bard.Plays(player, instrument)) {
				player.SysMessage("Nekdo v sini se zasmal. Zkus to znovu.");
				return false;
			}
			thing.Delete();
			return true;
		}
	}
}
