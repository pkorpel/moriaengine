using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#916 - .party, by three real clients.</summary>
	[Collection(PartyDialogCollection.Name)]
	public class PartyDialogTests {
		private const string AccountA = "partydialoga", AccountB = "partydialogb", AccountC = "partydialogc";
		private const int Found = 1, Leave = 2, Disband = 3, Kick = 100, Promote = 200, Invite = 300;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public PartyDialogTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void QuickToFormHandOverLeaveAndSwitch() {
			using (var a = this.Connect("Party Alpha", AccountA)) {
				this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var b = this.Connect("Party Beta", AccountB);
				using var c = this.Connect("Party Gamma", AccountC);
				uint aUid = this.Ready(a, "G");
				uint bUid = this.Ready(b, "G");
				uint cUid = this.Ready(c, "M");

				//the list has your own realm, and nobody hidden
				this.SayParty(a);
				var menu = a.ReadGumpDialogWithStrings(Wait);
				Has(menu.Strings, "Party Beta");
				Hasnt(menu.Strings, "Party Gamma");
				Has(menu.Strings, "Zalozit druzinu");
				a.SendGumpResponse(menu.GumpUid, 0);
				this.Ask(b, ".TestPartyHide 1", "PARTY_HIDDEN:");
				this.SayParty(a);
				menu = a.ReadGumpDialogWithStrings(Wait);
				Hasnt(menu.Strings, "Party Beta");
				a.SendGumpResponse(menu.GumpUid, 0);
				this.Ask(b, ".TestPartyHide 0", "PARTY_HIDDEN:");

				//one click invites, and makes the party
				this.SayParty(a);
				menu = a.ReadGumpDialogWithStrings(Wait);
				a.SendGumpResponse(menu.GumpUid, Invite);
				menu = a.ReadGumpDialogWithStrings(Wait);
				b.SendPartyAcceptInvite(aUid);
				Thread.Sleep(300);
				Assert.Contains("leader=Party_Alpha members=2", this.Report(b));

				//no party across the realms, by the target cursor either
				a.SendGumpResponse(menu.GumpUid, 0);
				a.SendPartyAddMemberRequest();
				Thread.Sleep(300);
				a.SendTargetUid(cUid);
				Thread.Sleep(300);
				c.SendPartyAcceptInvite(aUid);
				Thread.Sleep(300);
				Assert.Contains("inParty=False", this.Report(c));

				//a member who isn't the leader invites
				this.Ready(c, "G");
				this.SayParty(b);
				menu = b.ReadGumpDialogWithStrings(Wait);
				Has(menu.Strings, "Party Gamma");
				b.SendGumpResponse(menu.GumpUid, Invite);
				menu = b.ReadGumpDialogWithStrings(Wait);
				b.SendGumpResponse(menu.GumpUid, 0);
				c.SendPartyAcceptInvite(bUid);
				Thread.Sleep(300);
				Assert.Contains("members=3", this.Report(c));

				//the roster shows who's where, and the leader hands over and takes back
				this.SayParty(a);
				menu = a.ReadGumpDialogWithStrings(Wait);
				Has(menu.Strings, "Clenu: 3/10");
				Has(menu.Strings, "u tebe");
				a.SendGumpResponse(menu.GumpUid, Promote + 1);
				menu = a.ReadGumpDialogWithStrings(Wait);
				a.SendGumpResponse(menu.GumpUid, 0);
				Assert.Contains("leader=Party_Beta members=3", this.Report(a));

				//the leader leaves: the longest-standing takes over and the group stays
				this.SayParty(b);
				menu = b.ReadGumpDialogWithStrings(Wait);
				b.SendGumpResponse(menu.GumpUid, Leave);
				menu = b.ReadGumpDialogWithStrings(Wait);
				b.SendGumpResponse(menu.GumpUid, 0);
				Assert.Contains("inParty=False", this.Report(b));
				Assert.Contains("leader=Party_Alpha members=2", this.Report(c));

				//accepting moves you: Beta invites Gamma, the old party is down to one and ends
				this.SayParty(b);
				menu = b.ReadGumpDialogWithStrings(Wait);
				b.SendGumpResponse(menu.GumpUid, Invite); //the list is sorted, Gamma isn't first alone
				menu = b.ReadGumpDialogWithStrings(Wait);
				b.SendGumpResponse(menu.GumpUid, 0);
				a.SendPartyAcceptInvite(bUid);
				Thread.Sleep(300);
				Assert.Contains("leader=Party_Beta members=2", this.Report(a));
				Assert.Contains("inParty=False", this.Report(c));

				//the leader kicks; a party of one ends
				this.SayParty(b);
				menu = b.ReadGumpDialogWithStrings(Wait);
				b.SendGumpResponse(menu.GumpUid, Kick + 1);
				b.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("inParty=False", this.Report(a));
				Assert.Contains("inParty=False", this.Report(b));
			}
		}

		/// <summary>#929 - every step of a party member threw, and nothing failed for it.</summary>
		[Fact]
		public void WalkingInAPartyWorksAndMembersHearOfEachOther() {
			using (var a = this.Connect("Walker Alpha", AccountA)) {
				this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var b = this.Connect("Walker Beta", AccountB);
				uint aUid = this.Ready(a, "G");
				this.Ready(b, "G");
				this.SayParty(a);
				var menu = a.ReadGumpDialogWithStrings(Wait);
				a.SendGumpResponse(menu.GumpUid, Invite);
				menu = a.ReadGumpDialogWithStrings(Wait);
				a.SendGumpResponse(menu.GumpUid, 0);
				b.SendPartyAcceptInvite(aUid);
				Thread.Sleep(300);
				Assert.Contains("members=2", this.Report(b));

				int mark = this.fixture.GameOutputSnapshot().Length;
				for (byte step = 0; step < 3; step++) {
					a.SendMoveRequest(2, false, step);
					Thread.Sleep(250);
				}
				this.Ask(a, ".TestPartyHurt", "PARTY_HURT:");
				Thread.Sleep(500);
				Assert.DoesNotContain("CRITICAL", this.fixture.GameOutputSnapshot().Substring(mark));
				Assert.NotNull(b.ReadPacketPayload(0xA1, Wait));
			}
		}

		private static void Has(List<string> strings, string text) {
			Assert.True(strings.Exists(s => s.Contains(text)), "No '" + text + "' in: " + string.Join(" | ", strings));
		}

		private static void Hasnt(List<string> strings, string text) {
			Assert.False(strings.Exists(s => s.Contains(text)), "'" + text + "' in: " + string.Join(" | ", strings));
		}

		private void SayParty(TestGameClient client) {
			client.SendSpeech(".party");
		}

		private uint Ready(TestGameClient client, string realm) {
			string line = this.Ask(client, ".TestPartyReady " + realm, "PARTY_READY:");
			int start = line.IndexOf("uid=", StringComparison.Ordinal) + 4;
			return uint.Parse(line.Substring(start, line.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
		}

		private string Report(TestGameClient client) {
			return this.Ask(client, ".TestPartyReport", "PARTY_REPORT:");
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
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					Thread.Sleep(500);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PartyDialogCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "PartyDialog";
	}
}
