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

		/// <summary>#1173, #1179 - a new steamengine.ini saves hourly and keeps the perf counters off.</summary>
		[Fact]
		public void DefaultIniAutosavesHourlyWithPerfCountersOff() {
			string ini = File.ReadAllText(Path.Combine(this.fixture.TempDir, "steamengine.ini"));
			Assert.Contains("autosaveMinutes = 60", ini);
			Assert.Contains("[diagnostics]", ini);
			Assert.Contains("perfCounters = False", ini);
		}

		[Fact]
		public void LoggerIsUsable() {
			// Should not throw, regardless of whether file logging is on.
			Common.Logger.WriteLine((Common.LogStr) "EngineTestFixtureTests smoke check");
		}
	}
}
