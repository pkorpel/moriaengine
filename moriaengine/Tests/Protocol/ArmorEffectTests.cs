using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression tests for #96 (ArmorVsP/M discarded for every case except monster-attacks-
	/// player). See scripts/Tests/ArmorEffectTests.cs for the server-side detail.
	/// </summary>
	[Collection(ArmorEffectCollection.Name)]
	public class ArmorEffectTests {
		private const string Account = "armoreffecttest";
		private const string Password = "armoreffecttest";

		private readonly GameServerPairFixture fixture;

		public ArmorEffectTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ArmorReducesPvMDamage() {
			using (var client = this.ConnectLoginAndCreateCharacter("ArmorEffectPvMChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestArmorReducesPvMDamage");
				Assert.True(this.fixture.WaitForGameOutputSince("ARMOREFFECT_PVM_TEST:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ARMOREFFECT_PVM_TEST:");

				double avgUnarmored = ExtractDouble(line, "avgUnarmored");
				double avgArmored = ExtractDouble(line, "avgArmored");

				Assert.True(avgUnarmored > 0, "Expected some damage against the unarmored dummy. Line: " + line);
				Assert.True(avgArmored < avgUnarmored,
					"Expected real ArmorVsP to measurably reduce player-vs-monster damage (#96). Line: " + line);
			}
		}

		[Fact]
		public void ArmorReducesMvPDamage() {
			using (var client = this.ConnectLoginAndCreateCharacter("ArmorEffectMvPChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestArmorReducesMvPDamage");
				Assert.True(this.fixture.WaitForGameOutputSince("ARMOREFFECT_MVP_TEST:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ARMOREFFECT_MVP_TEST:");

				double avgUnarmored = ExtractDouble(line, "avgUnarmored");
				double avgArmored = ExtractDouble(line, "avgArmored");

				Assert.True(avgUnarmored > 0, "Expected some damage against the unarmored player. Line: " + line);
				Assert.True(avgArmored < avgUnarmored,
					"Expected the already-working monster-attacks-player case to still work after the #96 refactor. Line: " + line);
			}
		}

		private static double ExtractDouble(string line, string key) {
			string marker = key + "=";
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = line.IndexOf(' ', start);
			if (end < 0) {
				end = line.Length;
			}
			return double.Parse(line.Substring(start, end - start), CultureInfo.InvariantCulture);
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
	public class ArmorEffectCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ArmorEffectPair";
	}
}
