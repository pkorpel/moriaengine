using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's Taming: its checks in order, the timer, the spawn and mount knowledge.</summary>
	[Collection(TamingCollection.Name)]
	public class TamingTests {
		private const int TamingSkill = 35, HidingSkill = 21;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public TamingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void WaitForMessage(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
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

		private uint Spawn(TestGameClient client, string how) {
			string line = this.Ask(client, ".TestTamingSpawn \"" + how + "\"", "TAMING_SPAWNED:");
			return uint.Parse(Regex.Match(line, @"npc=#(\d+)").Groups[1].Value);
		}

		private static void Tame(TestGameClient client, uint npc) {
			client.SendSkillUse(TamingSkill);
			Thread.Sleep(300);
			client.SendTargetUid(npc);
			Thread.Sleep(300);
		}

		private static void Refused(TestGameClient client, uint npc, string message) {
			Tame(client, npc);
			WaitForMessage(client, message);
		}

		private void Prepare(TestGameClient client, string how) {
			this.Ask(client, ".TestTamingPrepare \"" + how + "\"", "TAMING_PREPARED:");
		}

		private void Crook(TestGameClient client, string defname) {
			Assert.Contains(defname, this.Ask(client, ".TestTamingCrook \"" + defname + "\"", "TAMING_CROOK:"));
		}

		[Fact]
		public void TheChecksComeInEreborsOrder() {
			using var client = CommandPermissionTests.Connect("tametest", "Tame Tester");
			this.Prepare(client, "1000 1000 open");
			string table = this.Ask(client, ".TestMountTable", "MOUNT_TABLE:");
			Assert.Contains("rows=18 missing=0 first=Lama,1,100,8438", table);
			Assert.Contains("most=100", table);

			uint rabbit = this.Spawn(client, "c_a_jackrabbit 1 100");
			Refused(client, rabbit, "Na ochocovani zvere potrebujes pasteveckou hul");
			this.Crook(client, "i_shepherds_crook_copper");

			this.Prepare(client, "50 1000");
			Refused(client, rabbit, "Zvire je prilis divoke");
			this.Prepare(client, "1000 0");
			uint knowing = this.Spawn(client, "c_a_jackrabbit 1 100 500");
			Refused(client, knowing, "Nemas nejmensi poneti");

			this.Prepare(client, "1000 1000");
			uint far = this.Spawn(client, "c_a_jackrabbit 7 100");
			Refused(client, far, "Jsi prilis daleko");

			//a better wood from each step, and anything that isn't a crook is no crook
			uint wild = this.Spawn(client, "c_a_jackrabbit 1 900");
			Refused(client, wild, "potrebujes lepsi pasteveckou hul");
			this.Crook(client, "i_shepherds_crook_valorite");
			Tame(client, wild);
			Assert.Contains("busy=True", this.Ask(client, ".TestTamingBusy", "TAMING_BUSY:"));
			this.Ask(client, ".TestTamingHurt", "TAMING_HURT:");
			WaitForMessage(client, "Ochocovani bylo preruseno");
			Assert.Contains("busy=False", this.Ask(client, ".TestTamingBusy", "TAMING_BUSY:"));

			//someone with no taming of their own can't be tamed, whatever they're worth
			this.Prepare(client, "1000 1000");
			uint self = uint.Parse(Regex.Match(this.Ask(client, ".TestTamingSelf", "TAMING_SELF:"), @"#(\d+)").Groups[1].Value);
			Refused(client, self, "Tohle nemuzes ochocit");
		}

		[Fact]
		public void TalkingItRoundLeavesTheSpawnAndCountsTheKnowledge() {
			using var client = CommandPermissionTests.Connect("tametest", "Tame Spawner");
			this.Prepare(client, "1400 1000 open");
			this.Crook(client, "i_shepherds_crook_copper");

			uint llama = this.Spawn(client, "c_llama 1 300");
			string gem = Regex.Match(this.Ask(client, ".TestTamingAttachGem " + llama, "TAMING_GEM:"), @"#(\d+)").Groups[1].Value;
			Tame(client, llama);
			Assert.Contains("busy=True", this.Ask(client, ".TestTamingBusy", "TAMING_BUSY:"));

			//taming again while it runs takes back nothing and starts nothing
			Tame(client, llama);
			WaitForMessage(client, "You must wait");
			Assert.Contains("busy=True", this.Ask(client, ".TestTamingBusy", "TAMING_BUSY:"));

			this.Ask(client, ".TestTamingShorten", "TAMING_SHORTENED:");
			WaitForMessage(client, "rozsiril");
			WaitForMessage(client, "prijalo za sveho pana");

			string report = this.Ask(client, ".TestTamingReport " + llama, "TAMING_REPORT:");
			Assert.Contains("master=Tame Spawner", report);
			Assert.Contains("karma=0", report);
			Assert.Contains("spawned=False", report);
			Assert.Contains("count=0", this.Ask(client, ".TestTamingGemReport " + gem, "TAMING_GEM_REPORT:"));
			Assert.Contains("total=1 bonus=0 llama=1", this.Ask(client, ".TestMountKnowledge", "MOUNT_KNOWLEDGE:"));

			//already someone's
			Refused(client, llama, "Zvire je jiz ochocene");

			//past what a mount can teach
			this.Ask(client, ".TestMountCount \"100\"", "MOUNT_COUNTED:");
			uint next = this.Spawn(client, "c_llama 1 300");
			this.Ask(client, ".TestTamingAttachGem " + next, "TAMING_GEM:");
			Tame(client, next);
			this.Ask(client, ".TestTamingShorten", "TAMING_SHORTENED:");
			WaitForMessage(client, "nic noveho nedozvedel");
		}

		[Fact]
		public void TheRollAndWhatEndsItEarly() {
			using var client = CommandPermissionTests.Connect("tametest", "Tame Roller");
			this.Prepare(client, "100 1000 open");
			this.Crook(client, "i_shepherds_crook_copper");

			//no margin over the animal, no pet
			uint equal = this.Spawn(client, "c_a_jackrabbit 1 100");
			Tame(client, equal);
			this.Ask(client, ".TestTamingShorten", "TAMING_SHORTENED:");
			WaitForMessage(client, "Nepovedlo se ti zvire ochocit");
			Assert.Contains("master=none", this.Ask(client, ".TestTamingReport " + equal, "TAMING_REPORT:"));

			//raised by a healer it comes back on the first go, whatever the roll
			this.Ask(client, ".TestTamingRaised " + equal, "TAMING_RAISED:");
			Tame(client, equal);
			WaitForMessage(client, "prijalo za sveho pana");

			//led away from home, it goes back and out of sight
			uint led = this.Spawn(client, "c_a_jackrabbit 1 100");
			string gem = Regex.Match(this.Ask(client, ".TestTamingAttachGem " + led, "TAMING_GEM:"), @"#(\d+)").Groups[1].Value;
			Tame(client, led);
			this.Ask(client, ".TestTamingMoveGem " + gem, "TAMING_GEM_MOVED:");
			WaitForMessage(client, "Ztratil jsi svuj cil z dohledu");

			//another skill ends it
			uint hidden = this.Spawn(client, "c_a_jackrabbit 1 100");
			Tame(client, hidden);
			client.SendSkillUse(HidingSkill);
			WaitForMessage(client, "Ochocovani bylo preruseno");
		}

		[Fact]
		public void TheKnowledgeDialogShowsWhatYouTamed() {
			using var client = CommandPermissionTests.Connect("tametest", "Tame Reader");
			this.Prepare(client, "1000 1000 open");
			this.Ask(client, ".TestMountCount \"100\"", "MOUNT_COUNTED:");
			client.SendSpeech(".TestMountDialog");
			var dialog = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Lama", dialog.Strings);
			Assert.Contains("100 / 100", dialog.Strings);
			Assert.Contains("5 %", dialog.Strings);
			Assert.Contains("?????", dialog.Strings);
			Assert.DoesNotContain("Roan", dialog.Strings);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TamingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Taming";
	}
}
