/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #791 - server side of Tests/Protocol/MonsterLootTests.cs: real converted monsters with
	/// "Loot =" lines (Erebor's @NPCRestock) and "Equip = i_monster_create" (Erebor's creaturestart).
	/// </summary>
	public static class MonsterLootTests {
		[SteamFunction]
		public static void TestOrcLoot(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC orc = Spawn("c_orc", self);
			bool pending = MonsterLoot.FlushCreatureStart(orc); //#794 - creaturestart runs on a 0 s timer
			int exp = orc.ExperienceReward;
			int expected = MonsterLoot.GoldFromExperience(exp, MonsterLoot.IsDungeonMapPlane(orc.M),
				LootSettings.instance.goldPermilleSurface, LootSettings.instance.goldPermilleDungeon);
			int gold = CountInPack(orc, "i_gold");
			bool boots = CountInPack(orc, "i_boots_thigh") > 0;
			bool monsterCreateItem = false;
			foreach (AbstractItem item in orc) {
				if (MonsterLoot.IsMonsterCreateMarker(item.Def.PrettyDefname)) {
					monsterCreateItem = true;
				}
			}

			int x = orc.X, y = orc.Y;
			orc.CauseDeath(null);
			int corpseGold = 0;
			bool corpseBoots = false;
			foreach (Thing t in self.GetMap().GetThingsInRange(x, y, 0)) {
				Corpse corpse = t as Corpse;
				if (corpse == null) {
					continue;
				}
				foreach (Item i in corpse.EnumShallow()) {
					string defname = i.Def.PrettyDefname;
					if (defname == "i_gold") {
						corpseGold += i.Amount;
					} else if (defname == "i_boots_thigh") {
						corpseBoots = true;
					}
				}
			}
			Console.WriteLine("MONSTER_LOOT: pending=" + pending + " exp=" + exp + " gold=" + gold + " expectedGold=" + expected +
				" boots=" + boots + " monsterCreateItem=" + monsterCreateItem +
				" corpseGold=" + corpseGold + " corpseBoots=" + corpseBoots);
		}

		/// <summary>loot_cyclops nests tm_rocks/tm_boulders templates (no CONTAINER of their own),
		/// which used to make Container.NewItem delete the backpack they'd just filled.</summary>
		[SteamFunction]
		public static void TestCyclopsLoot(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC cyclops = Spawn("c_cyclops", self);
			AbstractItem pack = cyclops.FindLayer(LayerNames.Pack);
			int items = 0;
			if (pack != null) {
				foreach (AbstractItem i in pack) {
					items++;
				}
			}
			Console.WriteLine("CYCLOPS_LOOT: backpackAlive=" + ((pack != null) && !pack.IsDeleted) + " items=" + items);
			cyclops.Delete();
		}

		/// <summary>loot_OGRE has "ITEM=RANDOM_JEWEL,{0 1}" - a rolled 0 must mean no item, not an
		/// empty stack.</summary>
		[SteamFunction]
		public static void TestNoZeroAmountLoot(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			int zero = 0, total = 0;
			List<NPC> ogres = new List<NPC>();
			for (int n = 0; n < 20; n++) {
				NPC ogre = Spawn("c_ogre", self);
				ogres.Add(ogre);
				foreach (AbstractItem i in ogre.Backpack) {
					total++;
					if (i.Amount <= 0) {
						zero++;
					}
				}
			}
			foreach (NPC ogre in ogres) {
				ogre.Delete();
			}
			Console.WriteLine("ZERO_LOOT: items=" + total + " zeroAmount=" + zero);
		}

		/// <summary>#793 - human NPCs' @NPCRestock clothing: worn, newbie (never on the corpse),
		/// RANDOM_* pick lists resolved, NAME= applied.</summary>
		[SteamFunction]
		public static void TestNpcRestockGear(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC banker = Spawn("c_h_banker", self);
			int worn = 0, notNewbie = 0;
			bool shirt = false, light = false;
			foreach (AbstractItem i in banker) {
				if (i == banker.Backpack) {
					continue;
				}
				worn++;
				if (!((Item) i).HasAttribute(ItemAttributes.attr_newbie)) {
					notNewbie++;
				}
				string dn = i.Def.PrettyDefname.ToLowerInvariant();
				shirt |= dn == "i_shirt_fancy";
				light |= dn.Contains("candle") || dn.Contains("torch") || dn.Contains("lantern");
			}
			foreach (AbstractItem i in banker.Backpack) {
				string dn = i.Def.PrettyDefname.ToLowerInvariant();
				light |= dn.Contains("candle") || dn.Contains("torch") || dn.Contains("lantern");
			}
			int x = banker.X, y = banker.Y;
			banker.CauseDeath(null);
			int corpseItems = 0;
			foreach (Thing t in self.GetMap().GetThingsInRange(x, y, 0)) {
				Corpse corpse = t as Corpse;
				if (corpse != null) {
					foreach (Item i in corpse.EnumShallow()) {
						corpseItems++;
					}
				}
			}
			NPC alchemist = Spawn("c_alchymista", self);
			bool namedRobe = false;
			foreach (AbstractItem i in alchemist) {
				namedRobe |= i.Name == "Roba s kapi";
			}
			alchemist.Delete();
			Console.WriteLine("NPC_GEAR: worn=" + worn + " notNewbie=" + notNewbie + " shirt=" + shirt +
				" light=" + light + " corpseItems=" + corpseItems + " namedRobe=" + namedRobe);
		}

		/// <summary>#794 - the elite transform on a real 1100+ experience monster, a boss refusing it,
		/// the spawn-gem roll, and creaturestart's gold following the raised experience.</summary>
		[SteamFunction]
		public static void TestEliteMonster(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			EliteSettings settings = EliteSettings.instance;
			NPC golem = Spawn("c_golem_mithril", self);
			golem.AggroMode = AggroMode.Calm;
			int str0 = golem.Str, exp0 = golem.ExperienceReward;
			bool transformed = EliteMonster.Transform(golem);
			bool again = EliteMonster.Transform(golem);
			int power = EliteMonster.GetPower(golem);
			MonsterLoot.FlushCreatureStart(golem);
			int gold = CountInPack(golem, "i_gold");
			int expectedGold = MonsterLoot.GoldFromExperience(golem.ExperienceReward, MonsterLoot.IsDungeonMapPlane(golem.M),
				LootSettings.instance.goldPermilleSurface, LootSettings.instance.goldPermilleDungeon);
			int diamonds = CountInPack(golem, "i_gem_diamond");
			int reagents = CountInPack(golem, "i_reag_garlic") + CountInPack(golem, "i_reag_ginseng");
			bool regenTimer = golem.HasTimer(EliteMonster.regenTimerKey);
			int str1 = golem.Str, exp1 = golem.ExperienceReward, resistPoison = golem.ResistPoison, resistSummon = golem.ResistSummon;
			golem.Hits = (short) (golem.MaxHits / 2);
			regenProbe = golem;
			regenProbeHits = golem.Hits;

			NPC boss = Spawn("c_balrogh", self);
			bool bossTransformed = EliteMonster.Transform(boss);
			boss.Delete();

			//the spawn-gem roll, made certain
			int oldChance = settings.maxSpawnChance, oldPercent = settings.spawnChancePercentOfExperience;
			settings.maxSpawnChance = 10000;
			settings.spawnChancePercentOfExperience = 100000;
			Item gem = (Item) ((ItemDef) ThingDef.GetByDefname("i_worldgem_bit")).Create(self.X, self.Y, self.Z, self.M);
			NPC spawned = Spawn("c_golem_mithril", self);
			spawned.SetTag(TagKey.Acquire("spawnGem"), gem);
			MonsterLoot.FlushCreatureStart(spawned);
			NPC unspawned = Spawn("c_golem_mithril", self);
			MonsterLoot.FlushCreatureStart(unspawned);
			gem.SetTag(TagKey.Acquire("denyElite"), 1);
			NPC denied = Spawn("c_golem_mithril", self);
			denied.SetTag(TagKey.Acquire("spawnGem"), gem);
			MonsterLoot.FlushCreatureStart(denied);
			settings.maxSpawnChance = oldChance;
			settings.spawnChancePercentOfExperience = oldPercent;
			bool spawnedElite = EliteMonster.IsElite(spawned), unspawnedElite = EliteMonster.IsElite(unspawned),
				deniedElite = EliteMonster.IsElite(denied);
			spawned.Delete();
			unspawned.Delete();
			denied.Delete();
			gem.Delete();

			Console.WriteLine("ELITE: transformed=" + transformed + " again=" + again + " power=" + power +
				" expectedPower=" + EliteMonster.PowerFromExperience(exp0, settings) +
				" str0=" + str0 + " str1=" + str1 + " exp0=" + exp0 + " exp1=" + exp1 +
				" expectedExp=" + (exp0 * EliteMonster.ExpBonusPercent(power, settings) / 100) +
				" gold=" + gold + " expectedGold=" + expectedGold + " diamonds=" + diamonds + " reagents=" + reagents +
				" resistPoison=" + resistPoison + " resistSummon=" + resistSummon + " regenTimer=" + regenTimer +
				" bossTransformed=" + bossTransformed + " spawnedElite=" + spawnedElite +
				" unspawnedElite=" + unspawnedElite + " deniedElite=" + deniedElite);
		}

		private static NPC regenProbe;
		private static int regenProbeHits;

		/// <summary>Run a few seconds after TestEliteMonster: the half-dead elite golem has regenerated.</summary>
		[SteamFunction]
		public static void TestEliteRegenCheck(Character self) {
			NPC golem = regenProbe;
			int hits = (golem == null || golem.IsDeleted) ? -1 : golem.Hits;
			int perTick = (golem == null) ? 0 : EliteMonster.GetPower(golem) / EliteSettings.instance.regenPowerDivisor;
			Console.WriteLine("ELITE_REGEN: before=" + regenProbeHits + " now=" + hits + " perTick=" + perTick +
				" timerAlive=" + (golem != null && golem.HasTimer(EliteMonster.regenTimerKey)));
			if (golem != null) {
				golem.Delete();
			}
			regenProbe = null;
		}

		private static NPC escapeProbe;
		private static double[] savedEscapeTimes;

		/// <summary>#794 - an elite meeting a player starts its escape countdown; a hit interrupts the
		/// gate spell. Times shortened to 1 s - TestEliteEscapeCheck restores them.</summary>
		[SteamFunction]
		public static void TestEliteEscapeStart(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			EliteSettings settings = EliteSettings.instance;
			savedEscapeTimes = new[] { settings.escapeAfterSeconds, settings.escapeCastSeconds, settings.escapeRetrySeconds };
			settings.escapeAfterSeconds = 1;
			settings.escapeCastSeconds = 1;
			settings.escapeRetrySeconds = 1;
			NPC golem = Spawn("c_golem_mithril", self);
			golem.AggroMode = AggroMode.Calm;
			MonsterLoot.FlushCreatureStart(golem);
			EliteMonster.Transform(golem);
			EliteMonster.OnFightingPlayer(golem, self);
			EliteEscapeTimer timer = golem.GetTimer(EliteMonster.escapeTimerKey) as EliteEscapeTimer;
			bool interrupted = false;
			if (timer != null) {
				timer.state = EliteEscapeTimer.Casting;
				bool leaving = EliteMonster.IsLeaving(golem);
				EliteMonster.OnGotHit(golem);
				interrupted = leaving && (timer.state == EliteEscapeTimer.Waiting) && !EliteMonster.IsLeaving(golem);
			}
			escapeProbe = golem;
			Console.WriteLine("ELITE_ESCAPE_START: timer=" + (timer != null) + " interrupted=" + interrupted);
		}

		[SteamFunction]
		public static void TestEliteEscapeCheck(Character self) {
			EliteSettings settings = EliteSettings.instance;
			if (savedEscapeTimes != null) {
				settings.escapeAfterSeconds = savedEscapeTimes[0];
				settings.escapeCastSeconds = savedEscapeTimes[1];
				settings.escapeRetrySeconds = savedEscapeTimes[2];
			}
			bool gone = (escapeProbe == null) || escapeProbe.IsDeleted;
			if (!gone) {
				escapeProbe.Delete();
			}
			escapeProbe = null;
			Console.WriteLine("ELITE_ESCAPE: gone=" + gone);
		}

		private static NPC Spawn(string defname, Character near) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname(defname);
			return (NPC) def.Create(near.X + 1, near.Y, near.Z, near.M);
		}

		private static int CountInPack(NPC npc, string defname) {
			int amount = 0;
			foreach (AbstractItem i in npc.Backpack) {
				if (i.Def.PrettyDefname == defname) {
					amount += i.Amount;
				}
			}
			return amount;
		}
	}
}
