using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for a real user-reported bug: an NPC caster (e.g. c_titan_mage) casting a
	/// beneficial spell (Magic Reflection) aimed it straight at its enemy target instead of
	/// itself - NPCAiTimer.PickSpell has no concept of offensive vs. defensive, and
	/// TryCastSpell always cast at whatever target the NPC was fighting. Verified via
	/// NPCAiTimer.TryCastSpell's own debug log line ("casts Magic Reflect at &lt;name&gt;") -
	/// see scripts/Tests/NpcBeneficialSpellTests.cs's own doc comment for why this doesn't
	/// assert on the resulting MagicReflectionPlugin installation instead (a separate,
	/// pre-existing, unrelated bug blocks that). Same real combat-engagement scenario shape as
	/// CombatScenarioTests.cs, own dedicated collection for the same account-isolation reason.
	/// </summary>
	[Collection(NpcBeneficialSpellCollection.Name)]
	public class NpcBeneficialSpellTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";
		private const string TestCharName = "Tester";

		public NpcBeneficialSpellTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void BeneficialSpellLandsOnCasterNotEnemy() {
			int redirectPort = WaitForServerListingAndGetRedirectPort();

			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				string[] characters = game.ReadCharacterList();
				Assert.All(characters, name => Assert.Null(name));

				game.SendCreateCharacter(TestCharName);
				game.SendSpeech(".TestBeneficialSpellCastsOnSelf");

				Assert.True(this.fixture.WaitForGameOutput("NPCAiTimer.TryCastSpell: Beneficial Spell Test Caster",
					TimeSpan.FromSeconds(20)),
					"Expected the caster to attempt a cast within 20s. Game output:\n" + this.fixture.GameOutputSnapshot());
			}

			string output = this.fixture.GameOutputSnapshot();
			int idx = output.LastIndexOf("NPCAiTimer.TryCastSpell: Beneficial Spell Test Caster", StringComparison.Ordinal);
			int end = output.IndexOf('\n', idx);
			string line = output.Substring(idx, (end < 0 ? output.Length : end) - idx);
			Assert.Contains("casts Magic Reflect at Beneficial Spell Test Caster", line);
			Assert.DoesNotContain("casts Magic Reflect at Zajic", line);
		}

		private readonly GameServerPairFixture fixture;

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
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
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class NpcBeneficialSpellCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcBeneficialSpellPair";
	}
}
