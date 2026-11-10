using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#918 - looting a corpse, by two real clients: a crime only from a player of one's own
	/// realm, where that realm's switch is on, and not with the owner's leave in a party.</summary>
	[Collection(LootingCrimeCollection.Name)]
	public class LootingCrimeTests {
		private const string AccountA = "lootcrimea", AccountB = "lootcrimeb";
		private const int Invite = 300, LootSwitch = 4, Leave = 2;
		private const string Crime = "took=True criminal=True", NoCrime = "took=True criminal=False";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;
		private uint pack;

		public LootingCrimeTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void OnlyAPlayerOfOnesOwnRealmWithTheSwitchOn() {
			using (var a = this.Connect("Loot Alpha", AccountA, false)) {
				this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				var b = this.Connect("Loot Beta", AccountB, false);
				try {
					string owner = this.Ask(b, ".TestLootOwner", "LOOT_OWNER:");
					this.pack = Number(this.Ask(a, ".TestLootLooter", "LOOT_LOOTER:"), "pack=");
					uint ownerUid = Number(owner, "uid=");

					//Gondor, a fellow Gondorian
					this.Realms(a, "GG");
					Assert.Contains(Crime, this.Loot(a));

					//opening the corpse alone isn't looting, nor is a pickup that's refused
					string stage = this.Ask(a, ".TestLootStage", "LOOT_STAGE:");
					this.Open(a, stage);
					Assert.Contains("took=False criminal=False", this.Report(a));
					Assert.Contains("took=False criminal=False", this.Loot(a, "F", false));

					//not of my realm
					foreach (string realms in new[] { "GM", "MG", "GN", "NG", "Gg", "gG", "gg", "NN", "MM" }) {
						this.Realms(a, realms);
						string result = this.Loot(a);
						Assert.True(result.Contains(NoCrime), realms + ": " + result);
					}

					//a monster's corpse, an outlaw's
					this.Realms(a, "GG");
					Assert.Contains(NoCrime, this.Loot(a, "N"));
					Assert.Contains(NoCrime, this.Loot(a, "C"));

					//each realm has its own switch
					this.Switches(a, "00");
					Assert.Contains(NoCrime, this.Loot(a));
					this.Switches(a, "01");
					Assert.Contains(NoCrime, this.Loot(a));
					this.Realms(a, "MM");
					Assert.Contains(Crime, this.Loot(a));
					this.Realms(a, "MG");
					Assert.Contains(NoCrime, this.Loot(a));
					this.Switches(a, "10");
					this.Realms(a, "MM");
					Assert.Contains(NoCrime, this.Loot(a));
					this.Realms(a, "GG");

					//a party isn't leave, the owner's switch is
					this.Ask(a, ".TestLootClean", "LOOT_CLEAN:");
					this.Partner(a, b);
					Assert.Contains(Crime, this.Loot(a));
					b.SendPartyCanLoot(true);
					Thread.Sleep(300);
					Assert.Contains("canLootMe=True", this.Ask(b, ".TestLootMyLootSetting", "LOOT_SETTING:"));
					Assert.Contains(NoCrime, this.Loot(a));
					this.Ask(a, ".TestLootClean", "LOOT_CLEAN:");

					//.party shows it and changes it
					this.Press(b, "Druzina me smi okrast: ano", LootSwitch);
					Assert.Contains(Crime, this.Loot(a));
					this.Ask(a, ".TestLootClean", "LOOT_CLEAN:");
					this.Press(b, "Druzina me smi okrast: ne", LootSwitch);

					//out of the party it means nothing, and it's there in the next one, as on the client
					this.Press(b, "Opustit druzinu", Leave);
					Assert.Contains(Crime, this.Loot(a));
					this.Ask(a, ".TestLootClean", "LOOT_CLEAN:");
					this.Partner(a, b);
					Assert.Contains(NoCrime, this.Loot(a));
					this.Ask(a, ".TestLootClean", "LOOT_CLEAN:");

					//a client logs in with its switch off
					b.Dispose();
					b = this.Connect("Loot Beta", AccountB, true);
					string setting = this.Ask(b, ".TestLootMyLootSetting", "LOOT_SETTING:");
					Assert.Equal(ownerUid, Number(setting, "me="));
					Assert.Contains("canLootMe=False", setting);
				} finally {
					b.Dispose();
				}
			}
		}

		/// <summary>#1023 - Erebor's limits on a player's body: none of it in the first seconds (a real pickup refused),
		/// then one item per 0.8 s with a short hold, and nobody just dead below Grandmaster takes from others.</summary>
		[Fact]
		public void EreborsLimitsOnAPlayersBody() {
			using (var a = this.Connect("Loot Gamma", AccountA, false)) {
				this.Ask(a, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				var b = this.Connect("Loot Delta", AccountB, false);
				try {
					this.Ask(b, ".TestLootOwner", "LOOT_OWNER:");
					this.pack = Number(this.Ask(a, ".TestLootLooter", "LOOT_LOOTER:"), "pack=");
					this.Realms(a, "GM"); //no crime to muddle it
					Assert.Contains("took=False", this.Loot(a, "W", false)); //this moment dead: nothing yet
					Assert.Contains("took=True", this.Loot(a));
					Assert.Contains("wait=True first=True held=True tooFast=True freshDead=True grandmaster=True monsterFree=True",
						this.Ask(a, ".TestLootLimits", "LOOT_LIMITS:"));
				} finally {
					b.Dispose();
				}
			}
		}

		private static uint Number(string line, string key) {
			int start = line.IndexOf(key, StringComparison.Ordinal) + key.Length;
			return uint.Parse(line.Substring(start, line.IndexOf(' ', start) - start), CultureInfo.InvariantCulture);
		}

		private void Realms(TestGameClient client, string realms) {
			this.Ask(client, ".TestLootRealms \"" + realms + "\"", "LOOT_REALMS:");
		}

		private void Switches(TestGameClient client, string switches) {
			this.Ask(client, ".TestLootSwitch \"" + switches + "\"", "LOOT_SWITCH:");
		}

		private void Open(TestGameClient client, string stage) {
			client.SendDoubleClick(this.pack); //a client can only reach into what it has open
			client.SendDoubleClick(Number(stage, "corpse="));
			Thread.Sleep(500);
		}

		/// <summary>A fresh corpse, emptied by the real client.</summary>
		private string Loot(TestGameClient client, string kind = "", bool expectToHold = true) {
			string stage = this.Ask(client, ".TestLootStage \"" + kind + "\"", "LOOT_STAGE:");
			this.Open(client, stage);
			uint item = Number(stage, "item=");
			client.SendPickUpItem(item, 1);
			Thread.Sleep(300);
			if (expectToHold) {
				client.SendDropItem(item, 0xFFFF, 0xFFFF, 0, (int) this.pack);
				Thread.Sleep(200);
			}
			return this.Report(client);
		}

		private string Report(TestGameClient client) {
			return this.Ask(client, ".TestLootReport", "LOOT_REPORT:");
		}

		/// <summary>The first one nearby is the other, the only other around.</summary>
		private void Partner(TestGameClient inviter, TestGameClient invited) {
			inviter.SendSpeech(".party");
			var menu = inviter.ReadGumpDialogWithStrings(Wait);
			inviter.SendGumpResponse(menu.GumpUid, Invite);
			menu = inviter.ReadGumpDialogWithStrings(Wait);
			inviter.SendGumpResponse(menu.GumpUid, 0);
			uint inviterUid = Number(this.Ask(inviter, ".TestLootMyLootSetting", "LOOT_SETTING:"), "me=");
			invited.SendPartyAcceptInvite(inviterUid);
			Thread.Sleep(300);
		}

		/// <summary>The .party line with this text is there; presses its button.</summary>
		private void Press(TestGameClient client, string text, int button) {
			client.SendSpeech(".party");
			var menu = client.ReadGumpDialogWithStrings(Wait);
			Assert.True(menu.Strings.Exists(s => s.Contains(text)), "No '" + text + "' in: " + string.Join(" | ", menu.Strings));
			client.SendGumpResponse(menu.GumpUid, button);
			menu = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(menu.GumpUid, 0);
			Thread.Sleep(200);
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private TestGameClient Connect(string charName, string account, bool existing) {
			int redirectPort = TownTestsLogin.RedirectPort(AccountA);
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					string[] names = game.ReadCharacterList();
					if (existing) {
						game.SendSelectCharacter(Math.Max(0, Array.IndexOf(names, charName))); //#1023 - the account holds more than one
					} else {
						game.SendCreateCharacter(charName);
					}
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
	public class LootingCrimeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LootingCrime";
	}
}
