using System;
using System.Collections.Generic;
using System.Text;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1254 - .settings "Hospoda": the tavern's room, dice and the bard (design 9.2).</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class TavernSettings : SettingsMetaCategory {
		[SavedMember]
		public static TavernSettings instance = new TavernSettings();

		[LoadingInitializer]
		public TavernSettings() {
		}

		/// <summary>A room for the night, once a real day; the booster it gives at the next login (Boosters' kinds, 0 = +25% for 1 h).</summary>
		[SaveableData]
		public int roomPrice = 100, roomBoosterKind = 0;

		/// <summary>The three bets at dice, and how many games a player has in a real hour.</summary>
		[SaveableData]
		public int betSmall = 10, betMedium = 50, betLarge = 100, gamesPerHour = 20;

		/// <summary>A tip for the bard; friendship +1 for the first one in a real day.</summary>
		[SaveableData]
		public int tip = 10;

		/// <summary>Each resident tells one rumour in this many seconds; the bard sings a line in this many.</summary>
		[SaveableData]
		public int rumourSeconds = 60, songLineSeconds = 4;
	}

	/// <summary>#1254 - the tavern's life, and the mess hall's for Mordor: rumours, a room for the night, dice, the bard's evening.
	/// Offered through VenueActions in a resident's dialog.</summary>
	public static class Tavern {
		internal static readonly string[] Drinking = { "tavern", "messhall" };
		internal static readonly TagKey RestedTK = TagKey.Acquire("_tavern_rested_");
		internal static readonly TagKey RoomPaidTK = TagKey.Acquire("_tavern_room_paid_");
		private static readonly TagKey diceHourTK = TagKey.Acquire("_tavern_dice_hour_");
		private static readonly TagKey diceCountTK = TagKey.Acquire("_tavern_dice_count_");
		private static readonly Dictionary<string, DateTime> lastRumour = new Dictionary<string, DateTime>(StringComparer.Ordinal);

		/// <summary>Tests: dice in this order before any random one.</summary>
		public static readonly Queue<int> RollOverride = new Queue<int>();

		public static void Bootstrap() {
			VenueActions.Register(new VenueAction("tavern_rumour", "Co je noveho?", Drinking,
				new[] { "innkeeper", "server", "patron", "cook", "grunt" }, null, TellRumour));
			VenueActions.Register(new VenueAction("tavern_room", "Pokoj na noc", new[] { "tavern" }, new[] { "innkeeper" }, null, RentRoom));
			VenueActions.Register(new VenueAction("tavern_dice", "Zahrajeme kostky?", Drinking, new[] { "innkeeper", "patron", "grunt" }, null, OfferDice));
			VenueActions.Register(new VenueAction("tavern_song", "Zahraj neco", new[] { "tavern" }, new[] { "bard" },
				(player, npc, gem) => (VenueClock.Shift == VenueShift.Evening) && !IsSinging(npc), Sing));
			VenueActions.Register(new VenueAction("tavern_tip", "Spropitne", new[] { "tavern" }, new[] { "bard" }, null, Tip));
		}

		private static TavernSettings Settings {
			get { return TavernSettings.instance; }
		}

		//a tag name holds only word characters (TagHolder reads saved names by \w+)
		private static TagKey KeyFor(string prefix, string residentId) {
			StringBuilder key = new StringBuilder(prefix);
			foreach (char c in residentId.ToLowerInvariant()) {
				key.Append(((c >= 'a') && (c <= 'z')) || ((c >= '0') && (c <= '9')) ? c : '_');
			}
			return TagKey.Acquire(key.ToString());
		}

		private static bool WithinADay(object ticks) {
			return (ticks is long at) && ((Errands.Now.Ticks - at) < TimeSpan.TicksPerDay);
		}

		#region Rumours
		internal static void TellRumour(Player player, NPC npc, Item gem) {
			string id = Residents.IdOf(gem);
			DateTime now = Errands.Now;
			if (lastRumour.TryGetValue(id, out DateTime last) && ((now - last).TotalSeconds < Settings.rumourSeconds)) {
				npc.Say(ErrandContext.Pick("Uz jsem ti rekl vsechno, co vim.", "Nic noveho. Zkus to pozdeji.", "Zatim nic, ale usi mam otevrene."));
				return;
			}
			lastRumour[id] = now;
			npc.Say(Rumour(gem));
		}

		/// <summary>Tests: a resident may tell the next rumour at once.</summary>
		public static void ForgetRumours() {
			lastRumour.Clear();
		}

		/// <summary>When the resident last told a rumour, if since the server started.</summary>
		public static DateTime? LastRumourOf(string residentId) {
			return lastRumour.TryGetValue(residentId, out DateTime at) ? at : (DateTime?) null;
		}

		/// <summary>Half about one of the city's other people and where to find them, half the region's news.</summary>
		public static string Rumour(Item teller) {
			CityVenue work = Residents.WorkplaceOf(teller);
			string city = (work == null) ? null : work.City;
			if (Globals.dice.Next(2) == 0) {
				string about = AboutSomeone(teller, city);
				if (about != null) {
					return about;
				}
			}
			return Lore(city);
		}

		public static string AboutSomeone(Item teller, string city) {
			List<Item> others = new List<Item>();
			foreach (Item gem in Residents.AllGems) {
				CityVenue work = Residents.WorkplaceOf(gem);
				if ((gem != teller) && (work != null) && string.Equals(work.City, city, StringComparison.OrdinalIgnoreCase)) {
					others.Add(gem);
				}
			}
			if (others.Count == 0) {
				return null;
			}
			Item g = others[Globals.dice.Next(others.Count)];
			string n = Residents.NameOf(g), place = Residents.WorkplaceOf(g).Label;
			string[] traits = Residents.TraitsOf(g);
			string trait = (traits.Length == 0) ? "" : traits[Globals.dice.Next(traits.Length)];
			switch (trait) {
				case "spolecensky":
					return n + " zna snad kazdeho ve meste. Kdyz nekoho hledas, zeptej se " + BasicErrands.Sex(g, "ho", "ji") + ".";
				case "zbozny":
					return n + " chodi kazdy vecer do chramu. Prej se modli za nekoho, kdo odesel.";
				case "morous":
					return n + " je " + BasicErrands.Sex(g, "bruclavy", "bruclava") + ", ale srdce ma dobre.";
				case "uzvaneny":
					return n + " mluvi, i kdyz neni s kym. Jestli spechas, nesedej si k " + BasicErrands.Sex(g, "nemu", "ni") + ".";
				case "lakomy":
					return n + " by " + BasicErrands.Sex(g, "prodal", "prodala") + " i vlastni babicku. Dej si pozor, az s " +
						BasicErrands.Sex(g, "nim", "ni") + " budes jednat.";
				case "romanticky":
					return "Rika se, ze " + n + " je " + BasicErrands.Sex(g, "zamilovany", "zamilovana") + ". Do koho, to nikdo nevi.";
				case "zvedavy":
					return n + " vi o vsem, co se ve meste sustne. Pred " + BasicErrands.Sex(g, "nim", "ni") + " neni tajemstvi.";
				case "pracovity":
					return n + " dre od rana do noci. Najdes " + BasicErrands.Sex(g, "ho", "ji") + " tady: " + place + ".";
				default:
					return n + " prej shani nekoho, kdo by " + BasicErrands.Sex(g, "mu", "ji") + " pomohl. Najdes " +
						BasicErrands.Sex(g, "ho", "ji") + " tady: " + place + ".";
			}
		}

		private static readonly string[] commonLore = {
			"Na cestach je ted nebezpecno, skreti prepadavaji kupce.",
			"Ceny zeleza zase stoupaji. Kovari si nestezuji.",
			"Prej nekdo videl v horach obrovsky stin. Mozna drak, mozna jen mrak.",
			"Kupci z vychodu nosi divne zpravy. Nikdo jim nechce verit.",
			"V lesich se prej ztraceji lovci. Ja bych tam nechodil.",
		};
		private static readonly string[] gondorLore = {
			"Straz na hradbach se zdvojnasobila. Z vychodu je videt kour.",
			"Prej se vratil posel z Osgiliathu a nesl zle zpravy.",
			"Bily strom na nadvori zase nevykvetl. Starci rikaji, ze az se vrati kral...",
			"Lode z Pelargiru meskaji. Rika se, ze za tim jsou korzari.",
		};
		private static readonly string[] rohanLore = {
			"Jezdci hlasi skurety na Zapadni marce.",
			"Na plani se toulaji divoci kone. Kdo je chyti, zbohatne.",
			"V Helmove zlebu prej opravuji hradby. Na neco se chystaji.",
		};
		private static readonly string[] shireLore = {
			"Letosni urodu dyni bude nejvetsi za posledni roky.",
			"U Brandyvine videli cizince v cernych platech. Nikdo nevi, co hledaji.",
			"Na tom kopci za mlynem prej nekdo zakopal truhlu.",
		};
		private static readonly string[] mordorLore = {
			"Oko se diva na zapad. Brzy pochodujeme.",
			"Nazgulove prej hledaji neco ztraceneho. Nechtel bych byt v kuzi toho, kdo to ma.",
			"V jamach Nurnu je malo otroku a dozorci zuri.",
			"Kovarny jedou dnem i noci. Kdo nekuje, ten nese.",
		};
		private static readonly string[] haradLore = {
			"Karavany z jihu privezly olifanty.",
			"Na trhu prodavaji koreni za cenu zlata.",
			"V pousti zase nekdo nasel stary chram, napul zavaty piskem.",
		};
		private static readonly string[] umbarLore = {
			"Korzari se vratili s plnymi lodemi.",
			"V pristavu kotvi cerna lod bez vlajky. Nikdo z ni nevystoupil.",
			"Kapitani se hadaji o kotviste. Brzy bude tect krev.",
		};

		public static string Lore(string city) {
			string[] local = null;
			switch ((city ?? "").ToLowerInvariant()) {
				case "minas tirith":
					local = gondorLore;
					break;
				case "edoras":
					local = rohanLore;
					break;
				case "hobitin":
					local = shireLore;
					break;
				case "barad-dur":
					local = mordorLore;
					break;
				case "harad":
					local = haradLore;
					break;
				case "umbar":
					local = umbarLore;
					break;
			}
			string[] pool = ((local == null) || (Globals.dice.Next(3) == 0)) ? commonLore : local;
			return pool[Globals.dice.Next(pool.Length)];
		}
		#endregion

		#region A room for the night
		internal static void RentRoom(Player player, NPC npc, Item gem) {
			if (player.GetTag(RestedTK) != null) {
				npc.Say("Pokoj uz mas zaplaceny. Vyspi se a prijd zase.");
				return;
			}
			if (WithinADay(player.GetTag(RoomPaidTK))) {
				npc.Say("Dnes uz mas noc u nas za sebou. Prijd zitra.");
				return;
			}
			int price = Settings.roomPrice;
			if (!GoldUtils.Pay(player, price, PaySource.Usual, "")) {
				npc.Say("Pokoj stoji " + price + " zlatych. Tolik u sebe nemas.");
				return;
			}
			player.SetTag(RoomPaidTK, Errands.Now.Ticks);
			player.SetTag(RestedTK, 1);
			npc.Say("Pokoj je tvuj. Postel je cista a nikdo te nebude rusit.");
			player.SysMessage("Az se priste prihlasis, budes odpocaty: dostanes booster zkusenosti.");
		}

		/// <summary>Player.On_LogIn: a night in a tavern's room gives a stored booster.</summary>
		public static void OnLogin(Player player) {
			if (player.GetTag(RestedTK) == null) {
				return;
			}
			player.RemoveTag(RestedTK);
			int kind = Settings.roomBoosterKind;
			if ((kind < 0) || (kind >= Boosters.Kinds.Length)) {
				return;
			}
			Boosters.Add(player, kind);
			player.SysMessage("Noc v hostinci ti prospela. Mezi boostery mas novy: " + Boosters.Kinds[kind].Description + ".");
		}
		#endregion

		#region Dice
		internal static void OfferDice(Player player, NPC npc, Item gem) {
			if (player.IsGM) {
				npc.Say("S GMem nehraju. Ty bys vyhral vzdycky.");
				return;
			}
			D_TavernDice dice = SingletonScript<D_TavernDice>.Instance;
			player.DialogClose(dice, 0);
			player.Dialog(player, dice, new DialogArgs(npc));
		}

		private static int Roll() {
			return (RollOverride.Count > 0) ? RollOverride.Dequeue() : Globals.dice.Next(1, 7);
		}

		/// <summary>2d6 against the resident's 2d6: the higher sum takes double the stake back, a tie returns it. The stake comes
		/// from the pack. False when no game was played.</summary>
		public static bool Play(Player player, NPC npc, int bet) {
			if (player.IsGM) {
				return false;
			}
			long now = Errands.Now.Ticks;
			int count = 0;
			if ((player.GetTag(diceHourTK) is long since) && ((now - since) < TimeSpan.TicksPerHour)) {
				count = (player.GetTag(diceCountTK) is int c) ? c : 0;
			} else {
				player.SetTag(diceHourTK, now);
			}
			if (count >= Settings.gamesPerHour) {
				npc.Say("Dost bylo, nechci te obrat o vsechno. Prijd za hodinu.");
				return false;
			}
			if (!GoldUtils.Pay(player, bet, PaySource.Pack, "")) {
				npc.Say("Na takovou sazku nemas.");
				return false;
			}
			player.SetTag(diceCountTK, count + 1);
			int a = Roll(), b = Roll(), c1 = Roll(), c2 = Roll();
			string said = "Ty mas " + a + " a " + b + ", ja " + c1 + " a " + c2 + ". ";
			if ((a + b) > (c1 + c2)) {
				GoldUtils.Grant(player, (uint) (bet * 2));
				said += "Tvoje vyhra, " + (bet * 2) + " zlatych.";
			} else if ((a + b) == (c1 + c2)) {
				GoldUtils.Grant(player, (uint) bet);
				said += "Remiza, sazka se vraci.";
			} else {
				said += ErrandContext.Pick("Smula, beru si to.", "Stesti preje pripravenym.", "Dnes neni tvuj den.");
			}
			npc.Say(said);
			return true;
		}

		internal static int[] Bets {
			get { return new[] { Settings.betSmall, Settings.betMedium, Settings.betLarge }; }
		}
		#endregion

		#region The bard
		private static readonly string[][] songs = {
			new[] {
				"Pres plane vitr kone zene,",
				"zlata hriva v slunci plane,",
				"kdo jednou jel, ten nezapomene",
				"na cestu, kde srdce zustane.",
			},
			new[] {
				"Nalej, hostinsky, dalsi dzban,",
				"at zpiva krcma, at zpiva pan,",
				"nez rano vstane slunce zas,",
				"jeste jednu pro kazdeho z nas!",
			},
			new[] {
				"V hlubinach hor mec davny spi,",
				"jej kral kdys nosil v slavne dni.",
				"Az statecna ruka vezme ho zas,",
				"slava se vrati v nas cas.",
				"Do te doby, pratele, pijme",
				"a na krale v pisni nezapomenme.",
			},
		};

		public static bool IsSinging(NPC npc) {
			return npc.GetTimer(BardSongTimer.Key) != null;
		}

		/// <summary>A song a line at a time overhead; the first now.</summary>
		internal static void Sing(Player player, NPC npc, Item gem) {
			if (IsSinging(npc)) {
				npc.Say("Uz hraju, poslouchej.");
				return;
			}
			BardSongTimer timer = new BardSongTimer { song = Globals.dice.Next(songs.Length), line = 0 };
			timer.SingNext(npc);
			if (!timer.Done) {
				int seconds = Math.Max(1, Settings.songLineSeconds);
				npc.AddTimer(BardSongTimer.Key, timer);
				timer.DueInSeconds = seconds;
				timer.PeriodInSeconds = seconds; //periodic: a BoundTimer due no more is taken off before its OnTimeout runs
			}
		}

		internal static string SongLine(int song, int line) {
			string[] lines = songs[song % songs.Length];
			return (line < lines.Length) ? lines[line] : null;
		}

		internal static void Tip(Player player, NPC npc, Item gem) {
			int tip = Settings.tip;
			if (!GoldUtils.Pay(player, tip, PaySource.Pack, "")) {
				npc.Say("Diky za dobrou vuli, ale kapsy mas prazdne.");
				return;
			}
			string id = Residents.IdOf(gem);
			TagKey key = KeyFor("_tavern_tip_", id);
			if (!WithinADay(player.GetTag(key))) {
				player.SetTag(key, Errands.Now.Ticks);
				Residents.AddFriendship(player, id, 1);
			}
			npc.Say(ErrandContext.Pick("Diky! Dalsi pisen je pro tebe.", "At ti pivo chutna!", "Dekuju, at se ti dari."));
		}
		#endregion
	}

	/// <summary>#1254 - the bard's song, a line every songLineSeconds; it deletes itself with the song's last line.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BardSongTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_bard_song_");

		[SaveableData, CopyableData]
		public int song, line;

		[LoadingInitializer, DeepCopyImplementation]
		public BardSongTimer() {
		}

		internal bool Done {
			get { return Tavern.SongLine(this.song, this.line) == null; }
		}

		internal void SingNext(NPC npc) {
			string text = Tavern.SongLine(this.song, this.line);
			if (text != null) {
				npc.Say("~ " + text + " ~");
				this.line++;
			}
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC npc = cont as NPC;
			if ((npc != null) && !npc.Flag_Dead) {
				this.SingNext(npc);
			}
			if ((npc == null) || npc.Flag_Dead || this.Done) {
				this.Delete();
			}
		}
	}

	/// <summary>#1254 - dice at the tavern: three bets (Tavern.Bets), a game a click; it comes back after each game.
	/// args: the resident playing.</summary>
	public class D_TavernDice : CompiledGumpDef {
		internal const int FirstBet = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			NPC npc = (NPC) args[0];
			int[] bets = Tavern.Bets;
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 320, 110 + (bets.Length * 24), 2620);
			this.AddLabel(16, 12, 95, "Kostky s " + npc.Name);
			this.AddHtml(16, 36, 288, 40, "<basefont color=#FFFFFF>Dve kostky proti dvema. Vyssi soucet bere dvojnasobek, remiza vraci sazku.</basefont>", false, false);
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
			Item gem = Residents.GemOf(npc);
			VenueAction dice = VenueActions.Get("tavern_dice");
			if ((dice == null) || !VenueActions.For(player, npc, gem).Contains(dice)) {
				return;
			}
			if (Tavern.Play(player, npc, bets[at])) {
				player.Dialog(player, this, new DialogArgs(npc));
			}
		}
	}
}
