using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1152 - a ridden mount is out of the world: its AI does nothing. Server side: scripts/Tests/RiddenMountTests.cs.</summary>
	[Collection(FloorAttackCollection.Name)]
	public class RiddenMountTests {
		private readonly GameServerPairFixture fixture;

		public RiddenMountTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Do(TestGameClient client, string what) {
			return CommandPermissionTests.Ask(this.fixture, client, ".TestRiddenMount \"" + what + "\"", "MOUNT_TEST: " + what + " ");
		}

		[Fact]
		public void ARiddenMountDoesNotWalkAfterItsRider() {
			using var client = CommandPermissionTests.Connect("mounttests", "Zostrich Rider");
			string ride = this.Do(client, "ride");
			Assert.True(ride.Contains("riding=True mountOut=True"), ride);
			Thread.Sleep(4000);
			string after = this.Do(client, "after");
			Assert.True(after.Contains("mountStayed=True"), after);
			Assert.True(after.Contains("dismountedUnderRider=True back=True"), after);
		}
	}
}
