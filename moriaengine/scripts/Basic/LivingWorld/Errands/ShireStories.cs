using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - the Shire's stories (design 9.3, the ones step 30 left): mushroom thieves in a farmer's field, the
	/// long-planned birthday party that ends in a party for everyone near, and the relative after the silver spoons. Only
	/// Hobitin's people give them. Texts Czech without diacritics.</summary>
	public static class ShireStories {
		public const string Hobitin = "Hobitin";
		public const string MushroomThiefDef = "c_errand_mushroom_thief", RelativeDef = "c_errand_relative", SpoonDef = "i_errand_spoon", FireworksDef = "i_errand_fireworks";
		internal static readonly TagKey PartyAtTK = TagKey.Acquire("_shire_party_at_"); //the last party the player was a guest at (UTC ticks)

		public static void Bootstrap() {
			ErrandTemplates.Register(new MushroomThievesErrand());
			ErrandTemplates.Register(new BirthdayPartyErrand());
			ErrandTemplates.Register(new SilverSpoonsErrand());
		}

		/// <summary>The venue types of the farmer's folk: the field's owner and those who buy and cook his mushrooms.</summary>
		internal static bool IsFarmFolk(Item gem) {
			switch (BasicErrands.VenueKey(gem)) {
				case "farm": case "market": case "tavern": case "bakery": case "brewery":
					return BasicErrands.IsWorker(gem);
				default:
					return false;
			}
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class ShireStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(ShireStories.MushroomThiefDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(ShireStories.RelativeDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(ShireStories.SpoonDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>Mushroom thieves (a farmer's folk in Hobitin): catch the three young hobbits who raid the field outside the town, then
	/// turn them in, or let them off and share the mushrooms: half the pay, and the mushrooms.</summary>
	internal sealed class MushroomThievesErrand : ErrandTemplate {
		internal const int Thieves = 3, ShareChoice = 1, MushroomsShared = 6;

		public override string Key { get { return "mushrooms"; } }
		public override string Name { get { return "Zlodeji hub"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, ShireStories.Hobitin) || !ShireStories.IsFarmFolk(c.GiverGem)) {
				return false;
			}
			e.offer = "Zase mi nekdo leze do hub! Tri mladi lumpove z vedlejsi vesnice, kazdou noc. Chyt je za mestem a privad je sem.";
			e.thanks = "Pole je zase v klidu.";
			ErrandStep thieves = Errands.Things(ShireStories.MushroomThiefDef, Thieves, ErrandThingAction.Catch, ErrandThingPlace.Outside, null, true,
				"Pochytej " + Thieves + " zlodeje hub za mestem", "Mas je vsechny, i s kosiky.");
			if (!Errands.CanPlace(e, thieves)) {
				return false; //no road out: they'd stand beside the player in town
			}
			e.steps.Add(thieves);
			e.steps.Add(Errands.Choose(c.GiverGem, "Rekni " + c.GiverName + ", co se zlodeji",
				new[] { "Tady jsou, at je potrestaji.", "Pustil jsem je. Podelili jsme se o houby." },
				new[] { "Ted si to rozmysli, nez zase vlezou do ciziho pole.", "Podelili? Hm. Mladi jsou mladi. Ale za to ti plnou mzdu nedam." }));
			return true;
		}

		/// <summary>Let off: half the pay.</summary>
		public override int PayPercent(Errand e) {
			return (e.ChoiceAt(1) == ShareChoice) ? 50 : 100;
		}

		/// <summary>Let off: the thieves' share of the mushrooms is the hero's.</summary>
		public override void OnDone(Player player, Errand e) {
			if (e.ChoiceAt(1) != ShareChoice) {
				return;
			}
			ItemDef mushroom = ThingDef.GetByDefname("i_mushroom") as ItemDef;
			if (mushroom != null) {
				for (int i = 0; i < MushroomsShared; i++) { //a mushroom doesn't stack: each its own
					mushroom.Create(player.Backpack);
				}
				player.SysMessage("Lumpove ti z vdecnosti nechali hrst hub.");
			}
		}

		/// <summary>A thief caught is marched off to the field's owner.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			player.SysMessage("Mas ho. Odevzdane se nechava vest s kosikem v ruce.");
			thing.Delete();
			return true;
		}
	}

	/// <summary>The birthday party (any worker in Hobitin, a long-planned one): two invitations, the fireworks fetched from another
	/// resident, a cake, and in the evening the party: every player near the host and in its sight is a guest, gets a little
	/// experience (once in 24 real hours each), and the host sets off the fireworks.</summary>
	internal sealed class BirthdayPartyErrand : ErrandTemplate {
		internal const int Invitations = 2, GuestRange = 12, GuestExp = 150, CakeCount = 1, FromHour = 19, ToHour = 23, GuestEveryHours = 24;

		public override string Key { get { return "birthday"; } }
		public override string Name { get { return "Narozeninova oslava"; } }
		public override ErrandTier Tier { get { return ErrandTier.Large; } }
		public override int Minutes { get { return 180; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, ShireStories.Hobitin) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			List<Item> others = new List<Item>();
			for (int i = 0; i <= Invitations; i++) { //the guests, and the one with the fireworks
				Item other = c.PickOther(gem => !others.Contains(gem));
				if (other == null) {
					return false;
				}
				others.Add(other);
			}
			e.offer = "Uz rok chystam oslavu svych narozenin a porad neni hotovo! Pozvanky, ohnostroj, dort... Pomuzes mi? Vecer bude slavit cele mesto.";
			e.thanks = "To byla oslava! Na tu se bude vzpominat jeste za sto let.";
			for (int i = 0; i < Invitations; i++) {
				e.steps.Add(Errands.Deliver(others[i], "i_scroll_blank", "Pozvanka na oslavu: " + ErrandContext.Who(others[i]),
					"Pozvanka na narozeniny od " + c.GiverName + ".", ErrandContext.Pick("Prijdu! A s chuti.", "Tam nesmim chybet.", "Konecne! Uz jsem myslel, ze nepozve.")));
			}
			Item fireworks = others[Invitations];
			e.steps.Add(Errands.Talk(fireworks, "Vyzvedni ohnostroj: " + ErrandContext.Who(fireworks), "Jdu pro ohnostroj na oslavu.", "Tady je. Opatrne s nim, at nam nevyleti strecha."));
			e.steps.Add(Errands.Deliver(c.GiverGem, ShireStories.FireworksDef, "Dones ohnostroj " + c.GiverName, "Ohnostroj je tady.", "Vyborne! Schovam ho do sklepa, at ho nenajdou deti."));
			e.steps.Add(Errands.Fetch(c.GiverGem, "i_cake", CakeCount, "Prines " + c.GiverName + " dort", "Tady je dort.", "Ten voni! Ten nikdo nesni driv, nez se zapali svicky."));
			e.steps.Add(Errands.Wait(c.GiverGem, FromHour, ToHour, "Prijd vecer na oslavu k " + c.GiverName + " (" + FromHour + ".-" + ToHour + ". hodina)",
				"Oslava zacina!", "Vitejte vsichni! Jidla je dost a ohnostroj uz ceka."));
			return true;
		}

		/// <summary>The party: the host's fireworks, and every player near is a guest (the errand's player too).</summary>
		public override void OnDone(Player player, Errand errand) {
			Item host = Residents.ById(errand.giverId);
			NPC body = (host == null) ? null : Residents.BodyOf(host);
			if (body == null) {
				return;
			}
			Party(body);
		}

		/// <summary>The party at the host: fireworks, a word, and each player near and in sight is a guest, once in 24 real hours
		/// (a game day is 24 minutes: parties in turn could have been farmed).</summary>
		internal static int Party(NPC host) {
			EffectFactory.StationaryEffectAt(new Point4D(host.X, host.Y, host.Z + 15, host.M), 0x373A, 10, 30);
			EffectFactory.StationaryEffectAt(new Point4D(host.X + 2, host.Y - 1, host.Z + 20, host.M), 0x3709, 10, 30);
			host.Say("Na zdravi vsem! A at zije dalsich sto jedenacti let!");
			int guests = 0;
			foreach (AbstractCharacter ch in host.GetMap().GetPlayersInRange(host.X, host.Y, GuestRange)) {
				if ((ch is Player guest) && !guest.Flag_Dead && !guest.IsGM && host.CanSeeLOS(guest).Allow
						&& !((guest.GetTag(ShireStories.PartyAtTK) is long was) && ((Errands.Now - new DateTime(was, DateTimeKind.Utc)).TotalHours < GuestEveryHours))) {
					guest.SetTag(ShireStories.PartyAtTK, Errands.Now.Ticks);
					LevelingSystem.AddExperience(guest, GuestExp);
					guest.SysMessage("Jsi hostem na hobiti oslave! Jidlo, pisne a ohnostroj. (+" + GuestExp + " zkusenosti)", ClassAbilities.ClassMessageHue);
					guests++;
				}
			}
			return guests;
		}
	}

	/// <summary>The silver spoons (any worker in Hobitin): a covetous relative made off with the spoons. Catch the relative in the
	/// streets (who drops them and runs), pick up the three spoons, and bring them back.</summary>
	internal sealed class SilverSpoonsErrand : ErrandTemplate {
		internal const int Spoons = 3;

		public override string Key { get { return "spoons"; } }
		public override string Name { get { return "Stribrne lzicky"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, ShireStories.Hobitin) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			e.offer = "Byla tu na navsteve sestrenice z druheho kolena. A s ni zmizely babiccine stribrne lzicky! Najdi ji, nez se ztrati z Kraje.";
			e.thanks = "Babiccine lzicky! Vsechny tri. Ta si u me cajicek uz neda.";
			ErrandStep relative = Errands.Things(ShireStories.RelativeDef, 1, ErrandThingAction.Catch, ErrandThingPlace.Streets, null, true,
				"Dostihni pribuznou " + c.GiverName + " v ulicich", "Pribuzna lzicky upustila a utekla. Posbirej je.");
			ErrandStep spoons = Errands.Things(ShireStories.SpoonDef, Spoons, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Posbirej " + Spoons + " stribrne lzicky v ulicich", "Mas vsechny lzicky.");
			if (!Errands.CanPlace(e, relative) || !Errands.CanPlace(e, spoons)) {
				return false;
			}
			e.steps.Add(relative);
			e.steps.Add(spoons);
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat lzicky " + c.GiverName, "Tady jsou tvoje lzicky.", "A ani jedna neni ohnuta. Dekuji!"));
			return true;
		}

		/// <summary>The relative caught drops the spoons and is off home; a spoon picked up goes into the hero's keeping.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (thing is NPC) {
				player.SysMessage("\"Ja si je jen pujcila!\" Pribuzna pousti lzicky na zem a uz je pryc.");
			}
			thing.Delete();
			return true;
		}
	}
}
