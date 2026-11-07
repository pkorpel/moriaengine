using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1243 - the levels of a city's view of a player (design section 15.2), worst first.</summary>
	public enum ReputationLevel {
		Nepritel, Podezrely, Neznamy, Znamy, Vazeny, Cteny, Hrdina
	}

	/// <summary>#1243 - a faction of a city (design 15.1). Serving one costs standing with its rival, by half.</summary>
	public sealed class Faction {
		public readonly string Key, Name, City, Rival;

		public Faction(string key, string name, string city, string rival) {
			this.Key = key;
			this.Name = name;
			this.City = city;
			this.Rival = rival;
		}

		public static readonly Faction[] All = {
			new Faction("mt_citadel", "Straz Citadely", "Minas_Tirith", null),
			new Faction("mt_merchants", "Kupci dolnich kruhu", "Minas_Tirith", null),
			new Faction("mt_healers", "Domy uzdraveni", "Minas_Tirith", null),
			new Faction("mt_archives", "Archivari", "Minas_Tirith", null),
			new Faction("ed_household", "Kraluv dvur", "Edoras", null),
			new Faction("ed_riders", "Jezdci", "Edoras", null),
			new Faction("ed_breeders", "Chovatele koni", "Edoras", null),
			new Faction("ho_post", "Hobitinska posta", "Hobitin", null),
			new Faction("ho_farmers", "Sedlaci", "Hobitin", "ho_innkeepers"),
			new Faction("ho_innkeepers", "Hostinsti", "Hobitin", "ho_farmers"),
			new Faction("bd_gorbag", "Gorbagova tlupa", "Barad-dur", "bd_shagrat"),
			new Faction("bd_shagrat", "Shagratova tlupa", "Barad-dur", "bd_gorbag"),
			new Faction("bd_overseers", "Dozorci", "Barad-dur", null),
			new Faction("bd_spies", "Ucho Sauronova Hlasu", "Barad-dur", null),
			new Faction("ha_caravans", "Vudci karavan", "Harad", null),
			new Faction("ha_mumak", "Krotitele olifantu", "Harad", null),
			new Faction("ha_serpent", "Chram hada", "Harad", null),
			new Faction("um_corsairs", "Korzarsti kapitani", "Umbar", "um_harbour"),
			new Faction("um_fences", "Otrokari a paseraci", "Umbar", null),
			new Faction("um_harbour", "Pani pristavu", "Umbar", "um_corsairs"),
		};

		public static Faction Get(string key) {
			foreach (Faction faction in All) {
				if (string.Equals(faction.Key, key, StringComparison.OrdinalIgnoreCase)) {
					return faction;
				}
			}
			return null;
		}
	}

	/// <summary>#1243 - .settings "Povest": the numbers of the reputation system.</summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class ReputationSettings : SettingsMetaCategory {
		[SavedMember]
		public static ReputationSettings instance = new ReputationSettings();

		[LoadingInitializer]
		public ReputationSettings() {
		}

		/// <summary>Where each level starts, Podezrely to Hrdina (Nepritel is everything below Podezrely).</summary>
		[SaveableData]
		public int suspectFrom = -300, strangerFrom = -100, knownFrom = 0, respectedFrom = 200, honouredFrom = 450, heroFrom = 750;
		[SaveableData]
		public int enemyRealmStart = -600;
		/// <summary>An outcast belongs to no city: distrusted everywhere, but not an enemy.</summary>
		[SaveableData]
		public int outcastStart = -200;
		/// <summary>The level a town stone wants for citizenship (ReputationLevel, 3 = Znamy): raise it once errands give standing.</summary>
		[SaveableData]
		public int citizenshipMinLevel = (int) ReputationLevel.Znamy;
		[SaveableData]
		public int crimeLoss = 50, murderLoss = 300, harmResidentLoss = 20, killResidentLoss = 150;
		[SaveableData]
		public int harmResidentEverySeconds = 60;
		/// <summary>Bad reputation drifts back toward 0 by this much a real day.</summary>
		[SaveableData]
		public int driftPerDay = 50;
		/// <summary>The top level wants a good deed in so many real days, or it shows one level lower.</summary>
		[SaveableData]
		public int heroRecentDays = 30;
		/// <summary>Price in percent by level, Nepritel to Hrdina; 0 = won't sell.</summary>
		[SaveableData]
		public int priceEnemy = 0, priceSuspect = 150, priceStranger = 110, priceKnown = 100, priceRespected = 95, priceHonoured = 90, priceHero = 85;
	}

	/// <summary>#1243 - reputation per city and per faction, design section 15. Kept on the player as tags whose names hold word
	/// characters only (a saved tag name reads back by \w+). A city never touched starts at 0 for its own realm and below for the
	/// enemy realm, computed, not stored. Bad standing drifts back toward 0, worked out when read.</summary>
	public static class Reputation {
		public const int Min = -1000, Max = 1000;
		private const double SecondsPerDay = 86400;

		/// <summary>For tests: the real time it is, in UTC.</summary>
		public static DateTime? NowOverride;

		private static DateTime Now {
			get { return NowOverride ?? DateTime.UtcNow; }
		}

		private static ReputationSettings Settings {
			get { return ReputationSettings.instance; }
		}

		internal static string Word(string text) {
			StringBuilder key = new StringBuilder();
			foreach (char c in (text ?? "").ToLowerInvariant()) {
				key.Append(((c >= 'a') && (c <= 'z')) || ((c >= '0') && (c <= '9')) ? c : '_');
			}
			return key.ToString();
		}

		private static TagKey CityKey(string city) {
			return TagKey.Acquire("_rep_c_" + Word(city));
		}

		private static TagKey CityTimeKey(string city) {
			return TagKey.Acquire("_rep_ct_" + Word(city));
		}

		private static TagKey CityDeedKey(string city) {
			return TagKey.Acquire("_rep_cg_" + Word(city));
		}

		private static TagKey FactionKey(string faction) {
			return TagKey.Acquire("_rep_f_" + Word(faction));
		}

		//regions are fixed once loaded and a recompile makes fresh statics, so both are kept
		private static readonly Dictionary<string, Realm> realmByCity = new Dictionary<string, Realm>(StringComparer.OrdinalIgnoreCase);
		private static readonly Dictionary<Region, string> cityByRegion = new Dictionary<Region, string>();

		/// <summary>The realm of a city: of its areas, the one with a realm.</summary>
		public static Realm RealmOfCity(string city) {
			Realm realm;
			if (!realmByCity.TryGetValue(city, out realm)) {
				foreach (FlaggedRegion area in VenueRegistry.AreasOf(city)) {
					if (area.Realm != Realm.None) {
						realm = area.Realm;
						break;
					}
				}
				realmByCity[city] = realm;
			}
			return realm;
		}

		/// <summary>The two realms at war: an act against the other realm is no crime against your own city.</summary>
		public static bool AreEnemies(Character a, Character b) {
			return (a != null) && (b != null) && (((a.Realm == Realm.Gondor) && (b.Realm == Realm.Mordor)) || ((a.Realm == Realm.Mordor) && (b.Realm == Realm.Gondor)));
		}

		private static int Start(Character player, string city) {
			if ((player.Realm == Realm.GondorOutcast) || (player.Realm == Realm.MordorOutcast)) {
				return Settings.outcastStart;
			}
			Realm cityRealm = RealmOfCity(city);
			if ((cityRealm != Realm.None) && (player.Realm != Realm.None) && (player.Realm != cityRealm)) {
				return Settings.enemyRealmStart;
			}
			return 0;
		}

		/// <summary>The city's view of the player, -1000 to 1000, with the drift since it was last changed.</summary>
		public static int Of(Character player, string city) {
			if ((player == null) || string.IsNullOrEmpty(city)) {
				return 0;
			}
			object stored = player.GetTag(CityKey(city));
			int value = (stored == null) ? Start(player, city) : Convert.ToInt32(stored, CultureInfo.InvariantCulture);
			int floor = Math.Min(0, Start(player, city)); //bad standing comes back to where a stranger of your realm starts, no further
			if (value < floor) {
				object since = player.GetTag(CityTimeKey(city));
				if (since != null) {
					double days = Math.Max(0, (Now - new DateTime(Convert.ToInt64(since, CultureInfo.InvariantCulture), DateTimeKind.Utc)).TotalSeconds / SecondsPerDay);
					value = Math.Min(floor, value + (int) (days * Settings.driftPerDay));
				}
			}
			return value;
		}

		public static int OfFaction(Character player, string faction) {
			object stored = player.GetTag(FactionKey(faction));
			return (stored == null) ? 0 : Convert.ToInt32(stored, CultureInfo.InvariantCulture);
		}

		public static ReputationLevel LevelOf(int value) {
			ReputationSettings s = Settings;
			if (value >= s.heroFrom) {
				return ReputationLevel.Hrdina;
			}
			if (value >= s.honouredFrom) {
				return ReputationLevel.Cteny;
			}
			if (value >= s.respectedFrom) {
				return ReputationLevel.Vazeny;
			}
			if (value >= s.knownFrom) {
				return ReputationLevel.Znamy;
			}
			if (value >= s.strangerFrom) {
				return ReputationLevel.Neznamy;
			}
			if (value >= s.suspectFrom) {
				return ReputationLevel.Podezrely;
			}
			return ReputationLevel.Nepritel;
		}

		/// <summary>The level a city shows: a hero with no good deed there for heroRecentDays shows as honoured.</summary>
		public static ReputationLevel Level(Character player, string city) {
			ReputationLevel level = LevelOf(Of(player, city));
			if (level == ReputationLevel.Hrdina) {
				object deed = player.GetTag(CityDeedKey(city));
				if ((deed == null) || ((Now - new DateTime(Convert.ToInt64(deed, CultureInfo.InvariantCulture), DateTimeKind.Utc)).TotalDays > Settings.heroRecentDays)) {
					return ReputationLevel.Cteny;
				}
			}
			return level;
		}

		public static ReputationLevel FactionLevel(Character player, string faction) {
			return LevelOf(OfFaction(player, faction));
		}

		/// <summary>Raises or lowers a city's view; with a faction, that faction's too, and its rival's the other way by half.
		/// Returns the city's new value.</summary>
		public static int Change(Character player, string city, int delta, string faction = null) {
			if ((player == null) || !player.IsPlayer || string.IsNullOrEmpty(city) || (delta == 0)) {
				return Of(player, city);
			}
			int now = Math.Max(Min, Math.Min(Max, Of(player, city) + delta)); //Of applies the drift so far, then it starts again
			player.SetTag(CityKey(city), now);
			player.SetTag(CityTimeKey(city), Now.Ticks);
			if (delta > 0) {
				player.SetTag(CityDeedKey(city), Now.Ticks);
			}
			Faction f = (faction == null) ? null : Faction.Get(faction);
			if (f != null) {
				SetFaction(player, f.Key, OfFaction(player, f.Key) + delta);
				if (f.Rival != null) {
					SetFaction(player, f.Rival, OfFaction(player, f.Rival) - (delta / 2));
				}
			}
			return now;
		}

		/// <summary>Sets a city's view outright (a GM, a test), without counting it as a deed.</summary>
		public static void Set(Character player, string city, int value) {
			player.SetTag(CityKey(city), Math.Max(Min, Math.Min(Max, value)));
			player.SetTag(CityTimeKey(city), Now.Ticks);
		}

		private static void SetFaction(Character player, string faction, int value) {
			player.SetTag(FactionKey(faction), Math.Max(Min, Math.Min(Max, value)));
		}

		/// <summary>The city a point is in, for the effects (VenueRegistry.CityAt, by geometry), remembered per region: guards ask
		/// it on every think.</summary>
		public static string CityAt(IPoint4D point) {
			if (point == null) {
				return null;
			}
			Region region = point.GetMap().GetRegionFor(point);
			string city;
			if ((region != null) && cityByRegion.TryGetValue(region, out city)) {
				return city;
			}
			city = VenueRegistry.CityAt(point);
			if (region != null) {
				cityByRegion[region] = city;
			}
			return city;
		}

		/// <summary>The price a player pays where they stand: by the city's view of them. 0 when nobody there sells to them.</summary>
		public static long PriceFor(Character player, long price) {
			if ((player == null) || player.IsGM || (price <= 0)) {
				return price;
			}
			string city = CityAt(player);
			if (city == null) {
				return price;
			}
			ReputationSettings s = Settings;
			int percent;
			switch (Level(player, city)) {
				case ReputationLevel.Nepritel: percent = s.priceEnemy; break;
				case ReputationLevel.Podezrely: percent = s.priceSuspect; break;
				case ReputationLevel.Neznamy: percent = s.priceStranger; break;
				case ReputationLevel.Vazeny: percent = s.priceRespected; break;
				case ReputationLevel.Cteny: percent = s.priceHonoured; break;
				case ReputationLevel.Hrdina: percent = s.priceHero; break;
				default: percent = s.priceKnown; break;
			}
			if (percent <= 0) {
				return 0;
			}
			return Math.Max(1, (price * percent + 50) / 100);
		}

		/// <summary>True (and told why) when nobody in this city sells to the player.</summary>
		public static bool Refuses(Character player) {
			if ((player == null) || player.IsGM) {
				return false;
			}
			string city = CityAt(player);
			if ((city != null) && (Settings.priceEnemy <= 0) && (Level(player, city) == ReputationLevel.Nepritel)) {
				player.RedMessage("Tady ti nikdo nic neproda. Jsi nepritelem mesta.");
				return true;
			}
			return false;
		}

		/// <summary>A guard of the player's own realm stops protecting one its city counts as an enemy. Cheap checks first: guards
		/// ask this on every think.</summary>
		public static bool IsEnemyHere(Character player, NPC guard) {
			if (!player.IsPlayer || player.IsGM || (guard.Realm == Realm.None) || (guard.Realm != player.Realm)) {
				return false;
			}
			string city = CityAt(guard);
			return (city != null) && (Level(player, city) == ReputationLevel.Nepritel);
		}

		private static readonly TagKey lastCrimeTK = TagKey.Acquire("_rep_lastcrime_");
		private static readonly TagKey warnedTK = TagKey.Acquire("_rep_warned_");

		/// <summary>From the crimes' callers (a hostile act, looting, snooping): a crime against `victim` in the city where it
		/// happened. One against the enemy realm is war, not a crime against your city. Every blow flags again, so it counts once a
		/// harmResidentEverySeconds, as one crime.</summary>
		public static void OnCrime(Character criminal, Character victim) {
			if ((criminal == null) || !criminal.IsPlayer || criminal.IsGM || AreEnemies(criminal, victim)) {
				return;
			}
			string city = CityAt(victim ?? criminal);
			if ((city != null) && Due(criminal, lastCrimeTK, Settings.harmResidentEverySeconds)) {
				Change(criminal, city, -Settings.crimeLoss);
			}
		}

		/// <summary>True, and the time noted, when the last time under `key` is at least `seconds` ago.</summary>
		private static bool Due(Character ch, TagKey key, double seconds) {
			object last = ch.GetTag(key);
			double now = Globals.TimeInSeconds;
			if ((last != null) && (now - Convert.ToDouble(last, CultureInfo.InvariantCulture) < seconds)) {
				return false;
			}
			ch.SetTag(key, now);
			return true;
		}

		/// <summary>From FlaggedRegion.On_Enter: a suspect walking into a city is told the guards watch them, by a guard if one is
		/// near; once in five minutes, as a city is many areas.</summary>
		public static void OnEnterCity(Player player, FlaggedRegion area) {
			if (player.IsGM || string.IsNullOrEmpty(area.City) || (Level(player, area.City) != ReputationLevel.Podezrely) || !Due(player, warnedTK, 300)) {
				return;
			}
			foreach (AbstractCharacter ch in player.GetMap().GetCharsInRange(player.X, player.Y, 18)) {
				NPC guard = ch as NPC;
				if ((guard != null) && GuardSpeech.IsGuard(guard) && (guard.Realm == player.Realm)) {
					guard.Say("Mam te na ocich, " + player.Name + ".");
					return;
				}
			}
			player.RedMessage("Straze mesta " + area.City.Replace('_', ' ') + " si te prohlizeji.");
		}

		/// <summary>From Notoriety.RegisterMurder: a murder in the city where the victim fell.</summary>
		public static void OnMurder(Character victim, Character killer) {
			if (!AreEnemies(victim, killer)) { //killing the enemy realm's raiders defends the city
				Change(killer, CityAt(victim), -Settings.murderLoss);
			}
		}

		/// <summary>Whoever answers for an act: a player, or the owner of a pet.</summary>
		private static Character Answerable(Character ch) {
			if ((ch != null) && !ch.IsPlayer && (ch.ControlMaster != null) && ch.ControlMaster.IsPlayer) {
				return ch.ControlMaster;
			}
			return ch;
		}

		private static readonly TagKey lastHarmTK = TagKey.Acquire("_rep_lastharm_");

		/// <summary>From DamageManager: a player hurting a resident, at most once a harmResidentEverySeconds.</summary>
		public static void OnHarm(Character attacker, Character defender) {
			attacker = Answerable(attacker); //a pet set on a resident is its owner's doing
			if ((attacker == null) || !attacker.IsPlayer || attacker.IsGM || (Residents.GemOf(defender) == null)) {
				return;
			}
			if (Due(attacker, lastHarmTK, Settings.harmResidentEverySeconds)) {
				Change(attacker, CityAt(defender), -Settings.harmResidentLoss);
			}
		}

		/// <summary>From NPC.On_Death: a player killing a resident.</summary>
		public static void OnDeath(NPC dead, Character killedBy) {
			killedBy = Answerable(killedBy);
			if ((killedBy != null) && killedBy.IsPlayer && !killedBy.IsGM && (Residents.GemOf(dead) != null)) {
				Change(killedBy, CityAt(dead), -Settings.killResidentLoss);
			}
		}

		/// <summary>The cities shown in .povest: the pilot cities and any the player has a standing in.</summary>
		public static List<string> CitiesOf(Character player) {
			List<string> cities = new List<string> { "Minas_Tirith", "Edoras", "Hobitin", "Barad-dur", "Harad", "Umbar" };
			foreach (FlaggedRegion area in AllCityAreas()) {
				if (!cities.Contains(area.City) && (player.GetTag(CityKey(area.City)) != null)) {
					cities.Add(area.City);
				}
			}
			return cities;
		}

		private static IEnumerable<FlaggedRegion> AllCityAreas() {
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area != null) && !string.IsNullOrEmpty(area.City)) {
					yield return area;
				}
			}
		}

		public static string LevelName(ReputationLevel level) {
			switch (level) {
				case ReputationLevel.Nepritel: return "Nepritel";
				case ReputationLevel.Podezrely: return "Podezrely";
				case ReputationLevel.Neznamy: return "Neznamy";
				case ReputationLevel.Znamy: return "Znamy";
				case ReputationLevel.Vazeny: return "Vazeny";
				case ReputationLevel.Cteny: return "Cteny";
				default: return "Hrdina mesta";
			}
		}

		/// <summary>.povest: the player's standing in each city and faction.</summary>
		[SteamFunction]
		public static void Povest(Character self) {
			self.Dialog(self, SingletonScript<D_Reputation>.Instance, new DialogArgs());
		}
	}

	/// <summary>#1243 - .povest: cities and their factions, the level and the value. The journal's Mesto tab (#1237 step 4) shows it too.</summary>
	public class D_Reputation : CompiledGumpDef {
		private const int Width = 460, LabelHue = 955, ValueHue = 52, BadHue = 33, RowHeight = 20;

		private const int CitiesOnPage = 2, PageHeight = 260;

		/// <summary>Two cities a page with their factions: all of them at once would be taller than a 3.0.6m screen.</summary>
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character player = (Character) sendTo;
			List<string> cities = Reputation.CitiesOf(player);
			int pages = (cities.Count + CitiesOnPage - 1) / CitiesOnPage;
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, PageHeight, 2620);
			this.AddLabel(14, 12, ValueHue, "Povest ve mestech");
			int y = 40;
			for (int i = 0; i < cities.Count; i++) {
				string city = cities[i];
				if ((i % CitiesOnPage) == 0) {
					int page = (i / CitiesOnPage) + 1;
					this.AddPage(page);
					y = 40;
					if (page > 1) {
						this.AddButton(Width - 70, 12, 0x15E3, 0x15E7, 0, GumpButtonType.Page, page - 1);
					}
					if (page < pages) {
						this.AddButton(Width - 40, 12, 0x15E1, 0x15E5, 0, GumpButtonType.Page, page + 1);
					}
				}
				int value = Reputation.Of(player, city);
				ReputationLevel level = Reputation.Level(player, city);
				this.AddLabel(14, y, LabelHue, city.Replace('_', ' '));
				this.AddLabel(200, y, (level <= ReputationLevel.Podezrely) ? BadHue : ValueHue, Reputation.LevelName(level));
				this.AddLabel(340, y, LabelHue, value.ToString(CultureInfo.InvariantCulture));
				y += RowHeight;
				foreach (Faction faction in Faction.All) {
					if (faction.City == city) {
						this.AddLabel(30, y, LabelHue, faction.Name);
						this.AddLabel(200, y, ValueHue, Reputation.LevelName(Reputation.FactionLevel(player, faction.Key)));
						this.AddLabel(340, y, LabelHue, Reputation.OfFaction(player, faction.Key).ToString(CultureInfo.InvariantCulture));
						y += RowHeight;
					}
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
