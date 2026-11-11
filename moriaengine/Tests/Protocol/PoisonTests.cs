using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's poisons and the bottle on a blade or on arrows.</summary>
	[Collection(PoisonCollection.Name)]
	public class PoisonTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public PoisonTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, params string[] patterns) {
			DateTime deadline = DateTime.UtcNow + Wait;
			foreach (string pattern in patterns) {
				string seen = "";
				try {
					string message;
					while (!Regex.IsMatch(message = client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
						seen += message + " | ";
					}
				} catch (TimeoutException) {
					throw new TimeoutException("No '" + pattern + "', only: " + seen);
				}
			}
		}

		private static uint Uid(string line) {
			return uint.Parse(Regex.Match(line, @"#(\d+)").Groups[1].Value);
		}

		private uint Prepare(TestGameClient client, string skill) {
			uint pack = Uid(this.Ask(client, ".TestPoisonPrepare \"" + skill + "\"", "POISON_PREPARED:"));
			client.SendDoubleClick(pack);
			Thread.Sleep(300);
			return pack;
		}

		private uint Give(TestGameClient client, string what) {
			return Uid(this.Ask(client, ".TestPoisonGive \"" + what + "\"", "POISON_GIVEN:"));
		}

		private uint Npc(TestGameClient client) {
			return Uid(this.Ask(client, ".TestPoisonNpc", "POISON_NPC:"));
		}

		private string Report(TestGameClient client, uint who) {
			return this.Ask(client, ".TestPoisonReport " + who, "POISON_REPORT:");
		}

		private static void OnTo(TestGameClient client, uint bottle, uint target) {
			client.SendDoubleClick(bottle);
			Thread.Sleep(300);
			client.SendTargetUid(target);
		}

		[Fact]
		public void EachKindDoesItsOwnAndCureEndsThem() {
			using var client = CommandPermissionTests.Connect("poisontest", "Poison Kinds");
			this.Prepare(client, "0");
			uint npc = this.Npc(client);

			//a percent of hits, a tenth weaker each hit
			this.Ask(client, ".TestPoisonApply \"" + npc + " 10 1 poison_c\"", "POISON_APPLIED:");
			string first = this.Report(client, npc);
			Assert.Contains("poisoned=True green=True", first);
			Assert.Contains("PercentOfHits=10", first);
			Thread.Sleep(1300);
			string hit = this.Report(client, npc);
			Assert.Contains("hits=900", hit);
			Assert.Contains("PercentOfHits=9", hit);

			//a stronger dose takes over, a weaker one doesn't
			this.Ask(client, ".TestPoisonApply \"" + npc + " 12 1 poison_c\"", "POISON_APPLIED:");
			Assert.Contains("PercentOfHits=12", this.Report(client, npc));
			this.Ask(client, ".TestPoisonApply \"" + npc + " 2 1 poison_c\"", "POISON_APPLIED:");
			Assert.Contains("PercentOfHits=", this.Report(client, npc));

			//each kind runs on its own
			this.Ask(client, ".TestPoisonApply \"" + npc + " 20 1 poison_weaken\"", "POISON_APPLIED:");
			this.Ask(client, ".TestPoisonApply \"" + npc + " 900 1 poison_para\"", "POISON_APPLIED:");
			Thread.Sleep(1300);
			string both = this.Report(client, npc);
			Assert.Contains("str=80", both);
			Assert.Contains("para=True", both);

			//a cure takes all of it, then nothing takes for a while
			this.Ask(client, ".TestPoisonCure " + npc, "POISON_CURED:");
			Assert.Contains("poisoned=False green=False", this.Report(client, npc));
			Assert.Contains("took=False", this.Ask(client, ".TestPoisonApply \"" + npc + " 10 1 poison_c\"", "POISON_APPLIED:"));

			//a monster's blow, at full skill
			uint viper = this.Npc(client);
			this.Ask(client, ".TestPoisonMonster " + viper, "POISON_MONSTER:");
			Expect(client, "Byl jsi otraven");
		}

		[Fact]
		public void ABottleOnABlade() {
			using var client = CommandPermissionTests.Connect("poisontest", "Poison Blade");
			this.Prepare(client, "1100");
			uint bottle = this.Give(client, "i_potion_PoisonDeadly");
			uint blade = this.Give(client, "i_katana");
			OnTo(client, bottle, blade);
			Expect(client, "Co chces otravit", "Uspesne jsi aplikoval jed");
			string poisoned = this.Ask(client, ".TestPoisonItem " + blade, "POISON_ITEM:");
			Assert.Matches(@"power=1[234] doses=20 kind=PercentOfHits", poisoned); //9-10 at 140 percent

			//plain damage from a blade, twelve times on a monster
			uint npc = this.Npc(client);
			this.Ask(client, ".TestPoisonBlade \"" + blade + " " + npc + "\"", "POISON_BLADE:");
			Assert.Matches(@"AbsoluteDamage=1[456]\d", this.Report(client, npc));
			Assert.Contains("doses=19", this.Ask(client, ".TestPoisonItem " + blade, "POISON_ITEM:"));

			//not on a staff
			uint second = this.Give(client, "i_potion_PoisonDeadly");
			uint staff = this.Give(client, "i_staff_black");
			OnTo(client, second, staff);
			Expect(client, "Na toto nemuzes aplikovat jed");
		}

		[Fact]
		public void ABottleOnArrows() {
			using var client = CommandPermissionTests.Connect("poisontest", "Poison Arrows");
			this.Prepare(client, "1100");
			uint bottle = this.Give(client, "i_potion_PoisonDeadly");
			uint tooMany = this.Give(client, "i_arrow_piercing_copper 51");
			OnTo(client, bottle, tooMany);
			Expect(client, "Na takove mnozstvi jedna lahvicka nestaci");

			uint arrows = this.Give(client, "i_arrow_piercing_copper 20");
			Thread.Sleep(300);
			client.SendTargetUid(arrows);
			Expect(client, "Uspesne jsi aplikoval jed");
			Assert.Matches(@"amount=20 power=[67]", this.Ask(client, ".TestPoisonArrows", "POISON_ARROWS:")); //9-10 at 75 percent
			Assert.Contains("exists=False", this.Ask(client, ".TestPoisonItem " + arrows, "POISON_ITEM:"));
		}
	}

	/// <summary>#1422 - the running poison is its plugin.</summary>
	[Collection(PoisonCollection.Name)]
	public class PoisonPluginTests {
		private readonly GameServerPairFixture fixture;

		public PoisonPluginTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void APoisonIsItsPlugin() {
			using var client = CommandPermissionTests.Connect("poisontest", "Poison Plugin");
			CommandPermissionTests.Ask(this.fixture, client, ".TestPoisonPrepare \"0\"", "POISON_PREPARED:");
			string made = CommandPermissionTests.Ask(this.fixture, client, ".TestPoisonNpc", "POISON_NPC:");
			string npc = made.Substring(made.IndexOf('#') + 1).Trim();
			CommandPermissionTests.Ask(this.fixture, client, ".TestPoisonPlugin " + npc, "POISON_PLUGIN_DONE");
			string output = this.fixture.GameOutputSnapshot();
			string failed = "";
			foreach (string check in new[] { "isPlugin", "listed", "bleedingKeepsGreen", "greenGoesAfter", "deathEndsIt" }) {
				string marker = "POISON_CHECK: " + check + "=";
				int at = output.LastIndexOf(marker, StringComparison.Ordinal);
				if ((at < 0) || !output.Substring(at + marker.Length).StartsWith("True", StringComparison.Ordinal)) {
					failed += " " + check;
				}
			}
			Assert.True(failed.Length == 0, "Failed checks:" + failed);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PoisonCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Poison";
	}
}
