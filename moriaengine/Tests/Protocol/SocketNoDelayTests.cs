using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1222 - the game server's socket to a client has Nagle off, so a step's ack isn't held back behind earlier data
	/// waiting for the client's delayed ACK (~40 ms in a crowd here, up to 200 ms on Windows). Server side:
	/// scripts/Tests/SocketNoDelayTests.cs.</summary>
	public class SocketNoDelayTests {
		[Fact]
		public void TheServersSocketToAClientSendsAtOnce() {
			using (var fixture = new GameServerPairFixture())
			using (var client = CommandPermissionTests.Connect("nodelay", "No Delay")) {
				string report = CommandPermissionTests.Ask(fixture, client, ".TestSocketNoDelay", "SOCKET_NODELAY:");
				Assert.True(report.StartsWith("SOCKET_NODELAY: True", StringComparison.Ordinal), "the server reported: " + report);
			}
		}
	}
}
