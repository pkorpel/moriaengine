using System;
using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class HouseDef {
	}

	/// <summary>Erebor's order, hm_findrank. A blocked house gives everybody but a GM Blocked.</summary>
	public enum HouseRank {
		Blocked = -1, Public = 0, Friend = 1, CoOwner = 2, Owner = 3, GM = 4
	}

	/// <summary>#941 - a house of Erebor (systems/housemenu.scp): a building on the map, a region with a house number, and
	/// this hidden gem holding who it belongs to. Everything is in tags, which the save takes care of.</summary>
	[ViewableClass]
	public partial class House {
		private static readonly TagKey ownerTK = TagKey.Acquire("_house_owner_");
		private static readonly TagKey friendsTK = TagKey.Acquire("_house_friends_");
		private static readonly TagKey coOwnersTK = TagKey.Acquire("_house_coowners_");
		private static readonly TagKey houseNameTK = TagKey.Acquire("_house_name_");
		private static readonly TagKey rentTK = TagKey.Acquire("_house_rent_");
		private static readonly TagKey paidUntilTK = TagKey.Acquire("_house_paid_until_");
		private static readonly TagKey manualRentTK = TagKey.Acquire("_house_manual_rent_");
		private static readonly TagKey blockedTK = TagKey.Acquire("_house_blocked_");

		private const double SecondsPerDay = 86400;

		//filled from the world on first use: nothing else knows which regions have a house
		private static Dictionary<FlaggedRegion, House> byRegion;

		private static Dictionary<FlaggedRegion, House> ByRegion {
			get {
				if (byRegion == null) {
					byRegion = new Dictionary<FlaggedRegion, House>();
					foreach (Thing thing in Thing.AllThings) {
						House house = thing as House;
						if ((house != null) && (house.Area != null)) {
							byRegion[house.Area] = house;
						}
					}
				}
				return byRegion;
			}
		}

		public static House ForRegion(FlaggedRegion region) {
			House house;
			return ByRegion.TryGetValue(region, out house) ? house : null;
		}

		/// <summary>The house at a point, or null.</summary>
		public static House FindHouseAt(IPoint4D point) {
			FlaggedRegion region = point.GetMap().GetRegionFor(point) as FlaggedRegion;
			return ((region != null) && (region.HouseNumber != 0)) ? ForRegion(region) : null;
		}

		/// <summary>The region's house, which Erebor makes when a sign is first used, without an owner.</summary>
		public static House Create(FlaggedRegion region) {
			House house = ForRegion(region);
			if (house == null) {
				Point4D p = TileInside(region);
				house = (House) ThingDef.GetByDefname("i_house_gem").Create(p.X, p.Y, p.Z, p.M);
				house.SetAttribute(ItemAttributes.attr_invis, true);
				house.SetAttribute(ItemAttributes.attr_move_never, true);
				ByRegion[region] = house;
				house.StartRentTimer();
			}
			return house;
		}

		//a house is known by where its gem stands, and most spawn points lie just outside the region
		private static Point4D TileInside(FlaggedRegion region) {
			Map map = Map.GetMap(region.Mapplane);
			foreach (ImmutableRectangle rectangle in region.Rectangles) {
				for (int x = rectangle.MinX; x <= rectangle.MaxX; x++) {
					for (int y = rectangle.MinY; y <= rectangle.MaxY; y++) {
						if (map.GetRegionFor(x, y) == region) {
							return new Point4D(x, y, WorldSpots.LowestStandZ(map, x, y), region.Mapplane);
						}
					}
				}
			}
			return region.P;
		}

		public override void On_Destroy() {
			this.UnlockAll(); //nothing could take the locks off later
			//it's in limbo by now, so no longer in its region
			foreach (KeyValuePair<FlaggedRegion, House> entry in ByRegion) {
				if (entry.Value == this) {
					ByRegion.Remove(entry.Key);
					break;
				}
			}
			base.On_Destroy();
		}

		public FlaggedRegion Area {
			get {
				return this.IsOnGround ? (this.GetMap().GetRegionFor(this) as FlaggedRegion) : null;
			}
		}

		public int Number {
			get {
				return this.Area.HouseNumber;
			}
		}

		public Character Owner {
			get {
				Character owner = this.GetTag(ownerTK) as Character;
				return ((owner == null) || owner.IsDeleted) ? null : owner;
			}
			set {
				this.SetTag(ownerTK, value);
			}
		}

		public List<Character> Friends {
			get {
				return this.ListOf(friendsTK);
			}
		}

		public List<Character> CoOwners {
			get {
				return this.ListOf(coOwnersTK);
			}
		}

		private List<Character> ListOf(TagKey key) {
			List<Character> list = this.GetTag(key) as List<Character>;
			if (list == null) {
				list = new List<Character>();
				this.SetTag(key, list);
			}
			list.RemoveAll(ch => ch.IsDeleted);
			return list;
		}

		/// <summary>What the owner calls it, shown on the sign.</summary>
		public string HouseName {
			get {
				return this.GetTag(houseNameTK) as string;
			}
			set {
				this.SetTag(houseNameTK, value);
			}
		}

		/// <summary>The name to tell people: Erebor's link.name.</summary>
		public string Title {
			get {
				return this.HouseName ?? this.Area.Name;
			}
		}

		/// <summary>Gold for 30 days: by size, until a GM sets it for this house.</summary>
		public int Rent {
			get {
				object manual = this.GetTag(manualRentTK);
				return (manual == null) ? RentOf(this.Area) : (int) manual;
			}
			set {
				this.SetTag(manualRentTK, value);
			}
		}

		public static int RentOf(FlaggedRegion region) {
			HouseSettings settings = HouseSettings.instance;
			int tiles = 0;
			foreach (ImmutableRectangle rectangle in region.Rectangles) {
				tiles += (rectangle.Width + 1) * (rectangle.Height + 1);
			}
			int rounded = (int) Math.Round((tiles * (double) settings.rentPerTile) / settings.rentRounding) * settings.rentRounding;
			return Math.Max(rounded, settings.minimumRent);
		}

		/// <summary>In Globals.TimeInSeconds, which like Sphere's serv.time only runs with the server.</summary>
		public double PaidUntil {
			get {
				object paid = this.GetTag(paidUntilTK);
				return (paid == null) ? 0 : (double) paid;
			}
			set {
				this.SetTag(paidUntilTK, value);
			}
		}

		public int PaidDays {
			get {
				return Math.Max(0, (int) ((this.PaidUntil - Globals.TimeInSeconds) / SecondsPerDay));
			}
		}

		public bool Blocked {
			get {
				return this.GetTag(blockedTK) != null;
			}
			set {
				if (value) {
					this.SetTag(blockedTK, true);
				} else {
					this.RemoveTag(blockedTK);
				}
				this.SetLockedBlocked(value);
			}
		}

		private static readonly TagKey autoRentOffTK = TagKey.Acquire("_house_autorent_off_");

		/// <summary>Whether unpaid rent empties the house. On unless a GM turns it off.</summary>
		public bool AutoRent {
			get {
				return this.GetTag(autoRentOffTK) == null;
			}
			set {
				if (value) {
					this.RemoveTag(autoRentOffTK);
				} else {
					this.SetTag(autoRentOffTK, true);
				}
			}
		}

		public HouseRank RankOf(Character ch) {
			if (ch.IsGM) {
				return HouseRank.GM;
			}
			if (this.Blocked) {
				return HouseRank.Blocked;
			}
			if (ch == this.Owner) {
				return HouseRank.Owner;
			}
			if (this.CoOwners.Contains(ch)) {
				return HouseRank.CoOwner;
			}
			return this.Friends.Contains(ch) ? HouseRank.Friend : HouseRank.Public;
		}

		/// <summary>The menu's check: f_checkrightsmes, a rank above the one given.</summary>
		public bool Allows(Character ch, HouseRank above) {
			if (this.RankOf(ch) > above) {
				return true;
			}
			ch.RedMessage("Nemas na to pravo.");
			return false;
		}

		public void Claim(Character owner) {
			this.Owner = owner;
			this.PaidUntil = Globals.TimeInSeconds + (HouseSettings.instance.claimGraceDays * SecondsPerDay);
			owner.SysMessage("Toto je nyni tvuj dum.");
			owner.SysMessage("Najem cini " + this.Rent + " gp");
			owner.RedMessage("Autorent zapnut, zaplat si najem do " + HouseSettings.instance.claimGraceDays + " dni!");
		}

		public void AddMember(Character ch, HouseRank rank) {
			((rank == HouseRank.CoOwner) ? this.CoOwners : this.Friends).Add(ch);
		}

		public void RemoveMember(Character ch) {
			this.Friends.Remove(ch);
			this.CoOwners.Remove(ch);
		}

		//for a GM's attr_lockeddown and attr_secure
		public bool CanMoveLockedItems(Character ch) {
			return this.RankOf(ch) >= HouseRank.CoOwner;
		}

		public bool HasAccess(Character ch) {
			return this.RankOf(ch) >= HouseRank.Friend;
		}
	}
}
