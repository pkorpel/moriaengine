using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's Hiding, Stealth and Detect Hidden.</summary>
	[Collection(HidingCollection.Name)]
	public class HidingTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public HidingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void WaitForMessage(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		[Fact]
		public void HiddenUntilAStepOrASearchFindsYou() {
			using var client = CommandPermissionTests.Connect("hidetest", "Hide Tester");
			this.Ask(client, ".TestHideReady \"2000\"", "HIDE_READY:");

			//a timer, not at once, and Stealth's button does the same
			this.Ask(client, ".TestHideButton", "HIDE_STARTED:");
			Assert.Contains("hidden=False", this.Ask(client, ".TestHideState", "HIDE_STATE:"));
			WaitForMessage(client, "Skryti se povedlo");
			Assert.Contains("hidden=True", this.Ask(client, ".TestHideState", "HIDE_STATE:"));

			//high Stealth walks unseen, nobody runs unseen
			string skilled = this.Ask(client, ".TestHideSteps", "HIDE_STEPS:");
			Assert.Contains("ran=0", skilled);
			Assert.Contains("stayed=100", skilled);
			Assert.Contains("pluginFollows=True", skilled); //#1478

			//none is caught at once, dark or not
			this.Ask(client, ".TestHideReady \"0\"", "HIDE_READY:");
			Assert.True(int.Parse(Regex.Match(this.Ask(client, ".TestHideSteps", "HIDE_STEPS:"), @"stayed=(\d+)").Groups[1].Value) < 60);

			//attacking and being hit show you, and so does speaking
			Assert.Contains("attacking=False hit=False", this.Ask(client, ".TestHideDiscovered", "HIDE_DISCOVERED:"));
			client.SendSpeech("hello");
			Thread.Sleep(300);
			Assert.Contains("hidden=False", this.Ask(client, ".TestHideState", "HIDE_STATE:"));

			//searching spoils a hiding under way, finds a poor hider, and has to wait for the next go
			this.Ask(client, ".TestHideStart", "HIDE_STARTED:");
			this.Ask(client, ".TestHideOther \"0\"", "HIDE_OTHER:");
			WaitForMessage(client, "Nepovedlo se ti schovat");
			WaitForMessage(client, "Prozkoumavas");
			WaitForMessage(client, "Objevil jsi");
			WaitForMessage(client, "Hledani ukonceno");
			Assert.Contains("hidden=False", this.Ask(client, ".TestHideOtherState", "HIDE_OTHER_STATE:"));
			this.Ask(client, ".TestHideOther \"0\"", "HIDE_OTHER:");
			WaitForMessage(client, "Jeste nelze");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class HidingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Hiding";
	}
}
