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

		/// <summary>The caravan arrived: each escort's gold, experience, standing in the city; how near the merchant a player stood
		/// in the fight to count as an escort; the goods cheaper (percent) for so many real minutes.</summary>
		[SaveableData]
		public int caravanGold = 250, caravanExp = 400, caravanFavour = 15, escortRange = 15, caravanPercent = -10, caravanOutcomeMinutes = 120;

		/// <summary>#1286 - corsairs at Pelargir: as the raid's numbers (time, size, steps out, pay, the outcome either way).</summary>
		[SaveableData]
		public int corsairMinutes = 15, corsairBase = 4, corsairPerPlayer = 2, corsairMax = 14, corsairSteps = 20;

		[SaveableData]
		public int corsairGold = 300, corsairKillGold = 50, corsairExp = 500, corsairFavour = 20, corsairWonPercent = -5, corsairWonMinutes = 60, corsairLostPercent = 20, corsairLostMinutes = 360;

		/// <summary>#1286 - the beacons of Gondor: how many, tiles between them (out from Minas Tirith towards Edoras), real minutes
		/// to light them all; each lighter's gold, experience, standing; the realm's experience bonus (percent) and its minutes.</summary>
		[SaveableData]
		public int beaconCount = 5, beaconSpacing = 30, beaconMinutes = 20, beaconGold = 200, beaconExp = 300, beaconFavour = 15, beaconBonusPercent = 10, beaconBonusMinutes = 60;

		/// <summary>#1286 - the troll at the ford: walking steps out of the city it stands; real minutes at most (it ends at dawn);
		/// each fighter's gold, experience, standing; minutes the stone troll stays; the night hours it comes in.</summary>
		[SaveableData]
		public int trollSteps = 30, trollMinutes = 30, trollGold = 400, trollExp = 800, trollFavour = 25, trollStoneMinutes = 240, trollNightFrom = 22, trollNightTo = 4;

		/// <summary>#1286 - fire in the city: fires at the start, at most (then the house is lost), seconds between spreads, real
		/// minutes to put them out; blows a fire takes and the stamina of each; each helper's gold, experience, standing, and
		/// friendship with each of the house's people (on the residents' 0-10 scale).</summary>
		[SaveableData]
		public int fireStart = 3, fireMax = 12, fireSpreadSeconds = 30, fireMinutes = 10, fireBeats = 3, fireBeatStamina = 5;

		[SaveableData]
		public int fireGold = 200, fireExp = 300, fireFavour = 15, fireFriendship = 2;

		/// <summary>#1286 - fever: how many residents fall ill, real minutes to cure them all; each cure's gold and experience;
		/// the curer's standing (and the healers' faction in Minas Tirith); friendship with each one cured (0-10 scale).</summary>
		[SaveableData]
		public int feverCount = 4, feverMinutes = 30, feverGold = 100, feverExp = 150, feverFavour = 15, feverFriendship = 2;

		/// <summary>#1289 - festivals: real minutes one lasts, the city's prices lower (percent) while it does, rounds of the
		/// runaway-animal game, each catch's gold and experience, and each guest's experience and standing at the end.</summary>
		[SaveableData]
		public int festivalMinutes = 60, festivalDiscount = 5, festivalRounds = 5, festivalPrizeGold = 150, festivalPrizeExp = 200, festivalExp = 100, festivalFavour = 5;

		/// <summary>#1289 - real hours before a place holds another festival (one a day, whichever).</summary>
		[SaveableData]
		public int festivalGapHours = 20;
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

		/// <summary>The places it can happen in, as FlaggedRegion.City names them: the pilot cities unless it says otherwise.</summary>
		public virtual IList<string> Places {
			get { return WorldEvents.Cities; }
		}

		/// <summary>The area whose spawn point is the event's centre in `city`.</summary>
		public virtual string AreaFor(string city) {
			return "a_" + city.Replace('-', '_');
		}

		/// <summary>#1289 - starts by the calendar (a festival): once a day per place when its time comes and a player is there,
		/// with no chance roll or gap; the random pick never takes it.</summary>
		public virtual bool StartsByCalendar {
			get { return false; }
		}

		/// <summary>#1289 - counts toward maxRunning (festivals don't: a city's feast doesn't stop a raid elsewhere).</summary>
		public virtual bool CountsTowardLimit {
			get { return true; }
		}

		/// <summary>The place's name in what the event says.</summary>
		public virtual string PlaceName(string city) {
			return city.Replace('_', ' ');
		}
	}

	/// <summary>#1284 - one event under way, saved with the world since #1307 (WorldEventsStore). Its NPCs
	/// carry its id in a tag; those who fought them are its heroes.</summary>
	public abstract class WorldEvent {
		internal static readonly TagKey EventTK = TagKey.Acquire("_wev_");

		public readonly WorldEventKind Kind;
		public readonly string City;
		public string Id { get; internal set; } //#1307 - a restored event keeps its id (its NPCs carry it)
		public Point4D Centre { get; internal set; }
		public readonly List<Item> Items = new List<Item>();
		public double StartedAt, EndsAt;
		public readonly List<NPC> Npcs = new List<NPC>();
		public readonly Dictionary<Player, int> Heroes = new Dictionary<Player, int>();
		public bool Ended, Won;

		protected WorldEvent(WorldEventKind kind, string city) {
			this.Kind = kind;
			this.City = city;
			this.Id = kind.Key + "/" + city + "/" + (++WorldEvents.serial).ToString(CultureInfo.InvariantCulture);
			this.Centre = WorldEvents.CentreOf(city, kind);
		}

		protected static WorldEventSettings Settings {
			get { return WorldEventSettings.instance; }
		}

		public string CityName {
			get { return this.Kind.PlaceName(this.City); }
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

		/// <summary>#1307 - the kind's own state into a record before a save (the base fields are saved by WorldEventRecord.Of).</summary>
		internal virtual void SaveTo(WorldEventRecord record) {
		}

		/// <summary>#1307 - the kind's own state back from a record after a restart.</summary>
		internal virtual void LoadFrom(WorldEventRecord record) {
		}

		/// <summary>An item of the event, fixed where it's put; it goes with the event.</summary>
		protected Item Place(string defname, int[] at, byte m) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			if ((def == null) || (at == null)) {
				return null;
			}
			Item item = (Item) def.Create(at[0], at[1], at[2], m);
			item.SetAttribute(ItemAttributes.attr_move_never, true);
			item.SetTag(EventTK, this.Id);
			this.Items.Add(item);
			return item;
		}

		/// <summary>Everything the event put in the world goes.</summary>
		internal void DeleteAll() {
			foreach (NPC npc in this.Npcs) {
				if (!npc.IsDeleted) {
					npc.Delete();
				}
			}
			foreach (Item item in this.Items) {
				if (!item.IsDeleted) {
					item.Delete();
				}
			}
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

		/// <summary>Whether fighting this NPC of the event makes a hero: its raiders and bandits do, its merchant and pack animals don't.</summary>
		protected abstract bool IsFoe(NPC npc);

		internal void Credit(NPC dead, Character killedBy) {
			if (!this.IsFoe(dead)) {
				return; //killing the caravan's own horse is no deed
			}
			foreach (Character fighter in LevelingSystem.FindFighters(dead, killedBy, false)) {
				if ((fighter is Player player) && WorldEvents.IsOnCitysSide(player, this.City)) {
					this.Heroes.TryGetValue(player, out int kills);
					this.Heroes[player] = kills + ((fighter == killedBy) ? 1 : 0);
				}
			}
		}

		/// <summary>The event is over: its NPCs go, the outcome stands.</summary>
		protected void End(bool won) {
			this.Ended = true;
			this.Won = won;
			this.DeleteAll();
		}
	}

	/// <summary>#1284 - the world events (design section 6): a scheduler on Globals starts one now and then in a city where
	/// a player is, events are saved with the world (#1307), and their outcomes change a city's prices for a while (Reputation.PriceFor) and go to
	/// the criers (CityNews). GM command .udalost.</summary>
	public static class WorldEvents {
		public const double TickSeconds = 5;
		private const int OutcomesShown = 5;
		private static readonly TimerKey timerTK = TimerKey.Acquire("_world_events_");

		/// <summary>Tests: the events' clock on Globals.</summary>
		internal static TimerKey TimerKeyForTests {
			get { return timerTK; }
		}

		internal static int serial;
		private static readonly List<WorldEventKind> kinds = new List<WorldEventKind>();
		internal static readonly List<WorldEvent> running = new List<WorldEvent>();
		internal static readonly Dictionary<string, WorldEvent> byId = new Dictionary<string, WorldEvent>(StringComparer.Ordinal);
		internal static readonly List<CityOutcome> outcomes = new List<CityOutcome>();
		internal static double nextStartAt, nextChanceAt, nextCalendarAt;
		private static bool leftoversCleared, gapAfterBoot;

		/// <summary>Test servers (the harness's environment variable) start no event by themselves unless a test allows it:
		/// hostile raiders and price changes would upset other tests.</summary>
		public static bool autoStartOnTestServer;

		private static bool IsTestServer {
			get { return Environment.GetEnvironmentVariable(TestHarness.CommandsVariable) == "1"; }
		}

		/// <summary>The cities the scheduler looks at: the pilots and (#1304) the map's other cities.</summary>
		public static readonly string[] Cities = { "Minas_Tirith", "Edoras", "Hobitin", "Barad-dur", "Harad", "Umbar",
			"Dol_Amroth", "Erebor", "Dol", "Fornost", "Kralovstvi_lesnich_elfu", "Sede_pristavy", "Nurn", "Rhun" }; //#1304 - the other cities too (not the abandoned Dainovo_kralovstvi, nor the test London)

		internal sealed class CityOutcome {
			internal string city, text, eventId;
			internal int percent;
			internal double until;
		}

		private static WorldEventSettings Settings {
			get { return WorldEventSettings.instance; }
		}

		public static void Bootstrap() {
			Register(new OrcRaidKind());
			Register(new CaravanKind());
			Register(new CorsairKind());
			Register(new BeaconsKind());
			Register(new TrollKind());
			Register(new FireKind());
			Register(new FeverKind());
			foreach (FestivalKind festival in FestivalKind.All) {
				Register(festival);
			}
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

		/// <summary>#1315 - every kind, for the GMs' dialog.</summary>
		public static IList<WorldEventKind> Kinds {
			get { return kinds.AsReadOnly(); }
		}

		/// <summary>A GM's start of a kind in a city (the command's and the dialog's): what the GM is told.</summary>
		public static string StartByGm(WorldEventKind kind, string city) {
			EnsureRunning();
			string known = (city == null) ? null : System.Linq.Enumerable.FirstOrDefault(kind.Places, c => string.Equals(c, city, StringComparison.OrdinalIgnoreCase));
			if ((known == null) || !kind.IsFor(known)) {
				return kind.Name + " nemuze byt v " + (city ?? "(zadne mesto)") + ".";
			}
			WorldEvent e = Start(kind, known);
			return (e == null) ? "Udalost se nepodarilo zacit (uz tam nejaka bezi, nebo neni misto)." : "Zacala: " + e.Describe();
		}

		/// <summary>The minutes an event has left.</summary>
		public static int MinutesLeft(WorldEvent e) {
			return Math.Max(0, (int) ((e.EndsAt - Globals.TimeInSeconds) / 60));
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
		private static readonly Dictionary<string, Point4D> centres = new Dictionary<string, Point4D>(StringComparer.OrdinalIgnoreCase);

		/// <summary>A city's centre: a standable spot at its main area's spawn point (a_City), or null. Remembered per area (#1304:
		/// the scheduler asks it for every city and kind each try; a recompile starts afresh).</summary>
		public static Point4D CentreOf(string city, WorldEventKind kind = null) {
			string key = city + "|" + ((kind == null) ? "" : kind.AreaFor(city));
			if (centres.TryGetValue(key, out Point4D known)) {
				return known;
			}
			Point4D found = FindCentre(city, kind);
			if (found != null) {
				centres[key] = found;
			}
			return found;
		}

		/// <summary>#1317 - the centre of one area (a town of a city with several, as Pelargir of Dol Amroth), remembered.</summary>
		public static Point4D CentreOfArea(StaticRegion area) {
			string key = "area|" + area.Defname;
			if (centres.TryGetValue(key, out Point4D known)) {
				return known;
			}
			Point4D p = area.P;
			int[] at = WorldSpots.NearestStandable(Map.GetMap(p.M), p.X, p.Y, 4);
			Point4D found = (at == null) ? p : new Point4D(at[0], at[1], at[2], p.M);
			centres[key] = found;
			return found;
		}

		private static Point4D FindCentre(string city, WorldEventKind kind) {
			StaticRegion area = StaticRegion.GetByDefname((kind == null) ? ("a_" + city.Replace('-', '_')) : kind.AreaFor(city));
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

		/// <summary>A player whose deeds count for the city: no GM, and not of the realm at war with it.</summary>
		public static bool IsOnCitysSide(Player player, string city) {
			return (player != null) && !player.IsDeleted && !player.IsGM && !NPCAiTimer.IsOppositeRealm(player.Realm, Reputation.RealmOfCity(city));
		}

		/// <summary>The players who count as in the city now: online, alive, on its side (IsOnCitysSide), near its centre.</summary>
		public static List<Player> PlayersNear(string city, WorldEventKind kind = null) {
			List<Player> players = new List<Player>();
			Point4D centre = CentreOf(city, kind);
			if (centre == null) {
				return players;
			}
			foreach (AbstractCharacter ch in Map.GetMap(centre.M).GetPlayersInRange(centre.X, centre.Y, Settings.playerRange)) {
				if ((ch is Player player) && !player.Flag_Dead && !player.Flag_Disconnected && IsOnCitysSide(player, city)) {
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
			//N steps out can still be just behind a wall: of the last ring, the tiles farthest from the centre in a straight line
			int farthest = 0;
			foreach (int[] tile in ring) {
				farthest = Math.Max(farthest, WorldSpots.Distance(tile, centre.X, centre.Y));
			}
			List<int[]> far = ring.FindAll(t => WorldSpots.Distance(t, centre.X, centre.Y) >= farthest - 2);
			List<int[]> route = new List<int[]>();
			for (int[] tile = far[Globals.dice.Next(far.Count)]; tile != null; tile = from[Key(tile)]) {
				route.Add(tile);
			}
			return route;
		}

		/// <summary>#1329 - the walks out of a town, found once per area and centre: a few roads to pick from, each only its stretch
		/// past the edge (all a placement needs). #1333 - kept with the world (RoadsOutStore) and walked by .SetupWorld
		/// (RoadsOutSetup), as a big town's walk takes up to ~0.9 s; .ResetRoadsOut forgets them.</summary>
		internal static readonly Dictionary<string, List<List<int[]>>> routesOut = new Dictionary<string, List<List<int[]>>>(StringComparer.Ordinal);
		private const int RoutesOutKept = 8;

		/// <summary>#1333 - the walks out of a town made since the boot (a test reads it: loaded roads are never walked).</summary>
		internal static int roadsWalked;

		internal static string RoadsOutKey(Point4D centre, StaticRegion area, int beyond, int maxSteps) {
			return area.Defname + "|" + centre.X.ToString(CultureInfo.InvariantCulture) + "," + centre.Y.ToString(CultureInfo.InvariantCulture) + ","
				+ centre.M.ToString(CultureInfo.InvariantCulture) + "|" + beyond.ToString(CultureInfo.InvariantCulture) + "|" + maxSteps.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>#1329 - the roads out of a town: out from the centre ring by ring by the real movement rules until some walk has gone
		/// `beyond` steps in a row outside the area (a step out through a notch or a corner and back in doesn't count); from each of a
		/// few tiles whose walk has been outside the longest, that stretch past the edge, the far end first (#1333: only the stretch,
		/// the way back in isn't kept). None (an empty list) when no walk gets outside within `maxSteps` steps (or the centre can't be
		/// left); near the cap the stretch may be shorter (or an island's shore ends it). The area is on the centre's map plane.
		/// Walked once, then remembered (and saved).</summary>
		public static List<List<int[]>> RoutesOutOf(Point4D centre, StaticRegion area, int beyond, int maxSteps) {
			string key = RoadsOutKey(centre, area, beyond, maxSteps);
			if (!routesOut.TryGetValue(key, out List<List<int[]>> routes)) {
				routes = WalkOutOf(centre, area, beyond, maxSteps);
				routesOut[key] = routes;
				roadsWalked++;
			}
			return new List<List<int[]>>(routes); //the roads themselves are only read
		}

		private static List<List<int[]>> WalkOutOf(Point4D centre, StaticRegion area, int beyond, int maxSteps) {
			Map map = Map.GetMap(centre.M);
			Dictionary<long, int[]> from = new Dictionary<long, int[]>();
			Dictionary<long, int> run = new Dictionary<long, int>(); //how many steps in a row a tile's walk has been outside, ending on it
			int[] start = { centre.X, centre.Y, centre.Z };
			from[Key(start)] = null;
			run[Key(start)] = 0;
			List<int[]> ring = new List<int[]> { start };
			List<int[]> longest = new List<int[]>(), outsideTiles = new List<int[]>();
			int longestRun = 0;
			for (int step = 0; (step < maxSteps) && (longestRun < beyond); step++) {
				List<int[]> next = new List<int[]>();
				foreach (int[] tile in ring) {
					Point3D probe = new Point3D(tile[0], tile[1], tile[2]);
					int parentRun = run[Key(tile)];
					for (int dir = 0; dir < 8; dir++) {
						if (map.CheckMovement(probe, walker, (Direction) dir, false, out int x, out int y, out int z)) {
							int[] reached = { x, y, z };
							long key = Key(reached);
							if (!from.ContainsKey(key)) {
								from[key] = tile;
								int outside = area.ContainsPoint(x, y) ? 0 : parentRun + 1;
								run[key] = outside;
								next.Add(reached);
								if (outside > longestRun) {
									longestRun = outside;
									longest.Clear();
								}
								if ((outside > 0) && (outside == longestRun)) {
									longest.Add(reached);
								}
								if (outside > 0) {
									outsideTiles.Add(reached);
								}
							}
						}
					}
				}
				if (next.Count == 0) {
					break;
				}
				ring = next;
			}
			List<int[]> ends = new List<int[]>();
			while (longest.Count > 0) {
				int pick = Globals.dice.Next(longest.Count);
				ends.Add(longest[pick]);
				longest.RemoveAt(pick);
			}
			if (ends.Count < RoutesOutKept) { //few tiles that far out (an island's shore): the other tiles outside too, the farthest out first
				outsideTiles.RemoveAll(t => ends.Contains(t));
				outsideTiles.Sort((a, b) => run[Key(b)].CompareTo(run[Key(a)]));
				ends.AddRange(outsideTiles.GetRange(0, Math.Min(outsideTiles.Count, RoutesOutKept - ends.Count)));
			}
			List<List<int[]>> routes = new List<List<int[]>>();
			foreach (int[] end in ends.GetRange(0, Math.Min(ends.Count, RoutesOutKept))) {
				List<int[]> route = new List<int[]>();
				for (int[] tile = end; (tile != null) && !area.ContainsPoint(tile[0], tile[1]); tile = from[Key(tile)]) {
					route.Add(tile); //the stretch past the edge only: the rest of the way in is never used, nor saved
				}
				routes.Add(route);
			}
			return routes;
		}

		/// <summary>The tiles a walker reaches from `start` in at most `steps` steps by the real movement rules, start included.</summary>
		public static List<int[]> Reachable(Map map, int[] start, int steps) {
			HashSet<long> seen = new HashSet<long> { Key(start) };
			List<int[]> all = new List<int[]> { start };
			List<int[]> ring = new List<int[]> { start };
			for (int step = 0; (step < steps) && (ring.Count > 0); step++) {
				List<int[]> next = new List<int[]>();
				foreach (int[] tile in ring) {
					Point3D probe = new Point3D(tile[0], tile[1], tile[2]);
					for (int dir = 0; dir < 8; dir++) {
						if (map.CheckMovement(probe, walker, (Direction) dir, false, out int x, out int y, out int z) && seen.Add(Key(new[] { x, y }))) {
							next.Add(new[] { x, y, z });
						}
					}
				}
				all.AddRange(next);
				ring = next;
			}
			return all;
		}

		private static long Key(int[] tile) {
			return (((long) tile[0]) << 16) | (uint) tile[1];
		}
		#endregion the city

		#region running
		/// <summary>Every TickSeconds: leftovers of a past run go, the events move on, and now and then a new one starts.</summary>
		internal static void Tick() {
			EnsureLeftoversCleared();
			TickLandmarks();
			if (!gapAfterBoot) {
				gapAfterBoot = true; //nothing starts by itself in the first minutes after a boot
				nextStartAt = Math.Max(nextStartAt, Globals.TimeInSeconds + (Settings.gapMinutes * 60.0));
			}
			foreach (WorldEvent e in running.ToArray()) {
				try {
					e.Tick();
				} catch (FatalException) {
					throw;
				} catch (Exception ex) {
					Logger.WriteError(ex);
					Stop(e); //a broken event mustn't stay forever, nor its raiders
					continue;
				}
				if (e.Ended) {
					Finish(e);
				}
			}
			double now = Globals.TimeInSeconds;
			if (!Settings.enabled || (IsTestServer && !autoStartOnTestServer)) {
				return;
			}
			if (now >= nextCalendarAt) {
				nextCalendarAt = now + 60;
				TryStartByCalendar();
			}
			if ((running.FindAll(e => e.Kind.CountsTowardLimit).Count >= Settings.maxRunning) || (now < nextStartAt) || (now < nextChanceAt)) {
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
				if (kind.StartsByCalendar || !kind.IsTime()) {
					continue;
				}
				foreach (string city in kind.Places) {
					if (kind.IsFor(city) && !IsBusy(city) && (PlayersNear(city, kind).Count > 0)) {
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

		/// <summary>#1289 - the calendar's events whose day it is, where a player is and they haven't been held today.</summary>
		internal static int TryStartByCalendar() {
			int started = 0;
			foreach (WorldEventKind kind in kinds.ToArray()) {
				if (!kind.StartsByCalendar || !kind.IsTime()) {
					continue;
				}
				foreach (string city in kind.Places) {
					if (kind.IsFor(city) && !IsBusy(city) && !HeldRecently(city) && (PlayersNear(city, kind).Count > 0)) {
						if (Start(kind, city) != null) {
							started++;
						} else {
							MarkHeld(city); //no room for it here: not tried again every minute
							Logger.WriteWarning(kind.Name + " couldn't start in " + city + ".");
						}
					}
				}
			}
			return started;
		}

		private static TagKey HeldKey(string city) {
			return TagKey.Acquire("_wev_held_" + Reputation.Word(city));
		}

		/// <summary>#1289 - whether the place held a calendar event (any festival) in the last festivalGapHours of real time; kept
		/// on Globals, so a restart doesn't hold the day's festival again, and a festival over midnight isn't held twice.</summary>
		public static bool HeldRecently(string city) {
			object held = Globals.Instance.GetTag(HeldKey(city));
			if (held == null) {
				return false;
			}
			DateTime at = new DateTime(Convert.ToInt64(held, CultureInfo.InvariantCulture), DateTimeKind.Utc);
			return (Errands.Now - at).TotalHours < Settings.festivalGapHours;
		}

		private static void MarkHeld(string city) {
			Globals.Instance.SetTag(HeldKey(city), Errands.Now.Ticks.ToString(CultureInfo.InvariantCulture));
		}

		private static bool IsBusy(string city) {
			return running.Exists(e => string.Equals(e.City, city, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Starts an event of `kind` in `city` now, whatever the hour; null when it found no room there.</summary>
		public static WorldEvent Start(WorldEventKind kind, string city) {
			if ((kind == null) || !kind.IsFor(city) || IsBusy(city)) {
				return null;
			}
			EnsureLeftoversCleared(); //before a new event can take a past run's id
			WorldEvent e = kind.Make(city);
			e.StartedAt = Globals.TimeInSeconds;
			bool begun = false;
			try {
				begun = (e.Centre != null) && e.Begin(PlayersNear(city, kind).Count);
			} catch (FatalException) {
				throw;
			} catch (Exception ex) {
				Logger.WriteError(ex);
			}
			if (!begun) {
				e.DeleteAll();
				return null;
			}
			running.Add(e);
			byId[e.Id] = e;
			if (kind.StartsByCalendar) {
				MarkHeld(city); //once a day per place, whichever festival; a GM's start too
			} else {
				nextStartAt = Globals.TimeInSeconds + (Settings.gapMinutes * 60.0);
			}
			EnsureRunning();
			return e;
		}

		/// <summary>A GM's stop: the event ends with no outcome.</summary>
		public static void Stop(WorldEvent e) {
			e.DeleteAll();
			e.Ended = true;
			outcomes.RemoveAll(o => o.eventId == e.Id); //what it changed while running goes with it (a festival's prices)
			Finish(e);
		}

		private static void Finish(WorldEvent e) {
			running.Remove(e);
			byId.Remove(e.Id);
		}

		private static void EnsureLeftoversCleared() {
			if (!leftoversCleared) {
				leftoversCleared = true;
				ClearLeftovers();
			}
		}

		/// <summary>The NPCs left by events that aren't running (a record that couldn't be restored, a GM's stop mid-save).</summary>
		internal static int ClearLeftovers() {
			List<Thing> leftovers = new List<Thing>();
			foreach (Thing thing in Thing.AllThings) {
				if (thing.IsDeleted) {
					continue;
				}
				if (((thing.GetTag(WorldEvent.EventTK) is string id) && !byId.ContainsKey(id))
						|| ((thing.GetTag(StoneUntilTK) != null) && !landmarks.Contains(thing))) {
					leftovers.Add(thing);
				}
			}
			foreach (Thing thing in leftovers) {
				thing.Delete();
			}
			return leftovers.Count;
		}

		#region landmarks and bonuses
		internal static readonly TagKey StoneUntilTK = TagKey.Acquire("_wev_landmark_");
		internal static readonly List<Thing> landmarks = new List<Thing>();

		/// <summary>A thing an event leaves behind for a while (a stone troll): it goes when its time is up (kept over a restart, #1307).</summary>
		public static void AddLandmark(Thing thing, int minutes) {
			thing.SetTag(StoneUntilTK, Globals.TimeInSeconds + (minutes * 60.0));
			landmarks.Add(thing);
		}

		private static void TickLandmarks() {
			double now = Globals.TimeInSeconds;
			foreach (Thing thing in landmarks.ToArray()) {
				if (thing.IsDeleted || (Convert.ToDouble(thing.GetTag(StoneUntilTK), CultureInfo.InvariantCulture) <= now)) {
					if (!thing.IsDeleted) {
						thing.Delete();
					}
					landmarks.Remove(thing);
				}
			}
		}

		internal static readonly Dictionary<Realm, KeyValuePair<int, double>> experienceBonus = new Dictionary<Realm, KeyValuePair<int, double>>();

		/// <summary>A realm's experience bonus: every player of it gets `percent` more of each gain for `minutes`.</summary>
		public static void SetExperienceBonus(Realm realm, int percent, int minutes) {
			experienceBonus[realm] = new KeyValuePair<int, double>(percent, Globals.TimeInSeconds + (minutes * 60.0));
		}

		/// <summary>From LevelingSystem.AddExperience: the extra a gain earns from the player's realm's bonus.</summary>
		public static int ExperienceBonus(Player player, int amount) {
			if ((player == null) || (amount <= 0) || !experienceBonus.TryGetValue(player.Realm, out KeyValuePair<int, double> bonus)
					|| (bonus.Value <= Globals.TimeInSeconds)) {
				return 0;
			}
			return (int) ((long) amount * bonus.Key / 100);
		}

		public static int ExperienceBonusPercent(Realm realm) {
			return (experienceBonus.TryGetValue(realm, out KeyValuePair<int, double> bonus) && (bonus.Value > Globals.TimeInSeconds)) ? bonus.Key : 0;
		}
		#endregion landmarks and bonuses

		/// <summary>The running event a thing belongs to, or null.</summary>
		public static WorldEvent EventOf(Thing thing) {
			return ((thing != null) && (thing.GetTag(WorldEvent.EventTK) is string id) && byId.TryGetValue(id, out WorldEvent e)) ? e : null;
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
		public static void SetOutcome(string city, int percent, int minutes, string news, string eventId = null) {
			if ((percent != 0) && (minutes > 0)) {
				outcomes.Add(new CityOutcome { city = city, percent = percent, until = Globals.TimeInSeconds + (minutes * 60.0), text = news, eventId = eventId });
			}
			CityNews.Add(city, news);
			history.Add(new CityOutcome { city = city, percent = percent, until = Globals.TimeInSeconds, text = news });
			if (history.Count > OutcomesShown) {
				history.RemoveAt(0);
			}
		}

		internal static readonly List<CityOutcome> history = new List<CityOutcome>();

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

		/// <summary>Pays a hero: gold, experience, standing in the city; and counts the event toward a deed (#1296: the defender's
		/// by default, null none).</summary>
		internal static void Reward(Player player, string city, int gold, int exp, int favour, string deed = Deeds.Defender) {
			if (!IsOnCitysSide(player, city)) {
				return; //no GM, no enemy of the city
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
			if (deed != null) {
				Deeds.Add(player, deed);
			}
		}

		/// <summary>Tests: no events, no outcomes, the scheduler ready to start one at once.</summary>
		public static void ResetForTests() {
			foreach (WorldEvent e in running.ToArray()) {
				Stop(e);
			}
			outcomes.Clear();
			history.Clear();
			experienceBonus.Clear();
			foreach (Thing thing in landmarks) {
				if (!thing.IsDeleted) {
					thing.Delete();
				}
			}
			landmarks.Clear();
			nextStartAt = 0;
			nextChanceAt = 0;
			nextCalendarAt = 0;
			foreach (WorldEventKind kind in kinds) {
				if (kind.StartsByCalendar) {
					foreach (string city in kind.Places) {
						Globals.Instance.RemoveTag(HeldKey(city));
					}
				}
			}
			gapAfterBoot = true;
		}

		/// <summary>Tests: the landmarks forgotten, as a restart forgets them (the things stay, for ClearLeftovers to find).</summary>
		public static void ForgetLandmarksForTests() {
			landmarks.Clear();
		}

		/// <summary>Tests: as just after a boot (the leftovers not yet cleared, the gap not yet waited).</summary>
		public static void PretendBootForTests() {
			leftoversCleared = false;
			gapAfterBoot = false;
		}

		/// <summary>Tests: the scheduler may roll again at once.</summary>
		public static void ClearScheduleForTests() {
			nextStartAt = 0;
			nextChanceAt = 0;
			nextCalendarAt = 0;
		}
		#endregion outcomes

		/// <summary>.udalost: the GMs' dialog (#1315; the events under way as lines from the console); .udalost start "key" ["City"] (the city a GM stands in, if left out);
		/// .udalost stop ["vse"] (the events in the GM's city, or all).</summary>
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
				self.SysMessage(StartByGm(kind, city));
				return;
			}
			if (verb == "stop") {
				string here = ((args.Length > 1) && (args[1].ToLowerInvariant() == "vse")) ? null : Reputation.CityAt(self);
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
			if ((verb.Length == 0) && (self is Player gm)) {
				D_WorldEvents.Show(gm, null); //#1315 - the GMs' dialog: what runs where, stop, start
				return;
			}
			if (running.Count == 0) {
				self.SysMessage("Ve svete ted zadna udalost nebezi." + (Settings.enabled ? "" : " (Udalosti jsou v .settings vypnute.)"));
			}
			foreach (WorldEvent e in running) {
				self.SysMessage(e.Describe() + " (zbyva " + MinutesLeft(e) + " min)");
			}
			self.SysMessage("Pouziti: .udalost | .udalost start <" + string.Join("|", kinds.ConvertAll(k => k.Key)) + "> [mesto] | .udalost stop [vse]");
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

		internal const string QuietHint = "Ve svete se ted nic zvlastniho nedeje.";

		internal static void Add(CompiledGumpDef gump, Player player, int width, int height) {
			int y = 135;
			List<string> recent = WorldEvents.RecentOutcomes();
			if ((WorldEvents.Running.Count == 0) && (recent.Count == 0)) {
				gump.AddLabel(85, y, TextHue, QuietHint);
				return;
			}
			gump.AddLabel(85, y, HeadHue, "Prave se deje");
			y += Row;
			if (WorldEvents.Running.Count == 0) {
				gump.AddLabel(90, y, TextHue, "Nic zvlastniho.");
				y += Row;
			}
			foreach (WorldEvent e in WorldEvents.Running) {
				if (y < height - 80) {
					y = AddWrapped(gump, y, e.Describe());
				}
			}
			y += 8;
			gump.AddLabel(85, y, HeadHue, "Nedavno");
			y += Row;
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
