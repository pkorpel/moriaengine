using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1302 - Mordor's stories (design 9.4) as errand templates: the pits of Nurn, the Eye's spies and the Mouth's
	/// dispatches (the last two are the war camp's orders). Texts Czech without diacritics.</summary>
	public static class MordorStories {
		public const string Nurn = "Nurn", Tower = "Minas_Morgul";
		public const string SlaveDef = "c_errand_slave", DispatchDef = "i_errand_dispatch_box";
		internal static readonly TagKey FreedTK = TagKey.Acquire("_nurn_freed_");

		/// <summary>Tests: rolls (0-99) in this order before any random one.</summary>
		public static readonly Queue<int> RollOverride = new Queue<int>();

		public static void Bootstrap() {
			ErrandTemplates.Register(new NurnQuotaErrand());
			ErrandTemplates.Register(new EyeSpyErrand());
			ErrandTemplates.Register(new DispatchErrand());
		}

		internal static int Roll(int below) {
			return (RollOverride.Count > 0) ? RollOverride.Dequeue() : Globals.dice.Next(below);
		}

		public static int Freed(Player player) {
			return (player.GetTag(FreedTK) is int freed) ? freed : 0;
		}

		internal static string FirstLetter(Item gem) {
			string name = Residents.NameOf(gem);
			return string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
		}

		internal static string WorkLabel(Item gem) {
			CityVenue work = Residents.WorkplaceOf(gem);
			return (work == null) ? "" : work.Label;
		}
	}

	/// <summary>Attaches the stories' things' double clicks to their defs.</summary>
	public static class MordorStoriesInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(MordorStories.SlaveDef, SingletonScript<E_ErrandThingNpc>.Instance);
			QuestNpc.Attach(MordorStories.DispatchDef, SingletonScript<E_ErrandThingItem>.Instance);
		}
	}

	/// <summary>The pits of Nurn (an overseer in Nurn): catch the three slaves who ran, then hand them all back or quietly let one
	/// go. Letting one go pays half and raises karma; one time in four the overseer finds out (standing in Nurn, more than the
	/// errand gives); every fifth slave freed leaves a secret gift (the medium errand's own pouch chance stays).</summary>
	internal sealed class NurnQuotaErrand : ErrandTemplate {
		internal const int Slaves = 3, FreeChoice = 1, FoundOutChance = 25, FoundOutLoss = 60, KarmaGain = 50, GiftEvery = 5;

		public override string Key { get { return "nurnquota"; } }
		public override string Name { get { return "Jamy Nurnu"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!CityFlavour.Is(c.City, MordorStories.Nurn) || (BasicErrands.VenueKey(c.GiverGem) != "overseer")) {
				return false;
			}
			e.offer = "Z jam utekli tri otroci. Pochytej je, nez se dozvi Oko, ze dozorce Nurnu nehlida.";
			e.thanks = "Kvota je splnena.";
			e.steps.Add(Errands.Things(MordorStories.SlaveDef, Slaves, ErrandThingAction.Catch, ErrandThingPlace.Streets, null, true,
				"Pochytej " + Slaves + " uprchle otroky v Nurnu", "Mas je vsechny. Ted je vrat dozorci."));
			e.steps.Add(Errands.Choose(c.GiverGem, "Predej otroky " + c.GiverName,
				new[] { "Tady jsou vsichni.", "Jeden mi cestou utekl." },
				new[] { "Dobre. Zpatky do jam s nimi.", "Utekl? Hm. Tak mene zaplaceno." }));
			return true;
		}

		/// <summary>One let go: half the pay.</summary>
		public override int PayPercent(Errand e) {
			return (e.ChoiceAt(1) == FreeChoice) ? 50 : 100;
		}

		/// <summary>One let go: karma; maybe found out; every third freed, the freed slaves' gift.</summary>
		public override void OnDone(Player player, Errand e) {
			if ((e.ChoiceAt(1) != FreeChoice) || player.IsGM) {
				return;
			}
			player.Karma = (short) Math.Min(short.MaxValue, player.Karma + KarmaGain);
			int freed = MordorStories.Freed(player) + 1;
			player.SetTag(MordorStories.FreedTK, freed);
			player.SysMessage("Otrok zmizel ve tme. Nikdo se nedozvi, ze jsi ho pustil.");
			if (MordorStories.Roll(100) < FoundOutChance) {
				Reputation.Change(player, MordorStories.Nurn, -FoundOutLoss);
				player.SysMessage("Dozorce ti neveri. Nekdo te videl, jak otroka pustis.", 0x21);
			}
			if ((freed % GiftEvery) == 0) {
				ItemDef pouch = ThingDef.GetByDefname("i_mi_pouch") as ItemDef;
				if (pouch != null) {
					pouch.Create(player.Backpack);
				}
				player.SysMessage("V batohu nachazis maly balicek a vzkaz: 'Od tech, ktere jsi pustil. Nezapomeneme.'", ClassAbilities.ClassMessageHue);
			}
		}
	}

	/// <summary>The Eye's spies (an overseer in the war camp, an order): two witnesses give clues to a Gondor spy among the camp's
	/// residents (where he works, the first letter of his name), then the hero accuses one of three suspects. The right one pays; a
	/// wrong one pays nothing and costs standing.</summary>
	internal sealed class EyeSpyErrand : ErrandTemplate {
		internal const int Suspects = 3;

		public override string Key { get { return "eyespy"; } }
		public override string Name { get { return "Speh v taboru"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!WarCamp.IsCamp(c.City) || (BasicErrands.VenueKey(c.GiverGem) != "overseer")) {
				return false;
			}
			Item spy = c.PickOther(gem => MordorStories.WorkLabel(gem).Length > 0);
			Item first = (spy == null) ? null : c.PickOther(gem => gem != spy);
			Item second = (first == null) ? null : c.PickOther(gem => (gem != spy) && (gem != first));
			if (second == null) {
				return false;
			}
			string place = MordorStories.WorkLabel(spy), letter = MordorStories.FirstLetter(spy);
			//the decoys: not the witnesses, and not matching both clues, so the clues single out the spy
			List<Item> decoys = new List<Item>();
			for (int i = 1; i < Suspects; i++) {
				Item decoy = c.PickOther(gem => (gem != spy) && (gem != first) && (gem != second) && !decoys.Contains(gem)
					&& !((MordorStories.WorkLabel(gem) == place) && (MordorStories.FirstLetter(gem) == letter))
					&& (Residents.NameOf(gem) != Residents.NameOf(spy)) && !decoys.Exists(d => Residents.NameOf(d) == Residents.NameOf(gem)));
				if (decoy == null) {
					return false;
				}
				decoys.Add(decoy);
			}
			List<Item> suspects = new List<Item>(decoys);
			int right = Globals.dice.Next(Suspects);
			suspects.Insert(right, spy);
			string[] names = new string[Suspects], replies = new string[Suspects];
			for (int i = 0; i < Suspects; i++) {
				names[i] = "Je to " + ErrandContext.Who(suspects[i]) + "."; //with the workplace: the clues decide it in the dialog
				replies[i] = (i == right) ? "Ano! Odvedte ho do vezeni. Oko vidi vse." : "Ten? Hm. Uvidime, co rekne pod bicem...";
			}
			e.offer = "V taboru je speh Gondoru. Vyptej se " + ErrandContext.Who(first) + " a " + ErrandContext.Who(second) + ", pak mi rekni, kdo to je.";
			e.thanks = "Oko je spokojeno.";
			e.steps.Add(Errands.Talk(first, "Vyptej se " + ErrandContext.Who(first), "Videl jsi v taboru nekoho podezreleho?",
				"Jo, nekdo se tu potloukal kolem: " + place + ". Nevim, kdo to byl."));
			e.steps.Add(Errands.Talk(second, "Vyptej se " + ErrandContext.Who(second), "Vis neco o spehovi?",
				"Slysel jsem, jak mu nekdo rika jmenem. Zacinalo na " + letter + "."));
			ErrandStep accuse = Errands.Choose(c.GiverGem, "Rekni " + c.GiverName + ", kdo je speh", names, replies);
			accuse.count = right; //the right answer, kept with the step
			e.steps.Add(accuse);
			return true;
		}

		private static bool Right(Errand e) {
			ErrandStep accuse = e.steps[e.steps.Count - 1];
			return accuse.chosen == accuse.count;
		}

		public override int PayPercent(Errand e) {
			return Right(e) ? 100 : 0;
		}

		/// <summary>A wrong accusation: no pay, no standing (Finish gives none for a share of 0) and the price of a failed order, in
		/// one change.</summary>
		public override void OnDone(Player player, Errand e) {
			if (!Right(e)) {
				Reputation.Change(player, e.city, -WarCampSettings.instance.orderFailLoss);
				player.SysMessage("Obvinil jsi nevinneho. Skutecny speh je dal na svobode.", 0x21);
			}
		}

		public override int GiverFriendship(Errand e, int baseFriend) {
			return Right(e) ? baseFriend : -1;
		}
	}

	/// <summary>The Mouth's dispatches (a camp worker, an order): carry the sealed dispatch to the box at the Nazgul's tower of Minas
	/// Morgul before the deadline.</summary>
	internal sealed class DispatchErrand : ErrandTemplate {
		public override string Key { get { return "dispatches"; } }
		public override string Name { get { return "Rozkazy Usta"; } }
		public override ErrandTier Tier { get { return ErrandTier.Medium; } }
		public override int Minutes { get { return 60; } }

		public override bool TryBuild(ErrandContext c, Errand e) {
			if (!WarCamp.IsCamp(c.City) || !BasicErrands.IsWorker(c.GiverGem)) {
				return false;
			}
			ErrandStep box = Errands.Things(MordorStories.DispatchDef, 1, ErrandThingAction.Use, ErrandThingPlace.Streets, null, false,
				"Doruc rozkazy Usta do schranky u veze Minas Morgul", "Rozkazy jsou ve schrance. Nazgul je dostane.", MordorStories.Tower);
			if (!Errands.CanPlace(e, box)) {
				return false;
			}
			e.offer = "Usta Sauronova posilaji rozkazy do Minas Morgul. Zapecetene. Doruc je vcas, nebo si te najdou Prizraky.";
			e.thanks = "Rozkazy dorazily vcas.";
			e.steps.Add(box);
			return true;
		}

		/// <summary>The giver is far away in the camp: the thanks come as a message.</summary>
		public override void OnDone(Player player, Errand errand) {
			player.SysMessage(errand.thanks, ClassAbilities.ClassMessageHue);
		}
	}
}
