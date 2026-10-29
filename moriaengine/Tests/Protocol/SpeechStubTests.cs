using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#825 - every Erebor archive [SPEECH] block is a [SpeechStub] placeholder (converter output): TSPEECH
	/// references resolve, ported speeches (banker, shopkeeper) win over their stub, and saying a phrase that matches a
	/// stub's pattern next to an NPC carrying it tells a GM which unported speech it hit - and tells a player nothing.
	/// Real speech packets from a real client. Server-side helpers: scripts/Tests/SpeechStubTests.cs.</summary>
	[Collection(SpeechStubCollection.Name)]
	public class SpeechStubTests {
		private const string Account = "speechstubtest";
		private const string Password = "speechstubtest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);
		private const string Hint = "[unported speech spk_kecy_a_drby on ";

		[Fact]
		public void ArchiveSpeechesArePlaceholdersThatHintGmsOnly() {
			using (var fixture = new GameServerPairFixture()) {
				using (var client = Connect(fixture, "Speech Tester")) {
					string report = Ask(fixture, client, ".TestSpeechStubReport", "SPEECH_STUB_REPORT:");
					//252 archive blocks: 3 ported in C#, spk_vendor_custom is the LScript one in scripts/Optional/PlayerVendor
					AssertHas(report, "stubs=248 chatter=77 first=*Vypravec* defHasIt=True banker=BankerSpeech shopkeep=ShopkeeperSpeech vendorCustom=ScriptedSpeech guard=GuardSpeech");

					Ask(fixture, client, ".TestSpawnSpeechStubListener", "SPEECH_STUB_LISTENER:");
					client.SendSpeech("hello there");
					Assert.Null(TryReadMessageContaining(client, Hint, TimeSpan.FromSeconds(3)));
					client.SendSpeech("hey vypravec");
					string hint = TryReadMessageContaining(client, Hint, TimeSpan.FromSeconds(10));
					Assert.NotNull(hint);
					AssertHas(hint, ": *Vypravec*]");

					AssertHas(Ask(fixture, client, ".TestSpeechStubBecomePlayer", "SPEECH_STUB_PLAYER:"), "gm=False");
					client.SendSpeech("hey vypravec");
					Assert.Null(TryReadMessageContaining(client, Hint, TimeSpan.FromSeconds(3)));
				}
			}
		}

		private static string TryReadMessageContaining(TestGameClient client, string text, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (true) {
				TimeSpan remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero) {
					return null;
				}
				string message;
				try {
					message = client.ReadSystemMessage(remaining);
				} catch (TimeoutException) {
					return null;
				}
				if (message.Contains(text, StringComparison.Ordinal)) {
					return message;
				}
			}
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(fixture, Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Password);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "no '" + marker + "' after " + speech);
			return LastLineContaining(fixture.GameOutputSnapshot(), marker);
		}

		private static void AssertHas(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture, string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);

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

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SpeechStubCollection {
		public const string Name = "SpeechStubPair";
	}
}
