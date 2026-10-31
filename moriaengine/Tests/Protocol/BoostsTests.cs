using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#923 - .boosts by real clients, what it shows by real spells. The test client is 7.0.15.1:
	/// this can't say how a 3.0.6m client draws it.</summary>
	[Collection(BoostsCollection.Name)]
	public class BoostsTests {
		private const string Account = "boostsviewer", AccountB = "boostsother";
		private const int Refresh = 1, Compact = 2, Position = 3, Live = 4;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public BoostsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void WhatItListsComesFromTheEffectsThemselves() {
			using var client = this.Connect("Boost Rules", Account, Account);
			string result = this.Ask(client, ".TestBoostsRules", "BOOSTS_RULES:", 60);
			Assert.True(result.Contains("fails=0"), result + "\n" + string.Join("\n", Failures(this.fixture.GameOutputSnapshot())));
		}

		private static List<string> Failures(string output) {
			List<string> found = new List<string>();
			foreach (string line in output.Split('\n')) {
				if (line.Contains("BOOSTS_FAIL:") || line.Contains("ERROR: (<command>")) {
					found.Add(line);
				}
			}
			return found;
		}

		[Fact]
		public void ThePlayersDialog() {
			using var client = this.Connect("Boost Viewer", Account, Account);
			this.Ask(client, ".TestBoostsLiveDefault \"0\"", "BOOSTS_DEFAULT:");
			this.Ask(client, ".TestBoostsPlain", "BOOSTS_PLAIN:");

			//a plain player can open it, and sees there's nothing
			client.SendSpeech(".boosts");
			var menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Nic na tebe nepusobi.");
			Has(menu.Strings, "Zive: ne");

			//it's a snapshot: Obnovit shows what happened since
			this.Ask(client, ".TestBoostsBless", "BOOSTS_BLESSED:");
			client.SendGumpResponse(menu.GumpUid, Refresh);
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Pozitivni vlivy");
			Has(menu.Strings, "Bless");
			Has(menu.Strings, " Str, ");

			//the strip, remembered
			client.SendGumpResponse(menu.GumpUid, Compact);
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Seznam");
			Hasnt(menu.Strings, "Pozitivni vlivy");
			client.SendGumpResponse(menu.GumpUid, 0);
			Thread.Sleep(200);
			client.SendSpeech(".boosts");
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Seznam");
			client.SendGumpResponse(menu.GumpUid, Compact);
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Pozitivni vlivy");

			//Pozice: an old client doesn't tell where it was dragged
			client.SendGumpResponse(menu.GumpUid, Position);
			var position = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(position.GumpUid, 1, new Dictionary<int, string> { { 1, "120" }, { 2, "80" } });
			menu = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("x=120 y=80", this.Ask(client, ".TestBoostsState", "BOOSTS_STATE:"));

			//.boosts again closes it; a client answers that with button 0
			client.SendSpeech(".boosts");
			Thread.Sleep(300);
			Assert.Contains("open=True", this.Ask(client, ".TestBoostsState", "BOOSTS_STATE:"));
			client.SendGumpResponse(menu.GumpUid, 0);
			Thread.Sleep(300);
			Assert.Contains("open=False timer=False", this.Ask(client, ".TestBoostsState", "BOOSTS_STATE:"));
		}

		[Fact]
		public void LiveRedrawsAndTheAnswerToItsClosingIsNotThePlayerClosing() {
			using var client = this.Connect("Boost Live", Account, Account);
			this.Ask(client, ".TestBoostsLiveDefault \"0\"", "BOOSTS_DEFAULT:");
			this.Ask(client, ".TestBoostsPlain", "BOOSTS_PLAIN:");
			client.SendSpeech(".boosts");
			var menu = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(menu.GumpUid, Live);
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Zive: ano");
			Assert.Contains("open=True timer=True", this.Ask(client, ".TestBoostsState", "BOOSTS_STATE:"));

			//nothing changes, nothing is sent: the countdown's period is long here
			Thread.Sleep(2500);
			int first = menu.GumpUid;

			//an effect starts: a new dialog comes by itself
			this.Ask(client, ".TestBoostsBless", "BOOSTS_BLESSED:");
			var redrawn = client.ReadGumpDialogWithStrings(Wait);
			Has(redrawn.Strings, "Bless");
			Assert.NotEqual(first, redrawn.GumpUid);

			//the client answers the closing of the old one, which doesn't stop it
			client.SendGumpResponse(first, 0);
			Thread.Sleep(300);
			Assert.Contains("open=True timer=True", this.Ask(client, ".TestBoostsState", "BOOSTS_STATE:"));
			this.Ask(client, ".TestBoostsMoreStrength", "BOOSTS_STRONGER:");
			var again = client.ReadGumpDialogWithStrings(Wait);

			//the player closing it does
			client.SendGumpResponse(again.GumpUid, 0);
			Thread.Sleep(300);
			Assert.Contains("open=False timer=False", this.Ask(client, ".TestBoostsState", "BOOSTS_STATE:"));
			client.SendSpeech(".boosts");
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Zive: ano"); //a player's choice stays
			client.SendGumpResponse(menu.GumpUid, Live);
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Zive: ne");
			client.SendGumpResponse(menu.GumpUid, 0);
			Thread.Sleep(300);

			//the default is the server's until a player chooses
			this.Ask(client, ".TestBoostsLiveDefault \"1\"", "BOOSTS_DEFAULT:");
			this.Ask(client, ".TestBoostsPlain", "BOOSTS_PLAIN:");
			client.SendSpeech(".boosts");
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Zive: ano");
			client.SendGumpResponse(menu.GumpUid, 0);
			Thread.Sleep(300);
			this.Ask(client, ".TestBoostsLiveDefault \"0\"", "BOOSTS_DEFAULT:");
		}

		[Fact]
		public void AGmSeesAnothersOnlyAGm() {
			using var gm = this.Connect("Boost Master", Account, Account);
			this.Ask(gm, ".TestTownBecomeGm", "TOWN_GM:");
			this.Ask(gm, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var other = this.Connect("Boost Other", AccountB, Account);
			uint otherUid = Number(this.Ask(other, ".TestBoostsPlain", "BOOSTS_PLAIN:"), "uid=");
			this.Ask(other, ".TestBoostsBless", "BOOSTS_BLESSED:");
			gm.SendSpeech(".boostsof");
			Thread.Sleep(300);
			gm.SendTargetUid(otherUid);
			var menu = gm.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Boosty: Boost Other");
			Has(menu.Strings, "Bless");
			gm.SendGumpResponse(menu.GumpUid, 0);

			//a player can't
			int mark = this.fixture.GameOutputSnapshot().Length;
			other.SendSpeech(".boostsof");
			Thread.Sleep(500);
			Assert.DoesNotContain("BoostsOf'. OK", this.fixture.GameOutputSnapshot().Substring(mark));
		}

		private static void Has(List<string> strings, string text) {
			Assert.True(strings.Exists(s => s.Contains(text)), "No '" + text + "' in: " + string.Join(" | ", strings));
		}

		private static void Hasnt(List<string> strings, string text) {
			Assert.False(strings.Exists(s => s.Contains(text)), "'" + text + "' in: " + string.Join(" | ", strings));
		}

		private static uint Number(string line, string key) {
			int start = line.IndexOf(key, StringComparison.Ordinal) + key.Length;
			return uint.Parse(line.Substring(start, line.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
		}

		private string Ask(TestGameClient client, string command, string marker, int seconds = 10) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(seconds)), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient Connect(string charName, string account, string portAccount) {
			int redirectPort = TownTestsLogin.RedirectPort(portAccount);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
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
	public class BoostsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Boosts";
	}

	/// <summary>#923 - a live .boosts' timer in a real save: a timer that can't be saved aborts the whole
	/// world save.</summary>
	[Collection(BoostsRestartCollection.Name)]
	public class BoostsRestartTests {
		private const string Account = "boostsrestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void ASaveWithItRunningLoadsAndTheTimerGoes() {
			using (var first = new GameServerPairFixture()) {
				using var client = Connect("Boost Saver");
				Ask(first, client, ".TestBoostsPlain", "BOOSTS_PLAIN:");
				client.SendSpeech(".boosts");
				var menu = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(menu.GumpUid, 4);
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("timer=True", Ask(first, client, ".TestBoostsState", "BOOSTS_STATE:"));
				Ask(first, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				using var client = Connect("Boost Loader");
				Assert.Contains("Boost", Ask(second, client, ".TestBoostsPlain", "BOOSTS_PLAIN:") + "Boost");
				Assert.DoesNotContain("Loading failed", second.GameOutputSnapshot());
			}
		}

		private static TestGameClient Connect(string charName) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + speech + ":\n" + fixture.GameOutputSnapshot());
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class BoostsRestartCollection {
		public const string Name = "BoostsRestart";
	}
}
