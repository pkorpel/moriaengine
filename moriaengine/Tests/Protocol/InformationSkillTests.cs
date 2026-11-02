using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Sphere's information skills with Erebor's own lines.</summary>
	[Collection(InformationSkillCollection.Name)]
	public class InformationSkillTests {
		private const int Anatomy = 1, ItemId = 3, ArmsLore = 4, EvalInt = 16, Forensics = 19, TasteId = 36;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public InformationSkillTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, params string[] patterns) {
			DateTime deadline = DateTime.UtcNow + Wait;
			foreach (string pattern in patterns) {
				string seen = "";
				try {
					string message;
					while (!Regex.IsMatch(message = client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
						seen += message + " | ";
					}
				} catch (TimeoutException) {
					throw new TimeoutException("No '" + pattern + "', only: " + seen);
				}
			}
		}

		private static uint Uid(string line) {
			return uint.Parse(Regex.Match(line, @"#(\d+)").Groups[1].Value);
		}

		private static void Use(TestGameClient client, int skill, uint target) {
			client.SendSkillUse(skill);
			Thread.Sleep(300);
			client.SendTargetUid(target);
		}

		[Fact]
		public void EachTellsWhatItKnows() {
			using var client = CommandPermissionTests.Connect("infotest", "Info Tester");
			this.Ask(client, ".TestInfoPrepare \"1100\"", "INFO_PREPARED:");
			uint rabbit = Uid(this.Ask(client, ".TestInfoNpc", "INFO_NPC:"));

			Use(client, Anatomy, rabbit);
			Expect(client, "Koho si prejes prozkoumat", "Test Rabbit looks superhumanly strong and very clumsy");

			Use(client, EvalInt, rabbit);
			Expect(client, "Vyber charakter pro odhad", "Test Rabbit looks about average", "They look clueless about magic and");

			uint sword = Uid(this.Ask(client, ".TestInfoItem \"i_katana\"", "INFO_ITEM:"));
			Use(client, ArmsLore, sword);
			Expect(client, "Jakou zbroj ci zbran", @"Attack \[\d+\]\. This item is in full repair\.");

			Use(client, ItemId, sword);
			Expect(client, "Co si prejes identifikovat", "Vyrobce: ", "does not apear to have any real resale value");

			Use(client, TasteId, sword);
			Expect(client, "Co si prejes ochutnat", "Neni otravena");

			uint corpse = Uid(this.Ask(client, ".TestInfoCorpse", "INFO_CORPSE:"));
			Use(client, Forensics, corpse);
			Expect(client, "Ci telo chces prozkoumat", "seconds old. It looks to have been killed by Info Tester");

			Use(client, Forensics, sword);
			Expect(client, "Forensics must be used on a corpse");
			Use(client, ArmsLore, rabbit);
			Expect(client, "That does not appear to be a weapon or armor");
			Use(client, TasteId, rabbit);
			Expect(client, "Try tasting some item");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class InformationSkillCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "InformationSkills";
	}
}
