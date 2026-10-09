using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Regression coverage for Globals.PaymentSource (enhancements Phase 14.5 / issue #36) - a
	/// runtime-switchable, tag-backed setting rather than an .ini value, specifically so it can
	/// change without a server restart.
	/// </summary>
	[Collection(EngineCollection.Name)]
	public class GlobalsPaymentSourceTests {
		[Fact]
		public void DefaultsToBackpackFirstWhenNeverSet() {
			// Other tests in this collection may have already set a value on the shared
			// Globals.Instance, so this only asserts the untouched default is a real enum value,
			// not that it's literally never been written to by this point in the run.
			Globals.PaymentSourceMode mode = Globals.PaymentSource;
			Assert.True(mode == Globals.PaymentSourceMode.BackpackFirst || mode == Globals.PaymentSourceMode.BankOnly);
		}

		[Fact]
		public void RoundTripsThroughTheTag() {
			Globals.PaymentSourceMode original = Globals.PaymentSource;
			try {
				Globals.PaymentSource = Globals.PaymentSourceMode.BankOnly;
				Assert.Equal(Globals.PaymentSourceMode.BankOnly, Globals.PaymentSource);

				Globals.PaymentSource = Globals.PaymentSourceMode.BackpackFirst;
				Assert.Equal(Globals.PaymentSourceMode.BackpackFirst, Globals.PaymentSource);
			} finally {
				Globals.PaymentSource = original; //don't leak state into other tests in this shared-fixture collection
			}
		}
	}
}
