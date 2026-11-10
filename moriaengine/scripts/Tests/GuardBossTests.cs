using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1234 - the guard boss system on the server, driven by Tests/Protocol/GuardBossTests.cs: three spawns A, B and C
	/// beside the GM, B guarding A and C guarding B.</summary>
	public static class GuardBossTests {
		private static readonly TagKey gemsTK = TagKey.Acquire("_guardBossTest_gems_");
		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");
		private static readonly TagKey spawnInfoTK = TagKey.Acquire("spawnInfo");
		private static readonly TriggerKey spawnNowTK = TriggerKey.Acquire("spawnNow");

		private static Item[] Gems(Character self) {
			return (Item[]) self.GetTag(gemsTK);
		}

		private static NPC MonsterOf(Item gem) {
			if (gem.IsDeleted) {
				return null;
			}
			foreach (AbstractCharacter ch in gem.GetMap().GetCharsInRange(gem.X, gem.Y, 6)) {
				if ((ch is NPC npc) && (npc.GetTag(spawnGemTK) == gem) && !npc.IsDeleted) {
					return npc;
				}
			}
			return null;
		}

		/// <summary>Three gems in a row east of the GM, each with its one monster spawned and held still.</summary>
		[SteamFunction]
		public static void TestGuardBossSetup(Character self) {
			Item[] gems = new Item[3];
			for (int i = 0; i < 3; i++) {
				Point4D at = new Point4D(self.X + 1 + (i * 4), self.Y, self.Z, self.M);
				ScriptHolder.GetFunction("f_spawninit").Run(at, new SpawnInfo(1, (IThingFactory) ThingDef.GetByDefname("c_bear_grizzly"), 30, 60, 0));
				foreach (AbstractItem item in self.GetMap().GetItemsInRange(at.X, at.Y, 0)) {
					if (item.GetTag(spawnInfoTK) != null) {
						gems[i] = (Item) item;
					}
				}
				gems[i].DeleteTimer(TimerKey.Acquire("SpawnTimer"));
				gems[i].TryTrigger(spawnNowTK, new ScriptArgs());
				NPCAiTimer.Freeze(MonsterOf(gems[i]), 600);
			}
			self.SetTag(gemsTK, gems);
			Console.WriteLine("GUARD_BOSS_SETUP: a=" + (uint) gems[0].FlaggedUid + " b=" + (uint) gems[1].FlaggedUid + " c=" + (uint) gems[2].FlaggedUid + " end");
		}

		/// <summary>The links' rules, through GuardBoss.Link itself: B guards A is set up by the dialog, these add C guards B and
		/// try every refusal.</summary>
		[SteamFunction]
		public static void TestGuardBossLinks(Character self) {
			Item[] g = Gems(self);
			string cGuardsB = GuardBoss.Link(g[1], g[2]);
			string self_ = GuardBoss.Link(g[0], g[0]);
			string duplicate = GuardBoss.Link(g[0], g[1]);
			string twoLoop = GuardBoss.Link(g[1], g[0]);
			string threeLoop = GuardBoss.Link(g[2], g[0]);
			string notGem = GuardBoss.Link(g[0], ((Player) self).Backpack);
			Console.WriteLine("GUARD_BOSS_LINKS: cGuardsB=" + (cGuardsB == null) + " self=" + Has(self_, "itself") + " duplicate=" + Has(duplicate, "already") +
				" twoLoop=" + Has(twoLoop, "loop") + " threeLoop=" + Has(threeLoop, "through") + " notGem=" + Has(notGem, "not a spawn") +
				" guardsOfA=" + GuardBoss.GuardsOf(g[0]).Count + " guardsOfB=" + GuardBoss.GuardsOf(g[1]).Count + " guardsOfC=" + GuardBoss.GuardsOf(g[2]).Count + " end");
		}

		private static bool Has(string refusal, string text) {
			return (refusal != null) && (refusal.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		/// <summary>Each spawn's monster hit twice, plain and irresistible (Erebor's 1000 resists let the latter through): what it lost.</summary>
		[SteamFunction]
		public static void TestGuardBossHit(Character self) {
			Item[] g = Gems(self);
			string report = "";
			string[] names = { "a", "b", "c" };
			for (int i = 0; i < 3; i++) {
				NPC npc = MonsterOf(g[i]);
				if (npc == null) {
					report += " " + names[i] + "=dead";
					continue;
				}
				npc.Hits = npc.MaxHits;
				DamageManager.CauseDamage((Character) self, npc, DamageType.Physical, 5);
				DamageManager.CauseDamage((Character) self, npc, DamageType.Irresistable, 5);
				report += " " + names[i] + "=" + (npc.MaxHits - npc.Hits);
				npc.Hits = npc.MaxHits;
			}
			Console.WriteLine("GUARD_BOSS_HIT:" + report + " end");
		}

		/// <summary>Kills the monster of spawn a, b or c (on a timer: a death can't run inside a command).</summary>
		[SteamFunction]
		public static void TestGuardBossKill(Character self, ScriptArgs sa) {
			int i = "abc".IndexOf(sa.Args.Trim().Trim('"'), StringComparison.Ordinal);
			NPC npc = MonsterOf(Gems(self)[i]);
			if (npc == null) {
				Console.WriteLine("GUARD_BOSS_KILL: none");
				return;
			}
			self.AddTimer(DeferredSpawnedNpcKillTimer.TimerKey, new DeferredSpawnedNpcKillTimer(npc)).DueInSeconds = 0;
			Console.WriteLine("GUARD_BOSS_KILL: done");
		}

		/// <summary>Deletes spawn c's gem: a deleted guard guards nothing.</summary>
		[SteamFunction]
		public static void TestGuardBossDeleteGem(Character self) {
			Gems(self)[2].Delete();
			Console.WriteLine("GUARD_BOSS_GEM_DELETED: guardsOfB=" + GuardBoss.GuardsOf(Gems(self)[1]).Count + " end");
		}
	}
}
