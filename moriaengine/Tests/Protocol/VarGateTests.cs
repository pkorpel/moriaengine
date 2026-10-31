using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#920 - i_varGate: its rules by real steps on the server, its dialogs by a real client.</summary>
	[Collection(VarGateCollection.Name)]
	public class VarGateTests {
		private const string Account = "vargatetest";
		private const int Ok = 1, New = 2, Position = 2, Remove = 4, Add = 5, Function = 6, Guards = 7, Plugins = 8, Edit = 100, Delete = 1000;
		private const int NpcFlag = 1, ExpFlag = 8, LevelFlag = 0x10, TimeLimitCheck = 1, CheckOnly = 11, SameRealmCheck = 1;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public VarGateTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryRuleWalkedByARealStep() {
			using var client = this.Connect("Gate Walker");
			string result = this.Ask(client, ".TestVarGateRules", "VARGATE_RULES:");
			string output = this.fixture.GameOutputSnapshot();
			Assert.True(result.Contains("fails=0"), result + "\n" + string.Join("\n", Failures(output)));
		}

		private static List<string> Failures(string output) {
			List<string> found = new List<string>();
			foreach (string line in output.Split('\n')) {
				if (line.Contains("VARGATE_FAIL:") || line.Contains("ERROR")) {
					found.Add(line);
				}
			}
			return found;
		}

		[Fact]
		public void TheDialogsSetItAll() {
			using var client = this.Connect("Gate Keeper");
			this.Ask(client, ".TestTownBecomeGm", "");
			string placed = this.Ask(client, ".TestVarGateForDialog", "VARGATE_PLACED:");
			uint gate = Number(placed, "gate=");

			//a double click opens it; cancelling keeps nothing
			client.SendDoubleClick(gate);
			var menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Variable Gate");
			client.SendGumpResponse(menu.GumpUid, 0, new[] { NpcFlag }, null);
			Thread.Sleep(300);
			Assert.Contains("flags=0 moreP=False", this.Ask(client, ".TestVarGateReport", "VARGATE_REPORT:"));

			//the checkboxes and the entries are kept by any button
			client.SendDoubleClick(gate);
			menu = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(menu.GumpUid, Position, new[] { NpcFlag, ExpFlag, LevelFlag }, new Dictionary<int, string> {
				{ 100, "500,30" }, { 101, "10,20" }, { 102, "4,5" }, { 103, "5,10" }
			});
			menu = client.ReadGumpDialogWithStrings(Wait);
			string report = this.Ask(client, ".TestVarGateReport", "VARGATE_REPORT:");
			Assert.Contains("flags=" + (NpcFlag | ExpFlag | LevelFlag) + " moreP=True exp=500,30 expLevel=10,20 decay=4,5 level=5,10", report);

			//the remove list: a bad name is refused and the dialog stays
			client.SendGumpResponse(menu.GumpUid, Remove, new[] { NpcFlag }, null);
			var list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, New);
			var entry = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(entry.GumpUid, Ok, new Dictionary<int, string> { { 1, "i_no_such_thing" }, { 2, "3" }, { 3, "0" } });
			entry = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(entry.GumpUid, Ok, new[] { 10, 11 }, new Dictionary<int, string> { { 1, "i_gold" }, { 2, "3" }, { 3, "0" } });
			list = client.ReadGumpDialogWithStrings(Wait);
			Has(list.Strings, "i_gold");
			Has(list.Strings, "3x item jdi kontrola");

			//edited, then a second line deleted
			client.SendGumpResponse(list.GumpUid, Edit);
			entry = client.ReadGumpDialogWithStrings(Wait);
			Has(entry.Strings, "i_gold");
			client.SendGumpResponse(entry.GumpUid, Ok, new Dictionary<int, string> { { 1, "i_dagger" }, { 2, "2" }, { 3, "0" } });
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, New);
			entry = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(entry.GumpUid, Ok, new Dictionary<int, string> { { 1, "i_gold" }, { 2, "1" }, { 3, "0" } });
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, Delete + 1);
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, Ok);
			menu = client.ReadGumpDialogWithStrings(Wait);
			Has(menu.Strings, "Pocet Itemu/Eventu: 1");

			//the add list, with its time limit, which must be positive
			client.SendGumpResponse(menu.GumpUid, Add, new[] { NpcFlag }, null);
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, Ok, new[] { TimeLimitCheck }, new Dictionary<int, string> { { 1, "0" } });
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, New, new[] { TimeLimitCheck }, new Dictionary<int, string> { { 1, "90" } });
			entry = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(entry.GumpUid, Ok, new Dictionary<int, string> { { 1, "i_gold" }, { 2, "0" }, { 3, "0" } });
			entry = client.ReadGumpDialogWithStrings(Wait); //an amount of 0 adds nothing, refused
			client.SendGumpResponse(entry.GumpUid, Ok, new Dictionary<int, string> { { 1, "i_gold" }, { 2, "7" }, { 3, "0" } });
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, Ok, new[] { TimeLimitCheck }, new Dictionary<int, string> { { 1, "90" } });
			menu = client.ReadGumpDialogWithStrings(Wait);

			//the function list: it must exist
			client.SendGumpResponse(menu.GumpUid, Function, new[] { NpcFlag }, null);
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, New);
			entry = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(entry.GumpUid, Ok, new Dictionary<int, string> { { 1, "f_no_such_function" } });
			entry = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(entry.GumpUid, Ok, new[] { CheckOnly }, new Dictionary<int, string> { { 1, "f_vg_mountDeny" } });
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, Ok);
			menu = client.ReadGumpDialogWithStrings(Wait);

			//guards, by target: only a spawn will do
			client.SendGumpResponse(menu.GumpUid, Guards, new[] { NpcFlag }, null);
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, New);
			Thread.Sleep(300);
			client.SendTargetUid(Number(placed, "notGem="));
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, New);
			Thread.Sleep(300);
			client.SendTargetUid(Number(placed, "gem="));
			list = client.ReadGumpDialogWithStrings(Wait);
			Has(list.Strings, "Spawn 0");
			client.SendGumpResponse(list.GumpUid, Ok);
			menu = client.ReadGumpDialogWithStrings(Wait);

			//the Time Seal
			client.SendGumpResponse(menu.GumpUid, Plugins, new[] { NpcFlag }, null);
			list = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(list.GumpUid, New);
			var seal = client.ReadGumpDialogWithStrings(Wait);
			Has(seal.Strings, "VarGate's Time Seal plugin");
			client.SendGumpResponse(seal.GumpUid, Ok, new Dictionary<int, string> { { 1, "0" }, { 2, "5" }, { 3, "2" } });
			seal = client.ReadGumpDialogWithStrings(Wait); //a seal of no length is refused
			client.SendGumpResponse(seal.GumpUid, 2, new Dictionary<int, string> { { 1, "30" }, { 2, "5" }, { 3, "2" } });
			seal = client.ReadGumpDialogWithStrings(Wait); //help
			Assert.Contains("Doba zablokovani", string.Join(" ", seal.Strings));
			client.SendGumpResponse(seal.GumpUid, Ok, new[] { SameRealmCheck }, new Dictionary<int, string> { { 1, "30" }, { 2, "5" }, { 3, "2" } });
			list = client.ReadGumpDialogWithStrings(Wait);
			Has(list.Strings, "Time Seal");
			client.SendGumpResponse(list.GumpUid, Ok);
			menu = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(menu.GumpUid, Ok, new[] { NpcFlag }, null);
			Thread.Sleep(300);

			report = this.Ask(client, ".TestVarGateReport", "VARGATE_REPORT:");
			Assert.Contains("seconds=90 remove=i_dagger:2:0:False:False add=i_gold:7:0:False:False functions=f_vg_mountDeny:0:0:False:True guards=1 seal=True,30,5,2,True", report);
			Assert.Contains("flags=" + (NpcFlag | 0x8000) + " ", report);
		}

		private static void Has(List<string> strings, string text) {
			Assert.True(strings.Exists(s => s.Contains(text)), "No '" + text + "' in: " + string.Join(" | ", strings));
		}

		private static uint Number(string line, string key) {
			int start = line.IndexOf(" " + key, StringComparison.Ordinal) + key.Length + 1;
			return uint.Parse(line.Substring(start, line.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			if (marker.Length == 0) {
				Thread.Sleep(300);
				return "";
			}
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(60)), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient Connect(string charName) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Account);
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
	public class VarGateCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "VarGate";
	}

	/// <summary>#920 - a gate with everything in use survives a real save and restart; the first thing a
	/// new saved field can break is the whole world save.</summary>
	[Collection(VarGateRestartCollection.Name)]
	public class VarGateRestartTests {
		private const string Account = "vargaterestart";

		[Fact]
		public void AFullGateSurvivesARestart() {
			using (var first = new GameServerPairFixture()) {
				using var client = Connect("Gate Saver");
				Ask(first, client, ".TestVarGateForSave", "VARGATE_SAVED:");
				Ask(first, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				using var client = Connect("Gate Loader");
				string loaded = Ask(second, client, ".TestVarGateAfterRestart", "VARGATE_LOADED:");
				Assert.Contains("exp=500,30", loaded);
				Assert.Contains("seconds=1000 remove=i_gold:3:0:True:True add=i_dagger:1:0:False:False functions=f_vg_mountDeny:0:0:False:True guards=1 seal=True,30,1000,2,True", loaded);
				string state = Ask(second, client, ".TestVarGateAfterRestart", "VARGATE_LOADED_STATE:");
				Assert.Contains("seals=1 records=2 nonMovable=True guardSurvived=True", state);
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
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(30)), "No " + marker + " after " + speech + ":\n" + fixture.GameOutputSnapshot());
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class VarGateRestartCollection {
		public const string Name = "VarGateRestart";
	}
}
