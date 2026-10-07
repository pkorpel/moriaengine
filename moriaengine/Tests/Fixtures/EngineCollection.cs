using Xunit;

namespace SteamEngine.Tests.Fixtures {
	/// <summary>
	/// GameCore.Globals/Logger are process-wide, one-shot statics (see EngineTestFixture) -
	/// there's no safe way to isolate them per test class. Every test class in this assembly
	/// must use [Collection(Name)] so xUnit runs them all sequentially against the one shared
	/// EngineTestFixture, instead of racing another test class's static touch against this
	/// fixture's setup (xUnit runs different collections in parallel by default).
	/// </summary>
	[CollectionDefinition(Name)]
	public class EngineCollection : ICollectionFixture<EngineTestFixture> {
		public const string Name = "Engine";
	}
}
