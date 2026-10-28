using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// Pure logic test for MonsterSpecialAttacks.ComputeMaxPushSteps (#758 - knockback's new
	/// configurable multi-tile push) - no engine boot needed, since this is the exact
	/// "walk outward until blocked" decision PushAway makes, factored out specifically so it's
	/// directly testable without a live Map/Character.
	/// </summary>
	public class KnockbackPushDistanceTests {
		[Fact]
		public void ReachesFullConfiguredDistanceWhenNothingBlocks() {
			int steps = MonsterSpecialAttacks.ComputeMaxPushSteps(3, step => true);
			Assert.Equal(3, steps);
		}

		[Fact]
		public void StopsAtTheLastOpenStepBeforeABlocker() {
			//open at 1 and 2, blocked at 3 - should land on 2, not 0 and not 3.
			int steps = MonsterSpecialAttacks.ComputeMaxPushSteps(3, step => step < 3);
			Assert.Equal(2, steps);
		}

		[Fact]
		public void ReturnsZeroWhenTheVeryFirstStepIsBlocked() {
			int steps = MonsterSpecialAttacks.ComputeMaxPushSteps(3, step => false);
			Assert.Equal(0, steps);
		}

		[Fact]
		public void NeverExceedsMaxDistanceEvenIfEveryStepIsOpen() {
			int steps = MonsterSpecialAttacks.ComputeMaxPushSteps(5, step => true);
			Assert.Equal(5, steps);
		}

		[Fact]
		public void ZeroMaxDistanceAlwaysReturnsZero() {
			int steps = MonsterSpecialAttacks.ComputeMaxPushSteps(0, step => true);
			Assert.Equal(0, steps);
		}
	}
}
