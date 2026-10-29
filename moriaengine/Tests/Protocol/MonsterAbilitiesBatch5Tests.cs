using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #799 - monster abilities batch 5 (eye, golem, craft monster, duplicator, multishot,
	/// backstab, mass leap, warcry, dispel bomb). See scripts/Tests/MonsterAbilitiesBatch5Tests.cs
	/// for the server-side helpers; the backstab formula is also covered in the fast tier.
	/// </summary>
	[Collection(MonsterAbilitiesBatch5Collection.Name)]
	public class MonsterAbilitiesBatch5Tests {
		private const string Account = "monsterabilitiesbatchfivetest";
		private const string Password = "monsterabilitiesbatchfivetest";

		private readonly GameServerPairFixture fixture;

		public MonsterAbilitiesBatch5Tests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SwingAbilitiesWork() {
			string line = this.Run("BatchFiveSwing", ".TestBatch5SwingAbilities", "B5_SWING_TEST:");
			Has(line, "leeched=20");
			Has(line, "stamSapped=True");
			Has(line, "manaPaid=True");
			Has(line, "golemTurned=True");
			Has(line, "golemTargetsMe=True");
			Has(line, "bonusAsOther=0");
			Has(line, "craftsmanBonusOk=True");
			Has(line, "copied=True");
			Has(line, "sameKind=True");
			Has(line, "secondCopy=False"); //DupeCount = 1
			Has(line, "copyOfCopy=False"); //a copy never splits
			Has(line, "shot=True");
			Has(line, "shotHurt=True");
		}

		[Fact]
		public void TimedAbilitiesWork() {
			string line = this.Run("BatchFiveTimed", ".TestBatch5TimedAbilities", "B5_TIMED_TEST:");
			Has(line, "hiddenAtOnce=False");
			Has(line, "hidden=True");
			Has(line, "ambush=540"); //(1000 maxhits - 100 hit) x 3/5
			Has(line, "revealed=True");
			Has(line, "interrupted=True");
			Has(line, "hidesRightAfterInterrupt=False");
			Has(line, "marked=True");
			Has(line, "hops=1");
			Has(line, "leaperNear=True");
			Has(line, "shocked=True");
			Has(line, "shockedAgain=-1");
			Has(line, "dispelled=True");
			Has(line, "summonGone=True");
		}

		//character names may only hold letters and spaces - a digit gets the client dropped
		private string Run(string charName, string command, string marker) {
			using (var client = this.ConnectLoginAndCreateCharacter(charName)) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTeleportToOpenGround");
				Thread.Sleep(500);
				client.SendSpeech(command);
				Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(10)),
					"no " + marker + " line - server output:\n" + this.fixture.GameOutputSnapshot());
				return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
			}
		}

		//the whole result line in the failure message - Assert.Contains truncates it
		private static void Has(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "expected '" + expected + "' in: " + line);
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
			throw new TimeoutException("Login server never listed a game server.", lastFailure);
		}
	}

	[CollectionDefinition(MonsterAbilitiesBatch5Collection.Name, DisableParallelization = true)]
	public class MonsterAbilitiesBatch5Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterAbilitiesBatch5Pair";
	}
}
