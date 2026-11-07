using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1252 - the first eight errand templates (design 9.1: 1 the letter, 2 ask around, 5 market run, 6 ingredients, 3 lost and
	/// found, 8 the debt, 9 a slow walk, 19 a shift at work). Texts are Czech without diacritics, a few to pick from for each line.</summary>
	public static class BasicErrands {
		public static void Bootstrap() {
			ErrandTemplates.Register(new LetterErrand());
			ErrandTemplates.Register(new AskAroundErrand());
			ErrandTemplates.Register(new MarketRunErrand());
			ErrandTemplates.Register(new IngredientsErrand());
			ErrandTemplates.Register(new LostAndFoundErrand());
			ErrandTemplates.Register(new DebtErrand());
			ErrandTemplates.Register(new SlowWalkErrand());
			ErrandTemplates.Register(new ShiftErrand());
		}

		internal static string Name(Item gem) {
			return Residents.NameOf(gem);
		}

		internal static string RoleKey(Item gem) {
			return Residents.RoleKeyOf(gem) ?? "";
		}

		internal static string VenueKey(Item gem) {
			CityVenue work = Residents.WorkplaceOf(gem);
			return ((work == null) || (work.VenueType == null)) ? "" : work.VenueType.Key;
		}

		/// <summary>A word as the resident's gender wants it: Sex(gem, "chtel", "chtela").</summary>
		internal static string Sex(Item gem, string male, string female) {
			return Residents.IsFemale(gem) ? female : male;
		}

		internal static bool IsWorker(Item gem) {
			string role = RoleKey(gem);
			return (role != "patron") && (role != "spectator") && (role != "bather") && (role != "pilgrim") && (role != "patient") && (role != "prisoner");
		}

		internal static bool IsElder(Item gem) {
			string role = RoleKey(gem);
			return (role == "scholar") || (role == "librarian") || (role == "priest") || (role == "pilgrim") || (role == "patient") || (role == "farmer") || (role == "patron");
		}
	}

	/// <summary>1. The letter: a sealed letter to another resident, who sometimes writes back.</summary>
	internal sealed class LetterErrand : ErrandTemplate {
		public override string Key { get { return "letter"; } }
		public override string Name { get { return "Dopis"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }
		public override int Weight { get { return 14; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			Item to = c.PickOther();
			if (to == null) {
				return false;
			}
			string giver = c.GiverName, receiver = BasicErrands.Name(to);
			e.offer = ErrandContext.Pick(
				"Odneses tenhle dopis " + ErrandContext.Who(to) + "? Je to dulezite a nemam na to cas.",
				"Mam psani k doruceni. Adresat je " + ErrandContext.Who(to) + ". Zaplatim ti cestu.",
				"Tady mam dopis pro " + ErrandContext.Who(to) + ". Neotvirej ho, prosim.");
			e.thanks = ErrandContext.Pick("Dekuji. Na tebe je spoleh.", "Vyborne, presne tak to melo byt.", "Diky, priteli.");
			e.steps.Add(Errands.Deliver(to, "i_scroll_blank", "Odnes dopis: " + ErrandContext.Who(to),
				"Nesu ti dopis od " + giver + ".", ErrandContext.Pick("Konecne! Uz na nej cekam.", "Od " + giver + "? To je mi novina.", "Dekuji, hned si ho prectu.")));
			if (Globals.dice.Next(3) == 0) {
				//the reply goes back
				e.steps.Add(Errands.Deliver(c.GiverGem, "i_scroll_blank", "Odnes odpoved " + giver,
					"Nesu ti odpoved od " + receiver + ".", "Odpoved! Vyborne."));
				e.tier = (int) ErrandTier.Medium;
			} else {
				e.steps.Add(Errands.Talk(c.GiverGem, "Rekni " + giver + ", ze dopis dosel", "Dopis je dorucen.", "Dobre."));
			}
			return true;
		}
	}

	/// <summary>2. Ask around: the giver wants news of someone; two or three people point onward, the last one knows.</summary>
	internal sealed class AskAroundErrand : ErrandTemplate {
		public override string Key { get { return "askaround"; } }
		public override string Name { get { return "Vyptavani"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		//"o kom": the locative, as "nevim nic o ..." wants it
		private static readonly string[] subjects = { "mem bratrovi", "toulave kocce z nasi ulice", "jednom cizinci v sedem plasti", "mem synovci", "obchodnikovi s latkami" };

		public override bool TryBuild(ErrandContext c, Errand e) {
			List<Item> chain = new List<Item>();
			int links = 2 + Globals.dice.Next(2);
			for (int i = 0; i < links; i++) {
				Item next = c.PickOther(gem => !chain.Contains(gem));
				if (next == null) {
					break;
				}
				chain.Add(next);
			}
			if (chain.Count < 2) {
				return false;
			}
			string subject = ErrandContext.Pick(subjects);
			e.offer = "Nevim nic o " + subject + " uz nekolik dni. Zeptej se lidi ve meste, prosim. Zacni u " + ErrandContext.Who(chain[0]) + ".";
			e.thanks = "Takze je v poradku! Dekuji ti, to je velka uleva.";
			for (int i = 0; i < chain.Count; i++) {
				bool last = i == chain.Count - 1;
				string reply = last ? "Ano, vim o tom. Vse je v poradku, jen je toho ted hodne. Vyrid to." :
					"Nevim, ale zeptej se " + ErrandContext.Who(chain[i + 1]) + ". Vi o vsem, co se tu deje.";
				string task = (i == 0) ? "Zeptej se " + ErrandContext.Who(chain[i]) : "Zeptej se dal: " + ErrandContext.Who(chain[i]);
				e.steps.Add(Errands.Talk(chain[i], task, "Nevis neco o " + subject + "?", reply));
			}
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Mam zpravy.", "Povidej!"));
			return true;
		}
	}

	/// <summary>5. Market run: bring the giver N of something sold in the city.</summary>
	internal sealed class MarketRunErrand : ErrandTemplate {
		public override string Key { get { return "market"; } }
		public override string Name { get { return "Nakup"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		//the def, the count and the counted noun as it goes with that count
		private static readonly string[][] goods = {
			new[] { "i_bread_loaf", "bochniky chleba", "3" }, new[] { "i_fruit_apple", "jablek", "5" },
			new[] { "i_cheese_wedge", "kusy syra", "2" }, new[] { "i_bottle_wine", "lahev vina", "1" }, new[] { "i_candle", "svicky", "3" }
		};

		public override bool TryBuild(ErrandContext c, Errand e) {
			string[] item = goods[Globals.dice.Next(goods.Length)];
			if (!(ThingDef.GetByDefname(item[0]) is ItemDef)) {
				return false;
			}
			int count = int.Parse(item[2], System.Globalization.CultureInfo.InvariantCulture);
			e.offer = "Nestiham dojit na trh. Prines mi " + item[2] + " " + item[1] + ", penize ti vratim i s necim navic.";
			e.thanks = "Presne tohle bylo potreba. Tady mas za namahu.";
			e.steps.Add(Errands.Fetch(c.GiverGem, item[0], count, "Prines " + c.GiverName + " " + item[2] + " " + item[1], "Tady je nakup.", "Diky!"));
			return true;
		}
	}

	/// <summary>6. Ingredients: what the giver's trade needs, from gathering or a shop.</summary>
	internal sealed class IngredientsErrand : ErrandTemplate {
		public override string Key { get { return "ingredients"; } }
		public override string Name { get { return "Suroviny"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!BasicErrands.IsWorker(c.GiverGem)) {
				return false; //a guest has no trade to supply
			}
			string def, what;
			int count;
			switch (BasicErrands.VenueKey(c.GiverGem)) {
				case "smithy": case "armoury": case "forge": def = "i_ingot_iron"; what = "zeleznych ingotu"; count = 10; break;
				case "tavern": case "messhall": case "bakery": def = "i_fish_raw"; what = "syrovych ryb"; count = 5; break;
				case "healing": case "temple": def = "i_reag_ginseng"; what = "korenu zensenu"; count = 10; break;
				case "harbour": case "stable": case "farm": def = "i_log"; what = "polen dreva"; count = 20; break;
				case "brewery": def = "i_fruit_apple"; what = "jablek na most"; count = 10; break;
				default: return false;
			}
			if (!(ThingDef.GetByDefname(def) is ItemDef)) {
				return false;
			}
			e.offer = "Uz mi skoro nezbylo " + what + ". Kdyby se ti jich podarilo sehnat " + count + ", dobre zaplatim. Kde je vezmes, je na tobe.";
			e.thanks = "Ted muzu zase pracovat. Dekuji!";
			e.steps.Add(Errands.Fetch(c.GiverGem, def, count, "Prines " + c.GiverName + " " + count + " " + what, "Nesu, co jsi " + BasicErrands.Sex(c.GiverGem, "chtel", "chtela") + ".", "Vyborne, to mi vystaci."));
			return true;
		}
	}

	/// <summary>3. Lost and found: something the giver lost lies in a venue of the city; find it and bring it back.</summary>
	internal sealed class LostAndFoundErrand : ErrandTemplate {
		public override string Key { get { return "lost"; } }
		public override string Name { get { return "Ztracena vec"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		//the item, what was lost (as the giver says it), "here is your ..." (as the player says it) and "bring it" (vrat ho/ji/ho)
		private static readonly string[][] things = {
			new[] { "i_ring_gold", "zlaty prsten", "tvuj zlaty prsten", "ho" }, new[] { "i_necklace_silver", "stribrny nahrdelnik", "tvuj stribrny nahrdelnik", "ho" },
			new[] { "i_book_sm", "knizku", "tvoje knizka", "ji" }, new[] { "i_hammer", "kladivo", "tvoje kladivo", "ho" }
		};

		public override bool TryBuild(ErrandContext c, Errand e) {
			Item where = c.PickOther();
			CityVenue venue = (where == null) ? null : Residents.WorkplaceOf(where);
			if ((venue == null) || (ResidentSchedule.SpotIn(venue, "lost") == null)) {
				return false; //nowhere in it to lie: it would end up in the pack, and that's no search
			}
			string[] thing = things[Globals.dice.Next(things.Length)];
			if (!(ThingDef.GetByDefname(thing[0]) is ItemDef)) {
				return false;
			}
			e.offer = "Nemuzu najit " + thing[1] + "! Naposledy " + BasicErrands.Sex(c.GiverGem, "jsem byl", "jsem byla") + " v: " + venue.Label + ". Podivas se tam?";
			e.thanks = "To je ono! " + BasicErrands.Sex(c.GiverGem, "Myslel", "Myslela") + " jsem, ze uz to neuvidim.";
			ErrandStep find = Errands.Deliver(c.GiverGem, thing[0], "Najdi " + thing[1] + " (" + venue.Label + ") a vrat " + thing[3] + " " + c.GiverName,
				"Tady je " + thing[2] + ".", "Dekuji!");
			find.venueId = venue.VenueId; //lies in the venue, not in the pack
			e.steps.Add(find);
			return true;
		}
	}

	/// <summary>8. The debt: collect from a debtor who pleads; they pay, the player forgives them (the debtor becomes a friend, less
	/// pay) or threatens (more pay, karma).</summary>
	internal sealed class DebtErrand : ErrandTemplate {
		public override string Key { get { return "debt"; } }
		public override string Name { get { return "Dluh"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			Item debtor = c.PickOther();
			if (debtor == null) {
				return false;
			}
			e.offer = ErrandContext.Who(debtor) + " mi dluzi uz dlouho. Zajdi tam a pripomen to. Slusne, jestli to pujde.";
			e.thanks = "Tak to je vyrizene.";
			e.steps.Add(Errands.Choose(debtor, "Vyrid dluh u " + ErrandContext.Who(debtor),
				new[] { BasicErrands.Sex(debtor, "Mel", "Mela") + " bys zaplatit, co dluzis.", "Odpustim ti to, tentokrat.", "Zaplat, nebo toho budes litovat!" },
				new[] { "Mas pravdu... tady to je.", "Opravdu? Nikdy ti to nezapomenu!", "Dobre, dobre! Tady, vezmi si to!" }));
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Ten dluh je vyrizeny.", "A jak?"));
			return true;
		}

		public override int PayPercent(Errand e) {
			switch (e.ChoiceAt(0)) {
				case 1: return 50; //forgiven: the giver got nothing
				case 2: return 120;
				default: return 100;
			}
		}

		public override void OnDone(Player player, Errand e) {
			Item debtor = Residents.ById(e.steps[0].residentId);
			if (debtor == null) {
				return;
			}
			if (e.ChoiceAt(0) == 1) {
				Residents.AddFriendship(player, Residents.IdOf(debtor), 2);
			} else if (e.ChoiceAt(0) == 2) {
				Residents.AddFriendship(player, Residents.IdOf(debtor), -2);
				player.Karma = (short) Math.Max(short.MinValue, player.Karma - 100);
			}
		}
	}

	/// <summary>9. A slow walk: walk an old resident to another venue; they follow the player and must be beside them at the end.</summary>
	internal sealed class SlowWalkErrand : ErrandTemplate {
		public override string Key { get { return "walk"; } }
		public override string Name { get { return "Doprovod"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			Item walker = BasicErrands.IsElder(c.GiverGem) ? c.GiverGem : c.PickOther(BasicErrands.IsElder);
			Item host = (walker == null) ? null : c.PickOther(gem => (gem != walker) && (Residents.WorkplaceOf(gem) != Residents.WorkplaceOf(walker)) && BasicErrands.IsWorker(gem));
			if (host == null) {
				return false;
			}
			CityVenue to = Residents.WorkplaceOf(host);
			string who = BasicErrands.Name(walker);
			e.offer = (walker == c.GiverGem) ? "Nohy uz mi neslouzi jako driv. Doprovodis me do: " + to.Label + "?" :
				who + " potrebuje doprovod do: " + to.Label + ". " + BasicErrands.Sex(walker, "Sam by tam nedosel. Pomuzes mu?", "Sama by tam nedosla. Pomuzes ji?");
			e.thanks = "Dekuji ti za tvou laskavost.";
			e.steps.Add(Errands.Talk(walker, "Vyzvedni " + ErrandContext.Who(walker), "Pujdeme?", "Ano, ano. Jen pomalu."));
			ErrandStep arrive = Errands.Talk(host, "Doprovod " + who + " do: " + to.Label + " (" + BasicErrands.Name(host) + ")", "Tady je " + who + ".", "Vitej! Posad se.");
			arrive.companionId = Residents.IdOf(walker);
			e.steps.Add(arrive);
			return true;
		}
	}

	/// <summary>19. A shift at work: the owner hands out mugs, bread or papers to carry to the people in the venue, then pays wages.</summary>
	internal sealed class ShiftErrand : ErrandTemplate {
		public override string Key { get { return "shift"; } }
		public override string Name { get { return "Smena"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			CityVenue work = Residents.WorkplaceOf(c.GiverGem);
			if ((work == null) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			//the venue's working people, in a random order (guests come only at their hours)
			List<Item> here = new List<Item>();
			foreach (Item gem in Residents.OfVenue(work.VenueId)) {
				if ((gem != c.GiverGem) && BasicErrands.IsWorker(gem)) {
					here.Insert(Globals.dice.Next(here.Count + 1), gem);
				}
			}
			if (here.Count == 0) {
				return false;
			}
			string def, what;
			switch (BasicErrands.VenueKey(c.GiverGem)) {
				case "tavern": case "messhall": def = "i_mug"; what = "korbel"; break;
				case "bakery": case "market": def = "i_bread_loaf"; what = "chleba"; break;
				default: def = "i_scroll_blank"; what = "psani"; break;
			}
			e.offer = "Mas chvili? Pomoz mi tady. Roznes, co ti dam, a zaplatim ti jako pomocnikovi.";
			e.thanks = "Dobra prace. Tady je tvoje mzda.";
			int rounds = Math.Min(3, here.Count);
			for (int i = 0; i < rounds; i++) {
				Item to = here[i];
				e.steps.Add(Errands.Deliver(to, def, "Dones " + what + " " + BasicErrands.Name(to), "Tady to je, od " + c.GiverName + ".", ErrandContext.Pick("Dekuji.", "Konecne!", "To je ono.")));
			}
			e.steps.Add(Errands.Talk(c.GiverGem, "Vrat se za " + c.GiverName, "Vse roznoseno.", "Vyborne."));
			return true;
		}
	}
}
