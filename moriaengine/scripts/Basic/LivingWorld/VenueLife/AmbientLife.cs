using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1262 - a city's news for its criers and its gossip: a new hero, a GM's call (.vyvolat), and later the world events.
	/// Kept in memory, each item for newsHours; a restart starts the feed afresh.</summary>
	public static class CityNews {
		private sealed class Item {
			internal string text;
			internal long until;
		}

		private static readonly Dictionary<string, List<Item>> news = new Dictionary<string, List<Item>>(StringComparer.OrdinalIgnoreCase);

		public static void Add(string city, string text) {
			if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(text)) {
				return;
			}
			if (!news.TryGetValue(city, out List<Item> list)) {
				list = new List<Item>();
				news[city] = list;
			}
			list.Add(new Item { text = text, until = Errands.Now.AddHours(VenueLifeSettings.instance.newsHours).Ticks });
		}

		/// <summary>The city's news still fresh, oldest first.</summary>
		public static List<string> Of(string city) {
			List<string> texts = new List<string>();
			if ((city != null) && news.TryGetValue(city, out List<Item> list)) {
				long now = Errands.Now.Ticks;
				list.RemoveAll(i => i.until <= now);
				foreach (Item i in list) {
					texts.Add(i.text);
				}
			}
			return texts;
		}

		/// <summary>Tests: no news anywhere.</summary>
		public static void Clear() {
			news.Clear();
		}

		/// <summary>.vyvolat text: a GM's news for the criers of the city they stand in.</summary>
		[SteamFunction]
		public static void Vyvolat(Character self, ScriptArgs sa) {
			string text = (sa == null) ? "" : sa.Args.Trim().Trim('"').Trim();
			string city = Reputation.CityAt(self);
			if (city == null) {
				self.SysMessage("Nestojis v zadnem meste.");
				return;
			}
			if (text.Length == 0) {
				self.SysMessage("Pouziti: .vyvolat \"text\" (vyvolavaci ve meste, kde stojis, to budou hlasat " + VenueLifeSettings.instance.newsHours + " hodin).");
				return;
			}
			Add(city, text);
			self.SysMessage("Vyvolavaci (" + city + ") to budou hlasat.");
		}
	}

	/// <summary>#1262 - the city's ambient life, from the residents' scheduler (every 20 s, sleeping with the city): barks when a
	/// player passes, two residents talking, the crier's calls (design 4.4).</summary>
	public static class AmbientLife {
		private static readonly Dictionary<string, DateTime> lastBark = new Dictionary<string, DateTime>(StringComparer.Ordinal);
		private static readonly Dictionary<string, DateTime> lastTalk = new Dictionary<string, DateTime>(StringComparer.Ordinal);
		private static readonly Dictionary<string, DateTime> lastCall = new Dictionary<string, DateTime>(StringComparer.Ordinal);
		private static readonly Dictionary<string, int> nextNews = new Dictionary<string, int>(StringComparer.Ordinal);

		/// <summary>Tests: the last bark said, and who said it.</summary>
		public static string LastBarkText, LastBarkBy;

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		public static void Tick() {
			Barks(Settings.barkChance);
			Conversations(Settings.talkChance);
			Criers();
		}

		/// <summary>Tests: every resident may bark, talk and call again at once.</summary>
		public static void Forget() {
			lastBark.Clear();
			lastTalk.Clear();
			lastCall.Clear();
			nextNews.Clear();
			LastBarkText = LastBarkBy = null;
		}

		#region Who's around
		private static bool PlayerWithin(NPC npc, int range) {
			Map map = npc.GetMap();
			if (map == null) {
				return false;
			}
			foreach (AbstractCharacter ch in map.GetPlayersInRange(npc.X, npc.Y, range)) {
				if (!ch.Flag_Disconnected && !((ch is Character c) && c.Flag_Dead)) {
					return true;
				}
			}
			return false;
		}

		//busy saying something else: a song, a play, a conversation
		private static bool IsBusy(NPC npc) {
			return (npc.GetTimer(ConversationTimer.Key) != null) || (npc.GetTimer(BardSongTimer.Key) != null) || (npc.GetTimer(TheatrePlayTimer.Key) != null);
		}

		private static bool IsUp(NPC npc) {
			return (npc != null) && !npc.IsDeleted && !npc.Flag_Dead;
		}

		private static bool WithinSeconds(Dictionary<string, DateTime> last, string key, double seconds) {
			return last.TryGetValue(key, out DateTime at) && ((Errands.Now - at).TotalSeconds < seconds);
		}
		#endregion

		#region Barks
		/// <summary>One pass: each venue lets at most one of its people bark at a passing player; a resident barks at most once in
		/// barkSeconds. Returns how many barked.</summary>
		public static int Barks(int chancePercent) {
			HashSet<CityVenue> spoke = new HashSet<CityVenue>();
			int count = 0;
			foreach (Item gem in new List<Item>(Residents.AllGems)) {
				NPC npc = Residents.BodyOf(gem);
				string id = Residents.IdOf(gem);
				if (!IsUp(npc) || (id == null) || IsBusy(npc)) {
					continue;
				}
				CityVenue venue = VenueRegistry.At(npc);
				if ((venue == null) || spoke.Contains(venue) || WithinSeconds(lastBark, id, Settings.barkSeconds) || !PlayerWithin(npc, Settings.barkRange)) {
					continue;
				}
				if (Globals.dice.Next(100) >= chancePercent) {
					continue;
				}
				string line = BarkFor(gem, venue);
				if (line == null) {
					continue;
				}
				spoke.Add(venue);
				lastBark[id] = Errands.Now;
				npc.Say(line);
				LastBarkText = line;
				LastBarkBy = id;
				count++;
			}
			return count;
		}

		private static readonly Dictionary<string, string[]> roleBarks = new Dictionary<string, string[]>(StringComparer.Ordinal) {
			{ "innkeeper", new[] { "Pivo tece proudem! Pojdte dal!", "Mame volne pokoje!", "Dnes peceme kance." } },
			{ "server", new[] { "Jeste jedno? Hned to bude.", "Pozor, horka polevka!" } },
			{ "cook", new[] { "Kdo mi zase vzal nabeckar?", "Polevka je skoro hotova." } },
			{ "bard", new[] { "La la la... kde jsem to skoncil?", "Pisen za minci, pribeh za dve!" } },
			{ "baker", new[] { "Cerstvy chleba! Jeste teply!", "Rohliky, kolace, chleba!" } },
			{ "smith", new[] { "*buch* *buch*", "Kdo potrebuje podkovy?", "Tohle zelezo je mekke jako maslo." } },
			{ "apprentice", new[] { "Uz zase mech...", "Mistr rika, ze z me jednou neco bude." } },
			{ "merchant", new[] { "Nejlepsi zbozi v cele zemi!", "Pojdte blize, levne prodam!", "Sukno z jihu, koreni z vychodu!" } },
			{ "crier", new[] { "Slyste, slyste!" } },
			{ "harbourmaster", new[] { "Kotviste je plne, at pockaji!", "Kdo zase nechal lano na mole?" } },
			{ "docker", new[] { "Hej rup!", "Tahle bedna vazi jako kun." } },
			{ "fisher", new[] { "Dneska neberou.", "Cerstve ryby!" } },
			{ "sailor", new[] { "More vola!", "Na sousi je mi zle." } },
			{ "stablemaster", new[] { "Klid, holka, klid.", "Seno je drahe jako zlato." } },
			{ "groom", new[] { "Prr! Stuj!", "Tenhle hrebec kope." } },
			{ "priest", new[] { "Svetlo s vami.", "Chram je otevreny vsem." } },
			{ "acolyte", new[] { "Svice musi horet.", "Ticho, prosim, tady se modli." } },
			{ "healer", new[] { "Kdo dalsi?", "Klid, to se zahoji." } },
			{ "patient", new[] { "Au...", "Uz je mi lip, opravdu." } },
			{ "quartermaster", new[] { "Kdo podepise prevzeti?", "Zase chybi tri kopi." } },
			{ "armourer", new[] { "Tohle brneni vydrzi vsechno.", "Neopirej se o ty stity." } },
			{ "captain", new[] { "Do radu!", "Hlidky se stridaji za soumraku." } },
			{ "guard", new[] { "Stuj! Ah, to jsi ty.", "Klid ve meste." } },
			{ "recruit", new[] { "Kopi je tezsi, nez vypada.", "Uz brzo budu straznym." } },
			{ "director", new[] { "Jeste jednou, od zacatku!", "Kde je zase ten herec?" } },
			{ "actor", new[] { "Mam to... mam to... ne, nemam.", "Vecer hrajeme, prijdte!" } },
			{ "attendant", new[] { "Horka koupel za par minci!", "Rucniky jsou cerstve." } },
			{ "stoker", new[] { "Drevo! Potrebuju drevo!", "Pec hori, voda se hreje." } },
			{ "brewer", new[] { "Tohle pivo bude stat za to.", "Slad voni po cele ulici." } },
			{ "librarian", new[] { "Pst!", "Knihy vracet vcas, prosim." } },
			{ "grunt", new[] { "Hlad! Kdy bude zradlo?", "Ten elfi chleba je hnusny." } },
			{ "pitmaster", new[] { "Krev a zlato! Sazky!", "Dnes se bude bojovat." } },
			{ "overseer", new[] { "Pracovat, cervi!", "Zadne flakani!" } },
			{ "slave", new[] { "*tise vzdycha*", "Jeste chvili..." } },
		};

		private static readonly string[] morning = { "Dobre rano.", "Uz je zase rano?", "Ranni ptace dal doskace." };
		private static readonly string[] day = { "Krasny den.", "Prace nepocka.", "Kde jsem jen nechal ty klice?" };
		private static readonly string[] evening = { "Konecne vecer.", "Na pivo by to chtelo.", "Den byl dlouhy." };
		private static readonly string[] night = { "*zivne*", "Uz bych mel spat.", "Psst, vsichni spi." };
		private static readonly string[] orcs = { "Co cumis?", "Gash! Zase hlad.", "Oko vidi vsechno.", "Uruk nespi." };
		private static readonly Dictionary<Season, string[]> seasons = new Dictionary<Season, string[]> {
			{ Season.Spring, new[] { "Konecne jaro.", "Vsechno kvete." } },
			{ Season.Summer, new[] { "To je ale vedro.", "Leto je tu." } },
			{ Season.Autumn, new[] { "Listi pada.", "Brzy budou plody." } },
			{ Season.Winter, new[] { "Brr, to je zima.", "Snad uz brzy jaro." } },
		};

		/// <summary>A line for the resident: their role's, else the time of day's or the season's; Mordor's orcs their own.</summary>
		public static string BarkFor(Item gem, CityVenue venue) {
			string city = (venue == null) ? null : venue.City;
			if (Residents.IsOrcCity(city) && (Globals.dice.Next(2) == 0)) {
				return ErrandContext.Pick(orcs);
			}
			if (roleBarks.TryGetValue(Residents.RoleKeyOf(gem) ?? "", out string[] lines) && (Globals.dice.Next(3) != 0)) {
				return ErrandContext.Pick(lines);
			}
			if ((Globals.dice.Next(3) == 0) && seasons.TryGetValue(LightAndWeather.CurrentSeason, out string[] season)) {
				return ErrandContext.Pick(season);
			}
			switch (VenueClock.Shift) {
				case VenueShift.Morning: return ErrandContext.Pick(morning);
				case VenueShift.Day: return ErrandContext.Pick(day);
				case VenueShift.Evening: return ErrandContext.Pick(evening);
				default: return ErrandContext.Pick(night);
			}
		}
		#endregion

		#region Conversations
		/// <summary>One pass: in each venue with a player near and no talk under way, two of its residents within 2 tiles may start
		/// one. Returns how many started.</summary>
		public static int Conversations(int chancePercent) {
			Dictionary<CityVenue, List<KeyValuePair<Item, NPC>>> byVenue = new Dictionary<CityVenue, List<KeyValuePair<Item, NPC>>>();
			foreach (Item gem in new List<Item>(Residents.AllGems)) {
				NPC npc = Residents.BodyOf(gem);
				CityVenue venue = IsUp(npc) ? VenueRegistry.At(npc) : null;
				if (venue == null) {
					continue;
				}
				if (!byVenue.TryGetValue(venue, out List<KeyValuePair<Item, NPC>> list)) {
					list = new List<KeyValuePair<Item, NPC>>();
					byVenue[venue] = list;
				}
				list.Add(new KeyValuePair<Item, NPC>(gem, npc));
			}
			int started = 0;
			foreach (KeyValuePair<CityVenue, List<KeyValuePair<Item, NPC>>> pair in byVenue) {
				List<KeyValuePair<Item, NPC>> people = pair.Value;
				if (people.Exists(p => p.Value.GetTimer(ConversationTimer.Key) != null)) {
					continue; //one at a time in a venue
				}
				bool done = false;
				for (int i = 0; (i < people.Count) && !done; i++) {
					for (int j = i + 1; (j < people.Count) && !done; j++) {
						NPC a = people[i].Value, b = people[j].Value;
						if (IsBusy(a) || IsBusy(b) || (Point2D.GetSimpleDistance(a, b) > 2) || !PlayerWithin(a, 12)) {
							continue;
						}
						if (Globals.dice.Next(100) >= chancePercent) {
							continue;
						}
						if (Start(people[i].Key, people[j].Key) != null) {
							started++;
							done = true;
						}
					}
				}
			}
			return started;
		}

		private static string PairKey(Item a, Item b) {
			string x = Residents.IdOf(a), y = Residents.IdOf(b);
			return (string.CompareOrdinal(x, y) < 0) ? x + "|" + y : y + "|" + x;
		}

		/// <summary>Two residents start talking, four lines; null when the pair talked within talkMinutes.</summary>
		public static ConversationTimer Start(Item a, Item b) {
			NPC first = Residents.BodyOf(a);
			if (!IsUp(first) || !IsUp(Residents.BodyOf(b)) || (Residents.IdOf(a) == null) || (Residents.IdOf(b) == null)) {
				return null;
			}
			string key = PairKey(a, b);
			if (WithinSeconds(lastTalk, key, Settings.talkMinutes * 60.0)) {
				return null;
			}
			lastTalk[key] = Errands.Now;
			CityVenue venue = Residents.WorkplaceOf(a);
			string[] lines = Script(a, b, (venue == null) ? null : venue.City);
			ConversationTimer timer = new ConversationTimer { aId = Residents.IdOf(a), bId = Residents.IdOf(b), lines = string.Join("\n", lines), line = 0 };
			first.AddTimer(ConversationTimer.Key, timer);
			timer.Step();
			if (!timer.Done) {
				int seconds = Math.Max(1, Settings.talkLineSeconds);
				timer.DueInSeconds = seconds;
				timer.PeriodInSeconds = seconds;
			}
			return timer;
		}

		private static readonly string[] reactions = { "Kdo by to byl rekl.", "No vida.", "To se ani nedivim.", "Hlavne at se to nedoneslo dal.", "Opravdu? To snad ne." };

		private static readonly Dictionary<Season, string[]> seasonTalk = new Dictionary<Season, string[]> {
			{ Season.Spring, new[] { "Konecne trochu slunce.", "A bahno vsude kolem.", "Aspon zase roste trava.", "To jo. Kone budou radi." } },
			{ Season.Summer, new[] { "To je ale horko.", "Ani mi nemluv, studna skoro vyschla.", "Snad brzo zaprsi.", "Kez by." } },
			{ Season.Autumn, new[] { "Listi uz pada.", "A noci jsou chladne.", "Musim nasekat drevo na zimu.", "Ja taky. Pomuzeme si." } },
			{ Season.Winter, new[] { "Ta zima snad nikdy neskonci.", "Drevo uz mi dochazi.", "U mne je teplo, stav se.", "Rad prijdu." } },
		};

		/// <summary>Four lines, A, B, A, B: the season, the city's news, or a third resident.</summary>
		internal static string[] Script(Item a, Item b, string city) {
			List<string> news = CityNews.Of(city);
			int topic = Globals.dice.Next(3);
			if ((topic == 1) || (news.Count > 0 && (Globals.dice.Next(2) == 0))) {
				string item = (news.Count > 0) ? news[news.Count - 1] : Tavern.Lore(city);
				return new[] { BasicErrands.Sex(a, "Slysel", "Slysela") + " jsi, co se povida?", "Ne, povidej.", item, ErrandContext.Pick(reactions) };
			}
			if (topic == 2) {
				string about = Tavern.AboutSomeone(a, city);
				if (about != null) {
					return new[] { "Vis, co se rika?", "Povidej.", about, ErrandContext.Pick(reactions) };
				}
			}
			return seasonTalk.TryGetValue(LightAndWeather.CurrentSeason, out string[] talk) ? talk : seasonTalk[Season.Summer];
		}
		#endregion

		#region The crier
		/// <summary>One pass: each market crier with a player near calls every crierMinutes: the city's news in turn, or market
		/// cries when there is none. Returns how many called.</summary>
		public static int Criers() {
			int called = 0;
			foreach (CityVenue venue in VenueRegistry.All) {
				if ((venue.VenueType == null) || (venue.VenueType.Key != "market")) {
					continue;
				}
				Item gem = WorkErrands.SameVenue(Theatre.GemOf(venue, "merchant") ?? Theatre.GemOf(venue, "crier"), "crier");
				NPC crier = (gem == null) ? null : Residents.BodyOf(gem);
				string id = (gem == null) ? null : Residents.IdOf(gem);
				if (!IsUp(crier) || (id == null) || IsBusy(crier) || WithinSeconds(lastCall, id, Settings.crierMinutes * 60.0) || !PlayerWithin(crier, 18)) {
					continue;
				}
				lastCall[id] = Errands.Now;
				crier.Say(CallFor(id, venue.City));
				called++;
			}
			return called;
		}

		private static readonly string[] cries = {
			"Slyste, slyste! Na trhu cerstve zbozi!", "Slyste, slyste! Kupci z jihu privezli koreni!", "Slyste, slyste! Kdo hleda praci, at se pta po meste!",
			"Slyste, slyste! Brany se zaviraji o pulnoci!",
		};

		/// <summary>The crier's next call: the city's news in turn, or a market cry.</summary>
		public static string CallFor(string crierId, string city) {
			List<string> news = CityNews.Of(city);
			if (news.Count == 0) {
				return ErrandContext.Pick(cries);
			}
			nextNews.TryGetValue(crierId, out int next);
			nextNews[crierId] = next + 1;
			return "Slyste, slyste! " + news[next % news.Count];
		}
		#endregion
	}

	/// <summary>#1262 - two residents talking: a line every talkLineSeconds, A then B; it ends with the fourth line or when either is gone.</summary>
	[SaveableClass, DeepCopyableClass]
	public class ConversationTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_resident_talk_");

		[SaveableData, CopyableData]
		public string aId, bId, lines;

		[SaveableData, CopyableData]
		public int line;

		[LoadingInitializer, DeepCopyImplementation]
		public ConversationTimer() {
		}

		private string[] Lines {
			get { return (this.lines ?? "").Split('\n'); }
		}

		internal bool Done {
			get { return this.line >= this.Lines.Length; }
		}

		/// <summary>The next line; false when the talk has ended.</summary>
		public bool Step() {
			string[] all = this.Lines;
			if (this.line >= all.Length) {
				return false;
			}
			Item gem = Residents.ById(((this.line % 2) == 0) ? this.aId : this.bId);
			NPC speaker = (gem == null) ? null : Residents.BodyOf(gem);
			if ((speaker == null) || speaker.IsDeleted || speaker.Flag_Dead) {
				this.line = all.Length;
				return false;
			}
			speaker.Say(all[this.line]);
			this.line++;
			return this.line < all.Length;
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!this.Step()) {
				this.Delete();
			}
		}
	}
}
