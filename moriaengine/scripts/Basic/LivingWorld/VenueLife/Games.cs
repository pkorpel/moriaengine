using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1298 - .settings "Hry": the games of design 9.7 beyond the tavern's dice and the barracks' target practice.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class GameSettings : SettingsMetaCategory {
		[SavedMember]
		public static GameSettings instance = new GameSettings();

		[LoadingInitializer]
		public GameSettings() {
		}

		/// <summary>Arm wrestling (bets are the dice's): the same resident takes the same player on once in armMinutes real minutes;
		/// a player has armPerHour bouts a real hour; a bout with no winner after armRounds pulls is a draw.</summary>
		[SaveableData]
		public int armMinutes = 20, armPerHour = 10, armRounds = 15;

		/// <summary>How strong the residents are at arm wrestling and the drinking contest (Str; a player's toughness at drinking is
		/// (Str + Vit) / 2). Residents' own Str is one value for all (120), so it says nothing about the person.</summary>
		[SaveableData]
		public int armRivalStr = 60, drinkRivalToughness = 60;

		/// <summary>The drinking contest: its stake, once in drinkMinutes real minutes; each round lowers the chance to keep up
		/// by drinkRoundPenalty percent.</summary>
		[SaveableData]
		public int drinkStake = 30, drinkMinutes = 60, drinkRoundPenalty = 10;

		/// <summary>The archery contest: its fee and prize, the guard's hit chance (0-1000, as Archery in tenths), once in
		/// archeryContestMinutes real minutes.</summary>
		[SaveableData]
		public int archeryFee = 50, archeryPrize = 150, archeryRival = 600, archeryContestMinutes = 60;

		/// <summary>Riddles: the experience a right answer pays, one riddle in riddleMinutes real minutes.</summary>
		[SaveableData]
		public int riddleExp = 50, riddleMinutes = 15;
	}

	/// <summary>#1298 - the games of design 9.7 offered as venue actions: arm wrestling and the drinking contest in the tavern and
	/// the mess hall, the archery contest at the barracks, riddles from a scholar or a tavern patron. GMs don't play for money:
	/// a GM pays nothing, so a GM's stake would make gold from nothing.</summary>
	public static class Games {
		internal const int Shots = 5, Pulls = 3;
		private static readonly TagKey armHourTK = TagKey.Acquire("_game_arm_hour_");
		private static readonly TagKey armCountTK = TagKey.Acquire("_game_arm_count_");
		private static readonly TagKey drinkAtTK = TagKey.Acquire("_game_drink_at_");
		private static readonly TagKey archeryAtTK = TagKey.Acquire("_game_archery_at_");
		internal static readonly TagKey RiddleTK = TagKey.Acquire("_game_riddle_");
		internal static readonly TagKey RiddleAtTK = TagKey.Acquire("_game_riddle_at_");

		/// <summary>Tests: pulls, rounds (0-99) and shots (0-999) in this order before any random one.</summary>
		public static readonly Queue<int> RollOverride = new Queue<int>();

		public static void Bootstrap() {
			VenueActions.Register(new VenueAction("game_arm", "Dame paku?", Tavern.Drinking, new[] { "patron", "grunt" }, null, OfferArm));
			VenueActions.Register(new VenueAction("game_drink", "Soutez v piti", Tavern.Drinking, new[] { "innkeeper", "cook" },
				(player, npc, gem) => (Residents.RoleKeyOf(gem) != "cook") || (Residents.WorkplaceOf(gem)?.VenueType.Key == "messhall"), Drink));
			VenueActions.Register(new VenueAction("game_archery", "Strelecka soutez", new[] { "barracks" }, new[] { "captain" }, null, ArcheryContest));
			VenueActions.Register(new VenueAction("game_riddle", "Hadanku?", new[] { "library", "tavern" }, new[] { "scholar", "patron" }, null, AskRiddle));
		}

		private static GameSettings Settings {
			get { return GameSettings.instance; }
		}

		private static int Roll(int below) {
			return (RollOverride.Count > 0) ? RollOverride.Dequeue() : Globals.dice.Next(below);
		}

		/// <summary>True when `at` (a tag of ticks) is less than `minutes` real minutes ago.</summary>
		private static bool Recently(Thing who, TagKey at, int minutes) {
			return (who.GetTag(at) is long then) && ((Errands.Now.Ticks - then) < minutes * TimeSpan.TicksPerMinute);
		}

		/// <summary>Double a stake back, without overflowing an int a GM's setting could make large.</summary>
		private static uint Double(int stake) {
			return (uint) Math.Min(uint.MaxValue, 2L * Math.Max(0, stake));
		}

		private static TagKey ArmAtKey(Item gem) {
			return Tavern.KeyFor("_game_arm_at_", Residents.IdOf(gem)); //a saved tag's name holds only word characters
		}

		#region Arm wrestling
		internal static void OfferArm(Player player, NPC npc, Item gem) {
			if (player.IsGM) {
				npc.Say("S GMem se nepretlacuju. GM za sazku neplati.");
				return;
			}
			D_ArmWrestle dialog = SingletonScript<D_ArmWrestle>.Instance;
			player.DialogClose(dialog, 0);
			player.Dialog(player, dialog, new DialogArgs(npc));
		}

		/// <summary>A bout for `bet` from the pack: each pull both roll up to their strength (the resident's is armRivalStr), the stronger pull moves the hands one
		/// notch; three notches win, double the stake back; no winner after armRounds pulls returns it. False when no bout was had.</summary>
		public static bool Wrestle(Player player, NPC npc, int bet) {
			Item gem = Residents.GemOf(npc);
			if (player.IsGM || (gem == null)) {
				return false;
			}
			if (Recently(player, ArmAtKey(gem), Settings.armMinutes)) {
				npc.Say("Ruka me jeste boli. Pozdeji.");
				return false;
			}
			long now = Errands.Now.Ticks;
			int count = 0;
			if ((player.GetTag(armHourTK) is long since) && ((now - since) < TimeSpan.TicksPerHour)) {
				count = (player.GetTag(armCountTK) is int c) ? c : 0;
			} else {
				player.SetTag(armHourTK, now);
				player.SetTag(armCountTK, 0);
			}
			if (count >= Settings.armPerHour) {
				npc.Say("Uz nikdo nechce. Prijd za hodinu.");
				return false;
			}
			if (!GoldUtils.Pay(player, bet, PaySource.Pack, "")) {
				npc.Say("Na takovou sazku nemas.");
				return false;
			}
			player.SetTag(armCountTK, count + 1);
			player.SetTag(ArmAtKey(gem), now);
			int mine = Math.Max(1, (int) player.Str), theirs = Math.Max(1, Settings.armRivalStr);
			int notch = 0, pulls = 0;
			while ((Math.Abs(notch) < Pulls) && (pulls < Settings.armRounds)) {
				pulls++;
				int a = Roll(mine), b = Roll(theirs);
				if (a > b) {
					notch++;
				} else if (b > a) {
					notch--;
				}
			}
			npc.Say("Ruce na stul... ted!");
			if (notch >= Pulls) {
				GoldUtils.Grant(player, Double(bet));
				npc.Say(ErrandContext.Pick("Au! Mas silu jako medved.", "Tos me polozil.", "Pristi kolo bude moje.") + " Tvoje vyhra, " + Double(bet) + " zlatych.");
			} else if (notch <= -Pulls) {
				npc.Say(ErrandContext.Pick("Ha! Na me nemas.", "Jeste trochu potrenuj.", "Ruka jako proutek.") + " Sazka je moje.");
			} else {
				GoldUtils.Grant(player, (uint) bet);
				npc.Say("Ani jeden nepovoli. Remiza, sazka se vraci.");
			}
			return true;
		}
		#endregion

		#region The drinking contest
		/// <summary>Who drinks against the player: one of the venue's patrons (or grunts) on hand, else a well-known toper.</summary>
		internal static NPC Rival(CityVenue venue, out string name) {
			name = (venue != null) && (venue.VenueType.Key == "messhall") ? "Stary Gorbuk" : "Bolger Sudovec";
			if (venue == null) {
				return null;
			}
			foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
				string role = Residents.RoleKeyOf(gem);
				NPC body = Residents.BodyOf(gem);
				if (((role == "patron") || (role == "grunt")) && (body != null) && !body.IsDeleted && !body.Flag_Dead && (VenueRegistry.At(body) == venue)) {
					name = Residents.NameOf(gem);
					return body;
				}
			}
			return null;
		}

		/// <summary>How well one keeps up: the player's Str and Vit; any rival drinkRivalToughness.</summary>
		private static int Toughness(Character ch) {
			if (ch is Player p) {
				return (p.Str + p.Vit) / 2;
			}
			return Settings.drinkRivalToughness;
		}

		private static int HoldChance(int round, int toughness) {
			return Math.Max(5, Math.Min(95, 100 - (round * Settings.drinkRoundPenalty) + ((toughness - 50) / 2)));
		}

		/// <summary>The contest for drinkStake: each round both drink, and each keeps up on a roll under their chance; the first to
		/// fall loses, both falling together is a draw (stake back). The winner takes double the stake back; a player who falls
		/// ends under the table with no stamina.</summary>
		internal static void Drink(Player player, NPC host, Item gem) {
			if (player.IsGM) {
				host.Say("GM nepije o sazku. Za nic by neplatil.");
				return;
			}
			if (Recently(player, drinkAtTK, Settings.drinkMinutes)) {
				host.Say("Ty jeste nevystrizlivel. Pozdeji.");
				return;
			}
			int stake = Math.Max(0, Settings.drinkStake);
			if (!GoldUtils.Pay(player, stake, PaySource.Pack, "")) {
				host.Say("Bez sazky se nesoutezi. Stoji to " + stake + " zlatych.");
				return;
			}
			player.SetTag(drinkAtTK, Errands.Now.Ticks);
			NPC rival = Rival(Residents.WorkplaceOf(gem), out string name);
			int mine = Toughness(player), theirs = Toughness(rival);
			int round = 0;
			bool iHold = true, theyHold = true;
			while (iHold && theyHold && (round < 20)) {
				round++;
				iHold = Roll(100) < HoldChance(round, mine);
				theyHold = Roll(100) < HoldChance(round, theirs);
			}
			host.Say(name + " a " + player.Name + " se pousteji do piti. Korbel za korbelem...");
			if (iHold == theyHold) {
				GoldUtils.Grant(player, (uint) stake);
				host.Say("Po " + round + ". korbeli padli oba naraz. Remiza, sazka se vraci.");
			} else if (iHold) {
				GoldUtils.Grant(player, Double(stake));
				host.Say("Po " + round + ". korbeli je " + name + " pod stolem! Vitez bere " + Double(stake) + " zlatych.");
				rival?.Say(ErrandContext.Pick("Hik... jeste jeden...", "Kde... kde je stul?", "Ty pijes jako trpaslik."));
			} else {
				player.Stam = 0;
				host.Say("Po " + round + ". korbeli je " + player.Name + " pod stolem. Sazku bere " + name + ".");
				player.SysMessage("Svet se toci a ty koncis pod stolem.");
			}
		}
		#endregion

		#region The archery contest
		/// <summary>Who shoots against the player: one of the barracks' guards, else a nameless one.</summary>
		internal static string GuardName(CityVenue venue) {
			if (venue != null) {
				foreach (Item gem in Residents.OfVenue(venue.VenueId)) {
					if (Residents.RoleKeyOf(gem) == "guard") {
						return Residents.NameOf(gem);
					}
				}
			}
			return "Strazny";
		}

		/// <summary>Five shots each for archeryFee: the player hits on a roll of 0-999 under Archery (in tenths), the guard under
		/// archeryRival. More hits take archeryPrize, a tie returns the fee.</summary>
		internal static void ArcheryContest(Player player, NPC captain, Item gem) {
			int skill = player.GetSkill(SkillName.Archery);
			if (player.IsGM) {
				captain.Say("GM nestrili o cenu. Za ucast by neplatil.");
				return;
			}
			if (skill <= 0) {
				captain.Say("Soutez je pro lukostrelce. Nejdriv se nauc strilet.");
				return;
			}
			if (Recently(player, archeryAtTK, Settings.archeryContestMinutes)) {
				captain.Say("Dalsi soutez az pozdeji. Terce se musi opravit.");
				return;
			}
			int fee = Math.Max(0, Settings.archeryFee);
			if (!GoldUtils.Pay(player, fee, PaySource.Pack, "")) {
				captain.Say("Startovne je " + fee + " zlatych.");
				return;
			}
			player.SetTag(archeryAtTK, Errands.Now.Ticks);
			string rival = GuardName(Residents.WorkplaceOf(gem));
			int mine = 0, theirs = 0;
			for (int i = 0; i < Shots; i++) {
				if (Roll(1000) < skill) {
					mine++;
				}
				if (Roll(1000) < Settings.archeryRival) {
					theirs++;
				}
			}
			string said = player.Name + ": " + mine + " zasahu, " + rival + ": " + theirs + ". ";
			if (mine > theirs) {
				GoldUtils.Grant(player, (uint) Math.Max(0, Settings.archeryPrize));
				said += "Vitezis! Cena je tvoje: " + Settings.archeryPrize + " zlatych.";
			} else if (mine == theirs) {
				GoldUtils.Grant(player, (uint) fee);
				said += "Nerozhodne, startovne se vraci.";
			} else {
				said += "Vitezi " + rival + ". Pristi rok se snad polepsis.";
			}
			captain.Say(said);
		}
		#endregion

		#region Riddles
		/// <summary>Old riddles (folk ones, in Czech without diacritics): the riddle, then the answers taken as right.</summary>
		internal static readonly string[][] Riddles = {
			new[] { "Ma zuby, a nekouse.", "hreben" },
			new[] { "Cim vic z ni beres, tim je vetsi.", "dira", "jama" },
			new[] { "Bez nohou bezi, bez ust huci, nikdy nespi a stale lezi v posteli.", "reka", "potok" },
			new[] { "Rano chodi po ctyrech, v poledne po dvou, vecer po trech.", "clovek" },
			new[] { "Ma oko, a nevidi.", "jehla" },
			new[] { "Cim vic ji je, tim min vidis.", "tma" },
			new[] { "Patri tobe, a druzi ho pouzivaji vic nez ty.", "jmeno" },
			new[] { "Vede nahoru i dolu, a porad stoji na miste.", "schody", "cesta" },
			new[] { "Cim vic susi, tim je mokrejsi.", "rucnik", "osuska" },
			new[] { "Ma krk, a nema hlavu.", "lahev", "flaska", "dzban" },
			new[] { "Leti bez kridel, place bez oci.", "mrak", "oblak" },
			new[] { "Kdo ji dela, ten ji neprodava; kdo ji kupuje, ten ji nepotrebuje; kdo ji potrebuje, ten o ni nevi.", "rakev" }
		};

		/// <summary>When a player last made this resident a friend with a right answer.</summary>
		internal static TagKey RiddleDayKey(Item gem) {
			return Tavern.KeyFor("_game_riddle_day_", Residents.IdOf(gem));
		}

		/// <summary>An answer as compared: lower case, no diacritics, letters only.</summary>
		internal static string Plain(string text) {
			StringBuilder plain = new StringBuilder();
			foreach (char c in (text ?? "").Normalize(NormalizationForm.FormD).ToLowerInvariant()) {
				if ((c >= 'a') && (c <= 'z')) {
					plain.Append(c);
				}
			}
			return plain.ToString();
		}

		internal static void AskRiddle(Player player, NPC npc, Item gem) {
			if (Recently(player, RiddleAtTK, Settings.riddleMinutes)) {
				npc.Say("Uz jsi jednu hadal. Nech mi chvili na premysleni.");
				return;
			}
			int riddle = ((player.GetTag(RiddleTK) is int asked) && (asked >= 0) && (asked < Riddles.Length)) ? asked : Roll(Riddles.Length); //an unanswered one stays the same
			player.SetTag(RiddleTK, riddle);
			npc.Say("Hadej: " + Riddles[riddle][0]);
			D_Riddle dialog = SingletonScript<D_Riddle>.Instance;
			player.DialogClose(dialog, 0);
			player.Dialog(player, dialog, new DialogArgs(npc, riddle));
		}

		/// <summary>The player's answer to the riddle they were asked: right pays riddleExp (and the first right one in a real day
		/// a friendship point), wrong is told the answer. Either way the next riddle waits riddleMinutes. False when there was no
		/// such riddle to answer.</summary>
		public static bool Answer(Player player, NPC npc, int riddle, string answer) {
			if (!(player.GetTag(RiddleTK) is int asked) || (asked != riddle) || (riddle < 0) || (riddle >= Riddles.Length)) {
				return false;
			}
			player.RemoveTag(RiddleTK);
			player.SetTag(RiddleAtTK, Errands.Now.Ticks);
			string given = Plain(answer);
			bool right = false;
			for (int i = 1; i < Riddles[riddle].Length; i++) {
				right |= given == Riddles[riddle][i];
			}
			if (!right) {
				npc.Say("Kdepak. Je to " + Riddles[riddle][1] + ".");
				return true;
			}
			LevelingSystem.AddExperience(player, Math.Max(0, Settings.riddleExp));
			Item gem = Residents.GemOf(npc);
			if ((gem != null) && !Recently(player, RiddleDayKey(gem), 24 * 60)) {
				player.SetTag(RiddleDayKey(gem), Errands.Now.Ticks);
				Residents.AddFriendship(player, Residents.IdOf(gem), 1);
			}
			npc.Say(ErrandContext.Pick("Spravne! Ty jsi bystry.", "Uhodl jsi. Tu mas neco za odmenu.", "Presne tak.") + " (" + Settings.riddleExp + " zkusenosti)");
			return true;
		}
		#endregion
	}

	/// <summary>#1298 - arm wrestling's bets (the dice's three), a bout a click; it doesn't come back, as the resident then rests
	/// their arm. args: the resident.</summary>
	public class D_ArmWrestle : CompiledGumpDef {
		internal const int FirstBet = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC npc = (NPC) args[0];
			int[] bets = Tavern.Bets;
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 320, 110 + (bets.Length * 24), 2620);
			this.AddLabel(16, 12, 95, "Paka: " + npc.Name);
			this.AddHtml(16, 36, 288, 40, "<basefont color=#FFFFFF>Kdo dotlaci soupere ke stolu, bere dvojnasobek. Remiza vraci sazku.</basefont>", false, false);
			int y = 80;
			for (int i = 0; i < bets.Length; i++) {
				this.AddButton(16, y, 0xFA5, 0xFA7, FirstBet + i, GumpButtonType.Reply, 0);
				this.AddLabel(52, y + 2, 68, "Vsadit " + bets[i] + " zlatych");
				y += 24;
			}
			this.AddButton(16, y, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(52, y + 2, 955, "Dost.");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC npc = args[0] as NPC;
			int at = gr.PressedButton - FirstBet;
			int[] bets = Tavern.Bets;
			if ((player == null) || (npc == null) || npc.IsDeleted || (at < 0) || (at >= bets.Length) || !Errands.CanTalk(player, npc)) {
				return;
			}
			VenueAction arm = VenueActions.Get("game_arm");
			if ((arm == null) || !VenueActions.For(player, npc, Residents.GemOf(npc)).Contains(arm)) {
				return;
			}
			Games.Wrestle(player, npc, bets[at]);
		}
	}

	/// <summary>#1298 - a riddle and a line for the answer. args: the resident asking, the riddle's number.</summary>
	public class D_Riddle : CompiledGumpDef {
		internal const int AnswerButton = 1, AnswerEntry = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC npc = (NPC) args[0];
			int riddle = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 340, 170, 2620);
			this.AddLabel(16, 12, 95, "Hadanka: " + npc.Name);
			this.AddHtml(16, 36, 308, 60, "<basefont color=#FFFFFF>" + Games.Riddles[riddle][0] + "</basefont>", false, false);
			this.AddLabel(16, 102, 955, "Odpoved:");
			this.AddTextEntry(80, 102, 240, 20, 52, AnswerEntry, "");
			this.AddButton(16, 132, 0xFA5, 0xFA7, AnswerButton, GumpButtonType.Reply, 0);
			this.AddLabel(52, 134, 68, "Odpovedet");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC npc = args[0] as NPC;
			if ((player == null) || (npc == null) || npc.IsDeleted || (gr.PressedButton != AnswerButton) || !Errands.CanTalk(player, npc)) {
				return;
			}
			VenueAction riddles = VenueActions.Get("game_riddle");
			if ((riddles == null) || !VenueActions.For(player, npc, Residents.GemOf(npc)).Contains(riddles)) {
				return;
			}
			Games.Answer(player, npc, Convert.ToInt32(args[1], CultureInfo.InvariantCulture), gr.GetTextResponse(AnswerEntry));
		}
	}
}
