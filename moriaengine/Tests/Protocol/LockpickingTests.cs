using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression for #282: Targ_Lockpick.On_TargonItem's locked-check was inverted
	/// (!ItemLockPlugin.IsLocked gated the skill attempt), so lockpicking only ever "worked"
	/// on items that were already unlocked, and silently re-targeted forever on an actually-
	/// locked one. Proves both directions: targeting a LOCKED item now actually starts the
	/// skill sequence (ends in a fail/success message, not another target-cursor prompt),
	/// and targeting an UNLOCKED item gets a clear "not locked" message instead.
	/// </summary>
	[Collection(LockpickingCollection.Name)]
	public class LockpickingTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LockpickingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void LockpickingOnLockedItemActuallyAttempts() {
			using (var client = this.ConnectLoginAndCreateCharacter("LockpickAttempter")) {
				var (lockpickUid, lockedUid, _) = this.SetupScenario(client);

				client.SendDoubleClick(lockpickUid);
				// Consume the "what do you want to use the lockpick on?" prompt - its exact text
				// uses this file's own legacy (non-UTF8) Czech byte encoding, not worth asserting
				// on here; just confirm a real prompt arrived before moving on.
				Assert.False(string.IsNullOrEmpty(client.ReadSystemMessage(TimeSpan.FromSeconds(10))));

				client.SendTargetUid(lockedUid);
				string result = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				// "odemk" is the common ASCII-safe root of both the fail ("...odemknout...") and
				// success ("...odemkl...") messages - either proves the skill sequence actually
				// started (PhaseStart ran). Pre-fix, targeting a locked item never got this far.
				Assert.Contains("odemk", result);
			}
		}

		[Fact]
		public void LockpickingOnUnlockedItemShowsNotLockedMessage() {
			using (var client = this.ConnectLoginAndCreateCharacter("LockpickOnUnlocked")) {
				var (lockpickUid, _, unlockedUid) = this.SetupScenario(client);

				client.SendDoubleClick(lockpickUid);
				Assert.False(string.IsNullOrEmpty(client.ReadSystemMessage(TimeSpan.FromSeconds(10))));

				client.SendTargetUid(unlockedUid);
				Assert.Equal("That isn't locked.", client.ReadSystemMessage(TimeSpan.FromSeconds(10)));
			}
		}

		private (uint lockpickUid, uint lockedUid, uint unlockedUid) SetupScenario(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestSetupLockpickScenario");
			Assert.True(this.fixture.WaitForGameOutputSince("LOCKPICK_SCENARIO:", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LOCKPICK_SCENARIO:");
			return (
				ExtractUidAfter(line, "lockpickUid="),
				ExtractUidAfter(line, "lockedUid="),
				ExtractUidAfter(line, "unlockedUid="));
		}

		private static uint ExtractUidAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + line);
			string rest = line.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), CultureInfo.InvariantCulture);
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
	public class LockpickingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LockpickingPair";
	}
}
