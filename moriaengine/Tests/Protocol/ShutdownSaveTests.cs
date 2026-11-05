using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1173 - a deliberate shutdown saves the world first: .exit, and a line typed at the server's own console. A save that
	/// fails only warns and the server still goes down; .exitnosave goes down without a save. A mark on the character, set before
	/// each shutdown and read after the next boot, shows what was saved. Server side: scripts/Tests/ShutdownSaveTests.cs.</summary>
	[Collection(ShutdownSaveCollection.Name)]
	public class ShutdownSaveTests {
		private const string Account = "shutdownsave";
		private const string CharName = "Shutdown Saver";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan ExitWait = TimeSpan.FromSeconds(60);
		private static readonly Regex ProblemRE = new Regex(@"\b(ERROR|WARNING|CRITICAL|FATAL):");

		[Fact]
		public void ShuttingDownSavesUnlessToldNotTo() {
			//1. .exit saves: the autosave interval comes from steamengine.ini, and a GM can see when it's due
			string first, nextSave, firstExit;
			using (var fixture = new GameServerPairFixture()) {
				using (var client = Connect(Account, CharName)) {
					first = Ask(fixture, client, ".TestShutdownReport", "SHUTDOWN_SAVE: mark=");
					int mark = fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".nextsave");
					nextSave = ReadMessage(fixture, mark, client, "Next save in");
					//a plain GM (plevel 4) can't shut the server down, not even through a nested call (#406 checks only the
					//first word: .serv.exitnosave used to reach Globals.ExitNoSave at plevel 4)
					Ask(fixture, client, ".TestShutdownSetMaxPlevel 4", "SHUTDOWN_SAVE: maxPlevel=4");
					foreach (string code in new[] { "exitnosave", "serv.exitnosave", "show exitnosave", "exit", "serv.exit" }) {
						StaysUp(fixture, client, code);
					}
					Ask(fixture, client, ".TestShutdownSetMaxPlevel 7", "SHUTDOWN_SAVE: maxPlevel=7");
					Ask(fixture, client, ".TestShutdownMark \"exit\"", "SHUTDOWN_SAVE: marked");
					firstExit = ExitBy(fixture, () => client.SendSpeech(".exit"));
				}
			}
			Assert.Contains("mark=none", first);
			Assert.Equal("60", Value(first, "autosaveMinutes"));
			Assert.Matches(@"^Next save in 0:5\d:\d\d, interval is 60 minutes$", nextSave);
			Assert.Contains("Saving world data", firstExit);
			Assert.DoesNotContain("the world is NOT saved", firstExit);

			//2. saves that fail. One whose writes fail leaves a half-written folder: it's set aside (*.failed), so no boot tries
			//it, and the autosave timer is set again for the next try. One that can't even make its folder: the same timer,
			//and .exit only warns and goes down anyway.
			string second, afterFailedWrites, setAside, afterFailedFolder, secondExit, brokenSaveFile;
			using (var fixture = GameServerPairFixture.BootPreservingSaves()) {
				List<string> problems = ProblemsAtBoot(fixture);
				Assert.True(problems.Count == 0, problems.Count + " at boot:\n" + string.Join("\n", problems));
				using (var client = ConnectAs(Account, CharName)) {
					second = Ask(fixture, client, ".TestShutdownReport", "SHUTDOWN_SAVE: mark=");
					Ask(fixture, client, ".TestShutdownMark \"failed save\"", "SHUTDOWN_SAVE: marked");
					Ask(fixture, client, ".TestShutdownBreakSaveWrites", "SHUTDOWN_SAVE: next save's writes broken");
					SaveAndWait(fixture, client);
					afterFailedWrites = Ask(fixture, client, ".TestShutdownReport", "SHUTDOWN_SAVE: mark=");
					setAside = Ask(fixture, client, ".TestShutdownFailedSaveReport", "SHUTDOWN_SAVE: failed save");
					brokenSaveFile = Value(Ask(fixture, client, ".TestShutdownBreakSaves", "SHUTDOWN_SAVE: saves broken"), "file");
					SaveAndWait(fixture, client);
					afterFailedFolder = Ask(fixture, client, ".TestShutdownReport", "SHUTDOWN_SAVE: mark=");
					secondExit = ExitBy(fixture, () => client.SendSpeech(".exit"));
				}
			}
			System.IO.File.Delete(brokenSaveFile);
			Assert.Contains("mark=exit", second);
			Assert.Contains("autosaveTimer=True", second);
			Assert.Contains("autosaveTimer=True", afterFailedWrites);
			Assert.Contains("setAside=True", setAside);
			Assert.Contains("autosaveTimer=True", afterFailedFolder);
			Assert.Contains("The shutdown save failed, the world is NOT saved. Shutting down anyway.", secondExit);

			//3. .exitnosave: no save
			string third, thirdExit;
			using (var fixture = GameServerPairFixture.BootPreservingSaves()) {
				List<string> problems = ProblemsAtBoot(fixture); //the half-written folder of step 2 isn't tried
				Assert.True(problems.Count == 0, problems.Count + " at boot:\n" + string.Join("\n", problems));
				using (var client = ConnectAs(Account, CharName)) {
					third = Ask(fixture, client, ".TestShutdownReport", "SHUTDOWN_SAVE: mark=");
					Ask(fixture, client, ".TestShutdownMark \"no save\"", "SHUTDOWN_SAVE: marked");
					thirdExit = ExitBy(fixture, () => client.SendSpeech(".exitnosave"));
				}
			}
			Assert.Contains("mark=exit", third); //the failed saves wrote nothing
			Assert.Contains("Exiting without saving the world", thirdExit);
			Assert.DoesNotContain("Saving world data", thirdExit);

			//4. a line at the server's own console saves, as .exit
			string fourth;
			using (var fixture = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = ConnectAs(Account, CharName)) {
					fourth = Ask(fixture, client, ".TestShutdownReport", "SHUTDOWN_SAVE: mark=");
					Ask(fixture, client, ".TestShutdownMark \"console\"", "SHUTDOWN_SAVE: marked");
				}
				string consoleExit = ExitBy(fixture, () => fixture.SendGameConsoleLine(""));
				Assert.Contains("Saving world data", consoleExit);
			}
			Assert.Contains("mark=exit", fourth); //.exitnosave saved nothing

			string fifth;
			using (var fixture = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = ConnectAs(Account, CharName)) {
					fifth = Ask(fixture, client, ".TestShutdownReport", "SHUTDOWN_SAVE: mark=");
				}
			}
			Assert.Contains("mark=console", fifth);
		}

		/// <summary>A command the account may not run: it's refused and the server stays up, with nothing saved.</summary>
		private static void StaysUp(GameServerPairFixture fixture, TestGameClient client, string code) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech("." + code);
			Assert.True(fixture.WaitForGameOutputSince("commands '" + code + "'", mark, Wait),
				"No end of ." + code + ":\n" + fixture.GameOutputSnapshot().Substring(mark));
			Thread.Sleep(1000);
			string output = fixture.GameOutputSnapshot().Substring(mark);
			Assert.False(fixture.GameProcessHasExited, "." + code + " shut the server down at plevel 4:\n" + output);
			Assert.DoesNotContain("Exiting without saving", output);
			Assert.DoesNotContain("Saving world data", output);
		}

		/// <summary>A GM's .save, and waits for its end (it fails here, which a client only sees as a broadcast).</summary>
		private static void SaveAndWait(GameServerPairFixture fixture, TestGameClient client) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".save");
			Assert.True(fixture.WaitForGameOutputSince("commands 'save'", mark, Wait),
				"No end of the .save:\n" + fixture.GameOutputSnapshot().Substring(mark));
		}

		/// <summary>Does what shuts the server down, waits for it to go, and returns its output from then on.</summary>
		private static string ExitBy(GameServerPairFixture fixture, Action shutDown) {
			int mark = fixture.GameOutputSnapshot().Length;
			shutDown();
			Assert.True(fixture.WaitForGameExit(ExitWait), "GameCore didn't go down. Output:\n" + fixture.GameOutputSnapshot().Substring(mark));
			Assert.Equal(0, fixture.GameProcessExitCode);
			return fixture.GameOutputSnapshot().Substring(mark);
		}

		private static string ReadMessage(GameServerPairFixture fixture, int mark, TestGameClient client, string start) {
			DateTime until = DateTime.UtcNow + Wait;
			List<string> seen = new List<string>();
			try {
				while (true) {
					string message = client.ReadSystemMessage(until - DateTime.UtcNow);
					if (message.StartsWith(start, StringComparison.Ordinal)) {
						return message;
					}
					seen.Add(message);
				}
			} catch (TimeoutException) {
				Assert.Fail("No message starting '" + start + "'. Seen: [" + string.Join("] [", seen) + "]. Server output:\n" +
					fixture.GameOutputSnapshot().Substring(mark));
				throw;
			}
		}

		private static string Value(string line, string key) {
			Match m = Regex.Match(line, @"\b" + key + @"=(\S+)");
			Assert.True(m.Success, "No " + key + "= in: " + line);
			return m.Groups[1].Value;
		}

		private static List<string> ProblemsAtBoot(GameServerPairFixture fixture) {
			string output = fixture.GameOutputSnapshot();
			int end = output.IndexOf("Init done.", StringComparison.Ordinal);
			List<string> problems = new List<string>();
			foreach (string line in output.Substring(0, end).Split('\n')) {
				if (ProblemRE.IsMatch(line)) {
					problems.Add(line.Trim());
				}
			}
			return problems;
		}

		private static TestGameClient Connect(string account, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(account, account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		/// <summary>Logs in as a character the account already has.</summary>
		private static TestGameClient ConnectAs(string account, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(account, account);
			int index = Array.IndexOf(client.ReadCharacterList(), charName);
			Assert.True(index >= 0, "No character " + charName + " on " + account);
			client.SendSelectCharacter(index);
			Thread.Sleep(1500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "No '" + marker + "' after " + speech);
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private static int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
						if (login.ReadServersList() < 1) {
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
	public class ShutdownSaveCollection {
		public const string Name = "ShutdownSave";
	}
}
