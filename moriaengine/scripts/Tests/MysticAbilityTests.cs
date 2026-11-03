using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

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
			Report("onlyPetsAndSummonsObey", !player.HasTimer(ControlNpcTimer.Key));
			SummonSpellDef.MarkAsSummonedCreature(wild);
			MysticMind.ControlTarget(player, wild);
			Report("aSummonCanBeTaken", player.HasTimer(ControlNpcTimer.Key));
			MysticMind.FinishControl(player, wild);
			Report("itObeysTheMystic", wild.ControlMaster == player);
			wild.Delete();

			ClassAbilities.SetPoints(player, "catch", 10);
			NPC held = ClassAbilityTests.Dummy(self, 2);
			held.ExperienceReward = 1000;
			for (int i = 0; (i < 60) && !player.HasTimer(CatchTimer.Key); i++) {
				MysticMind.CatchTarget(player, held);
			}
			CatchTimer catching = player.GetTimer(CatchTimer.Key) as CatchTimer;
			Report("catchTakesHold", catching != null);
			if (catching != null) {
				MysticMind.CatchTick(player, catching);
				Report("bothStandStill", !MysticMind.CheckCanMove(held) && !MysticMind.CheckCanMove(player));
				int mana = player.Mana;
				MysticMind.CatchTick(player, catching);
				Report("holdingDrainsMana", player.Mana == mana - MysticMind.CatchDrain(1000, 100, 1));
				MysticMind.Catch(player);
				Report("catchAgainLetsGo", MysticMind.CheckCanMove(held) && MysticMind.CheckCanMove(player) && !player.HasTimer(CatchTimer.Key));
				int hits = player.Hits;
				for (int i = 0; (i < 60) && (player.Hits == hits); i++) {
					MysticMind.CatchTarget(player, held);
				}
				Report("aSecondCatchHurts", (player.Hits < hits) && !player.HasTimer(CatchTimer.Key));
			}
			held.Delete();

			ClassAbilities.SetPoints(player, "catch", 20);
			NPC near = ClassAbilityTests.Dummy(self, 2);
			player.Mana = player.MaxMana;
			player.Stam = player.MaxStam;
			MysticMind.CatchField(player);
			Report("theFieldHoldsThoseAround", player.HasTimer(CatchFieldTimer.Key) && (near.GetTag(MysticMind.CatchedByTK) == player) && ParalyzeEffectPluginDef.IsParalyzed(near));
			MysticMind.OnSwungAt(near);
			Report("aBlowFreesOne", (near.GetTag(MysticMind.CatchedByTK) == null) && MysticMind.CheckCanMove(near) && !ParalyzeEffectPluginDef.IsParalyzed(near));
			MysticMind.CatchField(player);
			Report("theFieldEndsAgain", !player.HasTimer(CatchFieldTimer.Key) && MysticMind.CheckCanMove(player));
			player.Mana = player.MaxMana;
			MysticMind.CatchField(player);
			Report("theFieldCoolsDownAnHour", !player.HasTimer(CatchFieldTimer.Key));
			near.Delete();

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
