using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - the necromancer's book and spells.</summary>
	[Collection(NecromancyCollection.Name)]
	public class NecromancyTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public NecromancyTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		private static uint Uid(string line, string name) {
			return uint.Parse(Regex.Match(line, name + @"=#(\d+)").Groups[1].Value);
		}

		[Fact]
		public void TheBookHoldsNecroSpellsAndTheyHit() {
			using var client = CommandPermissionTests.Connect("necrotest", "Necro Tester");
			int mark = this.fixture.GameOutputSnapshot().Length;
			string ready = this.Ask(client, ".TestNecroReady", "NECRO_READY:");
			Assert.Contains("NECRO_NEW_BOOK: circle3=True circle4=False summon=True necro=63", this.fixture.GameOutputSnapshot().Substring(mark));
			Assert.Contains("has=False", ready);
			client.SendDoubleClick(Uid(ready, "pack"));
			Thread.Sleep(300);

			//a scroll dropped on the book teaches it
			foreach (string scroll in new[] { "firebolt", "frostbolt" }) {
				client.SendPickUpItem(Uid(ready, scroll), 1);
				Thread.Sleep(200);
				client.SendDropItem(Uid(ready, scroll), 0, 0, 0, (int) Uid(ready, "book"));
				Thread.Sleep(200);
			}
			string report = this.Ask(client, ".TestNecroReport", "NECRO_REPORT:");
			Assert.Contains("firebolt=True frostbolt=True", report);

			//it opens the necromancer's own page, its back arrow the plain book
			client.SendDoubleClick(Uid(ready, "book"));
			var dialog = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Firebolt", dialog.Strings);
			Assert.Contains("Neni v knize", dialog.Strings);

			//a button casts, as does the command; a word of the spell then the cursor
			client.SendGumpResponse(dialog.GumpUid, 1);
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "dummy"));
			Thread.Sleep(3500);
			report = this.Ask(client, ".TestNecroReport", "NECRO_REPORT:");
			Assert.Matches(@"mana=(278|2[89]\d)", report);

			client.SendSpeech(".frostbolt");
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "dummy"));
			Thread.Sleep(2500);
			report = this.Ask(client, ".TestNecroReport", "NECRO_REPORT:");
			Assert.DoesNotContain("hits=5000", report);

			//a Mystic heals only himself
			this.Ask(client, ".TestNecroMakeMystic", "NECRO_MYSTIC:");
			this.Ask(client, ".TestNecroCast \"s_heal\"", "NECRO_CAST:");
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "dummy"));
			Expect(client, "Toto kouzlo muzes pouzit jen na sebe");
		}
		[Fact]
		public void SpellEffectsAsErebor() {
			using var client = CommandPermissionTests.Connect("necrotest", "Necro Auditor");
			this.Ask(client, ".TestNecroReady", "NECRO_READY:");
			string audit = this.Ask(client, ".TestSpellAudit", "SPELL_AUDIT:");
			Assert.Contains("unskilled=100 skilled=75 hidden=False paralyzed=False", audit);
			Assert.Matches(@"lost=([1-9]\d*)", audit);
		}

		[Fact]
		public void DeadRiseAndSummonsAndBones() {
			using var client = CommandPermissionTests.Connect("necrotest", "Necro Raiser");
			this.Ask(client, ".TestNecroReady", "NECRO_READY:");
			string ready = this.Ask(client, ".TestAnimateReady", "ANIMATE_READY:");
			Assert.Contains("dead=False", ready);

			//no staff, no hold over it
			this.Ask(client, ".TestNecroAddAnimate", "NECRO_ADDED:");
			client.SendSpeech(".necroanimate");
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "corpse"));
			Expect(client, "Na toto monstrum nestaci tvoje hul");

			//with one it's up, it's yours, and it costs mana by the second
			this.Ask(client, ".TestAnimateStaff", "ANIMATE_STAFF:");
			client.SendSpeech(".necroanimate");
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "corpse"));
			Thread.Sleep(9500);
			string report = this.Ask(client, ".TestAnimateReport", "ANIMATE_REPORT:");
			Assert.Contains("raised=True owned=True corpse=False", report);
			int mana = int.Parse(Regex.Match(report, @"mana=(\d+)").Groups[1].Value);
			Thread.Sleep(2500);
			report = this.Ask(client, ".TestAnimateReport", "ANIMATE_REPORT:");
			Assert.True(int.Parse(Regex.Match(report, @"mana=(\d+)").Groups[1].Value) < mana, report);

			//it leaves nothing behind
			Assert.Contains("corpses=0", this.Ask(client, ".TestAnimatedDies", "ANIMATE_DIED:"));

			//the menu offers a necromancer's own, its pick goes to the ground
			this.Ask(client, ".TestNecroAddSummon", "NECRO_ADDED:");
			string row = this.Ask(client, ".TestSummonRow \"c_zombie_necro\"", "SUMMON_ROW:");
			client.SendSpeech(".necrosummon");
			var dialog = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Skeleton Sword", dialog.Strings);
			Assert.DoesNotContain("Black Bear", dialog.Strings);
			client.SendGumpResponse(dialog.GumpUid, int.Parse(Regex.Match(row, @"row=(\d+)").Groups[1].Value));
			Thread.Sleep(300);
			client.SendTargetGround(int.Parse(Regex.Match(ready, @"x=(\d+)").Groups[1].Value), int.Parse(Regex.Match(ready, @"y=(\d+)").Groups[1].Value), int.Parse(Regex.Match(ready, @"z=(-?\d+)").Groups[1].Value));
			Thread.Sleep(3500);
			Assert.Contains("found=True", this.Ask(client, ".TestSummonReport \"c_zombie_necro\"", "SUMMON_REPORT:"));

			//bones on the free layers, a second later
			this.Ask(client, ".TestBoneReady", "BONE_READY:");
			client.SendSpeech(".necroarmor");
			Thread.Sleep(1800);
			report = this.Ask(client, ".TestBoneReport", "BONE_REPORT:");
			Assert.Contains("chest=True mana=290 guarded=True", report);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class NecromancyCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Necromancy";
	}
}
