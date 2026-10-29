using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#822 - material-colored items (Colored*Def with Material + MaterialType, no Color line in
	/// the converted scripts) must have their material's hue on every boot of a world, not only on the
	/// boots where the save history happens to have recolored the defs. Three real boots: a fresh world,
	/// then two save + restart rounds. Server-side helper: scripts/Tests/MaterialColorTests.cs.</summary>
	[Collection(MaterialColorCollection.Name)]
	public class MaterialColorTests {
		private const string Account = "materialcolortest";
		private const string Password = "materialcolortest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		//color_o_mithril 098b (item_ore.def) - Erebor's weaponcreate(mithril)
		private const string MithrilSword = "i_sword_viking_mithril def=0x98b item=0x98b";
		//color_elven 07d6 - a wooden "mithril" weapon is elven wood: Erebor's weaponcreate(mithril,elven)
		private const string ElvenStaff = "i_staff_quarter_mithril def=0x7d6 item=0x7d6";

		[Fact]
		public void MaterialItemsHaveTheirMaterialHueOnEveryBoot() {
			string[] seen = new string[6];
			using (var fixture = new GameServerPairFixture()) {
				using (var client = Connect(fixture, "Color Checker")) {
					seen[0] = Ask(fixture, client, ".TestMaterialColor \"i_sword_viking_mithril\"", "MATERIAL_COLOR: i_sword");
					seen[1] = Ask(fixture, client, ".TestMaterialColor \"i_staff_quarter_mithril\"", "MATERIAL_COLOR: i_staff");
					Ask(fixture, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			for (int boot = 1; boot < 3; boot++) {
				using (var fixture = GameServerPairFixture.BootPreservingSaves()) {
					using (var client = Connect(fixture, "Color Rechecker")) {
						seen[boot * 2] = Ask(fixture, client, ".TestMaterialColor \"i_sword_viking_mithril\"", "MATERIAL_COLOR: i_sword");
						seen[boot * 2 + 1] = Ask(fixture, client, ".TestMaterialColor \"i_staff_quarter_mithril\"", "MATERIAL_COLOR: i_staff");
						Ask(fixture, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
					}
				}
			}
			string all = string.Join(" | ", seen);
			for (int i = 0; i < seen.Length; i++) {
				string expected = (i % 2 == 0) ? MithrilSword : ElvenStaff;
				Assert.True(seen[i].Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' on every boot: " + all);
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
			if (!fixture.WaitForGameOutputSince(marker, mark, Wait)) {
				string output = fixture.GameOutputSnapshot();
				Assert.Fail("no '" + marker + "' after " + speech + ", server said:\n" + output.Substring(Math.Min(mark, output.Length)));
			}
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
	public class MaterialColorCollection {
		public const string Name = "MaterialColorPair";
	}
}
