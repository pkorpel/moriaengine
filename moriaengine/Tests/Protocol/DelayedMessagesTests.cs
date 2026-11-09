using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#901 - Erebor's delayedmessage: read at once by someone online, kept for someone offline and listed at
	/// their login (important ones apart), deleted by a click, .ms and .clearms.</summary>
	[Collection(DelayedMessagesCollection.Name)]
	public class DelayedMessagesTests {
		private const string AccountA = "delayedmsga", AccountB = "delayedmsgb";
		private const int Close = 0, FirstMessage = 10000;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public DelayedMessagesTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void OfflinePlayersFindTheirMessagesAtLogin() {
			using (var sender = this.Connect("Mail Sender", AccountA, false)) {
				this.Ask(sender, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				var reader = this.Connect("Mail Reader", AccountB, false);
				uint readerUid = Number(this.Ask(reader, ".TestDelayedReport", "DELAYED_REPORT:"), "uid=");

				//online: read at once, nothing kept
				this.Ask(sender, ".TestDelayedSend " + readerUid, "DELAYED_SENT:");
				Assert.Contains("online=True normal=0 red=0", this.Report(sender, readerUid));

				//offline: kept, the important one apart
				reader.Dispose();
				DateTime until = DateTime.UtcNow + Wait;
				while (!this.Report(sender, readerUid).Contains("online=False") && (DateTime.UtcNow < until)) {
					Thread.Sleep(300);
				}
				this.Ask(sender, ".TestDelayedSend " + readerUid, "DELAYED_SENT:");
				Assert.Contains("online=False normal=2 red=1 first=Zprava_jedna|Zprava_dva", this.Report(sender, readerUid));

				//at login: the important ones first, then the rest, a button by each
				using (reader = this.Connect("Mail Reader", AccountB, true)) {
					var red = reader.ReadGumpDialogWithStrings(Wait);
					Assert.Contains("Nove dulezite zpravy: 1", string.Join(" | ", red.Strings));
					Assert.Contains("Dulezita jedna", string.Join(" | ", red.Strings));
					Assert.Matches(@"4005\s+4007\s+1\s+0\s+" + FirstMessage + @"\b", red.LayoutText);
					var normal = reader.ReadGumpDialogWithStrings(Wait);
					string normalText = string.Join(" | ", normal.Strings);
					Assert.Contains("Nove zpravy: 2", normalText);
					Assert.Contains("Zprava jedna", normalText);
					Assert.Contains("Zprava dva", normalText);
					Assert.Matches(@"4005\s+4007\s+1\s+0\s+" + (FirstMessage + 1) + @"\b", normal.LayoutText);

					//a click deletes the message, the list comes back without it
					reader.SendGumpResponse(normal.GumpUid, FirstMessage);
					normal = reader.ReadGumpDialogWithStrings(Wait);
					normalText = string.Join(" | ", normal.Strings);
					Assert.Contains("Nove zpravy: 1", normalText);
					Assert.DoesNotContain("Zprava jedna", normalText);
					reader.SendGumpResponse(normal.GumpUid, Close);
					reader.SendGumpResponse(red.GumpUid, FirstMessage);
					Thread.Sleep(300);
					Assert.Contains("normal=1 red=0 first=Zprava_dva", this.Report(reader, readerUid));

					//.ms opens them again, .clearms empties them: player commands
					reader.SendSpeech(".ms");
					normal = reader.ReadGumpDialogWithStrings(Wait);
					Assert.Contains("Zprava dva", string.Join(" | ", normal.Strings));
					reader.SendGumpResponse(normal.GumpUid, Close);
					reader.SendSpeech(".clearms");
					Thread.Sleep(300);
					Assert.Contains("normal=0 red=0", this.Report(reader, readerUid));
				}
			}
		}

		private string Report(TestGameClient client, uint uid) {
			return this.Ask(client, ".TestDelayedReport " + uid, "DELAYED_REPORT:");
		}

		private static uint Number(string line, string key) {
			int start = line.IndexOf(key, StringComparison.Ordinal) + key.Length;
			return uint.Parse(line.Substring(start, line.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
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

		private TestGameClient Connect(string charName, string account, bool existing) {
			int redirectPort = TownTestsLogin.RedirectPort(AccountA);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					if (existing) {
						game.SendSelectCharacter(0);
					} else {
						game.SendCreateCharacter(charName);
					}
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
	public class DelayedMessagesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DelayedMessages";
	}
}
