using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#931 - chat by real clients. The test client is 7.0.15.1: how a line looks on 3.0.6m isn't
	/// tested here.</summary>
	[Collection(ChatCollection.Name)]
	public class ChatTests {
		private const string AccountA = "chata", AccountB = "chatb", AccountC = "chatc";
		private const int Send = 1, Gondor = 2, Mordor = 3, ToGms = 4, ToAll = 5, GuildBox = 1, AddName = 2, ChannelBase = 100, RemoveName = 200;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static readonly TimeSpan NotHeard = TimeSpan.FromMilliseconds(800);

		private readonly GameServerPairFixture fixture;

		public ChatTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void WhoHearsWhat() {
			using var client = this.Connect("Chat Rules", AccountA);
			string result = this.Ask(client, ".TestChatRules", "CHAT_RULES:");
			List<string> found = new List<string>();
			foreach (string line in this.fixture.GameOutputSnapshot().Split('\n')) {
				if (line.Contains("CHAT_FAIL:")) {
					found.Add(line);
				}
			}
			Assert.True(result.Contains("fails=0"), result + "\n" + string.Join("\n", found));
		}

		private void Write(TestGameClient client, string text, int button = Send, int[] switches = null) {
			client.SendSpeech(".zprava");
			var input = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(input.GumpUid, button, switches, new Dictionary<int, string> { { 1, text } });
		}

		private static void Heard(TestGameClient client, string text) {
			Assert.True(MasterQuestTests.TryReadMessageContaining(client, text, Wait) != null, "Not heard: " + text);
		}

		private static void NotHeardBy(TestGameClient client, string text) {
			Assert.True(MasterQuestTests.TryReadMessageContaining(client, text, NotHeard) == null, "Heard: " + text);
		}

		[Fact]
		public void ARealmTalks() {
			using var a = this.Connect("Chat Alpha", AccountA);
			this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var b = this.Connect("Chat Beta", AccountB);
			using var c = this.Connect("Chat Gamma", AccountC);
			this.Ready(a, "G");
			this.Ready(b, "G");
			this.Ready(c, "M");

			//to the realm, sender too; what Erebor refused goes through
			this.Write(a, "ahoj (svete) [x] <y> {z}");
			Heard(b, "*Chat Alpha* ahoj (svete) [x] <y> {z}");
			Heard(a, "*Chat Alpha* ahoj");
			NotHeardBy(c, "ahoj");

			//too long is cut, and told
			this.Write(a, new string('x', 100));
			Heard(a, "prilis dlouha");
			string line = MasterQuestTests.TryReadMessageContaining(b, "*Chat Alpha*", Wait);
			Assert.Contains("* " + new string('x', 73), line);
			Assert.DoesNotContain(new string('x', 74), line);

			//nothing is nothing
			this.Write(a, "   ");
			NotHeardBy(b, "*Chat Alpha*");

			//one at a time
			this.Ask(a, ".TestChatRate \"60\"", "CHAT_RATE:");
			this.Write(a, "prvni");
			Heard(b, "prvni");
			this.Write(a, "druha");
			Heard(a, "Pockej chvili");
			NotHeardBy(b, "druha");
			this.Ask(a, ".TestChatRate \"0\"", "CHAT_RATE:");
			this.Ready(a, "G");

			//.chat: a channel off, and nothing goes out on it either; the other realm's isn't offered
			a.SendSpeech(".chat");
			var options = a.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("GONDOR Realm: On", string.Join("|", options.Strings));
			Assert.DoesNotContain("MORDOR Realm", string.Join("|", options.Strings));
			Assert.DoesNotContain("GMChat", string.Join("|", options.Strings));
			a.SendGumpResponse(options.GumpUid, ChannelBase + 1);
			options = a.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("GONDOR Realm: Off", string.Join("|", options.Strings));
			this.Write(a, "vypnuto");
			Heard(a, "not currently on that channel");
			NotHeardBy(b, "vypnuto");
			this.Write(b, "pro tebe nic");
			NotHeardBy(a, "pro tebe nic");
			a.SendGumpResponse(options.GumpUid, ChannelBase + 1);
			options = a.ReadGumpDialogWithStrings(Wait);
			a.SendGumpResponse(options.GumpUid, 0);

			//the list
			b.SendSpeech(".chat");
			options = b.ReadGumpDialogWithStrings(Wait);
			b.SendGumpResponse(options.GumpUid, AddName, new Dictionary<int, string> { { 1, "chat alpha" } });
			options = b.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("chat alpha", string.Join("|", options.Strings));
			this.Write(a, "umlcen");
			NotHeardBy(b, "umlcen");
			b.SendGumpResponse(options.GumpUid, RemoveName);
			options = b.ReadGumpDialogWithStrings(Wait);
			b.SendGumpResponse(options.GumpUid, 0);
			this.Write(a, "uz zase");
			Heard(b, "uz zase");

			//.mute
			this.Ask(a, ".TestChatSilence \"1\"", "CHAT_SILENCED:");
			this.Write(a, "ticho");
			Heard(a, "find yourself mute");
			NotHeardBy(b, "ticho");
			this.Ask(a, ".TestChatSilence \"0\"", "CHAT_SILENCED:");

			//an outcast has no channel
			this.Ready(a, "g");
			this.Write(a, "vyvrhel");
			Heard(a, "Nepatris k zadne risi");
			NotHeardBy(b, "vyvrhel");
		}

		[Fact]
		public void AGuildTalksAndAGmChoosesTheChannel() {
			using var a = this.Connect("Chat Master", AccountA);
			this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var b = this.Connect("Chat Bystander", AccountB);
			this.Ready(a, "G");
			this.Ready(b, "G");
			this.Ask(a, ".TestChatGuild", "CHAT_GUILD:");

			//the box sends to the guild and is remembered, so the bystander hears nothing
			this.Write(a, "cechu", Send, new[] { GuildBox });
			Heard(a, "*Chat Master* cechu");
			NotHeardBy(b, "cechu");
			a.SendSpeech(".zprava");
			var input = a.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("GuildChat", string.Join("|", input.Strings));
			Assert.Contains("checkbox", input.LayoutText.ToLowerInvariant());
			a.SendGumpResponse(input.GumpUid, 0);
			Thread.Sleep(200);

			a.SendSpeech(".guildscream");
			Heard(a, "Pomoc ! Jsem na souradnicich");
			NotHeardBy(b, "Pomoc");
			a.SendSpeech(".guildsay ahoj");
			Heard(a, "*Chat Master* ahoj");

			//a GM's dialog has the channels, and they hear them all
			this.Ask(a, ".TestChatGm", "CHAT_GM:");
			this.Write(a, "gondore", Gondor);
			Heard(b, "*Chat Master* gondore");
			this.Write(a, "mordore", Mordor);
			NotHeardBy(b, "mordore");
			Heard(a, "*Chat Master* mordore");
			this.Write(a, "gmove", ToGms);
			Heard(a, "*Chat Master* gmove");
			NotHeardBy(b, "gmove");
			this.Write(a, "vsichni", ToAll);
			Heard(b, "*Chat Master* vsichni");

			//the commands
			a.SendSpeech(".goch \"prikazem\"");
			Heard(b, "*Chat Master* prikazem");
			a.SendSpeech(".all \"oznameni\"");
			Heard(b, "*Chat Master* oznameni");
			a.SendSpeech(".gmch \"interni\"");
			NotHeardBy(b, "interni");

			//a player's commands are theirs
			int mark = this.fixture.GameOutputSnapshot().Length;
			b.SendSpeech(".gmch \"zkouska\"");
			Thread.Sleep(400);
			Assert.DoesNotContain("zkouska", this.fixture.GameOutputSnapshot().Substring(mark).Replace("gmch \"zkouska\"", ""));
		}

		[Fact]
		public void ALostRingSpeaksToMordor() {
			using var a = this.Connect("Chat Ringbearer", AccountA);
			this.Ready(a, "M");
			this.Ask(a, ".TestChatRingLost", "CHAT_RING:");
			Heard(a, "si me nezaslouzi");
		}

		private void Ready(TestGameClient client, string realm) {
			this.Ask(client, ".TestChatReady \"" + realm + "\"", "CHAT_READY:");
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient Connect(string charName, string account) {
			int redirectPort = TownTestsLogin.RedirectPort(AccountA);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					Thread.Sleep(500);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is IOException)) {
					game.Dispose();
					lastFailure = lastFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as '" + charName + "' after retrying.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ChatCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Chat";
	}
}
