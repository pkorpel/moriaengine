using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - Minas Tirith's stories (design 9.3; the beacons are the world event, the Citadel helm step 30's): the herb no one
	/// remembers in the Houses of Healing, and a mystery in the Steward's archives. Only Minas Tirith's people give them. Texts Czech
	/// without diacritics.</summary>
	public static class MinasTirithStories {
		public const string KingsfoilDef = "i_errand_kingsfoil", ShelfDef = "i_errand_archive_shelf", PageDef = "i_errand_archive_page";

		public static void Bootstrap() {
			ErrandTemplates.Register(new ForgottenHerbErrand());
			ErrandTemplates.Register(new StewardArchivesErrand());
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class MinasTirithStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(MinasTirithStories.KingsfoilDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(MinasTirithStories.ShelfDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>The herb no one remembers (a healer or herbalist of the Houses of Healing in Minas Tirith): ask an old resident who
	/// still knows the rhyme, gather three plants of kingsfoil on the road out, bring them.</summary>
	internal sealed class ForgottenHerbErrand : ErrandTemplate {
		internal const int Plants = 3;

		public override string Key { get { return "forgottenherb"; } }
		public override string Name { get { return "Zapomenuta bylina"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string role = Residents.RoleKeyOf(c.GiverGem);
			if (!CityFlavour.Is(c.City, RegionalStories.MinasTirith) || ((role != "healer") && (role != "herbalist"))) { //only the Houses of Healing have them
				return false;
			}
			Item elder = c.PickOther(BasicErrands.IsElder) ?? c.PickOther(BasicErrands.IsWorker); //the oldest there is
			ErrandStep plants = Errands.Things(MinasTirithStories.KingsfoilDef, Plants, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Natrhej " + Plants + " trsy kralovniku za mestem", "Mas dost kralovniku. Voni jako jaro.");
			if ((elder == null) || !Errands.CanPlace(e, plants)) {
				return false;
			}
			e.offer = "Je tu nemocny, kteremu nepomaha zadna nase bylina. Stare zeny kdysi mluvily o rostline, co lecila stin. Nikdo si nepamatuje jakou. " +
				ErrandContext.Who(elder) + " je nejstarsi ve meste, zeptej se.";
			e.thanks = "Kralovnik! Ted uz vim, proc ho stare zeny chvalily.";
			e.steps.Add(Errands.Talk(elder, "Zeptej se " + ErrandContext.Who(elder) + " na zapomenutou bylinu", "Pamatujes si bylinu, co leci stin?",
				"'Kdyz dech temnoty...' Ano! Kralovnik, rika se mu i athelas. Roste u cesty za mestem, nikdo si ho nevsima."));
			e.steps.Add(plants);
			e.steps.Add(Errands.Talk(c.GiverGem, "Dones kralovnik " + c.GiverName, "Tady je kralovnik.", "Rozdrtim ho do horke vody... Citis tu vuni? Nemocny uz dycha lip."));
			return true;
		}
	}

	/// <summary>A mystery in the Steward's archives (a librarian, scholar or scribe in Minas Tirith): a page of an old record is missing.
	/// Search the three dusty crates of records in the giver's venue (the last tells who borrowed it), get the page back, and return it.</summary>
	internal sealed class StewardArchivesErrand : ErrandTemplate {
		internal const int Shelves = 3;

		public override string Key { get { return "archives"; } }
		public override string Name { get { return "Zahada v archivu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string role = Residents.RoleKeyOf(c.GiverGem);
			CityVenue archive = Residents.WorkplaceOf(c.GiverGem);
			if (!CityFlavour.Is(c.City, RegionalStories.MinasTirith) || (archive == null) || ((role != "librarian") && (role != "scholar") && (role != "scribe"))) {
				return false;
			}
			Item borrower = c.PickOther(BasicErrands.IsWorker); //one the hero can reach: no prisoner behind bars
			ErrandStep search = Errands.Things(MinasTirithStories.ShelfDef, Shelves, ErrandThingAction.Use, ErrandThingPlace.Venue, archive, false,
				"Prohledej " + Shelves + " zaprasene bedny s listinami (" + archive.Label + ")",
				(borrower == null) ? "" : "V posledni bedne je listek: 'Stranu si " + BasicErrands.Sex(borrower, "vypujcil", "vypujcila") + " " + Residents.NameOf(borrower) + ".'");
			if ((borrower == null) || !Errands.CanPlace(e, search)) {
				return false;
			}
			e.offer = "Z kroniky Spravcu zmizela strana. Prave ta o prvnim Spravci! Prohledej bedny s listinami, nekde musi byt stopa.";
			e.thanks = "Kronika je zase cela. Dekuji.";
			e.steps.Add(search);
			e.steps.Add(Errands.Talk(borrower, "Vyzvedni stranu od " + ErrandContext.Who(borrower), "Mas stranu z kroniky Spravcu?",
				"Ach, ta! " + BasicErrands.Sex(borrower, "Chtel jsem si ji jen opsat a zapomnel jsem", "Chtela jsem si ji jen opsat a zapomnela jsem") + " ji vratit. Tady je."));
			e.steps.Add(Errands.Deliver(c.GiverGem, MinasTirithStories.PageDef, "Vrat stranu " + c.GiverName, "Tady je ztracena strana.",
				"To je ona. Kdo si ji vzal? ... Aha. " + BasicErrands.Sex(borrower, "Tomu", "Te") + " uz nic nepujcim."));
			return true;
		}

		/// <summary>A crate searched: dust and old deeds, but for the last one (its note is the step's reply).</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (errand.Current.progress + 1 < errand.Current.count) {
				player.SysMessage("Prach, stare listiny o danich z Lossarnachu...");
			}
			thing.Delete();
			return true;
		}
	}
}
