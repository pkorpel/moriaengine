using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1240 - what the living world's protocol tests share (#1237): log in a new character, run a server-side check
	/// command and assert its "PREFIX_CHECK: name=True" lines. Each Fact logs in once: an account holds 5 characters.</summary>
	public abstract class LivingWorldTestBase {
		protected static readonly TimeSpan Wait = TimeSpan.FromSeconds(60);
		protected readonly GameServerPairFixture fixture;
		private readonly string account, checkPrefix;

		protected LivingWorldTestBase(GameServerPairFixture servers, string account, string checkPrefix) {
			this.fixture = servers;
			this.account = account;
			this.checkPrefix = checkPrefix;
		}

		private protected TestGameClient Login(string name) {
			Exception firstFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				int redirectPort = this.WaitForServerListingAndGetRedirectPort();
				var game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(this.account, this.account);
					game.ReadCharacterList();
					game.SendCreateCharacter(name);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose(); //the last test's character can still be leaving
					firstFailure = firstFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as " + name, firstFailure);
		}

		/// <summary>Says a command and waits for the marker it prints when done.</summary>
		private protected void Run(TestGameClient game, string command, string doneMarker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			game.SendSpeech(command);
			for (int i = 0; i < 600; i++) {
				string output = this.fixture.GameOutputSnapshot();
				if (output.IndexOf(doneMarker, mark, StringComparison.Ordinal) >= 0) {
					return;
				}
				Thread.Sleep(100);
			}
			throw new TimeoutException("No " + doneMarker + " after " + command + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		protected void AssertChecks(params string[] checks) {
			string output = this.fixture.GameOutputSnapshot();
			foreach (string check in checks) {
				string marker = this.checkPrefix + "_CHECK: " + check + "=";
				int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
				Assert.True(idx >= 0, "Check '" + check + "' never reported. Game output:\n" + output);
				Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0, "Check '" + check + "' failed. Game output:\n" + output);
			}
		}

		protected string LastLine(string marker) {
			string output = this.fixture.GameOutputSnapshot();
			int at = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(at >= 0, "No " + marker + ". Game output:\n" + output);
			int end = output.IndexOf('\n', at);
			return output.Substring(at, ((end < 0) ? output.Length : end) - at);
		}

		protected static int Number(string line, string name) {
			return int.Parse(Regex.Match(line, name + @"=(-?\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(this.account, this.account);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
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
}
