using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1260 - the errands of the harbour, the market, the bakery and the barracks (design 9.2).</summary>
	public static class TradeErrands {
		public static void Bootstrap() {
			ErrandTemplates.Register(new FishOrderErrand());
			ErrandTemplates.Register(new CargoListErrand());
			ErrandTemplates.Register(new CounterfeitErrand());
			ErrandTemplates.Register(new BreadRoundErrand());
			ErrandTemplates.Register(new PatrolErrand());
		}

		/// <summary>A choose step whose right answer's place is kept in its count (a choose step doesn't otherwise use it): the
		/// right one pays in full, a wrong one a quarter.</summary>
		internal static int ChoicePay(ErrandStep step) {
			return (step.chosen == step.count) ? 100 : 25;
		}

		/// <summary>The choices with the right one put at a random place; returns that place.</summary>
		internal static int Shuffle(List<string> choices, string right) {
			int at = Globals.dice.Next(choices.Count + 1);
			choices.Insert(at, right);
			return at;
		}
	}

	/// <summary>The harbour's fish order: three big fish from fishing.</summary>
	internal sealed class FishOrderErrand : ErrandTemplate {
		public override string Key { get { return "fishorder"; } }
		public override string Name { get { return "Rybi zakazka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Small; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			string role = Residents.RoleKeyOf(c.GiverGem);
			if (((role != "harbourmaster") && (role != "fisher")) || !(ThingDef.GetByDefname("i_fish_big_1") is ItemDef)) {
				return false;
			}
			const int count = 3;
			e.offer = "Lod na jih potrebuje zasoby a ryby nam dosly. Ulovis mi " + count + " velke ryby?";
			e.thanks = "Cerstve jako rano. Diky.";
			e.steps.Add(Errands.Fetch(c.GiverGem, "i_fish_big_1", count, "Prines " + c.GiverName + " " + count + " velke ryby", "Tady jsou ryby.", "Ty se budou hodit."));
			return true;
		}
	}

	/// <summary>Smuggling: a docker says which crate is too heavy for its label; the player names it to the harbourmaster.</summary>
	internal sealed class CargoListErrand : ErrandTemplate {
		public override string Key { get { return "cargolist"; } }
		public override string Name { get { return "Pasovani"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		private static readonly string[] lightGoods = { "peri", "seno", "vlna", "sukno", "slama", "papir" };

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (Residents.RoleKeyOf(c.GiverGem) != "harbourmaster") {
				return false;
			}
			Item docker = WorkErrands.SameVenue(c.GiverGem, "docker");
			if (docker == null) {
				return false;
			}
			List<string> labels = new List<string>(lightGoods);
			string heavy = labels[Globals.dice.Next(labels.Count)];
			labels.Remove(heavy);
			List<string> choices = new List<string>();
			for (int i = 0; i < 2; i++) {
				string other = labels[Globals.dice.Next(labels.Count)];
				labels.Remove(other);
				choices.Add("Bedna \"" + other + "\"");
			}
			int right = TradeErrands.Shuffle(choices, "Bedna \"" + heavy + "\"");
			e.offer = "Nekdo nam pres pristav pasuje zbrane. Zeptej se " + BasicErrands.Sex(docker, "pristavniho delnika ", "pristavni delnice ") +
				Residents.NameOf(docker) + ", co je na posledni lodi divneho.";
			e.thanks = "Diky. Na tu bednu se podivam.";
			e.steps.Add(Errands.Talk(docker, "Zeptej se: " + ErrandContext.Who(docker) + ", co je na lodi divneho", "Bylo na posledni lodi neco divneho?",
				"Bedna oznacena \"" + heavy + "\" byla tezka jako olovo. To nebylo " + heavy + "."));
			string[] replies = new string[3];
			for (int i = 0; i < 3; i++) {
				replies[i] = (i == right) ? "Tezka jako olovo, rikas? Hned ji otevru." : "Tahle? Tu jsem uz otevrel" + BasicErrands.Sex(c.GiverGem, "", "a") + ", nic v ni nebylo.";
			}
			ErrandStep name = Errands.Choose(c.GiverGem, "Rekni " + c.GiverName + ", ktera bedna je podezrela", choices.ToArray(), replies);
			name.count = right;
			e.steps.Add(name);
			return true;
		}

		public override int PayPercent(Errand e) {
			return TradeErrands.ChoicePay(e.steps[1]);
		}
	}

	/// <summary>Counterfeit coins: the crier says who paid with coins that shone too much; the player names them to the merchant.</summary>
	internal sealed class CounterfeitErrand : ErrandTemplate {
		public override string Key { get { return "counterfeit"; } }
		public override string Name { get { return "Falesne mince"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (Residents.RoleKeyOf(c.GiverGem) != "merchant") {
				return false;
			}
			Item crier = WorkErrands.SameVenue(c.GiverGem, "crier");
			CityVenue market = Residents.WorkplaceOf(c.GiverGem);
			List<Item> buyers = new List<Item>();
			for (int i = 0; i < 3; i++) {
				Item buyer = c.PickOther(gem => (Residents.WorkplaceOf(gem) != market) && !buyers.Contains(gem));
				if (buyer != null) {
					buyers.Add(buyer);
				}
			}
			if ((crier == null) || (buyers.Count < 3)) {
				return false;
			}
			Item cheat = buyers[Globals.dice.Next(3)];
			string cheatName = Residents.NameOf(cheat);
			List<string> others = new List<string>();
			foreach (Item buyer in buyers) {
				if (buyer != cheat) {
					others.Add(Residents.NameOf(buyer));
				}
			}
			List<string> choices = new List<string>(others);
			int right = TradeErrands.Shuffle(choices, cheatName);
			e.offer = "V pokladne mam falesne mince! Vyvolavac videl, kdo dnes platil. Zeptej se " + BasicErrands.Sex(crier, "ho", "ji") + ".";
			e.thanks = "Diky. Ted vim, komu uz neprodam.";
			e.steps.Add(Errands.Talk(crier, "Zeptej se: " + ErrandContext.Who(crier) + ", kdo dnes platil u stanku", "Kdo dnes platil u stanku?",
				"Platili " + string.Join(", ", choices) + ". " + cheatName + " mel" + BasicErrands.Sex(cheat, "", "a") + " mince, co se az moc leskly."));
			string[] replies = new string[3];
			for (int i = 0; i < 3; i++) {
				replies[i] = (i == right) ? "Ano! Ty nove leskle mince. To je ono." : "To ne, ti platili starymi okoukanymi penezi.";
			}
			ErrandStep name = Errands.Choose(c.GiverGem, "Rekni " + c.GiverName + ", kdo platil falesnymi mincemi", choices.ToArray(), replies);
			name.count = right;
			e.steps.Add(name);
			return true;
		}

		public override int PayPercent(Errand e) {
			return TradeErrands.ChoicePay(e.steps[1]);
		}
	}

	/// <summary>The dawn round: bread to three residents before it cools, a short errand.</summary>
	internal sealed class BreadRoundErrand : ErrandTemplate {
		public override string Key { get { return "breadround"; } }
		public override string Name { get { return "Ranni roznaska"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override int Minutes {
			get { return 20; } //before the bread cools
		}

		public override bool TryBuild(ErrandContext c, Errand e) {
			if ((Residents.RoleKeyOf(c.GiverGem) != "baker") || !(ThingDef.GetByDefname("i_bread_loaf") is ItemDef)) {
				return false;
			}
			List<Item> homes = new List<Item>();
			for (int i = 0; i < 3; i++) {
				Item to = c.PickOther(gem => !homes.Contains(gem) && (Residents.WorkplaceOf(gem) != Residents.WorkplaceOf(c.GiverGem)));
				if (to != null) {
					homes.Add(to);
				}
			}
			if (homes.Count < 3) {
				return false;
			}
			e.offer = "Chleba je z peci! Roznes ho trem zakaznikum, nez vychladne. Mas na to dvacet minut.";
			e.thanks = "Vsichni dostali teply chleba? Vyborne.";
			foreach (Item to in homes) {
				e.steps.Add(Errands.Deliver(to, "i_bread_loaf", "Dones chleba: " + ErrandContext.Who(to), "Cerstvy chleba od pekare.", "Jeste je teply! Diky."));
			}
			return true;
		}
	}

	/// <summary>A recruit's first patrol: the recruit walks with the player to one of the city's people and back to the captain.</summary>
	internal sealed class PatrolErrand : ErrandTemplate {
		public override string Key { get { return "patrol"; } }
		public override string Name { get { return "Prvni hlidka"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (Residents.RoleKeyOf(c.GiverGem) != "captain") {
				return false;
			}
			Item recruit = WorkErrands.SameVenue(c.GiverGem, "recruit");
			CityVenue barracks = Residents.WorkplaceOf(c.GiverGem);
			Item visit = c.PickOther(gem => (Residents.WorkplaceOf(gem) != barracks) && BasicErrands.IsWorker(gem));
			if ((recruit == null) || (visit == null)) {
				return false;
			}
			string name = Residents.NameOf(recruit), him = BasicErrands.Sex(recruit, "ho", "ji");
			e.offer = name + " jeste nebyl" + BasicErrands.Sex(recruit, "", "a") + " na obchuzce. Vezmi " + him + " s sebou do: " +
				Residents.WorkplaceOf(visit).Label + ", a pak zpatky.";
			e.thanks = "Dobra prace. Z rekruta jednou bude strazny.";
			e.steps.Add(Errands.Talk(recruit, "Vyzvedni rekruta: " + name, "Jdeme na obchuzku.", "Rozkaz! Jdu za tebou."));
			ErrandStep there = Errands.Talk(visit, "Dojdi s rekrutem za: " + ErrandContext.Who(visit), "Obchuzka. Je tu vse v poradku?", "Vse v poradku, diky za optani.");
			there.companionId = Residents.IdOf(recruit);
			e.steps.Add(there);
			ErrandStep back = Errands.Talk(c.GiverGem, "Vrat se s rekrutem ke kapitanovi", "Obchuzka skoncila, nic se nedelo.", "Dobre.");
			back.companionId = Residents.IdOf(recruit);
			e.steps.Add(back);
			return true;
		}
	}
}
