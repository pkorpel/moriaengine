using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - the mystic: controlnpc, catch, catchField, buffSteal, hypnosis.</summary>
	public static class MysticAbilityTests {
		private static Player Prepare(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			player.Profession = Professions.Mystic;
			player.Level = 100;
			player.Int = 300;
			player.Vit = 200;
			player.Hits = player.MaxHits;
			player.Mana = player.MaxMana;
			player.Stam = player.MaxStam;
			ClassAbilities.Reset(player);
			ClassAbilities.OnDeath(player);
			if (CatchPlugin.Of(player) is CatchPlugin leftHold) { //#1455 - a run aborted part-way on a reused character
				MysticMind.ReleaseCatch(player, leftHold, false);
			}
			if (CatchFieldPlugin.Of(player) is CatchFieldPlugin leftField) {
				MysticMind.EndCatchField(player, leftField);
			}
			if (!MysticMind.HasBall(player)) {
				((ItemDef) ThingDef.GetByDefname(MysticMind.BallDef)).Create(player.Backpack);
			}
			return player;
		}

		[SteamFunction]
		public static void TestMysticAbilities(Character self) {
			Player player = Prepare(self);
			Report("theBallIsItsOwnThing", MysticHypnosis.FindBall(player) != null);

			ClassAbilities.SetPoints(player, "controlnpc", 20);
			NPC wild = ClassAbilityTests.Dummy(self, 2);
			MysticMind.ControlTarget(player, wild);
			Report("onlyPetsAndSummonsObey", ControlNpcPlugin.Of(player) == null);
			SummonSpellDef.MarkAsSummonedCreature(wild);
			MysticMind.ControlTarget(player, wild);
			ControlNpcPlugin control = ControlNpcPlugin.Of(player); //#1455 - i_controlnpc
			Report("aSummonCanBeTaken", (control != null) && (control.target == wild) && (control.Timer > 4) && (control.Timer <= 5));
			ClassAbilities.SetPoints(player, "controlnpc", 20);
			if (control != null) {
				control.TryRun(TriggerKey.Acquire("timer"), null, out object scripted, out object compiled); //its own @timer, as PluginTimer runs it
			}
			Report("itObeysTheMystic", (wild.ControlMaster == player) && (ControlNpcPlugin.Of(player) == null));
			wild.Delete();

			ClassAbilities.SetPoints(player, "catch", 10);
			NPC held = ClassAbilityTests.Dummy(self, 2);
			held.ExperienceReward = 1000;
			for (int i = 0; (i < 60) && (CatchPlugin.Of(player) == null); i++) {
				MysticMind.CatchTarget(player, held);
			}
			CatchPlugin catching = CatchPlugin.Of(player); //#1455 - i_catchitem
			Report("catchTakesHold", catching != null);
			if (catching != null) {
				MysticMind.CatchTick(player, catching);
				Report("bothStandStill", !MysticMind.CheckCanMove(held) && !MysticMind.CheckCanMove(player));
				int mana = player.Mana;
				MysticMind.CatchTick(player, catching);
				Report("holdingDrainsMana", player.Mana == mana - MysticMind.CatchDrain(1000, 100, 1));
				MysticMind.Catch(player);
				Report("catchAgainLetsGo", MysticMind.CheckCanMove(held) && MysticMind.CheckCanMove(player) && (CatchPlugin.Of(player) == null));
				int hits = player.Hits;
				for (int i = 0; (i < 60) && (player.Hits == hits); i++) {
					MysticMind.CatchTarget(player, held);
				}
				Report("aSecondCatchHurts", (player.Hits < hits) && (CatchPlugin.Of(player) == null));
			}
			held.Delete();

			ClassAbilities.SetPoints(player, "catch", 20);
			NPC near = ClassAbilityTests.Dummy(self, 2);
			player.Mana = player.MaxMana;
			player.Stam = player.MaxStam;
			MysticMind.CatchField(player);
			CatchFieldPlugin field = CatchFieldPlugin.Of(player); //#1455 - i_memory_catchField
			CaughtPlugin caught = CaughtPlugin.Of(near); //e_catchedInField
			Report("theFieldHoldsThoseAround", (field != null) && field.held.Contains(near) && (caught != null) && (caught.mystic == player) && ParalyzeEffectPluginDef.IsParalyzed(near));
			Report("theFieldIsTheMysticsPlugin", (MysticProfessionPlugin.Of(player) is MysticProfessionPlugin mystic) && (mystic.nextCatchField > Globals.TimeInSeconds + 3000));
			DamageManager.Trigger_GetSwingLanding(DamageManager.GetWeaponSwingArgs(player, near)); //#1455 - a blow at it, as ProcessSwing raises it
			Report("aBlowFreesOne", (CaughtPlugin.Of(near) == null) && (field != null) && !field.held.Contains(near) && MysticMind.CheckCanMove(near) && !ParalyzeEffectPluginDef.IsParalyzed(near));
			MysticMind.CatchField(player);
			Report("theFieldEndsAgain", (CatchFieldPlugin.Of(player) == null) && MysticMind.CheckCanMove(player));
			player.Mana = player.MaxMana;
			MysticMind.CatchField(player);
			Report("theFieldCoolsDownAnHour", CatchFieldPlugin.Of(player) == null);
			near.Delete();

			MysticProfessionPlugin cooldowns = MysticProfessionPlugin.Of(player); //#1455 - a field ends with its holder's death
			if (cooldowns != null) {
				cooldowns.nextCatchField = 0;
			}
			NPC second = ClassAbilityTests.Dummy(self, 2);
			player.Mana = player.MaxMana;
			player.Stam = player.MaxStam;
			for (int i = 0; (i < 40) && (CatchFieldPlugin.Of(player) == null); i++) {
				if (cooldowns != null) {
					cooldowns.nextCatchField = 0;
				}
				player.Mana = player.MaxMana;
				player.Stam = player.MaxStam;
				MysticMind.CatchField(player);
			}
			bool heldBefore = CaughtPlugin.Of(second) != null;
			player.TryTrigger(TriggerKey.Acquire("death"), new ScriptArgs((Character) null)); //@death alone, nobody dies
			Report("deathEndsTheField", heldBefore && (CatchFieldPlugin.Of(player) == null) && (CaughtPlugin.Of(second) == null) && MysticMind.CheckCanMove(player));
			second.Delete();

			ClassAbilities.SetPoints(player, "buffSteal", 20);
			NPC robbed = ClassAbilityTests.Dummy(self, 2);
			NPCDef def = (NPCDef) robbed.Def;
			int flags = def.Hypnotizable;
			def.Hypnotizable = 0x8 | 0x400000; //strength, power 1
			short str = player.Str;
			BuffStealPlugin stealing = null;
			for (int i = 0; (i < 30) && (BuffStolenPlugin.Of(robbed) == null); i++) {
				player.DeletePlugin(BuffStealPlugin.Key);
				player.Stam = player.MaxStam;
				MysticMind.BuffStealTarget(player, robbed);
				stealing = BuffStealPlugin.Of(player); //#1457 - i_buffSteal
				if (stealing != null) {
					stealing.TryRun(TriggerKey.Acquire("timer"), null, out object s0, out object c0); //its own @timer: the wind-up ends
				}
			}
			BuffStolenPlugin stolen = BuffStolenPlugin.Of(robbed); //i_buffStealNPC
			Report("aTalentIsStolen", (player.Str == str + 2) && (stolen != null) && (stolen.cut == 20) && (stealing != null) && (stealing.state == 1) && (stealing.Timer > 59));

			int cutBlow = 0; //#1457 - e_npcChangeDmg's @AfterSwing: the robbed monster's blow loses its cut
			WeaponSwingArgs monsterBlow = DamageManager.GetWeaponSwingArgs(robbed, player);
			monsterBlow.DamageAfterAC = 100;
			DamageManager.Trigger_SwingLanding(monsterBlow);
			cutBlow = (int) monsterBlow.DamageAfterAC;
			Report("theRobbedMonsterHitsLess", cutBlow == 80);

			if (stealing != null) {
				stealing.TryRun(TriggerKey.Acquire("timer"), null, out object s1, out object c1); //the 60 s end
			}
			Report("andGivenBack", (player.Str == str) && (stealing != null) && (stealing.state == 2) && (BuffStealPlugin.Of(player) == stealing));

			if (stealing != null) { //an attack talent: the bonus on blows and spells against monsters
				stealing.type = 4;
				stealing.state = 1;
				stealing.success = true;
				stealing.bonus = 5;
			}
			WeaponSwingArgs mysticBlow = DamageManager.GetWeaponSwingArgs(player, robbed);
			mysticBlow.DamageAfterAC = 100;
			DamageManager.Trigger_SwingLanding(mysticBlow);
			SpellPowerArgs power = new SpellPowerArgs(player, (SpellDef) SpellDef.GetByDefname("s_magic_arrow"), robbed, robbed, 1000);
			player.TryTrigger(TriggerKey.Acquire("spellPower"), new ScriptArgs(power));
			Report("aStolenAttackAddsToBlowsAndSpells", ((int) mysticBlow.DamageAfterAC == 105) && (power.SpellPower == 1050));

			if (stealing != null) { //#1457 - e_mystik's @death consumes i_buffSteal; a held Strength goes back
				stealing.type = 2;
				stealing.bonus = 0;
				stealing.changed = 2;
				stealing.state = 1;
				player.Str = (short) (str + 2);
			}
			player.TryTrigger(TriggerKey.Acquire("death"), new ScriptArgs((Character) null)); //@death alone, nobody dies
			Report("deathEndsTheStealAndGivesItBack", (BuffStealPlugin.Of(player) == null) && (player.Str == str));

			def.Hypnotizable = flags;
			robbed.Delete();

			TestLightningPastReflection(player); //#1509

			MysticHypnosis.FindBall(player).On_DClick(player);
			Console.WriteLine("MYSTIC_DONE");
		}

		/// <summary>#1509 - the special strike's lightning goes straight to the effect, as Erebor's dospell_final: a monster
		/// holding a magic reflection is struck and keeps its reflection; nothing comes back at the mystic.</summary>
		private static void TestLightningPastReflection(Player player) {
			NPC monster = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(player.X + 1, player.Y, player.Z, player.M);
			NPCAiTimer.Freeze(monster, 300);
			try {
				monster.MaxHits = 30000;
				monster.Hits = 30000;
				SpellDef.GetByDefname("s_magic_reflection").ApplyEffectChar(monster, monster, 700, EffectFlag.FromSpellBook | EffectFlag.BeneficialEffect);
				bool reflecting = MagicReflectionPluginDef.HasMagicReflection(monster);
				player.Hits = player.MaxHits;
				MysticAbilities.StrikeWithLightning(player, monster);
				Console.WriteLine("MYSTIC_LIGHTNING: reflecting=" + reflecting + " monsterHits=" + monster.Hits + " stillReflecting="
					+ MagicReflectionPluginDef.HasMagicReflection(monster) + " mysticHurt=" + (player.Hits < player.MaxHits));
				Report("theLightningPassesAReflection", reflecting && (monster.Hits < 30000) && MagicReflectionPluginDef.HasMagicReflection(monster)
					&& (player.Hits == player.MaxHits));
			} finally {
				monster.Delete();
				player.Hits = player.MaxHits;
			}
		}

		[SteamFunction]
		public static void TestMysticHypnosis(Character self) {
			Player player = Prepare(self);
			NPC body = ClassAbilityTests.Dummy(self, 3);
			NPCDef def = (NPCDef) body.Def;
			int flags = def.Hypnotizable;
			def.Hypnotizable = MysticMind.FlagCanBe;
			body.ExperienceReward = 100;
			body.MaxHits = 300;
			body.Hits = 300;
			Item shirt = (Item) ((ItemDef) ThingDef.GetByDefname("i_shirt_plain")).Create(player);
			int x = player.X, y = player.Y, model = player.Model, experience = player.Experience;
			string name = player.Name;
			int bodyX = body.X;

			MysticHypnosis.Targeted(player, body);
			ConcentrationPlugin concentration = ConcentrationPlugin.Of(player); //#1461 - i_hypno_timer
			Report("concentrationStarts", (concentration != null) && (concentration.monster == body) && (concentration.Timer > 0)); //1 to 14 s, read a moment after it's set
			player.TryTrigger(TriggerKey.stepTaken, new ScriptArgs(false)); //@stepTaken, as a real step raises it
			Report("aStepBreaksIt", ConcentrationPlugin.Of(player) == null);
			MysticHypnosis.Targeted(player, body);
			ConcentrationPlugin second = ConcentrationPlugin.Of(player);
			DamageManager.Trigger_GetSwingLanding(DamageManager.GetWeaponSwingArgs(body, player)); //a blow at the mystic
			Report("aBlowBreaksIt", (second != null) && (ConcentrationPlugin.Of(player) == null));
			int bodyExp = body.ExperienceReward;
			body.ExperienceReward = 0;
			bool indifferent = !MysticHypnosis.Succeeds(player, body); //"zcela netecne": 2 s before another try
			body.ExperienceReward = bodyExp;
			MysticProfessionPlugin mystic = MysticProfessionPlugin.Of(player);
			Report("theDelayIsTheMysticsPlugin", indifferent && (mystic != null) && (mystic.nextHypnosis > Globals.TimeInSeconds + 1));
			MysticHypnosis.Targeted(player, body);
			Report("theDelayRefusesAnotherTry", ConcentrationPlugin.Of(player) == null);
			if (mystic != null) {
				mystic.nextHypnosis = 0;
			}

			player.Hits = 180;
			MysticHypnosis.Swap(player, body);
			PossessionPlugin state = MysticHypnosis.Possession(player); //#1467 - i_hypno_delka
			NPC ego = (state == null) ? null : state.ego;
			AlterEgoPlugin alterEgo = AlterEgoPlugin.Of(ego);
			Report("thePossessionIsAPlugin", (state != null) && (state.monster == body) && (state.Timer > 0));
			Report("theEgoIsAPlugin", (alterEgo != null) && (alterEgo.mystic == player) && (alterEgo.Timer > 0));
			Report("theMysticIsTheMonster", (state != null) && (player.Model == body.Model) && (player.X == bodyX) && (player.Name == body.Name));
			Report("theMonsterWaitsAway", body.M == MysticHypnosis.ParkedPlane);
			Report("anAlterEgoStandsInPlace", (ego != null) && (ego.X == x) && (ego.Y == y) && (ego.Name == name) && (ego.Model == model));
			Report("theGearWaitsInTheBank", shirt.TopObj() == player && (shirt.Cont is Container));
			Report("itFightsAsTheMonster", !player.IsPlayerForCombat && (player.DefForCombat == body.TypeDef));
			Boosters.Clear(player); //#1467 - a running booster: the possession counts the base, bonused once at home
			Boosters.Add(player, 0);
			bool boosted = Boosters.Use(player, Boosters.Stored(player).FindIndex(row => row.Key == 0));
			LevelingSystem.AddExperience(player, 100);
			Report("experienceWaits", player.Experience == experience);
			Report("onlyTheBaseIsCounted", boosted && (state != null) && (state.gained == 100));
			Boosters.Clear(player);

			//#1070 - what the world makes of a possessing mystic
			NPC guard = (NPC) ThingDef.GetByDefname("c_vguard_mordor_hala_jezdec").Create((ushort) (player.X + 4), player.Y, player.Z, player.M);
			guard.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			Realm realm = player.Realm;
			player.Realm = Realm.Gondor;
			short karma = body.Karma;
			body.Karma = 0;
			Report("guardsLetAMildBodyAlone", !GuardSpeech.AttacksOnSight(guard, player) && !GuardSpeech.SendAfter(guard, player));
			body.Karma = -1000;
			Report("guardsGoForAnEvilBody", GuardSpeech.AttacksOnSight(guard, player));
			body.Karma = karma;
			guard.Delete();
			NPC monster = (NPC) ThingDef.GetByDefname("c_ogre").Create((ushort) (player.X + 4), player.Y, player.Z, player.M);
			monster.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			Report("monstersGoForIt", monster.ShouldAggroOn(player) && MysticHypnosis.MonsterGoesFor(monster, player));
			HighlightColor shown = Notoriety.GetHighlightColor(monster, player);
			Report("itShowsAsTheMonster", (shown == Notoriety.GetHighlightColor(monster, body)) && (shown != HighlightColor.Innocent));
			player.Realm = realm;
			monster.Delete();
			Report("theMenusRefuse", MysticHypnosis.RefusesMenu(player));
			Report("noGuildInTheName", Guild.NameWithAbbreviation(player) == player.Name);
			VarGate gate = (VarGate) ((ItemDef) ThingDef.GetByDefname("i_vargate")).Create((ushort) (player.X + 1), player.Y, player.Z, player.M);
			Report("aGateRefusesIt", !gate.CanPass(player));
			gate.Flags = VarGateFlags.HypnoGoThrough;
			Report("unlessTheGateLetsHypnosisThrough", gate.CanPass(player));
			gate.Delete();
			ClassAbilities.SetPoints(player, "speldodge", 100);
			Report("noAbsorptionWhilePossessing", !ClassAbilities.TryAbsorbSpell(player, body));
			ClassAbilities.SetPoints(player, "speldodge", 0);

			DamageManager.Trigger_GetSwingLanding(DamageManager.GetWeaponSwingArgs(player, ego)); //#1467 - its @getSwingLanding
			Report("aBlowOnTheEgoBringsItHome", !MysticHypnosis.IsPossessing(player) && (player.Model == model) && (player.X == x) && (player.Name == name) && ego.IsDeleted);
			Report("withHalfItsHits", player.Hits == 90);
			Report("theGearIsBack", shirt.Cont == player);
			Report("theMonsterIsBack", (body.M != MysticHypnosis.ParkedPlane) && (body.ExperienceReward == 200));
			Report("experienceIsPaid", player.Experience != experience);

			player.Hits = 100;
			MysticHypnosis.Swap(player, body);
			player.CauseDeath(null);
			Report("dyingInTheBodyBringsItHome", !player.Flag_Dead && !MysticHypnosis.IsPossessing(player) && (player.Hits == 20));

			MysticHypnosis.Swap(player, body);
			ego = MysticHypnosis.Possession(player).ego;
			ego.CauseDeath(null);
			Report("theEgosDeathBringsItHomeHurt", !MysticHypnosis.IsPossessing(player) && (player.Hits == 1) && ego.IsDeleted);

			player.Hits = 100; //#1467 - a spell on the ego, through its @spellEffect
			MysticHypnosis.Swap(player, body);
			ego = MysticHypnosis.Possession(player).ego;
			SpellDef heal = (SpellDef) SpellDef.GetByDefname("s_heal");
			heal.Trigger_EffectChar(ego, SpellEffectArgs.Acquire(player, ego, ego, heal, 100, EffectFlag.BeneficialEffect));
			Report("aSpellOnTheEgoBringsItHome", !MysticHypnosis.IsPossessing(player) && (player.Hits == 50) && ego.IsDeleted);

			MysticHypnosis.Swap(player, body); //#1467 - @logout
			ego = MysticHypnosis.Possession(player).ego;
			player.TryTrigger(TriggerKey.logout, null);
			Report("aLogoutBringsItHome", !MysticHypnosis.IsPossessing(player) && (player.Name == name) && ego.IsDeleted);

			//#1070 - speldodge on a player at home: a monster's spell is swallowed, a player's never
			ClassAbilities.SetPoints(player, "speldodge", 100);
			Report("aMonstersSpellIsAbsorbed", ClassAbilities.TryAbsorbSpell(player, body) && !ClassAbilities.TryAbsorbSpell(player, player));
			ClassAbilities.SetPoints(player, "speldodge", 0);
			Report("noPointsNoAbsorption", !ClassAbilities.TryAbsorbSpell(player, body));

			def.Hypnotizable = flags;
			body.Delete();
			shirt.Delete();
			player.Hits = player.MaxHits;
			Console.WriteLine("HYPNOSIS_DONE");
		}

		private static void Report(string name, bool ok) {
			ClassAbilityTests.Report(name, ok);
		}
	}
}
