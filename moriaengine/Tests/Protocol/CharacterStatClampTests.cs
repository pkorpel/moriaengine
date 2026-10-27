using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#731 - see scripts/Tests/CharacterStatClampTests.cs's own doc comment for the
	/// full root-cause writeup (Character.AddHits/AddMana/AddStamina's inverted clamp always
	/// zeroed the stat, discovered via an NPC guard self-casting Greater Heal and dying).</summary>
	[Collection(CharacterStatClampCollection.Name)]
	public class CharacterStatClampTests {
		private const string Account = "statclamptest";
		private const string Password = "statclamptest";

		private readonly GameServerPairFixture fixture;

		public CharacterStatClampTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void AddHitsHealsDamagesAndClampsWithoutZeroing() {
			using (var client = this.ConnectLoginAndCreateCharacter("AddHitsChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddHitsClampsCorrectly");
				Assert.True(this.fixture.WaitForGameOutputSince("ADD_HITS_CLAMP_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ADD_HITS_CLAMP_TEST:");

				Assert.Contains("afterHeal=80", line);
				Assert.Contains("afterOverheal=100", line);
				Assert.Contains("afterLethal=0", line);
			}
		}

		[Fact]
		public void AddManaAndStaminaDoNotZeroOnGain() {
			using (var client = this.ConnectLoginAndCreateCharacter("AddManaStaminaChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddManaAndStaminaClampCorrectly");
				Assert.True(this.fixture.WaitForGameOutputSince("ADD_MANA_STAMINA_CLAMP_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ADD_MANA_STAMINA_CLAMP_TEST:");

				Assert.Contains("afterManaGain=30", line);
				Assert.Contains("afterStamGain=30", line);
			}
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

	[CollectionDefinition(CharacterStatClampCollection.Name, DisableParallelization = true)]
	public class CharacterStatClampCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CharacterStatClampPair";
	}
}
