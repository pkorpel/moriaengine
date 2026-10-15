using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Coverage for the GM ".spawn" command (#191) - Erebor's ".spawn" lets a GM place a
	/// "spawn gem" (scripts/Basic/Spawning/spawn.scp, data in SpawnInfo) that periodically creates
	/// NPCs of a chosen chardef near itself. The gem's dialog is a raw-LScript [dialog d_spawn]
	/// built via the ImprovedDialog/GUTA* framework (scripts/Basic/dialogs/ImprovedDialog.cs).
	/// Split across two fresh collections/server processes for the same reason as
	/// CombatDepthTestsA/B (#69) and PetCommandTestsA/B (#70) - new characters on the same account
	/// all enter the world at the same default location, so a successful spawn left behind by one
	/// test would otherwise be visible to (and break the negative assertion of) the other.</summary>
	public abstract class SpawnCommandTestBase {
		protected const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		protected SpawnCommandTestBase(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		protected void AssertSeen(string marker, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutput(marker, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		internal TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Password);

						int serverCount = login.ReadServersList();
						if (serverCount < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}

						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						Assert.True(redirectPort > 0, "Expected a real port in the login redirect.");
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SpawnCommandCollectionA : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SpawnCommandPairA";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SpawnCommandCollectionB : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SpawnCommandPairB";
	}

	[Collection(SpawnCommandCollectionA.Name)]
	public class SpawnCommandTestsA : SpawnCommandTestBase {
		public SpawnCommandTestsA(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>Full end-to-end flow: GM opens ".spawn", submits valid dialog fields (field
		/// ids 10=Amount, 11=Baseid, 12=Mintime, 13=Maxtime, 14=Homedist - see [dialog d_spawn]),
		/// and within a few seconds both the spawn gem and a spawned NPC of the chosen chardef
		/// must be present at the GM's location (spawnGem.AddTimer's first tick is immediate).</summary>
		[Fact]
		public void GmSpawnCommandCreatesGemAndSpawnsNpc() {
			using (var client = this.ConnectLoginAndCreateCharacter("SpawnGmA")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".spawn");
				var (gumpUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				client.SendGumpResponse(gumpUid, 1, new Dictionary<int, string> {
					{ 10, "1" }, { 11, "c_a_jackrabbit" }, { 12, "1" }, { 13, "2" }, { 14, "1" },
				});
				Thread.Sleep(3000);

				client.SendSpeech(".TestVerifySpawnResult");
				this.AssertSeen("SPAWN_TEST: gem=True npc=True", TimeSpan.FromSeconds(10));
			}
		}
	}

	[Collection(SpawnCommandCollectionB.Name)]
	public class SpawnCommandTestsB : SpawnCommandTestBase {
		public SpawnCommandTestsB(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>Invalid chardef text in the Baseid field must be rejected (re-shown as an error,
		/// not silently accepted) rather than creating a gem that can never spawn anything -
		/// spawnValuesMakeSense checks SpawnInfo.HasValidCharDef.</summary>
		[Fact]
		public void InvalidChardefReopensDialogInsteadOfSpawning() {
			using (var client = this.ConnectLoginAndCreateCharacter("SpawnGmB")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".spawn");
				var (gumpUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				client.SendGumpResponse(gumpUid, 1, new Dictionary<int, string> {
					{ 10, "1" }, { 11, "c_this_chardef_does_not_exist" }, { 12, "1" }, { 13, "2" }, { 14, "1" },
				});
				// Rejection re-opens [dialog d_spawn] - confirms the request was rejected rather
				// than silently creating a non-functional spawn gem.
				client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				client.SendSpeech(".TestVerifySpawnResult");
				this.AssertSeen("SPAWN_TEST: gem=False npc=False", TimeSpan.FromSeconds(10));
			}
		}
	}
}
