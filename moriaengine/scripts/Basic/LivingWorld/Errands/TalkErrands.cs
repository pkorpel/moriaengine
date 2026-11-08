using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1291 - errand templates of the talking kind (design 9.1: 11 the quarrel, 12 the secret admirer, 17 the invitation,
	/// 20 the love letter, 21 return the books, 22 the omen, 25 ale for the guards, 26 census). Built from the engine's steps; texts
	/// Czech without diacritics. The war camp gives orders, not these (WarCamp.IsCamp), but ale for the guards.</summary>
	public static class TalkErrands {
		public const string WineDef = "i_bottle_wine", BreadDef = "i_bread_loaf", CheeseDef = "i_cheese_wheel", LetterDef = "i_scroll_blank";
		public const string BookDef = "i_book_sm", AleDef = "i_mug_ale";
		internal static readonly string[] OmenDefs = { "i_reag_garlic", "i_reag_spider_silk", "i_reag_black_pearl", "i_reag_mandrake_root", "i_feather" };

		public static void Bootstrap() {
			ErrandTemplates.Register(new QuarrelErrand());
			ErrandTemplates.Register(new AdmirerErrand());
			ErrandTemplates.Register(new InvitationErrand());
			ErrandTemplates.Register(new LoveLetterErrand());
			ErrandTemplates.Register(new BooksErrand());
			ErrandTemplates.Register(new OmenErrand());
			ErrandTemplates.Register(new AleErrand());
			ErrandTemplates.Register(new CensusErrand());
		}

		internal static bool IsCamp(ErrandContext c) {
			return WarCamp.IsCamp(c.City);
		}

		/// <summary>The thing's name as the errand says it, in the accusative.</summary>
		internal static string Thing(string def) {
			switch (def) {
				case WineDef: return "lahev vina";
				case BreadDef: return "bochnik chleba";
				case CheeseDef: return "kolo syra";
				case "i_reag_garlic": return "palicku cesneku";
				case "i_reag_spider_silk": return "pavuciny";
				case "i_reag_black_pearl": return "cernou perlu";
				case "i_reag_mandrake_root": return "koren mandragory";
				case "i_feather": return "pero";
				default: return "to";
			}
		}
	}

	/// <summary>11. The quarrel: two neighbours argue; hear the other side, then decide. Each side's friendship follows the verdict.</summary>
	internal sealed class QuarrelErrand : ErrandTemplate {
		public override string Key { get { return "quarrel"; } }
		public override string Name { get { return "Hadka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		//the giver's words, and the player's to the other neighbour
		private static readonly string[][] subjects = {
			new[] { "o plot mezi nasimi zahradami", "o plot mezi vasimi zahradami" },
			new[] { "o kozu, ktera mi spasla zeli", "o kozu a spasene zeli" },
			new[] { "o to, kdo ma ucet za pivo", "o to, kdo ma ucet za pivo" },
			new[] { "o vodu ze spolecne studny", "o vodu ze spolecne studny" }
		};

		public override bool TryBuild(ErrandContext c, Errand e) {
			Item other = TalkErrands.IsCamp(c) ? null : c.PickOther();
			if (other == null) {
				return false;
			}
			string[] subject = subjects[Globals.dice.Next(subjects.Length)];
			e.offer = "Hadam se s " + ErrandContext.Who(other) + " " + subject[0] + ". Vyslechni i druhou stranu a rozsud nas. At je klid.";
			e.thanks = "Slovo je slovo. Ted uz je to vyrizene.";
			e.steps.Add(Errands.Talk(other, "Vyslechni " + ErrandContext.Who(other), "Co rikas na tu hadku " + subject[1] + "?",
				ErrandContext.Pick("Je to cele jinak, nez rika! Ja jsem v pravu.", "At si nevymysli. Vsichni vi, jak to bylo.", "Ja jen chci, co je moje.")));
			e.steps.Add(Errands.Choose(c.GiverGem, "Rozsud spor u " + c.GiverName,
				new[] { "Pravdu mas ty.", "Pravdu ma " + BasicErrands.Name(other) + ".", "Oba mate kus pravdy. Podelte se." },
				new[] { BasicErrands.Sex(c.GiverGem, "Vedel", "Vedela") + " jsem to!", "Hm... Tak dobre. Kdyz to rikas ty.", "Asi mas pravdu. Nema cenu se dal prit." }));
			return true;
		}

		public override int GiverFriendship(Errand e, int baseFriend) {
			switch (e.ChoiceAt(1)) {
				case 0: return baseFriend + 2;
				case 1: return baseFriend - 1;
				default: return baseFriend + 1;
			}
		}

		public override void OnDone(Player player, Errand e) {
			int other = (e.ChoiceAt(1) == 0) ? -1 : ((e.ChoiceAt(1) == 1) ? 2 : 1);
			Residents.AddFriendship(player, e.steps[0].residentId, other);
		}
	}

	/// <summary>12. The secret admirer: an anonymous gift; the receiver asks who sent it. Keeping the secret pleases the giver.</summary>
	internal sealed class AdmirerErrand : ErrandTemplate {
		public override string Key { get { return "admirer"; } }
		public override string Name { get { return "Tajny ctitel"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			Item to = TalkErrands.IsCamp(c) ? null : c.PickOther();
			if (to == null) {
				return false;
			}
			e.offer = "Odneses " + ErrandContext.Who(to) + " tuhle lahev vina? A nerikej, od koho je. Ani kdyby se ptal" + BasicErrands.Sex(to, "", "a") + ".";
			e.thanks = "Dekuji ti. A ani slovo, ano?";
			e.steps.Add(Errands.Deliver(to, TalkErrands.WineDef, "Predej dar: " + ErrandContext.Who(to), "Tohle je pro tebe.", "Pro me? A od koho?"));
			e.steps.Add(Errands.Choose(to, "Odpovez " + ErrandContext.Who(to) + ", od koho dar je",
				new[] { "To nemohu rict.", "Posila ti to " + c.GiverName + "." },
				new[] { "Tajemstvi... To se mi libi.", "Opravdu? No... To je mi novina." }));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Dar je predany.", "A co rikal" + BasicErrands.Sex(to, "", "a") + "?"));
			return true;
		}

		public override int GiverFriendship(Errand e, int baseFriend) {
			return baseFriend + ((e.ChoiceAt(1) == 0) ? 1 : -2); //a secret kept, or given away
		}
	}

	/// <summary>17. The invitation: invite three residents to a feast; one of them comes only if the player brings something.</summary>
	internal sealed class InvitationErrand : ErrandTemplate {
		public override string Key { get { return "invitation"; } }
		public override string Name { get { return "Pozvanka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (TalkErrands.IsCamp(c)) {
				return false;
			}
			List<Item> guests = new List<Item>();
			for (int i = 0; i < 3; i++) {
				Item next = c.PickOther(gem => !guests.Contains(gem) && BasicErrands.IsWorker(gem)); //the city's own people, not a prisoner or a passing guest
				if (next == null) {
					return false;
				}
				guests.Add(next);
			}
			int choosy = Globals.dice.Next(guests.Count);
			string wish = ErrandContext.Pick(TalkErrands.WineDef, TalkErrands.BreadDef, TalkErrands.CheeseDef);
			e.offer = "Chystam hostinu a chci pozvat " + ErrandContext.Who(guests[0]) + ", " + ErrandContext.Who(guests[1]) + " a " + ErrandContext.Who(guests[2]) + ". Pozves je za me?";
			e.thanks = "Prijdou vsichni! To bude vecer.";
			for (int i = 0; i < guests.Count; i++) {
				if (i == choosy) {
					e.steps.Add(Errands.Fetch(guests[i], wish, 1, "Pozvi " + ErrandContext.Who(guests[i]) + " (chce " + TalkErrands.Thing(wish) + ")",
						"Zves te " + c.GiverName + " na hostinu. Nesu ti " + TalkErrands.Thing(wish) + ", jak sis prial" + BasicErrands.Sex(guests[i], "", "a") + ".",
						"Tak to prijdu rad" + BasicErrands.Sex(guests[i], "", "a") + "!"));
				} else {
					e.steps.Add(Errands.Talk(guests[i], "Pozvi " + ErrandContext.Who(guests[i]), "Zves te " + c.GiverName + " na hostinu.",
						ErrandContext.Pick("Prijdu! Dekuji.", "Hostina? To si nenecham ujit.", "Rad" + BasicErrands.Sex(guests[i], "", "a") + ". Rekni, ze dorazim.")));
				}
			}
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Vsichni prijdou.", "Vyborne!"));
			return true;
		}
	}

	/// <summary>20. The love letter: a jealous rival stops the courier first and offers money to read it; then the letter is delivered.
	/// Selling it out pays the rival's bribe but costs the friendship of both lovers.</summary>
	internal sealed class LoveLetterErrand : ErrandTemplate {
		public const int RivalBribe = 50;

		public override string Key { get { return "loveletter"; } }
		public override string Name { get { return "Milostny dopis"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			Item to = TalkErrands.IsCamp(c) ? null : c.PickOther();
			Item rival = (to == null) ? null : c.PickOther(gem => gem != to);
			if (rival == null) {
				return false;
			}
			e.offer = "Tenhle dopis je pro " + ErrandContext.Who(to) + ". Je... osobni. A dej si pozor na " + ErrandContext.Who(rival)
				+ ", slidi a jiste se te bude ptat.";
			e.thanks = "Dekuji ti z celeho srdce.";
			e.steps.Add(Errands.Choose(rival, ErrandContext.Who(rival) + " chce s tebou mluvit",
				new[] { "Nic, co by te zajimalo.", "Dopis pro " + BasicErrands.Name(to) + ". Za " + RivalBribe + " zlatych si ho prectes." },
				new[] { "Hm. Ja si to zjistim.", "Ukaz! ... Tady mas. A nikomu ani slovo." }));
			e.steps.Add(Errands.Deliver(to, TalkErrands.LetterDef, "Dorucit dopis: " + ErrandContext.Who(to), "Mam pro tebe dopis od " + c.GiverName + ".",
				"Od " + c.GiverName + "? Ach..."));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Dopis je dorucen.", "Precetl" + BasicErrands.Sex(to, "", "a") + " si ho?"));
			return true;
		}

		public override int GiverFriendship(Errand e, int baseFriend) {
			return baseFriend + ((e.ChoiceAt(0) == 1) ? -3 : 1);
		}

		public override void OnDone(Player player, Errand e) {
			if (e.ChoiceAt(0) == 1) {
				int bribe = (RivalBribe * e.payPercent) / 100; //the repeat penalty's share, as the pay
				if (bribe > 0) {
					GoldUtils.Grant(player, (uint) bribe);
				}
				Residents.AddFriendship(player, e.steps[1].residentId, -1);
			}
		}
	}

	/// <summary>21. Return the books: the library's books are out; each borrower has an excuse, and the last one hands the pile over.</summary>
	internal sealed class BooksErrand : ErrandTemplate {
		public override string Key { get { return "books"; } }
		public override string Name { get { return "Vypujcene knihy"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		private static string Excuse(Item borrower) {
			switch (Globals.dice.Next(4)) {
				case 0: return "Jeste jsem ji " + BasicErrands.Sex(borrower, "nedocetl", "nedocetla") + ", ale dobre, vem si ji.";
				case 1: return "Pes mi ji trochu okousal. Rekni, ze to tak uz bylo.";
				case 2: return BasicErrands.Sex(borrower, "Myslel", "Myslela") + " jsem, ze jsem ji uz " + BasicErrands.Sex(borrower, "vratil", "vratila") + ". Tady je.";
				default: return "Je to nejlepsi kniha, co jsem kdy " + BasicErrands.Sex(borrower, "cetl", "cetla") + ". Skoda ji vracet.";
			}
		}

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (TalkErrands.IsCamp(c) || (BasicErrands.VenueKey(c.GiverGem) != "library")) {
				return false;
			}
			List<Item> borrowers = new List<Item>();
			for (int i = 0; i < 2; i++) {
				Item next = c.PickOther(gem => !borrowers.Contains(gem) && (Residents.WorkplaceOf(gem) != Residents.WorkplaceOf(c.GiverGem)));
				if (next == null) {
					return false;
				}
				borrowers.Add(next);
			}
			e.offer = "Knihy jsou po terminu. Maji je " + ErrandContext.Who(borrowers[0]) + " a " + ErrandContext.Who(borrowers[1]) + ". Vyzvedni je, prosim.";
			e.thanks = "Konecne zase na svem miste. Dekuji.";
			foreach (Item borrower in borrowers) {
				e.steps.Add(Errands.Talk(borrower, "Vyzvedni knihu u " + ErrandContext.Who(borrower), "Knihovna chce knihu zpatky.", Excuse(borrower)));
			}
			e.steps.Add(Errands.Deliver(c.GiverGem, TalkErrands.BookDef, "Vrat knihy " + c.GiverName, "Tady jsou knihy.", "Vsechny! Ty jsi poklad."));
			return true;
		}
	}

	/// <summary>22. The omen: an old woman wants three odd things for a charm.</summary>
	internal sealed class OmenErrand : ErrandTemplate {
		public override string Key { get { return "omen"; } }
		public override string Name { get { return "Znameni"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (TalkErrands.IsCamp(c) || !BasicErrands.IsElder(c.GiverGem) || !Residents.IsFemale(c.GiverGem)) {
				return false;
			}
			List<string> things = new List<string>(TalkErrands.OmenDefs);
			while (things.Count > 3) {
				things.RemoveAt(Globals.dice.Next(things.Count));
			}
			e.offer = "Videla jsem znameni. Na amulet potrebuji " + TalkErrands.Thing(things[0]) + ", " + TalkErrands.Thing(things[1]) + " a "
				+ TalkErrands.Thing(things[2]) + ". Neptej se proc.";
			e.thanks = "Amulet je hotovy. At te chrani na tvych cestach.";
			foreach (string thing in things) {
				e.steps.Add(Errands.Fetch(c.GiverGem, thing, 1, "Prines " + c.GiverName + " " + TalkErrands.Thing(thing), "Nesu ti " + TalkErrands.Thing(thing) + ".", "Dobre... Dobre."));
			}
			return true;
		}
	}

	/// <summary>25. Ale for the guards: a mug from the tavern or the brewery to a guard before the shift changes (a short deadline).</summary>
	internal sealed class AleErrand : ErrandTemplate {
		public override string Key { get { return "ale"; } }
		public override string Name { get { return "Pivo pro strazne"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }
		public override int Minutes { get { return 15; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string venue = BasicErrands.VenueKey(c.GiverGem);
			if (((venue != "tavern") && (venue != "brewery") && (venue != "messhall")) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			Item guard = c.PickOther(gem => (BasicErrands.RoleKey(gem) == "guard") || (BasicErrands.VenueKey(gem) == "barracks"));
			if (guard == null) {
				return false;
			}
			e.offer = "Straz ma za chvili stridani a " + ErrandContext.Who(guard) + " si objednal" + BasicErrands.Sex(guard, "", "a") + " pivo. Pospes si, at nevyprchne.";
			e.thanks = "Strazni budou spokojeni. A ja taky.";
			e.steps.Add(Errands.Deliver(guard, TalkErrands.AleDef, "Donest pivo: " + ErrandContext.Who(guard), "Pivo od " + c.GiverName + ". Tady.",
				ErrandContext.Pick("Konecne! Na zdravi.", "Jeste studene. Dekuju.", "To je ono. Smena muze zacit.")));
			return true;
		}
	}

	/// <summary>26. Census: the town hall counts the households; visit four residents of different workplaces and bring the count back.</summary>
	internal sealed class CensusErrand : ErrandTemplate {
		public override string Key { get { return "census"; } }
		public override string Name { get { return "Scitani"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		private static readonly string[] answers = {
			"Bydlime ctyri, a pes.", "Jen ja a moje kocka.", "Sest, kdyz pocitas i tchyni. Ja ji nepocitam.", "Dva. A brzy tri, jestli bohove dovoli."
		};

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (TalkErrands.IsCamp(c) || (BasicErrands.VenueKey(c.GiverGem) != "townhall")) {
				return false;
			}
			List<Item> households = new List<Item>();
			List<CityVenue> seen = new List<CityVenue> { Residents.WorkplaceOf(c.GiverGem) };
			for (int i = 0; i < 4; i++) {
				Item next = c.PickOther(gem => !seen.Contains(Residents.WorkplaceOf(gem)) && BasicErrands.IsWorker(gem)); //households of the city's own people
				if (next == null) {
					break;
				}
				households.Add(next);
				seen.Add(Residents.WorkplaceOf(next));
			}
			if (households.Count < 3) {
				return false;
			}
			e.offer = "Mesto pocita domacnosti. Zeptej se " + ((households.Count == 3) ? "tri" : "ctyr") + " lidi, kolik jich doma bydli.";
			e.thanks = "Cisla sedi. Mesto ti dekuje.";
			foreach (Item household in households) {
				e.steps.Add(Errands.Talk(household, "Secti domacnost: " + ErrandContext.Who(household), "Kolik vas doma bydli? Je to pro scitani.", ErrandContext.Pick(answers)));
			}
			e.steps.Add(Errands.Talk(c.GiverGem, "Odnes cisla " + c.GiverName, "Tady jsou cisla.", "Hned je zapisu."));
			return true;
		}
	}
}
