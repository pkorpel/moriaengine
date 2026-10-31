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
			Console.WriteLine("LOOT_CLEAN: done");
		}

		/// <summary>A corpse beside me with something in it. C = its owner is a criminal, F = the thing
		/// can't be picked up, N = a monster's corpse.</summary>
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
			loot = (Item) ThingDef.GetByDefname("i_dagger").Create(corpse);
			if (kind == "F") {
				loot.Flag_NonMovable = true;
			}
			Console.WriteLine("LOOT_STAGE: corpse=" + (uint) corpse.FlaggedUid + " item=" + (uint) loot.FlaggedUid + " ownerOfCorpse=" + (corpse.Owner != null) + " end");
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
