using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>A citizen and how they stand with the tax: Erebor's i_town_crystal in the citizens' pouch.</summary>
	[SaveableClass]
	public class TownCitizen {
		[LoadingInitializer]
		public TownCitizen() {
		}

		[SaveableData]
		public Character who;

		[SaveableData]
		public bool paidTax;

		/// <summary>The tax when they paid, which shows a mayor who raised it after everyone else had paid.</summary>
		[SaveableData]
		public int amountPaid;

		[SaveableData]
		public bool autoPay;
	}

	/// <summary>#892 - a city of systems/starostove.scp. Erebor kept a town in tags of its stone and in
	/// invisible pouches of crystals; here it's this, saved with the stone. There's one per tag.jmeno_mesta,
	/// which several map areas can share. Timers run when someone uses the stone, as on Erebor.</summary>
	[SaveableClass]
	[ViewableClass]
	public class Town {
		public const int RankEnemy = 0, RankPublic = 1, RankCitizen = 2, RankMayor = 3, RankGm = 4;
		private const double SecondsInDay = 24 * 60 * 60;

		private static readonly List<Town> all = new List<Town>();

		[SaveableData]
		public string city;

		[SaveableData]
		public Item stone;

		[SaveableData]
		public TaxChest taxChest;

		[SaveableData]
		public Character mayor;

		/// <summary>tag.dane</summary>
		[SaveableData]
		public int tax;

		/// <summary>tag.pokladna</summary>
		[SaveableData]
		public long treasury;

		/// <summary>tag.cas_dani, in Globals.TimeInSeconds.</summary>
		[SaveableData]
		public double taxDueAt;

		/// <summary>tag.daneNextUpdate</summary>
		[SaveableData]
		public double nextTaxChangeAt;

		[SaveableData]
		public List<TownCitizen> citizens = new List<TownCitizen>();

		[SaveableData]
		public List<string> news = new List<string>();

		[LoadingInitializer]
		public Town() {
			all.Add(this);
		}

		public static IList<Town> All {
			get { return all; }
		}

		public static Town ForCity(string city) {
			foreach (Town town in all) {
				if (StringComparer.OrdinalIgnoreCase.Equals(town.city, city)) {
					return town;
				}
			}
			return null;
		}

		/// <summary>The town of this stone, new if this is the stone's first use.</summary>
		public static Town ForStone(Item stone, string city) {
			Town town = ForCity(city);
			if (town == null) {
				town = new Town { city = city, stone = stone };
				town.tax = TownSettings.instance.defaultTax;
				town.taxDueAt = Globals.TimeInSeconds + (TownSettings.instance.taxPeriodDays * SecondsInDay);
			}
			town.stone = stone;
			return town;
		}

		/// <summary>The town a stone already has, null if it's yet to be used.</summary>
		public static Town OfStone(Item stone) {
			foreach (Town town in all) {
				if (town.stone == stone) {
					return town;
				}
			}
			return null;
		}

		public static Town OfCitizen(Character ch) {
			foreach (Town town in all) {
				if (town.FindCitizen(ch) != null) {
					return town;
				}
			}
			return null;
		}

		public static Town OfChest(TaxChest chest) {
			foreach (Town town in all) {
				if (town.taxChest == chest) {
					return town;
				}
			}
			return null;
		}

		public string Name {
			get { return this.city.Replace('_', ' '); }
		}

		public void Remove() {
			all.Remove(this);
			if ((this.taxChest != null) && !this.taxChest.IsDeleted) {
				this.taxChest.Delete();
			}
		}

		public TownCitizen FindCitizen(Character ch) {
			foreach (TownCitizen citizen in this.citizens) {
				if (citizen.who == ch) {
					return citizen;
				}
			}
			return null;
		}

		/// <summary>tm_findrank</summary>
		public int GetRank(Character ch) {
			if (ch.IsGM) {
				return RankGm;
			}
			if (ch == this.mayor) {
				return RankMayor;
			}
			return (this.FindCitizen(ch) != null) ? RankCitizen : RankPublic;
		}

		/// <summary>The chest stands beside the stone, and comes back if it's gone.</summary>
		public void EnsureTaxChest(Character user) {
			if ((this.taxChest == null) || this.taxChest.IsDeleted) {
				bool lost = this.taxChest != null;
				this.taxChest = (TaxChest) ThingDef.GetByDefname("i_tax_chest").Create(this.stone.X + 1, this.stone.Y, this.stone.Z, this.stone.M);
				this.taxChest.SetAttribute(ItemAttributes.attr_move_never, true);
				if (lost) {
					user.SysMessage("Truhlicka navracena!");
					user.SysMessage("Dan cini " + this.tax + "gp !");
				}
			}
		}

		/// <summary>zkontroluj_dan</summary>
		public void RaiseTaxToMinimum(Character user) {
			int minimum = this.MinimumTax;
			if (minimum > this.tax) {
				this.SetTax(user, minimum);
				user.RedMessage("Dan je mensi nez povolena minimalni dan. Navysuji dan na " + minimum + ".");
			}
		}

		public int MinimumTax {
			get { return this.citizens.Count * TownSettings.instance.minTaxPerCitizen; }
		}

		/// <summary>taxset</summary>
		public bool SetTax(Character user, int amount) {
			if (amount < this.MinimumTax) {
				user.SysMessage("Minimalni dan musi byt " + this.MinimumTax + " gp.");
				return false;
			}
			if (amount > TownSettings.instance.maxTax) {
				user.SysMessage("Nastavit dan muzes maximalne na " + TownSettings.instance.maxTax + ".");
				return false;
			}
			if (Globals.TimeInSeconds < this.nextTaxChangeAt) {
				user.RedMessage("Upravu dan muzes provest jen jednou tydne.");
				return false;
			}
			this.nextTaxChangeAt = Globals.TimeInSeconds + (TownSettings.instance.taxChangeDays * SecondsInDay);
			this.tax = amount;
			foreach (TownCitizen citizen in this.citizens) {
				citizen.who.RedMessage("Nove dane: " + this.tax);
			}
			return true;
		}

		/// <summary>Erebor's pay: the pack first, then the bank, and a GM pays nothing.</summary>
		public static bool Pay(Character who, int amount) {
			if (who.IsGM) {
				who.SysMessage("Jako GM nic neplatis.");
				return true;
			}
			if (GoldUtils.TryPay(who, (uint) amount)) {
				return true;
			}
			who.SysMessage("Potrebujes " + amount + " zlatych.");
			return false;
		}

		/// <summary>stan_se_obcanem</summary>
		public bool TryJoin(Player player) {
			if (player.Realm != Realm.Gondor) {
				player.RedMessage("Obcanem se muze stat jen pravoplatny Gondoran!");
				return false;
			}
			TownSettings settings = TownSettings.instance;
			if ((player.Level < settings.citizenMinLevel) || ((int) player.SkillRank < settings.citizenMinRank)) {
				player.RedMessage("Jsi velmi nezkuseny, aby jsi mohl reprezentovat mesto " + this.Name + " svymi ciny!");
				return false;
			}
			if (this.GetRank(player) == RankEnemy) {
				player.RedMessage("Jsi na cerne listine! Nemuzes se stat obcanem!");
				return false;
			}
			Town current = OfCitizen(player);
			if (current != null) {
				player.RedMessage("Jiz jsi obcanem mesta " + current.Name);
				return false;
			}
			if (!Pay(player, settings.citizenshipFee)) {
				return false;
			}
			this.AddCitizen(player);
			player.SysMessage("Stal jsi se obcanem mesta " + this.Name + ".");
			this.RaiseTaxToMinimum(player);
			return true;
		}

		public void AddCitizen(Character ch) {
			this.citizens.Add(new TownCitizen { who = ch });
		}

		/// <summary>zrus_obcanstvi; a mayor who leaves stops being mayor.</summary>
		public void RemoveCitizen(Character ch) {
			TownCitizen citizen = this.FindCitizen(ch);
			if (citizen != null) {
				this.citizens.Remove(citizen);
				if (this.mayor == ch) {
					this.mayor = null;
				}
			}
		}

		/// <summary>zaplatit_dan</summary>
		public void PayTax(Player player) {
			TownCitizen citizen = this.FindCitizen(player);
			if (player == this.mayor) {
				player.SysMessage("Jako starosta se tesis privilegiu neplatit dane");
			} else if ((citizen != null) && citizen.paidTax) {
				player.SysMessage("Tve dane uz byly zaplaceny");
			} else if ((citizen != null) && Pay(player, this.tax)) {
				this.MarkPaid(citizen);
				player.SysMessage("Dan zaplacena");
			}
		}

		private void MarkPaid(TownCitizen citizen) {
			this.treasury += this.tax;
			citizen.paidTax = true;
			citizen.amountPaid = this.tax;
		}

		/// <summary>The automatic payment switch of d_tmseznam; switched on, it pays what's due at once.</summary>
		public void ToggleAutoPay(TownCitizen citizen) {
			citizen.autoPay = !citizen.autoPay;
			citizen.who.SysMessage(citizen.autoPay ? "Automaticka platba dani nastavena" : "Automaticka platba dani zrusena");
			if (citizen.autoPay && !citizen.paidTax && (citizen.who != this.mayor)) {
				if (Pay(citizen.who, this.tax)) {
					this.MarkPaid(citizen);
					citizen.who.SysMessage("Dane zaplaceny");
				}
			}
		}

		/// <summary>What's due since the stone was last used, as kontrola_dani: the mayor pays nothing,
		/// a citizen who hasn't paid stops being one, and automatic payment covers the coming period.</summary>
		public void CheckTimers() {
			this.citizens.RemoveAll(citizen => (citizen.who == null) || citizen.who.IsDeleted);
			if (Globals.TimeInSeconds <= this.taxDueAt) {
				return;
			}
			this.taxDueAt += TownSettings.instance.taxPeriodDays * SecondsInDay;
			foreach (TownCitizen citizen in new List<TownCitizen>(this.citizens)) {
				if (citizen.who == this.mayor) {
					continue;
				}
				if (!citizen.paidTax) {
					citizen.who.RedMessage("Nezaplatil jsi dane, pro mesto " + this.Name + " jiz nejsi zadoucim obcanem");
					this.RemoveCitizen(citizen.who);
					continue;
				}
				citizen.paidTax = false;
				citizen.amountPaid = 0;
				if (citizen.autoPay) {
					if (GoldUtils.TryPay(citizen.who, (uint) this.tax)) {
						this.MarkPaid(citizen);
						citizen.who.SysMessage("Dane automaticky zaplaceny - " + this.tax + "gp");
					} else {
						citizen.who.RedMessage("Nedostatek penez na zaplaceni dani, vyres si sve platby!");
					}
				}
			}
		}

		/// <summary>vyhod_starostu, rezignovat</summary>
		public void ClearMayor() {
			this.mayor = null;
		}

		/// <summary>zmenit_home: citizens move for free, others pay the treasury.</summary>
		public void MoveHomeHere(Player player, Point4D home) {
			if (player.Realm != Realm.Gondor) {
				player.SysMessage("Takove jako ty zde nechceme !");
			} else if (this.GetRank(player) >= RankCitizen) {
				player.SysMessage("Jako obcan se tesis privilegiu neplatit za zmenu");
				player.Home = home;
			} else if (Pay(player, TownSettings.instance.homeChangeFee)) {
				this.treasury += TownSettings.instance.homeChangeFee;
				player.Home = home;
				player.SysMessage("Domovske mesto zmeneno");
			}
		}

		/// <summary>The gold players left in the chest: checktax.</summary>
		public void CollectChest() {
			foreach (AbstractItem item in new List<AbstractItem>(this.taxChest)) {
				if (item.Def == GoldUtils.GoldDef) {
					this.treasury += item.Amount;
					item.Delete();
				}
			}
		}

		public override string ToString() {
			return "Town " + this.city;
		}
	}
}
