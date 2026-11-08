using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1300 - regional stories (design 9.3) as errand templates that only their city's people give: the hobbit post
	/// (Hobitin), the horse thieves (Edoras), the Citadel guard's helm (Minas Tirith) and the elves' letters to the Grey Havens
	/// (given in Hobitin). Texts Czech without diacritics.</summary>
	public static class RegionalStories {
		public const string Hobitin = "Hobitin", Edoras = "Edoras", MinasTirith = "Minas_Tirith", Havens = "Sede_pristavy";
		public const string HorseThiefDef = "c_errand_horsethief", HorseDef = "c_errand_stolen_horse", HelmDef = "i_errand_helm", ChestDef = "i_errand_elf_chest";
		internal static readonly TagKey PostRoundsTK = TagKey.Acquire("_hobbit_post_rounds_");

		/// <summary>The hobbit post's ranks: the rounds done that reach each, and its name.</summary>
		internal static readonly int[] PostRankFrom = { 0, 5, 15, 40 };
		internal static readonly string[] PostRanks = { "Postak", "Starsi postak", "Postmistr", "Mistr posty" };

		public static void Bootstrap() {
			ErrandTemplates.Register(new HobbitPostErrand());
			ErrandTemplates.Register(new HorseThievesErrand());
			ErrandTemplates.Register(new CitadelHelmErrand());
			ErrandTemplates.Register(new GreyHavensErrand());
		}

		public static int PostRounds(Player player) {
			return (player.GetTag(PostRoundsTK) is int rounds) ? rounds : 0;
		}

		/// <summary>The rank (0 = Postak) the player's rounds have reached.</summary>
		public static int PostRank(Player player) {
			int rounds = PostRounds(player), rank = 0;
			while ((rank + 1 < PostRankFrom.Length) && (rounds >= PostRankFrom[rank + 1])) {
				rank++;
			}
			return rank;
		}

		/// <summary>A venue of the city of this type, at random, or null.</summary>
		internal static CityVenue VenueOfType(string city, string type) {
			List<CityVenue> venues = VenueRegistry.InCity(city);
			venues.RemoveAll(v => v.VenueType.Key != type);
			return (venues.Count == 0) ? null : venues[Globals.dice.Next(venues.Count)];
		}

		internal static string VenueTypeOf(Item gem) {
			CityVenue work = Residents.WorkplaceOf(gem);
			return (work == null) ? null : work.VenueType.Key;
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class RegionalStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(RegionalStories.HorseDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(RegionalStories.HelmDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(RegionalStories.ChestDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>The hobbit post (Hobitin): a timed round of three letters to residents; the rounds done raise the player's post rank,
	/// and each rank takes a better share.</summary>
	internal sealed class HobbitPostErrand : ErrandTemplate {
		internal const int Letters = 3, SharePerRank = 15;

		public override string Key { get { return "hobbitpost"; } }
		public override string Name { get { return "Hobiti posta"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }
		public override int Minutes { get { return 20; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, RegionalStories.Hobitin) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			List<Item> to = new List<Item>();
			for (int i = 0; i < Letters; i++) {
				Item other = c.PickOther(gem => !to.Contains(gem));
				if (other == null) {
					return false;
				}
				to.Add(other);
			}
			string rank = RegionalStories.PostRanks[RegionalStories.PostRank(c.Player)];
			e.offer = "Posta, " + rank.ToLowerInvariant() + "! Tri dopisy, a do dvaceti minut, at je kazdy doma vcas na svaci.";
			e.thanks = "Vsechno doruceno vcas. Tak se to dela.";
			foreach (Item gem in to) {
				e.steps.Add(Errands.Deliver(gem, "i_scroll_blank", "Dorucit dopis: " + ErrandContext.Who(gem), "Posta pro tebe!", "Dekuji, hned si ho prectu."));
			}
			return true;
		}

		/// <summary>The rank's better share, by the rank held as the round is done (OnDone counts it after).</summary>
		public override int PayPercent(Player player, Errand errand) {
			return 100 + (SharePerRank * RegionalStories.PostRank(player));
		}

		/// <summary>The rounds done go up; a new rank is told.</summary>
		public override void OnDone(Player player, Errand errand) {
			int before = RegionalStories.PostRank(player);
			player.SetTag(RegionalStories.PostRoundsTK, RegionalStories.PostRounds(player) + 1);
			int after = RegionalStories.PostRank(player);
			if (after > before) {
				player.SysMessage("Hobiti posta te povysila: " + RegionalStories.PostRanks[after] + ".", ClassAbilities.ClassMessageHue);
			}
		}
	}

	/// <summary>Horse thieves (Edoras): drive off the thieves on the road out of the city, bring back the horses they took, report.</summary>
	internal sealed class HorseThievesErrand : ErrandTemplate {
		internal const int Thieves = 2, Horses = 2;

		public override string Key { get { return "horsethieves"; } }
		public override string Name { get { return "Zlodeji koni"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string type = RegionalStories.VenueTypeOf(c.GiverGem);
			if (!CityFlavour.Is(c.City, RegionalStories.Edoras) || !BasicErrands.IsWorker(c.GiverGem)
					|| ((type != "stable") && (type != "barracks") && (type != "townhall") && (type != "market"))) {
				return false;
			}
			e.offer = "Zlodeji z kopcu nam v noci odvedli kone. Jsou na ceste za mestem. Zazen je a privad kone zpatky!";
			e.thanks = "Kone jsou doma. Rohan ti dekuje.";
			e.steps.Add(Errands.Things(RegionalStories.HorseThiefDef, Thieves, ErrandThingAction.Kill, ErrandThingPlace.Outside, null, false,
				"Zazen " + Thieves + " zlodeje koni za mestem", "Zlodeji jsou pryc."));
			e.steps.Add(Errands.Things(RegionalStories.HorseDef, Horses, ErrandThingAction.Catch, ErrandThingPlace.Outside, null, false,
				"Chyt " + Horses + " ukradene kone za mestem", "Kone jsou na ceste domu."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas se u " + c.GiverName, "Kone jsou zpatky.", "Vyborne! Takovych jezdcu mame malo."));
			return true;
		}

		/// <summary>A horse caught trots home to the stables.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Kun te poznal a klusem se vraci do mesta.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>The Citadel guard's helm (Minas Tirith): a guard lost it in a tavern the night before inspection; find it there and
	/// bring it back before the deadline.</summary>
	internal sealed class CitadelHelmErrand : ErrandTemplate {
		public override string Key { get { return "citadelhelm"; } }
		public override string Name { get { return "Ztracena prilba"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }
		public override int Minutes { get { return 30; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string role = Residents.RoleKeyOf(c.GiverGem);
			if (!CityFlavour.Is(c.City, RegionalStories.MinasTirith) || ((role != "guard") && (role != "recruit"))) {
				return false;
			}
			CityVenue tavern = RegionalStories.VenueOfType(c.City, "tavern");
			e.offer = (tavern == null)
				? "Vcera vecer jsem nekde v ulicich nechal prilbu. Zitra je inspekce! Najdi ji, prosim, at me kapitan nevyhodi."
				: "Vcera vecer v: " + tavern.Label + " jsem tam nechal prilbu. Zitra je inspekce! Najdi ji, prosim, at me kapitan nevyhodi.";
			e.thanks = "Moje prilba! Zachranil jsi mi krk.";
			e.steps.Add(Errands.Things(RegionalStories.HelmDef, 1, ErrandThingAction.Use, (tavern == null) ? ErrandThingPlace.Streets : ErrandThingPlace.Venue, tavern, false,
				"Najdi prilbu " + c.GiverName + ((tavern == null) ? "" : " (" + tavern.Label + ")"), "Mas ji. Je trochu pomacknuta, ale cela."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat prilbu " + c.GiverName + " pred inspekci", "Tady je tvoje prilba.", "Kapitan nic nepozna. Dekuji!"));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			thing.Delete(); //picked up
			return true;
		}
	}

	/// <summary>The elves' letters (given in Hobitin): carry them to the elves' chest at the Grey Havens before the ship sails.</summary>
	internal sealed class GreyHavensErrand : ErrandTemplate {
		public override string Key { get { return "greyhavens"; } }
		public override string Name { get { return "Dopisy elfu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Large; } }
		public override int Minutes { get { return 60; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, RegionalStories.Hobitin) || !BasicErrands.IsWorker(c.GiverGem) || (WorldEvents.CentreOf(RegionalStories.Havens) == null)) {
				return false;
			}
			e.offer = "Elfove, co tudy tahli na zapad, tu nechali dopisy pro sve blizke. Lod v Sedych pristavech brzy odpluje. Doneses je tam vcas?";
			e.thanks = "Dopisy odplouvaji za more. Elfove na tebe nezapomenou.";
			e.steps.Add(Errands.Things(RegionalStories.ChestDef, 1, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Vloz dopisy do truhly elfu v Sedych pristavech, nez lod odpluje", "Dopisy jsou v truhle. Lod muze plout.", RegionalStories.Havens));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			thing.Delete(); //the chest goes aboard
			return true;
		}
	}
}
