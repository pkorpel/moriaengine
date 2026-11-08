using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1282 - Harad and Umbar run on the normal city model (design section 17, option A), with their own colour: their people's
	/// names and looks, how they greet a stranger, which faction an errand serves (by the giver's workplace), and two errands of
	/// their own: a caravan's cargo in Harad, contraband past the harbourmaster in Umbar.
	/// </summary>
	public static class CityFlavour {
		public const string Harad = "Harad", Umbar = "Umbar";
		public const string CargoDef = "i_cloth"; //a bolt of cloth: not a container, so nothing of the player's goes with it when the errand takes it back

		/// <summary>The darker of the client's skin hues (1050-1058), for Harad's people.</summary>
		private const int HaradSkinFirst = 1050, HaradSkins = 9;

		public static void Bootstrap() {
			ErrandTemplates.Register(new CaravanErrand());
			ErrandTemplates.Register(new ContrabandErrand());
		}

		internal static string CityOf(Item gem) {
			CityVenue work = (gem == null) ? null : Residents.WorkplaceOf(gem);
			return (work == null) ? null : work.City;
		}

		internal static bool Is(string city, string which) {
			return string.Equals(city, which, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>A resident's body as their city has it: Harad's are dark-skinned, the same shade for the same person each time.</summary>
		internal static void Dress(Item gem, Character body) {
			if (Is(CityOf(gem), Harad)) {
				string id = Residents.IdOf(gem) ?? "";
				int hash = 0;
				foreach (char c in id) {
					hash = (hash * 31) + c;
				}
				body.Color = (ushort) (HaradSkinFirst + (Math.Abs(hash % HaradSkins)));
			}
		}

		/// <summary>The faction an errand from this giver serves, by their city and workplace; null where none does.</summary>
		public static string FactionOf(Item giverGem) {
			if ((giverGem == null) || !BasicErrands.IsWorker(giverGem)) {
				return null; //a giver gone since, or a guest: no workplace to serve
			}
			string city = CityOf(giverGem);
			string venue = BasicErrands.VenueKey(giverGem);
			if (Is(city, Harad)) {
				switch (venue) {
					case "caravanserai":
					case "market":
						return "ha_caravans";
					case "stable":
					case "kennels":
						return "ha_mumak";
					case "temple":
					case "healing":
						return "ha_serpent";
				}
			} else if (Is(city, Umbar)) {
				switch (venue) {
					case "harbour":
						return "um_harbour";
					case "corsairden":
					case "tavern":
						return "um_corsairs";
					case "market":
					case "slavepen":
						return "um_fences";
				}
			}
			return null;
		}

		/// <summary>How the city's people greet a stranger, or null for the usual words.</summary>
		internal static string StrangerGreeting(string city) {
			if (Is(city, Harad)) {
				return ErrandContext.Pick("Pokoj s tebou, poutniku z dalky.", "Vitej pod nasim sluncem. Co hledas?", "Prach cest je na tobe. Mluv.");
			}
			if (Is(city, Umbar)) {
				return ErrandContext.Pick("Co chces, suchozemce? Mluv rychle.", "Dalsi nova tvar v pristavu. Co tu pohledavas?", "Mas zlato, nebo jen otazky?");
			}
			return null;
		}
	}

	/// <summary>#1282 - Karavana (Harad): a caravan's cargo carried to someone of another trade, then word back.</summary>
	internal sealed class CaravanErrand : ErrandTemplate {
		public override string Key { get { return "caravan"; } }
		public override string Name { get { return "Karavana"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Weight { get { return 16; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			if (!CityFlavour.Is(c.City, CityFlavour.Harad) || !BasicErrands.IsWorker(c.GiverGem) || ((venue != "caravanserai") && (venue != "market") && (venue != "stable"))) {
				return false;
			}
			Item to = c.PickOther(gem => BasicErrands.IsWorker(gem) && (BasicErrands.VenueKey(gem) != venue));
			if (to == null) {
				return false;
			}
			string giver = c.GiverName;
			e.offer = ErrandContext.Pick(
				"Karavana z jihu prisla a jeden naklad patri " + ErrandContext.Who(to) + ". Dovez mu ho, at nemusim posilat honaka.",
				"Koreni a hedvabi nepockaji. Tenhle naklad je pro " + ErrandContext.Who(to) + ". Odvez ho.",
				"Velbloudi jsou unaveni, ale zbozi musi dal. Dones tohle " + ErrandContext.Who(to) + ".");
			e.thanks = ErrandContext.Pick("Dobre. At ti slunce nezhorkne.", "Rychle jako vitr pouste. Dekuji.", "Karavana muze dal. Tady je tvuj podil.");
			e.steps.Add(Errands.Deliver(to, CityFlavour.CargoDef, "Dovez naklad karavany: " + ErrandContext.Who(to),
				"Vezu naklad od " + giver + ".", ErrandContext.Pick("Koreni! Presne na to jsem cekal.", "Konecne. Uz jsem se bal, ze karavanu prepadli.", "Dobre zbozi. Vyrid " + giver + " dik.")));
			e.steps.Add(Errands.Talk(c.GiverGem, "Ohlas " + giver + ", ze naklad dorazil", "Naklad je na miste.", "Dobre. Karavana muze dal."));
			return true;
		}
	}

	/// <summary>#1282 - Kontraband (Umbar): a fence's bundle carried past the harbourmaster, by bribe or by denial, to the buyer.
	/// The bribe is paid at the end; a denial rolls Hiding. Getting caught costs standing with the harbour's masters (and their rivals the
	/// corsair captains gain half); the city itself isn't touched.</summary>
	internal sealed class ContrabandErrand : ErrandTemplate {
		internal const int Bribe = 150, HarbourLoss = 40, HarbourLikesBribes = 5;

		/// <summary>For a test: the denial's roll (0-99), or null for a random one.</summary>
		internal static int? forcedRoll;

		public override string Key { get { return "contraband"; } }
		public override string Name { get { return "Kontraband"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Weight { get { return 16; } }
		public override int Minutes { get { return 40; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			if (!CityFlavour.Is(c.City, CityFlavour.Umbar) || !BasicErrands.IsWorker(c.GiverGem)
					|| ((venue != "market") && (venue != "slavepen") && (venue != "corsairden") && (venue != "tavern"))) {
				return false;
			}
			Item inspector = c.PickOther(gem => (BasicErrands.VenueKey(gem) == "harbour") && (BasicErrands.RoleKey(gem) == "harbourmaster"))
				?? c.PickOther(gem => BasicErrands.VenueKey(gem) == "harbour");
			if (inspector == null) {
				return false;
			}
			Item buyer = c.PickOther(gem => (gem != inspector) && BasicErrands.IsWorker(gem) && (BasicErrands.VenueKey(gem) != "harbour"));
			if (buyer == null) {
				return false;
			}
			string giver = c.GiverName;
			e.offer = "Mam balik pro " + ErrandContext.Who(buyer) + ". Nikdo se nesmi dozvedet, co v nem je. V pristavu hlida " + ErrandContext.Who(inspector)
				+ ". Bud ho podplat, nebo mu lzi do oci. Je to na tobe.";
			e.thanks = "Mlcet umis. To se v Umbaru ceni.";
			e.steps.Add(Errands.Choose(inspector, "Projdi s balikem kolem " + ErrandContext.Who(inspector),
				new[] { "Tohle je pro tebe (" + Bribe + " zlatych).", "Nic nevezu, jen sve veci." },
				new[] { "Nic jsem nevidel. Jdi.", "Hm... Tak jdi. Ale mam te na ocich." }));
			e.steps.Add(Errands.Deliver(buyer, CityFlavour.CargoDef, "Dorucit balik: " + ErrandContext.Who(buyer),
				"Mam pro tebe zbozi od " + giver + ".", "Konecne. Nikdo te nevidel?"));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + giver, "Balik je dorucen.", "Dobre."));
			return true;
		}

		/// <summary>The way past the harbourmaster settled: the bribe paid now (none to pay: he remembers), or the denial's Hiding roll
		/// (Hiding/10 percent, at least 20).</summary>
		public override void OnDone(Player player, Errand errand) {
			if (errand.ChoiceAt(0) == 0) {
				if (GoldUtils.Pay(player, Bribe, PaySource.Usual, "")) {
					Reputation.ChangeFaction(player, "um_harbour", HarbourLikesBribes);
					player.SysMessage("Zaplatil jsi strazneho pristavu " + Bribe + " zlatych.");
				} else {
					Reputation.ChangeFaction(player, "um_harbour", -HarbourLoss);
					player.RedMessage("Na uplatek jsi nemel. Pristavni mistr si te zapamatoval.");
				}
			} else if ((forcedRoll ?? Globals.dice.Next(100)) >= DenialChance(player)) {
				Reputation.ChangeFaction(player, "um_harbour", -HarbourLoss);
				player.RedMessage("Pristavni mistr ti neuveril. Pani pristavu si te zapamatuji.");
			} else {
				player.SysMessage("Pristavni mistr nic nepoznal.");
			}
		}

		internal static int DenialChance(Player player) {
			return Math.Max(20, player.GetSkill((int) SkillName.Hiding) / 10);
		}
	}
}
