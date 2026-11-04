using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1116 - the keep conquest. Server side: scripts/Tests/ConquestTests.cs.</summary>
	[Collection(ConquestCollection.Name)]
	public class ConquestTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public ConquestTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void SeeMessage(TestGameClient client, string text) {
			List<string> seen = new List<string>();
			for (int i = 0; i < 20; i++) {
				string message = client.ReadSystemMessage(Wait);
				if (message.Contains(text)) {
					return;
				}
				seen.Add(message);
			}
			Assert.Fail("No '" + text + "' among: " + string.Join(" / ", seen));
		}

		private static Dictionary<int, string> When(string day, string month, string hour) {
			return new Dictionary<int, string> { { 201, day }, { 202, month }, { 203, hour } };
		}

		/// <summary>Step 8: declaring at the stone, the queue, the war clock, the end, a cancel, a lapse.</summary>
		[Fact]
		public void DeclareQueueRunAndEnd() {
			using var first = CommandPermissionTests.Connect("conquest", "Conquest Tester");
			this.Ask(first, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var second = CommandPermissionTests.Connect("conquestsecond", "Conquest Second");
			string ready = this.Ask(first, ".TestConquestReady", "CONQUEST_READY:");
			Assert.Contains("name=Gondorsky vyhlasovac valky second=True", ready);
			uint stone = uint.Parse(ready.Substring(ready.IndexOf("stone=") + 6).Split(' ')[0]);

			//the dialog: Mordor's front keep, the hours; 18 is refused and the dialog comes back
			first.SendDoubleClick(stone);
			var (uid, _, strings) = first.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Vyhlaseni valky Mordorske pevnosti", strings);
			Assert.Contains("Osgiliath vychodni breh", strings);
			Assert.Contains("Mozne casy dobyvani: 19-21", strings);
			first.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "18"));
			(uid, _, _) = first.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("declared=none", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			first.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "20"));
			Thread.Sleep(500);
			Assert.Contains("declared=Mordor_1 lord=Conquest Tester begins=2026-11-05 20:00 queue=0 running=False", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));

			//the second one: the same hour is taken, a later one queues
			second.SendDoubleClick(stone);
			(uid, _, strings) = second.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Conquest Tester", strings);
			second.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "20"));
			(uid, _, _) = second.ReadGumpDialogWithStrings(Wait);
			second.SendGumpResponse(uid, 1, new[] { 101 }, When("5", "11", "21"));
			Thread.Sleep(500);
			Assert.Contains("queue=1", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));

			//.dobyvani
			first.SendSpeech(".dobyvani");
			SeeMessage(first, "Dobyvani pevnosti Osgiliath vychodni breh zacne 5.11 v 20h");

			//the war starts at its hour, runs out defended, and the queued one takes over
			this.Ask(first, ".TestConquestTick \"2026-11-05 19:59\"", "CONQUEST_TICK");
			Assert.Contains("running=False", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			this.Ask(first, ".TestConquestTick \"2026-11-05 20:00\"", "CONQUEST_TICK");
			Assert.Contains("running=True remaining=3000", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			this.Ask(first, ".TestConquestRunOut", "CONQUEST_RUNOUT");
			Assert.Contains("declared=Mordor_1 lord=Conquest Second begins=2026-11-05 21:00 queue=0 running=False remaining=0 right=True", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));

			//the new war lord cancels: peace, and the right is gone
			second.SendDoubleClick(stone);
			(uid, _, _) = second.ReadGumpDialogWithStrings(Wait);
			second.SendGumpResponse(uid, 2, new Dictionary<int, string>());
			Thread.Sleep(500);
			Assert.Contains("declared=none lord=none", this.Ask(first, ".TestConquestState", "CONQUEST_STATE:"));
			Assert.Contains("right=False", this.Ask(second, ".TestConquestState", "CONQUEST_STATE:"));

			//an evening missed with the server down lapses
			Assert.Contains("declared=True after=none", this.Ask(first, ".TestConquestLapse", "CONQUEST_LAPSE:"));
		}
	}

	/// <summary>#1116 step 9 - the war lieutenants.</summary>
	[Collection(ConquestCollection.Name)]
	public class WarLieutenantTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public WarLieutenantTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void NorbertSellsTheRightToDeclareWar() {
			using var client = CommandPermissionTests.Connect("conquest", "Conquest Buyer");
			string ready = this.Ask(client, ".TestLieutenantReady \"29\"", "LIEUTENANT_READY:");
			uint npc = uint.Parse(ready.Substring(ready.IndexOf("npc=") + 4).Split(' ')[0]);
			//level 29: too green, nothing paid
			client.SendSpeech("ahoj");
			var (uid, _, strings) = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Vyhlasit utok", strings);
			Assert.Contains("Ano, to mi vyhovuje. Zaplatim 100 000 GP.", strings);
			client.SendGumpResponse(uid, 1, new Dictionary<int, string>());
			Thread.Sleep(500);
			Assert.Contains("right=False gold=100000", this.Ask(client, ".TestLieutenantState", "LIEUTENANT_STATE:"));
			//level 30: the right for 100 000
			this.Ask(client, ".TestLieutenantLevel \"30\"", "LIEUTENANT_LEVEL");
			client.SendDoubleClick(npc);
			(uid, _, _) = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(uid, 1, new Dictionary<int, string>());
			Thread.Sleep(500);
			Assert.Contains("right=True gold=0", this.Ask(client, ".TestLieutenantState", "LIEUTENANT_STATE:"));
		}
	}

	/// <summary>#1116 step 10 - the palantir.</summary>
	[Collection(ConquestCollection.Name)]
	public class PalantirTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public PalantirTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static uint Uid(string line, string key) {
			return uint.Parse(line.Substring(line.IndexOf(key + "=") + key.Length + 1).Split(' ')[0]);
		}

		[Fact]
		public void BreachCounterCaptureAndTheCashBox() {
			using var attacker = CommandPermissionTests.Connect("conquest", "Conquest Attacker");
			this.Ask(attacker, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var defender = CommandPermissionTests.Connect("conquestsecond", "Conquest Defender");
			string spots = this.Ask(attacker, ".TestConquestSpots", "CONQUEST_SPOTS:");
			Assert.True(!spots.Contains("False"), spots);
			string ready = this.Ask(attacker, ".TestPalantirReady", "PALANTIR_READY:");
			Assert.Contains("declared=True name=Palantir Osgiliath vychodni breh", ready);
			uint palantir = Uid(ready, "palantir"), box = Uid(ready, "box");

			//the attacker breaches, the countdown runs
			attacker.SendDoubleClick(palantir);
			Thread.Sleep(500);
			Assert.Contains("owner=Mordor breached=True conqueror=Gondor hold=900 color=5", this.Ask(attacker, ".TestPalantirState", "PALANTIR_STATE:"));
			this.Ask(attacker, ".TestPalantirTicks \"5\"", "PALANTIR_TICKS");
			Assert.Contains("hold=895", this.Ask(attacker, ".TestPalantirState", "PALANTIR_STATE:"));
			//the defender turns it back, it fills up again
			defender.SendDoubleClick(palantir);
			Thread.Sleep(500);
			this.Ask(attacker, ".TestPalantirTicks \"3\"", "PALANTIR_TICKS");
			string state = this.Ask(attacker, ".TestPalantirState", "PALANTIR_STATE:");
			Assert.True(state.Contains("owner=Mordor breached=False conqueror=Mordor hold=898"), state);
			//breached again and held to the end: the keep is Gondor's, the line moves, the war lord's right is spent
			attacker.SendDoubleClick(palantir);
			Thread.Sleep(500);
			this.Ask(attacker, ".TestPalantirHold \"2\"", "PALANTIR_HOLD");
			this.Ask(attacker, ".TestPalantirTicks \"2\"", "PALANTIR_TICKS");
			//1 gp for each 5 s from 11:43:20 on 4.11 to 20:00 on 5.11
			Assert.Contains("owner=Gondor breached=False conqueror=Gondor hold=900 color=0 gondorDeclared=none gondorConquered=-1 mordorConquered=-2 right=False cash=23240 collector=Conquest Attacker cheque=0", this.Ask(attacker, ".TestPalantirState", "PALANTIR_STATE:"));
			//the cash box pays its war lord a cheque
			attacker.SendDoubleClick(box);
			Thread.Sleep(500);
			Assert.Contains("cash=0 collector=none cheque=23240", this.Ask(attacker, ".TestPalantirState", "PALANTIR_STATE:"));
		}
	}

	/// <summary>#1116 step 11 - keep captains and their guards.</summary>
	[Collection(ConquestCollection.Name)]
	public class KeepCaptainTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public KeepCaptainTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static (int, string, List<string>) GumpWith(TestGameClient client, string text) {
			for (int i = 0; i < 6; i++) {
				var gump = client.ReadGumpDialogWithStrings(Wait);
				if (gump.Strings.Exists(s => s.Contains(text))) {
					return gump;
				}
			}
			throw new Xunit.Sdk.XunitException("No gump with '" + text + "'");
		}

		[Fact]
		public void AppointHireFreeGuardsAndHandOverOnCapture() {
			using var gm = CommandPermissionTests.Connect("conquest", "Conquest Keeper");
			this.Ask(gm, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var captain = CommandPermissionTests.Connect("conquestsecond", "Conquest Captain");
			string ready = this.Ask(gm, ".TestCaptainReady", "CAPTAIN_READY:");
			Assert.Contains("inKeep=True", ready);
			uint stone = uint.Parse(ready.Substring(ready.IndexOf("stone=") + 6).Split(' ')[0]);
			uint captainUid = uint.Parse(ready.Substring(ready.IndexOf("captain=") + 8).Split(' ')[0]);

			//the GM at the stone: yes, then the target
			gm.SendDoubleClick(stone);
			var (confirm, _, _) = GumpWith(gm, "Prejes si dosadit");
			gm.SendGumpResponse(confirm, 1, new Dictionary<int, string>());
			Thread.Sleep(500);
			gm.SendTargetUid(captainUid);
			Thread.Sleep(500);
			Assert.Contains("captain=Kapitan Conquest Captain isCaptain=True captainName=Kapitan Conquest Captain owner=Gondor guards= alive=False", this.Ask(gm, ".TestCaptainState", "CAPTAIN_STATE:"));

			//the captain at the stone: the recruiter, a free guard
			captain.SendDoubleClick(stone);
			var (recruiter, _, strings) = GumpWith(captain, "Mozno najmout");
			Assert.Contains("Mozno najmout: 3 neplacenych strazi", strings);
			captain.SendGumpResponse(recruiter, 1000, new Dictionary<int, string>());
			Thread.Sleep(500);
			Assert.Contains("guards=c_vguard_gondor_hala_pesak,", this.Ask(gm, ".TestCaptainState", "CAPTAIN_STATE:"));
			//posted too far, refused; near the stone, it stands and the palantir can't be breached
			Assert.Contains("posted=False", this.Ask(gm, ".TestCaptainPost \"far\"", "CAPTAIN_POST:"));
			Assert.Contains("posted=True", this.Ask(gm, ".TestCaptainPost \"near\"", "CAPTAIN_POST:"));
			Assert.Contains("guards=c_vguard_gondor_hala_pesak(standing), alive=True", this.Ask(gm, ".TestCaptainState", "CAPTAIN_STATE:"));

			//Mordor takes the keep: the captain goes, the guard is Mordor's, the war lord is the captain
			Assert.Contains("lord=Kapitan Conquest Keeper", this.Ask(gm, ".TestCaptainCapture", "CAPTAIN_CAPTURE:"));
			Assert.Contains("isCaptain=False captainName=Conquest Captain owner=Mordor guards=c_vguard_mordor_hala_pesak(standing),", this.Ask(gm, ".TestCaptainState", "CAPTAIN_STATE:"));
		}
	}

	/// <summary>#1116 step 12 - the keep blacklist.</summary>
	[Collection(ConquestCollection.Name)]
	public class KeepBlacklistTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public KeepBlacklistTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void TheCaptainBlacklistsAndTheKeepsGuardsTurnOnThem() {
			using var first = CommandPermissionTests.Connect("conquest", "Conquest Ban Opener");
			this.Ask(first, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			first.Dispose();
			using var outlaw = CommandPermissionTests.Connect("conquestsecond", "Conquest Outlaw");
			using var captain = CommandPermissionTests.Connect("conquestthird", "Conquest Ban Captain");
			string ready = this.Ask(captain, ".TestBanReady", "BAN_READY:");
			Assert.Contains("captain=True guard=True", ready);
			uint outlawUid = uint.Parse(ready.Substring(ready.IndexOf("outlaw=") + 7).Split(' ')[0]);
			Assert.Contains("bans=0 guardAttacks=False", this.Ask(captain, ".TestBanState", "BAN_STATE:"));

			//.keep_blacklist, the target: the keep's guard turns on them; a second time changes nothing
			captain.SendSpeech(".keep_blacklist");
			Thread.Sleep(500);
			captain.SendTargetUid(outlawUid);
			Thread.Sleep(500);
			Assert.Contains("bans=1 guardAttacks=True", this.Ask(captain, ".TestBanState", "BAN_STATE:"));
			captain.SendSpeech(".keep_blacklist");
			Thread.Sleep(500);
			captain.SendTargetUid(outlawUid);
			Thread.Sleep(500);
			Assert.Contains("bans=1", this.Ask(captain, ".TestBanState", "BAN_STATE:"));

			//off the list through the executioner dialog
			captain.SendSpeech(".keepcontrol");
			for (int i = 0; i < 6; i++) {
				var (uid, _, strings) = captain.ReadGumpDialogWithStrings(Wait);
				if (strings.Contains("Nepratel : 1")) {
					Assert.Contains("Conquest Outlaw", strings);
					captain.SendGumpResponse(uid, 100, new Dictionary<int, string>());
					break;
				}
			}
			Thread.Sleep(500);
			Assert.Contains("bans=0 guardAttacks=False", this.Ask(captain, ".TestBanState", "BAN_STATE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ConquestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Conquest";
	}
}
