using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #411: Corpse.direction is a plain .ct Var (a statically-typed
	/// field with its own generated Save/LoadLine, NOT a tag) - a different code path from
	/// #407/EnumTagPersistenceTests.cs's tag-based fix. #407's Save-side change (every enum
	/// now writes "(Enum:Name)value", unconditionally, since output.WriteValue always boxes
	/// to object regardless of the caller's static field type) broke this path's generated
	/// LoadLine, which called ConvertTools.ParseByte(valueString) directly on what used to
	/// be a bare number. Confirms the fix (excluding enums from ClassTemplate/
	/// DecoratedClassesSaveImplementor's "simple direct-parse" codegen decision, forcing them
	/// through the generic ObjectSaver.Load dispatch instead) produces the same type-
	/// preserving save format for a genuinely different code path than the tag one.
	/// </summary>
	[Collection(CorpseDirectionPersistenceCollection.Name)]
	public class CorpseDirectionPersistenceTests {
		private const string Account = "corpsedirpersisttest";
		private const string Password = "corpsedirpersisttest";

		private readonly GameServerPairFixture fixture;

		public CorpseDirectionPersistenceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void CorpseDirectionSurvivesARealSave() {
			using (var client = this.ConnectLoginAndCreateCharacter("CorpseDirectionChecker")) {
				int killMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestKillNpcWithDirectionAndReportCorpse");
				Assert.True(this.fixture.WaitForGameOutputSince("CORPSE_DIRECTION_TEST:", killMark, TimeSpan.FromSeconds(10)));
				Assert.Contains("direction=SouthWest", LastLineContaining(this.fixture.GameOutputSnapshot(), "CORPSE_DIRECTION_TEST:"), StringComparison.Ordinal);

				int saveMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTriggerGlobalSave");
				Assert.True(this.fixture.WaitForGameOutputSince("TEST_SAVE: done", saveMark, TimeSpan.FromSeconds(30)));

				string savesRoot = Path.Combine(this.fixture.RepoRoot, "saves");
				string latestDir = Directory.GetDirectories(savesRoot)
					.SelectMany(Directory.GetDirectories)
					.OrderByDescending(Directory.GetLastWriteTimeUtc)
					.First();
				string thingsSav = File.ReadAllText(Path.Combine(latestDir, "things.sav"));

				Assert.Contains("direction=(Enum:Direction)5", thingsSav, StringComparison.OrdinalIgnoreCase);
			}
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

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CorpseDirectionPersistenceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CorpseDirectionPersistencePair";
	}
}
