using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1247 - the t_collector bag checked on the server; Tests/Protocol/CollectorTests.cs drives them and aims a real bag
	/// through a client.</summary>
	public static class CollectorTests {
		private const string BagDef = "i_q006_vacek_stehno"; //a converted bag: "Vacek na stehna grizliho"
		private static readonly TagKey bagTK = TagKey.Acquire("_collectortest_bag_");
		private static readonly TagKey corpseTK = TagKey.Acquire("_collectortest_corpse_");

		private static void Report(string name, bool ok) {
			Console.WriteLine("COLLECTOR_CHECK: " + name + "=" + ok);
		}

		/// <summary>A fresh body at my feet (nothing between us to block the sight), by killing a man there: its name is
		/// whatever its def calls it.</summary>
		private static Corpse Body(Character self) {
			Map map = self.GetMap();
			HashSet<Thing> before = new HashSet<Thing>();
			foreach (Thing thing in map.GetThingsInRange(self.X, self.Y, 0)) {
				before.Add(thing);
			}
			NPC victim = (NPC) ThingDef.GetByDefname("c_resident_man").Create(self.P());
			victim.CauseDeath(victim);
			foreach (Thing thing in map.GetThingsInRange(self.X, self.Y, 0)) {
				if ((thing is Corpse corpse) && !corpse.IsDeleted && !before.Contains(corpse)) {
					return corpse;
				}
			}
			return null;
		}

		/// <summary>The converted bag reads its settings from its def; it lies at my feet, aimed at a body beside me.</summary>
		[SteamFunction]
		public static void TestCollectorSetup(Character self) {
			try {
				Collector bag = (Collector) ThingDef.GetByDefname(BagDef).Create(self.P());
				Report("theConverterMadeACollector", (bag.Def is CollectorDef) && !(bag.Def is EquippableDef));
				Report("itsSettingsComeFromTheDef", (bag.Text(Collector.TargetIdTK) == "Body of Grizzly") && (bag.MaxCount == 10) &&
					(bag.Number(Collector.ValueToCheckTK) == 75) && (bag.Number(Collector.AbsoluteSuccessTK) == 100) && (bag.Number(Collector.TargetRemoveTK) == 1));
				Report("aClickTellsTheCount", bag.Describe() == "Ve vacku je 0 stehen grizliho");
				Corpse body = Body(self);
				//a sure take on this body: an instance tag overrides the def's, as an errand's own bag would
				bag.SetTag(Collector.TargetIdTK, body.Name);
				bag.SetTag(Collector.ValueToCheckTK, 100);
				bag.SetTag(Collector.AbsoluteSuccessTK, 0);
				self.SetTag(bagTK, bag);
				self.SetTag(corpseTK, body);
				Console.WriteLine("COLLECTOR_TARGETS: bag=" + bag.Uid + " corpse=" + body.Uid + " desc=" + bag.Text(Collector.TargetDescTK));
			} finally {
				Console.WriteLine("COLLECTOR_SETUP_DONE");
			}
		}

		/// <summary>After the client aimed the bag at the body.</summary>
		[SteamFunction]
		public static void TestCollectorAfterTarget(Character self) {
			Collector bag = self.GetTag(bagTK) as Collector;
			Corpse body = self.GetTag(corpseTK) as Corpse;
			Report("aimingAtTheBodyCounts", (bag != null) && (bag.Collected == 1));
			Report("theBodyIsUsedUp", (body == null) || body.IsDeleted);
			Console.WriteLine("COLLECTOR_AFTER_DONE");
		}

		[SteamFunction]
		public static void TestCollectorRules(Character self) {
			Player player = (Player) self;
			List<Thing> made = new List<Thing>();
			try {
				Rules(player, made);
			} finally {
				foreach (Thing thing in made) {
					if (!thing.IsDeleted) {
						thing.Delete();
					}
				}
				Console.WriteLine("COLLECTOR_RULES_DONE");
			}
		}

		private static Collector NewBag(Player player, List<Thing> made, int value, int absolute, int remove) {
			Collector bag = (Collector) ThingDef.GetByDefname(BagDef).Create(player.Backpack);
			bag.SetTag(Collector.ValueToCheckTK, value);
			bag.SetTag(Collector.AbsoluteSuccessTK, absolute);
			bag.SetTag(Collector.TargetRemoveTK, remove);
			made.Add(bag);
			return bag;
		}

		private static Corpse Aim(Player player, List<Thing> made, Collector bag) {
			Corpse body = Body(player);
			made.Add(body);
			bag.SetTag(Collector.TargetIdTK, body.Name);
			bag.Collect(player, body);
			return body;
		}

		private static void Rules(Player player, List<Thing> made) {
			//a failed try spends the body too (target_remove 1), and counts nothing
			Collector bag = NewBag(player, made, -1, 100, 1);
			Corpse body = Aim(player, made, bag);
			Report("aFailCountsNothingAndSpendsTheBody", (bag.Collected == 0) && body.IsDeleted);

			//target_remove 0: the body stays, marked as taken, and a second try is refused
			bag = NewBag(player, made, 100, 0, 0);
			body = Aim(player, made, bag);
			bag.Collect(player, body);
			Report("aKeptBodyIsMarkedTakenOnce", (bag.Collected == 1) && !body.IsDeleted && Collectors.IsTaken(body));

			//repeatedcollecting: no mark, so it can be taken again
			bag = NewBag(player, made, 100, 0, 0);
			bag.SetTag(Collector.RepeatedTK, 1);
			body = Aim(player, made, bag);
			bag.Collect(player, body);
			Report("aRepeatedOneCanBeTakenAgain", (bag.Collected == 2) && !Collectors.IsTaken(body));

			//a wrong name is refused
			bag = NewBag(player, made, 100, 0, 1);
			Corpse other = Body(player);
			made.Add(other);
			bag.SetTag(Collector.TargetIdTK, "Body of Nikdo");
			bag.Collect(player, other);
			Report("theWrongBodyIsRefused", (bag.Collected == 0) && !other.IsDeleted);

			//a player's body spoils the bag
			bag.Collected = 4;
			Corpse human = Body(player);
			made.Add(human);
			human.Owner = player;
			bag.Collect(player, human);
			Report("aPlayersBodySpoilsTheBag", bag.Collected == 0);

			//full: no more, and the double click says so before any cursor
			bag = NewBag(player, made, 100, 0, 1);
			bag.Collected = bag.MaxCount;
			body = Aim(player, made, bag);
			Report("aFullBagTakesNoMore", (bag.Collected == bag.MaxCount) && bag.IsFull && !body.IsDeleted);

			//the colour rule, on items
			bag = NewBag(player, made, 100, 0, 1);
			bag.SetTag(Collector.ColorTK, 0x481);
			Item egg = (Item) ThingDef.GetByDefname("i_bag").Create(player.P());
			made.Add(egg);
			egg.Color = 0x482;
			bag.SetTag(Collector.TargetIdTK, egg.Name);
			bag.Collect(player, egg);
			bool wrongColourRefused = bag.Collected == 0;
			egg.Color = 0x481;
			bag.Collect(player, egg);
			Report("onlyTheRightColourCounts", wrongColourRefused && (bag.Collected == 1));

			//a collection source: one pick, then a wait, then nothing left
			bag = NewBag(player, made, 100, 0, 1);
			Item bush = (Item) ThingDef.GetByDefname("i_bag").Create(player.P());
			made.Add(bush);
			bush.SetTag(CollectionSource.CollectorIdTK, BagDef);
			bush.SetTag(CollectionSource.PickDelayTK, 600);
			bush.SetTag(CollectionSource.ItemsToPickTK, 1);
			bag.Collect(player, bush);
			int afterFirst = bag.Collected;
			bag.Collect(player, bush);
			int afterWait = bag.Collected;
			bush.SetTag(CollectionSource.NextPickTK, 0);
			bag.Collect(player, bush);
			Report("aSourceGivesThenWaitsThenEmpties", (afterFirst == 1) && (afterWait == 1) && (bag.Collected == 1) && !bush.IsDeleted);

			//a player whose name matches is never removed, only marked
			bag = NewBag(player, made, 100, 0, 1);
			bag.SetTag(Collector.TargetIdTK, player.Name);
			bag.Collect(player, player);
			Report("aPlayerIsNeverRemoved", !player.IsDeleted && (bag.Collected == 1) && Collectors.IsTaken(player));
			player.RemoveTag(Collector.UncollectableTK);

			//repeated collecting spares a failed item no mark (Erebor's item branch)
			bag = NewBag(player, made, -1, 100, 0);
			bag.SetTag(Collector.RepeatedTK, 1);
			Item stone = (Item) ThingDef.GetByDefname("i_bag").Create(player.P());
			made.Add(stone);
			bag.SetTag(Collector.TargetIdTK, stone.Name);
			bag.Collect(player, stone);
			Report("aFailedItemIsMarkedEvenWhenRepeated", (bag.Collected == 0) && Collectors.IsTaken(stone));

			//found in the pack by def, as quests ask
			Report("questsFindTheBag", Collectors.FindIn(player, BagDef) != null);
		}
	}
}
