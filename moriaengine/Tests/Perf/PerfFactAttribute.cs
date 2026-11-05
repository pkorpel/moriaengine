using System;
using Xunit;

namespace SteamEngine.Tests.Perf {
	/// <summary>#1179 - a perf harness scenario: skipped unless PERF_RUN=1 (scripts/test-perf.sh sets it). The scenarios boot
	/// their own servers inside the test, never through a collection fixture, so a skipped one boots nothing in the fast tier.
	/// They live outside SteamEngine.Tests.Protocol, so test-full's class list doesn't take them either.</summary>
	public sealed class PerfFactAttribute : FactAttribute {
		public const string Variable = "PERF_RUN";

		public PerfFactAttribute() {
			if (Environment.GetEnvironmentVariable(Variable) != "1") {
				this.Skip = "a perf harness scenario: run it with scripts/test-perf.sh (" + Variable + "=1)";
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class PerfCollection {
		public const string Name = "Perf";
	}
}
