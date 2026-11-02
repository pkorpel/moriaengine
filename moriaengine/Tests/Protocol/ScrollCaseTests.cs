using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - casting from scrolls, Erebor's custom scrolls and the scroll case.</summary>
	[Collection(ScrollCaseCollection.Name)]
	public class ScrollCaseTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public ScrollCaseTests(GameServerPairFixture servers) {
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

		private string Report(TestGameClient client) {
			return this.Ask(client, ".TestScrollReport", "SCROLL_REPORT:");
		}

		[Fact]
		public void ScrollsCastAndTheCaseHoldsThem() {
			using var client = CommandPermissionTests.Connect("scrollcase", "Scroll Tester");
			string ready = this.Ask(client, ".TestScrollReady", "SCROLL_READY:");
			client.SendDoubleClick(Uid(ready, "pack"));
			Thread.Sleep(300);

			//a double click casts a plain scroll, at half mana, and takes it
			client.SendDoubleClick(Uid(ready, "plain"));
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "self"));
			Thread.Sleep(2500);
			string report = this.Report(client);
			Assert.Contains("plain=0", report);
			Assert.Contains("mana=297", report);

			//mana shield is for yourself alone and costs its full 30
			client.SendDoubleClick(Uid(ready, "shield"));
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "dummy"));
			Expect(client, "Kouzlo lze zamerit pouze na sebe");
			Assert.Contains("shieldscrolls=2", this.Report(client));
			client.SendDoubleClick(Uid(ready, "shield"));
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "self"));
			Thread.Sleep(3500);
			report = this.Report(client);
			Assert.Contains("shieldscrolls=1", report);
			if (report.Contains("shielded=True")) {
				Assert.Contains("mana=267", report);
			} else {
				Assert.Contains("mana=282", report); //the roll fizzled: half
			}

			//mass bless is a priest's, mana shield isn't
			client.SendDoubleClick(Uid(ready, "bless"));
			Expect(client, "Pro toto kouzlo nemas vhodne povolani");
			this.Ask(client, ".TestScrollProfession \"profession_priest\"", "SCROLL_PROFESSION:");
			client.SendDoubleClick(Uid(ready, "shield"));
			Expect(client, "Pro toto kouzlo nemas vhodne povolani");

			//#1001 - Mass Bless reaches 8 tiles from its target; a try can fizzle, so a few scrolls
			string blessed = "";
			for (int tries = 0; (tries < 6) && !blessed.Contains("main=True"); tries++) {
				client.SendDoubleClick(Uid(ready, "bless"));
				Thread.Sleep(300);
				client.SendTargetUid(Uid(ready, "dummy"));
				Thread.Sleep(3500);
				blessed = this.Ask(client, ".TestMassBlessReport", "MASS_BLESS:");
			}
			Assert.Contains("main=True near=True far=False", blessed);

			//in by dropping
			client.SendPickUpItem(Uid(ready, "clumsy"), 5);
			Thread.Sleep(200);
			client.SendDropItem(Uid(ready, "clumsy"), 0, 0, 0, (int) Uid(ready, "case"));
			Expect(client, "Vlozil jsi do svitkovace 5");

			//the book of counts, and out again
			client.SendDoubleClick(Uid(ready, "case"));
			var dialog = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Clumsy", dialog.Strings);
			Assert.Contains("5", dialog.Strings);
			client.SendGumpResponse(dialog.GumpUid, 1);
			dialog = client.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Kolik svitku odebrat?", dialog.Strings);
			client.SendGumpResponse(dialog.GumpUid, 2, new Dictionary<int, string> { { 3, "2" } });
			Expect(client, "Vybral jsi ze svitkovace 2");
			dialog = client.ReadGumpDialogWithStrings(Wait);
			report = this.Report(client);
			Assert.Contains("incase=3", report);
			Assert.Contains("clumsy=2", report);

			//one of each
			client.SendGumpResponse(dialog.GumpUid, 19);
			Thread.Sleep(300);
			report = this.Report(client);
			Assert.Contains("incase=2", report);
			Assert.Contains("clumsy=3", report);

			//by the cursor, and what's past Sphere's spells shows on the last page
			client.SendDoubleClick(Uid(ready, "case"));
			dialog = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(dialog.GumpUid, 18);
			Thread.Sleep(300);
			client.SendTargetUid(Uid(ready, "shield"));
			Expect(client, "Uloz dalsi svitek");
			Assert.Contains("shieldincase=1", this.Report(client));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ScrollCaseCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ScrollCase";
	}
}
