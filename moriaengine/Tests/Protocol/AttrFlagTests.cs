using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Coverage for the item ATTR flag core infrastructure (#215/#217) - the
	/// ItemAttributes enum/storage, ".def" "ATTR = x|y" parsing, and the dedicated D_AttrList
	/// checkbox dialog (reachable via the ".AttrList" target-cursor command, matching ".flip"'s
	/// own shape, per explicit user request for a dedicated dialog rather than scattered fields
	/// in the generic .info view).</summary>
	[Collection(AttrFlagTestCollection.Name)]
	public class AttrFlagTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public AttrFlagTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real ".def" "ATTR = attr_static|attr_magic" line (AttrFlagTest.def) parses
		/// correctly into ItemDef.AttributesDefault, and a freshly created instance's own
		/// Attributes gets seeded from it at creation time (Item.On_Create).</summary>
		[Fact]
		public void DefAttrLineParsesAndSeedsNewInstances() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrDefTester")) {
				client.SendSpeech(".TestAttrDefParsing");
				// ItemAttributes.ToString() lists flags in enum declaration order (attr_magic
				// before attr_static), not the order they appeared in the ".def" line.
				this.AssertSeen("ATTR_DEF_TEST: defDefault=attr_magic, attr_static instanceAttributes=attr_magic, attr_static hasStatic=True hasMagic=True hasNewbie=False", TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>SetAttribute/HasAttribute (the programmatic API #218-225's real behaviors
		/// will consume) correctly read/write the same underlying bitmask.</summary>
		[Fact]
		public void ProgrammaticToggleWorks() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrBoolTester")) {
				client.SendSpeech(".TestAttrProgrammaticToggle");
				this.AssertSeen("ATTR_BOOL_TEST: beforeAny=False afterToggle=True afterUntoggle=True", TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>#218: attr_newbie/attr_blessed items stay on the body (no fall), attr_cursed
		/// forces a fall even though it co-exists with nothing else protective here, attr_insured
		/// also protects but is consumed by the one death that used it, and a plain item with no
		/// flags falls normally.</summary>
		[Fact]
		public void DeathDropRespectsAttrFlags() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrDeathTester")) {
				client.SendSpeech(".TestDeathDropAttrItems");
				this.AssertSeen(
					"ATTR_DEATH_RESULT: newbieStayed=True blessedStayed=True cursedStayed=False insuredStayed=True plainStayed=False insuredConsumed=True",
					TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>Real end-to-end flow: ".AttrList" target cursor opens D_AttrList, submitting
		/// it with attr_newbie's and attr_static's checkboxes selected (and nothing else) must
		/// leave exactly those two set on the targeted item, no others.</summary>
		[Fact]
		public void AttrListDialogTogglesSelectedFlags() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrListTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestCreateAttrTestItem");
				this.AssertSeen("ATTR_ITEM_SETUP:", TimeSpan.FromSeconds(10));
				uint itemUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), "ATTR_ITEM_SETUP: uid=");

				client.SendSpeech(".AttrList");
				Thread.Sleep(300);
				client.SendTargetUid(itemUid);
				var (gumpUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				// D_AttrList's allFlags order: 0=identified,1=decay,2=newbie,...,10=static - ids
				// are 10+index, so attr_newbie=12, attr_static=20.
				client.SendGumpResponse(gumpUid, 1, new[] { 12, 20 }, null);
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportAttrTestItem " + itemUid);
				this.AssertSeenSince("ATTR_ITEM_REPORT:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_ITEM_REPORT:");
				Assert.Equal("ATTR_ITEM_REPORT: attributes=attr_newbie, attr_static", line);
			}
		}

		private void AssertSeen(string marker, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutput(marker, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private void AssertSeenSince(string marker, int since, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutputSince(marker, since, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx).TrimEnd('\r');
		}

		private static uint ExtractUidAfter(string output, string marker) {
			int idx = output.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
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
						login.SendLogin(Account, Password);
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

	[CollectionDefinition(AttrFlagTestCollection.Name, DisableParallelization = true)]
	public class AttrFlagTestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "AttrFlagTestPair";
	}
}
