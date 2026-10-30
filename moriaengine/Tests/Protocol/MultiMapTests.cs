using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#841 - plane 2 is map2.mul, as on Erebor: its regions have ground, a client going there is told to change
	/// its map and back, and one logging in there is told too. Server-side helpers: scripts/Tests/MultiMapTests.cs.</summary>
	[Collection(MultiMapCollection.Name)]
	public class MultiMapTests {
		private const string Account = "multimapplayer";
		private const string AdminAccount = "multimapadmin"; //the server's first account: a GM, can turn on auto-creation
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(20);
		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public MultiMapTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void Plane2IsItsOwnMap() {
			using (var client = this.Connect(Account, "Map Walker")) {
				Assert.Equal(0, client.ReadFacetChange(Wait)); //at login

				string walks = this.Ask(client, ".TestMultiMapWalks", "MULTIMAP_WALKS:");
				Assert.Contains("regions=14 sizeX=2304 sizeY=1600 plane0=6144,4096 land=-10", walks);
				Assert.Contains("stuck=[]", walks);

				this.Ask(client, ".TestMultiMapGo \"2771 2761 0 0\"", "MULTIMAP_AT:"); //from Gondor to the plane 2 cave and back
				Assert.Contains("m=2 region=Ered Mithrinska Jeskyne", this.Ask(client, ".TestMultiMapGo \"19 1196 -10 2\"", "MULTIMAP_AT:"));
				Assert.Equal(2, client.ReadFacetChange(Wait));
				this.Ask(client, ".TestMultiMapGo \"2771 2761 0 0\"", "MULTIMAP_AT:");
				Assert.Equal(0, client.ReadFacetChange(Wait));
				this.Ask(client, ".TestMultiMapGo \"19 1196 -10 2\"", "MULTIMAP_AT:");
				Assert.Equal(2, client.ReadFacetChange(Wait));
			}

			//logging back in on plane 2: without the login's map change, the client would stay on map 0
			using (var client = this.Relog(Account)) {
				Assert.Equal(2, client.ReadFacetChange(Wait));
				Assert.Contains("m=2 region=Ered Mithrinska Jeskyne", this.Ask(client, ".TestMultiMapGo \"19 1196 -10 2\"", "MULTIMAP_AT:"));
			}
		}

		/// <summary>#848: .go x,y,z,m reached the plane's number as z and never changed plane; .go x y z threw.</summary>
		[Fact]
		public void GoCommandTakesZAndPlane() {
			using (var gm = this.ConnectOnce(AdminAccount, "Map Goer")) {
				Assert.Contains("x=1 y=1 ", this.GoAndReport(gm, ".go 1,1,1,10"));
				Assert.Contains(" m=10", this.GoAndReport(gm, ".go 1,1,1,10"));
				Assert.Contains("x=19 y=1196 z=-10 m=2", this.GoAndReport(gm, ".go 19 1196 -10 2")); //negative z, spaces, the other map
				Assert.Contains("x=2771 y=2761", this.GoAndReport(gm, ".go 2771,2761,0,0"));
				string threeArgs = this.GoAndReport(gm, ".go 2772 2761 0");
				Assert.Contains("x=2772 y=2761", threeArgs);
				Assert.Contains(" m=0", threeArgs);
				Assert.Contains(" m=10", this.GoAndReport(gm, ".go 5331 2745 10 10"));
				Assert.Contains(" m=10", this.GoAndReport(gm, ".go 5332 2745")); //no plane given: stays on this one
			}
		}

		private string GoAndReport(TestGameClient client, string go) {
			client.SendSpeech(go);
			Thread.Sleep(300);
			return this.Ask(client, ".TestMultiMapWhere", "MULTIMAP_WHERE:");
		}

		private TestGameClient Relog(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", this.WaitForServerListingAndGetRedirectPort(account));
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendSelectCharacter(0);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log back in.", lastFailure);
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			if (!this.fixture.WaitForGameOutputSince(marker, mark, Wait)) {
				string output = this.fixture.GameOutputSnapshot();
				Assert.Fail("no '" + marker + "' after " + speech + ", server said:\n" + output.Substring(Math.Min(mark, output.Length)));
			}
			return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
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

		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectOnce(AdminAccount, "Map Admin")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, Wait));
			}
			autoAccountCreationEnabled = true;
		}

		private TestGameClient Connect(string account, string charName) {
			this.EnsureAutoAccountCreationEnabled();
			return this.ConnectOnce(account, charName);
		}

		//character names may only hold letters and spaces - a digit gets the client dropped
		private TestGameClient ConnectOnce(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			Exception firstFailure = null; //later attempts only say the account is in use, by the first one
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (Exception e) when ((e is InvalidOperationException) || (e is System.IO.IOException)) {
					game.Dispose();
					firstFailure = firstFailure ?? e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", firstFailure);
		}

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
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
			throw new TimeoutException("Login server never listed a game server.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MultiMapCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MultiMapPair";
	}
}
