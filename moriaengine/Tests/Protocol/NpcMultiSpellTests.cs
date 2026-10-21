using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #564 - proves NPCAiTimer.PickSpell (scripts/Basic/AI/NPCAiTimer.cs) picks uniformly at
	/// random among NPCDef.Spells (addspell=) when that list is configured, and falls back to
	/// the single CombatSpell unchanged when it isn't. npc_test_multi_spell
	/// (scripts/Tests/NpcMultiSpellTest.def) has two AddSpell entries; npc_test_single_combat_spell
	/// has only the old CombatSpell field, no AddSpell at all.
	/// </summary>
	[Collection(NpcMultiSpellCollection.Name)]
	public class NpcMultiSpellTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcMultiSpellTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void PicksRandomlyAmongBothGrantedSpells() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "SpellPickChecker")) {
				client.SendSpeech(".TestSampleSpellPickDistribution");
				Assert.True(this.fixture.WaitForGameOutput("NPC_SPELL_PICK_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_SPELL_PICK_TEST_RESULT:");

				int magicArrow = ParseIntField(line, "magicArrow=");
				int fireball = ParseIntField(line, "fireball=");

				//proves it's genuinely random-among-the-list, not silently always picking the
				//first entry - both spells must show up at least once across 400 samples
				Assert.True(magicArrow > 0, "Expected magic arrow to be picked at least once: " + line);
				Assert.True(fireball > 0, "Expected fireball to be picked at least once: " + line);
				Assert.Equal(400, magicArrow + fireball);
			}
		}

		[Fact]
		public void SingleCombatSpellOnlyPathStillWorksUnchanged() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "SingleSpellChecker")) {
				client.SendSpeech(".TestSampleSingleCombatSpellPick");
				Assert.True(this.fixture.WaitForGameOutput("NPC_SINGLE_SPELL_PICK_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_SINGLE_SPELL_PICK_TEST_RESULT:");

				Assert.Contains("matches=True", line);
			}
		}

		private static int ParseIntField(string line, string prefix) {
			int start = line.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
			int end = start;
			while (end < line.Length && (char.IsDigit(line[end]) || line[end] == '-')) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start));
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

	[CollectionDefinition(NpcMultiSpellCollection.Name, DisableParallelization = true)]
	public class NpcMultiSpellCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcMultiSpellPair";
	}
}
