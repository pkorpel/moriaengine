using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class StallDef {
	}

	[ViewableClass]
	public partial class FieldDef {
	}

	/// <summary>#1017 - a vendor stall (stanek): an owner, rent by size, locks, a bank, and room for a vendor. Erebor leased
	/// them at auction; here they're claimed as houses are, the auction waits for the user's word.</summary>
	[ViewableClass]
	public partial class Stall {
		protected internal override string PlaqueText {
			get { return "Stanek cislo "; }
		}

		protected internal override string ClaimQuestion {
			get { return "Chces obsadit toto prodejni misto?"; }
		}

		protected internal override string ClaimedText {
			get { return "Tento prostor pro vendora nyni patri tobe."; }
		}

		protected internal override string EvictedText {
			get { return "Nedostatecne zaplaceny najem - vystehovavam prostor pro vendora."; }
		}

		protected internal override string ChestDefname {
			get { return "i_vendor_chest"; }
		}

		protected internal override bool HasMembers {
			get { return false; }
		}

		protected internal override int RentByArea(FlaggedRegion region) {
			HouseSettings settings = HouseSettings.instance;
			return RentOf(region, settings.stallRentPerTile, settings.stallMinimumRent);
		}

		private static readonly TagKey vendorTK = TagKey.Acquire("_stall_vendor_");
		private static readonly TagKey vendorSpotTK = TagKey.Acquire("_stall_vendor_spot_");

		/// <summary>vendoruid: one vendor a stall, through its menu.</summary>
		public EreborVendor Vendor {
			get {
				EreborVendor vendor = this.GetTag(vendorTK) as EreborVendor;
				return ((vendor == null) || vendor.IsDeleted) ? null : vendor;
			}
			set {
				this.SetTag(vendorTK, value);
			}
		}

		/// <summary>vendpos: where the vendor stands.</summary>
		public Point4D VendorSpot {
			get { return this.GetTag(vendorSpotTK) as Point4D; }
			set { this.SetTag(vendorSpotTK, value); }
		}

		public override void Empty() {
			RentedPlaces.Empty(this, "veci ze stanku ");
			EreborVendor vendor = this.Vendor;
			if (vendor != null) { //trashvendor_itagy: Erebor moved it to a GM room keeping its stock and gold; here a GM redeeds it later
				vendor.Say("Stanek byl vyklizen.");
			}
			this.RemoveTag(vendorTK);
			this.RemoveTag(vendorSpotTK);
		}

		/// <summary>Button 5: Umisteni / Premisteni Vendora.</summary>
		internal void PlaceVendor(Player owner) {
			Point4D spot = this.VendorSpot;
			EreborVendor vendor = this.Vendor;
			if (this.VendorPunished) {
				if (owner.IsGM) {
					owner.SysMessage("Vendor odstranen za trest, chces li jej vratit pouzij prislusny cudlik!");
				} else {
					owner.RedMessage("Vendor odstranen za trest, napis page pro vyreseni situace");
				}
				return;
			}
			if (vendor == null) {
				if (spot == null) {
					owner.SysMessage("Neni nastavena pozice!");
					return;
				}
				this.Vendor = EreborVendors.Place(owner, false, spot);
				owner.SysMessage("Vendor uspesne pridan");
			} else if ((vendor.X == spot.X) && (vendor.Y == spot.Y)) {
				owner.SysMessage("Neni potreba ho premistovat, na urcenem miste uz stoji");
				owner.SysMessage("Pro premisteni vendora nastav napred jeho novou pozici");
			} else {
				vendor.P(spot);
				owner.SysMessage("Vendor premisten");
			}
		}

		private static readonly TagKey punishedTK = TagKey.Acquire("_stall_vendor_punished_");

		public bool VendorPunished {
			get { return this.GetTag(punishedTK) != null; }
		}

		/// <summary>Button 8, GM: Erebor's punishment moved the vendor to a GM room on its plane 100, which isn't here; the
		/// stall's own spot on plane 250, where nobody walks, stands for it. Pressed again, the vendor comes back.</summary>
		internal void PunishVendor(Player gm) {
			EreborVendor vendor = this.Vendor;
			if (this.VendorPunished) {
				this.RemoveTag(punishedTK);
				if (vendor == null) {
					gm.SysMessage("Vendor nenalezen - pravdepodobne byl zrusen po potrestani!");
				} else {
					vendor.P(this.VendorSpot ?? new Point4D(this.X, this.Y, this.Z, this.M));
					gm.SysMessage("Vendor navracen");
				}
				return;
			}
			if (vendor == null) {
				gm.SysMessage("Vendor nenalezen!");
				return;
			}
			D_TownConfirm.Ask(gm, "Opravdu chces odstranit vendora? Nezrusi tagy => majitel si nemuze pridat noveho vendora - pouzij pouze jako TREST nikoliv k vyklizeni stanku !!!", this.Punish);
		}

		internal void Punish() {
			if (this.Vendor != null) {
				this.Vendor.P(this.X, this.Y, this.Z, (byte) MysticHypnosis.ParkedPlane);
				this.SetTag(punishedTK, 1);
			}
		}

		/// <summary>Button 18, GM: the vendor goes to the same place for good and the stall forgets it, so its owner may add
		/// another. It keeps its stock and gold for a GM to redeed.</summary>
		internal void RemoveVendor(Player gm) {
			EreborVendor vendor = this.Vendor;
			D_TownConfirm.Ask(gm, "Opravdu chces odstranit vendora? Odstraneni i s tagy => pokud nechas majitele muze si pridat noveho vendora!", () => {
				if (vendor != null) {
					vendor.P(this.X, this.Y, this.Z, (byte) MysticHypnosis.ParkedPlane);
				}
				this.RemoveTag(vendorTK);
				this.RemoveTag(vendorSpotTK);
				this.RemoveTag(punishedTK);
			});
		}

		/// <summary>Button 6: the other body.</summary>
		internal void ChangeVendorSex(Player owner) {
			EreborVendor vendor = this.Vendor;
			if (vendor == null) {
				owner.SysMessage("Neni tu vendor, nemuzes menit nic");
				return;
			}
			vendor.Model = (vendor.Model == 0x191) ? 0x190 : 0x191;
			owner.SysMessage("Pohlavi vendora zmeneno.");
		}

		internal void SetVendorSpot(Player owner, IPoint4D point) {
			if (point.GetMap().GetRegionFor(point) == this.Area) {
				this.VendorSpot = new Point4D(point.X, point.Y, point.Z, point.M);
				owner.SysMessage("Vendorovo misto nastaveno");
			} else {
				owner.SysMessage("Vendora muzes umistit jen do stanku");
			}
		}
	}

	/// <summary>#1017 - a field (pole): a stall without the vendor, and a dog whose owner may open its gates.</summary>
	[ViewableClass]
	public partial class Field {
		private static readonly TagKey guardTK = TagKey.Acquire("_field_guard_");

		protected internal override string PlaqueText {
			get { return "Pole cislo "; }
		}

		protected internal override string ClaimQuestion {
			get { return "Chces obsadit toto misto?"; }
		}

		protected internal override string ClaimedText {
			get { return "Toto pole nyni patri tobe."; }
		}

		protected internal override string EvictedText {
			get { return "Nedostatecne zaplaceny najem - vystehovavam pole."; }
		}

		protected internal override string ChestDefname {
			get { return "i_field_chest"; }
		}

		protected internal override bool HasMembers {
			get { return false; }
		}

		protected internal override int RentByArea(FlaggedRegion region) {
			HouseSettings settings = HouseSettings.instance;
			return RentOf(region, settings.fieldRentPerTile, settings.fieldMinimumRent);
		}

		/// <summary>The field's hlidac: its owner passes the field's locked gates.</summary>
		public NPC Guard {
			get {
				NPC dog = this.GetTag(guardTK) as NPC;
				return ((dog == null) || dog.IsDeleted) ? null : dog;
			}
			set {
				this.SetTag(guardTK, value);
			}
		}

		public override void Empty() {
			this.RemoveTag(guardTK);
			RentedPlaces.Empty(this, "veci z pole ");
		}
	}

	public static class RentedPlaces {
		/// <summary>trashvendorplac / trashfieldplac: only what was locked goes to the chest, doors unlock and stay, the rent is by
		/// size again. Erebor gave the place to a GM's uid for its auction; here it's free.</summary>
		internal static void Empty(House place, string backupName) {
			place.EnsureRentChest(place, null);
			string when = DateTime.Now.ToString("d.M.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
			List<Item> locked = new List<Item>();
			FlaggedRegion area = place.Area;
			Map map = place.GetMap();
			foreach (ImmutableRectangle rectangle in area.Rectangles) {
				foreach (AbstractItem found in map.GetItemsInRectangle(rectangle)) {
					if ((found is Item item) && (item.LockedTo == place) && !t_housemenu.IsAnySign(item)) {
						locked.Add(item);
					}
				}
			}
			place.UnlockAll();
			Container things = (Container) ThingDef.GetByDefname("i_backpack").Create(place.RentChest);
			things.Name = backupName + place.Title + " - " + when;
			things.SetAttribute(ItemAttributes.attr_invis, true);
			things.SetAttribute(ItemAttributes.attr_move_never, true);
			foreach (Item item in locked) {
				if (item.IsDeleted) {
					continue;
				}
				if (item.IsDoor) {
					item.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door");
					item.LockUid = 0;
				} else {
					item.SetAttribute(ItemAttributes.attr_move_never, false);
					item.Cont = things;
				}
			}
			if (things.Count == 0) {
				things.Delete();
			}
			place.Owner = null;
			place.BankActivated = false;
			place.ForgetManualRent();
		}

		/// <summary>t_door_field: a gate locked on a field opens for its GM, owner, and the owner of its dog.</summary>
		internal static bool GuardOwnerPasses(Character user, House place) {
			return (place is Field field) && (field.Guard != null) && (field.Guard.ControlMaster == user);
		}

		/// <summary>Button 5 of a field: the owner's own dog guards it.</summary>
		internal static void SetGuard(Player player, Field field, Character target) {
			if (field.RankOf(target) >= HouseRank.Owner) {
				player.SysMessage("Cil uz majitelem je");
			} else if ((target is NPC dog) && (dog.Def.PrettyDefname == "c_dog") && (dog.ControlMaster == player)) {
				field.Guard = dog;
				player.SysMessage(dog.Name + " hlida pole.");
			} else {
				player.SysMessage("Tohle neni tvuj pes");
			}
		}
	}

	/// <summary>A stall's sign: within 3 tiles or in its region, as Erebor.</summary>
	public class t_vendormenu : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			FlaggedRegion region = self.GetMap().GetRegionFor(self) as FlaggedRegion;
			if ((Point2D.GetSimpleDistance(self, clicker) > 3) && (clicker.Region != region)) {
				clicker.SysMessage("Jsi prilis daleko.");
				return;
			}
			t_housemenu.Use(self, clicker);
		}
	}

	/// <summary>A field's sign: Erebor checked no distance.</summary>
	public class t_fieldmenu : CompiledTriggerGroup {
		public void On_DClick(Item self, Character clicker) {
			t_housemenu.Use(self, clicker);
		}
	}

	public class Targ_StallVendorSpot : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			Stall stall = (Stall) parameter;
			stall.SetVendorSpot(self, new Point4D(targetted.X, targetted.Y, targetted.Z, self.M));
			return TargetResult.Done;
		}
	}

	public class Targ_FieldGuard : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			RentedPlaces.SetGuard(self, (Field) parameter, targetted);
			return TargetResult.Done;
		}
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>d_vendor_menu and d_field_menu, Erebor's button numbers, without the auction and (for now) the vendor.</summary>
	public class D_RentedPlace : CompiledGumpDef {
		private const int Width = 260, RowHeight = 22, LabelHue = 955, ButtonHue = 2301;
		private const int LockItem = 2, Guard = 5, PlaceVendor = 5, VendorSex = 6, VendorSpot = 17, PunishVendor = 8, RemoveVendor = 18, Rename = 7, Bank = 9, Transfer = 10, Block = 11, Empty = 12, ResetPaid = 13, SetRent = 14, AutoRent = 15;

		public static void Show(Player player, House place, Item sign) {
			player.Dialog(player, SingletonScript<D_RentedPlace>.Instance, new DialogArgs(place, sign));
		}

		private static string RankName(HouseRank rank) {
			return (rank == HouseRank.GM) ? "GM" : (rank == HouseRank.Owner) ? "Majitel" : "Verejnost";
		}

		private static List<KeyValuePair<int, string>> Entries(House place, HouseRank rank) {
			List<KeyValuePair<int, string>> entries = new List<KeyValuePair<int, string>>();
			string noun = (place is Field) ? "pole" : "stanek";
			void Add(int button, HouseRank atLeast, string text) {
				if (rank >= atLeast) {
					entries.Add(new KeyValuePair<int, string>(button, text));
				}
			}
			Add(LockItem, HouseRank.Owner, "Zamknout/odemknout vec");
			if (place is Field) {
				Add(Guard, HouseRank.Owner, "Priradit hlidace");
			} else if (place is Stall stall) {
				Add(PlaceVendor, HouseRank.Owner, (stall.Vendor == null) ? "Umisteni Vendora" : "Premisteni Vendora");
				Add(VendorSex, HouseRank.Owner, "Zmena pohlavi Vendora");
				Add(VendorSpot, HouseRank.Owner, "Nastavit pozici pro vendora");
			}
			Add(Rename, HouseRank.Owner, "Prejmenovat " + noun);
			Add(Bank, HouseRank.Public, place.BankActivated ? "Otevrit banku" : "Aktivovat banku (" + (HouseSettings.instance.bankActivationPrice / 1000) + "k gp)");
			Add(Transfer, HouseRank.Owner, "Predat vlastnictvi " + ((place is Field) ? "pole" : "stanku"));
			Add(Block, HouseRank.GM, (place.Blocked ? "Od" : "Za") + "blokovat " + noun);
			Add(Empty, HouseRank.GM, "Trash " + noun + ((place is Stall) ? " (necha vendora)" : ""));
			if (place is Stall punishing) {
				Add(PunishVendor, HouseRank.GM, punishing.VendorPunished ? "Vratit vendora z trestu" : "Odstranit vendora (trest)");
				Add(RemoveVendor, HouseRank.GM, "Odstranit vendora (uplne)");
			}
			Add(ResetPaid, HouseRank.GM, "Vynulovat zaplaceni");
			Add(SetRent, HouseRank.GM, "Nastavit najem");
			Add(AutoRent, HouseRank.GM, (place.AutoRent ? "Vy" : "Za") + "pnout autorent");
			return entries;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			House place = (House) args[0];
			Item sign = (Item) args[1];
			Player viewer = (Player) sendTo;
			HouseRank rank = place.RankOf(viewer);
			string title = place.PlaqueText + place.Number + "<br>" + place.HouseName;
			this.SetLocation(30, 0);
			this.AddPage(0);
			if (place.Blocked) {
				viewer.RedMessage(((place is Field) ? "Pole je zablokovano!" : "Prostor pro vendora je zablokovan!") + " Pro vice informaci napis page.");
			}
			List<KeyValuePair<int, string>> entries = Entries(place, rank);
			this.AddBackground(0, 0, Width, 216 + (entries.Count * RowHeight), 2620);
			this.AddImage(14, 14, 100);
			this.AddHtml(34, 34, 100, 60, title, false, false);
			this.AddLabel(150, 16, LabelHue, "Majitel:");
			this.AddLabel(150, 36, LabelHue, (place.Owner == null) ? "nikdo" : place.Owner.Name);
			this.AddLabel(150, 66, LabelHue, "Tvuj status:");
			this.AddLabel(150, 86, LabelHue, RankName(rank));
			this.AddLabel(14, 154, LabelHue, "Najem: " + place.Rent + " gp");
			this.AddLabel(14, 174, LabelHue, "Zaplaceno na " + place.PaidDays + " dni");
			int y = 204;
			foreach (KeyValuePair<int, string> entry in entries) {
				this.AddButton(14, y, 4005, 4007, entry.Key, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, ButtonHue, entry.Value);
				y += RowHeight;
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			House place = (House) args[0];
			Item sign = (Item) args[1];
			Player player = (Player) gi.Cont;
			if (place.IsDeleted) {
				return;
			}
			string noun = (place is Field) ? "pole" : "stanek";
			switch (gr.PressedButton) {
				case LockItem:
					if (place.Allows(player, HouseRank.CoOwner)) {
						player.SysMessage("Co chces zamknout/odemknout?");
						player.Target(SingletonScript<Targ_HouseLock>.Instance, place);
					}
					break;
				case Guard:
					if ((place is Field field) && place.Allows(player, HouseRank.CoOwner)) {
						player.SysMessage("Koho chces udelat hlidacem?");
						player.Target(SingletonScript<Targ_FieldGuard>.Instance, field);
					} else if ((place is Stall stall) && place.Allows(player, HouseRank.CoOwner)) {
						stall.PlaceVendor(player);
					}
					break;
				case PunishVendor:
					if ((place is Stall punishStall) && place.Allows(player, HouseRank.Owner)) {
						punishStall.PunishVendor(player);
					}
					break;
				case RemoveVendor:
					if ((place is Stall removeStall) && place.Allows(player, HouseRank.Owner)) {
						removeStall.RemoveVendor(player);
					}
					break;
				case VendorSex:
					if ((place is Stall sexStall) && place.Allows(player, HouseRank.CoOwner)) {
						sexStall.ChangeVendorSex(player);
					}
					break;
				case VendorSpot:
					if ((place is Stall spotStall) && place.Allows(player, HouseRank.CoOwner)) {
						player.SysMessage("Zamer pozici!");
						player.Target(SingletonScript<Targ_StallVendorSpot>.Instance, spotStall);
					}
					break;
				case Rename:
					if (place.Allows(player, HouseRank.CoOwner)) {
						D_TownInput.Ask(player, "Jak se ma " + noun + " jmenovat?", place.HouseName ?? "", answer => {
							place.HouseName = answer;
						});
					}
					break;
				case Bank:
					if (place.Allows(player, HouseRank.Blocked)) {
						place.UseBank(player, sign);
					}
					break;
				case Transfer:
					if (place.Allows(player, HouseRank.CoOwner)) {
						player.SysMessage("Koho chces udelat vlastnikem?");
						player.Target(SingletonScript<Targ_HouseMember>.Instance, new object[] { place, HouseRank.Owner });
					}
					break;
				case Block:
					if (place.Allows(player, HouseRank.Owner)) {
						place.Blocked = !place.Blocked;
						string done = (place is Field) ? "Pole " + (place.Blocked ? "zablokovano." : "odblokovano.") : "Stanek " + (place.Blocked ? "zablokovan." : "odblokovan.");
						player.SysMessage(done);
					}
					break;
				case Empty:
					if (place.Allows(player, HouseRank.Owner)) {
						D_TownConfirm.Ask(player, "Opravdu chces vyklidit " + noun + "?", place.Empty);
					}
					break;
				case ResetPaid:
					if (place.Allows(player, HouseRank.Owner)) {
						D_TownConfirm.Ask(player, "Opravdu chces vynulovat najem?", () => place.PaidUntil = Globals.TimeInSeconds);
					}
					break;
				case SetRent:
					if (place.Allows(player, HouseRank.Owner)) {
						D_TownInput.Ask(player, "Kolik ma byt najem?", place.Rent.ToString(System.Globalization.CultureInfo.InvariantCulture), answer => {
							if (int.TryParse(answer, out int rent)) {
								place.SetRent(rent);
							}
						});
					}
					break;
				case AutoRent:
					if (place.Allows(player, HouseRank.Owner)) {
						place.AutoRent = !place.AutoRent;
						player.SysMessage("Automaticky najem " + (place.AutoRent ? "zapnut." : "vypnut."));
					}
					break;
			}
		}
	}
}
