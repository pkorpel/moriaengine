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

	/// <summary>Someone standing for mayor: their crystal in the candidates' pouch.</summary>
	[SaveableClass]
	public class TownCandidate {
		[LoadingInitializer]
		public TownCandidate() {
		}

		[SaveableData]
		public Character who;

		[SaveableData]
		public int votes;
	}

	/// <summary>A name on a blacklist, or suggested for one. The ban is on that name of the character, so
	/// someone masked as another isn't recognized, and a mask of a banned name doesn't get them banned.</summary>
	[SaveableClass]
	public class TownBan {
		[LoadingInitializer]
		public TownBan() {
		}

		[SaveableData]
		public Character who;

		[SaveableData]
		public string name;

		[SaveableData]
		public string reason;

		/// <summary>Who suggested it, for a suggestion.</summary>
		[SaveableData]
		public string suggestedBy;
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

		/// <summary>tag.funkce_major, when the mayor's term began; 0 when there's no term to wait for.</summary>
		[SaveableData]
		public double mayorSince;

		/// <summary>tag.zacatek_voleb</summary>
		[SaveableData]
		public double electionStart;

		[SaveableData]
		public List<TownCandidate> candidates = new List<TownCandidate>();

		/// <summary>Who has voted in this election.</summary>
		[SaveableData]
		public List<Character> voters = new List<Character>();

		[SaveableData]
		public List<string> news = new List<string>();

		/// <summary>The citizens' pouch of enemies: tag.enemyscont.</summary>
		[SaveableData]
		public List<TownBan> banned = new List<TownBan>();

		/// <summary>tag.suggestedenemyscont</summary>
		[SaveableData]
		public List<TownBan> suggestedBans = new List<TownBan>();

		[SaveableData]
		public GuardForce force;

		/// <summary>The town's hired guards.</summary>
		public GuardForce Force {
			get {
				if (this.force == null) {
					this.force = new GuardForce { city = this.city, town = this };
				}
				this.force.EnsureCheckTimer(this.stone);
				return this.force;
			}
		}

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

		/// <summary>Where a player's .navrhban goes: their town, or in Mordor, which has no citizens, the
		/// Mordor city they're in.</summary>
		public static Town ForSuggesting(Character ch) {
			if (ch.Realm != Realm.Mordor) {
				return OfCitizen(ch);
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(ch.Region);
			return ((area != null) && (area.Realm == Realm.Mordor) && !string.IsNullOrEmpty(area.City)) ? ForCity(area.City) : null;
		}

		/// <summary>The town this character rules.</summary>
		public static Town OfRuler(Character ch) {
			foreach (Town town in all) {
				if (town.mayor == ch) {
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

		/// <summary>The realm of the map area its stone stands in.</summary>
		public Realm Realm {
			get {
				FlaggedRegion area = RegionEventSystem.AreaOf(this.stone.Region);
				return (area != null) ? area.Realm : Realm.None;
			}
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
			if ((ch == this.mayor) || ((this.Realm == Realm.Mordor) && NazgulRings.IsFirst(ch))) {
				return RankMayor;
			}
			if (this.FindCitizen(ch) != null) {
				return RankCitizen;
			}
			return (this.FindBan(ch) != null) ? RankEnemy : RankPublic;
		}

		public TownBan FindBan(Character ch) {
			return FindIn(this.banned, ch);
		}

		public TownBan FindSuggestion(Character ch) {
			return FindIn(this.suggestedBans, ch);
		}

		private static TownBan FindIn(List<TownBan> bans, Character ch) {
			foreach (TownBan ban in bans) {
				if ((ban.who == ch) && (ban.name == ch.Name)) {
					return ban;
				}
			}
			return null;
		}

		/// <summary>blacklist_him</summary>
		public void Ban(Character who, string reason) {
			this.banned.Add(new TownBan { who = who, name = who.Name, reason = reason });
			who.RedMessage("Byl jsi dan na cernou listinu mesta " + this.Name);
		}

		/// <summary>navrhni</summary>
		public void SuggestBan(Character who, string reason, Character by) {
			this.suggestedBans.Add(new TownBan { who = who, name = who.Name, reason = reason, suggestedBy = by.Name });
			who.RedMessage("Byl jsi navrzen na blacklist mesta " + this.Name + ". Ocekavej rozhodnuti starosty.");
		}

		/// <summary>Which town's guards a guard is: the city of the area it calls home.</summary>
		public static Town OfGuard(NPC guard) {
			FlaggedRegion area = RegionEventSystem.AreaOf(HomeRegion(guard));
			return ((area != null) && !string.IsNullOrEmpty(area.City)) ? ForCity(area.City) : null;
		}

		private static Region HomeRegion(NPC guard) {
			NPCAiTimer timer = NPCAiTimer.GetFor(guard);
			if ((timer != null) && timer.hasHome) {
				return Map.GetMap(timer.homeM).GetRegionFor(new Point2D(timer.homeX, timer.homeY));
			}
			return guard.Region;
		}

		/// <summary>isfrommytown: a citizen of the town this guard guards.</summary>
		public static bool IsFromGuardsTown(NPC guard, Character ch) {
			Town town = OfGuard(guard);
			return (town != null) && (town.FindCitizen(ch) != null);
		}

		/// <summary>isbannedplayer: on the blacklist of the town this guard guards, unless a citizen of it.</summary>
		public static bool IsBannedFor(NPC guard, Character ch) {
			Town town = OfGuard(guard);
			if ((town != null) && (town.FindCitizen(ch) != null)) {
				return false;
			}
			return RealmOffice.IsBannedFor(guard, ch) || ((town != null) && (town.FindBan(ch) != null));
		}

		/// <summary>The warning of isbannedplayer, to someone who hasn't crossed into the town yet.</summary>
		public static void WarnBanned(NPC guard, Character ch) {
			FlaggedRegion guardsArea = RegionEventSystem.AreaOf(HomeRegion(guard));
			if (IsBannedFor(guard, ch) && (RegionEventSystem.AreaOf(ch.Region) != guardsArea)) {
				guard.Say("Aaaa " + ch.Name + (RealmOffice.IsBannedFor(guard, ch)
					? ", mame prikaz nevpustit te do nasich mest, strez se vstoupit!"
					: ", mame prikaz nevpustit te do mesta, strez se prekrocit jeho hranici!"));
				guard.Say("Zmizni od mesta!");
			}
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
				this.WithdrawCandidate(ch);
				if (this.mayor == ch) {
					this.ClearMayor();
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
			this.CheckTax();
			this.CheckElection();
		}

		private void CheckTax() {
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

		/// <summary>vyhod_starostu, rezignovat: the next candidate needn't wait for the term to end.</summary>
		public void ClearMayor() {
			this.mayor = null;
			this.mayorSince = 0;
		}

		public void SetMayor(Character ch) {
			this.mayor = ch;
			this.mayorSince = Globals.TimeInSeconds;
		}

		public TownCandidate FindCandidate(Character ch) {
			foreach (TownCandidate candidate in this.candidates) {
				if (candidate.who == ch) {
					return candidate;
				}
			}
			return null;
		}

		private void TellCitizens(string text) {
			foreach (TownCitizen citizen in this.citizens) {
				citizen.who.SysMessage(text);
			}
		}

		/// <summary>kandidovat: the first candidate starts the election, once the mayor's term is over.</summary>
		public void Stand(Player player) {
			if (player.IsGM) {
				player.SysMessage("GM na starostu nekandiduji");
			} else if (this.FindCandidate(player) != null) {
				player.SysMessage("Kandidatem na starostu uz jsi.");
			} else if ((this.candidates.Count == 0) && (this.mayorSince > 0) &&
					(Globals.TimeInSeconds < (this.mayorSince + (TownSettings.instance.mayorTermDays * SecondsInDay)))) {
				player.RedMessage("Jeste neskoncilo funkcni obdobi soucasneho starosty");
			} else {
				if (this.candidates.Count == 0) {
					this.electionStart = Globals.TimeInSeconds;
					this.TellCitizens("Nove volby starosty zahajeny!");
				}
				this.candidates.Add(new TownCandidate { who = player });
				this.TellCitizens("Mestsky Hlasatel: Novy kandidat na starostu mesta, muzete hlasovat.");
				player.SysMessage("Tva kandidatura na starostu mesta " + this.Name + " prijata.");
			}
		}

		public void WithdrawCandidate(Character ch) {
			TownCandidate candidate = this.FindCandidate(ch);
			if (candidate != null) {
				this.candidates.Remove(candidate);
				ch.RedMessage("Kandidatura zrusena");
			}
		}

		/// <summary>The vote of d_tmseznam: citizens who've paid their tax, once, and not for themselves.</summary>
		public void Vote(Player voter, TownCandidate candidate) {
			TownCitizen citizen = this.FindCitizen(voter);
			if (citizen == null) {
				return;
			}
			if (!citizen.paidTax && (voter != this.mayor)) {
				voter.RedMessage("Neplatic dani nemuze hlasovat! Zaplat nejdrive dane!");
			} else if (this.voters.Contains(voter)) {
				voter.SysMessage("V techto volbach uz jsi jednou hlasoval");
			} else if (candidate.who == voter) {
				voter.SysMessage("Sam pro sebe hlasovat nemuzes");
			} else {
				candidate.votes++;
				this.voters.Add(voter);
				voter.SysMessage("Hlas zaznamenan");
			}
		}

		/// <summary>kontrola_voleb, ukonci_volby: the most votes win. Of a tie, a sitting mayor stays, or the lot decides.</summary>
		private void CheckElection() {
			this.candidates.RemoveAll(candidate => (candidate.who == null) || candidate.who.IsDeleted);
			if ((this.candidates.Count == 0) ||
					(Globals.TimeInSeconds <= (this.electionStart + (TownSettings.instance.electionDays * SecondsInDay)))) {
				return;
			}
			int mostVotes = 0;
			foreach (TownCandidate candidate in this.candidates) {
				mostVotes = Math.Max(mostVotes, candidate.votes);
			}
			List<TownCandidate> best = this.candidates.FindAll(candidate => candidate.votes == mostVotes);
			Character sitting = this.mayor;
			Character winner = best.Exists(candidate => candidate.who == sitting) ? sitting : best[Globals.dice.Next(best.Count)].who;
			if ((sitting != null) && (sitting != winner)) {
				sitting.RedMessage("Prisel jsi ve volbach o urad starosty ve meste " + this.Name);
			}
			this.TellCitizens("Volby ukonceny, starostou byl " + ((winner == sitting) ? "znovu" : "") + "zvolen " + winner.Name + ".");
			winner.SysMessage(((winner == sitting) ? "Byl jsi znovuzvolen starostou mesta " : "Byl jsi zvolen starostou mesta ") + this.Name);
			this.SetMayor(winner);
			this.candidates.Clear();
			this.voters.Clear();
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
