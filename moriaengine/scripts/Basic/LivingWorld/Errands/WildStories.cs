using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1317 - the wild's stories (design 9.3; the troll at dusk is the world event): a cave creature's riddles (typed answers,
	/// a wrong one and it attacks), the ferry crossing with a Black Rider behind (the Shire's), and the singing hermit who finds lost
	/// travellers. Texts Czech without diacritics.</summary>
	public static class WildStories {
		public const string PoolDef = "i_errand_dark_pool", CreatureDef = "c_errand_cave_creature", FerryDef = "i_errand_ferry_rope", RiderDef = "c_errand_black_rider",
			HutDef = "i_errand_hermit_hut", TravellerDef = "c_errand_lost_traveller";
		internal static readonly TagKey AngryUntilTK = TagKey.Acquire("_wild_angry_until_"), AnsweredTK = TagKey.Acquire("_wild_answered_"),
			RiddleTK = TagKey.Acquire("_wild_riddle_");
		internal const double AngrySeconds = 120, RiderSeconds = 300;

		/// <summary>The player's tag holding the Black Rider of this errand: one per errand, so a second ferry errand's rider never
		/// takes the first's place (nor is deleted with it).</summary>
		internal static TagKey RiderKey(Errand errand) {
			return TagKey.Acquire("_wild_rider_" + errand.id.ToString(CultureInfo.InvariantCulture) + "_");
		}

		public static void Bootstrap() {
			ErrandTemplates.Register(new CaveRiddlesErrand());
			ErrandTemplates.Register(new BlackRiderErrand());
			ErrandTemplates.Register(new SingingHermitErrand());
		}

		/// <summary>An NPC of the wild set on the hero, gone after `seconds` (its own timer: it never outlives a few minutes).</summary>
		internal static NPC Spawn(string defname, Player hero, int dx, int dy, double seconds) {
			NPCDef def = ThingDef.GetByDefname(defname) as NPCDef;
			int[] at = WorldSpots.NearestStandable(hero.GetMap(), hero.X + dx, hero.Y + dy, 4);
			if ((def == null) || (at == null)) {
				return null;
			}
			NPC npc = (NPC) def.Create(at[0], at[1], at[2], hero.M);
			Item robe = npc.FindLayer(LayerNames.Robe) as Item;
			if (robe != null) {
				robe.SetAttribute(ItemAttributes.attr_newbie, true); //goes with the body: no robe to farm from each ferry errand
			}
			WildVanishTimer timer = new WildVanishTimer();
			npc.AddTimer(WildVanishTimer.Key, timer);
			timer.DueInSeconds = seconds;
			NPCAiTimer.GetFor(npc)?.EngageTarget(hero);
			return npc;
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class WildStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(WildStories.PoolDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(WildStories.FerryDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(WildStories.HutDef, SingletonScript<E_ErrandThingItem>.Instance);
			QuestNpc.Attach(WildStories.TravellerDef, SingletonScript<E_ErrandThingNpc>.Instance);
		}
	}

	/// <summary>#1317 - a creature of the wild leaves when its time is up.</summary>
	[SaveableClass, DeepCopyableClass]
	public class WildVanishTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_wildVanish_");

		[LoadingInitializer, DeepCopyImplementation]
		public WildVanishTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Thing thing) && !thing.IsDeleted) {
				thing.Delete();
			}
		}
	}

	/// <summary>A cave creature's riddles (a tavern or library worker of any city): at the dark pool on the road out something asks a
	/// riddle; the typed answer must be right (Games' riddles). A wrong one and it comes out to attack, and the pool stays dark for two
	/// minutes. Then tell the giver.</summary>
	internal sealed class CaveRiddlesErrand : ErrandTemplate {
		public override string Key { get { return "caveriddles"; } }
		public override string Name { get { return "Hadanky ve tme"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			if (!BasicErrands.IsWorker(c.GiverGem) || ((venue != "tavern") && (venue != "library"))) {
				return false;
			}
			ErrandStep pool = Errands.Things(WildStories.PoolDef, 1, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Najdi temnou tun za mestem a vyhraj hru hadanek", "Hlas ze tmy zasipi a zmizi. Vyhral jsi.");
			if (!Errands.CanPlace(e, pool)) {
				return false;
			}
			e.offer = "Za mestem je jeskyne s temnou tuni. Kdo tam zajde, toho neco vyzve ke hre hadanek. Kdo prohraje... no. Vyhraj a povez mi, co to bylo.";
			e.thanks = "Vyhral jsi nad tim? Tak ted mame pokoj aspon na cas.";
			e.steps.Add(pool);
			e.steps.Add(Errands.Talk(c.GiverGem, "Povez " + c.GiverName + ", co sidli v jeskyni", "Vyhral jsem hru hadanek.", "Hubeny, bledy, s velkyma ocima? To jsem si myslel."));
			return true;
		}

		/// <summary>The pool: once answered, it counts; while the creature is angry it stays dark; else the riddle.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			if (thing.GetTag(WildStories.AnsweredTK) != null) {
				thing.Delete();
				return true;
			}
			if ((thing.GetTag(WildStories.AngryUntilTK) is double until) && (Globals.TimeInSeconds < until)) {
				player.SysMessage("Z tmy se ozyva zle syceni. Ted ne.");
				return false;
			}
			int riddle = (thing.GetTag(WildStories.RiddleTK) is int asked) ? asked : Globals.dice.Next(Games.Riddles.Length); //the same one until it's answered
			thing.SetTag(WildStories.RiddleTK, riddle);
			player.SysMessage("Z tmy se ozve: 'Hadej, my zlatko: " + Games.Riddles[riddle][0] + "'");
			D_CaveRiddle dialog = SingletonScript<D_CaveRiddle>.Instance;
			player.DialogClose(dialog, 0); //one riddle on the screen at a time
			player.Dialog(player, dialog, new DialogArgs(thing, riddle));
			return false;
		}

		/// <summary>The typed answer: right lets the pool count; wrong brings the creature out at the hero.</summary>
		internal static bool Answer(Player player, Item pool, int riddle, string answer) {
			if ((pool == null) || pool.IsDeleted || player.Flag_Dead || (riddle < 0) || (riddle >= Games.Riddles.Length)) {
				return false;
			}
			string given = Games.Plain(answer);
			bool right = false;
			for (int i = 1; i < Games.Riddles[riddle].Length; i++) {
				right |= given == Games.Riddles[riddle][i];
			}
			if (right) {
				pool.SetTag(WildStories.AnsweredTK, 1);
				Errands.UseThing(pool, player);
				return true;
			}
			pool.SetTag(WildStories.AngryUntilTK, Globals.TimeInSeconds + WildStories.AngrySeconds);
			pool.RemoveTag(WildStories.RiddleTK); //another riddle next time
			player.SysMessage("'Spatne, spatne, my zlatko! Ted te sezereme!'", 0x21);
			if (!player.IsGM) {
				WildStories.Spawn(WildStories.CreatureDef, player, 1, 1, WildStories.AngrySeconds);
			}
			return false;
		}
	}

	/// <summary>#1317 - the cave creature's riddle and a line for the answer. args: the dark pool, the riddle's number.</summary>
	public class D_CaveRiddle : CompiledGumpDef {
		internal const int AnswerButton = 1, AnswerEntry = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int riddle = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 340, 170, 2620);
			this.AddLabel(16, 12, 95, "Hlas ze tmy");
			this.AddHtml(16, 36, 308, 60, "<basefont color=#FFFFFF>" + Games.Riddles[riddle][0] + "</basefont>", false, false);
			this.AddLabel(16, 102, 955, "Odpoved:");
			this.AddTextEntry(80, 102, 240, 20, 52, AnswerEntry, "");
			this.AddButton(16, 132, 0xFA5, 0xFA7, AnswerButton, GumpButtonType.Reply, 0);
			this.AddLabel(52, 134, 68, "Odpovedet");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			Item pool = args[0] as Item;
			if ((player == null) || (pool == null) || pool.IsDeleted || (gr.PressedButton != AnswerButton)) {
				return;
			}
			if ((player.M != pool.M) || (Point2D.GetSimpleDistance(player, pool) > 2) || (Math.Abs(player.Z - pool.Z) > 15) || !player.CanSeeLOS(pool).Allow) {
				player.SysMessage("Hlas z tmy neslysis. Pojd blize k tuni."); //the reach of Errands.UseThing, so a right answer always counts
				return;
			}
			CaveRiddlesErrand.Answer(player, pool, Convert.ToInt32(args[1], CultureInfo.InvariantCulture), gr.GetTextResponse(AnswerEntry));
		}
	}

	/// <summary>The ferry crossing (any worker in Hobitin): reach the ferry on the road out within 15 minutes and pull the rope; a
	/// Black Rider sets out after the hero as the step begins (calm to others, gone in five minutes or when the river is crossed).</summary>
	internal sealed class BlackRiderErrand : ErrandTemplate {
		public override string Key { get { return "blackrider"; } }
		public override string Name { get { return "Cerny jezdec u privozu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Minutes { get { return 15; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, RegionalStories.Hobitin) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			ErrandStep ferry = Errands.Things(WildStories.FerryDef, 1, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Dobehni k privozu za mestem a strhni lano", "Privoz odrazi od brehu. Jezdec na brehu zustal stat a sycel.");
			if (!Errands.CanPlace(e, ferry)) {
				return false;
			}
			e.offer = "Na ceste stoji jezdec v cernem a vyptava se na pytliky. Musis za reku, a hned! Privoz je za mestem. Utikej a neohlizej se.";
			e.thanks = "Jsi v bezpeci. Ten jezdec... ten nebyl z tohoto sveta.";
			e.steps.Add(ferry);
			e.steps.Add(Errands.Talk(c.GiverGem, "Hlas " + c.GiverName + ", ze jsi za rekou", "Jsem za rekou. Jezdec zustal na brehu.", "Vodu nemaji radi. Dobre, ze jsi utekl."));
			return true;
		}

		/// <summary>The ferry step begins: the rider appears behind the hero.</summary>
		public override void OnStepStart(Player player, Errand errand) {
			if ((errand.index == 0) && !player.IsGM) {
				NPC rider = WildStories.Spawn(WildStories.RiderDef, player, -6, -6, WildStories.RiderSeconds);
				if (rider != null) {
					player.SetTag(WildStories.RiderKey(errand), rider);
					player.SysMessage("Za tebou se ozve dusot kopyt. Cerny jezdec!", 0x21);
				}
			}
		}

		/// <summary>The rope pulled: across the river the rider can't follow.</summary>
		public override bool OnThing(Player player, Errand errand, Thing thing) {
			this.OnEnd(player, errand);
			thing.Delete();
			return true;
		}

		/// <summary>Given up or run out (or done): the rider goes too.</summary>
		public override void OnEnd(Player player, Errand errand) {
			TagKey key = WildStories.RiderKey(errand);
			if ((player.GetTag(key) is NPC rider) && !rider.IsDeleted) {
				rider.Delete();
			}
			player.RemoveTag(key);
		}
	}

	/// <summary>The singing hermit (any worker in Hobitin, Fornost or Edoras): find the hermit's hut on the road out (he heard of the
	/// lost traveller), find the traveller there and set them on the way home, then tell the giver.</summary>
	internal sealed class SingingHermitErrand : ErrandTemplate {
		public override string Key { get { return "singinghermit"; } }
		public override string Name { get { return "Zpevavy poustevnik"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!(CityFlavour.Is(c.City, RegionalStories.Hobitin) || CityFlavour.Is(c.City, FornostStories.Fornost) || CityFlavour.Is(c.City, RegionalStories.Edoras))
					|| !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			ErrandStep hut = Errands.Things(WildStories.HutDef, 1, ErrandThingAction.Use, ErrandThingPlace.Outside, null, false,
				"Najdi chysi zpevaveho poustevnika za mestem", "Poustevnik prozpevuje: 'Poutnik bloudi kolem, hned tady za stromy.'");
			ErrandStep traveller = Errands.Things(WildStories.TravellerDef, 1, ErrandThingAction.Catch, ErrandThingPlace.Outside, null, false,
				"Najdi zbloudileho poutnika u cesty", "Poutnik si oddechne. Ted uz cestu domu najde sam.");
			if (!Errands.CanPlace(e, hut) || !Errands.CanPlace(e, traveller)) {
				return false;
			}
			e.offer = "Muj bratranec sel vcera pres les a nevratil se. Za mestem zije poustevnik, co porad zpiva. Rika se, ze najde kazdeho, kdo zabloudi.";
			e.thanks = "Je doma! A porad si pobrukuje poustevnikovu pisen.";
			e.steps.Add(hut);
			e.steps.Add(traveller);
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Poutnik je zpatky.", "Diky tobe. A poustevnikovi, at mu dobre zpiva."));
			return true;
		}

		public override bool OnThing(Player player, Errand errand, Thing thing) {
			thing.Delete();
			return true;
		}
	}
}
