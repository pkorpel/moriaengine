using System;
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#918 - server-side helpers for Tests/Protocol/LootingCrimeTests.cs.</summary>
	public static class LootingCrimeTests {
		private static Character owner;
		private static Item loot;

		/// <summary>The one whose corpse gets looted.</summary>
		[SteamFunction]
		public static void TestLootOwner(Character self) {
			owner = self;
			self.Account.PLevel = 1;
			Console.WriteLine("LOOT_OWNER: uid=" + (uint) self.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestLootLooter(Character self) {
			self.P(owner.P());
			self.Account.PLevel = 1;
			Console.WriteLine("LOOT_LOOTER: pack=" + (uint) self.Backpack.FlaggedUid + " end");
		}

		private static Realm RealmOf(char letter) {
			switch (letter) {
				case 'G': return Realm.Gondor;
				case 'M': return Realm.Mordor;
				case 'g': return Realm.GondorOutcast;
				default: return Realm.None;
			}
		}

		/// <summary>Two letters: my realm, the owner's.</summary>
		[SteamFunction]
		public static void TestLootRealms(Character self, ScriptArgs sa) {
			string realms = sa.Args.Trim().Trim('"');
			self.Realm = RealmOf(realms[0]);
			owner.Realm = RealmOf(realms[1]);
			Console.WriteLine("LOOT_REALMS: done");
		}

		/// <summary>Two digits: Gondor's switch, Mordor's.</summary>
		[SteamFunction]
		public static void TestLootSwitch(Character self, ScriptArgs sa) {
			string switches = sa.Args.Trim().Trim('"');
			NotorietySettings.instance.gondorLootingIsACrime = switches[0] == '1';
			NotorietySettings.instance.mordorLootingIsACrime = switches[1] == '1';
			Console.WriteLine("LOOT_SWITCH: done");
		}

		private static List<Corpse> Corpses(Character near) {
			List<Corpse> found = new List<Corpse>();
			foreach (Thing thing in near.GetMap().GetThingsInRectangle(new ImmutableRectangle(near.X - 2, near.Y - 2, near.X + 2, near.Y + 2))) {
				if (thing is Corpse corpse) {
					found.Add(corpse);
				}
			}
			return found;
		}

		/// <summary>Everyone alive, no corpses, nobody criminal.</summary>
		[SteamFunction]
		public static void TestLootClean(Character self) {
			if (owner.Flag_Dead) {
				owner.Resurrect();
			}
			foreach (Corpse corpse in Corpses(owner)) {
				corpse.Delete();
			}
			if ((loot != null) && !loot.IsDeleted) {
				loot.Delete();
			}
			Notoriety.ClearCriminal(self);
			Notoriety.ClearCriminal(owner);
			self.RemoveTag(TagKey.Acquire("_nextLoot_")); //#1023 - the rate and the hold start afresh
			self.DeletePlugin(LootHoldPlugin.key);
			Console.WriteLine("LOOT_CLEAN: done");
		}

		/// <summary>A corpse beside me with something in it. C = its owner is a criminal, F = the thing
		/// can't be picked up, N = a monster's corpse, W = a body this moment dead (#1023: the others died 10 s ago,
		/// past Erebor's first seconds when a body can't be looted).</summary>
		[SteamFunction]
		public static void TestLootStage(Character self, ScriptArgs sa) {
			string kind = sa.Args.Trim().Trim('"');
			TestLootClean(self);
			if (kind == "N") {
				((Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(owner.P())).CauseDeath(null);
			} else {
				if (kind == "C") {
					Notoriety.FlagCriminal(owner);
				}
				owner.CauseDeath(null);
			}
			Corpse corpse = Corpses(owner)[0];
			if (kind != "W") {
				corpse.SetTag(TagKey.Acquire("_diedAt_"), Globals.TimeInSeconds - 10);
			}
			loot = (Item) ThingDef.GetByDefname("i_dagger").Create(corpse);
			if (kind == "F") {
				loot.Flag_NonMovable = true;
			}
			Console.WriteLine("LOOT_STAGE: corpse=" + (uint) corpse.FlaggedUid + " item=" + (uint) loot.FlaggedUid + " ownerOfCorpse=" + (corpse.Owner != null) + " end");
		}

		/// <summary>#1023 - Erebor's limits on a player's body, through CorpseLooting itself.</summary>
		[SteamFunction]
		public static void TestLootLimits(Character self) {
			Player looter = (Player) self;
			TestLootStage(self, new ScriptArgs("W"));
			Corpse corpse = Corpses(owner)[0];
			bool wait = CorpseLooting.Refusal(looter, corpse) == "Z tohoto tela jeste nemuzes nic brat.";
			corpse.SetTag(TagKey.Acquire("_diedAt_"), Globals.TimeInSeconds - 10);
			bool first = CorpseLooting.Refusal(looter, corpse) == null;
			bool held = !CorpseLooting.CheckCanMove(looter);
			bool tooFast = CorpseLooting.Refusal(looter, corpse) == "Ne tak rychle!";
			looter.SetTag(TagKey.Acquire("_levelingLastDeathTime_"), Globals.TimeAsSpan); //just died himself
			looter.RemoveTag(TagKey.Acquire("_nextLoot_"));
			SkillRank rank = looter.SkillRank;
			looter.SkillRank = SkillRank.Master;
			bool freshDead = CorpseLooting.Refusal(looter, corpse) == "Jeste nemuzes obirat cizi mrtvoly.";
			looter.SkillRank = SkillRank.Grandmaster;
			looter.RemoveTag(TagKey.Acquire("_nextLoot_"));
			bool grandmaster = CorpseLooting.Refusal(looter, corpse) == null;
			looter.SkillRank = rank;
			looter.RemoveTag(TagKey.Acquire("_levelingLastDeathTime_"));
			Corpse rabbit = null;
			NPC hare = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(owner.P());
			hare.CauseDeath(null);
			foreach (Thing t in owner.GetMap().GetThingsInRectangle(new ImmutableRectangle(owner.X - 1, owner.Y - 1, owner.X + 1, owner.Y + 1))) {
				if ((t is Corpse c) && (c.Owner == null)) {
					rabbit = c;
				}
			}
			bool monsterFree = (rabbit != null) && (CorpseLooting.Refusal(looter, rabbit) == null) && (CorpseLooting.Refusal(looter, rabbit) == null);
			TestLootClean(self);
			Console.WriteLine("LOOT_LIMITS: wait=" + wait + " first=" + first + " held=" + held + " tooFast=" + tooFast + " freshDead=" + freshDead +
				" grandmaster=" + grandmaster + " monsterFree=" + monsterFree + " end");
		}

		[SteamFunction]
		public static void TestLootReport(Character self) {
			Console.WriteLine("LOOT_REPORT: took=" + (loot.TopObj() == self) + " criminal=" + Notoriety.IsCriminal(self) + " end");
		}

		[SteamFunction]
		public static void TestLootMyLootSetting(Character self) {
			Console.WriteLine("LOOT_SETTING: me=" + (uint) self.FlaggedUid + " canLootMe=" + Party.CanLootMe(self) + " end");
		}
	}
}
