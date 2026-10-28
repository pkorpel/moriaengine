using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// First-ever Magery/spellcasting protocol tests (#483/#484) - zero test coverage existed
	/// for any spell before this. Covers Phase 0's own fix: SpellDef.Difficulty used to return 0
	/// for almost every spell (only DamageSpellDef content ever set it explicitly), so the real,
	/// correctly-wired fizzle mechanism never actually fired once mana was sufficient. Account is
	/// this collection's own first-ever login (auto-elevated to GM) for the end-to-end pipeline
	/// smoke test; AccountB (via EnsureAutoAccountCreationEnabled) is a normal, non-elevated,
	/// no-profession account for the real fizzle-rate regression test - SkillDef.CheckSuccess has
	/// its own "GM is always successful" bypass that would otherwise mask the fix entirely.
	/// </summary>
	[Collection(MagerySpellCollection.Name)]
	public class MagerySpellTests {
		private const string Account = "magerytest";
		private const string AccountB = "magerytest2";
		// #488 - Account was already at its own hard cap (AbstractAccount.maxCharactersPerGameAccount
		// = 5: AutoAccountEnabler/MagePipelineChecker/MageSummonChecker/MageDispelBuffChecker/
		// MageDispelSummonChecker) and AccountB at 4/5 before this phase. A 6th character-creation
		// attempt on an already-full account makes AbstractCharacter.MakeBePlayer throw
		// SEException("account is already full") server-side, unhandled - which doesn't just fail
		// that one test, it wedges the whole shared single-threaded server for every OTHER test
		// still to run in this fixture (confirmed: all 3 new Phase 4 tests passed individually in
		// isolation, but crashed everything after them - including an untouched pre-existing
		// Phase 1 test - when run together in the shared collection). New tests needing a fresh
		// character go on this 3rd account instead of overloading Account/AccountB further.
		private const string AccountC = "magerytest3";
		// #489 - same account-slot-cap lesson as AccountC's own comment above, applied again: by
		// the end of Phase 4, AccountB sat at 4/5 and AccountC at 3/5 - between them only 3 free
		// slots remained, not enough for Phase 5's 5 new character-creating Facts
		// (EnergyBoltDamageTypeChecker/GateTravelDurationChecker/ParalyzeFieldChecker fit in
		// those 3; Invisibility/Reveal's checkers go on this 4th account instead of pushing
		// either existing account over its cap).
		private const string AccountD = "magerytest4";
		private const string Password = "magerytest";

		private readonly GameServerPairFixture fixture;

		public MagerySpellTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>#484 - the real regression test. Before the fix, GetDifficulty would return
		/// 0 for any spell outside damageSpells.def (the only file that sets difficulty=
		/// explicitly) - meaning every sample below would succeed regardless of skill (100%
		/// success at skill 0 is the actual old-bug symptom). After the fix, difficulty comes
		/// from Requirements=X Magery instead. Uses s_poison_field specifically (Requirements=
		/// 80.0, the largest non-damage-spell requirement in this engine's content) rather than
		/// a smaller one - SkillUtils.GetBellCurve's own math (skillCheckVariance=250) means a
		/// small requirement like 20 only shifts the chance a few percent off 50%, within normal
		/// 300-sample binomial noise; 80 gives a clean, safely-separated ~42%-vs-50% signal.
		/// Expected at skill=0/difficulty=80: GetBellCurve(80) = 500-(250*80/250) = 420 -> 42%.
		/// Expected at skill=1000/difficulty=80: valDiff=920, three /2 halvings down to chance 62,
		/// then 1000-(62-(31*170/250)) = 1000-41 = 959 -> 95.9%.</summary>
		[Fact]
		public void FizzleRateVariesWithSkillOnceDifficultyFallbackIsReal() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageFizzleChecker")) {
				int difficulty = this.ReportDifficulty(client, "s_poison_field");
				Assert.True(difficulty > 0,
					"Expected s_poison_field's fallback difficulty to be > 0 now that it's derived " +
					"from Requirements=X Magery instead of a hardcoded 0.");

				int lowSkillSuccesses = this.SampleFizzleRate(client, "s_poison_field", skillValue: 0, sampleCount: 300);
				int highSkillSuccesses = this.SampleFizzleRate(client, "s_poison_field", skillValue: 1000, sampleCount: 300);

				Assert.True(lowSkillSuccesses < 170,
					"Expected skill=0 to fail noticeably more than half the time against a real " +
					"difficulty (" + difficulty + ", expected ~42% success). Got " + lowSkillSuccesses + "/300 successes.");
				Assert.True(highSkillSuccesses > 250,
					"Expected skill=1000 (max) to succeed almost always against difficulty " +
					difficulty + " (expected ~96% success). Got " + highSkillSuccesses + "/300 successes.");
			}
		}

		/// <summary>#487 - Cunning's CASTTIME was 8 (full seconds) - far outside every sibling
		/// buff spell's 1.0-2.0s range, a near-certain typo. Fixed to 1.5s, matching Strength
		/// (same shape/flags/effect).</summary>
		[Fact]
		public void CunningCastTimeIsFixedNotTheOldTypo() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageCunningCastTimeChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSpellCastTime s_Cunning");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_CASTTIME_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_CASTTIME_TEST:");
				Assert.Contains("castTime=1.5", line);
			}
		}

		/// <summary>#487 - Harm's DamageType was MagicAcid, unverified and unusual (classic Harm
		/// is cold-based, and Erebor's source doesn't rescript it). Fixed to MagicCold, matching
		/// the classic/faithful value instead of the unbacked moriaengine-only choice.</summary>
		[Fact]
		public void HarmDamageTypeIsFixedNotTheUnbackedAcidChoice() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageHarmDamageTypeChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSpellDamageType s_harm");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DAMAGETYPE_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_DAMAGETYPE_TEST:");
				Assert.Contains("damageType=MagicCold", line);
			}
		}

		/// <summary>#489 - Energy Bolt's DamageType was MagicFire. Erebor's Energy Bolt is
		/// lightning-based (dam_magic|dam_lightning in source), not fire. Fixed to
		/// MagicElectric.</summary>
		[Fact]
		public void EnergyBoltDamageTypeIsFixedToElectricNotFire() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageEnergyBoltDamageTypeChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSpellDamageType s_energy_bolt");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DAMAGETYPE_TEST:", mark, TimeSpan.FromSeconds(10)),
					"Output since command:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_DAMAGETYPE_TEST:");
				Assert.Contains("damageType=MagicElectric", line);
			}
		}

		/// <summary>#489 - Gate Travel's conjured-gate DURATION was 3,4 (3-4 SECONDS) while the
		/// spell header's own comment says "one minute should suffice" - a unit bug. Fixed to a
		/// flat 60s,60s.</summary>
		[Fact]
		public void GateTravelDurationIsFixedToOneMinuteNotSeconds() {
			this.EnsureAutoAccountCreationEnabled();
			// AccountC, not AccountB - AccountB already had 4 characters before Phase 5
			// (FizzleChecker/CunningCastTimeChecker/HarmDamageTypeChecker/FizzleEvenChecker)
			// and EnergyBoltDamageTypeIsFixedToElectricNotFire above already took the 5th
			// (last) slot - putting this one on AccountB too would have been a 6th
			// character, over the account's 5-character-slot hard cap (confirmed directly:
			// reproduced reliably as a WaitForGameOutputSince timeout once both ran
			// together, the same silent-server-wedge class Phase 4's own account-cap
			// lesson already documented).
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "MageGateTravelDurationChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSpellDuration s_gate_travel");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DURATION_TEST:", mark, TimeSpan.FromSeconds(10)),
					"Output since command:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_DURATION_TEST:");
				Assert.Contains("min=60", line);
				Assert.Contains("max=60", line);
			}
		}

		/// <summary>#489 - Paralyze Field was a dead stub before this (old pre-port flag tokens
		/// that parse to nothing, no ItemDef at all - casting it crashed). Now a real field: a
		/// dummy spawned AFTER the field already exists, exactly on the field's own center tile
		/// (see TestCastParalyzeFieldNearSelf's own doc comment for why cast-then-spawn order
		/// matters), actually gets paralyzed after the field's first tick - reusing Circle V's
		/// already-shipped ParalyzeEffectPlugin.</summary>
		[Fact]
		public void ParalyzeFieldActuallyParalyzesACharacterStandingInIt() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "MageParalyzeFieldChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				// The default spawn point's surrounding terrain is too cramped/irregular for
				// SpellEffectItem.CheckPositionForItem to ever place a real (height>0) item
				// anywhere nearby (confirmed directly: canFit=False at every one of the 5
				// candidate tiles, at both distance=3 and distance=15 along the same axis an
				// already-working ground-targeted character-spawn test uses successfully -
				// NPC/character placement and item placement go through entirely different
				// fit-checking logic, so "a creature can be summoned there" doesn't imply "an
				// item can be placed there"). TestTeleportToOpenGround (#78's own established
				// helper) finds real open ground with no static items within 7 tiles first.
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastParalyzeFieldNearSelf");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_PARAFIELD_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));
				string startLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_PARAFIELD_TEST: STARTED");
				int targetX = ExtractInt(startLine, "targetX");
				int targetY = ExtractInt(startLine, "targetY");
				int targetZ = ExtractInt(startLine, "targetZ");
				int targetM = ExtractInt(startLine, "targetM");

				Thread.Sleep(9000); // CASTTIME is 5.0s; generous margin (matching this file's own
				// ~2x-CASTTIME convention elsewhere) for the field item to definitely exist
				// before the dummy spawns - too little margin here would race the field's own
				// creation, and if the dummy already occupied the target tile by the time the
				// field item actually gets created, CheckPositionForItem's occupancy check would
				// skip placing an item on that exact tile (the original bug this spawn-after-
				// cast ordering was designed to avoid in the first place).

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnDummyAt " + targetX + " " + targetY + " " + targetZ + " " + targetM);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SPAWN_TEST:", mark, TimeSpan.FromSeconds(10)));
				uint dummyUid = ExtractMarkedUid(LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SPAWN_TEST:"), "dummyUid=");

				Thread.Sleep(3000); // field's own tick fires every paralyzeFieldTickIntervalSeconds
				// (default 1.0s) relative to the field's own creation time, not the dummy's spawn
				// time - generous margin covers at least one full tick cycle after the dummy
				// exists, regardless of where in that cycle the dummy happened to spawn.

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportIsParalyzed " + dummyUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_PARALYZE_TEST:", mark, TimeSpan.FromSeconds(10)));
				string resultLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_PARALYZE_TEST:");
				Assert.True(resultLine.Contains("paralyzed=True"), "FULL:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		/// <summary>#489 - Invisibility: Character.Flag_InvisByMagic already existed and was
		/// already wired into IsNotVisible/CanSeeVisibility, but nothing set it anywhere before
		/// this. Casts the test-only s_test_invisibility (flat 3s duration) and confirms the flag
		/// goes true immediately, then false again once the duration expires.</summary>
		[Fact]
		public void InvisibilitySetsAndClearsFlagInvisByMagic() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "MageInvisibilityChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));
				uint selfUid = SelfUidOf(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastInvisibilityOnSelf");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_INVIS_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(3500); // CASTTIME is 1.0s; wide margin for the stroke delay -
				// widened after an intermittent flake under sandbox load (same class of
				// full-boot-tier timing contention already documented throughout this
				// repo's CLAUDE.md), confirmed not a logic bug by re-running clean
				// immediately after with no code change

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportVisibilityFlags " + selfUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_VISIBILITY_TEST:", mark, TimeSpan.FromSeconds(10)));
				string duringLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_VISIBILITY_TEST:");
				Assert.True(duringLine.Contains("invisByMagic=True"), "FULL:\n" + this.fixture.GameOutputSnapshot());

				Thread.Sleep(3000); // DURATION is a flat 3s; 2s elapsed already above, this covers the rest plus margin

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportVisibilityFlags " + selfUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_VISIBILITY_TEST:", mark, TimeSpan.FromSeconds(10)));
				string afterLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_VISIBILITY_TEST:");
				Assert.Contains("invisByMagic=False", afterLine);
			}
		}

		/// <summary>#489 - Reveal: unconditionally clears Flag_InvisByMagic (no skill contest -
		/// classic Reveal strips magical invisibility outright) and, for a Flag_Hidden target,
		/// only clears it after winning a Magery-vs-Hiding skill contest. AccountC's caster has
		/// max Magery (via TestGrantFullSpellbook) against a Hiding-0 dummy, so the contest is as
		/// close to deterministic as SkillUtils' bell curve allows (same bell-curve shape already
		/// confirmed elsewhere in this file - skill 1000 vs. difficulty 0 lands far into the
		/// near-certain-success tail).</summary>
		[Fact]
		public void RevealClearsInvisibilityAlwaysAndHidingOnSkillContestSuccess() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "MageRevealChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastRevealNearSelf");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_REVEAL_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));
				string startLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_REVEAL_TEST: STARTED");
				uint hiddenUid = ExtractMarkedUid(startLine, "hiddenUid=");
				uint invisUid = ExtractMarkedUid(startLine, "invisUid=");

				Thread.Sleep(5000); // CASTTIME is 3.0s; generous margin for the stroke + area-effect dispatch

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportVisibilityFlags " + invisUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_VISIBILITY_TEST:", mark, TimeSpan.FromSeconds(10)));
				string invisLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_VISIBILITY_TEST:");
				Assert.Contains("invisByMagic=False", invisLine);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportVisibilityFlags " + hiddenUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_VISIBILITY_TEST:", mark, TimeSpan.FromSeconds(10)));
				string hiddenLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_VISIBILITY_TEST:");
				Assert.Contains("hidden=False", hiddenLine);
			}
		}

		/// <summary>Sanity check that the Phase 0 fix doesn't swing the other way either - a
		/// caster exactly AT the spell's own stated skill requirement should land close to the
		/// documented ~50% (GetSCurve's own 0-difference midpoint), not near 0% or 100%.</summary>
		[Fact]
		public void FizzleRateIsRoughlyEvenAtExactlyTheSkillRequirement() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageFizzleEvenChecker")) {
				int difficulty = this.ReportDifficulty(client, "s_poison_field");
				int successes = this.SampleFizzleRate(client, "s_poison_field", skillValue: difficulty, sampleCount: 500);

				Assert.True((successes > 150) && (successes < 350),
					"Expected roughly a coin-flip (allowing wide margin for the bell-curve shape, " +
					"not a strict 50%) at skill == difficulty (" + difficulty + "). Got " + successes + "/500.");
			}
		}

		/// <summary>End-to-end sanity check that SpellDef.cs's Difficulty change didn't break the
		/// real casting pipeline - a real damage spell cast from a real spellbook at a real
		/// target still consumes mana and lands damage. Runs on the GM account (CheckSuccess's
		/// own GM-always-succeeds bypass is actually desirable here, since this test isn't about
		/// fizzle at all, just "does casting still work end to end").</summary>
		[Fact]
		public void DamageSpellStillLandsDamageAndConsumesMana() {
			// Account is this collection's first-ever login (full LScript parser) - spell
			// defnames are registered AbstractScripts, so an unquoted bareword like
			// s_magic_arrow gets silently resolved to the live SpellDef's own ToString(), not
			// kept as literal text (the same established bareword-resolution footgun as
			// elsewhere this session) - quote it.
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MagePipelineChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastSpellAtDummy \"s_magic_arrow\"");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_CAST_STARTED:", mark, TimeSpan.FromSeconds(10)),
					"Output since cast command:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
				string startLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_CAST_STARTED:");
				uint dummyUid = ExtractMarkedUid(startLine, "dummyUid=");
				int startMana = ExtractInt(startLine, "startMana");
				int startHits = ExtractInt(startLine, "startHits");

				Thread.Sleep(5000); // s_magic_arrow's CASTTIME is 2.5s; generous margin for the stroke delay too

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportCastResult " + dummyUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_CAST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string resultLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_CAST_RESULT:");

				int casterMana = ExtractInt(resultLine, "casterMana");
				Assert.True(casterMana < startMana,
					"Expected mana to be spent casting s_magic_arrow. startMana=" + startMana + " line: " + resultLine);

				bool targetDeleted = resultLine.Contains("targetDeleted=True");
				if (!targetDeleted) {
					int targetHits = ExtractInt(resultLine, "targetHits");
					Assert.True(targetHits < startHits,
						"Expected s_magic_arrow to have damaged the dummy. startHits=" + startHits + " line: " + resultLine);
				}
			}
		}

		/// <summary>#485 - the generic summon-pet infrastructure: casting the test-only
		/// s_test_summon spell produces a real NPC, controlled by the caster, non-aggressive,
		/// tagged as a summoned creature - and it actually despawns once its 5-second duration
		/// elapses (not just a decoration that sits there forever).</summary>
		[Fact]
		public void SummonedCreatureAppearsControlledThenDespawns() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MageSummonChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportMyUid");
				Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
				uint selfUid = ExtractMarkedUid(LastLineContaining(this.fixture.GameOutputSnapshot(), "MY_UID:"), "MY_UID: ");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastSummonSpell");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(2500); // CASTTIME is 1.0s; generous margin for the stroke + effect dispatch

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(foundLine.Contains("FOUND"), "Expected the summon to exist shortly after casting. line: " + foundLine);
				Assert.Contains("controlMasterUid=" + selfUid, foundLine);
				Assert.Contains("warMode=False", foundLine);

				Thread.Sleep(9000); // duration is a flat 10s; 2.5s elapsed already above, this covers the rest plus margin

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string goneLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(goneLine.Contains("NONE"), "Expected the summon to have despawned after its duration. line: " + goneLine);
			}
		}

		/// <summary>#486 - Character.On_Dispell used to be an empty no-op, so casting Dispel on
		/// a buffed character removed nothing. Casts a real beneficial buff (s_bless) on self,
		/// confirms a dispellable plugin is really there, casts Dispel on self, confirms it's
		/// gone.</summary>
		[Fact]
		public void DispelRemovesABuffFromACharacter() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MageDispelBuffChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastBuffOnSelf");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: BUFF_CAST_STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(2500); // s_bless CASTTIME is 2.0s; generous margin

				int beforeCount = this.ReportDispellablePluginCount(client);
				Assert.True(beforeCount > 0, "Expected a real dispellable plugin after casting Bless on self.");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastDispelOnTarget " + SelfUidOf(client));
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: DISPEL_CAST_STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(3500); // s_dispel CASTTIME is 2.5s; generous margin

				int afterCount = this.ReportDispellablePluginCount(client);
				Assert.True(afterCount < beforeCount,
					"Expected Dispel to remove the Bless plugin. beforeCount=" + beforeCount + " afterCount=" + afterCount);
			}
		}

		/// <summary>#486 - a summoned creature (#485) is tagged specifically so Character.
		/// On_Dispell's fix can remove the whole creature, not just a buff on it. Uses a longer-
		/// lived summon (TestCastSummonSpell's own 5s duration is enough margin) and dispels it
		/// well before it would naturally despawn.</summary>
		[Fact]
		public void DispelRemovesASummonedCreature() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MageDispelSummonChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastSummonSpell");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(2500);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(foundLine.Contains("FOUND"), "Expected the summon to exist. line: " + foundLine);
				uint summonUid = ExtractMarkedUid(foundLine, "uid=");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastDispelOnTarget " + summonUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: DISPEL_CAST_STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(3500); // well before the summon's own 5s despawn timer would fire anyway

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportCastResult " + summonUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_CAST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string resultLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_CAST_RESULT:");
				Assert.Contains("targetDeleted=True", resultLine);
			}
		}

		/// <summary>#488 - Mana Drain reduces the target's mana and does NOT transfer any of it
		/// to the caster (the distinguishing feature vs. Circle VII's not-yet-built Mana
		/// Vampire, which does transfer). The caster's own mana should only ever go down (by the
		/// spell's own MANAUSE cast cost), never up.</summary>
		[Fact]
		public void ManaDrainReducesTargetManaWithoutTransferringToCaster() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "MageManaDrainChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastManaDrainAtDummy");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_MANADRAIN_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));
				string startLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_MANADRAIN_TEST: STARTED");
				uint dummyUid = ExtractMarkedUid(startLine, "dummyUid=");
				int startCasterMana = ExtractInt(startLine, "startCasterMana");
				int startTargetMana = ExtractInt(startLine, "startTargetMana");

				Thread.Sleep(4500); // s_mana_drain's CASTTIME is 2.0s; generous margin for the stroke delay too

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportManaDrainResult " + dummyUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_MANADRAIN_TEST: RESULT", mark, TimeSpan.FromSeconds(10)));
				string resultLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_MANADRAIN_TEST: RESULT");
				int casterMana = ExtractInt(resultLine, "casterMana");
				int targetMana = ExtractInt(resultLine, "targetMana");

				Assert.True(targetMana < startTargetMana,
					"Expected s_mana_drain to reduce the target's mana. startTargetMana=" + startTargetMana + " line: " + resultLine);
				Assert.True(casterMana <= startCasterMana,
					"Expected casting Mana Drain to never INCREASE the caster's own mana (no transfer, unlike Mana Vampire). " +
					"startCasterMana=" + startCasterMana + " line: " + resultLine);
			}
		}

		/// <summary>#488 - Blade Spirits summons a creature with NO ControlMaster (unlike Summon
		/// Creature below) - the "popular kill-all" the old pre-port script comment warned
		/// about, not a leashed pet.</summary>
		[Fact]
		public void BladeSpiritsSummonsAnUncontrolledCreature() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "MageBladeSpiritsChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				// AccountC is non-elevated (SimpleCommandParser) - unlike Account's GM/full-LScript
				// parser, quoting a bareword here is wrong, not just unnecessary: SimpleCommandParser
				// doesn't strip quote characters, so a quoted defname fails SpellDef.GetByDefname,
				// leaving Param1 null and crashing MagerySkillDef.On_Select's SpellBook.HasSpell with
				// a NullReferenceException (confirmed directly - silent, logged server-side, the cast
				// just never starts). See TestCastSpellAtDummy for the opposite (GM/Account) case.
				//
				// distance=17 (beyond NpcAggroSettings.instance.aggroRange, default 14 - was
				// AggroRange=8 and distance=15 before that setting shipped, #762) is deliberate,
				// not arbitrary - a Hostile summon with nobody else around WILL autonomously
				// engage the only nearby character it can find (its own caster, absent any other
				// target), and an actively-engaged NPC hanging the whole server on later cleanup
				// was confirmed directly (see TestNeutralizeCharacterByUid's own doc comment) -
				// keeping it out of aggro range entirely avoids that whole class of problem
				// instead of trying to recover from it after the fact.
				//
				// NOTE (#762): this exact Fact currently fails regardless of distance - confirmed
				// via a clean isolated worktree at unmodified main (commit 8504712, before any
				// of this change) that it ALSO fails there with "MAGERY_SUMMON_TEST: NONE",
				// same symptom. Pre-existing, unrelated to this change - not root-caused here
				// (filed as its own issue). An earlier theory blaming Globals.MaxUpdateRange=18
				// (the real client vision-range ceiling CanInteractWith enforces for ground-
				// targeted casts) was disproven the same way - distance=15 (the original,
				// once-passing value) fails identically to distance=25, so the real cause is
				// something else entirely.
				client.SendSpeech(".TestCastGroundTargetedSpellNearSelf s_blade_spirits 17");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_GROUNDCAST_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(4000); // CASTTIME is 2.5s; generous margin for the stroke + effect dispatch

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(foundLine.Contains("FOUND"), "Expected Blade Spirits to summon a creature. line: " + foundLine);
				Assert.Contains("controlMasterUid=null", foundLine);
				Assert.Contains("warMode=False", foundLine); // out of aggro range - confirms it never engaged

				uint summonUid = ExtractMarkedUid(foundLine, "uid=");
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestNeutralizeCharacterByUid " + summonUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_NEUTRALIZE_TEST:", mark, TimeSpan.FromSeconds(10)));
				string cleanupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_NEUTRALIZE_TEST:");
				Assert.Contains("neutralized=True", cleanupLine);
			}
		}

		/// <summary>#488 - Summon Creature summons a real controlled pet (ControlMaster set to
		/// the caster) from the random mage-tier animal pool, unlike Blade Spirits above.</summary>
		[Fact]
		public void SummonCreatureSummonsAControlledPet() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "MageSummonCreatureChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				uint selfUid = SelfUidOf(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastGroundTargetedSpellNearSelf s_summon_creature"); // non-elevated AccountC, see BladeSpirits test's own comment above
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_GROUNDCAST_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(4000);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(foundLine.Contains("FOUND"), "Expected Summon Creature to summon a pet. line: " + foundLine);
				Assert.Contains("controlMasterUid=" + selfUid, foundLine);
			}
		}

		/// <summary>#490 - Circle VIII's 6 summon spells, each wired to Phase 1's generic
		/// SummonSpellDef against its own real mage-tier chardef (summons.def) - confirmed
		/// correct NPCDef via the reported defname, and confirmed a normal controlled pet
		/// (ControlMaster set, unlike Blade Spirits' Hostile=true shape). All 6 share one
		/// character/connection (AccountD's 3rd and last available slot - see this file's
		/// account-budget comments elsewhere; Account/AccountB/AccountC are all already at their
		/// 5-character cap) and TestCleanupNearbySummons deletes each summon before the next
		/// cast so TestReportNearbySummon's "first match in range" scan can't see a stale one
		/// from an earlier iteration - safe because these are idle, non-hostile, never-engaged
		/// pets (the server-hang hazard is specific to actively-engaged NPCs, confirmed
		/// elsewhere in this file, and doesn't apply to a pet with no ControlMaster order and no
		/// autonomous aggro).</summary>
		[Fact]
		public void AllSixCircleEightSummonsAppearAsControlledPetsWithCorrectChardef() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "MageCircleEightSummonsChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));
				uint selfUid = SelfUidOf(client);

				(string defname, string expectedChardef)[] summons = new[] {
					("s_energy_vortex", "c_vortex_mage"),
					("s_summon_elem_air", "c_elem_air_m"),
					("s_summon_elem_earth", "c_elem_earth_m"),
					("s_summon_elem_fire", "c_elem_fire_m"),
					("s_summon_elem_water", "c_elem_water_m"),
					("s_summon_daemon", "c_daemon_mage1"),
				};

				for (int i = 0; i < summons.Length; i++) {
					(string defname, string expectedChardef) = summons[i];
					int mark = this.fixture.GameOutputSnapshot().Length;
					// AccountD is non-elevated (SimpleCommandParser), same as AccountC's own
					// summon tests above - unquoted defname is correct here, not optional.
					// distance = 2 + i: a distinct target tile per iteration (not the same spot
					// every time) - avoids any possible leftover-tile-state ambiguity from the
					// previous iteration's just-deleted summon.
					client.SendSpeech(".TestCastGroundTargetedSpellNearSelf " + defname + " " + (2 + i));
					Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_GROUNDCAST_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

					Thread.Sleep(9000); // CASTTIME is 4.0s for every Circle VIII summon; wide margin - confirmed via two
					// different intermittent failures (at two different iteration indices across two runs,
					// never the same one twice) under this sandbox's heavy background dotnet-process load that
					// 6s wasn't consistently enough, same class of full-boot-tier timing contention already
					// documented throughout this repo's CLAUDE.md - not a logic bug.

					mark = this.fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestReportNearbySummon");
					Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
					string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
					Assert.True(foundLine.Contains("FOUND"), "Expected " + defname + " to summon a creature. line: " + foundLine);
					Assert.Contains("controlMasterUid=" + selfUid, foundLine);

					mark = this.fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestReportNearbySummonMatchesDefname " + expectedChardef);
					Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_DEFNAME_TEST:", mark, TimeSpan.FromSeconds(10)));
					string defnameLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_DEFNAME_TEST:");
					Assert.Contains("matches=True", defnameLine);

					mark = this.fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestCleanupNearbySummons");
					Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_CLEANUP_TEST: DONE", mark, TimeSpan.FromSeconds(10)));
				}
			}
		}

		private int ReportDispellablePluginCount(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportDispellablePluginCount");
			Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: dispellablePluginCount=", mark, TimeSpan.FromSeconds(10)));
			return ExtractInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "dispellablePluginCount="), "dispellablePluginCount");
		}

		private uint SelfUidOf(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportMyUid");
			Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
			return ExtractMarkedUid(LastLineContaining(this.fixture.GameOutputSnapshot(), "MY_UID:"), "MY_UID: ");
		}

		private int ReportDifficulty(TestGameClient client, string spellDefname) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportSpellDifficulty " + spellDefname);
			Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DIFFICULTY_TEST:", mark, TimeSpan.FromSeconds(10)));
			return ExtractInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_DIFFICULTY_TEST:"), "difficulty");
		}

		private int SampleFizzleRate(TestGameClient client, string spellDefname, int skillValue, int sampleCount) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestSampleFizzleRate " + spellDefname + " " + skillValue + " " + sampleCount);
			Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_FIZZLE_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
			return ExtractInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_FIZZLE_SAMPLE:"), "successes");
		}

		private static int ExtractInt(string line, string key) {
			string marker = key + "=";
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while ((end < line.Length) && (char.IsDigit(line[end]) || (line[end] == '-'))) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}

		private static uint ExtractMarkedUid(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while ((end < line.Length) && char.IsDigit(line[end])) {
				end++;
			}
			return uint.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}

		// Guards against the established "EnsureAutoAccountCreationEnabled creates a brand new
		// character every call" trap (AttrFlagTests.cs/ProfessionTests.cs hit this too) - each
		// call otherwise burns one of Account's 5 character slots, and this file has 12 Facts
		// that call it (count grows - update when adding more). Safe under this collection's
		// DisableParallelization=true.
		private static bool autoAccountCreationEnabled;

		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			autoAccountCreationEnabled = true;
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));

				// #489 - a double[]-typed FieldValue (Duration/Effect) is lazily resolved on
				// first access by running its stored expression as a real LScript snippet
				// (FieldValue.ResolveSingleValue -> LScriptMain.RunSnippet), which gates itself
				// behind Commands.AuthorizeCommandThrow the FIRST time this happens inside any
				// player command's execution - and that check fails for a non-GM commandSrc
				// (confirmed directly: GateTravelDurationIsFixedToOneMinuteNotSeconds threw "No
				// permission to run that command" when s_gate_travel's never-yet-resolved
				// Duration field was read from AccountB). The resolved value is cached on the
				// SpellDef singleton forever after, so "warming" it once here under GM (this
				// account's own auto-elevation) on this already-open connection - no new
				// character slot spent - makes every later non-GM read a cache hit that skips
				// the gated lazy path entirely. Pre-existing engine quirk, not fixed at the
				// root here (out of scope for a Duration-unit-bug fix) - this is a documented
				// workaround for this test suite specifically. Quoted defname - this runs on
				// Account's own elevated/full-LScript-parser session, where an unquoted bareword
				// like s_gate_travel gets silently resolved to the live SpellDef's own ToString()
				// instead of staying literal text (the same established bareword-resolution
				// footgun as TestCastSpellAtDummy's own doc comment describes).
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSpellDuration \"s_gate_travel\"");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DURATION_TEST:", mark, TimeSpan.FromSeconds(10)),
					"Output since command:\n" + this.fixture.GameOutputSnapshot().Substring(mark));

				// Same lazy-resolve mechanism as s_gate_travel above - warm
				// s_test_invisibility's own Duration field too (InvisibilitySpellDef.
				// On_EffectChar reads it via GetDurationForValue).
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSpellDuration \"s_test_invisibility\"");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DURATION_TEST:", mark, TimeSpan.FromSeconds(10)),
					"Output since command:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
			}
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);

						int serverCount = login.ReadServersList();
						if (serverCount < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}

						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						Assert.True(redirectPort > 0, "Expected a real port in the login redirect.");
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MagerySpellCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MagerySpellPair";
	}
}
