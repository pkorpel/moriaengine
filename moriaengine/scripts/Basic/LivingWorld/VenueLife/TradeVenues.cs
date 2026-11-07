using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1260 - the harbour, the stable, the market, the bakery and the barracks in a resident's dialog: labour at the docks,
	/// the stable and the trough, tending a stall, the night sentry and target practice (design 9.2).</summary>
	public static class TradeVenues {
		internal static readonly TagKey ArcheryAtTK = TagKey.Acquire("_barracks_archery_at_");
		public const int Shots = 5;

		private static readonly VenueLabour.Lines dockLines = new VenueLabour.Lines {
			Rest = "Oddechni si, lod neutece.",
			Tired = "Sotva stojis na nohou. Prijd, az naberes sily.",
			Done = "Na dnesek je vylozeno. Prijd zitra.",
			Thanks = new[] { "Dalsi bedna na brehu. Diky, tady mas.", "S tebou to jde rychleji. Tady mas za praci.", "Opatrne s tim sudem... dobre. Diky." }
		};

		private static readonly VenueLabour.Lines stableLines = new VenueLabour.Lines {
			Rest = "Kone uz jsou vycistene. Prijd za chvili.",
			Tired = "Vypadas utahane. Odpocin si.",
			Done = "Na dnesek je ve stajich hotovo. Prijd zitra.",
			Thanks = new[] { "Hrive se leskne. Diky, tady mas.", "Seno je nahore, voda v korytu. Tady mas za praci.", "Kobyla te ma rada. Diky." }
		};

		private static readonly VenueLabour.Lines doughLines = new VenueLabour.Lines {
			Rest = "Testo musi kynout. Prijd za chvili.",
			Tired = "Hnest testo chce silu. Odpocin si.",
			Done = "Na dnesek je napeceno. Prijd zitra.",
			Thanks = new[] { "Krasne vyhnetene. Tady mas.", "Ze tohohle bude chleba jako z pohadky. Diky.", "Mouky je vsude, ale testo je dobre. Tady mas." }
		};

		public static void Bootstrap() {
			VenueActions.Register(new VenueAction("harbour_unload", "Pomoct s vykladkou", new[] { "harbour" }, new[] { "harbourmaster", "docker", "sailor" }, null,
				(player, npc, gem) => VenueLabour.Work(player, npc, gem, "harbour", dockLines)));
			VenueActions.Register(new VenueAction("stable_chores", "Pomoct ve stajich", new[] { "stable" }, new[] { "stablemaster", "groom" }, null,
				(player, npc, gem) => VenueLabour.Work(player, npc, gem, "stable", stableLines)));
			VenueActions.Register(new VenueAction("bakery_dough", "Pomoct s testem", new[] { "bakery" }, new[] { "baker" }, null,
				(player, npc, gem) => VenueLabour.Work(player, npc, gem, "bakery", doughLines)));
			VenueActions.Register(new VenueAction("market_stall", "Pohlidat stanek", new[] { "market" }, new[] { "merchant" },
				(player, npc, gem) => ((VenueClock.Shift & VenueShift.Work) != 0) && !VenueStay.IsStaying(player), (player, npc, gem) => VenueStay.Begin(player, npc, gem, "stall")));
			VenueActions.Register(new VenueAction("barracks_sentry", "Nocni hlidka", new[] { "barracks" }, new[] { "guard" },
				(player, npc, gem) => (VenueClock.Shift == VenueShift.Night) && !VenueStay.IsStaying(player), (player, npc, gem) => VenueStay.Begin(player, npc, gem, "sentry")));
			VenueActions.Register(new VenueAction("barracks_archery", "Strelba do terce", new[] { "barracks" }, new[] { "captain", "guard" }, null, Archery));
			VenueStay.Register(new StayKind("stall", () => Settings.stallSeconds, "Pohlidej mi to tu, hned jsem zpatky. A nikomu nic nedavej zadarmo!", StallEnds));
			VenueStay.Register(new StayKind("sentry", () => Settings.sentrySeconds, "Hlidkuj tady v kasarnach. Kdo opusti stanoviste, nedostane nic.", SentryEnds));
		}

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		private static void StallEnds(Player player, CityVenue market, string merchantId) {
			GoldUtils.Grant(player, (uint) Math.Max(0, Settings.stallWage));
			if (merchantId != null) {
				Residents.AddFriendship(player, merchantId, 1);
			}
			Item merchant = (merchantId == null) ? null : Residents.ById(merchantId);
			player.SysMessage((merchant == null) ? ("Za hlidani stanku mas " + Settings.stallWage + " zlatych.") :
				(Residents.NameOf(merchant) + " je zpatky a za hlidani ti " + BasicErrands.Sex(merchant, "dal", "dala") + " " + Settings.stallWage + " zlatych."));
		}

		private static void SentryEnds(Player player, CityVenue barracks, string guardId) {
			GoldUtils.Grant(player, (uint) Math.Max(0, Settings.sentryWage));
			if (barracks.City != null) {
				Reputation.Change(player, barracks.City, Settings.sentryReputation, null, false); //a routine night is no hero's deed
			}
			if (guardId != null) {
				Residents.AddFriendship(player, guardId, 1);
			}
			player.SysMessage("Hlidka skoncila bez potizi. Mas " + Settings.sentryWage + " zlatych a mesto si toho vazi.");
		}

		/// <summary>Five shots at the target, each a hit when a roll of 0-999 comes under the Archery skill (in tenths); experience
		/// per hit, a round every archeryMinutes real minutes.</summary>
		internal static void Archery(Player player, NPC npc, Item gem) {
			int skill = player.GetSkill(SkillName.Archery);
			if (skill <= 0) {
				npc.Say("Nejdriv se nauc drzet luk. Pak prijd.");
				return;
			}
			long now = Errands.Now.Ticks;
			if ((player.GetTag(ArcheryAtTK) is long at) && ((now - at) < Settings.archeryMinutes * TimeSpan.TicksPerMinute)) {
				npc.Say("Terc se jeste opravuje. Prijd pozdeji.");
				return;
			}
			player.SetTag(ArcheryAtTK, now);
			int hits = 0;
			for (int i = 0; i < Shots; i++) {
				if (Globals.dice.Next(1000) < skill) {
					hits++;
				}
			}
			if (hits > 0) {
				LevelingSystem.AddExperience(player, hits * Math.Max(0, Settings.archeryExp));
			}
			npc.Say("Zasahu: " + hits + " z " + Shots + ". " + ((hits >= 4) ? "Vyborna rana!" : ((hits >= 2) ? "Ujde to." : "Jeste trenuj.")));
		}
	}
}
