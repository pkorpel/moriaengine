using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1405 - a GM's change to an NPCDef's list fields (arrays of defs and of strings with commas and spaces)
	/// is saved and loads back after a real restart: the parsed lists read the same before the save and after. Server
	/// side: TestSetNpcDefLists / TestReportNpcDefLists in scripts/Tests/NpcDefListsTests.cs.</summary>
	[Collection(NpcDefListsRestartCollection.Name)]
	public class NpcDefListsRestartTests {
		private const string Account = "npcdeflistsrestart";
		private const string CharName = "ListChanger";

		[Fact]
		public void AChangedListFieldSurvivesARestart() {
			string before;
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Login(fixture1, true)) {
					before = Ask(fixture1, client, ".TestSetNpcDefLists", "NPCDEFLISTS_REPORT:");
					Assert.Contains("loot=i_gold,7 ; i_apple", before);
					Assert.Contains("equip=i_dagger|5 7|0x21|Rabbit's knife ; {i_bardiche 1 i_halberd 2}|||", before);
					Assert.Contains("can=False True False", before);
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Login(fixture2, false)) {
					string after = Ask(fixture2, client, ".TestReportNpcDefLists", "NPCDEFLISTS_REPORT:");
					Assert.True(before == after, "before the restart:\n" + before + "\nafter:\n" + after);
				}
			}
		}

		private static TestGameClient Login(GameServerPairFixture fixture, bool create) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Account);
					string[] names = game.ReadCharacterList();
					if (create) {
						game.SendCreateCharacter(CharName);
					} else {
						int slot = Array.IndexOf(names, CharName);
						Assert.True(slot >= 0, "'" + CharName + "' is gone after the restart.");
						game.SendSelectCharacter(slot);
					}
					Thread.Sleep(500);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose();
					lastFailure = lastFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in as '" + CharName + "'.", lastFailure);
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string command, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(30)), "No " + marker + " after " + command + ":\n" + fixture.GameOutputSnapshot().Substring(mark));
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return ((end < 0) ? output.Substring(index) : output.Substring(index, end - index)).TrimEnd('\r');
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class NpcDefListsRestartCollection {
		public const string Name = "NpcDefListsRestart";
	}
}
