using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Professions system (#134-138): the real-GM .profession assignment command + starting-
	/// skill grant (#135), and weapon/armor-by-profession equip restriction (#138).
	/// </summary>
	[Collection(ProfessionCollection.Name)]
	public class ProfessionTests {
		// Account is the server's literal first-ever account, which AbstractAccount.
		// HandleLoginAttempt auto-elevates to MaximalPlevel permanently (accounts.Count == 1 at
		// the moment the ACCOUNT itself was created, not per-character) - every character ever
		// created under it, including ones that never call TestPromoteToGM, is already a GM.
		// Real finding while debugging this file: that silently made ProfessionPlugin.
		// On_DenyEquip's own IsGM bypass skip the restriction entirely for every equip test on
		// this account, with no error of any kind (equip just never completes, see
		// GameCore/ThingManipulationTriggers.cs's own fallthrough to TryPutItemOnItem when the
		// cascade never reaches item.MakeLimbo()). AccountB is a normal, non-elevated account
		// for the two equip-restriction Facts specifically, enabled via
		// TestEnableAutoAccountCreation (same pattern as SocialTests.cs/LevelingTests.cs).
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltest2";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public ProfessionTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Exercises the real .profession GM command end-to-end (not the TestSetProfession
		/// shortcut used by the other Facts here) - confirms the command itself is wired up and
		/// that ProfessionPlugin.On_Assign grants the .def's starting skill minimums (#135).
		/// Mage's Magery minimum is 300 (mage.def).</summary>
		[Fact]
		public void ProfessionCommandAssignsAndGrantsStartingSkills() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "ProfessionAssignee")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestPromoteToGM");
				Assert.True(this.fixture.WaitForGameOutputSince("PROMOTE_TO_GM:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportMyUid");
				Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
				uint selfUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "MY_UID: ");

				int profMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".Profession mag");
				Thread.Sleep(300);
				client.SendTargetUid(selfUid);
				Thread.Sleep(300);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckSkillValue \"Magery\"");
				Assert.True(this.fixture.WaitForGameOutputSince("PROFESSION_SKILL_VALUE:", mark, TimeSpan.FromSeconds(10)),
					"Did not see PROFESSION_SKILL_VALUE. Output since .Profession:\n" + this.fixture.GameOutputSnapshot().Substring(profMark));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "PROFESSION_SKILL_VALUE:");
				Assert.Contains("value=300", line);
			}
		}

		/// <summary>Weapon restriction (#138): a mage (magical-staves-only profession) can't
		/// equip a sword - the deny message arrives and the item stays unequipped. Real finding
		/// while building this: converted weapon content's WeaponType field is almost entirely a
		/// blanket Converter guess (OneHandSpike on nearly every weapon regardless of its real
		/// type, scripts/converted/zbrane/*.def, #157) - not fixed here. This test
		/// exploits that fact rather than fighting it: it still proves the restriction mechanism
		/// itself correctly denies whatever WeaponType a real equipped item reports.</summary>
		[Fact]
		public void MageCannotEquipASword() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageSwordTester")) {
				int setMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession profession_mage");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", setMark, TimeSpan.FromSeconds(10)));
				Assert.Contains("profession=profession_mage", LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_PROFESSION_SET:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportMyUid");
				Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
				uint selfUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "MY_UID: ");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantItem i_sword_viking_copper");
				Assert.True(this.fixture.WaitForGameOutputSince("PROFESSION_ITEM_GRANTED:", mark, TimeSpan.FromSeconds(10)));
				uint itemUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "PROFESSION_ITEM_GRANTED: uid=");

				client.SendPickUpItem(itemUid, 1);
				Thread.Sleep(200);
				client.SendDropToWearItem(itemUid, selfUid);
				Thread.Sleep(200);

				string denyMessage = ReadSystemMessageSkippingWelcome(client);
				Assert.Equal("Your profession does not allow you to use this weapon.", denyMessage);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckItemEquipped " + itemUid);
				Assert.True(this.fixture.WaitForGameOutputSince("PROFESSION_ITEM_EQUIPPED:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("equipped=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "PROFESSION_ITEM_EQUIPPED:"));
			}
		}

		/// <summary>Sibling of MageCannotEquipASword - a thief's allowedWeaponTypes includes
		/// OneHandSpike (which the same sword item reports, see that test's own doc comment),
		/// so the identical equip attempt should succeed with no deny message.</summary>
		[Fact]
		public void ThiefCanEquipASword() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "ThiefSwordTester")) {
				int setMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession profession_thief");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", setMark, TimeSpan.FromSeconds(10)));
				Assert.Contains("profession=profession_thief", LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_PROFESSION_SET:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportMyUid");
				Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
				uint selfUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "MY_UID: ");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantItem i_sword_viking_copper");
				Assert.True(this.fixture.WaitForGameOutputSince("PROFESSION_ITEM_GRANTED:", mark, TimeSpan.FromSeconds(10)));
				uint itemUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "PROFESSION_ITEM_GRANTED: uid=");

				int equipMark = this.fixture.GameOutputSnapshot().Length;
				client.SendPickUpItem(itemUid, 1);
				Thread.Sleep(200);
				client.SendDropToWearItem(itemUid, selfUid);
				Thread.Sleep(200);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckItemEquipped " + itemUid);
				Assert.True(this.fixture.WaitForGameOutputSince("PROFESSION_ITEM_EQUIPPED:", mark, TimeSpan.FromSeconds(10)));
				string equippedLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "PROFESSION_ITEM_EQUIPPED:");
				Assert.True(equippedLine.Contains("equipped=True"),
					"Expected equipped=True, got: " + equippedLine +
					"\nDiagnostic output since pickup attempt:\n" + this.fixture.GameOutputSnapshot().Substring(equipMark));
			}
		}

		/// <summary>Flips Globals.AutoAccountCreation on (server-wide, via an already-existing
		/// account's connection - "Test"-prefixed commands bypass CommandsAuthorization
		/// entirely, so this works regardless of plevel) so AccountB's first-ever login below
		/// can succeed. Same pattern as SocialTests.cs/LevelingTests.cs's own non-first
		/// accounts. Idempotent - harmless to call more than once across Facts.</summary>
		private void EnsureAutoAccountCreationEnabled() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
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

		/// <summary>The DelayedResyncTimer's "Welcome to {shard}." broadcast (see
		/// GameCore/Networking/PacketSequences.cs) can land in the stream ahead of this test's
		/// own deny message, depending on exactly when its timer fires relative to this test's
		/// own commands - skips any such unrelated message rather than asserting on whichever
		/// one happens to arrive first.</summary>
		private static string ReadSystemMessageSkippingWelcome(TestGameClient client) {
			for (int attempt = 0; attempt < 3; attempt++) {
				string message = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				if (!message.StartsWith("Welcome to", StringComparison.Ordinal)) {
					return message;
				}
			}
			throw new TimeoutException("Only ever saw welcome-shard messages, never the expected deny message.");
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

		private static uint ExtractMarkedUid(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	[CollectionDefinition(ProfessionCollection.Name, DisableParallelization = true)]
	public class ProfessionCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ProfessionPair";
	}
}
