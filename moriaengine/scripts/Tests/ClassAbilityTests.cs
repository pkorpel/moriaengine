using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - spending Erebor's two points a level, by class and level.</summary>
	public static class ClassAbilityTests {
		[SteamFunction]
		public static void TestClassAbilityRules(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Profession = Professions.Thief;
			ClassAbilities.Reset(player);
			RearAbility dodge = ClassAbilities.Find("weapdodge"), stun = ClassAbilities.Find("stuntattack"), regen = ClassAbilities.Find("manareg");

			player.Level = 0;
			Report("noLevelNoPoints", (ClassAbilities.Spare(player) == 0) && !ClassAbilities.Raise(player, regen));

			player.Level = 60;
			Report("sparePointsAreLevelTimesTwo", ClassAbilities.Spare(player) == 120);
			Report("notForThisClass", !ClassAbilities.Raise(player, stun));
			Report("offeredOnlyWhatTheClassHas", !ClassAbilities.Available(player).Contains(stun) && ClassAbilities.Available(player).Contains(dodge));

			Fill(player, dodge);
			Report("level60ThiefDodgeStopsAt20", ClassAbilities.Points(player, "weapdodge") == 20);
			player.Level = 61;
			Fill(player, dodge);
			Report("level61ThiefDodgeGoesTo40", ClassAbilities.Points(player, "weapdodge") == 40);
			Report("spentPointsLeaveTheSpare", ClassAbilities.Spare(player) == 82);
			Report("anExistingAbilityGetsThePoints", player.GetAbility(AbilityDef.GetByDefname("a_weapon_dodge")) == 40);

			player.Profession = Professions.Mage;
			Report("aMageKeepsToItsOwnLimit", !ClassAbilities.MayRaise(player, dodge, false));

			player.Profession = Professions.Warrior;
			player.Level = 49;
			Report("tooYoungForTheFirstStep", !ClassAbilities.MayRaise(player, ClassAbilities.Find("stuntattack_prodleva"), false));
			player.Level = 50;
			Report("oldEnoughForIt", ClassAbilities.Raise(player, ClassAbilities.Find("stuntattack_prodleva")));
			Report("aTagAbilityGetsThePoint", ClassAbilities.Points(player, "stuntattack_prodleva") == 1);

			ClassAbilities.Reset(player);
			Report("resetClearsEverything", ClassAbilities.Spent(player) == 0);

			player.Level = 10;
			Console.WriteLine("CLASS_ABILITY_DONE");
		}

		[SteamFunction]
		public static void TestClassAbilityReport(Character self) {
			Console.WriteLine("CLASS_ABILITY_POINTS: manareg=" + ClassAbilities.Points(self, "manareg") + " end");
		}

		/// <summary>Sets a warrior up with rabbits to hit and a two handed sword.</summary>
		[SteamFunction]
		public static void TestWarriorAbilities(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			player.Profession = Professions.Warrior;
			player.Level = 100;
			player.Str = 250;
			player.MaxStam = 200;
			player.Stam = 200;
			ClassAbilities.Reset(player);
			foreach (SkillName skill in new[] { SkillName.Swords, SkillName.Tactics, SkillName.Anatomy, SkillName.ArmsLore }) {
				player.SetRealSkillValue((int) skill, 1000);
			}
			player.Dex = 100;

			ClassAbilities.SetPoints(player, "roundattack", 10);
			Item sword = (Item) ((ItemDef) ThingDef.GetByDefname("i_axe_battle_large")).Create(player);
			Console.WriteLine("WARRIOR_INFO: weapon type=" + ((sword.Type == null) ? "null" : sword.Type.Defname) + " layer=" + sword.Layer);
			NPC near = Dummy(self, 1), far = Dummy(self, 3);
			short nearHits = near.Hits, farHits = far.Hits;
			double weapAttack = CombatSettings.instance.weapAttack;
			CombatSettings.instance.weapAttack = weapAttack * 100; //a bare test character's blow doesn't get through a rabbit's hide
			WarriorAbilities.FinishRoundAttack(player);
			CombatSettings.instance.weapAttack = weapAttack;
			Report("roundAttackHitsWhoIsNear", near.Hits < nearHits);
			Report("roundAttackLeavesTheRest", far.Hits == farHits);
			player.Mana = 50;
			ClassAbilities.SetPoints(player, "warcry", 10);
			WarriorAbilities.Warcry(player);
			Report("warcryTakesOwnMana", player.Mana == 0);
			Report("warcryMustCoolDown", WarriorAbilities.IsWarrior(player));

			ClassAbilities.SetPoints(player, "stuntattack", 10);
			WarriorAbilities.Omraceni(player);
			Report("stunWindowsShrink", (WarriorAbilities.StunWindowSeconds(0) == 20) && (WarriorAbilities.StunWindowSeconds(30) == 11));
			Report("stunLimitGrowsWithTheMetal", (WarriorAbilities.StunLimit(Material.Copper, false, 0) == 51) && (WarriorAbilities.StunLimit(Material.Mithril, false, 30) == 901 + (901 * 20 / 100)));
			Report("plainMaceStunsNothing", WarriorAbilities.StunLimit(Material.None, false, 0) == 0);
			Report("shieldsByMetal", (WarriorAbilities.ShieldBlockPercent(Material.Mithril) == 50) && (WarriorAbilities.ShieldBlockPercent(Material.None) == 100));

			player.Stam = 200;
			WarriorAbilities.DefStance(player);
			Report("stanceOnCostsHalfStamina", WarriorAbilities.InDefStance(player) && (player.Stam == 100));
			player.Stam = 200;
			WarriorAbilities.DefStance(player);
			Report("stanceOff", !WarriorAbilities.InDefStance(player));

			ClassAbilities.SetPoints(player, "taunt", 10);
			player.Stam = 200;
			near.Delete();
			far.Hits = far.MaxHits;
			WarriorAbilities.TauntOne(player, far);
			NPCAiTimer ai = NPCAiTimer.GetFor(far);
			Report("tauntTurnsItOnTheWarrior", (ai.Target == player) && far.HasTimer(TauntTimer.Key));
			Report("tauntingHardensIt", TauntTimer.Resist(far) == 250);

			Character none = WeaponSkillTargetQueuePlugin.CurrentTarget(player);
			Report("leapNeedsAFight", none == null);
			player.Stam = player.MaxStam;
			far.Delete();
			Console.WriteLine("WARRIOR_DONE");
		}

		[SteamFunction]
		public static void TestRangerAbilities(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			player.Profession = Professions.Ranger;
			player.Level = 100;
			player.MaxStam = 200;
			player.Stam = 200;
			ClassAbilities.Reset(player);

			ClassAbilities.SetPoints(player, "rangearrow", 10);
			int rangeBefore = player.WeaponRangeModifier;
			RangerAbilities.LongRangeAbility(player);
			Report("longRangeAddsThree", player.WeaponRangeModifier == rangeBefore + 3);
			Report("longRangeCostsStamina", player.Stam == 150);

			ClassAbilities.SetPoints(player, "knockback", 10);
			RangerAbilities.KnockbackAbility(player);
			Report("knockbackEndsLongRange", player.WeaponRangeModifier == rangeBefore);

			Report("splitRangeGrowsWithDistance", (RangerAbilities.SplitRange(20, 10) > RangerAbilities.SplitRange(20, 3)));
			Report("splitRangeStaysUnderItsLimit", RangerAbilities.SplitRange(20, 15) == 3);
			Report("splitNextToTheTargetIsStrongest", (RangerAbilities.SplitPermille(20, 175, 1) > RangerAbilities.SplitPermille(20, 175, 3)));
			Report("splitIsNeverAFullBlow", RangerAbilities.SplitPermille(20, 175, 1) < 1000);

			ClassAbilities.SetPoints(player, "splitshot", 10);
			player.Stam = 200;
			RangerAbilities.SplitShot(player);
			Report("splitShotCostsStamina", player.Stam == 125);
			player.Stam = 200;
			RangerAbilities.SplitShot(player);
			Report("splitShotMustCoolDown", player.Stam == 200);
			NPC prey = Dummy(self, 2);
			prey.ResistSlashing = 500;
			CrushedResistsTimer.Apply(prey, 100, 30);
			Report("crushingArrowWearsResists", prey.ResistSlashing == 400);
			prey.ResistBlunt = 1000;
			CrushedResistsTimer.Apply(prey, 100, 30);
			Report("aSecondArrowReplacesTheFirst", prey.ResistSlashing == 400);

			ClassAbilities.SetPoints(player, "killingSpree", 10);
			RangerAbilities.OnKill(player, prey);
			RangerAbilities.OnKill(player, prey);
			SpreeTimer spree = (SpreeTimer) player.GetTimer(SpreeTimer.Key);
			Report("killsBuildASpree", (spree != null) && (spree.charges == 2));
			prey.Delete();
			Console.WriteLine("RANGER_DONE");
		}

		[SteamFunction]
		public static void TestPriestAbilities(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Profession = Professions.Priest;
			player.Level = 100;
			ClassAbilities.Reset(player);
			ClassAbilities.SetPoints(player, "selfres", 20);
			ClassAbilities.SetPoints(player, "selfres_hp", 5);

			Report("restoredByTens", (PriestAbilities.Restored(5, 200) == 100) && (PriestAbilities.Restored(0, 200) == 3));
			LifeCandle candle = (LifeCandle) ((ItemDef) ThingDef.GetByDefname("i_svice_zivota")).Create(player.Backpack);
			candle.On_DClick(player);
			Report("lightingMakesALitCandle", (PriestAbilities.FindLitCandle(player) != null) && candle.IsDeleted);
			player.CauseDeath(null);
			Report("theCandleStaysThroughDeath", PriestAbilities.FindLitCandle(player) != null);
			PriestAbilities.Resurrection(player);
			Report("tooRestlessRightAfterDying", player.Flag_Dead);
			Console.WriteLine("PRIEST_DIED");
		}

		[SteamFunction]
		public static void TestPriestRise(Character self) {
			Player player = (Player) self;
			for (int i = 0; (i < 200) && player.Flag_Dead; i++) { //90% a try at 20 points, each failure's delay wiped
				PriestAbilities.Resurrection(player);
				self.RemoveTag(TagKey.Acquire("_priestNextSelfres_"));
			}
			Report("thePriestRises", !player.Flag_Dead);
			Report("risesAtHalfHits", player.Hits == (player.MaxHits / 2));
			Report("theCandleBurnsOut", PriestAbilities.FindLitCandle(player) == null);
			Console.WriteLine("PRIEST_DONE");
		}

		[SteamFunction]
		public static void TestCraftsmanVigour(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Profession = Professions.Craftsman;
			player.Level = 100;
			player.Stam = player.MaxStam;
			ClassAbilities.Reset(player);
			RegionResourceDef iron = RegionResourceDef.GetByDefname("o_iron");
			Report("ironLeadsToVerite", (iron != null) && (CraftsmanAbilities.Next(iron) == RegionResourceDef.GetByDefname("o_verite")));
			Report("mithrilIsTheTop", CraftsmanAbilities.Next(RegionResourceDef.GetByDefname("l_mithril")) == null);
			Report("noCounterNoLuck", !CraftsmanAbilities.FindsBetter(player, "vigourMining"));
			CraftsmanAbilities.Vigour(player);
			ClassAbilities.SetPoints(player, "vigourMining", 500);
			Report("theCounterAndPointsFindBetter", CraftsmanAbilities.FindsBetter(player, "vigourMining"));
			Console.WriteLine("CRAFTSMAN_DONE");
		}

		[SteamFunction]
		public static void TestThiefAbilities(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			player.Profession = Professions.Thief;
			player.Level = 100;
			player.MaxStam = 200;
			player.Stam = 200;
			ClassAbilities.Reset(player);
			Report("behindBonusSteps", (ThiefAbilities.FromBehindBonus(10) == 100) && (ThiefAbilities.FromBehindBonus(20) == 220));
			Report("throatBonusSteps", (ThiefAbilities.ThroatBonus(10) == 40) && (ThiefAbilities.ThroatBonus(30) == 70));
			NPC victim = Dummy(self, 1);
			victim.Direction = Direction.East; //looking away from the thief, who stands to its west
			Report("seenFromBehind", ThiefAbilities.IsBehind(player, victim));
			victim.Direction = Direction.West;
			Report("notFromTheFront", !ThiefAbilities.IsBehind(player, victim));

			ClassAbilities.SetPoints(player, "podriznuti", 10);
			ThiefAbilities.Podriznuti(player);
			Report("throatSlitCostsStamina", player.Stam == 155);
			ThiefAbilities.Podriznuti(player);
			Report("throatSlitMustCoolDown", player.Stam == 155);

			ClassAbilities.SetPoints(player, "uderstitem", 10);
			player.Stam = 200;
			ThiefAbilities.UderStitem(player);
			Report("shieldStrikeCostsStamina", player.Stam == 170);

			foreach (SkillName skill in new[] { SkillName.Fencing, SkillName.Tactics, SkillName.Anatomy }) {
				player.SetRealSkillValue((int) skill, 1000);
			}
			((ItemDef) ThingDef.GetByDefname("i_kryss")).Create(player);
			double weapAttack = CombatSettings.instance.weapAttack;
			CombatSettings.instance.weapAttack = weapAttack * 100;
			ClassAbilities.SetPoints(player, "backstab", 30);
			HidingSkillDef.Hide(player);
			player.SetTag(TagKey.Acquire("_hiddenAt_"), Globals.TimeInSeconds - 10);
			short before = victim.Hits;
			for (int i = 0; (i < 20) && (victim.Hits == before); i++) { //30 points: 105% a try against a monster
				player.Stam = 200;
				ThiefAbilities.FinishStab(player, victim);
			}
			CombatSettings.instance.weapAttack = weapAttack;
			Report("aBackstabHurts", victim.Hits < before);
			Report("aBackstabReveals", !player.Flag_Hidden);
			victim.Delete();

			ThiefMask.ClearMask(player);
			player.Realm = Realm.Gondor;
			string realName = player.Name;
			ThiefMask.SetAttributes(player, "Stinovy", -5000, 3, Realm.Mordor);
			ThiefMask.Swap(player, ThiefMask.Attributes(player));
			Report("maskChangesNameAndRealm", (player.Name == "Stinovy") && (player.Realm == Realm.Mordor) && (Notoriety.GetMurderCount(player) == 3));
			ThiefMask.Swap(player, ThiefMask.Attributes(player));
			Report("maskSwapsBack", (player.Name == realName) && (player.Realm == Realm.Gondor));
			player.Realm = Realm.None;
			NPC mark = Dummy(self, 3);
			WeaponSkillTargetQueuePlugin.AddTarget(player, mark);
			Item knives = (Item) ((ItemDef) ThingDef.GetByDefname("i_kudla")).Create(player.Backpack);
			knives.Amount = 20;
			ClassAbilities.SetPoints(player, "throwing", 10);
			short markHits = mark.Hits;
			for (int i = 0; (i < 10) && (mark.Hits == markHits); i++) {
				player.Stam = 200;
				player.RemoveTag(TagKey.Acquire("_nextThrow_"));
				ThiefAbilities.ThrowKnife(player);
			}
			Report("aKnifeHitsTheMonster", mark.Hits == markHits - 1);
			Report("knivesAreUsedUp", knives.IsDeleted || (knives.Amount < 20));
			ThiefDefence.SetMagicDefence(mark, 0, 0, 100, 0); //#1070 - magic ranged reflex, then block
			markHits = mark.Hits;
			player.Stam = 200;
			player.RemoveTag(TagKey.Acquire("_nextThrow_"));
			ThiefAbilities.ThrowKnife(player);
			Report("aMagicReflexSendsTheKnifeBack", (mark.Hits == markHits) && (player.Stam == ((200 - (3 * ClassAbilitySettings.instance.throwStaminaPerTile)) * 4) / 5));
			ThiefDefence.SetMagicDefence(mark, 0, 0, 0, 100);
			player.RemoveTag(TagKey.Acquire("_nextThrow_"));
			ThiefAbilities.ThrowKnife(player);
			Report("aMagicBlockStopsIt", mark.Hits == markHits);
			ThiefDefence.SetMagicDefence(mark, 0, 0, 0, 0);
			WeaponSkillTargetQueuePlugin.RemoveTarget(player, mark);
			mark.Delete();
			StrikeChargeTimer.Add(player);
			StrikeChargeTimer.Add(player);
			player.Stam = 200;
			NPC beside = Dummy(self, 1);
			short besideHits = beside.Hits;
			CombatSettings.instance.weapAttack = weapAttack * 100;
			ThiefAbilities.Strike(player);
			CombatSettings.instance.weapAttack = weapAttack;
			Report("strikeSpendsChargesOnThoseAround", (beside.Hits < besideHits) && !player.HasTimer(StrikeChargeTimer.Key) && (player.Stam == 160));
			beside.Delete();
			Console.WriteLine("THIEF_DONE");
		}

		[SteamFunction]
		public static void TestNecroAbilities(Character self) {
			Player player = (Player) self;
			Report("undeadHitsByPoints", (NecroAbilities.UndeadHits(100, 10) == 120) && (NecroAbilities.UndeadHits(100, 40) == 135) && (NecroAbilities.UndeadHits(100, 0) == 100));
			player.Profession = Professions.Necromant;
			player.Level = 100;
			player.Stam = player.MaxStam;
			ClassAbilities.OnDeath(player); //the craftsman's counter from earlier on this character
			NecroAbilities.Darkpower(player);
			Report("darkpowerTurnsTheCounterOn", ClassAbilities.CounterOn(player));
			player.MaxMana = 200;
			player.Mana = 0;
			SpellDef fireball = SpellDef.GetByDefname("s_fireball");
			for (int i = 0; (i < 400) && (player.Mana == 0); i++) {
				NecroAbilities.SpellDone(player, fireball, false);
			}
			Report("darkpowerGivesManaBack", player.Mana == (fireball.ManaUse * 850) / 1000);
			player.Mana = 0;
			NecroAbilities.SpellDone(player, SpellDef.GetByDefname("s_recall"), false);
			Report("notForRecall", player.Mana == 0);
			Console.WriteLine("NECRO_DONE");
		}

		internal static NPC Dummy(Character self, int offset) {
			NPC dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + offset, self.Y, self.Z, self.M);
			dummy.MaxHits = 30000;
			dummy.Hits = 30000;
			return dummy;
		}

		private static void Fill(Player player, RearAbility ability) {
			while (ClassAbilities.MayRaise(player, ability, false) && ClassAbilities.Raise(player, ability)) {
			}
		}

		internal static void Report(string name, bool ok) {
			Console.WriteLine("CLASS_ABILITY: " + name + "=" + ok);
		}
	}
}
