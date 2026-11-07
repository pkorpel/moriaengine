using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1256 - one short play: who says each line (an actor by number), the line an actor forgets, and three lines the
	/// prompter can offer for it, one of them right.</summary>
	public sealed class TheatrePlay {
		public readonly string Title;
		public readonly int[] Speakers;
		public readonly string[] Lines;
		public readonly int Forgotten;
		public readonly string[] Prompts;
		public readonly int RightPrompt;

		public TheatrePlay(string title, int forgotten, string[] prompts, int rightPrompt, params object[] speakerAndLine) {
			this.Title = title;
			this.Forgotten = forgotten;
			this.Prompts = prompts;
			this.RightPrompt = rightPrompt;
			this.Speakers = new int[speakerAndLine.Length / 2];
			this.Lines = new string[speakerAndLine.Length / 2];
			for (int i = 0; i < this.Lines.Length; i++) {
				this.Speakers[i] = (int) speakerAndLine[i * 2];
				this.Lines[i] = (string) speakerAndLine[(i * 2) + 1];
			}
		}
	}

	/// <summary>#1256 - the theatre: a play at dusk, an extra's part, the prompter's help, and errands for the director (design 9.2).
	/// A play runs on the director's TheatrePlayTimer; ResidentSchedule's timer starts it (Theatre.Tick).</summary>
	public static class Theatre {
		public const int Dusk = 19;
		internal static readonly TagKey LastPlayTK = TagKey.Acquire("_theatre_last_play_");
		internal static readonly TagKey ExtrasTK = TagKey.Acquire("_theatre_extras_");

		/// <summary>Tests: a play's timer isn't armed and the test calls Step itself; and the next play's number.</summary>
		public static bool StepByHand;
		public static int? PlayOverride;

		internal static readonly TheatrePlay[] Plays = {
			new TheatrePlay("Kral a pastyr", 5,
				new[] { "Vlci? To jsou jen psi s vetsimi zuby.", "Vlci? V mem kralovstvi? Kde jsou moji lovci?", "Ovce? Kdo potrebuje ovce, kdyz ma zlato?" }, 1,
				0, "Kdo jsi, poutniku, ze stojis pred kralem?",
				1, "Jen pastyr, pane. Prisel jsem pro spravedlnost.",
				0, "Spravedlnost? Ta se v teto sini neprodava.",
				1, "Pak ji vezmu darem, nebo odejdu s prazdnou.",
				2, "Pane, ten pastyr mluvi pravdu. Vlci mu sezrali stado.",
				0, "Vlci? V mem kralovstvi? Kde jsou moji lovci?",
				2, "Pili v hospode, pane. Jako kazdy vecer.",
				0, "Dejte mu deset ovci. A lovcum vodu misto piva!"),
			new TheatrePlay("Ztraceny prsten", 4,
				new[] { "Zametat se ma pred domem, ne v pokladnici.", "Kdo zameta, ten nekrade, to vi kazdy.", "S kostetem v ruce a prstenem v kapse?" }, 2,
				0, "Prsten! Kde je muj prsten? Byl tu jeste vcera!",
				1, "Mozna ho vzala straka, pani. Ty kradou vsechno, co se leskne.",
				0, "Straka? A ma ta straka i klic od me truhly?",
				2, "Pani, ja nic, ja jen zametal.",
				1, "S kostetem v ruce a prstenem v kapse?",
				2, "To je muj vlastni prsten! Od maminky!",
				0, "Tvoje maminka mela muj erb? Straze!",
				1, "A straka je nevinna. Jako vzdycky."),
			new TheatrePlay("Drak z papiru", 5,
				new[] { "To byl hostinsky, kdyz uvidel ucet.", "To byla bourka nad horami.", "To chrapal stary Bert u kamen." }, 0,
				0, "Utecte! Nad mestem leti drak!",
				1, "Drak? To je jen papirovy drak, co pousteji deti.",
				0, "Papirovy drak s ohnivym dechem?",
				2, "To nebyl ohen, to kovar zatopil ve vyhni.",
				1, "A ten rev?",
				2, "To byl hostinsky, kdyz uvidel ucet.",
				0, "Takze zadny drak? Skoda. Uz jsem si koupil mec.",
				1, "Mec se hodi vzdycky. Treba na papirove draky."),
		};

		public static void Bootstrap() {
			string[] all = { "director", "actor", "prompter", "spectator" };
			VenueActions.Register(new VenueAction("theatre_when", "Kdy hrajete?", new[] { "theatre" }, all, null, TellWhen));
			VenueActions.Register(new VenueAction("theatre_extra", "Muzu hrat v komparzu?", new[] { "theatre" }, new[] { "director" },
				(player, npc, gem) => !IsExtra(player, Residents.WorkplaceOf(gem)), BecomeExtra));
			VenueActions.Register(new VenueAction("theatre_prompt", "Napovez!", new[] { "theatre" }, new[] { "prompter" },
				(player, npc, gem) => WaitsForPrompt(Residents.WorkplaceOf(gem)), OfferPrompt));
			ErrandTemplates.Register(new MissingActorErrand());
			ErrandTemplates.Register(new ForgottenPropErrand());
		}

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		#region Who is where
		internal static Item GemOf(CityVenue venue, string role) {
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				if (Residents.RoleKeyOf(gem) == role) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>The venue's living residents of a role who are in it now: an actor out on an errand doesn't speak, an absent
		/// audience doesn't clap.</summary>
		internal static List<NPC> BodiesOf(CityVenue venue, string role) {
			List<NPC> bodies = new List<NPC>();
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				NPC npc = (Residents.RoleKeyOf(gem) == role) ? Residents.BodyOf(gem) : null;
				if ((npc != null) && !npc.Flag_Dead && (VenueRegistry.At(npc) == venue)) {
					bodies.Add(npc);
				}
			}
			return bodies;
		}

		internal static NPC DirectorOf(CityVenue venue) {
			Item gem = (venue == null) ? null : GemOf(venue, "director");
			NPC npc = (gem == null) ? null : Residents.BodyOf(gem);
			return ((npc == null) || npc.Flag_Dead) ? null : npc;
		}

		public static TheatrePlayTimer PlayIn(CityVenue venue) {
			NPC director = DirectorOf(venue);
			return (director == null) ? null : director.GetTimer(TheatrePlayTimer.Key) as TheatrePlayTimer;
		}

		internal static bool WaitsForPrompt(CityVenue venue) {
			TheatrePlayTimer play = (venue == null) ? null : PlayIn(venue);
			return (play != null) && (play.gapLeft > 0);
		}
		#endregion

		#region Starting a play
		/// <summary>From ResidentSchedule's timer: each theatre plays once at dusk when a player is near to see it.</summary>
		public static void Tick() {
			double hour = VenueClock.Hour;
			if ((hour < Dusk) || (hour >= Dusk + 2)) {
				return;
			}
			foreach (CityVenue venue in VenueRegistry.All) {
				if ((venue.VenueType != null) && (venue.VenueType.Key == "theatre") && MayStart(venue) && PlayerNear(venue)) {
					StartPlay(venue);
				}
			}
		}

		internal static bool MayStart(CityVenue venue) {
			if ((DirectorOf(venue) == null) || (PlayIn(venue) != null)) {
				return false;
			}
			return !(venue.GetTag(LastPlayTK) is double last) || ((Globals.TimeInSeconds - last) >= (Settings.playEveryMinutes * 60.0));
		}

		private static bool PlayerNear(CityVenue venue) {
			Map map = venue.GetMap();
			if (map == null) {
				return false;
			}
			foreach (AbstractCharacter ch in map.GetPlayersInRange(venue.X, venue.Y, 18)) {
				if (!ch.Flag_Disconnected) {
					return true;
				}
			}
			return false;
		}

		/// <summary>The director opens the play; the rest follows a line at a time.</summary>
		public static TheatrePlayTimer StartPlay(CityVenue venue) {
			NPC director = DirectorOf(venue);
			if (director == null) {
				return null;
			}
			venue.SetTag(LastPlayTK, Globals.TimeInSeconds);
			int play = PlayOverride ?? Globals.dice.Next(Plays.Length);
			TheatrePlayTimer timer = new TheatrePlayTimer { play = play, venueId = venue.VenueId };
			director.AddTimer(TheatrePlayTimer.Key, timer);
			timer.Step(director);
			if (!StepByHand) {
				int seconds = Math.Max(1, Settings.playLineSeconds);
				timer.DueInSeconds = seconds;
				timer.PeriodInSeconds = seconds;
			}
			return timer;
		}
		#endregion

		#region Lines in the dialog
		internal static void TellWhen(Player player, NPC npc, Item gem) {
			CityVenue venue = Residents.WorkplaceOf(gem);
			double hour = VenueClock.Hour;
			bool dusk = (hour >= Dusk) && (hour < Dusk + 2);
			if (PlayIn(venue) != null) {
				npc.Say("Prave hrajeme! Pojd dal a bud zticha.");
			} else if (dusk && !MayStart(venue) && (DirectorOf(venue) != null)) { //tonight's has been
				npc.Say("Dnes uz je po predstaveni. Hrajeme zase zitra za soumraku.");
			} else {
				npc.Say("Hrajeme kazdy vecer za soumraku, v sedm.");
			}
		}

		internal static bool IsExtra(Player player, CityVenue venue) {
			return (venue != null) && (Array.IndexOf(Extras(venue), player.Uid.ToString(CultureInfo.InvariantCulture)) >= 0);
		}

		private static string[] Extras(CityVenue venue) {
			return ((venue.GetTag(ExtrasTK) as string) ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}

		internal static void BecomeExtra(Player player, NPC npc, Item gem) {
			CityVenue venue = Residents.WorkplaceOf(gem);
			if (PlayIn(venue) != null) {
				npc.Say("Uz hrajeme. Prijd zitra pred predstavenim.");
				return;
			}
			if (player.GetSkill(SkillName.Musicianship) < Settings.extraMusicianship) {
				npc.Say("Na jevisti musis aspon trochu umet zpivat a hrat. Prijd, az se to naucis.");
				return;
			}
			List<string> extras = new List<string>(Extras(venue)) { player.Uid.ToString(CultureInfo.InvariantCulture) };
			venue.SetTag(ExtrasTK, string.Join(",", extras));
			npc.Say("Dobre, budes stat vzadu a drzet kopi. Hlavne nezakopni. Hrajeme za soumraku.");
		}

		/// <summary>The end of a play: every extra still in the theatre gets their wages.</summary>
		internal static void PayExtras(CityVenue venue, NPC director) {
			Item directorGem = Residents.GemOf(director);
			string directorId = (directorGem == null) ? null : Residents.IdOf(directorGem);
			foreach (string uid in Extras(venue)) {
				if (!int.TryParse(uid, NumberStyles.Integer, CultureInfo.InvariantCulture, out int u)) {
					continue;
				}
				if ((Thing.UidGetCharacter(u) is Player extra) && !extra.Flag_Disconnected && (VenueRegistry.At(extra) == venue)) {
					GoldUtils.Grant(extra, (uint) Math.Max(0, Settings.extraWage));
					LevelingSystem.AddExperience(extra, Math.Max(0, Settings.extraExp));
					if (directorId != null) {
						Residents.AddFriendship(extra, directorId, 1);
					}
					extra.SysMessage("Reziser ti vyplatil " + Settings.extraWage + " zlatych za tvou roli.");
				}
			}
			venue.RemoveTag(ExtrasTK);
		}

		internal static void OfferPrompt(Player player, NPC npc, Item gem) {
			CityVenue venue = Residents.WorkplaceOf(gem);
			TheatrePlayTimer play = PlayIn(venue);
			if ((play == null) || (play.gapLeft <= 0)) {
				return;
			}
			D_TheatrePrompt dialog = SingletonScript<D_TheatrePrompt>.Instance;
			player.DialogClose(dialog, 0);
			player.Dialog(player, dialog, new DialogArgs(npc));
		}

		/// <summary>The player's pick for the forgotten line: the actor says it; the right one pays, a wrong one gets laughs.
		/// False when there was no gap to fill.</summary>
		public static bool Prompt(Player player, NPC prompter, int choice) {
			Item gem = Residents.GemOf(prompter);
			CityVenue venue = (gem == null) ? null : Residents.WorkplaceOf(gem);
			TheatrePlayTimer play = (venue == null) ? null : PlayIn(venue);
			if ((play == null) || (play.gapLeft <= 0)) {
				return false;
			}
			TheatrePlay p = play.Play;
			if ((choice < 0) || (choice >= p.Prompts.Length)) {
				return false;
			}
			NPC actor = play.Speaker(venue, DirectorOf(venue), p.Forgotten);
			actor.Say(p.Prompts[choice]);
			if (choice == p.RightPrompt) {
				GoldUtils.Grant(player, (uint) Math.Max(0, Settings.promptReward));
				Residents.AddFriendship(player, Residents.IdOf(gem), 1);
				prompter.Say("Presne tak! Diky, to bylo o chlup.");
			} else {
				foreach (NPC spectator in BodiesOf(venue, "spectator")) {
					spectator.Say(ErrandContext.Pick("*smich*", "Hahaha!", "*chichotani*"));
				}
				prompter.Say("To nebylo ono...");
			}
			play.gapLeft = 0;
			play.line = p.Forgotten + 1;
			return true;
		}
		#endregion
	}

	/// <summary>#1256 - a running play on its director: the opening, then a line each step; at the forgotten line the actor waits
	/// promptLines steps for a prompt, then the prompter whispers it; at the end the audience claps and the extras are paid.</summary>
	[SaveableClass, DeepCopyableClass]
	public class TheatrePlayTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_theatre_play_");

		[SaveableData, CopyableData]
		public int play, line, gapLeft;

		[SaveableData, CopyableData]
		public bool opened, gapDone;

		[SaveableData, CopyableData]
		public string venueId;

		[LoadingInitializer, DeepCopyImplementation]
		public TheatrePlayTimer() {
		}

		internal TheatrePlay Play {
			get { return Theatre.Plays[Math.Abs(this.play) % Theatre.Plays.Length]; }
		}

		internal bool Done {
			get { return this.opened && (this.line > this.Play.Lines.Length); }
		}

		/// <summary>Who says a line: the actor of its number, or the director when the theatre has no such actor.</summary>
		internal NPC Speaker(CityVenue venue, NPC director, int lineIndex) {
			List<NPC> actors = Theatre.BodiesOf(venue, "actor");
			int who = this.Play.Speakers[lineIndex];
			return (actors.Count == 0) ? director : actors[who % actors.Count];
		}

		/// <summary>One step of the play. Returns false once it has ended.</summary>
		public bool Step(NPC director) {
			CityVenue venue = VenueRegistry.ById(this.venueId);
			if ((venue == null) || (director == null) || director.Flag_Dead) {
				return false;
			}
			TheatrePlay p = this.Play;
			if (!this.opened) {
				this.opened = true;
				director.Say("Vazeni divaci, hrajeme pro vas: " + p.Title + "!");
				return true;
			}
			if (this.gapLeft > 0) {
				this.gapLeft--;
				if (this.gapLeft == 0) { //nobody helped: the prompter whispers it
					NPC prompter = Theatre.BodiesOf(venue, "prompter").Count > 0 ? Theatre.BodiesOf(venue, "prompter")[0] : null;
					if (prompter != null) {
						prompter.Say("(septem) " + p.Lines[p.Forgotten]);
					}
					this.Speaker(venue, director, p.Forgotten).Say(p.Lines[p.Forgotten]);
					this.line = p.Forgotten + 1;
				}
				return true;
			}
			if (this.line < p.Lines.Length) {
				NPC speaker = this.Speaker(venue, director, this.line);
				if ((this.line == p.Forgotten) && !this.gapDone) {
					this.gapDone = true;
					this.gapLeft = Math.Max(1, VenueLifeSettings.instance.promptLines);
					speaker.Say(ErrandContext.Pick("Ehm... ehm...", "Ja... totiz...", "*rozpacite mlci*"));
					return true;
				}
				speaker.Say(p.Lines[this.line]);
				this.line++;
				return true;
			}
			foreach (NPC spectator in Theatre.BodiesOf(venue, "spectator")) {
				spectator.Say(ErrandContext.Pick("*tleska*", "Bravo!", "*nadsene tleska*"));
			}
			director.Say("Dekujeme! Byli jste skvele publikum.");
			Theatre.PayExtras(venue, director);
			this.line = p.Lines.Length + 1;
			return false;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.Step(cont as NPC)) {
				this.Delete();
			}
		}
	}

	/// <summary>#1256 - the prompter's three lines for the forgotten one. args: the prompter.</summary>
	public class D_TheatrePrompt : CompiledGumpDef {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC prompter = (NPC) args[0];
			Item gem = Residents.GemOf(prompter);
			TheatrePlayTimer play = Theatre.PlayIn(Residents.WorkplaceOf(gem));
			string[] prompts = (play == null) ? new string[0] : play.Play.Prompts;
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 420, 90 + (prompts.Length * 24), 2620);
			this.AddLabel(16, 12, 95, "Napoveda sepce: Ktera veta to byla?");
			int y = 44;
			for (int i = 0; i < prompts.Length; i++) {
				this.AddButton(16, y, 0xFA5, 0xFA7, 1 + i, GumpButtonType.Reply, 0);
				this.AddLabel(52, y + 2, 68, prompts[i]);
				y += 24;
			}
			this.AddButton(16, y, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(52, y + 2, 955, "Nevim.");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC prompter = args[0] as NPC;
			if ((player == null) || (prompter == null) || prompter.IsDeleted || (gr.PressedButton <= 0) || !Errands.CanTalk(player, prompter)) {
				return;
			}
			VenueAction action = VenueActions.Get("theatre_prompt");
			if ((action == null) || !VenueActions.For(player, prompter, Residents.GemOf(prompter)).Contains(action)) {
				return;
			}
			Theatre.Prompt(player, prompter, gr.PressedButton - 1);
		}
	}

	/// <summary>#1256 - the director's actor isn't there before the curtain: find them and send them in.</summary>
	internal sealed class MissingActorErrand : ErrandTemplate {
		public override string Key { get { return "missingactor"; } }
		public override string Name { get { return "Chybejici herec"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			CityVenue theatre = Residents.WorkplaceOf(c.GiverGem);
			if ((Residents.RoleKeyOf(c.GiverGem) != "director") || (theatre == null)) {
				return false;
			}
			Item actor = c.PickOther(gem => (Residents.RoleKeyOf(gem) == "actor") && (Residents.WorkplaceOf(gem) == theatre));
			if (actor == null) {
				return false;
			}
			string name = Residents.NameOf(actor);
			e.offer = "Za chvili zvedame oponu a " + name + " nikde! Najdi " + BasicErrands.Sex(actor, "ho", "ji") + " a posli sem, prosim.";
			e.thanks = "Uz je tady! Predstaveni je zachranene.";
			e.steps.Add(Errands.Talk(actor, "Najdi " + BasicErrands.Sex(actor, "herce ", "herecku ") + name + " a posli " + BasicErrands.Sex(actor, "ho", "ji") + " do divadla",
				"Reziser te shani, za chvili se hraje!", "Proboha, ja zapomnel" + BasicErrands.Sex(actor, "", "a") + "! Uz bezim."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se k " + c.GiverName, "Uz je na ceste.", "Diky."));
			return true;
		}
	}

	/// <summary>#1256 - a prop left in another venue: bring it to the director.</summary>
	internal sealed class ForgottenPropErrand : ErrandTemplate {
		public override string Key { get { return "prop"; } }
		public override string Name { get { return "Zapomenuta rekvizita"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		//the item, what it is (as the director says it), "here's the ..." and "bring it" (ho/ji)
		private static readonly string[][] props = {
			new[] { "i_book_sm", "scenar", "scenar", "ho" }, new[] { "i_ring_gold", "kralovsky prsten", "kralovsky prsten", "ho" },
			new[] { "i_mug", "pohar pro krale", "pohar", "ho" }
		};

		public override bool TryBuild(ErrandContext c, Errand e) {
			CityVenue theatre = Residents.WorkplaceOf(c.GiverGem);
			if ((Residents.RoleKeyOf(c.GiverGem) != "director") || (theatre == null)) {
				return false;
			}
			Item where = c.PickOther(gem => Residents.WorkplaceOf(gem) != theatre);
			CityVenue venue = (where == null) ? null : Residents.WorkplaceOf(where);
			if ((venue == null) || (ResidentSchedule.SpotIn(venue, "prop") == null)) {
				return false;
			}
			string[] prop = props[Globals.dice.Next(props.Length)];
			if (!(ThingDef.GetByDefname(prop[0]) is ItemDef)) {
				return false;
			}
			e.offer = "Herci nechali " + prop[1] + " v: " + venue.Label + ". Bez toho nemuzeme hrat! Doneses " + prop[3] + "?";
			e.thanks = "Konecne! Ted muze predstaveni zacit.";
			ErrandStep find = Errands.Deliver(c.GiverGem, prop[0], "Najdi " + prop[1] + " (" + venue.Label + ") a dones " + prop[3] + " " + c.GiverName,
				"Tady je " + prop[2] + ".", "Vyborne!");
			find.venueId = venue.VenueId;
			e.steps.Add(find);
			return true;
		}
	}
}
