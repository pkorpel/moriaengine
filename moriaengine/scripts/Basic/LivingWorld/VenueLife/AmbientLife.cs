using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1262 - a city's news for its criers and its gossip: a new hero, a GM's call (.vyvolat), and later the world events.
	/// Each item for newsHours; saved with the world events (#1307).</summary>
	public static class CityNews {
		private sealed class Item {
			internal string text;
			internal long until;
		}

		private static readonly Dictionary<string, List<Item>> news = new Dictionary<string, List<Item>>(StringComparer.OrdinalIgnoreCase);

		public const int MaxPerCity = 10;

		/// <summary>A piece of news; the same text still fresh isn't added twice, and a city keeps its MaxPerCity newest.</summary>
		public static void Add(string city, string text) {
			if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(text)) {
				return;
			}
			text = text.Replace('\n', ' ').Replace('\r', ' ');
			if (!news.TryGetValue(city, out List<Item> list)) {
				list = new List<Item>();
				news[city] = list;
			}
			long now = Errands.Now.Ticks;
			list.RemoveAll(i => i.until <= now);
			if (list.Exists(i => i.text == text)) {
				return;
			}
			list.Add(new Item { text = text, until = Errands.Now.AddHours(VenueLifeSettings.instance.newsHours).Ticks });
			if (list.Count > MaxPerCity) {
				list.RemoveRange(0, list.Count - MaxPerCity);
			}
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

		/// <summary>#1307 - the news still fresh, for the world events' save: three parallel lists.</summary>
		internal static void Snapshot(List<string> cities, List<string> texts, List<long> untils) {
			long now = Errands.Now.Ticks;
			foreach (KeyValuePair<string, List<Item>> pair in news) {
				foreach (Item i in pair.Value) {
					if (i.until > now) {
						cities.Add(pair.Key);
						texts.Add(i.text);
						untils.Add(i.until);
					}
				}
			}
		}

		/// <summary>#1307 - the news back after a restart (real time: what went stale meanwhile is dropped as read).</summary>
		internal static void Restore(List<string> cities, List<string> texts, List<long> untils) {
			news.Clear();
			for (int i = 0; (i < cities.Count) && (i < texts.Count) && (i < untils.Count); i++) {
				if (!news.TryGetValue(cities[i], out List<Item> list)) {
					list = new List<Item>();
					news[cities[i]] = list;
				}
				list.Add(new Item { text = texts[i], until = untils[i] });
			}
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

	/// <summary>#1262 - the city's ambient life, from the residents' scheduler (every 20 s): barks when a player passes, two
	/// residents talking, the crier's calls (design 4.4). Only residents with a player near do anything.</summary>
	public static class AmbientLife {
		private static readonly Dictionary<string, DateTime> lastBark = new Dictionary<string, DateTime>(StringComparer.Ordinal);
		private static readonly Dictionary<string, DateTime> lastTalk = new Dictionary<string, DateTime>(StringComparer.Ordinal);
		private static readonly Dictionary<string, DateTime> lastVenueTalk = new Dictionary<string, DateTime>(StringComparer.Ordinal);
		private static readonly Dictionary<string, DateTime> lastCall = new Dictionary<string, DateTime>(StringComparer.Ordinal);
		private static readonly Dictionary<string, int> nextNews = new Dictionary<string, int>(StringComparer.Ordinal);

		/// <summary>Tests: the last bark said in a pass, and who said it (null when nobody barked).</summary>
		public static string LastBarkText, LastBarkBy;

		private static VenueLifeSettings Settings {
			get { return VenueLifeSettings.instance; }
		}

		/// <summary>A resident with a player near enough to be heard, where they stand now.</summary>
		public sealed class Present {
			public Item Gem;
			public NPC Body;
			public CityVenue Venue;
			public string Id;
		}

		public static void Tick() {
			List<Present> near = Gather();
			Barks(near, Settings.barkChance);
			Conversations(near, Settings.talkChance);
			Criers();
		}

		/// <summary>Tests: every resident may bark, talk and call again at once.</summary>
		public static void Forget() {
			lastBark.Clear();
			lastTalk.Clear();
			lastVenueTalk.Clear();
			lastCall.Clear();
			nextNews.Clear();
			LastBarkText = LastBarkBy = null;
		}

		#region Who's around
		/// <summary>The residents with a player within 12 tiles, and the venue each stands in: the cheap checks first, the
		/// venue (a walk over the venues) only for those. One pass a tick, shared by barks and conversations.</summary>
		public static List<Present> Gather() {
			List<Present> near = new List<Present>();
			foreach (Item gem in new List<Item>(Residents.AllGems)) {
				NPC npc = Residents.BodyOf(gem);
				string id = Residents.IdOf(gem);
				if (!IsUp(npc) || (id == null) || !PlayerWithin(npc, 12)) {
					continue;
				}
				CityVenue venue = VenueRegistry.At(npc);
				if (venue != null) {
					near.Add(new Present { Gem = gem, Body = npc, Venue = venue, Id = id });
				}
			}
			return near;
		}

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

		private static bool IsUp(NPC npc) {
			return (npc != null) && !npc.IsDeleted && !npc.Flag_Dead;
		}

		//a song, or a conversation the resident is in (as either side)
		private static bool IsBusy(NPC npc, HashSet<string> talking, string id) {
			return talking.Contains(id) || (npc.GetTimer(BardSongTimer.Key) != null);
		}

		//the residents in a conversation now: the timer sits on its first speaker
		private static HashSet<string> Talking(List<Present> near) {
			HashSet<string> talking = new HashSet<string>(StringComparer.Ordinal);
			foreach (Present p in near) {
				if (p.Body.GetTimer(ConversationTimer.Key) is ConversationTimer t) {
					talking.Add(t.aId ?? "");
					talking.Add(t.bId ?? "");
				}
			}
			return talking;
		}

		//a play is on: the venue listens
		private static bool IsHushed(CityVenue venue) {
			return (venue.VenueType != null) && (venue.VenueType.Key == "theatre") && (Theatre.PlayIn(venue) != null);
		}

		private static bool WithinSeconds(Dictionary<string, DateTime> last, string key, double seconds) {
			return last.TryGetValue(key, out DateTime at) && ((Errands.Now - at).TotalSeconds < seconds);
		}
		#endregion

		#region Barks
		public static int Barks(int chancePercent) {
			return Barks(Gather(), chancePercent);
		}

		/// <summary>One pass: each venue rolls barkChance once, then one of its people within barkRange of a player and not barked
		/// in barkSeconds says a line. Returns how many barked.</summary>
		public static int Barks(List<Present> near, int chancePercent) {
			LastBarkText = LastBarkBy = null;
			HashSet<string> talking = Talking(near);
			Dictionary<CityVenue, List<Present>> byVenue = ByVenue(near);
			int count = 0;
			foreach (KeyValuePair<CityVenue, List<Present>> pair in byVenue) {
				if (IsHushed(pair.Key) || (Globals.dice.Next(100) >= chancePercent)) {
					continue;
				}
				List<Present> ready = pair.Value.FindAll(p => !IsBusy(p.Body, talking, p.Id) && !WithinSeconds(lastBark, p.Id, Settings.barkSeconds) &&
					PlayerWithin(p.Body, Settings.barkRange));
				if (ready.Count == 0) {
					continue;
				}
				Present who = ready[Globals.dice.Next(ready.Count)];
				string line = BarkFor(who.Gem, pair.Key);
				if (line == null) {
					continue;
				}
				lastBark[who.Id] = Errands.Now;
				who.Body.Say(line);
				LastBarkText = line;
				LastBarkBy = who.Id;
				count++;
			}
			return count;
		}

		private static Dictionary<CityVenue, List<Present>> ByVenue(List<Present> near) {
			Dictionary<CityVenue, List<Present>> byVenue = new Dictionary<CityVenue, List<Present>>();
			foreach (Present p in near) {
				if (!byVenue.TryGetValue(p.Venue, out List<Present> list)) {
					list = new List<Present>();
					byVenue[p.Venue] = list;
				}
				list.Add(p);
			}
			return byVenue;
		}

		private static readonly Dictionary<string, string[]> roleBarks = new Dictionary<string, string[]>(StringComparer.Ordinal) {
			{ "innkeeper", new[] { "Pivo tece proudem! Pojdte dal!", "Mame volne pokoje!", "Dnes peceme kance." } },
			{ "server", new[] { "Jeste jedno? Hned to bude.", "Pozor, horka polevka!" } },
			{ "cook", new[] { "Kdo mi zase vzal naberacku?", "Polevka je skoro hotova." } },
			{ "bard", new[] { "La la la... kde jsem to skoncil?", "Pisen za minci, pribeh za dve!" } },
			{ "baker", new[] { "Cerstvy chleba! Jeste teply!", "Rohliky, kolace, chleba!" } },
			{ "smith", new[] { "*buch* *buch*", "Kdo potrebuje podkovy?", "Tohle zelezo je mekke jako maslo." } },
			{ "apprentice", new[] { "Uz zase mech...", "Mistr rika, ze jednou budu umet vic nez on." } },
			{ "merchant", new[] { "Nejlepsi zbozi v cele zemi!", "Pojdte blize, levne prodam!", "Sukno z jihu, koreni z vychodu!" } },
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
			{ "captain", new[] { "Nastoupit!", "Hlidky se stridaji za soumraku." } },
			{ "guard", new[] { "Stuj! Ah, to jsi ty.", "Klid ve meste." } },
			{ "recruit", new[] { "Kopi je tezsi, nez vypada.", "Jednou z me bude strazny." } },
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
		private static readonly string[] day = { "Krasny den.", "Prace nepocka.", "Kde jen jsou ty klice?" };
		private static readonly string[] evening = { "Konecne vecer.", "Na pivo by to chtelo.", "Den byl dlouhy." };
		private static readonly string[] night = { "*zivne*", "Uz je cas jit spat.", "Psst, vsichni spi." };
		private static readonly string[] orcs = { "Co cumis?", "Gash! Zase hlad.", "Oko vidi vsechno.", "Uruk nespi." };
		private static readonly Dictionary<Season, string[]> seasons = new Dictionary<Season, string[]> {
			{ Season.Spring, new[] { "Konecne jaro.", "Vsechno kvete." } },
			{ Season.Summer, new[] { "To je ale vedro.", "Leto je tu." } },
			{ Season.Autumn, new[] { "Listi pada.", "Brzy budou plody." } },
			{ Season.Winter, new[] { "Brr, to je zima.", "Snad uz brzy jaro." } },
		};

		/// <summary>A line for the resident: their role's while at their own workplace, else the season's or the time of day's;
		/// Mordor's orcs their own. The crier has no barks: its calls are its voice.</summary>
		public static string BarkFor(Item gem, CityVenue venue) {
			string city = (venue == null) ? null : venue.City;
			string role = Residents.RoleKeyOf(gem) ?? "";
			if (role == "crier") {
				return null;
			}
			if (Residents.IsOrcCity(city) && (Globals.dice.Next(2) == 0)) {
				return ErrandContext.Pick(orcs);
			}
			bool atWork = (venue != null) && (Residents.WorkplaceOf(gem) == venue);
			if (atWork && roleBarks.TryGetValue(role, out string[] lines) && (Globals.dice.Next(3) != 0)) {
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
		public static int Conversations(int chancePercent) {
			return Conversations(Gather(), chancePercent);
		}

		/// <summary>One pass: each venue with no talk under way and none in talkVenueSeconds rolls talkChance once, then two of its
		/// people within 2 tiles who haven't talked in talkMinutes start. Returns how many started.</summary>
		public static int Conversations(List<Present> near, int chancePercent) {
			HashSet<string> talking = Talking(near);
			int started = 0;
			foreach (KeyValuePair<CityVenue, List<Present>> pair in ByVenue(near)) {
				CityVenue venue = pair.Key;
				List<Present> people = pair.Value;
				if (IsHushed(venue) || people.Exists(p => talking.Contains(p.Id)) || WithinSeconds(lastVenueTalk, venue.VenueId ?? "", Settings.talkVenueSeconds)) {
					continue;
				}
				if (Globals.dice.Next(100) >= chancePercent) {
					continue;
				}
				bool done = false;
				for (int i = 0; (i < people.Count) && !done; i++) {
					for (int j = i + 1; (j < people.Count) && !done; j++) {
						Present a = people[i], b = people[j];
						if (IsBusy(a.Body, talking, a.Id) || IsBusy(b.Body, talking, b.Id) || (Point2D.GetSimpleDistance(a.Body, b.Body) > 2)) {
							continue;
						}
						if (Start(a.Gem, b.Gem) != null) {
							lastVenueTalk[venue.VenueId ?? ""] = Errands.Now;
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
			timer.Step();
			if (!timer.Done) { //only a talk that goes on keeps a timer: one left without a due time would hold the venue for good
				int seconds = Math.Max(1, Settings.talkLineSeconds);
				first.AddTimer(ConversationTimer.Key, timer);
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
			{ Season.Winter, new[] { "Ta zima snad nikdy neskonci.", "Drevo uz mi dochazi.", "U mne je teplo, stav se.", "To se mi hodi, diky." } },
		};

		/// <summary>Four lines, A, B, A, B: the season, the city's news, or a third resident (never B, who'd hear about themselves).</summary>
		internal static string[] Script(Item a, Item b, string city) {
			List<string> news = CityNews.Of(city);
			int topic = Globals.dice.Next(3);
			if ((topic == 1) || (news.Count > 0 && (Globals.dice.Next(2) == 0))) {
				string item = (news.Count > 0) ? news[news.Count - 1] : Tavern.Lore(city);
				return new[] { BasicErrands.Sex(a, "Slysel", "Slysela") + " jsi, co se povida?", "Ne, povidej.", item, ErrandContext.Pick(reactions) };
			}
			if (topic == 2) {
				string bName = Residents.NameOf(b);
				for (int tries = 0; tries < 4; tries++) {
					string about = Tavern.AboutSomeone(a, city);
					if ((about != null) && !about.Contains(bName)) {
						return new[] { "Vis, co se rika?", "Povidej.", about, ErrandContext.Pick(reactions) };
					}
				}
			}
			return seasonTalk.TryGetValue(LightAndWeather.CurrentSeason, out string[] talk) ? talk : seasonTalk[Season.Summer];
		}
		#endregion

		#region The crier
		/// <summary>One pass: each market's crier, standing at the market with a player within 18 tiles, calls every crierMinutes:
		/// the city's news in turn, or market cries when there is none. Returns how many called.</summary>
		public static int Criers() {
			int called = 0;
			foreach (CityVenue venue in VenueRegistry.All) {
				if ((venue.VenueType == null) || (venue.VenueType.Key != "market")) {
					continue;
				}
				Item gem = Theatre.GemOf(venue, "crier");
				NPC crier = (gem == null) ? null : Residents.BodyOf(gem);
				string id = (gem == null) ? null : Residents.IdOf(gem);
				if (!IsUp(crier) || (id == null) || (crier.GetTimer(ConversationTimer.Key) != null) || WithinSeconds(lastCall, id, Settings.crierMinutes * 60.0)) {
					continue;
				}
				if (!PlayerWithin(crier, 18) || (VenueRegistry.At(crier) != venue)) {
					continue; //at home or in the tavern, a crier is off duty
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
