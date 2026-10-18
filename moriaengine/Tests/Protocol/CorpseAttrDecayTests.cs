using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#383 - a GM-visibility follow-up to #236's corpse-decay design: an NPC's corpse
	/// now carries attr_decay cosmetically (it was already decaying unconditionally regardless of
	/// the flag; this just makes the flag reflect that for anyone checking .AttrList/.info).
	/// Shares AttrFlagTests.cs's existing TestKillNpcAndReportCorpse helper (#218) rather than
	/// re-deriving an NPC-kill/corpse-lookup path from scratch.</summary>
	[Collection(CorpseAttrDecayCollection.Name)]
	public class CorpseAttrDecayTests {
		private const string Account = "corpsedecaytest";
		private const string Password = "corpsedecaytest";

		private readonly GameServerPairFixture fixture;

		public CorpseAttrDecayTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void NpcCorpseCarriesAttrDecay() {
			using (var client = this.ConnectLoginAndCreateCharacter("CorpseDecayTester")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestKillNpcAndReportCorpse");
				Assert.True(this.fixture.WaitForGameOutputSince("ATTR_CORPSE_SETUP:", mark, TimeSpan.FromSeconds(10)));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_CORPSE_SETUP:");
				string uid = ExtractAfter(setupLine, "uid=");
				Assert.NotEqual("none", uid);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportCorpseHasAttrDecay " + uid);
				Assert.True(this.fixture.WaitForGameOutputSince("ATTR_CORPSE_DECAY_TEST:", mark, TimeSpan.FromSeconds(10)));
				string decayLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_CORPSE_DECAY_TEST:");
				Assert.Contains("hasAttrDecay=True", decayLine);
			}
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx).TrimEnd('\r');
		}

		private static string ExtractAfter(string text, string marker) {
			int idx = text.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + text);
			return text.Substring(idx + marker.Length).Trim();
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

	[CollectionDefinition(CorpseAttrDecayCollection.Name, DisableParallelization = true)]
	public class CorpseAttrDecayCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CorpseAttrDecayPair";
	}
}
