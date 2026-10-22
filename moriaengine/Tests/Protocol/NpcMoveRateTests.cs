using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #590 - proves NPCDef.MoveRate actually drives real movement speed through
	/// NPCAiTimer's step-budget accumulator, not just that the `.def` field parses. Spawns
	/// two otherwise-identical NPCs side by side - one with MoveRate=6 (double the real
	/// baseline of 3), one left unconfigured (defaults to that same baseline) - sends both
	/// toward the same distant goal point, waits several real AI ticks, and confirms the
	/// faster one covered strictly more ground than the baseline one over the same wall-clock
	/// window.
	/// </summary>
	[Collection(NpcMoveRateCollection.Name)]
	public class NpcMoveRateTests {
		private const string Account = "npcmoveratetest";
		private const string Password = "npcmoveratetest";
		private const string CharName = "MoveRateChecker";

		private readonly GameServerPairFixture fixture;

		public NpcMoveRateTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void HigherMoveRateCoversMoreGroundThanBaselineOverTheSameWindow() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, CharName)) {
				//This map's terrain varies wildly tile to tile (documented elsewhere in this
				//codebase - CombatScenarioTests/MovementSpeedTests both note the same thing) -
				//TestTeleportToOpenGround only guarantees the SPAWN tile is clear, not a long
				//runway in every direction. Retry the whole setup on a fresh open-ground spot
				//rather than asserting against one possibly-unlucky location.
				int fastDisplacement = -1;
				int baselineDisplacement = -1;
				Exception lastFailure = null;
				for (int attempt = 0; attempt < 3; attempt++) {
					try {
						int mark = this.fixture.GameOutputSnapshot().Length;
						client.SendSpeech(".TestTeleportToOpenGround");
						Assert.True(this.fixture.WaitForGameOutputSince("OPEN_GROUND_FOUND:", mark, TimeSpan.FromSeconds(10)));
						Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

						mark = this.fixture.GameOutputSnapshot().Length;
						client.SendSpeech(".TestSpawnMoveRateNpcs");
						Assert.True(this.fixture.WaitForGameOutputSince("MOVERATE_TEST_SPAWNED:", mark, TimeSpan.FromSeconds(10)));
						string spawnLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MOVERATE_TEST_SPAWNED:");
						uint fastUid = ExtractUidAfter(spawnLine, "fastUid=");
						uint baselineUid = ExtractUidAfter(spawnLine, "baselineUid=");

						//Several real 1.5s AI ticks' worth of wall-clock time - both NPCs need a
						//real chance to act on their PetGoalPoint before comparing how far each
						//one got.
						Thread.Sleep(4500);

						fastDisplacement = this.ReportDisplacement(client, fastUid);
						baselineDisplacement = this.ReportDisplacement(client, baselineUid);

						if ((baselineDisplacement > 0) && (fastDisplacement > baselineDisplacement)) {
							return; //success
						}
						lastFailure = null;
					} catch (Exception e) {
						lastFailure = e;
					}
				}

				Assert.True(baselineDisplacement > 0, "Expected the baseline NPC to have moved at all.");
				Assert.True(fastDisplacement > baselineDisplacement,
					"Expected MoveRate=6 to cover strictly more ground than the baseline over the same window (fast=" +
					fastDisplacement + ", baseline=" + baselineDisplacement + ")." +
					(lastFailure != null ? " Last attempt threw: " + lastFailure : ""));
			}
		}

		/// <summary>#599 - proves a per-instance MoveRate override (what .info's reflection-
		/// based editor would do to one specific spawned monster) actually takes effect,
		/// instead of NPCAiTimer silently continuing to read the species-level NPCDef default.
		/// Deliberately SWAPS the two NPCs' effective speeds via instance overrides (the
		/// MoveRate=6-by-Def one gets overridden down to 1.5, the baseline one gets overridden
		/// up to 6) - if the override were a no-op, the original (Def-driven) ordering would
		/// still hold; only a real instance-level override reverses it.</summary>
		[Fact]
		public void PerInstanceOverrideActuallyChangesSpeed() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MoveRateOverride")) {
				int defDrivenFastDisplacement = -1;
				int defDrivenBaselineDisplacement = -1;
				Exception lastFailure = null;
				for (int attempt = 0; attempt < 3; attempt++) {
					try {
						int mark = this.fixture.GameOutputSnapshot().Length;
						client.SendSpeech(".TestTeleportToOpenGround");
						Assert.True(this.fixture.WaitForGameOutputSince("OPEN_GROUND_FOUND:", mark, TimeSpan.FromSeconds(10)));
						Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

						mark = this.fixture.GameOutputSnapshot().Length;
						client.SendSpeech(".TestSpawnMoveRateNpcs");
						Assert.True(this.fixture.WaitForGameOutputSince("MOVERATE_TEST_SPAWNED:", mark, TimeSpan.FromSeconds(10)));
						string spawnLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MOVERATE_TEST_SPAWNED:");
						uint defDrivenFastUid = ExtractUidAfter(spawnLine, "fastUid=");
						uint defDrivenBaselineUid = ExtractUidAfter(spawnLine, "baselineUid=");

						//Swap: the MoveRate=6-by-Def NPC gets overridden down to 1.5; the
						//MoveRate=3-by-Def (baseline) NPC gets overridden up to 6.
						mark = this.fixture.GameOutputSnapshot().Length;
						client.SendSpeech(".TestOverrideNpcMoveRate " + defDrivenFastUid + " 1.5");
						Assert.True(this.fixture.WaitForGameOutputSince("MOVERATE_TEST_OVERRIDDEN: uid=" + defDrivenFastUid, mark, TimeSpan.FromSeconds(10)));

						mark = this.fixture.GameOutputSnapshot().Length;
						client.SendSpeech(".TestOverrideNpcMoveRate " + defDrivenBaselineUid + " 6");
						Assert.True(this.fixture.WaitForGameOutputSince("MOVERATE_TEST_OVERRIDDEN: uid=" + defDrivenBaselineUid, mark, TimeSpan.FromSeconds(10)));

						Thread.Sleep(4500);

						defDrivenFastDisplacement = this.ReportDisplacement(client, defDrivenFastUid);
						defDrivenBaselineDisplacement = this.ReportDisplacement(client, defDrivenBaselineUid);

						//Reversed from HigherMoveRateCoversMoreGroundThanBaselineOverTheSameWindow
						//on purpose - this is the whole point of the swap.
						if ((defDrivenFastDisplacement > 0) && (defDrivenBaselineDisplacement > defDrivenFastDisplacement)) {
							return; //success
						}
						lastFailure = null;
					} catch (Exception e) {
						lastFailure = e;
					}
				}

				Assert.True(defDrivenFastDisplacement > 0, "Expected the overridden-to-slow NPC to have moved at all.");
				Assert.True(defDrivenBaselineDisplacement > defDrivenFastDisplacement,
					"Expected the instance override to reverse which NPC moves faster (overridden-to-slow=" +
					defDrivenFastDisplacement + ", overridden-to-fast=" + defDrivenBaselineDisplacement + ")." +
					(lastFailure != null ? " Last attempt threw: " + lastFailure : ""));
			}
		}

		private int ReportDisplacement(TestGameClient client, uint uid) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportNpcDisplacement " + uid);
			Assert.True(this.fixture.WaitForGameOutputSince("MOVERATE_TEST_DISPLACEMENT:", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MOVERATE_TEST_DISPLACEMENT: uid=" + uid + " ");
			int idx = line.IndexOf("displacement=", StringComparison.Ordinal);
			return int.Parse(line.Substring(idx + "displacement=".Length).Trim());
		}

		private static uint ExtractUidAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + line);
			idx += marker.Length;
			int end = idx;
			while ((end < line.Length) && char.IsDigit(line[end])) {
				end++;
			}
			return uint.Parse(line.Substring(idx, end - idx));
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);

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

	[CollectionDefinition(NpcMoveRateCollection.Name, DisableParallelization = true)]
	public class NpcMoveRateCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcMoveRatePair";
	}
}
