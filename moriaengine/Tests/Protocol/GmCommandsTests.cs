using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Coverage for the GM ".kill" and ".flip" commands (#192, #193) -
	/// scripts/Basic/administration/Commands.cs. Both are single-use target-cursor commands,
	/// matching the existing ".Profession"/".Remove" pattern in that file.</summary>
	[Collection(GmCommandsCollection.Name)]
	public class GmCommandsTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public GmCommandsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>GM targets an NPC with ".kill" - it must die outright (Flag_Dead) regardless
		/// of its current hits, with a lightning effect/sound played (not directly observable
		/// over the protocol, but EffectFactory.LightningEffect/Sound are unconditional calls
		/// ahead of Character.Kill() in the command itself).</summary>
		[Fact]
		public void KillCommandKillsTargetedNpc() {
			using (var client = this.ConnectLoginAndCreateCharacter("GmKillTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupKillTarget");
				this.AssertSeen("KILL_SETUP:", mark, TimeSpan.FromSeconds(10));
				uint targetUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), mark, "KILL_SETUP: uid=");

				client.SendSpeech(".Kill");
				Thread.Sleep(300);
				client.SendTargetUid(targetUid);
				Thread.Sleep(500);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckDead " + targetUid);
				this.AssertSeen("KILL_TEST: dead=True", mark, TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>GM targets a multi-orientation wall piece (i_wall_stone_4, whose DUPEITEM
		/// flip group survived conversion) with ".flip" - its Model must change to the next one
		/// in the group (AbstractItemDef.GetNextFlipModel).</summary>
		[Fact]
		public void FlipCommandChangesItemModel() {
			using (var client = this.ConnectLoginAndCreateCharacter("GmFlipTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupFlippableItem");
				this.AssertSeen("FLIP_SETUP:", mark, TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "FLIP_SETUP:");
				uint itemUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), mark, "FLIP_SETUP: uid=");
				int originalModel = ExtractIntAfter(setupLine, "model=");

				client.SendSpeech(".Flip");
				Thread.Sleep(300);
				client.SendTargetUid(itemUid);
				Thread.Sleep(500);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckItemModel " + itemUid);
				this.AssertSeen("ITEM_CHECK:", mark, TimeSpan.FromSeconds(10));
				string checkLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ITEM_CHECK:");
				int newModel = ExtractIntAfter(checkLine, "model=");
				Assert.NotEqual(originalModel, newModel);
			}
		}

		/// <summary>GM targets i_wall_stone with ".flip" (#206) - a plain wall piece nothing
		/// else's DUPEITEM points back at, so Flip() is a genuine no-op. Must get a clear
		/// SysMessage instead of silently doing nothing.</summary>
		[Fact]
		public void FlipCommandOnNonFlippableItemShowsMessage() {
			using (var client = this.ConnectLoginAndCreateCharacter("GmFlipNoopTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupNonFlippableItem");
				this.AssertSeen("FLIP_SETUP:", mark, TimeSpan.FromSeconds(10));
				uint itemUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), mark, "FLIP_SETUP: uid=");

				client.SendSpeech(".Flip");
				client.ReadSystemMessage(TimeSpan.FromSeconds(10)); // "Target the item to flip."
				client.SendTargetUid(itemUid);

				string message = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				Assert.Equal("This item has no flip variants.", message);
			}
		}

		private void AssertSeen(string marker, int since, TimeSpan timeout) {
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
			return output.Substring(idx, end - idx);
		}

		private static uint ExtractUidAfter(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), CultureInfo.InvariantCulture);
		}

		private static int ExtractIntAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + line);
			string rest = line.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return int.Parse(rest.Substring(0, end), CultureInfo.InvariantCulture);
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

	[CollectionDefinition(GmCommandsCollection.Name, DisableParallelization = true)]
	public class GmCommandsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GmCommandsPair";
	}
}
