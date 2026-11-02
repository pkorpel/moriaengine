using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's live spell values, @spellcast rules, and Sphere's paying at the end.</summary>
	[Collection(MageryRulesCollection.Name)]
	public class MageryRulesTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public MageryRulesTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		[Fact]
		public void ValuesRulesAndPayment() {
			using var client = CommandPermissionTests.Connect("rulestest", "Rules Tester");
			Assert.Contains("time=3.5 mana=30 difficulty=700 scroll=350", this.Ask(client, ".TestRulesValues", "RULES_VALUES:"));

			this.Ask(client, ".TestRulesPrepare \"1100\"", "RULES_READY:");

			//paid when it lands, not when it starts
			Assert.Contains("mana=200", this.Ask(client, ".TestRulesCast \"s_magic_arrow\"", "RULES_CAST:"));
			Thread.Sleep(2500);
			Assert.Contains("mana=194", this.Ask(client, ".TestRulesReport", "RULES_REPORT:"));

			//the Poison spell is a plain damage poison, a monster's stats fall by a share
			this.Ask(client, ".TestRulesCast \"s_poison\"", "RULES_CAST:");
			Thread.Sleep(3000);
			string poisoned = this.Ask(client, ".TestRulesReport", "RULES_REPORT:");
			Assert.Matches(@"poison=\d+", poisoned);
			Assert.Contains("share=-30", poisoned);

			//no teleporting onto a creature
			this.Ask(client, ".TestRulesCast \"s_teleport\"", "RULES_CAST:");
			Expect(client, "Nelze se premistovat na NPC");

			//no skill, no spell
			this.Ask(client, ".TestRulesPrepare \"10\"", "RULES_READY:");
			this.Ask(client, ".TestRulesCast \"s_flamestrike\"", "RULES_CAST:");
			Expect(client, "You lack the skill to cast this spell");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MageryRulesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MageryRules";
	}
}
