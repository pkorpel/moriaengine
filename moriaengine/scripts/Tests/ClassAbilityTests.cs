using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

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
			Report("aTauntedMonsterIsEvil", far.Karma == -100); //#1070

			Character none = WeaponSkillTargetQueuePlugin.CurrentTarget(player);
			Report("leapNeedsAFight", none == null);
			player.Stam = player.MaxStam;
			far.Delete();
			TestWarriorPlugin(player, sword);
			Console.WriteLine("WARRIOR_DONE");
		}

		/// <summary>#1429 - the warrior's state is its profession plugin's, and the blow modifiers reach a blow through
		/// @swingLanding and @getSwingLanding: the stance both ways, the shield block (any class) on the way in.</summary>
		private static void TestWarriorPlugin(Player player, Item sword) {
			WarriorProfessionPlugin war = ProfessionPlugin.GetInstalledPlugin(player) as WarriorProfessionPlugin;
			Report("theWarriorsPluginIsItsProfessionPlugin", war != null);
			if (war == null) {
				return;
			}
			Report("cooldownsAreThePlugins", (war.nextCry > Globals.TimeInSeconds) && (war.nextSingleTaunt > Globals.TimeInSeconds) && (war.nextRoundAttack > Globals.TimeInSeconds));

			ClassAbilities.SetPoints(player, "defstance", 10);
			ClassAbilities.SetPoints(player, "doubleswing", 0);
			ClassAbilities.SetPoints(player, "vykryti", 0);
			player.Stam = 200;
			WarriorAbilities.DefStance(player);
			NPC monster = Dummy(player, 1);
			double stance = (100 - ((ClassAbilitySettings.instance.defStanceDamagePercent * 10) / 10)) / 100.0;

			WeaponSwingArgs own = DamageManager.GetWeaponSwingArgs(player, monster);
			own.DamageAfterAC = 1000;
			DamageManager.Trigger_SwingLanding(own);
			Report("theStanceWeakensOwnBlowThroughTheTrigger", Math.Abs(own.DamageAfterAC - (1000 * stance)) < 0.001);

			WeaponSwingArgs incoming = DamageManager.GetWeaponSwingArgs(monster, player);
			incoming.DamageAfterAC = 1000;
			DamageManager.Trigger_GetSwingLanding(incoming);
			Report("theStanceSoftensABlowThroughTheTrigger", Math.Abs(incoming.DamageAfterAC - (1000 * stance)) < 0.001);

			sword.Delete();
			Item shield = (Item) ((ItemDef) ThingDef.GetByDefname("i_MITHRIL_METAL_SHIELD")).Create(player);
			player.InvalidateCombatWeaponValues(); //Character.Weapon is a cache a scripted delete leaves stale
			ClassAbilities.SetPoints(player, "vykryti", 100); //always over the roll of 0-99
			incoming = DamageManager.GetWeaponSwingArgs(monster, player);
			incoming.DamageAfterAC = 1000;
			DamageManager.Trigger_GetSwingLanding(incoming);
			Report("theShieldBlocksWithTheStance", (player.FindLayer(LayerNames.Hand2) == shield) && (Math.Abs(incoming.DamageAfterAC - (1000 * stance * 0.5)) < 0.001));

			ClassAbilities.SetPoints(player, "vykryti", 0);
			TestSwingLandingInASwing(player);

			player.Stam = 200;
			WarriorAbilities.DefStance(player); //off
			player.Profession = Professions.Thief; //the shield is any class's, on the base profession plugin
			ClassAbilities.SetPoints(player, "vykryti", 100);
			incoming = DamageManager.GetWeaponSwingArgs(monster, player);
			incoming.DamageAfterAC = 1000;
			DamageManager.Trigger_GetSwingLanding(incoming);
			Report("aThiefsShieldBlocksThroughItsPlugin", !(ProfessionPlugin.GetInstalledPlugin(player) is WarriorProfessionPlugin) && (Math.Abs(incoming.DamageAfterAC - 500) < 0.001));
			player.Profession = Professions.Warrior;
			ClassAbilities.Reset(player);
			shield.Delete();
			monster.Delete();
		}

		internal static bool setTheBlow;
		internal static double getHitSaw;

		/// <summary>#1429 - the triggers are raised inside a real swing, before @hit: a probe sets the blow to 1000 in the
		/// attacker's @swingLanding (trigger groups run before plugins), the defender's @getHit sees the stance's 750. Both
		/// ways, the warrior in its stance hitting and being hit. @getHit zeroes the blow, so nobody gets hurt.</summary>
		private static void TestSwingLandingInASwing(Player player) {
			NPC orc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(orc, 300);
			orc.AddTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
			player.AddTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
			try {
				Report("aMonstersBlowMeetsTheStanceInTheSwing", Math.Abs(LandOne(orc, player) - 750) < 0.001);
				Report("ownBlowMeetsTheStanceInTheSwing", Math.Abs(LandOne(player, orc) - 750) < 0.001);
				Console.WriteLine("CLASS_ABILITY_INFO: getHitSaw=" + getHitSaw);
			} finally {
				orc.RemoveTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
				player.RemoveTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
				orc.Delete();
				player.Hits = player.MaxHits;
			}
		}

		/// <summary>Swings until one blow lands (dodges don't reach @hit); what @getHit saw, -1 if none landed.</summary>
		private static double LandOne(Character attacker, Character defender) {
			getHitSaw = -1;
			setTheBlow = true;
			for (int i = 0; (i < 300) && (getHitSaw < 0); i++) {
				DamageManager.ProcessSwing(attacker, defender);
			}
			setTheBlow = false;
			return getHitSaw;
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
			CrushedResistsTimer.Apply(prey, 50, 60); //#1070 - weaker but longer: waits its turn
			Report("aWeakerArrowWaitsItsTurn", prey.ResistSlashing == 400);
			var records = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<double, int>>();
			bool first = CrushedResistsTimer.AddRecord(records, 10, 100);
			bool weakerShorter = CrushedResistsTimer.AddRecord(records, 5, 50);
			bool weakerLonger = CrushedResistsTimer.AddRecord(records, 20, 50);
			bool strongerLonger = CrushedResistsTimer.AddRecord(records, 15, 200);
			Report("arrowRecordsAsErebor", first && !weakerShorter && weakerLonger && strongerLonger && (records.Count == 2) &&
				(records[0].Value == 200) && (records[1].Value == 50));

			//#1070 - a light crossbow takes jagged bolts only, and bleeds with them
			Item heldBow = player.FindLayer(LayerNames.Hand2) as Item;
			if (heldBow != null) {
				heldBow.Delete();
			}
			Item crossbow = (Item) ((ItemDef) ThingDef.GetByDefname("i_crossbow_copper")).Create(player);
			Item plainBolts = (Item) ((ItemDef) ThingDef.GetByDefname("i_xbolt_copper")).Create(player.Backpack);
			player.InvalidateCombatWeaponValues();
			bool plainRefused = player.WeaponProjectile == null;
			Item jagged = (Item) ((ItemDef) ThingDef.GetByDefname("i_bolt_jagged_copper")).Create(player.Backpack);
			jagged.Amount = 10;
			player.InvalidateCombatWeaponValues();
			Report("aLightCrossbowTakesJaggedBoltsOnly", plainRefused && (player.WeaponProjectile == jagged) &&
				(((ProjectileDef) jagged.Def).BleedCoefficient == 600));
			ClassAbilities.SetPoints(player, "bleed_bolt", 100);
			NPC bleeder = Dummy(self, 3);
			RangerAbilities.TryBleedBolt(player, bleeder, new WeaponSwingArgs(player, bleeder, 0, 0, 0, 100));
			Report("aJaggedBoltBleeds", BleedingEffectPluginDef.IsBleeding(bleeder));
			ClassAbilities.SetPoints(player, "crushingArrow", 10);
			player.SetRealSkillValue((int) SkillName.Archery, 1000);
			RangerAbilities.FireCrushingArrow(player, bleeder);
			Report("aCrushingArrowUsesABolt", jagged.Amount == 9);
			bleeder.Delete();
			crossbow.Delete();
			plainBolts.Delete();
			jagged.Delete();

			ClassAbilities.SetPoints(player, "killingSpree", 10);
			TestRangerPlugin(player); //#1431 - leaves two charges, from a kill of the spree's own target
			SpreeTimer spree = (SpreeTimer) player.GetTimer(SpreeTimer.Key);
			Report("killsBuildASpree", (spree != null) && (spree.charges == 2));
			Report("aSpreeShowsItsIcon", player.HasOpenedDialog(SingletonScript<D_ChargeIcon>.Instance)); //#1070
			ClassAbilities.SetPoints(player, "splitshot", 10);
			RangerProfessionPlugin rangerPlugin = RangerProfessionPlugin.Of(player);
			if (rangerPlugin != null) { //theRangersPluginIsItsProfessionPlugin reports it otherwise
				rangerPlugin.nextSplitShot = 0;
			}
			player.Stam = 200;
			RangerAbilities.SplitShot(player);
			Report("aSplitShotShowsItsIcon", player.HasOpenedDialog(SingletonScript<D_SplitShotIcon>.Instance));
			spree.Drop(player);
			spree.Drop(player);
			Report("theLastChargeClosesTheIcon", !player.HasOpenedDialog(SingletonScript<D_ChargeIcon>.Instance) &&
				player.HasOpenedDialog(SingletonScript<D_SplitShotIcon>.Instance));
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
			PriestProfessionPlugin priest = ProfessionPlugin.GetInstalledPlugin(player) as PriestProfessionPlugin; //#1435
			Report("thePriestsPluginIsItsProfessionPlugin", priest != null);
			bool failureWaits = false;
			if (priest != null) { //no points: fails 1000 times in 1001, and waits the failure's 75 s
				ClassAbilities.SetPoints(player, "selfres", 0);
				PriestAbilities.Resurrection(player);
				failureWaits = !player.Flag_Dead || (Math.Abs(priest.nextSelfRes - (Globals.TimeInSeconds + ClassAbilitySettings.instance.selfResFailSeconds)) < 1);
				ClassAbilities.SetPoints(player, "selfres", 20);
				priest.nextSelfRes = 0;
			}
			for (int i = 0; (i < 200) && player.Flag_Dead && (priest != null); i++) { //90% a try at 20 points, each failure's delay wiped
				PriestAbilities.Resurrection(player);
				if (player.Flag_Dead) {
					priest.nextSelfRes = 0;
				}
			}
			Report("thePriestRises", !player.Flag_Dead);
			Report("risesAtHalfHits", player.Hits == (player.MaxHits / 2));
			Report("theCandleBurnsOut", PriestAbilities.FindLitCandle(player) == null);
			Report("selfResCooldownIsThePlugins", failureWaits && (priest != null) &&
				(priest.nextSelfRes > Globals.TimeInSeconds + ClassAbilitySettings.instance.selfResSuccessSeconds - 60)); //rising set the long one
			Console.WriteLine("PRIEST_DONE");
		}

		/// <summary>#1431 - the ranger's state is its profession plugin's; the killing spree adds to a blow through
		/// @swingLanding (also in a real swing, before @hit) and gains its charges through @kill: one a kill, two when the
		/// spree's own target dies. 10 points: a charge adds 5 + 10 percent.</summary>
		private static void TestRangerPlugin(Player player) {
			RangerProfessionPlugin ranger = ProfessionPlugin.GetInstalledPlugin(player) as RangerProfessionPlugin;
			Report("theRangersPluginIsItsProfessionPlugin", ranger != null);
			if (ranger == null) {
				return;
			}
			Report("rangerCooldownsAreThePlugins", (ranger.nextKnockback > Globals.TimeInSeconds) && (ranger.nextSplitShot > Globals.TimeInSeconds));
			player.InvalidateCombatWeaponValues(); //the crossbow was deleted: Character.Weapon is a cache a scripted delete leaves stale

			NPC orc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(orc, 300);
			orc.AddTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
			player.AddTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
			try {
				getHitSaw = -1;
				setTheBlow = true;
				for (int i = 0; (i < 300) && (getHitSaw < 0); i++) {
					player.DeleteTimer(SpreeTimer.Key); //one charge for each try: a dodged swing uses one up too
					SpreeTimer.Add(player);
					DamageManager.ProcessSwing(player, orc);
				}
				setTheBlow = false;
				Report("theSpreeMeetsABlowInTheSwing", Math.Abs(getHitSaw - 1150) < 0.001);
			} finally {
				setTheBlow = false;
				orc.RemoveTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
				player.RemoveTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
				orc.Delete();
				player.DeleteTimer(SpreeTimer.Key);
			}

			NPC first = Dummy(player, 2), second = Dummy(player, 3);
			first.CauseDeath(player);
			SpreeTimer spree = player.GetTimer(SpreeTimer.Key) as SpreeTimer;
			Report("aKillChargesTheSpreeThroughTheTrigger", (spree != null) && (spree.charges == 1));

			WeaponSwingArgs swing = DamageManager.GetWeaponSwingArgs(player, second);
			swing.DamageAfterAC = 1000;
			DamageManager.Trigger_SwingLanding(swing);
			Report("theSpreeAddsThroughTheTrigger", (Math.Abs(swing.DamageAfterAC - 1150) < 0.001) && (ranger.spreeTarget == second) &&
				!player.HasTimer(SpreeTimer.Key));
			second.CauseDeath(player); //the spree's own target: two charges
		}

		/// <summary>#1436 - a backstab whose target is deleted before its 0.1 s timer: the timer must still free the
		/// thief to stab again. Step one readies the stab and deletes the target; the protocol side waits, then
		/// TestBackstabTargetGoneCheck reads the mark.</summary>
		[SteamFunction]
		public static void TestBackstabTargetGone(Character self) {
			Player player = (Player) self;
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(player);
			if (thief == null) {
				Report("aStabIsUnderWay", false);
				Console.WriteLine("STAB_GONE_SET");
				return;
			}
			thief.stabbing = false;
			NPC target = Dummy(self, 1);
			ThiefAbilities.StabAt(player, target);
			Report("aStabIsUnderWay", thief.stabbing && player.HasTimer(BackstabTimer.Key));
			target.Delete();
			Console.WriteLine("STAB_GONE_SET");
		}

		[SteamFunction]
		public static void TestBackstabTargetGoneCheck(Character self) {
			ThiefProfessionPlugin thief = ThiefProfessionPlugin.Of(self);
			Report("aVanishedTargetFreesTheBackstab", (thief != null) && !thief.stabbing && !self.HasTimer(BackstabTimer.Key));
			Console.WriteLine("STAB_GONE_DONE");
		}

		/// <summary>#1433 - e_thief's @AfterSwing in a real swing, before @hit: a fencing blow from behind with a readied
		/// throat slit, 10 points of each: the probe's 1000, +100 permille from behind, +40% for the slit, and the slit spent.</summary>
		private static void TestThiefBlowInASwing(Player player, ThiefProfessionPlugin thief, NPC victim) {
			ClassAbilities.SetPoints(player, "utok_do_zad", 10);
			ClassAbilities.SetPoints(player, "podriznuti", 10);
			thief.throatReady = true;
			NPCAiTimer.Freeze(victim, 300);
			victim.Direction = Direction.East; //looking away from the thief, who stands to its west
			victim.AddTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
			player.AddTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
			try {
				getHitSaw = -1;
				setTheBlow = true;
				DamageManager.ProcessSwing(player, victim); //a player's blow isn't dodged
				Report("fromBehindWithASlitInTheSwing", (Math.Abs(getHitSaw - 1540) < 0.001) && !thief.throatReady);
				Console.WriteLine("CLASS_ABILITY_INFO: thiefGetHitSaw=" + getHitSaw);
			} finally {
				setTheBlow = false;
				ClassAbilities.SetPoints(player, "utok_do_zad", 0); //the war fork's check later must see no blow from behind
				victim.RemoveTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
				player.RemoveTriggerGroup(SingletonScript<E_SwingLandingProbe>.Instance);
			}
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
			ProfessionPlugin plugin = ProfessionPlugin.GetInstalledPlugin(player); //#1438
			Report("theCounterIsTheProfessionPlugins", (plugin != null) && plugin.counterOn);
			player.TryTrigger(TriggerKey.Acquire("death"), new ScriptArgs((Character) null)); //@death alone, nobody dies
			Report("deathTakesTheCounter", !ClassAbilities.CounterOn(player));
			player.Stam = player.MaxStam;
			CraftsmanAbilities.Vigour(player);
			bool onBefore = ClassAbilities.CounterOn(player);
			player.Profession = Professions.Craftsman; //set again: a fresh plugin
			Report("aNewProfessionDropsTheCounter", onBefore && !ClassAbilities.CounterOn(player));
			player.Stam = player.MaxStam;
			CraftsmanAbilities.Vigour(player); //on again, for the check below that turns it off

			//#427 - the elven probe: near it ebony turns elven and an elven tree has nothing, for craftsmen only
			CraftsmanAbilities.Vigour(player); //the counter off, or vigour could move the ebony up first
			Report("theCounterIsOffAgain", !ClassAbilities.CounterOn(player));
			FlaggedRegion grove = null;
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				if ((region is FlaggedRegion area) && area.HasElvenGrove) {
					player.Go(new Point4D(area.ElvenGroveX, area.ElvenGroveY, 0, area.Mapplane));
					if (RegionEventSystem.AreaOf(player.Region) == area) {
						grove = area;
						break;
					}
				}
			}
			Report("anAreaHoldsTheProbe", grove != null);
			if (grove != null) {
				Point2D first = ElvenGrove.SpotOf(grove);
				Report("theProbeWandersRoundItsPoint", (Math.Abs(first.X - grove.ElvenGroveX) <= ElvenGrove.Wander) && (Math.Abs(first.Y - grove.ElvenGroveY) <= ElvenGrove.Wander));
				player.SetRealSkillValue((int) SkillName.Lumberjacking, ElvenGrove.RollMax + 1);
				byte m = player.M;
				int x = player.X + 1, y = player.Y;
				ElvenGrove.PlaceForTest(grove, new Point2D(player.X, player.Y));
				LumberjackingVeinSystem.ForceVeinForTest(m, x, y, WoodTiers.Eben, 5);
				Report("ebonyTurnsElvenNearTheProbe", LumberjackingVeinSystem.Chop(player, m, x, y) == WoodTiers.Elven.ItemDef);
				Report("oneElvenLogOnly", LumberjackingVeinSystem.Chop(player, m, x, y) == null);
				LumberjackingVeinSystem.ForceVeinForTest(m, x, y, WoodTiers.Elven, 5);
				Report("anElvenTreeHasNothingThere", LumberjackingVeinSystem.Chop(player, m, x, y) == null);
				player.Profession = Professions.Warrior;
				LumberjackingVeinSystem.ForceVeinForTest(m, x, y, WoodTiers.Elven, 5);
				Report("onlyCraftsmenMeetTheProbe", LumberjackingVeinSystem.Chop(player, m, x, y) == WoodTiers.Elven.ItemDef);
				player.Profession = Professions.Craftsman;
				ElvenGrove.PlaceForTest(grove, new Point2D(player.X + ElvenGrove.Reach, player.Y));
				LumberjackingVeinSystem.ForceVeinForTest(m, x, y, WoodTiers.Eben, 5);
				Report("farFromTheProbeEbonyStays", LumberjackingVeinSystem.Chop(player, m, x, y) == WoodTiers.Eben.ItemDef);
			}
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
			ThiefProfessionPlugin thief = ProfessionPlugin.GetInstalledPlugin(player) as ThiefProfessionPlugin; //#1433
			Report("theThiefsPluginIsItsProfessionPlugin", thief != null);
			if (thief == null) {
				Console.WriteLine("THIEF_DONE");
				return;
			}
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
			Report("thiefStateIsThePlugins", thief.throatReady && (thief.nextThroat > Globals.TimeInSeconds) && thief.shieldReady &&
				(thief.nextShield > Globals.TimeInSeconds));

			foreach (SkillName skill in new[] { SkillName.Fencing, SkillName.Tactics, SkillName.Anatomy }) {
				player.SetRealSkillValue((int) skill, 1000);
			}
			((ItemDef) ThingDef.GetByDefname("i_kryss")).Create(player);
			double weapAttack = CombatSettings.instance.weapAttack;
			CombatSettings.instance.weapAttack = weapAttack * 100;
			ClassAbilities.SetPoints(player, "backstab", 30);
			HidingSkillDef.Hide(player);
			Report("hidingIsNotedOnThePlugin", Math.Abs(thief.hiddenAt - Globals.TimeInSeconds) < 1); //#1433
			thief.hiddenAt = Globals.TimeInSeconds - 10;
			short before = victim.Hits;
			for (int i = 0; (i < 20) && (victim.Hits == before); i++) { //30 points: 105% a try against a monster
				player.Stam = 200;
				ThiefAbilities.FinishStab(player, victim);
			}
			CombatSettings.instance.weapAttack = weapAttack;
			Report("aBackstabHurts", victim.Hits < before);
			Report("aBackstabReveals", !player.Flag_Hidden);
			TestThiefBlowInASwing(player, thief, victim);
			victim.Delete();

			ThiefMask.ClearMask(player);
			player.Realm = Realm.Gondor;
			string realName = player.Name;
			ThiefMask.SetAttributes(player, "Stinovy", -5000, 3, Realm.Mordor);
			ThiefMask.Swap(player, ThiefMask.Attributes(player));
			Report("maskChangesNameAndRealm", (player.Name == "Stinovy") && (player.Realm == Realm.Mordor) && (Notoriety.GetMurderCount(player) == 3));
			ThiefMask.Swap(player, ThiefMask.Attributes(player));
			Report("maskSwapsBack", (player.Name == realName) && (player.Realm == Realm.Gondor));

			//#1070 - the mask's leftovers
			string suggested = NameList.Random("NAMES_HUMANMALE");
			Report("theMaskSuggestsAName", !string.IsNullOrEmpty(suggested) && (NameList.Resolve("#NAMES_HUMANFEMALE") != "#NAMES_HUMANFEMALE"));
			Character viewer = (Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(player.X + 2, player.Y, player.Z, player.M);
			viewer.Realm = Realm.Mordor;
			Report("rentalInfoShowsTheOtherIdentity", (ThiefMask.RealName(player, viewer) == "Stinovy") && (ThiefMask.SeenRealmHue(player, viewer) == 0x26) &&
				(ThiefMask.RealName(player, player) == realName));
			viewer.Delete();
			((SpellDef) SpellDef.GetByDefname("s_incognito")).ApplyEffectChar(player, player, 1000, EffectFlag.FromAbility | EffectFlag.BeneficialEffect);
			string incognitoName = player.Name;
			ThiefMask.Swap(player, ThiefMask.Attributes(player));
			bool runeRenamed = (player.Name == incognitoName) && (IncognitoSpellDef.TrueName(player) == "Stinovy");
			IncognitoSpellDef.End(player);
			Report("underIncognitoTheRuneIsRenamed", runeRenamed && (player.Name == "Stinovy"));
			ThiefMask.Swap(player, ThiefMask.Attributes(player));
			player.Realm = Realm.None;
			NPC mark = Dummy(self, 3);
			WeaponSkillTargetQueuePlugin.AddTarget(player, mark);
			Item knives = (Item) ((ItemDef) ThingDef.GetByDefname("i_kudla")).Create(player.Backpack);
			knives.Amount = 20;
			ClassAbilities.SetPoints(player, "throwing", 10);
			short markHits = mark.Hits;
			for (int i = 0; (i < 10) && (mark.Hits == markHits); i++) {
				player.Stam = 200;
				thief.nextThrow = 0;
				ThiefAbilities.ThrowKnife(player);
			}
			Report("aKnifeHitsTheMonster", mark.Hits == markHits - 1);
			Report("knivesAreUsedUp", knives.IsDeleted || (knives.Amount < 20));
			ThiefDefence.SetMagicDefence(mark, 0, 0, 100, 0); //#1070 - magic ranged reflex, then block
			markHits = mark.Hits;
			player.Stam = 200;
			thief.nextThrow = 0;
			ThiefAbilities.ThrowKnife(player);
			Report("aMagicReflexSendsTheKnifeBack", (mark.Hits == markHits) && (player.Stam == ((200 - (3 * ClassAbilitySettings.instance.throwStaminaPerTile)) * 4) / 5));
			ThiefDefence.SetMagicDefence(mark, 0, 0, 0, 100);
			thief.nextThrow = 0;
			ThiefAbilities.ThrowKnife(player);
			Report("aMagicBlockStopsIt", mark.Hits == markHits);
			ThiefDefence.SetMagicDefence(mark, 0, 0, 0, 0);
			PoisonedItemPlugin.Bind(knives, PoisonKind.AbsoluteDamage, "test", 50, 0); //#1070 - the knife's own poison
			for (int i = 0; (i < 10) && !Poison.IsPoisoned(mark); i++) {
				player.Stam = 200;
				thief.nextThrow = 0;
				ThiefAbilities.ThrowKnife(player);
			}
			Report("aPoisonedKnifePoisons", Poison.IsPoisoned(mark));
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

			//#1070 - the rest of e_thief's @AfterSwing
			Item held = player.FindLayer(LayerNames.Hand1) as Item;
			if (held != null) {
				held.Delete();
			}
			Item fork = (Item) ((ItemDef) ThingDef.GetByDefname("i_war_fork")).Create(player);
			ClassAbilities.SetPoints(player, "doubleswing", 200);
			player.Stam = 200;
			thief.throatReady = true;
			NPC forked = Dummy(self, 1);
			WeaponSwingArgs forkSwing = new WeaponSwingArgs(player, forked, 0, 0, 0, 100);
			DamageManager.Trigger_SwingLanding(forkSwing); //#1433 - e_thief's @AfterSwing is the plugin's @swingLanding
			Report("aWarForkSwingsTwice", (forkSwing.DamageAfterAC == 150) && (player.Stam == 200 - ClassAbilitySettings.instance.doubleSwingStamina));
			Report("aFencingBlowSpendsTheSlit", !thief.throatReady);
			forked.Delete();
			fork.Delete();
			ClassAbilities.SetPoints(player, "doubleswing", 0);
			Console.WriteLine("THIEF_DONE");
		}

		[SteamFunction]
		public static void TestNecroAbilities(Character self) {
			Player player = (Player) self;
			Report("undeadHitsByPoints", (NecroAbilities.UndeadHits(100, 10) == 120) && (NecroAbilities.UndeadHits(100, 40) == 135) && (NecroAbilities.UndeadHits(100, 0) == 100));
			player.Profession = Professions.Necromant;
			player.Level = 100;
			player.Stam = player.MaxStam;
			ClassAbilities.OnDeath(player); //the other classes' leftovers; a counter goes with each new profession plugin (#1438)
			NecroAbilities.Darkpower(player);
			Report("darkpowerTurnsTheCounterOn", ClassAbilities.CounterOn(player));
			player.MaxMana = 200;
			player.Mana = 0;
			SpellDef fireball = SpellDef.GetByDefname("s_fireball");
			for (int i = 0; (i < 400) && (player.Mana == 0); i++) {
				CastDone(player, fireball, false); //#1440 - @spellSuccess, as Magery raises it
			}
			Report("darkpowerGivesManaBack", player.Mana == (fireball.ManaUse * 850) / 1000);
			player.Mana = 0;
			for (int i = 0; (i < 400) && (player.Mana == 0); i++) {
				CastDone(player, fireball, true);
			}
			Report("aScrollGivesHalfBack", player.Mana == ((fireball.ManaUse * 850) / 1000) / 2);
			player.Mana = 0;
			CastDone(player, SpellDef.GetByDefname("s_recall"), false);
			Report("notForRecall", player.Mana == 0);
			player.Profession = Professions.Thief; //another class this character has been, its own counter on
			player.Stam = player.MaxStam;
			ThiefAbilities.Ripper(player);
			player.Mana = 0;
			for (int i = 0; i < 100; i++) {
				CastDone(player, fireball, false);
			}
			Report("noManaBackForOtherClasses", ClassAbilities.CounterOn(player) && (player.Mana == 0));
			player.Profession = Professions.Necromant;
			Console.WriteLine("NECRO_DONE");
		}

		/// <summary>Magery's @spellSuccess after a cast, without the cast.</summary>
		private static void CastDone(Player player, SpellDef spell, bool fromScroll) {
			player.TryTrigger(TriggerKey.Acquire("spellSuccess"), new ScriptArgs(new SpellSuccessArgs(player, spell, null, fromScroll)));
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

	/// <summary>#1429 - a probe on both: the attacker's @swingLanding sets the blow, the defender's @getHit reads it.</summary>
	public class E_SwingLandingProbe : CompiledTriggerGroup {
		public void On_SwingLanding(Character self, WeaponSwingArgs swingArgs) {
			if (ClassAbilityTests.setTheBlow) {
				swingArgs.DamageAfterAC = 1000;
			}
		}

		public void On_GetHit(Character self, WeaponSwingArgs swingArgs) {
			if (ClassAbilityTests.setTheBlow) {
				ClassAbilityTests.getHitSaw = swingArgs.DamageAfterAC;
				swingArgs.DamageAfterAC = 0;
			}
		}
	}
}
