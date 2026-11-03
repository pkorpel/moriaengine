using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - systems/administrace/rental_system.scp: who lives where in a city, for those who run it. A city here is
	/// FlaggedRegion.City, several areas sharing one, which is what Erebor's lists of places per city did. A house's Parent
	/// doesn't say: it's often the territory around the city, so its city is the one whose area holds it.</summary>
	public static class HouseRentals {
		private const string ImportantPersonMessage = "Je to dulezita osoba, nelze jej vyvrhnout z celeho realmu";

		public static string CityOf(FlaggedRegion house) {
			ImmutableRectangle rectangle = house.Rectangles[0];
			int x = rectangle.MinX + (rectangle.Width / 2), y = rectangle.MinY + (rectangle.Height / 2);
			string city = null;
			int smallest = int.MaxValue;
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area != null) && !string.IsNullOrEmpty(area.City) && (area.Mapplane == house.Mapplane) && area.ContainsPoint(new Point2D(x, y))) {
					int tiles = Tiles(area);
					if (tiles < smallest) {
						smallest = tiles;
						city = area.City;
					}
				}
			}
			return city;
		}

		private static int Tiles(FlaggedRegion area) {
			int tiles = 0;
			foreach (ImmutableRectangle rectangle in area.Rectangles) {
				tiles += (rectangle.Width + 1) * (rectangle.Height + 1);
			}
			return tiles;
		}

		/// <summary>The city someone stands in, or null.</summary>
		public static string CityAt(Character ch) {
			FlaggedRegion area = ch.Region as FlaggedRegion;
			if ((area != null) && area.IsHouse) {
				return CityOf(area);
			}
			while (area != null) {
				if (!string.IsNullOrEmpty(area.City)) {
					return area.City;
				}
				area = area.Parent as FlaggedRegion;
			}
			return null;
		}

		/// <summary>Houses that exist, so have been used or set up, by number.</summary>
		public static List<House> InCity(string city) {
			List<House> houses = new List<House>();
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area != null) && area.IsHouse) {
					House house = House.ForRegion(area);
					if ((house != null) && StringComparer.OrdinalIgnoreCase.Equals(CityOf(area), city)) {
						houses.Add(house);
					}
				}
			}
			houses.Sort((a, b) => a.Number.CompareTo(b.Number));
			return houses;
		}

		public static bool IsGeneral(Character ch) {
			RealmOffice office = RealmOffice.For(Realm.Gondor);
			return (office != null) && (office.holder == ch);
		}

		/// <summary>Erebor's check of who gets the dialog at all.</summary>
		public static bool MayView(Character ch) {
			return ch.IsGM || NazgulRings.IsNazgul(ch) || (Town.OfRuler(ch) != null) || IsGeneral(ch);
		}

		public static bool MayRealmBan(Character ch) {
			return ch.IsGM || NazgulRings.IsFirst(ch) || IsGeneral(ch);
		}

		public static bool IsImportant(Character ch) {
			return ch.IsGM || NazgulRings.IsFirst(ch) || IsGeneral(ch);
		}

		/// <summary>canbetownbannedfrom. A Gondor city's own citizens go through the mayor's other tools.</summary>
		public static bool CanBeTownBanned(Character owner, Town town) {
			if (IsImportant(owner) || (owner.Realm != town.Realm) || (town.FindBan(owner) != null)) {
				return false;
			}
			return (town.Realm != Realm.Gondor) || (town.FindCitizen(owner) == null);
		}

		/// <summary>.rentalinfo, Erebor's name, for the city it's given or the one the GM stands in.</summary>
		[SteamFunction]
		public static void RentalInfo(Character self, ScriptArgs sa) {
			Player player = self as Player;
			if (player == null) {
				return;
			}
			string city = ((sa != null) && !string.IsNullOrEmpty(sa.Args)) ? sa.Args.Trim('"', ' ') : (CityAt(player) ?? ((FlaggedRegion) player.Region).Name);
			D_HouseRentals.Show(player, city, 0);
		}

		/// <summary>urobobcany: the owners of a city's houses become its citizens, under the rules of Targ_TownCitizen.</summary>
		[SteamFunction]
		public static void HouseCitizens(Character self, ScriptArgs sa) {
			string city = ((sa != null) && !string.IsNullOrEmpty(sa.Args)) ? sa.Args.Trim('"', ' ') : CityAt(self);
			Town town = (city == null) ? null : Town.ForCity(city);
			if (town == null) {
				self.SysMessage("Tady neni zadne mesto.");
				return;
			}
			int made = 0;
			foreach (House house in InCity(city)) {
				Character owner = house.Owner;
				if ((owner != null) && (owner.Realm == Realm.Gondor) && (town.GetRank(owner) != Town.RankEnemy) && (Town.OfCitizen(owner) == null)) {
					town.AddCitizen(owner);
					owner.SysMessage("Stal jsi se obcanem mesta " + town.Name + ".");
					made++;
				}
			}
			self.SysMessage("Novych obcanu: " + made);
		}

		internal static void ToggleRealmBan(Player player, Character owner) {
			RealmOffice office = RealmOffice.For(player.IsGM ? owner.Realm : player.Realm);
			if (office == null) {
				return;
			}
			if (!player.IsGM && (owner.Realm != player.Realm)) {
				player.SysMessage("Vyvrhnout z realmu muzes jen hrace tvoji strany");
				return;
			}
			TownBan ban = office.FindBan(owner);
			if (ban != null) {
				office.bans.Remove(ban);
				owner.SysMessage("Byl jsi odebran z globalni cerne listiny");
			} else if (IsImportant(owner)) {
				player.SysMessage(ImportantPersonMessage);
			} else {
				D_TownInput.Ask(player, "Udej duvod pro blacklist:", "", reason => {
					office.bans.Add(new TownBan { who = owner, name = owner.Name, reason = reason });
					owner.RedMessage("Byl jsi dan na globalni cernou listinu");
				});
			}
		}

		internal static void ToggleTownBan(Player player, Town town, Character owner) {
			if (!player.IsGM && (owner.Realm != player.Realm)) {
				player.SysMessage("Vyvrhnout z mesta muzes jen hrace tvoji strany");
				return;
			}
			TownBan ban = town.FindBan(owner);
			if (ban != null) {
				town.banned.Remove(ban);
				owner.SysMessage("Byl jsi odebran z cerne listiny mesta " + town.Name);
			} else if (CanBeTownBanned(owner, town)) {
				D_TownInput.Ask(player, "Udej duvod pro blacklist:", "", reason => town.Ban(owner, reason));
			} else {
				player.SysMessage("Dotycny nemuze byt pridan na blacklist");
			}
		}
	}

	/// <summary>d_show_housing_info: columns by who's looking, 20 houses a page. Ban buttons are the same two as Erebor's, and a
	/// third picture for one not to touch.</summary>
	public class D_HouseRentals : CompiledGumpDef {
		private const int Rows = 20, RowHeight = 20, FirstRow = 62, Hue = 955, LabelHue = 0x481, HeaderRow = 38;
		private const int PreviousButton = 1, NextButton = 2, RealmBanBase = 1000, TownBanBase = 2000, CoOwnersBase = 3000, FriendsBase = 4000;
		private const int NumberX = 14, PlaceX = 90, OwnerX = 210, RealmBanX = 350, TownBanX = 420, CoOwnersX = 490, FriendsX = 550;
		private const int GiveUp = 4005, GiveDown = 4007, TakeUp = 0xfb1, TakeDown = 0xfb3, DontTouch = 0xfb6;

		public static void Show(Player player, string city, int first) {
			if (HouseRentals.MayView(player)) {
				player.Dialog(player, SingletonScript<D_HouseRentals>.Instance, new DialogArgs(city, HouseRentals.InCity(city), first));
			} else {
				player.RedMessage("Nemas na to pravo.");
			}
		}

		private static Town TownHere(Player player) {
			string city = HouseRentals.CityAt(player);
			return (city == null) ? null : Town.ForCity(city);
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			string city = (string) args[0];
			List<House> houses = (List<House>) args[1];
			int first = (int) args[2];
			bool coOwners = viewer.IsGM || NazgulRings.IsFirst(viewer);
			int width = viewer.IsGM ? 620 : (coOwners ? 560 : 490);
			this.SetLocation(20, 20);
			this.AddPage(0);
			this.AddBackground(0, 0, width, FirstRow + (Rows * RowHeight) + 34, 2620);
			this.AddLabel(14, 12, LabelHue, "Pocet domu v oblasti " + city + " : " + houses.Count);
			this.AddButton(width - 45, 12, TakeUp, TakeDown, 0, GumpButtonType.Reply, 0);
			this.AddLabel(NumberX, HeaderRow, LabelHue, "Dum");
			this.AddLabel(PlaceX, HeaderRow, LabelHue, "Pozice domu");
			this.AddLabel(OwnerX, HeaderRow, LabelHue, "Vlastnik");
			if (HouseRentals.MayRealmBan(viewer)) {
				this.AddLabel(RealmBanX, HeaderRow, LabelHue, "RealmBan");
			}
			this.AddLabel(TownBanX, HeaderRow, LabelHue, "TownBan");
			if (coOwners) {
				this.AddLabel(CoOwnersX, HeaderRow, LabelHue, "Spoluvl.");
			}
			if (viewer.IsGM) {
				this.AddLabel(FriendsX, HeaderRow, LabelHue, "Pratele");
			}
			Town town = TownHere(viewer);
			int y = FirstRow;
			for (int index = first; (index < houses.Count) && (index < first + Rows); index++) {
				this.Row(viewer, town, houses[index], index, y, coOwners);
				y += RowHeight;
			}
			int bottom = FirstRow + (Rows * RowHeight) + 6;
			if (first > 0) {
				this.AddButton(14, bottom, 0xfa, 0xfb, PreviousButton, GumpButtonType.Reply, 0);
			}
			if (first + Rows < houses.Count) {
				this.AddButton(40, bottom, 0xfc, 0xfd, NextButton, GumpButtonType.Reply, 0);
			}
		}

		private void Row(Player viewer, Town town, House house, int index, int y, bool coOwners) {
			Character owner = house.Owner;
			this.AddLabel(NumberX, y, Hue, "Dum " + house.Number);
			this.AddLabel(PlaceX, y, Hue, house.X + "," + house.Y + "," + house.Z + "," + house.M);
			if (owner == null) {
				this.AddLabel(OwnerX, y, Hue, "Neobydleny");
			} else {
				int ownerHue = (owner.Realm == Realm.Mordor) ? 0x26 : ((owner.Realm == Realm.Gondor) ? 0x63 : Hue);
				this.AddLabel(OwnerX, y, ThiefMask.SeenRealmHue(owner, viewer) ?? ownerHue, ThiefMask.RealName(owner, viewer));
				bool own = viewer.IsGM || (owner.Realm == viewer.Realm);
				if (HouseRentals.MayRealmBan(viewer)) {
					RealmOffice office = RealmOffice.For(viewer.IsGM ? owner.Realm : viewer.Realm);
					bool banned = (office != null) && (office.FindBan(owner) != null);
					this.BanCell(RealmBanX, y, own && (office != null) && (banned || !HouseRentals.IsImportant(owner)), banned, RealmBanBase + index);
				}
				if (town != null) {
					bool banned = town.FindBan(owner) != null;
					this.BanCell(TownBanX, y, own && (banned || HouseRentals.CanBeTownBanned(owner, town)), banned, TownBanBase + index);
				}
			}
			if (coOwners) {
				this.AddButton(CoOwnersX + 10, y, 0xfa8, 0xfaa, CoOwnersBase + index, GumpButtonType.Reply, 0);
			}
			if (viewer.IsGM) {
				this.AddButton(FriendsX + 10, y, 0xfa8, 0xfaa, FriendsBase + index, GumpButtonType.Reply, 0);
			}
		}

		private void BanCell(int x, int y, bool active, bool banned, int button) {
			if (!active) {
				this.AddImage(x + 10, y, DontTouch);
			} else if (banned) {
				this.AddButton(x + 10, y, TakeUp, TakeDown, button, GumpButtonType.Reply, 0);
			} else {
				this.AddButton(x + 10, y, GiveUp, GiveDown, button, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			string city = (string) args[0];
			List<House> houses = (List<House>) args[1];
			int first = (int) args[2];
			int button = gr.PressedButton;
			if ((button == 0) || !HouseRentals.MayView(player)) {
				return;
			}
			if (button == PreviousButton) {
				Show(player, city, Math.Max(0, first - Rows));
			} else if (button == NextButton) {
				Show(player, city, first + Rows);
			} else if (button >= FriendsBase) {
				D_HouseMembers.Show(player, houses[button - FriendsBase], false);
			} else if (button >= CoOwnersBase) {
				D_HouseMembers.Show(player, houses[button - CoOwnersBase], true);
			} else if (button >= TownBanBase) {
				Character owner = houses[button - TownBanBase].Owner;
				Town town = TownHere(player);
				if ((owner == null) || (town == null)) {
					player.SysMessage("Nejdrive se vrat do mesta");
				} else {
					HouseRentals.ToggleTownBan(player, town, owner);
				}
			} else if (button >= RealmBanBase) {
				Character owner = houses[button - RealmBanBase].Owner;
				if ((owner != null) && HouseRentals.MayRealmBan(player)) {
					HouseRentals.ToggleRealmBan(player, owner);
				}
			}
		}
	}

	/// <summary>d_bags_housing_info: who's a house's co-owner or friend, to read.</summary>
	public class D_HouseMembers : CompiledGumpDef {
		private const int Width = 250, Rows = 20, RowHeight = 20, FirstRow = 40, Hue = 2301;

		public static void Show(Player player, House house, bool coOwners) {
			player.Dialog(player, SingletonScript<D_HouseMembers>.Instance, new DialogArgs(house, coOwners));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			House house = (House) args[0];
			bool coOwners = (bool) args[1];
			List<Character> members = coOwners ? house.CoOwners : house.Friends;
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (Rows * RowHeight) + 16, 2620);
			this.AddLabel(14, 12, 0x481, (coOwners ? "Spoluvlastnici" : "Pratele") + " domu " + house.Title);
			int y = FirstRow;
			for (int index = 0; (index < members.Count) && (index < Rows); index++) {
				this.AddLabel(14, y, Hue, members[index].Name);
				y += RowHeight;
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
		}
	}
}
