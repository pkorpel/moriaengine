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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ModerationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Moderation";
	}
}
