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
			BuffStealTimer stealing = null;
			for (int i = 0; (i < 30) && !robbed.HasTimer(BuffStolenTimer.Key); i++) {
				player.DeleteTimer(BuffStealTimer.Key);
				player.Stam = player.MaxStam;
				MysticMind.BuffStealTarget(player, robbed);
				stealing = player.GetTimer(BuffStealTimer.Key) as BuffStealTimer;
				if (stealing != null) {
					MysticMind.StealTick(player, stealing);
				}
			}
			Report("aTalentIsStolen", (player.Str == str + 2) && (robbed.GetTag(MysticMind.NpcDamageCutTK) is int cut) && (cut == 20));
			if (stealing != null) {
				MysticMind.Reverse(player, stealing);
			}
			Report("andGivenBack", player.Str == str);
			def.Hypnotizable = flags;
			robbed.Delete();
			player.DeleteTimer(BuffStealTimer.Key);

			MysticHypnosis.FindBall(player).On_DClick(player);
			Console.WriteLine("MYSTIC_DONE");
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
			Report("concentrationStarts", player.HasTimer(ConcentrationTimer.Key));
			MysticHypnosis.OnStep(player);
			Report("aStepBreaksIt", !player.HasTimer(ConcentrationTimer.Key));

			player.Hits = 180;
			MysticHypnosis.Swap(player, body);
			PossessionTimer state = MysticHypnosis.Possession(player);
			NPC ego = (state == null) ? null : state.ego;
			Report("theMysticIsTheMonster", (state != null) && (player.Model == body.Model) && (player.X == bodyX) && (player.Name == body.Name));
			Report("theMonsterWaitsAway", body.M == MysticHypnosis.ParkedPlane);
			Report("anAlterEgoStandsInPlace", (ego != null) && (ego.X == x) && (ego.Y == y) && (ego.Name == name) && (ego.Model == model));
			Report("theGearWaitsInTheBank", shirt.TopObj() == player && (shirt.Cont is Container));
			Report("itFightsAsTheMonster", !player.IsPlayerForCombat && (player.DefForCombat == body.TypeDef));
			LevelingSystem.AddExperience(player, 100);
			Report("experienceWaits", player.Experience == experience);

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

			MysticHypnosis.OnSwungAt(ego);
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
