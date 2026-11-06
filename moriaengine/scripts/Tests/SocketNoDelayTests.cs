using System;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1222 - what the game server's socket to the caller's client says about Nagle. Client side:
	/// Tests/Protocol/SocketNoDelayTests.cs.</summary>
	public static class SocketNoDelayTests {
		[SteamFunction]
		public static void TestSocketNoDelay(Character self) {
			GameState state = self.GameState;
			Console.WriteLine("SOCKET_NODELAY: " + ((state != null) && state.Conn.NoDelay));
		}
	}
}
