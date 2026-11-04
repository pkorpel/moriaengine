using System;
using System.Collections.Generic;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1022 - server side of Tests/Protocol/DragonEventsTests.cs.</summary>
	public static class DragonEventsTests {
		private static int hitsBefore, stamBefore;
		private static Item cageFire;
		private static NPC lastDragon;

		private static NPC Dragon(Character self, string defname, int dx, Direction facing) {
			if ((lastDragon != null) && !lastDragon.IsDeleted) {
				lastDragon.Delete();
			}
			NPC dragon = (NPC) ThingDef.GetByDefname(defname).Create(self.X + dx, self.Y, self.Z, self.M);
			dragon.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			dragon.Direction = facing;
			lastDragon = dragon;
			return dragon;
		}

		/// <summary>2 tiles off on a side it sees the player from, facing them (the open ground has statics around).</summary>
		private static NPC FacingDragon(Character self, string defname) {
			int[][] sides = { new[] { 2, 0, (int) Direction.West }, new[] { -2, 0, (int) Direction.East }, new[] { 0, 2, (int) Direction.North }, new[] { 0, -2, (int) Direction.South } };
			NPC dragon = null;
			foreach (int[] side in sides) {
				dragon = Dragon(self, defname, 0, (Direction) side[2]);
				dragon.P(self.X + side[0], self.Y + side[1], self.Z, self.M);
				if (dragon.CanSeeLOS(self).Allow) {
					break;
				}
			}
			return dragon;
		}

		private static void Mark(Character self) {
			self.Hits = self.MaxHits;
			self.Stam = self.MaxStam;
			hitsBefore = self.Hits;
			stamBefore = self.Stam;
		}

		private static void ClearLingering(Character self) {
			for (int type = 1; type <= MonsterSpecialAttacks.DragonFireTypes; type++) {
				self.DeleteTimer(DragonLingerTimer.KeyOf(type));
			}
			self.DeleteTimer(DragonAuraTimer.FireKey);
		}

		private static string Lingering(Character self) {
			string types = "";
			for (int type = 1; type <= MonsterSpecialAttacks.DragonFireTypes; type++) {
				if (self.GetTimer(DragonLingerTimer.KeyOf(type)) != null) {
					types += type;
				}
			}
			return (types.Length == 0) ? "none" : types;
		}

		[SteamFunction]
		public static void TestDragon(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			Player player = (Player) self;
			string line;
			switch (what) {
				case "ready":
					MovementSpeedTests.TestTeleportToOpenGround(self);
					VerificationTestUtils.CleanupNearbyNpcs(self, 30);
					self.Account.PLevel = 1;
					self.MaxHits = 30000;
					player.Profession = Professions.Mage;
					line = "ready";
					break;
				case "cone": {
						ClearLingering(self);
						NPC dragon = FacingDragon(self, "c_drak_hnedy");
						int[] tuned = (dragon.Def as NPCDef).AbilityTuning("fireDragonDamage");
						Mark(self);
						MonsterSpecialAttacks.ForceFireDragonBreath(dragon, 120, 4);
						line = "tuned=" + ((tuned == null) ? "none" : tuned[0].ToString()) + " lost=" + (hitsBefore - self.Hits) + " lingering=" + Lingering(self) +
							" breathAbility=" + dragon.HasAbility(MonsterSpecialAttacks.FireDragonDef, out _) + " sees=" + dragon.CanSeeLOS(self).Allow;
						break;
					}
				case "resist": {
						ClearLingering(self);
						NPC dragon = FacingDragon(self, "c_drak_hnedy");
						self.SetTag(TagKey.Acquire("dragon_resist"), 200);
						Mark(self);
						MonsterSpecialAttacks.ForceFireDragonBreath(dragon, 120, 4);
						self.RemoveTag(TagKey.Acquire("dragon_resist"));
						line = "lost=" + (hitsBefore - self.Hits) + " lingering=" + Lingering(self);
						break;
					}
				case "bomb": {
						ClearLingering(self);
						NPC dragon = Dragon(self, "c_drak_led", 5, Direction.West);
						Mark(self);
						MonsterSpecialAttacks.DropDragonBomb(dragon, self, 40, 2);
						line = "lost=" + (hitsBefore - self.Hits) + " lingering=" + Lingering(self);
						break;
					}
				case "spin": {
						ClearLingering(self);
						NPC dragon = Dragon(self, "c_drak_bily", -2, Direction.NorthEast);
						Mark(self);
						MonsterSpecialAttacks.SpinDragon(dragon, 100, 3);
						line = "facing=" + dragon.Direction + " lost=" + (hitsBefore - self.Hits) + " lingering=" + Lingering(self) + " frozen=" + NPCAiTimer.IsFrozen(dragon);
						break;
					}
				case "aura": {
						ClearLingering(self);
						NPC dragon = Dragon(self, "c_drak_hnedy", 3, Direction.West);
						Mark(self);
						int reached = MonsterSpecialAttacks.SpreadDragonAura(dragon, self, false, true, 50, 10, 5);
						line = "reached=" + reached + " aura=" + (self.GetTimer(DragonAuraTimer.FireKey) != null);
						break;
					}
				case "auratick":
					line = "lost=" + (hitsBefore - self.Hits) + " stamLost=" + (stamBefore - self.Stam);
					break;
				case "cage": {
						NPC dragon = Dragon(self, "c_drak_cerny", 3, Direction.West);
						int caged = MonsterSpecialAttacks.CageAround(dragon, 2, 4);
						cageFire = DragonCage.FireAt(self.X + 1, self.Y, self.M);
						bool refused = DragonCage.Refuses(self, self.X + 1, self.Y, self.M);
						bool clicked = DragonCage.TryClick(self, cageFire);
						line = "caged=" + caged + " fire=" + (cageFire != null) + " refused=" + refused + " clicked=" + clicked + " standing=" + !cageFire.IsDeleted;
						break;
					}
				case "cageclick": {
						bool clicked = DragonCage.TryClick(self, cageFire);
						line = "clicked=" + clicked + " gone=" + cageFire.IsDeleted + " open=" + !DragonCage.Refuses(self, self.X + 1, self.Y, self.M);
						break;
					}
				case "blood": {
						NPC dragon = Dragon(self, "c_drak_bahno", 2, Direction.West);
						dragon.MaxHits = 1000;
						dragon.Hits = 790;
						MonsterSpecialAttacks.TryDragonReactions(self, dragon);
						int first = Puddles(dragon);
						dragon.Hits = 300;
						MonsterSpecialAttacks.TryDragonReactions(self, dragon);
						line = "first=" + first + " later=" + Puddles(dragon);
						break;
					}
				case "stamp": {
						NPC dragon = Dragon(self, "c_dragon_swamp", 2, Direction.West);
						Mark(self);
						MonsterSpecialAttacks.DragonThink(dragon, 1);
						line = "brain=" + dragon.HasAbility(MonsterSpecialAttacks.DragonBrainDef, out _) + " frozen=" + NPCAiTimer.IsFrozen(dragon);
						break;
					}
				case "stamped":
					line = "lost=" + (hitsBefore - self.Hits) + " paralysed=" + ParalyzeEffectPluginDef.IsParalyzed(self);
					break;
				default:
					line = "unknown";
					break;
			}
			Console.WriteLine("DRAGON_TEST: " + what + " " + line + " |");
		}

		private static int Near(Character self, string defname, NPC owner) {
			int count = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 6)) {
				if ((ch is NPC npc) && (npc.Def == ThingDef.GetByDefname(defname)) && ((owner == null) || (npc.ControlMaster == owner)) && !npc.IsDeleted) {
					count++;
				}
			}
			return count;
		}

		private static NPC Boss(Character self, string defname, int dx) {
			NPC boss = Dragon(self, defname, 0, Direction.West);
			boss.P(self.X + dx, self.Y, self.Z, self.M);
			return boss;
		}

		/// <summary>#1022 PR 2 - the summoning bosses, each roll forced by its odds set to 1 in 1.</summary>
		[SteamFunction]
		public static void TestBoss(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			Player player = (Player) self;
			string line;
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			switch (what) {
				case "firelord": {
						NPC lord = Boss(self, "c_fire_lord", 2);
						FireLordAbilityDef def = MonsterSpecialAttacks.FireLordDef;
						int explosion = def.ExplosionOneIn, summon = def.SummonOneIn;
						def.ExplosionOneIn = 1;
						def.SummonOneIn = 1;
						MonsterSpecialAttacks.TrySummonerReactions(lord, self);
						def.ExplosionOneIn = explosion;
						def.SummonOneIn = summon;
						line = "phoenixes=" + Near(self, "c_m_phoenix_fire", lord) + " eyes=" + Near(self, "c_eye_fire", lord) + " columns=" + (MonsterSpecialAttacks.CountLiveHazardTiles() > 0);
						Mark(self);
						FightMemory.Record(lord, self);
						lord.CauseDeath(self);
						line += " deathBlast=" + (hitsBefore - self.Hits);
						break;
					}
				case "cajun": {
						NPC cajun = Boss(self, "c_cajun", 2);
						CajunAbilityDef def = MonsterSpecialAttacks.CajunDef;
						int odds = def.SummonOneIn;
						def.SummonOneIn = 1;
						MonsterSpecialAttacks.TrySummonerReactions(cajun, self);
						def.SummonOneIn = odds;
						line = "children=" + (Near(self, "c_cajun_child1", null) + Near(self, "c_cajun_child2", null) + Near(self, "c_cajun_child3", null)) +
							" third=" + Near(self, "c_cajun_child3", null);
						break;
					}
				case "drone": {
						NPC drone = Boss(self, "c_drone_large", 2);
						NPCAiTimer.EnsureInstalled(drone);
						NPCAiTimer ai = NPCAiTimer.GetFor(drone);
						ai.hasHome = true;
						ai.homeX = self.X - 3;
						ai.homeY = self.Y;
						ai.homeZ = (sbyte) self.Z;
						ai.homeM = self.M;
						DroneSummonerAbilityDef def = MonsterSpecialAttacks.DroneSummonerDef;
						int three = def.ThreeOneIn, one = def.OneOneIn;
						def.ThreeOneIn = 1;
						def.OneOneIn = 1000000;
						MonsterSpecialAttacks.TrySummonerReactions(drone, self);
						def.ThreeOneIn = three;
						def.OneOneIn = one;
						line = "drones=" + Near(self, "c_drone_small", null) + " home=" + ((drone.X == self.X - 3) && (drone.Y == self.Y));
						break;
					}
				case "druid": {
						NPC druid = Boss(self, "c_druid_serpent", 2);
						SerpentDruidAbilityDef def = MonsterSpecialAttacks.SerpentDruidDef;
						int odds = def.SummonOneIn;
						def.SummonOneIn = 1;
						MonsterSpecialAttacks.TrySummonerOwnSwing(druid, self);
						int snakes = Near(self, "c_serpent_malej", druid);
						druid.Hits = (short) ((druid.MaxHits / 2) - 1);
						MonsterSpecialAttacks.TrySummonerReactions(druid, self);
						MonsterSpecialAttacks.TrySummonerOwnSwing(druid, self);
						def.SummonOneIn = odds;
						line = "snakes=" + snakes + " serpent=" + MonsterSpecialAttacks.InSerpentForm(druid) +
							" model=" + (druid.Model == ((NPCDef) ThingDef.GetByDefname("c_druid_had")).Model) + " snakesAfter=" + Near(self, "c_serpent_malej", druid);
						break;
					}
				case "worms": {
						NPC worm = Boss(self, "c_m_swarpus", 3);
						NPCAiTimer.EnsureInstalled(worm);
						MonsterSpecialAttacks.TrySeeNewPlayers(worm);
						int sight = Near(self, "c_cerv", null);
						NPCAiTimer.GetFor(worm).Disengage();
						MonsterSpecialAttacks.TrySeeNewPlayers(worm);
						int again = Near(self, "c_cerv", null);
						bool poisonRefused = MonsterSpecialAttacks.TryBossSpellEffect(worm, SpellDef.GetById(20));
						bool healRefused = MonsterSpecialAttacks.TryBossSpellEffect(worm, SpellDef.GetById(4));
						//other test characters stand around too: three worms for each player it sees
						line = "sightThreeEach=" + ((sight > 0) && (sight % 3 == 0)) + " noneAgain=" + (again == sight) + " poisonRefused=" + poisonRefused +
							" healRefused=" + healRefused + " spellWorms=" + (Near(self, "c_cerv", null) - again);
						break;
					}
				case "guardian": {
						player.Profession = Professions.Mage;
						NPC guardian = Boss(self, "c_Strazce_numerojcu", 2);
						MonsterSpecialAttacks.SeeNewPlayer(guardian, player);
						int golems = Near(self, "c_pisecny_sumon_golem", null);
						Mark(self);
						guardian.CauseDeath(self);
						line = "golems=" + golems + " vanishes=" + MonsterSpecialAttacks.VanishesAtDeath(guardian) + " blast=" + (hitsBefore - self.Hits) +
							" exp=" + guardian.ExperienceReward;
						break;
					}
				default:
					line = "unknown";
					break;
			}
			Console.WriteLine("BOSS_TEST: " + what + " " + line + " |");
		}

		private static int Puddles(NPC dragon) {
			int count = 0;
			foreach (AbstractItem item in dragon.GetMap().GetItemsInRange(dragon.X, dragon.Y, 4)) {
				if (item.Def == ThingDef.GetByDefname("i_bahenny_drak_krev")) {
					count++;
				}
			}
			return count;
		}
	}
}
