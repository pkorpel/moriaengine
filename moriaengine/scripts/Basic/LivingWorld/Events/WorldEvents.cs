using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1284 - .settings "Udalosti": the world events (design section 6), when they start and what they pay.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class WorldEventSettings : SettingsMetaCategory {
		[SavedMember]
		public static WorldEventSettings instance = new WorldEventSettings();

		[LoadingInitializer]
		public WorldEventSettings() {
		}

		/// <summary>Off, no event starts by itself (a GM's .udalost still starts one).</summary>
		[SaveableData]
		public bool enabled = true;

		/// <summary>Real minutes at least between two events starting by themselves; after that, each minute's chance (percent) of
		/// one starting; how many may run at once.</summary>
		[SaveableData]
		public int gapMinutes = 45, startChance = 25, maxRunning = 1;

		/// <summary>Tiles from a city's centre (its spawn point) within which a player counts as there: an event starts only where
		/// someone is, and its size goes by how many are.</summary>
		[SaveableData]
		public int playerRange = 40;

		/// <summary>The orc raid: real minutes to beat it; raiders: so many, more for each player there, at most so many; how
		/// many walking steps from the centre they come from; the game hours of dusk it starts in.</summary>
		[SaveableData]
		public int raidMinutes = 15, raidBase = 3, raidPerPlayer = 2, raidMax = 12, raidSteps = 24, raidDuskFrom = 17, raidDuskTo = 21;

		/// <summary>The orc raid beaten: each defender's gold, more for each raider they killed, experience, standing in the city;
		/// the feast after it (price change in percent, real minutes). Lost: the price change (no bread), and for how long.</summary>
		[SaveableData]
		public int raidGold = 300, raidKillGold = 50, raidExp = 500, raidFavour = 20, raidWonPercent = -5, raidWonMinutes = 60, raidLostPercent = 15, raidLostMinutes = 1440;

		/// <summary>The caravan: real minutes for it to arrive; how many walking steps out it starts; its pack animals; the
		/// bandits: so many, more for each player there, at most so many.</summary>
		[SaveableData]
		public int caravanMinutes = 15, caravanSteps = 36, caravanPack = 2, banditBase = 2, banditPerPlayer = 1, banditMax = 8;

		/// <summary>The caravan arrived: each escort's gold, experience, standing in the city; tiles from the merchant an escort
		/// stands at the end; the goods cheaper (percent) for so many real minutes.</summary>
		[SaveableData]
		public int caravanGold = 250, caravanExp = 400, caravanFavour = 15, escortRange = 15, caravanPercent = -10, caravanOutcomeMinutes = 120;
	}

	/// <summary>#1284 - a kind of world event: where and when it can start, and a fresh one.</summary>
	public abstract class WorldEventKind {
		public abstract string Key { get; }
		public abstract string Name { get; }

		/// <summary>The cities it can happen in.</summary>
		public abstract bool IsFor(string city);

		/// <summary>Whether it may start by itself now (a GM's start skips this).</summary>
		public virtual bool IsTime() {
			return true;
		}

		public abstract WorldEvent Make(string city);
	}

	/// <summary>#1284 - one event under way: in memory only (a restart ends it; WorldEvents clears the NPCs it left). Its NPCs
	/// carry its id in a tag; those who fought them are its heroes.</summary>
	public abstract class WorldEvent {
		internal static readonly TagKey EventTK = TagKey.Acquire("_wev_");

		public readonly WorldEventKind Kind;
		public readonly string City, Id;
		public double StartedAt, EndsAt;
		public readonly List<NPC> Npcs = new List<NPC>();
		public readonly Dictionary<Player, int> Heroes = new Dictionary<Player, int>();
		public bool Ended, Won;

		protected WorldEvent(WorldEventKind kind, string city) {
			this.Kind = kind;
			this.City = city;
			this.Id = kind.Key + "/" + city + "/" + (++WorldEvents.serial).ToString(CultureInfo.InvariantCulture);
		}

		protected static WorldEventSettings Settings {
			get { return WorldEventSettings.instance; }
		}

		public string CityName {
			get { return this.City.Replace('_', ' '); }
		}

		/// <summary>Puts the event in the world: its NPCs, its start message. False when there's no room for it here.</summary>
		internal abstract bool Begin(int playersNear);

		/// <summary>Every WorldEvents.TickSeconds: moves things on, and ends the event (End) when it's won or lost.</summary>
		internal abstract void Tick();

		/// <summary>The journal's line about it while it runs.</summary>
		public abstract string Describe();

		/// <summary>An NPC of this event died.</summary>
		internal virtual void OnDeath(NPC dead, Character killedBy) {
		}

		protected NPC Spawn(string defname, int[] at, byte m, string name) {
			NPCDef def = ThingDef.GetByDefname(defname) as NPCDef;
			if ((def == null) || (at == null)) {
				return null;
			}
			NPC npc = (NPC) def.Create(at[0], at[1], at[2], m);
			if (name != null) {
				npc.Name = name;
			}
			npc.SetTag(EventTK, this.Id);
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			if (ai != null) {
				ai.NeverSleeps = true; //its doings matter unseen: a raid or a caravan goes on with nobody watching
			}
			this.Npcs.Add(npc);
			return npc;
		}

		/// <summary>Its NPCs still standing.</summary>
		protected int Alive(Predicate<NPC> which = null) {
			int count = 0;
			foreach (NPC npc in this.Npcs) {
				if (!npc.IsDeleted && !npc.Flag_Dead && ((which == null) || which(npc))) {
					count++;
				}
			}
			return count;
		}

		/// <summary>An NPC walks to a point and stays about it: the go order, and its home there so it doesn't walk back.</summary>
		protected static void Send(NPC npc, int[] to, byte m, int leash) {
			if ((npc == null) || npc.IsDeleted || npc.Flag_Dead || (to == null)) {
				return;
			}
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			if (ai != null) {
				ai.homeX = to[0];
				ai.homeY = to[1];
				ai.homeZ = (sbyte) to[2];
				ai.homeM = m;
				ai.hasHome = true;
				ai.WanderLeash = leash;
				ai.WakeUp();
			}
			if (Point2D.GetSimpleDistance(npc, new Point2D(to[0], to[1])) > 1) {
				npc.PetGoalPoint = new Point3D(to[0], to[1], to[2]);
			}
		}

		internal void Credit(NPC dead, Character killedBy) {
			foreach (Character fighter in LevelingSystem.FindFighters(dead, killedBy, false)) {
				if (fighter is Player player) {
					this.Heroes.TryGetValue(player, out int kills);
					this.Heroes[player] = kills + ((fighter == killedBy) ? 1 : 0);
				}
			}
		}

		/// <summary>The event is over: its NPCs go, the outcome stands.</summary>
		protected void End(bool won) {
			this.Ended = true;
			this.Won = won;
			foreach (NPC npc in this.Npcs) {
				if (!npc.IsDeleted) {
					npc.Delete();
				}
			}
		}
	}

	/// <summary>#1284 - the world events (design section 6): a scheduler on Globals starts one now and then in a pilot city where
	/// a player is, events run in memory, and their outcomes change a city's prices for a while (Reputation.PriceFor) and go to
	/// the criers (CityNews). GM command .udalost.</summary>
	public static class WorldEvents {
		public const double TickSeconds = 5;
		private const int OutcomesShown = 5;
		private static readonly TimerKey timerTK = TimerKey.Acquire("_world_events_");

		internal static int serial;
		private static readonly List<WorldEventKind> kinds = new List<WorldEventKind>();
		private static readonly List<WorldEvent> running = new List<WorldEvent>();
		private static readonly Dictionary<string, WorldEvent> byId = new Dictionary<string, WorldEvent>(StringComparer.Ordinal);
		private static readonly List<CityOutcome> outcomes = new List<CityOutcome>();
		private static double nextStartAt, nextChanceAt;
		private static bool leftoversCleared;

		/// <summary>The pilot cities (design section 4), as FlaggedRegion.City names them.</summary>
		public static readonly string[] Cities = { "Minas_Tirith", "Edoras", "Hobitin", "Barad-dur", "Harad", "Umbar" };

		private sealed class CityOutcome {
			internal string city, text;
			internal int percent;
			internal double until;
		}

		private static WorldEventSettings Settings {
			get { return WorldEventSettings.instance; }
		}

		public static void Bootstrap() {
			Register(new OrcRaidKind());
			Register(new CaravanKind());
			QuestJournalPages.Register(D_QuestJournal.SectionEvents, WorldEventsPage.Add);
		}

		public static void Register(WorldEventKind kind) {
			kinds.RemoveAll(k => k.Key == kind.Key);
			kinds.Add(kind);
		}

		public static WorldEventKind KindOf(string key) {
			return kinds.Find(k => string.Equals(k.Key, key, StringComparison.OrdinalIgnoreCase));
		}

		public static IList<WorldEvent> Running {
			get { return running.AsReadOnly(); }
		}

		public static void EnsureRunning() {
			if (!Globals.Instance.HasTimer(timerTK)) {
				WorldEventTimer timer = new WorldEventTimer();
				Globals.Instance.AddTimer(timerTK, timer);
				timer.DueInSeconds = TickSeconds;
				timer.PeriodInSeconds = TickSeconds;
			}
		}

		#region the city
		/// <summary>A city's centre: a standable spot at its main area's spawn point (a_City), or null.</summary>
		public static Point4D CentreOf(string city) {
			StaticRegion area = StaticRegion.GetByDefname("a_" + city.Replace('-', '_'));
			if (area == null) {
				List<FlaggedRegion> areas = VenueRegistry.AreasOf(city);
				area = (areas.Count > 0) ? areas[0] : null;
			}
			if (area == null) {
				return null;
			}
			Point4D p = area.P;
			int[] at = WorldSpots.NearestStandable(Map.GetMap(p.M), p.X, p.Y, 4);
			return (at == null) ? p : new Point4D(at[0], at[1], at[2], p.M);
		}

		/// <summary>The players who count as in the city now: online, alive, not GMs, near its centre.</summary>
		public static List<Player> PlayersNear(string city) {
			List<Player> players = new List<Player>();
			Point4D centre = CentreOf(city);
			if (centre == null) {
				return players;
			}
			foreach (AbstractCharacter ch in Map.GetMap(centre.M).GetPlayersInRange(centre.X, centre.Y, Settings.playerRange)) {
				if ((ch is Player player) && !player.IsGM && !player.Flag_Dead && !player.Flag_Disconnected) {
					players.Add(player);
				}
			}
			return players;
		}

		private static readonly IMovementSettings walker = new DynamicMovementSettings();

		/// <summary>A walk into the city: from a tile `steps` real walking steps from the centre (as far as there is to walk),
		/// tile by tile to the centre. Null when the centre can't be left.</summary>
		public static List<int[]> RouteIn(Point4D centre, int steps) {
			Map map = Map.GetMap(centre.M);
			Dictionary<long, int[]> from = new Dictionary<long, int[]>();
			int[] start = { centre.X, centre.Y, centre.Z };
			from[Key(start)] = null;
			List<int[]> ring = new List<int[]> { start };
			for (int step = 0; step < steps; step++) {
				List<int[]> next = new List<int[]>();
				foreach (int[] tile in ring) {
					Point3D probe = new Point3D(tile[0], tile[1], tile[2]);
					for (int dir = 0; dir < 8; dir++) {
						if (map.CheckMovement(probe, walker, (Direction) dir, false, out int x, out int y, out int z)) {
							int[] reached = { x, y, z };
							long key = Key(reached);
							if (!from.ContainsKey(key)) {
								from[key] = tile;
								next.Add(reached);
							}
						}
					}
				}
				if (next.Count == 0) {
					break;
				}
				ring = next;
			}
			if ((ring.Count == 1) && (ring[0] == start)) {
				return null;
			}
			List<int[]> route = new List<int[]>();
			for (int[] tile = ring[Globals.dice.Next(ring.Count)]; tile != null; tile = from[Key(tile)]) {
				route.Add(tile);
			}
			return route;
		}

		private static long Key(int[] tile) {
			return (((long) tile[0]) << 16) | (uint) tile[1];
		}
		#endregion the city

		#region running
		/// <summary>Every TickSeconds: leftovers of a past run go, the events move on, and now and then a new one starts.</summary>
		internal static void Tick() {
			if (!leftoversCleared) {
				leftoversCleared = true;
				ClearLeftovers();
			}
			foreach (WorldEvent e in running.ToArray()) {
				try {
					e.Tick();
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteError(ex);
					Finish(e); //a broken event mustn't stay forever
				}
				if (e.Ended) {
					Finish(e);
				}
			}
			double now = Globals.TimeInSeconds;
			if (!Settings.enabled || (running.Count >= Settings.maxRunning) || (now < nextStartAt) || (now < nextChanceAt)) {
				return;
			}
			nextChanceAt = now + 60;
			if (Globals.dice.Next(100) < Settings.startChance) {
				TryStartAny();
			}
		}

		/// <summary>An event of a kind whose time it is, in a city it's for where players are. True when one started.</summary>
		internal static bool TryStartAny() {
			List<KeyValuePair<WorldEventKind, string>> choices = new List<KeyValuePair<WorldEventKind, string>>();
			foreach (WorldEventKind kind in kinds) {
				if (!kind.IsTime()) {
					continue;
				}
				foreach (string city in Cities) {
					if (kind.IsFor(city) && !IsBusy(city) && (PlayersNear(city).Count > 0)) {
						choices.Add(new KeyValuePair<WorldEventKind, string>(kind, city));
					}
				}
			}
			while (choices.Count > 0) {
				int i = Globals.dice.Next(choices.Count);
				if (Start(choices[i].Key, choices[i].Value) != null) {
					return true;
				}
				choices.RemoveAt(i);
			}
			return false;
		}

		private static bool IsBusy(string city) {
			return running.Exists(e => string.Equals(e.City, city, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Starts an event of `kind` in `city` now, whatever the hour; null when it found no room there.</summary>
		public static WorldEvent Start(WorldEventKind kind, string city) {
			if ((kind == null) || !kind.IsFor(city) || IsBusy(city)) {
				return null;
			}
			WorldEvent e = kind.Make(city);
			e.StartedAt = Globals.TimeInSeconds;
			if (!e.Begin(PlayersNear(city).Count)) {
				foreach (NPC npc in e.Npcs) {
					if (!npc.IsDeleted) {
						npc.Delete();
					}
				}
				return null;
			}
			running.Add(e);
			byId[e.Id] = e;
			nextStartAt = Globals.TimeInSeconds + (Settings.gapMinutes * 60.0);
			EnsureRunning();
			return e;
		}

		/// <summary>A GM's stop: the event ends with no outcome.</summary>
		public static void Stop(WorldEvent e) {
			foreach (NPC npc in e.Npcs) {
				if (!npc.IsDeleted) {
					npc.Delete();
				}
			}
			e.Ended = true;
			Finish(e);
		}

		private static void Finish(WorldEvent e) {
			running.Remove(e);
			byId.Remove(e.Id);
		}

		/// <summary>The NPCs a past run's events left (a restart forgets the events, not their NPCs).</summary>
		internal static int ClearLeftovers() {
			List<NPC> leftovers = new List<NPC>();
			foreach (Thing thing in Thing.AllThings) {
				if ((thing is NPC npc) && !npc.IsDeleted && (npc.GetTag(WorldEvent.EventTK) is string id) && !byId.ContainsKey(id)) {
					leftovers.Add(npc);
				}
			}
			foreach (NPC npc in leftovers) {
				npc.Delete();
			}
			return leftovers.Count;
		}

		/// <summary>From NPC.On_Death: an event's NPC died.</summary>
		public static void OnDeath(NPC dead, Character killedBy) {
			if ((dead.GetTag(WorldEvent.EventTK) is string id) && byId.TryGetValue(id, out WorldEvent e) && !e.Ended) {
				e.Credit(dead, killedBy);
				e.OnDeath(dead, killedBy);
			}
		}
		#endregion running

		#region outcomes
		/// <summary>An outcome for a city: its prices change by `percent` for `minutes`, and the criers tell it.</summary>
		public static void SetOutcome(string city, int percent, int minutes, string news) {
			if ((percent != 0) && (minutes > 0)) {
				outcomes.Add(new CityOutcome { city = city, percent = percent, until = Globals.TimeInSeconds + (minutes * 60.0), text = news });
			}
			CityNews.Add(city, news);
			history.Add(new CityOutcome { city = city, percent = percent, until = Globals.TimeInSeconds, text = news });
			if (history.Count > OutcomesShown) {
				history.RemoveAt(0);
			}
		}

		private static readonly List<CityOutcome> history = new List<CityOutcome>();

		/// <summary>In percent: how the city's prices stand now after its events (from -50 up to +100).</summary>
		public static int PricePercent(string city) {
			if (city == null) {
				return 0;
			}
			double now = Globals.TimeInSeconds;
			outcomes.RemoveAll(o => o.until <= now);
			int percent = 0;
			foreach (CityOutcome o in outcomes) {
				if (string.Equals(o.city, city, StringComparison.OrdinalIgnoreCase)) {
					percent += o.percent;
				}
			}
			return Math.Max(-50, Math.Min(100, percent));
		}

		/// <summary>A price changed by the city's events; never below 1.</summary>
		public static long AdjustPrice(string city, long price) {
			int percent = PricePercent(city);
			if ((percent == 0) || (price <= 0)) {
				return price;
			}
			return Math.Max(1, (price * (100 + percent) + 50) / 100);
		}

		/// <summary>The latest outcomes, newest last.</summary>
		public static List<string> RecentOutcomes() {
			List<string> texts = new List<string>();
			foreach (CityOutcome o in history) {
				texts.Add(o.text);
			}
			return texts;
		}

		/// <summary>Pays a hero: gold, experience, standing in the city.</summary>
		internal static void Reward(Player player, string city, int gold, int exp, int favour) {
			if ((player == null) || player.IsDeleted) {
				return;
			}
			if (gold > 0) {
				GoldUtils.Grant(player, (uint) gold);
			}
			if (exp > 0) {
				LevelingSystem.AddExperience(player, exp);
			}
			if ((favour != 0) && (Reputation.RealmOfCity(city) != Realm.None) && !NPCAiTimer.IsOppositeRealm(player.Realm, Reputation.RealmOfCity(city))) {
				Reputation.Change(player, city, favour);
			}
			player.SysMessage("Odmena: " + gold + " zlatych, " + exp + " zkusenosti.");
		}

		/// <summary>Tests: no events, no outcomes, the scheduler ready to start one at once.</summary>
		public static void ResetForTests() {
			foreach (WorldEvent e in running.ToArray()) {
				Stop(e);
			}
			outcomes.Clear();
			history.Clear();
			nextStartAt = 0;
			nextChanceAt = 0;
		}
		#endregion outcomes

		/// <summary>.udalost: the events under way; .udalost start "key" ["City"] (the city a GM stands in, if left out);
		/// .udalost stop (the events in the GM's city, or all).</summary>
		[SteamFunction]
		public static void Udalost(Character self, ScriptArgs sa) {
			string[] args = ((sa == null) ? "" : sa.Args).Replace("\"", " ").Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
			string verb = (args.Length > 0) ? args[0].ToLowerInvariant() : "";
			EnsureRunning();
			if (verb == "start") {
				WorldEventKind kind = (args.Length > 1) ? KindOf(args[1]) : null;
				string city = (args.Length > 2) ? args[2] : Reputation.CityAt(self);
				if (kind == null) {
					self.SysMessage("Pouziti: .udalost start <" + string.Join("|", kinds.ConvertAll(k => k.Key)) + "> [mesto]");
					return;
				}
				string known = Array.Find(Cities, c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
				if ((known == null) || !kind.IsFor(known)) {
					self.SysMessage(kind.Name + " nemuze byt v " + (city ?? "(zadne mesto)") + ".");
					return;
				}
				WorldEvent e = Start(kind, known);
				self.SysMessage((e == null) ? "Udalost se nepodarilo zacit (uz tam nejaka bezi, nebo neni misto)." : "Zacala: " + e.Describe());
				return;
			}
			if (verb == "stop") {
				string here = Reputation.CityAt(self);
				int stopped = 0;
				foreach (WorldEvent e in running.ToArray()) {
					if ((here == null) || string.Equals(e.City, here, StringComparison.OrdinalIgnoreCase)) {
						Stop(e);
						stopped++;
					}
				}
				self.SysMessage("Ukonceno udalosti: " + stopped + ".");
				return;
			}
			if (running.Count == 0) {
				self.SysMessage("Ve svete ted zadna udalost nebezi." + (Settings.enabled ? "" : " (Udalosti jsou v .settings vypnute.)"));
			}
			foreach (WorldEvent e in running) {
				self.SysMessage(e.Describe() + " (zbyva " + Math.Max(0, (int) ((e.EndsAt - Globals.TimeInSeconds) / 60)) + " min)");
			}
			self.SysMessage("Pouziti: .udalost | .udalost start <" + string.Join("|", kinds.ConvertAll(k => k.Key)) + "> [mesto] | .udalost stop");
		}
	}

	/// <summary>#1284 - the world events' clock.</summary>
	[SaveableClass, DeepCopyableClass]
	public class WorldEventTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public WorldEventTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			WorldEvents.Tick();
		}
	}

	/// <summary>#1284 - the journal's Udalosti tab: the events under way, then the latest outcomes.</summary>
	public static class WorldEventsPage {
		private const int HeadHue = 95, TextHue = 955, Row = 20, LineChars = 34;

		internal static void Add(CompiledGumpDef gump, Player player, int width, int height) {
			int y = 135;
			gump.AddLabel(85, y, HeadHue, "Prave se deje");
			y += Row;
			if (WorldEvents.Running.Count == 0) {
				gump.AddLabel(90, y, TextHue, "Nic zvlastniho.");
				y += Row;
			}
			foreach (WorldEvent e in WorldEvents.Running) {
				y = AddWrapped(gump, y, e.Describe());
			}
			y += 8;
			gump.AddLabel(85, y, HeadHue, "Nedavno");
			y += Row;
			List<string> recent = WorldEvents.RecentOutcomes();
			if (recent.Count == 0) {
				gump.AddLabel(90, y, TextHue, "Nic se nestalo.");
			}
			for (int i = recent.Count - 1; (i >= 0) && (y < height - 40); i--) {
				y = AddWrapped(gump, y, recent[i]);
			}
		}

		private static int AddWrapped(CompiledGumpDef gump, int y, string text) {
			string rest = text;
			while (rest.Length > 0) {
				int cut = rest.Length;
				if (cut > LineChars) {
					cut = rest.LastIndexOf(' ', LineChars);
					if (cut <= 0) {
						cut = LineChars;
					}
				}
				gump.AddLabel(90, y, TextHue, rest.Substring(0, cut).Trim());
				rest = rest.Substring(cut).TrimStart();
				y += Row;
			}
			return y;
		}
	}
}
