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

	/// <summary>#1107 - Erebor's AFK check.</summary>
	[Collection(ModerationCollection.Name)]
	public class AfkCheckTests {
		private readonly GameServerPairFixture fixture;

		public AfkCheckTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static string Field(string line, string key) {
			int at = line.IndexOf(key) + key.Length;
			return line.Substring(at, line.IndexOf(' ', at) - at);
		}

		private void Targeted(TestGameClient client, string command, uint self) {
			client.SendSpeech(command);
			Thread.Sleep(500);
			client.SendTargetUid(self);
			Thread.Sleep(300);
		}

		/// <summary>A check left to run out.</summary>
		private string MissOne(TestGameClient client, uint self) {
			this.Targeted(client, ".afk 2", self);
			Thread.Sleep(3500);
			return this.Ask(client, ".TestAfkState", "AFK_STATE:");
		}

		[Fact]
		public void TheCheckItsFailuresAndTheCamp() {
			using var client = CommandPermissionTests.Connect("moderation", "Afk Tester");
			string ready = this.Ask(client, ".TestAfkReady", "AFK_READY:");
			uint self = uint.Parse(ready.Substring(ready.IndexOf("uid=") + 4).Trim());

			//the dialog: Erebor's words, and as many red dots as the four letters have in the font
			this.Targeted(client, ".afk", self);
			var (gumpUid, layout, strings) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			string state = this.Ask(client, ".TestAfkState", "AFK_STATE:");
			Assert.Contains("check=True", state);
			Assert.Contains("AFK KONTROLA!", strings);
			int dots = System.Text.RegularExpressions.Regex.Matches(layout, @"gumppic \d+ \d+ 2360").Count;
			Assert.True(dots >= 4 * 8, state); //every letter of the font has 8 to 13 dots
			Assert.Equal(int.Parse(Field(state, "dots=")), dots);

			//a wrong answer: the check goes on and the dialog comes back
			client.SendGumpResponse(gumpUid, D_AfkCheckOk, new System.Collections.Generic.Dictionary<int, string> { { 1, "TEXT: 1234" } });
			var (againUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(5));
			Assert.Contains("check=True", this.Ask(client, ".TestAfkState", "AFK_STATE:"));
			//the right one ends it
			client.SendGumpResponse(againUid, D_AfkCheckOk, new System.Collections.Generic.Dictionary<int, string> { { 1, "TEXT: " + Field(state, "letters=").ToUpperInvariant() } });
			Thread.Sleep(500);
			Assert.Contains("check=False countdown=False letters= dots=0 fails=0 inCamp=False", this.Ask(client, ".TestAfkState", "AFK_STATE:"));

			//two failures are let go, the third sends to the camp for AFK, at Erebor's first AFK level
			Assert.Contains("fails=1 inCamp=False", this.MissOne(client, self));
			Assert.Contains("fails=2 inCamp=False", this.MissOne(client, self));
			Assert.Contains("fails=0 inCamp=True afkLevel=1 owed=1500", this.MissOne(client, self));
			//in the camp, three more double what's owed
			this.MissOne(client, self);
			this.MissOne(client, self);
			Assert.Contains("inCamp=True afkLevel=2 owed=3000", this.MissOne(client, self));

			//.runafk: the warning, then the check
			this.Targeted(client, ".runafk", self);
			Assert.Contains("check=False countdown=True", this.Ask(client, ".TestAfkState", "AFK_STATE:"));
			Thread.Sleep(3500);
			Assert.Contains("check=True countdown=False", this.Ask(client, ".TestAfkState", "AFK_STATE:"));
			this.Ask(client, ".TestAfkRestore", "AFK_RESTORED");
		}

		private const int D_AfkCheckOk = 1;

		/// <summary>#1107 step 4 - the AFK log: punish lowers the macroed skill, pardon takes back an AFK level, the filter,
		/// delete.</summary>
		[Fact]
		public void TheLogPunishesPardonsFiltersAndDeletes() {
			using var client = CommandPermissionTests.Connect("moderation", "Afk Log Tester");
			this.Ask(client, ".TestAfkLogSetup", "AFKLOG_SETUP:");

			client.SendSpeech(".afklog");
			var (logUid, _, strings) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			Assert.Contains(strings, s => s.Contains("makrar do lagru za Skill_Mining"));
			client.SendGumpResponse(logUid, 100000, new System.Collections.Generic.Dictionary<int, string>()); //Trest on the camp line
			var (inputUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
			client.SendGumpResponse(inputUid, 1, new System.Collections.Generic.Dictionary<int, string> { { 1, "10.0" } });
			Thread.Sleep(300);
			Assert.Contains("mining=400 afkLevel=2 count=3", this.Ask(client, ".TestAfkLogState", "AFKLOG_STATE:")); //the punishment is logged too

			client.SendSpeech(".afklog");
			(logUid, _, _) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			client.SendGumpResponse(logUid, 1000000, new System.Collections.Generic.Dictionary<int, string>()); //Milost
			var (confirmUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
			client.SendGumpResponse(confirmUid, 1, new System.Collections.Generic.Dictionary<int, string>());
			Thread.Sleep(300);
			Assert.Contains("afkLevel=1 count=4", this.Ask(client, ".TestAfkLogState", "AFKLOG_STATE:"));

			client.SendSpeech(".afklog makrar");
			(logUid, _, strings) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			Assert.Contains(strings, s => s.StartsWith("Vyhovujici zpravy: 1 celkem: 4"));
			client.SendGumpResponse(logUid, 10000, new System.Collections.Generic.Dictionary<int, string>()); //Smaz the first line
			client.ReadGumpDialog(TimeSpan.FromSeconds(10));
			Assert.Contains("count=3", this.Ask(client, ".TestAfkLogState", "AFKLOG_STATE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ModerationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Moderation";
	}
}
