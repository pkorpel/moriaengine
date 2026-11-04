using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1116 - the PvP batch: league, arenas, RP rank, conquest. Server side: scripts/Tests/PvpTests.cs.</summary>
	[Collection(PvpCollection.Name)]
	public class PvpTests {
		private readonly GameServerPairFixture fixture;

		public PvpTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		/// <summary>Step 1: Erebor's tag.arena and tag.bf come through the converter.</summary>
		[Fact]
		public void RegionsKnowArenasAndBattlefields() {
			using var client = CommandPermissionTests.Connect("pvp", "Pvp Region Tester");
			string regions = this.Ask(client, ".TestPvpRegionTags", "PVP_REGIONS:");
			Assert.Contains("a_Arena_Mordor=True a_Mordorska_bojova_arena=True a_Nova_Arena_Equip_Room=True a_Arena_Lamy=False a_Murdoc=False", regions);
			Assert.Contains("a_Pelenorska_pole=1 a_Anorien=2 a_Vychodni_breh=-1 a_Minas_Morgul=-2 a_Rhovanion=0", regions);
		}
	}

	/// <summary>#1116 step 2 - the PvP league's points, rules, board and seasons.</summary>
	[Collection(PvpCollection.Name)]
	public class PvpLeagueTests {
		private readonly GameServerPairFixture fixture;

		public PvpLeagueTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void KillsAssistsRulesBoardAndSeasons() {
			using var killer = CommandPermissionTests.Connect("pvp", "Pvp Killer");
			this.Ask(killer, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var victim = CommandPermissionTests.Connect("pvpvictim", "Pvp Victim");
			Assert.Contains("victim=True", this.Ask(killer, ".TestLeagueReady", "LEAGUE_READY:"));
			//3 for the kill; nothing for a death right after dying, the same realm, a craftsman, rank 2, an arena, a GM
			Assert.Contains("kill=3 recentlyDead=3 sameRealm=3 craftsman=3 rank2=3 arena=3 gm=3 again=6", this.Ask(killer, ".TestLeagueKill", "LEAGUE_KILL:"));
			//the board keeps 30 and the three tied with the 30th; a season roll clears it
			Assert.Contains("shown=33 topIsNewcomer=True lastTie=5 season=1->2 afterRoll=1,0 endsDay=1", this.Ask(killer, ".TestLeagueBoard", "LEAGUE_BOARD:"));

			//#1116 step 3 - the journal's PvP side: the season, the board with one's own row, one's points, the end
			string own = this.Ask(killer, ".TestLeagueOwnPoints", "LEAGUE_OWN:");
			string end = own.Substring(own.IndexOf("end=") + 4).Trim();
			killer.SendSpeech(".questy");
			var (journalUid, _) = killer.ReadGumpDialog(TimeSpan.FromSeconds(10));
			killer.SendGumpResponse(journalUid, 21, new System.Collections.Generic.Dictionary<int, string>());
			var (_, _, strings) = killer.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			Assert.Contains("PvP Sezona 2", strings);
			Assert.Contains("1.", strings);
			Assert.Contains("Pvp Killer", strings);
			Assert.Contains("Tvuj Rank", strings);
			Assert.Contains("7", strings);
			Assert.Contains(end, strings);
		}
	}

	/// <summary>#1116 step 4 - Erebor's RP rank.</summary>
	[Collection(PvpCollection.Name)]
	public class RpRankTests {
		private readonly GameServerPairFixture fixture;

		public RpRankTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void GmSetsItThroughTheDialogsAndItMovesExperience() {
			using var gm = CommandPermissionTests.Connect("pvp", "Rp Rank Tester");
			string ready = this.Ask(gm, ".TestReportMyUid", "MY_UID:");
			uint self = uint.Parse(ready.Substring(ready.IndexOf("MY_UID:") + 7).Trim().Split(' ')[0]);
			//Erebor's table: +70% at 7, -30% at -3; 8 is refused; the log keeps every change
			Assert.Contains("rank0=0 rank7=700 rank-3=-300 refused=True log=4", this.Ask(gm, ".TestRpRankGain", "RP_GAIN:"));

			//.rprank, the info dialog, its button to the changer, OK with a rank and a reason
			gm.SendSpeech(".rprank");
			Thread.Sleep(500);
			gm.SendTargetUid(self);
			var (infoUid, _, strings) = gm.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			Assert.Contains("RP Rank - Rp Rank Tester", strings);
			Assert.Contains("Prehled postav na acc:", strings);
			gm.SendGumpResponse(infoUid, 1, new System.Collections.Generic.Dictionary<int, string>());
			var (changerUid, _) = gm.ReadGumpDialog(TimeSpan.FromSeconds(10));
			gm.SendGumpResponse(changerUid, 1, new System.Collections.Generic.Dictionary<int, string> { { 1, "5" }, { 2, "hezky RP" } });
			var (_, _, after) = gm.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			Assert.Contains(after, s => s.EndsWith(": 0->5 - hezky RP", StringComparison.Ordinal));
			Assert.Contains("rank=5", this.Ask(gm, ".TestRpRankState", "RP_STATE:"));

			//the player sees it in .explevel
			gm.SendSpeech(".explevel");
			var (_, _, level) = gm.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
			Assert.Contains("RP Rank:", level);
			Assert.Contains("5", level);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PvpCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Pvp";
	}
}
