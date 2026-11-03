using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1017 - server side in scripts/Tests/ClassAbilityTests.cs.</summary>
	[Collection(ClassAbilityCollection.Name)]
	public class ClassAbilityTests {
		private const string TestAccount = "protocoltest";
		private const int ManaRegenButton = 10, ClassInfoButton = 1;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		private static readonly string[] Checks = {
			"noLevelNoPoints", "sparePointsAreLevelTimesTwo", "notForThisClass", "offeredOnlyWhatTheClassHas", "level60ThiefDodgeStopsAt20",
			"level61ThiefDodgeGoesTo40", "spentPointsLeaveTheSpare", "anExistingAbilityGetsThePoints", "aMageKeepsToItsOwnLimit",
			"tooYoungForTheFirstStep", "oldEnoughForIt", "aTagAbilityGetsThePoint", "resetClearsEverything",
		};

		private readonly GameServerPairFixture fixture;

		public ClassAbilityTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void PointsAreSpentByClassAndLevelThroughTheDialogs() {
			using (TestGameClient game = this.Login("Abler")) {
				game.SendSpeech(".TestClassAbilityRules");
				Assert.True(this.fixture.WaitForGameOutput("CLASS_ABILITY_DONE", Wait), this.fixture.GameOutputSnapshot());
				string output = this.fixture.GameOutputSnapshot();
				foreach (string check in Checks) {
					string marker = "CLASS_ABILITY: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}

				game.SendSpeech(".explevel");
				var gump = game.ReadGumpDialogWithStrings(Wait);
				game.SendGumpResponse(gump.GumpUid, 3);
				gump = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Vedlejsi ability", gump.Strings);
				Assert.Contains("Bodu k rozdeleni: 20", gump.Strings);
				Assert.Contains("Regenerace many", gump.Strings);
				Assert.Contains("Omracujici uder", gump.Strings);
				Assert.DoesNotContain("Isn't implemented", gump.LayoutText, StringComparison.OrdinalIgnoreCase);

				game.SendGumpResponse(gump.GumpUid, ManaRegenButton);
				gump = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Bodu k rozdeleni: 19", gump.Strings);

				game.SendGumpResponse(gump.GumpUid, ClassInfoButton);
				gump = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Roundattack", gump.Strings);
				Assert.Contains(".warcry", gump.Strings);
			}
		}

		[Fact]
		public void WarriorAbilitiesFollowErebor() {
			using (TestGameClient game = this.Login("Warlord")) {
				game.SendSpeech(".TestWarriorAbilities");
				Assert.True(this.fixture.WaitForGameOutput("WARRIOR_DONE", Wait), this.fixture.GameOutputSnapshot());
				string output = this.fixture.GameOutputSnapshot();
				foreach (string check in new[] { "roundAttackHitsWhoIsNear", "roundAttackLeavesTheRest", "warcryTakesOwnMana", "stunWindowsShrink",
						"stunLimitGrowsWithTheMetal", "plainMaceStunsNothing", "shieldsByMetal", "stanceOnCostsHalfStamina", "stanceOff",
						"tauntTurnsItOnTheWarrior", "tauntingHardensIt", "aTauntedMonsterIsEvil", "leapNeedsAFight" }) {
					string marker = "CLASS_ABILITY: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
				this.TheShamanFollowsErebor(game);
			}
		}

		[Fact]
		public void RangerAbilitiesFollowErebor() {
			using (TestGameClient game = this.Login("Pathfinder")) {
				game.SendSpeech(".TestRangerAbilities");
				Assert.True(this.fixture.WaitForGameOutput("RANGER_DONE", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech(".TestCraftsmanVigour"); //the same character, an account holds five
				Assert.True(this.fixture.WaitForGameOutput("CRAFTSMAN_DONE", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech(".TestThiefAbilities");
				Assert.True(this.fixture.WaitForGameOutput("THIEF_DONE", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech(".TestNecroAbilities");
				Assert.True(this.fixture.WaitForGameOutput("NECRO_DONE", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech(".TestMysticAbilities");
				Assert.True(this.fixture.WaitForGameOutput("MYSTIC_DONE", Wait), this.fixture.GameOutputSnapshot());
				var ball = game.ReadGumpDialogWithStrings(Wait);
				for (int i = 0; (i < 10) && (ball.Strings.Count == 0); i++) { //#1070 - the ranger's and thief's icons come first
					ball = game.ReadGumpDialogWithStrings(Wait);
				}
				Assert.Contains("Ovladani NPC", ball.Strings);
				Assert.Contains(".catchfield", ball.Strings);
				game.SendSpeech(".TestMysticHypnosis");
				Assert.True(this.fixture.WaitForGameOutput("HYPNOSIS_DONE", Wait), this.fixture.GameOutputSnapshot());
				string output = this.fixture.GameOutputSnapshot();
				foreach (string check in new[] { "longRangeAddsThree", "longRangeCostsStamina", "knockbackEndsLongRange", "splitRangeGrowsWithDistance",
						"splitRangeStaysUnderItsLimit", "splitNextToTheTargetIsStrongest", "splitIsNeverAFullBlow", "splitShotCostsStamina", "splitShotMustCoolDown",
						"aSpreeShowsItsIcon", "aSplitShotShowsItsIcon", "theLastChargeClosesTheIcon", "crushingArrowWearsResists", "aSecondArrowReplacesTheFirst", "aWeakerArrowWaitsItsTurn", "arrowRecordsAsErebor",
						"aLightCrossbowTakesJaggedBoltsOnly", "aJaggedBoltBleeds", "aCrushingArrowUsesABolt", "killsBuildASpree",
						"ironLeadsToVerite", "mithrilIsTheTop", "noCounterNoLuck", "theCounterAndPointsFindBetter",
						"behindBonusSteps", "throatBonusSteps", "seenFromBehind", "notFromTheFront", "throatSlitCostsStamina", "throatSlitMustCoolDown",
						"shieldStrikeCostsStamina", "aBackstabHurts", "aBackstabReveals", "maskChangesNameAndRealm", "maskSwapsBack", "theMaskSuggestsAName", "rentalInfoShowsTheOtherIdentity", "underIncognitoTheRuneIsRenamed", "aKnifeHitsTheMonster", "knivesAreUsedUp", "aMagicReflexSendsTheKnifeBack", "aMagicBlockStopsIt", "aPoisonedKnifePoisons", "strikeSpendsChargesOnThoseAround", "aWarForkSwingsTwice", "aFencingBlowSpendsTheSlit", "undeadHitsByPoints", "darkpowerTurnsTheCounterOn", "darkpowerGivesManaBack", "notForRecall",
						"theBallIsItsOwnThing", "onlyPetsAndSummonsObey", "aSummonCanBeTaken", "itObeysTheMystic", "catchTakesHold", "bothStandStill",
						"holdingDrainsMana", "catchAgainLetsGo", "aSecondCatchHurts", "theFieldHoldsThoseAround", "aBlowFreesOne", "theFieldEndsAgain",
						"theFieldCoolsDownAnHour", "aTalentIsStolen", "andGivenBack", "concentrationStarts", "aStepBreaksIt", "theMysticIsTheMonster",
						"theMonsterWaitsAway", "anAlterEgoStandsInPlace", "theGearWaitsInTheBank", "itFightsAsTheMonster", "experienceWaits",
						"guardsLetAMildBodyAlone", "guardsGoForAnEvilBody", "monstersGoForIt", "itShowsAsTheMonster", "theMenusRefuse", "noGuildInTheName",
						"aGateRefusesIt", "unlessTheGateLetsHypnosisThrough", "noAbsorptionWhilePossessing", "aMonstersSpellIsAbsorbed", "noPointsNoAbsorption",
						"aBlowOnTheEgoBringsItHome", "withHalfItsHits", "theGearIsBack", "theMonsterIsBack", "experienceIsPaid",
						"dyingInTheBodyBringsItHome", "theEgosDeathBringsItHomeHurt" }) {
					string marker = "CLASS_ABILITY: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
			}
		}

		[Fact]
		public void ThePriestRisesByItsCandle() {
			using (TestGameClient game = this.Login("Canon")) {
				game.SendSpeech(".TestPriestAbilities");
				Assert.True(this.fixture.WaitForGameOutput("PRIEST_DIED", Wait), this.fixture.GameOutputSnapshot());
				Thread.Sleep(11000);
				game.SendSpeech(".TestPriestRise");
				Assert.True(this.fixture.WaitForGameOutput("PRIEST_DONE", Wait), this.fixture.GameOutputSnapshot());
				string output = this.fixture.GameOutputSnapshot();
				foreach (string check in new[] { "restoredByTens", "lightingMakesALitCandle", "theCandleStaysThroughDeath", "tooRestlessRightAfterDying",
						"thePriestRises", "risesAtHalfHits", "theCandleBurnsOut" }) {
					string marker = "CLASS_ABILITY: " + check + "=";
					int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
					Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
					Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
				}
			}
		}

		[Fact]
		public void TheAlchemistSellsTheCandle() {
			using (TestGameClient game = this.Login("Buyer")) {
				game.SendSpeech(".TestAlchemistPrepare");
				Assert.True(this.fixture.WaitForGameOutput("ALCHEMIST_READY", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech("chci koupit");
				(int GumpUid, string LayoutText, System.Collections.Generic.List<string> Strings) shop;
				try {
					shop = game.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
				} catch (Exception e) {
					throw new Xunit.Sdk.XunitException("No shop: " + e.Message + "\nGame output:\n" + this.fixture.GameOutputSnapshot());
				}
				Assert.Contains(shop.Strings, s => s.Contains("Svice zivota", StringComparison.Ordinal));
				game.SendGumpResponse(shop.GumpUid, 1, new System.Collections.Generic.Dictionary<int, string> { { 7, "1" }, { 6, "4" } });
				var order = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Nakupni Listek", order.Strings);
				Assert.Contains("45000", order.Strings);
				game.SendGumpResponse(order.GumpUid, 13);
				Thread.Sleep(500);
				int mark = this.fixture.GameOutputSnapshot().Length;
				game.SendSpeech(".TestAlchemistReport");
				Assert.True(this.fixture.WaitForGameOutputSince(" end", mark, Wait), this.fixture.GameOutputSnapshot());
				string report = this.fixture.GameOutputSnapshot().Substring(mark);
				Assert.Contains("gold=5000 candles=1 sight=4", report, StringComparison.Ordinal);

				game.SendSpeech(".TestAlchemistOtherRealm");
				Assert.True(this.fixture.WaitForGameOutput("ALCHEMIST_OTHER_REALM", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech("koupit");
				Assert.ThrowsAny<Exception>(() => game.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(3)));

				//#1070 - the Prodejce: coal as a stack, bowstrings in a bag past five, the sale counted
				game.SendSpeech(".TestProdejcePrepare");
				Assert.True(this.fixture.WaitForGameOutput("PRODEJCE_READY", Wait), this.fixture.GameOutputSnapshot());
				game.SendSpeech("chci koupit");
				var prodejce = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(prodejce.Strings, s => s.Contains("Tetiva Spruce", StringComparison.Ordinal));
				game.SendGumpResponse(prodejce.GumpUid, 1, new System.Collections.Generic.Dictionary<int, string> { { 1, "10" }, { 12, "6" } });
				var list = game.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("2650", list.Strings);
				game.SendGumpResponse(list.GumpUid, 13);
				Thread.Sleep(500);
				mark = this.fixture.GameOutputSnapshot().Length;
				game.SendSpeech(".TestProdejceReport");
				Assert.True(this.fixture.WaitForGameOutputSince(" end", mark, Wait), this.fixture.GameOutputSnapshot());
				report = this.fixture.GameOutputSnapshot().Substring(mark);
				Assert.Contains("coal=10 strings=6 bag=True sold=10 soldFor=2650 cascade=1M 234K 567", report, StringComparison.Ordinal);
			}
		}

		/// <summary>The shaman is the warrior's character, as an account holds five; its partner is on its own account.</summary>
		private void TheShamanFollowsErebor(TestGameClient game) {
			{
				int mark = this.fixture.GameOutputSnapshot().Length;
				game.SendSpeech(".TestEnableAutoAccountCreation"); //protocoltest is this server's first account
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, Wait), this.fixture.GameOutputSnapshot());
				using (TestGameClient partner = this.Login("Seer", "shamanpartner")) {
					partner.SendSpeech(".TestShamanPartner");
					Assert.True(this.fixture.WaitForGameOutput("SHAMAN_PARTNER_READY", Wait), this.fixture.GameOutputSnapshot());
					game.SendSpeech(".TestShamanAbilities");
					Assert.True(this.fixture.WaitForGameOutput("SHAMAN_PARTNER_DIED", Wait), this.fixture.GameOutputSnapshot());
					game.SendSpeech(".pact");
					var gump = game.ReadGumpDialogWithStrings(Wait);
					Assert.Contains("Pakt krve", gump.Strings);
					Assert.Contains(".lifepact", gump.Strings);
					Thread.Sleep(6000);
					game.SendSpeech(".TestShamanRise");
					Assert.True(this.fixture.WaitForGameOutput("SHAMAN_DONE", Wait), this.fixture.GameOutputSnapshot());
					string output = this.fixture.GameOutputSnapshot();
					foreach (string check in new[] { "bloodToMind", "mindToBloodTurnsItTwice", "balanceComesBack", "sacrificeTradesHitsForMana", "theShieldCostsACitrine", "theShieldIgnoresPlayers", "theShieldTakesAMonstersSpell", "theRestGetsThrough", "concentrationIsSometimesAThirdStronger", "noConcentrationOnPlayers", "noConcentrationForFields", "weakeningTakesARuby", "aBloodPactCostsBlood", "aMonstersBlowIsShared", "theGiverEarnsThroughTheBearer", "aPlayersBlowSwitchesItOff", "cancellingEndsBothSides", "aManaPactMayBeOwn", "theCaseIsAShamansThing", "aLifePactGivesASoul", "resistsRead", "theElixirLetsResistsBeSeen", "theSoulStaysThroughDeath", "tooRestlessRightAfterDying", "theSoulBringsItBack", "backWithATenth",
								"aPlayersBodyFallsApart", "daggersCurseByMetal", "aCurseHurtsAFifth", "aSecondCurseFallsBack", "noPortRefusesRecall", "aMonstersBodyGivesItsResources", "theAstralMorphTakesTheBody" }) {
						string marker = "CLASS_ABILITY: " + check + "=";
						int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
						Assert.True(idx >= 0, marker + " never reported. Game output:\n" + output);
						Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, marker + " failed. Game output:\n" + output);
					}
				}
			}
		}

		private TestGameClient Login(string name, string account = TestAccount) {
			Exception firstFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				int redirectPort = this.RedirectPort(account);
				var game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(name);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose();
					firstFailure = firstFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as " + name, firstFailure);
		}

		private int RedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
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
	public class ClassAbilityCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ClassAbility";
	}
}
