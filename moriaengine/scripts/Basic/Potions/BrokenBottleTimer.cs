using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>The bottle is still in the hand while it's put down, so it goes a moment later.</summary>
	[SaveableClass, DeepCopyableClass]
	public class BrokenBottleTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_broken_bottle_");

		[LoadingInitializer, DeepCopyImplementation]
		public BrokenBottleTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Item item) && !item.IsDeleted) {
				item.Delete();
			}
		}
	}
}
