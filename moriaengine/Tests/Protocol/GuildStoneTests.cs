using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#884 - a guild run from its stone, by two real clients.</summary>
	[Collection(GuildStoneCollection.Name)]
	public class GuildStoneTests {
		private const string AccountA = "guildstonetesta";
		private const string AccountB = "guildstonetestb";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public GuildStoneTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void DeedStoneMenuCandidatesAndTransfer() {
			using (var master = this.ConnectLoginAndCreateCharacter(AccountA, "GuildStoneFounder")) {
				this.Ask(master, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var joiner = this.ConnectLoginAndCreateCharacter(AccountB, "GuildStoneJoiner");

				//the deed puts the stone where it's targeted
				this.Ask(master, ".TestGuildStoneDeed", "GUILD_DEED:");
				string deed = this.Line("GUILD_DEED:");
				master.SendDoubleClick((uint) Number(deed, "pack"));
				Thread.Sleep(300);
				master.SendDoubleClick((uint) Number(deed, "uid"));
				Thread.Sleep(500);
				master.SendTargetGround((int) Number(deed, "x"), (int) Number(deed, "y"), (int) Number(deed, "z"));
				Thread.Sleep(500);
				this.Ask(master, ".TestGuildStoneFind", "GUILD_STONE:");
				uint stone = (uint) Number(this.Line("GUILD_STONE:"), "uid");
				Assert.NotEqual(0u, stone);

				//the first to use it becomes the master, and sees all 8 lines
				master.SendDoubleClick(stone);
				var gump = master.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Tvuj rank:");
				AssertHas(gump.Strings, "Predat vedeni");
				this.Ask(master, ".TestGuildStoneReport", "GUILD_REPORT:");
				Assert.Contains("master=GuildStoneFounder members=1 candidates=0 rank=3", this.Line("GUILD_REPORT:"));

				this.Ask(joiner, ".TestGuildStoneGoTo", "GUILD_ME:");
				uint joinerUid = (uint) Number(this.Line("GUILD_ME:"), "uid");
				Thread.Sleep(300);
				joiner.SendDoubleClick(stone);
				gump = joiner.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Verejnost");
				AssertLacks(gump.Strings, "Vzdat se clenstvi");

				//candidate, then member
				master.SendDoubleClick(stone);
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 5);
				Thread.Sleep(300);
				master.SendTargetUid(joinerUid);
				Thread.Sleep(300);
				joiner.SendDoubleClick(stone);
				gump = joiner.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Kandidat");
				AssertHas(gump.Strings, "Vzdat se clenstvi");
				AssertLacks(gump.Strings, "Pridat kandidata na clenstvi");

				master.SendDoubleClick(stone);
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 2);
				gump = master.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "GuildStoneJoiner");
				master.SendGumpResponse(gump.GumpUid, 500); //Prijmout
				gump = master.ReadGumpDialogWithStrings(Wait);
				AssertLacks(gump.Strings, "GuildStoneJoiner");
				this.Ask(joiner, ".TestGuildStoneReport", "GUILD_REPORT:");
				Assert.Contains("members=2 candidates=0 rank=2", this.Line("GUILD_REPORT:"));

				//name, abbreviation, the abbreviation switch
				master.SendGumpResponse(gump.GumpUid, 0); //back to the menu
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 6);
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 1, new Dictionary<int, string> { { 1, "Bratrstvo" } });
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 7);
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 1, new Dictionary<int, string> { { 1, "BR" } });
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 0);

				joiner.SendDoubleClick(stone);
				gump = joiner.ReadGumpDialogWithStrings(Wait);
				joiner.SendGumpResponse(gump.GumpUid, 4);
				Thread.Sleep(500);
				this.Ask(joiner, ".TestGuildStoneReport", "GUILD_REPORT:");
				Assert.Contains("name=Bratrstvo stoneName=Bratrstvo abbrev=BR", this.Line("GUILD_REPORT:"));
				Assert.Contains("shown=GuildStoneJoiner_[BR]", this.Line("GUILD_REPORT:"));
				master.SendSingleClick(joinerUid);
				AssertNameSent(master, "GuildStoneJoiner [BR]");

				//the master hands over and leaves
				master.SendDoubleClick(stone);
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 8);
				Thread.Sleep(300);
				master.SendTargetUid(joinerUid);
				Thread.Sleep(500);
				this.Ask(master, ".TestGuildStoneReport", "GUILD_REPORT:");
				Assert.Contains("master=GuildStoneJoiner", this.Line("GUILD_REPORT:"));
				Assert.Contains("rank=2", this.Line("GUILD_REPORT:"));

				master.SendDoubleClick(stone);
				gump = master.ReadGumpDialogWithStrings(Wait);
				master.SendGumpResponse(gump.GumpUid, 3);
				gump = master.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Potvrzeni prikazu");
				master.SendGumpResponse(gump.GumpUid, 1);
				Thread.Sleep(500);
				this.Ask(master, ".TestGuildStoneReport", "GUILD_REPORT:");
				Assert.Contains("master=GuildStoneJoiner members=1 candidates=0 rank=0", this.Line("GUILD_REPORT:"));
			}
		}

		private static void AssertHas(List<string> strings, string text) {
			Assert.True(strings.Exists(s => s.Contains(text)), "No '" + text + "' in: " + string.Join(" | ", strings));
		}

		private static void AssertLacks(List<string> strings, string text) {
			Assert.False(strings.Exists(s => s.Contains(text)), "'" + text + "' in: " + string.Join(" | ", strings));
		}

		/// <summary>The overhead name as a client gets it, 0xAE's text after the 30 byte name field.</summary>
		private static void AssertNameSent(TestGameClient client, string name) {
			const int TextOffset = 47;
			DateTime deadline = DateTime.UtcNow + Wait;
			string seen = "";
			while (DateTime.UtcNow < deadline) {
				byte[] payload = client.ReadPacketPayload(0xAE, deadline - DateTime.UtcNow);
				seen = Encoding.BigEndianUnicode.GetString(payload, TextOffset, payload.Length - TextOffset).TrimEnd('\0');
				if (seen == name) {
					return;
				}
			}
			Assert.Fail("No name '" + name + "', the last was '" + seen + "'");
		}

		private void Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
		}

		private string Line(string marker) {
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(index >= 0, "Could not find '" + marker + "'");
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private static double Number(string line, string key) {
			string marker = key + "=";
			int start = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(start >= 0, "Could not find '" + marker + "' in: " + line);
			start += marker.Length;
			int end = line.IndexOf(' ', start);
			string text = (end < 0) ? line.Substring(start) : line.Substring(start, end - start);
			return double.Parse(text.Trim(), CultureInfo.InvariantCulture);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(AccountA, AccountA);

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
	public class GuildStoneCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GuildStonePair";
	}
}
