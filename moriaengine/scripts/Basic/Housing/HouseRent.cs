using System;
using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - systems/najem.scp and f_payRentFrom. Rent is checked when someone clicks the sign or the chest, as on
	/// Erebor, and once a day, so a house nobody visits doesn't stay taken.</summary>
	public partial class House {
		private const double SecondsPerMonth = 30 * SecondsPerDay, SecondsPerHour = 3600, SecondsPerMinute = 60;
		private const double MaxPaidUntil = 214748360; //Erebor's 2147483600 deciseconds
		private const double BackupSeconds = 365 * SecondsPerDay;
		private const int MaxMonthsAtOnce = 80;

		private static readonly TagKey rentChestTK = TagKey.Acquire("_house_rent_chest_");
		private static readonly TagKey backupMadeTK = TagKey.Acquire("_house_backup_made_");

		public RentChest RentChest {
			get {
				RentChest chest = this.GetTag(rentChestTK) as RentChest;
				return ((chest == null) || chest.IsDeleted) ? null : chest;
			}
			set {
				this.SetTag(rentChestTK, value);
			}
		}

		/// <summary>The chest, brought back if it's gone: "jednou se mi stalo ze zmizela..."</summary>
		public void EnsureRentChest(IPoint4D where, Character tell) {
			if (this.RentChest == null) {
				this.PlaceRentChest(where.X, where.Y, where.Z, where.M);
				if (tell != null) {
					tell.SysMessage("Truhlicka navracena!");
					tell.SysMessage("Najem cini " + this.Rent + " gp");
				}
			}
		}

		public void PlaceRentChest(int x, int y, int z, byte m) {
			RentChest chest = (RentChest) ThingDef.GetByDefname(this.ChestDefname).Create(x, y, z, m);
			chest.SetAttribute(ItemAttributes.attr_move_never, true);
			chest.House = this;
			this.RentChest = chest;
		}

		public void StartRentTimer() {
			this.AddTimer(HouseRentTimer.Key, new HouseRentTimer()).DueInSeconds = SecondsPerDay;
		}

		/// <summary>Erebor's GM dialog: time already paid keeps its worth at the new rent.</summary>
		public void SetRent(int rent) {
			if (rent > 0) {
				double left = this.PaidUntil - Globals.TimeInSeconds;
				if (left > 0) {
					this.PaidUntil = Globals.TimeInSeconds + ((left * this.Rent) / rent);
				}
				this.Rent = rent;
			}
		}

		/// <summary>checknajem: what's in the chest becomes time, and a house with none left is emptied. Unlike Erebor an
		/// unfinished last day still counts: below a whole day it read as nothing paid.</summary>
		public void CheckRent(Character src) {
			if (this.Owner == null) {
				return;
			}
			double now = Globals.TimeInSeconds;
			if (this.PaidUntil < now) {
				this.PaidUntil = now;
			}
			RentChest chest = this.RentChest;
			if (chest != null) {
				this.PayFrom(chest, src);
				if ((src != null) && (this.PaidUntil >= MaxPaidUntil)) {
					src.SysMessage("Najem je plne zaplacen a nemuze byt dale navysovan.");
				}
				RemoveOldBackups(chest);
			}
			if (this.AutoRent && (this.PaidUntil <= now)) {
				if (src != null) {
					src.RedMessage(this.EvictedText);
				}
				this.Empty();
			}
		}

		private void PayFrom(RentChest chest, Character src) {
			foreach (Item item in new List<Item>(ItemsIn(chest))) {
				if (this.PaidUntil >= MaxPaidUntil) {
					return;
				}
				CashiersItem cashiers = item as CashiersItem;
				if (cashiers != null) {
					if (!this.PayFromCashiers(cashiers, src)) {
						return;
					}
				} else if (IsGold(item)) {
					double seconds = (((long) item.Amount * 2592) / this.Rent) * 1000;
					this.PaidUntil = Math.Min(this.PaidUntil + seconds, MaxPaidUntil);
					item.Delete();
				}
			}
		}

		private static bool IsGold(Item item) {
			return (item.Type != null) && string.Equals(item.Type.Defname, "t_gold", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>What a rent chest takes: anything else would sit in it for good.</summary>
		public static bool PaysRent(Item item) {
			return (item is CashiersItem) || IsGold(item);
		}

		private static IEnumerable<Item> ItemsIn(Container container) {
			foreach (Item item in container) {
				yield return item;
			}
		}

		/// <summary>f_payRentFrom: whole months, days, hours and minutes, what's smaller than a minute is lost. A big one pays
		/// 80 months and keeps the rest. Erebor credited a second cheque in the chest with the first one's value.</summary>
		private bool PayFromCashiers(CashiersItem item, Character src) {
			int rent = this.Rent;
			long toPay = item.Value;
			double dailyPay = rent / 30, hourPay = rent / 720, minutePay = rent / 43200;
			double room = MaxPaidUntil - this.PaidUntil;

			if ((toPay / rent) > MaxMonthsAtOnce) {
				double hours = Math.Floor(Math.Min(MaxMonthsAtOnce * SecondsPerMonth, room) / SecondsPerHour);
				this.PaidUntil += hours * SecondsPerHour;
				item.Value = (int) (toPay - (long) (hours * hourPay));
				return true;
			}
			double seconds = 0;
			void Take(double pay, double worth) {
				if ((pay > 0) && (toPay > pay)) {
					long count = (long) (toPay / pay);
					seconds += count * worth;
					toPay -= (long) (count * pay);
				}
			}
			if (toPay > dailyPay) {
				long months = toPay / rent;
				seconds += months * SecondsPerMonth;
				toPay -= months * rent;
			}
			Take(dailyPay, SecondsPerDay);
			Take(hourPay, SecondsPerHour);
			Take(minutePay, SecondsPerMinute);
			if (seconds > room) {
				if (src != null) {
					src.RedMessage("Najem je preplacen ! V truhle je jeste sek na " + item.Value + "gp.");
				}
				return false;
			}
			this.PaidUntil += seconds;
			item.Delete();
			return true;
		}

		private static void RemoveOldBackups(RentChest chest) {
			double now = Globals.TimeInSeconds;
			foreach (Item item in new List<Item>(ItemsIn(chest))) {
				object made = item.GetTag(backupMadeTK);
				if ((made != null) && ((now - (double) made) > BackupSeconds)) {
					item.Delete();
				}
			}
		}

		protected Container NewBackup(string name) {
			Container bag = (Container) ThingDef.GetByDefname("i_backpack").Create(this.RentChest);
			bag.Name = name;
			bag.SetAttribute(ItemAttributes.attr_invis, true);
			bag.SetAttribute(ItemAttributes.attr_move_never, true);
			bag.SetTag(backupMadeTK, Globals.TimeInSeconds);
			return bag;
		}

		/// <summary>trashhouse: what's on the ground goes into a backpack in the chest, tame animals are shrunk into another,
		/// doors stay and unlock, and the house is free. Only a GM reaches the backpacks; they go after a year.</summary>
		public virtual void Empty() {
			this.EnsureRentChest(this, null);
			string ownerName = (this.Owner == null) ? "nikdo" : this.Owner.Name;
			string when = DateTime.Now.ToString("d.M.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
			Container things = this.NewBackup("veci z domu " + this.Title + " - " + when + ", majitel " + ownerName);
			Container animals = this.NewBackup("Zvirata z domu " + this.Title + " - " + when);
			this.UnlockAll();
			FlaggedRegion area = this.Area;
			Map map = this.GetMap();
			foreach (ImmutableRectangle rectangle in area.Rectangles) {
				foreach (AbstractItem found in new List<AbstractItem>(map.GetItemsInRectangle(rectangle))) {
					Item item = found as Item;
					if ((item != null) && item.IsOnGround && (map.GetRegionFor(item) == area) && this.TakesWhenEmptied(item)) {
						if (item.IsDoor) {
							item.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door");
							item.LockUid = 0;
						} else {
							item.SetAttribute(ItemAttributes.attr_move_never, false);
							item.Cont = things;
						}
					}
				}
				foreach (AbstractCharacter found in new List<AbstractCharacter>(map.GetCharsInRectangle(rectangle))) {
					NPC pet = found as NPC;
					if ((pet != null) && (pet.ControlMaster != null) && (map.GetRegionFor(pet) == area) && !SummonSpellDef.IsSummonedCreature(pet)) {
						PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(animals);
						figurine.Keep(pet);
						pet.Delete();
					}
				}
			}
			DeleteIfEmpty(things);
			DeleteIfEmpty(animals);
			this.PaidUntil = Globals.TimeInSeconds;
			this.Friends.Clear();
			this.CoOwners.Clear();
			this.Owner = null;
			this.BankActivated = false;
		}

		protected static void DeleteIfEmpty(Container bag) {
			if (bag.Count == 0) {
				bag.Delete();
			}
		}

		private bool TakesWhenEmptied(Item item) {
			if ((item == this) || (item == this.RentChest) || t_housemenu.IsAnySign(item)) {
				return false;
			}
			return !HouseDoorstep.IsDoorstep(item);
		}
	}

	/// <summary>The daily look at a house. Rent is otherwise checked by clicks only.</summary>
	[SaveableClass, DeepCopyableClass]
	public class HouseRentTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_house_rent_");

		[LoadingInitializer, DeepCopyImplementation]
		public HouseRentTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			((House) cont).CheckRent(null);
			this.DueInSeconds = 86400;
			this.PeriodInSeconds = 86400;
		}
	}
}
