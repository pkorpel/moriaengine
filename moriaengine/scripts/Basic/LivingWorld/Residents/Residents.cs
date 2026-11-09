using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1242 - residents of the living cities. A resident is a world piece: a spawn gem of one that carries who the resident
	/// is (name, sex, role, workplace and home venue, traits), so a killed resident comes back as the same person and a rebuilt
	/// world gets them back from the file. The NPC is only the body: SpawnedPieces.Prepare calls Prepare for each one spawned.
	/// What a resident thinks of a player is kept on the player (Friendship), so it outlives the body too.</summary>
	public static class Residents {
		private static readonly TagKey nameTK = TagKey.Acquire("_res_name_");
		private static readonly TagKey femaleTK = TagKey.Acquire("_res_female_");
		private static readonly TagKey roleTK = TagKey.Acquire("_res_role_");
		private static readonly TagKey venueTK = TagKey.Acquire("_res_venue_");
		private static readonly TagKey homeTK = TagKey.Acquire("_res_home_");
		private static readonly TagKey placedTK = TagKey.Acquire("_res_placed_"); //#1315 - x,y where the gem was put when made
		private static readonly TagKey traitsTK = TagKey.Acquire("_res_traits_");
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static readonly TagKey pieceIdTK = TagKey.Acquire("_worldPieceId_");
		private static readonly TagKey bodyTK = TagKey.Acquire("_res_body_");

		/// <summary>A resident's traits: what a schedule and later errands ask of them.</summary>
		public static readonly string[] AllTraits = { "spolecensky", "zbozny", "morous", "uzvaneny", "lakomy", "romanticky", "zvedavy", "pracovity" };

		public const int MaxFriendship = 10;

		private static List<Item> all;

		/// <summary>Every resident's gem, from the world on first use.</summary>
		public static IList<Item> AllGems {
			get {
				if (all == null) {
					all = new List<Item>();
					foreach (Thing thing in Thing.AllThings) {
						Item gem = thing as Item;
						if ((gem != null) && !gem.IsDeleted && IsResidentGem(gem)) {
							all.Add(gem);
						}
					}
				}
				all.RemoveAll(g => g.IsDeleted);
				return all;
			}
		}

		public static void Reset() {
			all = null;
		}

		public static bool IsResidentGem(Thing gem) {
			return (gem != null) && (gem.GetTag(roleTK) != null);
		}

		public static string IdOf(Thing gem) {
			return gem.GetTag(pieceIdTK) as string;
		}

		public static string NameOf(Thing gem) {
			return (gem.GetTag(nameTK) as string) ?? "";
		}

		public static bool IsFemale(Thing gem) {
			return gem.GetTag(femaleTK) != null;
		}

		public static string RoleKeyOf(Thing gem) {
			return gem.GetTag(roleTK) as string;
		}

		public static string VenueIdOf(Thing gem) {
			return gem.GetTag(venueTK) as string;
		}

		/// <summary>#1315 - a gem made before #1315 has no record of where it was put: where it stands when first seen is that, so
		/// its computed spot shifting later (furniture added: SpotsOf's CanFit) never pins it as moved.</summary>
		internal static void EnsurePlaced(Item gem) {
			if ((gem.GetTag(placedTK) == null) && (gem.Cont == null)) {
				gem.SetTag(placedTK, gem.X.ToString(CultureInfo.InvariantCulture) + "," + gem.Y.ToString(CultureInfo.InvariantCulture));
			}
		}

		/// <summary>#1315 - x,y where the gem was put when made, or null (made before #1315).</summary>
		internal static int[] PlacedAt(Item gem) {
			string[] xy = (gem.GetTag(placedTK) as string ?? "").Split(',');
			int x, y;
			if ((xy.Length == 2) && int.TryParse(xy[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x) && int.TryParse(xy[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y)) {
				return new[] { x, y };
			}
			return null;
		}

		public static string HomeIdOf(Thing gem) {
			return gem.GetTag(homeTK) as string;
		}

		public static void SetHome(Thing gem, string homeVenueId) {
			if (string.IsNullOrEmpty(homeVenueId)) {
				gem.RemoveTag(homeTK);
			} else {
				gem.SetTag(homeTK, homeVenueId);
			}
		}

		public static string[] TraitsOf(Thing gem) {
			string traits = gem.GetTag(traitsTK) as string;
			return string.IsNullOrEmpty(traits) ? new string[0] : traits.Split(',');
		}

		public static void SetTraits(Thing gem, string traits) {
			gem.SetTag(traitsTK, traits ?? "");
		}

		public static bool HasTrait(Thing gem, string trait) {
			return Array.IndexOf(TraitsOf(gem), trait) >= 0;
		}

		public static CityVenue WorkplaceOf(Thing gem) {
			return VenueRegistry.ById(VenueIdOf(gem));
		}

		public static VenueRole RoleOf(Thing gem) {
			CityVenue venue = WorkplaceOf(gem);
			VenueType type = (venue == null) ? null : venue.VenueType;
			if (type != null) {
				foreach (VenueRole role in type.Roles) {
					if (role.Key == RoleKeyOf(gem)) {
						return role;
					}
				}
			}
			return null;
		}

		/// <summary>The resident's gem behind an NPC, or null when the NPC isn't a resident.</summary>
		public static Item GemOf(Character npc) {
			Item gem = (npc == null) ? null : npc.GetTag(spawnGemTK) as Item;
			return IsResidentGem(gem) ? gem : null;
		}

		/// <summary>The resident's living body, or null while dead (the gem spawns another). Prepare keeps it on the gem, so the
		/// scheduler doesn't search the map for a thousand residents every round.</summary>
		public static NPC BodyOf(Item gem) {
			NPC npc = gem.GetTag(bodyTK) as NPC;
			if ((npc != null) && !npc.IsDeleted && !npc.Flag_Dead && (npc.GetTag(spawnGemTK) == gem)) {
				return npc;
			}
			foreach (AbstractCharacter ch in gem.GetMap().GetCharsInRange(gem.X, gem.Y, 24)) { //a body made before the gem knew it
				npc = ch as NPC;
				if ((npc != null) && !npc.IsDeleted && !npc.Flag_Dead && (npc.GetTag(spawnGemTK) == gem)) {
					gem.SetTag(bodyTK, npc);
					return npc;
				}
			}
			return null;
		}

		public static List<Item> OfVenue(string venueId) {
			List<Item> list = new List<Item>();
			foreach (Item gem in AllGems) {
				if (string.Equals(VenueIdOf(gem), venueId, StringComparison.OrdinalIgnoreCase)) {
					list.Add(gem);
				}
			}
			return list;
		}

		/// <summary>A venue's residents gone, gems and bodies. Returns how many.</summary>
		public static int RemoveOfVenue(string venueId) {
			List<Item> gems = OfVenue(venueId);
			foreach (Item gem in gems) {
				NPC body = BodyOf(gem);
				if (body != null) {
					body.Delete();
				}
				gem.Delete();
			}
			return gems.Count;
		}

		public const int MaxRoleCount = 10;

		private static TagKey CountKey(string roleKey) {
			return TagKey.Acquire("_venue_n_" + roleKey);
		}

		/// <summary>#1315 - how many of a role this venue wants: the GM's number for it (D_VenueRoles, a tag on the venue), else
		/// the type's. A small house of healing needs fewer people than the type says.</summary>
		public static int CountOf(CityVenue venue, VenueRole role) {
			object n = venue.GetTag(CountKey(role.Key));
			return (n == null) ? role.Count : Convert.ToInt32(n, CultureInfo.InvariantCulture);
		}

		public static void SetCount(CityVenue venue, VenueRole role, int count) {
			count = Math.Max(0, Math.Min(MaxRoleCount, count));
			if (count == role.Count) {
				venue.RemoveTag(CountKey(role.Key));
			} else {
				venue.SetTag(CountKey(role.Key), count);
			}
		}

		/// <summary>#1315 - back to the type's counts (the venue's type changed).</summary>
		public static void ClearCounts(CityVenue venue) {
			List<TagKey> counts = new List<TagKey>();
			foreach (KeyValuePair<TagKey, object> tag in venue.GetAllTags()) {
				if (tag.Key.Name.StartsWith("_venue_n_", StringComparison.Ordinal)) {
					counts.Add(tag.Key);
				}
			}
			foreach (TagKey key in counts) {
				venue.RemoveTag(key);
			}
		}

		/// <summary>#1315 - whether a resident of this id belongs in the venue by its type and counts now (a setup file from before a
		/// count was lowered doesn't bring the extra one back).</summary>
		public static bool Fits(CityVenue venue, string roleKey, string id) {
			VenueRole role = (venue.VenueType == null) ? null : Array.Find(venue.VenueType.Roles, r => r.Key == roleKey);
			int n;
			return (role != null) && int.TryParse(id.Substring(id.LastIndexOf('/') + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && (n <= CountOf(venue, role));
		}

		/// <summary>All the people the venue wants, by its counts.</summary>
		public static int RoleCountOf(CityVenue venue) {
			int count = 0;
			if (venue.VenueType != null) {
				foreach (VenueRole role in venue.VenueType.Roles) {
					count += CountOf(venue, role);
				}
			}
			return count;
		}

		/// <summary>#1315 - the venue's own counts for .venues export: "healer=1,patient=0", only those not the type's.</summary>
		public static string CountsText(CityVenue venue) {
			List<string> counts = new List<string>();
			if (venue.VenueType != null) {
				foreach (VenueRole role in venue.VenueType.Roles) {
					if (venue.GetTag(CountKey(role.Key)) != null) {
						counts.Add(role.Key + "=" + CountOf(venue, role).ToString(CultureInfo.InvariantCulture));
					}
				}
			}
			return string.Join(",", counts);
		}

		public static void ApplyCounts(CityVenue venue, string text) {
			if ((venue.VenueType == null) || string.IsNullOrEmpty(text)) {
				return;
			}
			foreach (string pair in text.Split(',')) {
				string[] kv = pair.Split('=');
				int n;
				if ((kv.Length == 2) && int.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) {
					foreach (VenueRole role in venue.VenueType.Roles) {
						if (role.Key == kv[0].Trim()) {
							SetCount(venue, role, n);
						}
					}
				}
			}
		}

		/// <summary>After a venue's type changed: residents of roles the new type doesn't have, or beyond a role's count (#1315: the
		/// venue's count), leave. Returns how many.</summary>
		public static int RemoveStrayRoles(CityVenue venue) {
			int gone = 0;
			foreach (Item gem in OfVenue(venue.VenueId)) {
				VenueRole role = RoleOf(gem);
				string id = IdOf(gem) ?? "";
				int n;
				bool beyond = (role != null) && int.TryParse(id.Substring(id.LastIndexOf('/') + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && (n > CountOf(venue, role));
				if ((role == null) || beyond) {
					NPC body = BodyOf(gem);
					if (body != null) {
						body.Delete();
					}
					gem.Delete();
					gone++;
				}
			}
			return gone;
		}

		public static void Rename(Item gem, string name) {
			gem.SetTag(nameTK, name);
			NPC body = BodyOf(gem);
			if (body != null) {
				body.Name = name;
			}
		}

		/// <summary>From SpawnedPieces.Prepare, for every body a resident's gem spawns: who it is, and where it should be now.</summary>
		public static void Prepare(Thing gem, Character body) {
			if (!IsResidentGem(gem)) {
				return;
			}
			body.Name = NameOf(gem);
			gem.SetTag(bodyTK, body);
			CityFlavour.Dress((Item) gem, body); //#1282 - Harad's people are dark-skinned
			VenueRole role = RoleOf((Item) gem);
			if (role != null) {
				body.Title = role.Name.ToLowerInvariant();
			}
			NPC npc = body as NPC;
			if (npc != null) {
				EnsureRoleAbilities((Item) gem, npc);
				ResidentSchedule.Apply((Item) gem, npc, true);
			}
			ResidentSchedule.EnsureRunning();
		}

		/// <summary>#1315 - the healing venue's healer and herbalist are healers: a ghost clicking one is brought back (e_healer,
		/// as the archive's healer NPCs); the patients aren't.</summary>
		public static bool IsHealerRole(Item gem) {
			CityVenue work = WorkplaceOf(gem);
			string role = RoleKeyOf(gem);
			return (work != null) && (work.VenueType != null) && (work.VenueType.Key == "healing") && ((role == "healer") || (role == "herbalist"));
		}

		/// <summary>#1315 - what the role gives the body: a fresh one (Prepare), and one spawned before the role gave it (the scheduler).</summary>
		internal static void EnsureRoleAbilities(Item gem, NPC npc) {
			AbilityDef healer = MonsterSpecialAttacks.HealerDef;
			if ((healer != null) && IsHealerRole(gem) && !npc.HasAbility(healer, out _)) {
				npc.SetRealAbilityPoints(healer, 1);
			}
		}

		/// <summary>The roles of a venue that have nobody yet, filled: the count each role wants, by ids that stay
		/// (venue id/role/n), so filling again adds only what's missing. Returns how many were added.</summary>
		public static int FillRoles(CityVenue venue) {
			return FillRoles(venue, null);
		}

		/// <summary>One WorldPieces for many venues: it reads the whole world when made.</summary>
		public static int FillRoles(CityVenue venue, WorldPieces pieces) {
			VenueType type = venue.VenueType;
			if ((type == null) || (venue.VenueId == null)) {
				return 0;
			}
			pieces = pieces ?? new WorldPieces();
			int added = 0;
			List<string> namesInCity = NamesInCity(venue.City);
			foreach (VenueRole role in type.Roles) {
				for (int n = 1; n <= CountOf(venue, role); n++) {
					string id = venue.VenueId + "/" + role.Key + "/" + n.ToString(CultureInfo.InvariantCulture);
					if (ById(id) != null) {
						continue;
					}
					bool female = !IsOrcCity(venue.City) && (Globals.dice.Next(2) == 0);
					string name = PickName(venue.City, female, namesInCity);
					namesInCity.Add(name);
					Create(pieces, id, venue, role.Key, female, name, PickHome(venue), PickTraits());
					added++;
				}
			}
			return added;
		}

		/// <summary>One resident's gem with its identity, and its first body named at once.</summary>
		public static Item Create(WorldPieces pieces, string id, CityVenue venue, string roleKey, bool female, string name, string homeId, string traits) {
			int[] spot = ResidentSchedule.SpotIn(venue, id);
			if (spot == null) {
				spot = new[] { venue.X, venue.Y, venue.Z };
			}
			string defname = IsOrcCity(venue.City) ? "c_resident_orc" : (female ? "c_resident_woman" : "c_resident_man");
			Item gem = pieces.Npc(id, defname, spot, venue.M, RealmOf(venue), false, ResidentSchedule.LeashInside, false);
			if (string.IsNullOrWhiteSpace(name)) {
				name = female ? "Obyvatelka" : "Obyvatel";
			}
			gem.SetTag(nameTK, name);
			if (female) {
				gem.SetTag(femaleTK, 1);
			} else {
				gem.RemoveTag(femaleTK);
			}
			gem.SetTag(roleTK, roleKey);
			gem.SetTag(venueTK, venue.VenueId);
			SetHome(gem, homeId);
			gem.SetTag(traitsTK, traits ?? "");
			if (gem.GetTag(placedTK) == null) {
				gem.SetTag(placedTK, gem.X.ToString(CultureInfo.InvariantCulture) + "," + gem.Y.ToString(CultureInfo.InvariantCulture));
			}
			if (all != null && !all.Contains(gem)) {
				all.Add(gem);
			}
			NPC body = BodyOf(gem);
			if (body != null) { //spawned before the gem knew who it is
				Prepare(gem, body);
			}
			return gem;
		}

		public static Item ById(string id) {
			foreach (Item gem in AllGems) {
				if (string.Equals(IdOf(gem), id, StringComparison.OrdinalIgnoreCase)) {
					return gem;
				}
			}
			return null;
		}

		/// <summary>Barad-dur's folk are orcs; Harad and Umbar are cities of Men.</summary>
		public static bool IsOrcCity(string city) {
			return string.Equals(city, "Barad-dur", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>The realm of the city's own area holding the venue, by geometry as VenueRegistry.CityAt: a house's Parent is often
		/// the territory around the city, not the city.</summary>
		private static Realm RealmOf(CityVenue venue) {
			Realm realm = Realm.None;
			if (venue.City == null) {
				return realm; //a venue outside any city has no realm to take
			}
			int smallest = int.MaxValue;
			Point2D p = new Point2D(venue.X, venue.Y);
			foreach (FlaggedRegion area in VenueRegistry.AreasOf(venue.City)) {
				if ((area.Mapplane == venue.M) && area.ContainsPoint(p) && (area.Realm != Realm.None)) {
					int tiles = 0;
					foreach (ImmutableRectangle rect in area.Rectangles) {
						tiles += (rect.Width + 1) * (rect.Height + 1);
					}
					if (tiles < smallest) {
						smallest = tiles;
						realm = area.Realm;
					}
				}
			}
			return realm;
		}

		internal static string NameList(string city, bool female) {
			if (IsOrcCity(city)) {
				return "NAMES_ORC";
			}
			if (string.Equals(city, "Hobitin", StringComparison.OrdinalIgnoreCase)) {
				return female ? "NAMES_HOBBIT_FEMALE" : "NAMES_HOBBIT_MALE";
			}
			if (string.Equals(city, CityFlavour.Harad, StringComparison.OrdinalIgnoreCase)) {
				return female ? "NAMES_HARAD_FEMALE" : "NAMES_HARAD_MALE"; //#1282
			}
			if (string.Equals(city, CityFlavour.Umbar, StringComparison.OrdinalIgnoreCase)) {
				return female ? "NAMES_UMBAR_FEMALE" : "NAMES_UMBAR_MALE";
			}
			switch (CityFlavour.FolkOf(city)) { //#1304 - the other cities' folk
				case CityFolk.Elves: return female ? "NAMES_ELF_FEMALE" : "NAMES_ELF_MALE";
				case CityFolk.Dwarves: return female ? "NAMES_DWARF_FEMALE" : "NAMES_DWARF_MALE";
				case CityFolk.Easterlings: return female ? "NAMES_EASTERLING_FEMALE" : "NAMES_EASTERLING_MALE";
			}
			return female ? "NAMES_HUMANFEMALE" : "NAMES_HUMANMALE";
		}

		/// <summary>A name of the city's list not yet used in that city, if one comes up in a few tries.</summary>
		private static string PickName(string city, bool female, List<string> taken) {
			string list = NameList(city, female);
			string name = null;
			for (int i = 0; i < 12; i++) {
				name = SteamEngine.CompiledScripts.NameList.Random(list) ?? (female ? "Obyvatelka" : "Obyvatel");
				if (!taken.Contains(name)) {
					break;
				}
			}
			return name;
		}

		private static List<string> NamesInCity(string city) {
			List<string> names = new List<string>();
			foreach (Item gem in AllGems) {
				CityVenue venue = WorkplaceOf(gem);
				if ((venue != null) && string.Equals(venue.City, city, StringComparison.OrdinalIgnoreCase)) {
					names.Add(NameOf(gem));
				}
			}
			return names;
		}

		/// <summary>The city's home with the fewest residents, or none: then the workplace is home too.</summary>
		private static string PickHome(CityVenue workplace) {
			CityVenue best = null;
			int fewest = int.MaxValue;
			foreach (CityVenue venue in VenueRegistry.InCity(workplace.City)) {
				if ((venue.VenueType != null) && (venue.VenueType.Key == "house")) {
					int count = 0;
					foreach (Item gem in AllGems) {
						count += string.Equals(HomeIdOf(gem), venue.VenueId, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
					}
					if (count < fewest) {
						fewest = count;
						best = venue;
					}
				}
			}
			return (best == null) ? null : best.VenueId;
		}

		private static string PickTraits() {
			int first = Globals.dice.Next(AllTraits.Length), second = Globals.dice.Next(AllTraits.Length - 1);
			if (second >= first) {
				second++;
			}
			return AllTraits[first] + "," + AllTraits[second];
		}

		/// <summary>What resident `residentId` thinks of the player, 0 to 10. Kept on the player, so it survives the resident's death.</summary>
		public static int Friendship(Character player, string residentId) {
			object value = player.GetTag(FriendshipKey(residentId));
			return (value == null) ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		public static int AddFriendship(Character player, string residentId, int delta) {
			int now = Math.Max(0, Math.Min(MaxFriendship, Friendship(player, residentId) + delta));
			if (now == 0) {
				player.RemoveTag(FriendshipKey(residentId));
			} else {
				player.SetTag(FriendshipKey(residentId), now);
			}
			return now;
		}

		/// <summary>Only word characters: a saved tag's name is read back by \w+ (TagHolder), and an id holds slashes, so
		/// venue/Edoras/1/innkeeper/1 becomes _rf_venue_edoras_1_innkeeper_1.</summary>
		internal static TagKey FriendshipKey(string residentId) {
			System.Text.StringBuilder key = new System.Text.StringBuilder("_rf_");
			foreach (char c in residentId.ToLowerInvariant()) {
				key.Append(((c >= 'a') && (c <= 'z')) || ((c >= '0') && (c <= '9')) ? c : '_');
			}
			return TagKey.Acquire(key.ToString());
		}
	}

	/// <summary>#1242 - where a resident is at each hour: at work in the role's shifts, some evenings at the tavern (a sociable one)
	/// or the temple (a pious one), otherwise at home, which is the workplace when the city has no home venue. Moving the NPC's
	/// home point is all it takes: the AI walks home within its small leash. Where no player is near (NPC sleep) the body is put
	/// there at once, so an area wakes with everyone in place.</summary>
	public static class ResidentSchedule {
		private static readonly TimerKey schedulerTK = TimerKey.Acquire("_resident_scheduler_");
		public const double TickSeconds = 20;
		internal const int LeashInside = 2, LeashOpenAir = 4;

		private sealed class VenueSpots {
			internal IList<ImmutableRectangle> rects;
			internal int zMin, zMax;
			internal List<int[]> spots;
		}

		private static readonly Dictionary<int, VenueSpots> spotsByVenue = new Dictionary<int, VenueSpots>();

		public static void EnsureRunning() {
			if (!Globals.Instance.HasTimer(schedulerTK)) {
				ResidentSchedulerTimer timer = new ResidentSchedulerTimer();
				Globals.Instance.AddTimer(schedulerTK, timer);
				timer.DueInSeconds = TickSeconds;
				timer.PeriodInSeconds = TickSeconds;
			}
		}

		/// <summary>The venue a resident belongs in during the given shift.</summary>
		public static CityVenue PlaceFor(Item gem, VenueShift shift) {
			CityVenue work = Residents.WorkplaceOf(gem);
			VenueRole role = Residents.RoleOf(gem);
			if ((work != null) && (role != null) && ((role.Shift & shift) != 0)) {
				return work;
			}
			string city = (work == null) ? null : work.City;
			if ((shift == VenueShift.Evening) && (city != null)) {
				if (Residents.HasTrait(gem, "spolecensky")) {
					CityVenue tavern = FirstOfType(city, "tavern", "messhall");
					if (tavern != null) {
						return tavern;
					}
				}
				if (Residents.HasTrait(gem, "zbozny")) {
					CityVenue temple = FirstOfType(city, "temple", null);
					if (temple != null) {
						return temple;
					}
				}
			}
			CityVenue home = VenueRegistry.ById(Residents.HomeIdOf(gem));
			return home ?? work;
		}

		private static CityVenue FirstOfType(string city, string key, string orKey) {
			foreach (CityVenue venue in VenueRegistry.InCity(city)) {
				VenueType type = venue.VenueType;
				if ((type != null) && ((type.Key == key) || (type.Key == orKey))) {
					return venue;
				}
			}
			return null;
		}

		/// <summary>Moves the resident's home point to its place for now; puts the body there at once where no player is near,
		/// on another plane, or when `place` is set (a fresh body).</summary>
		public static void Apply(Item gem, NPC npc, bool place) {
			if (Errands.HoldsFollower(npc)) {
				return; //#1252 - walking with a player on an errand: the day resumes when the errand lets them go
			}
			CityVenue venue = PlaceFor(gem, VenueClock.Shift);
			if (venue == null) {
				return;
			}
			int[] spot = SpotFor(gem, venue);
			if (spot == null) {
				return;
			}
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			if (ai != null) {
				ai.homeX = spot[0];
				ai.homeY = spot[1];
				ai.homeZ = (sbyte) spot[2];
				ai.homeM = venue.M;
				ai.hasHome = true;
				ai.WanderLeash = venue.IsOpenAir ? LeashOpenAir : LeashInside;
			}
			if (venue.Contains(npc)) {
				return;
			}
			double now = Globals.TimeInSeconds;
			bool bodyAsleep = !npc.GetMap().Activity.IsAwake(npc.X, npc.Y, now);
			bool placeAsleep = !Map.GetMap(venue.M).Activity.IsAwake(spot[0], spot[1], now);
			Point4D target = new Point4D(spot[0], spot[1], spot[2], venue.M);
			if (place || (npc.M != venue.M) || (bodyAsleep && placeAsleep)) {
				npc.Go(target); //nobody sees it happen
				return;
			}
			if (bodyAsleep) { //players wait where it's going: it comes in from out of their sight, on foot
				Point4D staging = OutOfSight(npc, target);
				if (staging == null) {
					npc.Go(target);
					return;
				}
				npc.Go(staging);
			}
			npc.PetGoalPoint = target; //walks at the follow pace, and an NPC going somewhere doesn't fall asleep
			NPCAiTimer.WakeUp(npc);
		}

		private const int SightRange = 20, StagingDistance = 24;

		/// <summary>A tile about StagingDistance from the target toward where the body is, that no player sees, or null.</summary>
		private static Point4D OutOfSight(NPC npc, Point4D target) {
			Map map = target.GetMap();
			double dx = npc.X - target.X, dy = npc.Y - target.Y, length = Math.Sqrt((dx * dx) + (dy * dy));
			if (length < 1) {
				return null;
			}
			int sx = target.X + (int) Math.Round(dx * StagingDistance / length), sy = target.Y + (int) Math.Round(dy * StagingDistance / length);
			int[] spot = WorldSpots.NearestStandable(map, sx, sy, 6);
			if (spot == null) {
				return null;
			}
			foreach (AbstractCharacter ch in map.GetCharsInRange(spot[0], spot[1], SightRange)) {
				if (ch.IsPlayer) {
					return null;
				}
			}
			return new Point4D(spot[0], spot[1], spot[2], target.M);
		}

		/// <summary>#1315 - the resident's spot in this venue: at work, where a GM moved its gem to (MovedSpot); else its own spot
		/// (SpotIn). A new body after a death keeps a moved one.</summary>
		public static int[] SpotFor(Item gem, CityVenue venue) {
			if (venue == Residents.WorkplaceOf(gem)) {
				int[] moved = MovedSpot(gem);
				if (moved != null) {
					return moved;
				}
			}
			return SpotIn(venue, Residents.IdOf(gem));
		}

		/// <summary>#1315 - where a GM moved the gem inside its workplace, or null: the gem on the ground in the workplace, away from
		/// where it was put when made (a gem from before #1315 has no record of that: away from its own spot). Editing the venue's
		/// area moves SpotIn, not the gem, so it doesn't count as moved.</summary>
		public static int[] MovedSpot(Item gem) {
			CityVenue work = Residents.WorkplaceOf(gem);
			if ((work == null) || (gem.Cont != null) || (gem.M != work.M) || !work.Contains(gem.X, gem.Y, gem.Z)) {
				return null;
			}
			int[] placed = Residents.PlacedAt(gem) ?? SpotIn(work, Residents.IdOf(gem));
			if ((placed != null) && (placed[0] == gem.X) && (placed[1] == gem.Y)) {
				return null;
			}
			return new int[] { gem.X, gem.Y, gem.Z };
		}

		/// <summary>A tile of the venue a character can stand on, the same one for the same resident: residents of one venue spread
		/// over it rather than piling on its middle.</summary>
		public static int[] SpotIn(CityVenue venue, string residentId) {
			List<int[]> spots = SpotsOf(venue);
			if (spots.Count == 0) {
				return null;
			}
			int hash = 0;
			foreach (char c in residentId ?? "") {
				hash = unchecked((hash * 31) + c);
			}
			return spots[(hash & 0x7FFFFFFF) % spots.Count];
		}

		/// <summary>For tests: spots are found again on next use.</summary>
		internal static void ForgetSpots() {
			spotsByVenue.Clear();
		}

		/// <summary>Kept per venue until its area changes (the rectangles list is a new object then).</summary>
		private static List<int[]> SpotsOf(CityVenue venue) {
			VenueSpots cached;
			IList<ImmutableRectangle> rects = venue.Rectangles;
			if (spotsByVenue.TryGetValue(venue.Uid, out cached) && ReferenceEquals(cached.rects, rects) && (cached.zMin == venue.ZMin) && (cached.zMax == venue.ZMax)) {
				return cached.spots;
			}
			List<int[]> spots = new List<int[]>();
			Map map = venue.GetMap();
			foreach (ImmutableRectangle rect in rects) {
				for (int x = rect.MinX; x <= rect.MaxX; x++) {
					for (int y = rect.MinY; y <= rect.MaxY; y++) {
						int z;
						if (map.IsValidPos(x, y) && FloorIn(venue, map, x, y, out z)) {
							spots.Add(new[] { x, y, z });
						}
					}
				}
			}
			spotsByVenue[venue.Uid] = new VenueSpots { rects = rects, zMin = venue.ZMin, zMax = venue.ZMax, spots = spots };
			return spots;
		}

		/// <summary>A floor at x,y inside the venue's heights: the ground under the open sky (not a roof), indoors the floor of the
		/// venue's own storey, which needn't be the ground one.</summary>
		private static bool FloorIn(CityVenue venue, Map map, int x, int y, out int z) {
			if (venue.IsOpenAir) {
				z = WorldSpots.LowestStandZ(map, x, y);
				return map.CanFit(x, y, z, 16, true, false, true);
			}
			z = map.GetTileZ(x, y);
			if (venue.Contains(x, y, z) && map.CanFit(x, y, z, 16, true, false, true)) {
				return true;
			}
			foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				int top = s.Z + s.Height;
				if (venue.Contains(x, y, top) && map.CanFit(x, y, top, 16, true, false, true)) {
					z = top;
					return true;
				}
			}
			return false;
		}

		/// <summary>One round for every resident with a body: the scheduler's tick, and tests'.</summary>
		public static int ApplyAll() {
			int moved = 0;
			foreach (Item gem in new List<Item>(Residents.AllGems)) {
				NPC npc = Residents.BodyOf(gem);
				if (npc != null) {
					try {
						Residents.EnsurePlaced(gem);
						Residents.EnsureRoleAbilities(gem, npc);
						Apply(gem, npc, false);
						moved++;
					} catch (FatalException) {
						throw;
					} catch (Exception e) {
						Logger.WriteError(e); //one broken resident mustn't stop the city
					}
				}
			}
			return moved;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class ResidentSchedulerTimer : BoundTimer {
		[LoadingInitializer, DeepCopyImplementation]
		public ResidentSchedulerTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			ResidentSchedule.ApplyAll();
			try {
				Theatre.Tick(); //#1256 - the theatres' plays at dusk
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
			try {
				AmbientLife.Tick(); //#1262 - barks, conversations, the crier
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}
	}
}
