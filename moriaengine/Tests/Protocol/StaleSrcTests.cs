using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1155/#1156 - knockback needs damage; a monster's timed swing after the last packet's sender logged out.
	/// Server side: scripts/Tests/StaleSrcTests.cs.</summary>
	[Collection(FloorAttackCollection.Name)]
	public class StaleSrcTests {
		private readonly GameServerPairFixture fixture;

		public StaleSrcTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestStaleSrc \"" + what + "\"", "STALESRC_TEST: " + what + " ");
		}

		[Fact]
		public void KnockbackNeedsDamageAndSwingsGoOnAfterTheLastSenderLeaves() {
			using (var client = CommandPermissionTests.Connect("floortests", "Src Leaver")) {
				string knock = this.Do(client, "knockback");
				Assert.True(knock.Contains("noDamageNoPush=True hurt=True pushed=True"), knock);
				this.Do(client, "setup");
			}
			//the player who sent the last packet is gone; the monster keeps swinging on its own timers
			int mark = this.fixture.GameOutputSnapshot().Length;
			Thread.Sleep(5000);
			string log = this.fixture.GameOutputSnapshot().Substring(mark);
			using var back = CommandPermissionTests.Connect("floortests", "Src Returner");
			string after = this.Do(back, "after");
			Assert.True(after.Contains("rabbitHurt=True knockedBack=True"), after);
			Assert.DoesNotContain("get_SrcTcpConnection", log);
		}
	}
}
