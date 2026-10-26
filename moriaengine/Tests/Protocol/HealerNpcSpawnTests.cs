using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for two real user-reported bugs hit creating c_f_HEALER/c_m_HEALER
	/// (both widespread archive chardef patterns, not healer-specific - "ITEMNEWBIE=
	/// random_female_hair/random_male_hair/random_facial_hair" are native Sphere random-hair-
	/// pick keywords, and a "COLOR=" line right after an item line colors THAT item, including
	/// the "match_hair" pseudo-color meaning "copy whatever color the item equipped just before
	/// this one got"): the converter previously collapsed every "COLOR=" line inside a chardef's
	/// @Create body into one top-level, last-line-wins Character.Color field - "random_*_hair"
	/// silently failed to equip (logged, not fatal), but "match_hair" reaching
	/// ConvertTools.ParseAnyNumber as a literal crashed character creation outright
	/// (uncaught inside Thing's own constructor). NPC.On_Create now recognizes both as real
	/// mechanics instead.
	/// </summary>
	[Collection(HealerNpcSpawnCollection.Name)]
	public class HealerNpcSpawnTests {
		private const string Account = "healernpcspawntest";
		private const string Password = "healernpcspawntest";
		private const string CharName = "HealerSpawner";

		[Fact]
		public void SpawningBothHealersSucceedsAndMatchesHairColor() {
			using (var fixture = new GameServerPairFixture()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					client.ReadCharacterList();
					client.SendCreateCharacter(CharName);

					//c_f_HEALER: "ITEMNEWBIE=random_female_hair / COLOR=colors_hair" - no
					//match_hair involved, but previously logged a spurious "not a known
					//ItemDef" warning and never actually equipped any hair at all.
					int mark = fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestSpawnHealer \"C_f_HEALER\"");
					Assert.True(fixture.WaitForGameOutputSince("HEALER_SPAWNED:", mark, TimeSpan.FromSeconds(10)));
					string femaleLine = LastLineContaining(fixture.GameOutputSnapshot(), "HEALER_SPAWNED:");
					Assert.DoesNotContain("hair=none", femaleLine);

					//c_m_HEALER: "ITEMNEWBIE=random_male_hair / COLOR=colors_hair /
					//ITEMNEWBIE=random_facial_hair / COLOR=match_hair" - this is the one that
					//previously crashed creation entirely. A real exception here (the
					//pre-fix behavior) would blow up this whole Fact, not just fail an assert.
					mark = fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestSpawnHealer \"C_m_HEALER\"");
					Assert.True(fixture.WaitForGameOutputSince("HEALER_SPAWNED:", mark, TimeSpan.FromSeconds(10)));
					string maleLine = LastLineContaining(fixture.GameOutputSnapshot(), "HEALER_SPAWNED:");
					Assert.DoesNotContain("hair=none", maleLine);
					Assert.DoesNotContain("beard=none", maleLine);

					//the real point of "match_hair": the beard's own color should exactly
					//match the hair's - not left at its own default, not a parse crash.
					string hairColor = ExtractColor(maleLine, "hair=Color");
					string beardColor = ExtractColor(maleLine, "beard=Color");
					Assert.Equal(hairColor, beardColor);
				}
			}
		}

		private static string ExtractColor(string line, string prefix) {
			int idx = line.IndexOf(prefix, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + prefix + "' in:\n" + line);
			int start = idx + prefix.Length;
			int end = line.IndexOf(' ', start);
			if (end < 0) {
				end = line.Length;
			}
			return line.Substring(start, end - start);
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture) {
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

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}
	}

	[CollectionDefinition(HealerNpcSpawnCollection.Name, DisableParallelization = true)]
	public class HealerNpcSpawnCollection {
		public const string Name = "HealerNpcSpawnPair";
	}
}
