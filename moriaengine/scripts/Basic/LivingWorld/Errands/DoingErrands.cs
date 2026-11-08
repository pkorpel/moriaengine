using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1293 - errand templates of the doing kind on the engine's own steps (design 9.1: 7 a commission, 10 the courier race,
	/// 23 words for the dead, 24 the brawl, 27 the stowaway). Texts Czech without diacritics. The war camp gives orders, not these.</summary>
	public static class DoingErrands {
		public const string PackageDef = "i_scroll_blank";

		public static void Bootstrap() {
			ErrandTemplates.Register(new CommissionErrand());
			ErrandTemplates.Register(new CourierErrand());
			ErrandTemplates.Register(new GhostWordsErrand());
			ErrandTemplates.Register(new BrawlErrand());
			ErrandTemplates.Register(new StowawayErrand());
		}

		internal static bool IsCamp(ErrandContext c) {
			return WarCamp.IsCamp(c.City);
		}
	}

	/// <summary>7. A commission: the giver wants something made (a dagger, a shirt, a loaf), by the player's own hands or bought.</summary>
	internal sealed class CommissionErrand : ErrandTemplate {
		//the def, its name in the accusative, and the pronoun for it
		private static readonly string[][] goods = {
			new[] { "i_dagger", "dyku", "ji" }, new[] { "i_shirt_plain", "kosili", "ji" }, new[] { "i_bread_loaf", "bochnik chleba", "ho" }
		};

		/// <summary>For a test: which of the goods (0-2), or null for a random one.</summary>
		internal static int? forcedGood;

		public override string Key { get { return "commission"; } }
		public override string Name { get { return "Zakazka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (DoingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			string[] good = goods[forcedGood ?? Globals.dice.Next(goods.Length)];
			e.offer = "Potreboval" + BasicErrands.Sex(c.GiverGem, "", "a") + " bych " + good[1] + ". Udelas mi " + good[2] + "? Nebo " + good[2] + " aspon sezen.";
			e.thanks = ErrandContext.Pick("Pekna prace. Dekuji.", "Presne tohle jsem potreboval" + BasicErrands.Sex(c.GiverGem, "", "a") + ".", "Dobre udelane.");
			e.steps.Add(Errands.Fetch(c.GiverGem, good[0], 1, "Prines " + c.GiverName + " " + good[1], "Nesu ti " + good[1] + ".", "Ukaz... Ano, to je ono."));
			return true;
		}
	}

	/// <summary>10. The courier race: a sealed package to a resident in another city of the giver's realm, on a short deadline;
	/// arriving with half the time left pays more (LOTRO's postal service).</summary>
	internal sealed class CourierErrand : ErrandTemplate {
		internal const int FastPercent = 130;

		public override string Key { get { return "courier"; } }
		public override string Name { get { return "Kuryr"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } } //not Large: a travel stone makes it quick, and a large errand's magic bag isn't scaled by the pay
		public override int Minutes { get { return 30; } }
		public override int Weight { get { return 5; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (DoingErrands.IsCamp(c) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			Realm realm = Reputation.RealmOfCity(c.City);
			List<Item> far = new List<Item>();
			foreach (Item gem in Residents.AllGems) {
				CityVenue work = Residents.WorkplaceOf(gem);
				if ((work != null) && !string.Equals(work.City, c.City, StringComparison.OrdinalIgnoreCase) && BasicErrands.IsWorker(gem)
						&& (Reputation.RealmOfCity(work.City) == realm) && !WarCamp.IsCamp(work.City)) {
					far.Add(gem);
				}
			}
			if ((realm == Realm.None) || (far.Count == 0)) {
				return false;
			}
			Item to = far[Globals.dice.Next(far.Count)];
			string where = Residents.WorkplaceOf(to).City.Replace('_', ' ');
			e.offer = "Tenhle zapeceteny balicek musi do mesta " + where + ", pro " + ErrandContext.Who(to) + ". Spechej: kdo tam bude rychle, dostane vic.";
			e.thanks = "Vcas! Na tebe je spoleh.";
			e.steps.Add(Errands.Deliver(to, DoingErrands.PackageDef, "Dorucit balicek do " + where + ": " + ErrandContext.Who(to),
				"Balicek od " + c.GiverName + ", z daleka.", "Uz jsem ho cekal" + BasicErrands.Sex(to, "", "a") + ". Dekuji, rychly posle."));
			return true;
		}

		/// <summary>More for speed: done with half the time or more left.</summary>
		public override int PayPercent(Errand e) {
			double left = (e.Expires - Errands.Now).TotalMinutes;
			return (left * 2 >= WarCamp.Minutes(e.city, this.Minutes)) ? FastPercent : 100;
		}
	}

	/// <summary>23. Words for the dead: at night the priest feels a spirit with a message for a living relative; the player carries
	/// it. Only for those who hear the dead (Spirit Speak).</summary>
	internal sealed class GhostWordsErrand : ErrandTemplate {
		internal const int SpiritSpeakNeeded = 300, NightFrom = 22, NightTo = 4;

		public override string Key { get { return "ghostwords"; } }
		public override string Name { get { return "Slova mrtvych"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (DoingErrands.IsCamp(c) || (BasicErrands.VenueKey(c.GiverGem) != "temple") || (BasicErrands.RoleKey(c.GiverGem) != "priest") || (c.Player.GetSkill((int) SkillName.SpiritSpeak) < SpiritSpeakNeeded)) {
				return false;
			}
			Item relative = c.PickOther(gem => BasicErrands.IsWorker(gem) && (Residents.WorkplaceOf(gem) != Residents.WorkplaceOf(c.GiverGem)));
			if (relative == null) {
				return false;
			}
			string dead = NameList.Random(Residents.NameList(c.City, false)) ?? "Hardo";
			e.offer = "Neco tu v noci bloudi. Ty slysis mrtve, ze? Prijd do chramu po setmeni. Mozna ti neco rekne.";
			e.thanks = "Duch uz je klidny. Dekuji ti.";
			e.steps.Add(Errands.Wait(c.GiverGem, NightFrom, NightTo, "Prijd v noci do chramu (" + NightFrom + "-" + NightTo + " h)", "Jsem tu. Citim ho.",
				"Je tu... Je to " + dead + ". Chce, aby " + ErrandContext.Who(relative) + " vedel" + BasicErrands.Sex(relative, "", "a") + ", ze " + BasicErrands.Sex(relative, "mu", "ji") + " odpousti."));
			e.steps.Add(Errands.Talk(relative, "Vyrid vzkaz mrtveho: " + ErrandContext.Who(relative), "Mam pro tebe vzkaz od " + dead + ". Odpousti ti.",
				"Od " + dead + "? ... Celou dobu jsem na to cekal" + BasicErrands.Sex(relative, "", "a") + ". Dekuji."));
			return true;
		}
	}

	/// <summary>24. The brawl: two guests fight at the tavern. Split them (the innkeeper's thanks), or bet on one: the stake is paid
	/// at the end, and half the time it comes back double; the innkeeper pays nothing for that.</summary>
	internal sealed class BrawlErrand : ErrandTemplate {
		internal const int Bet = 50;

		/// <summary>For a test: the bet's roll (0-99, below 50 wins), or null for a random one.</summary>
		internal static int? forcedRoll;

		public override string Key { get { return "brawl"; } }
		public override string Name { get { return "Rvacka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (DoingErrands.IsCamp(c) || (BasicErrands.VenueKey(c.GiverGem) != "tavern") || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			CityVenue tavern = Residents.WorkplaceOf(c.GiverGem);
			Item first = c.PickOther(gem => Residents.WorkplaceOf(gem) == tavern) ?? c.PickOther();
			Item second = (first == null) ? null : c.PickOther(gem => gem != first);
			if (second == null) {
				return false;
			}
			e.offer = ErrandContext.Who(first) + " a " + ErrandContext.Who(second) + " se perou! Udelej s tim neco, nez mi rozbiji hospodu.";
			e.thanks = "Klid. Konecne.";
			e.steps.Add(Errands.Choose(first, "Zakroc v rvacce: " + ErrandContext.Who(first) + " a " + ErrandContext.Who(second),
				new[] { "Dost! Rozejdete se, nebo vas vyhodim.", "Vsadim " + Bet + " zlatych, ze vyhrajes." },
				new[] { "Dobre, dobre... Uz jsem " + BasicErrands.Sex(first, "klidny", "klidna") + ".", "Ha! Tak se divej!" }));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Rvacka skoncila.", ErrandContext.Pick("Diky.", "No konecne.")));
			return true;
		}

		public override int PayPercent(Errand e) {
			return (e.ChoiceAt(0) == 1) ? 0 : 100; //the innkeeper doesn't pay for a bet
		}

		public override int GiverFriendship(Errand e, int baseFriend) {
			return baseFriend + ((e.ChoiceAt(0) == 1) ? -1 : 1);
		}

		public override void OnDone(Player player, Errand e) {
			if (e.ChoiceAt(0) != 1) {
				return;
			}
			if (player.IsGM) {
				player.SysMessage("Jako GM nesazis."); //a GM pays nothing, so a bet would be free gold
				return;
			}
			if (!GoldUtils.Pay(player, Bet, PaySource.Usual, "")) {
				player.SysMessage("Na sazku jsi nemel. Nikdo s tebou nesazel.");
				return;
			}
			if ((forcedRoll ?? Globals.dice.Next(100)) < 50) {
				GoldUtils.Grant(player, 2 * Bet);
				player.SysMessage("Tvuj borec vyhral! Beres " + (2 * Bet) + " zlatych.");
			} else {
				player.SysMessage("Tvuj borec prohral. Sazka je pryc.");
			}
		}
	}

	/// <summary>27. The stowaway: someone hides aboard a ship; hand them over to the harbour (its thanks) or let them go (their
	/// friendship, half the pay).</summary>
	internal sealed class StowawayErrand : ErrandTemplate {
		public override string Key { get { return "stowaway"; } }
		public override string Name { get { return "Cerny pasazer"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (DoingErrands.IsCamp(c) || (BasicErrands.VenueKey(c.GiverGem) != "harbour") || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			Item stowaway = c.PickOther(gem => Residents.WorkplaceOf(gem) != Residents.WorkplaceOf(c.GiverGem));
			if (stowaway == null) {
				return false;
			}
			e.offer = "Na lodi se nekdo schovava. Mam podezreni na " + ErrandContext.Who(stowaway) + ". Zjisti to a prived mi " + BasicErrands.Sex(stowaway, "ho", "ji") + ".";
			e.thanks = "Pristav ma sva pravidla. Dekuji.";
			e.steps.Add(Errands.Choose(stowaway, "Promluv si s " + ErrandContext.Who(stowaway),
				new[] { "Pujdes se mnou za " + c.GiverName + ".", "Zmiz, nez si te nekdo vsimne. Nic jsem nevidel." },
				new[] { "Dobre... nechci potize.", "Dekuji! Nikdy ti to nezapomenu!" }));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Byl jsem na lodi.", "A?"));
			return true;
		}

		public override int PayPercent(Errand e) {
			return (e.ChoiceAt(0) == 1) ? 50 : 100;
		}

		public override int GiverFriendship(Errand e, int baseFriend) {
			return baseFriend + ((e.ChoiceAt(0) == 1) ? -1 : 1);
		}

		public override void OnDone(Player player, Errand e) {
			if (e.ChoiceAt(0) == 1) {
				Residents.AddFriendship(player, e.steps[0].residentId, 2);
			}
		}
	}
}
