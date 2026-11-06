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
			int mark;
			using (var client = CommandPermissionTests.Connect("floortests", "Src Leaver")) {
				string knock = this.Do(client, "knockback");
				Assert.True(knock.Contains("noDamageNoPush=True hurt=True pushed=True"), knock);
				mark = this.fixture.GameOutputSnapshot().Length; //before the setup: the server prints the hit line only after the logout
				this.Do(client, "setup");
			}
			//the player who sent the last packet is gone; the monster keeps swinging on its own timers. #1180 - wait for the
			//server to see a hurting, pushing blow after the logout (up to 20 s: a slow AI tick or a few misses made a fixed
			//5 s too short), then 1 s more. No client logs in meanwhile, or it would be the last packet's sender.
			bool hit = this.fixture.WaitForGameOutputSince("STALESRC_TEST: hit |", mark, TimeSpan.FromSeconds(20));
			Thread.Sleep(1000);
			string log = this.fixture.GameOutputSnapshot().Substring(mark);
			using var back = CommandPermissionTests.Connect("floortests", "Src Returner");
			string after = this.Do(back, "after");
			Assert.DoesNotContain("get_SrcTcpConnection", log); //first: a #1156 regression throws before the push, so no hit line either
			Assert.True(hit, "no hurting, pushing blow within 20 s after the logout; then: " + after);
			Assert.True(after.Contains("rabbitHurt=True knockedBack=True noClientSrc=True"), after);
		}
	}
}
