using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1116 - the keep conquest. Server side: scripts/Tests/ConquestTests.cs.</summary>
	[Collection(ConquestCollection.Name)]
	public class ConquestTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public ConquestTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void SeeMessage(TestGameClient client, string text) {
			List<string> seen = new List<string>();
			for (int i = 0; i < 20; i++) {
				string message = client.ReadSystemMessage(Wait);
				if (message.Contains(text)) {
					return;
				}
				seen.Add(message);
			}
			Assert.Fail("No '" + text + "' among: " + string.Join(" / ", seen));
		}

		private static Dictionary<int, string> When(string day, string month, string hour) {
			return new Dictionary<int, string> { { 201, day }, { 202, month }, { 203, hour } };
		}

		/// <summary>Step 8: declaring at the stone, the queue, the war clock, the end, a cancel, a lapse.</summary>
		[Fact]
		public void DeclareQueueRunAndEnd() {
			using var first = CommandPermissionTests.Connect("conquest", "Conquest Tester");
			this.Ask(first, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var second = CommandPermissionTests.Connect("conquestsecond", "Conquest Second");
			string ready = this.Ask(first, ".TestConquestReady", "CONQUEST_READY:");
			Assert.Contains("name=Gondorsky vyhlasovac valky second=True", ready);
			uint stone = uint.Parse(ready.Substring(ready.IndexOf("stone=") + 6).Split(' ')[0]);

			//the dialog: Mordor's front keep, the hours; 18 is refused and the dialog comes back
			first.SendDoubleClick(stone);
			var (uid, _, strings) = first.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Vyhlaseni valky Mordorske pevnosti", strings);
			Assert.Contains("Osgiliath vychodni breh", strings);
			Assert.Contains("Mozne casy dobyvani: 19-21", strings);
			first.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "18"));
			(uid, _, _) = first.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("declared=none", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			first.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "20"));
			Thread.Sleep(500);
			Assert.Contains("declared=Mordor_1 lord=Conquest Tester begins=2026-11-05 20:00 queue=0 running=False", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));

			//the second one: the same hour is taken, a later one queues
			second.SendDoubleClick(stone);
			(uid, _, strings) = second.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Conquest Tester", strings);
			second.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "20"));
			(uid, _, _) = second.ReadGumpDialogWithStrings(Wait);
			second.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "21"));
			Thread.Sleep(500);
			Assert.Contains("queue=1", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));

			//.dobyvani
			first.SendSpeech(".dobyvani");
			SeeMessage(first, "Dobyvani pevnosti Osgiliath vychodni breh zacne 5.11 v 20h");

			//the war starts at its hour, runs out defended, and the queued one takes over
			this.Ask(first, ".TestConquestTick \"2026-11-05 19:59\"", "CONQUEST_TICK");
			Assert.Contains("running=False", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			this.Ask(first, ".TestConquestTick \"2026-11-05 20:00\"", "CONQUEST_TICK");
			Assert.Contains("running=True remaining=3000", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			this.Ask(first, ".TestConquestRunOut", "CONQUEST_RUNOUT");
			Assert.Contains("declared=Mordor_1 lord=Conquest Second begins=2026-11-05 21:00 queue=0 running=False remaining=0 right=True", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));

			//the new war lord cancels: peace, and the right is gone
			second.SendDoubleClick(stone);
			(uid, _, _) = second.ReadGumpDialogWithStrings(Wait);
			second.SendGumpResponse(uid, 2, new Dictionary<int, string>());
			Thread.Sleep(500);
			Assert.Contains("declared=none lord=none", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			Assert.Contains("right=False", this.Ask(second, ".TestConquestState", "CONQUEST_STATE:"));

			//an evening missed with the server down lapses
			Assert.Contains("declared=True after=none", this.Ask(first, ".TestConquestLapse", "CONQUEST_LAPSE:"));
		}
	}

	/// <summary>#1116 step 9 - the war lieutenants.</summary>
	[Collection(ConquestCollection.Name)]
	public class WarLieutenantTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public WarLieutenantTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void NorbertSellsTheRightToDeclareWar() {
			using var client = CommandPermissionTests.Connect("conquest", "Conquest Buyer");
			string ready = this.Ask(client, ".TestLieutenantReady \"29\"", "LIEUTENANT_READY:");
			uint npc = uint.Parse(ready.Substring(ready.IndexOf("npc=") + 4).Split(' ')[0]);
			//level 29: too green, nothing paid
			client.SendSpeech("ahoj");
			var (uid, _, strings) = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Vyhlasit utok", strings);
			Assert.Contains("Ano, to mi vyhovuje. Zaplatim 100 000 GP.", strings);
			client.SendGumpResponse(uid, 1, new Dictionary<int, string>());
			Thread.Sleep(500);
			Assert.Contains("right=False gold=100000", this.Ask(client, ".TestLieutenantState", "LIEUTENANT_STATE:"));
			//level 30: the right for 100 000
			this.Ask(client, ".TestLieutenantLevel \"30\"", "LIEUTENANT_LEVEL");
			client.SendDoubleClick(npc);
			(uid, _, _) = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(uid, 1, new Dictionary<int, string>());
			Thread.Sleep(500);
			Assert.Contains("right=True gold=0", this.Ask(client, ".TestLieutenantState", "LIEUTENANT_STATE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ConquestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Conquest";
	}
}
