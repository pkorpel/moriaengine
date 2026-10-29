using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #813 - the Erebor archive's region events, ported to C# (RegionEventSystem): real archive
	/// areas carrying r_SafeZone, r_nosumon, r_playerhouse, r_nofield, r_notele and r_doly_nohorse,
	/// entered by a real teleport or step so the engine's own region transition runs.
	/// </summary>
	[Collection(RegionEventCollection.Name)]
	public class RegionEventTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltestb";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public RegionEventTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>r_SafeZone: a (non-GM) player is invulnerable inside, and not once out again.</summary>
		[Fact]
		public void SafeZoneMakesAPlayerInvulnerableOnlyInside() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "SafeZoneWalker")) {
				Assert.Contains("invul=False", this.Go(client, "r_safezone", "outside"));
				string inside = this.Go(client, "r_safezone", "inside");
				Assert.Contains("invul=True", inside);
				Assert.DoesNotContain("area=none", inside);
				Assert.Contains("invul=False", this.Go(client, "r_safezone", "outside"));
			}
		}

		/// <summary>r_nosumon and r_playerhouse (removeAgressiveSummon): a summoned creature
		/// entering is unsummoned; a summoned mount and an ordinary NPC stay.</summary>
		[Theory]
		[InlineData("r_nosumon")]
		[InlineData("r_playerhouse")]
		public void SummonedCreatureEnteringIsUnsummoned(string eventName) {
			using (var client = this.ConnectLoginAndCreateCharacter("SummonChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRegionSummonsEnter \"" + eventName + "\"");
				string marker = "REGION_SUMMONS: " + eventName;
				Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(15)), "no " + marker);
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
				Assert.DoesNotContain("area=none", line);
				int summon = ExtractInt(line, "summon=");
				int mount = ExtractInt(line, "mount=");
				int plain = ExtractInt(line, "plain=");

				Thread.Sleep(2000); //the despawn timer runs out on the next timer tick
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRegionThingsAlive " + summon + " " + mount + " " + plain);
				Assert.True(this.fixture.WaitForGameOutputSince("REGION_ALIVE:", mark, TimeSpan.FromSeconds(10)));
				string alive = LastLineContaining(this.fixture.GameOutputSnapshot(), "REGION_ALIVE:");
				Assert.Contains(" " + summon + "=False", alive);
				Assert.Contains(" " + mount + "=True", alive);
				Assert.Contains(" " + plain + "=True", alive);
			}
		}

		/// <summary>r_nofield blocks the field spells, r_notele blocks Teleport - for players, not GMs.</summary>
		[Fact]
		public void NoFieldAndNoTeleBlockTheirSpellsForPlayers() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "SpellZonePlayer")) {
				string field = this.Spells(client, "r_nofield");
				Assert.Contains("gm=False", field);
				Assert.Contains("field=False", field);
				Assert.Contains("other=True", field);
				string tele = this.Spells(client, "r_notele");
				Assert.Contains("teleport=False", tele);
				Assert.Contains("other=True", tele);
			}
			using (var gm = this.ConnectLoginAndCreateCharacter("SpellZoneGm")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				gm.SendSpeech(".TestSetGmOn");
				Assert.True(this.fixture.WaitForGameOutputSince("GM_MODE_TEST:", mark, TimeSpan.FromSeconds(10)));
				string field = this.Spells(gm, "r_nofield");
				Assert.Contains("gm=True", field);
				Assert.Contains("field=True", field);
			}
		}

		/// <summary>r_doly_nohorse (e_nohorse): a rider taking a step in the mines gets off.</summary>
		[Fact]
		public void RiderSteppingInTheMinesIsDismounted() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MineRider")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRegionMountedStep \"r_doly_nohorse\" \"c_horse_tan\"");
				Assert.True(this.fixture.WaitForGameOutputSince("REGION_STEP: r_doly_nohorse", mark, TimeSpan.FromSeconds(30)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "REGION_STEP: r_doly_nohorse");
				Assert.DoesNotContain(" none", line);
				Assert.Contains("ridingBefore=True", line);
				Assert.Contains("ridingAfter=False", line);
			}
		}

		private string Go(TestGameClient client, string eventName, string where) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestRegionEventGo \"" + eventName + "\" \"" + where + "\"");
			string marker = "REGION_GO: " + eventName + " " + where;
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(15)), "no " + marker);
			return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
		}

		private string Spells(TestGameClient client, string eventName) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestRegionSpellsAllowed \"" + eventName + "\"");
			string marker = "REGION_SPELLS: " + eventName;
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(15)), "no " + marker);
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
			Assert.DoesNotContain("area=none", line);
			return line;
		}

		private static int ExtractInt(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while (end < line.Length && char.IsDigit(line[end])) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx).TrimEnd('\r');
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			return this.ConnectLoginAndCreateCharacter(Account, charName);
		}

		//character names may only hold letters and spaces - a digit gets the client dropped
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

		private static bool autoAccountCreationEnabled;

		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "RegionAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
			autoAccountCreationEnabled = true;
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class RegionEventCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RegionEventPair";
	}
}
