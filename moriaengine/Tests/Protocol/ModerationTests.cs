using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1107 - the moderation toolkit: jail.</summary>
	[Collection(ModerationCollection.Name)]
	public class ModerationTests {
		private readonly GameServerPairFixture fixture;

		public ModerationTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static uint Number(string text, string key) {
			int at = text.IndexOf(key) + key.Length;
			int end = at;
			while ((end < text.Length) && char.IsDigit(text[end])) {
				end++;
			}
			return uint.Parse(text.Substring(at, end - at));
		}

		/// <summary>Sphere's JAIL and FORGIVE, through the real cursor commands.</summary>
		[Fact]
		public void JailAndForgive() {
			using var client = CommandPermissionTests.Connect("moderation", "Jail Tester");
			string ready = this.Ask(client, ".TestJailReady", "JAIL_READY:");
			uint self = Number(ready, "uid=");

			client.SendSpeech(".jail");
			Thread.Sleep(500);
			client.SendTargetUid(self);
			Thread.Sleep(500);
			Assert.Contains("inJail=True goRedirected=True loginRedirected=True monstersIgnore=True", this.Ask(client, ".TestJailReport", "JAIL_REPORT:"));

			client.SendSpeech(".forgive");
			Thread.Sleep(500);
			client.SendTargetUid(self);
			Thread.Sleep(500);
			Assert.Contains("jailed=False backWhereItWas=True", this.Ask(client, ".TestForgiveReport", "FORGIVE_REPORT:"));
		}
	}

	/// <summary>#1107 - Erebor's labour camp.</summary>
	[Collection(ModerationCollection.Name)]
	public class CampTests {
		private readonly GameServerPairFixture fixture;

		public CampTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private void Say(TestGameClient client, string speech, int times) {
			for (int i = 0; i < times; i++) {
				client.SendSpeech(speech);
				Thread.Sleep(300);
			}
		}

		[Fact]
		public void TheCampFromSentenceToRelease() {
			using var client = CommandPermissionTests.Connect("moderation", "Camp Tester");
			string ready = this.Ask(client, ".TestCampReady", "CAMP_READY:");
			uint self = uint.Parse(ready.Substring(ready.IndexOf("uid=") + 4).Trim());

			client.SendSpeech(".lagr");
			Thread.Sleep(500);
			client.SendTargetUid(self);
			Thread.Sleep(500);
			string sent = this.Ask(client, ".TestCampReport", "CAMP_REPORT:");
			Assert.Contains("name=Trestanec Camp Tester inCamp=True inArea=True level=1 lumber=200 coinsOwed=10 swordBanked=True hatchet=1", sent);

			Assert.Contains("overloadedStops=True free=True", this.Ask(client, ".TestCampLogs", "CAMP_LOGS:"));
			this.Say(client, "vymena", 3); //the third has no wood left
			Assert.Contains("coins=2 logs=0 owed=10", this.Ask(client, ".TestCampCoins", "CAMP_COINS:"));
			this.Say(client, "propustit", 1);
			Assert.Contains("coins=0 logs=0 owed=8", this.Ask(client, ".TestCampCoins", "CAMP_COINS:")); //the two are paid in
			this.Ask(client, ".TestCampGrantCoins", "CAMP_GRANTED:");
			this.Say(client, "propustit", 1);
			Assert.Contains("name=Camp Tester inCamp=False convict=False lumber=777 home=True coins=0 level=1", this.Ask(client, ".TestCampReleased", "CAMP_RELEASED:"));

			Assert.Contains("level=8 lumber=0 owed=0 recidivist=True", this.Ask(client, ".TestCampRecidivist", "CAMP_RECIDIVIST:"));

			//d_lagr_menu: OK saves the levels typed in
			client.SendSpeech(".lagrmenu");
			Thread.Sleep(500);
			client.SendTargetUid(self);
			var (gumpUid, _, strings) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			Assert.Contains("Zavrit za AFK", strings);
			client.SendGumpResponse(gumpUid, 1, new System.Collections.Generic.Dictionary<int, string> { { 101, "3" }, { 102, "2" }, { 103, "0" }, { 104, "0" } });
			client.ReadGumpDialog(TimeSpan.FromSeconds(10)); //OK opens it again, as Erebor's
			Assert.Contains("level=3 afk=2", this.Ask(client, ".TestCampLevels", "CAMP_LEVELS:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ModerationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Moderation";
	}
}
