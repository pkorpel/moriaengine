using System.IO;
using Xunit;

namespace SteamEngine.Tests.Fixtures {
	[Collection(EngineCollection.Name)]
	public class EngineTestFixtureTests {
		private readonly EngineTestFixture fixture;

		public EngineTestFixtureTests(EngineTestFixture fixture) {
			this.fixture = fixture;
		}

		[Fact]
		public void GlobalsInitializesWithoutThrowing() {
			Assert.False(string.IsNullOrEmpty(Globals.ServerName));
		}

		[Fact]
		public void GlobalsWroteADefaultIniIntoTheTempDir() {
			Assert.True(File.Exists(Path.Combine(this.fixture.TempDir, "steamengine.ini")));
		}

		[Fact]
		public void LoggerIsUsable() {
			// Should not throw, regardless of whether file logging is on.
			Common.Logger.WriteLine((Common.LogStr) "EngineTestFixtureTests smoke check");
		}
	}
}
