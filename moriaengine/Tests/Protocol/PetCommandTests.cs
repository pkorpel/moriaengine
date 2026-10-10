using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Shared plumbing for V-2 (#70) pet-command scenarios. Split across two fresh collections
	/// for the same reason as CombatDepthTestsA/B (#69) - the account's 5-character-slot cap.
	/// </summary>
	public abstract class PetCommandTestBase {
		protected const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		protected PetCommandTestBase(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		protected string GameOutputSnapshot() {
			return this.fixture.GameOutputSnapshot();
		}

		/// <summary>Connects, logs in, creates a character, and runs the given setup/speech
		/// commands in order, waiting briefly between each so the server has a chance to process
		/// one before the next arrives (world-entry itself is asynchronous - see
		/// PacketSequences.SendStartGameSequence's own 1s delay).</summary>
		internal TestGameClient ConnectAndRun(string charName, params string[] speechCommands) {
			TestGameClient game = this.ConnectLoginAndCreateCharacter(charName);
			foreach (string command in speechCommands) {
				Thread.Sleep(300);
				game.SendSpeech(command);
			}
			return game;
		}

		protected void AssertSeen(string marker, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutput(marker, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse race applies here.</summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 20; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(TestAccount, TestPassword);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(250);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 20; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);

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
					Thread.Sleep(250);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PetCommandCollectionA : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "PetCommandPairA";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PetCommandCollectionB : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "PetCommandPairB";
	}

	[Collection(PetCommandCollectionA.Name)]
	public class PetCommandTestsA : PetCommandTestBase {
		public PetCommandTestsA(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>Real UO rule: a regular player can only command pets they actually own
		/// (ControlMaster == self) - a wild, unowned NPC must not respond to "all come" at all.
		/// Every test in this class shares one account (see PetCommandTestBase), so GM state has
		/// to be set deterministically (.TestSetGmOff/On, not the real, history-dependent ".GM"
		/// toggle) and restored afterward so this doesn't leak into whichever test xUnit happens
		/// to run next.</summary>
		[Fact]
		public void NonGmCannotCommandAnUnownedNpc() {
			using (var game = this.ConnectAndRun("PetNonGm", ".TestSetupWildNpc", ".TestSetGmOff", "all come", ".TestSetGmOn")) {
				this.AssertSeen("0 owned by me", TimeSpan.FromSeconds(10));
				this.AssertSeen("0 matched target 'all'", TimeSpan.FromSeconds(5));
			}
		}

		/// <summary>Real UO rule: a GM can order ANY NPC, owned or not - "all kill" from a GM
		/// against a wild, unowned NPC actually lands damage on the GM's chosen target. Forces
		/// GM on explicitly rather than assuming the account's default state, since that default
		/// depends on whatever the other tests in this class already did to it.</summary>
		[Fact]
		public void GmCanOrderAnUnownedNpcToKill() {
			using (var game = this.ConnectAndRun("PetGmKill", ".TestSetGmOn", ".TestSetupWildNpcAndVictim")) {
				// Addressing the wild NPC by name (not "all") so this doesn't also sweep up the
				// victim itself into the "pets" list - GM's "all" matches every NPC in range
				// regardless of role, which would otherwise have the victim told to attack
				// itself too (a confusing, unrelated edge case this test isn't about).
				Thread.Sleep(300);
				game.SendSpeech("TestWild kill");
				game.SendTargetUid(FindVictimUid(this.GameOutputSnapshot()));
				this.AssertSeen("PET_KILL_TEST: PASS", TimeSpan.FromSeconds(15));
			}
		}

		/// <summary>"all" must affect every owned pet, not just one - forced non-GM so this
		/// counts only TestPetOne/TestPetTwo, not also whatever wild NPCs an earlier test in this
		/// class left wandering nearby (a GM's "all" matches every NPC in range, owned or not).</summary>
		[Fact]
		public void AllVerbAffectsEveryOwnedPet() {
			using (var game = this.ConnectAndRun("PetAllVerb", ".TestSetGmOff", ".TestSetupTwoOwnedPets", "all guard")) {
				this.AssertSeen("-> matched 2 NPC(s)", TimeSpan.FromSeconds(10));
			}
		}

		private static uint FindVictimUid(string output) {
			// "PET_SETUP: STARTED (wild #4, victim #5)"
			int idx = output.LastIndexOf("victim #", StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find victim uid in game output:\n" + output);
			int start = idx + "victim #".Length;
			int end = start;
			while (end < output.Length && char.IsDigit(output[end])) {
				end++;
			}
			return uint.Parse(output.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	[Collection(PetCommandCollectionB.Name)]
	public class PetCommandTestsB : PetCommandTestBase {
		public PetCommandTestsB(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>"release" must actually revoke ownership, not just print a message - after
		/// releasing, and with GM mode off, the former pet must no longer respond to commands.</summary>
		[Fact]
		public void ReleaseActuallyRevokesOwnership() {
			using (var game = this.ConnectAndRun("PetRelease", ".TestSetupOwnedPet", "TestPet release", ".GM", "TestPet come")) {
				this.AssertSeen("0 matched target 'TestPet'", TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>"stop" must actually break off an active fight (NPCAiTimer.Disengage), not
		/// just stop wander/follow state - the original "stop" bug this session's combat work found.</summary>
		[Fact]
		public void StopInterruptsActiveCombat() {
			using (var game = this.ConnectAndRun("PetStop", ".TestSetupFightingPet", "TestPet stop")) {
				this.AssertSeen("NPCAiTimer.Disengage", TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>"go" opens a target cursor and, once resolved, sets PetGoalPoint to exactly
		/// that point - verified by reading the pet's actual state back, not just the SysMessage.</summary>
		[Fact]
		public void GoCommandSetsGoalPointFromTargetCursor() {
			using (var game = this.ConnectAndRun("PetGo", ".TestSetupOwnedPet")) {
				Thread.Sleep(300);
				game.SendSpeech("TestPet go");
				Thread.Sleep(300);
				game.SendTargetGround(5240, 40, 20);
				this.AssertSeen("sent 1 pet(s) to", TimeSpan.FromSeconds(10));
				this.AssertSeen("(5240,40,20)", TimeSpan.FromSeconds(5));
			}
		}
	}
}
