using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// A Hostile-AggroMode creature must never target/damage whoever currently commands it
	/// (ControlMaster or the GM-"come" FollowTarget override) - see NPC.ShouldAggroOn and
	/// NPCAiTimer.IsValidTarget. A legitimate "rebel" (owner attacks their own pet first) drops
	/// ownership so the pet is free to defend itself afterward (NPC.On_HostileAction).
	/// </summary>
	[Collection(OwnerAggroCollection.Name)]
	public class OwnerAggroTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public OwnerAggroTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void HostileFollowerNeverAutonomouslyTargetsItsCommander() {
			using (var client = this.ConnectLoginAndCreateCharacter("OwnerAggroFollower")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(300);

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupHostileFollower");
				Assert.True(this.fixture.WaitForGameOutputSince("OWNER_AGGRO_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				uint titanUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "OWNER_AGGRO_SETUP: STARTED (uid=");

				//Give the AI timer (1.5s tick) several real chances to autonomously aggro before checking.
				Thread.Sleep(4000);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckOwnerAggroState " + titanUid);
				Assert.True(this.fixture.WaitForGameOutputSince("OWNER_AGGRO_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				string checkLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "OWNER_AGGRO_CHECK:");

				// DIAGNOSTIC: dump every DIAG line from the server's own stdout to THIS
				// process's stdout - GameOutputSnapshot() is only ever an in-memory buffer
				// otherwise, so none of it reaches the CI console log unless explicitly
				// printed here.
				foreach (string line in this.fixture.GameOutputSnapshot().Split('\n')) {
					if (line.Contains("DIAG")) {
						Console.WriteLine(line);
					}
				}

				Assert.Contains("target=none", checkLine);
			}
		}

		[Fact]
		public void OwnerAttackingOwnedPetMakesItRebel() {
			using (var client = this.ConnectLoginAndCreateCharacter("OwnerAggroRebel")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(300);

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupOwnedHostilePet");
				Assert.True(this.fixture.WaitForGameOutputSince("OWNER_AGGRO_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				uint petUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "OWNER_AGGRO_SETUP: STARTED (uid=");

				int markBefore = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckOwnerAggroState " + petUid);
				Assert.True(this.fixture.WaitForGameOutputSince("OWNER_AGGRO_CHECK:", markBefore, TimeSpan.FromSeconds(10)));
				string beforeLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "OWNER_AGGRO_CHECK:");
				Assert.Contains("controlMaster=OwnerAggroRebel", beforeLine);

				int markAttack = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestOwnerAttacksPet " + petUid);
				Assert.True(this.fixture.WaitForGameOutputSince("OWNER_ATTACK:", markAttack, TimeSpan.FromSeconds(10)));

				int markAfter = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckOwnerAggroState " + petUid);
				Assert.True(this.fixture.WaitForGameOutputSince("OWNER_AGGRO_CHECK:", markAfter, TimeSpan.FromSeconds(10)));
				string afterLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "OWNER_AGGRO_CHECK:");
				Assert.Contains("controlMaster=none", afterLine);
			}
		}

		private static uint ExtractUidAfter(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
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

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Password);

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
	public class OwnerAggroCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "OwnerAggroPair";
	}
}
