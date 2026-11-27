namespace SteamEngine.CompiledScripts {

	/// <summary>#1558 - @disruption's args: argv[0] is InterruptCasting. #860 - a blow breaks paralysis and the like
	/// whatever its force, but casting only when it's hard enough (DamageManager.DoesSwingInterrupt).</summary>
	public class DisruptionArgs : ScriptArgs {
		public DisruptionArgs(bool interruptCasting)
			: base(interruptCasting) {
		}

		public bool InterruptCasting {
			get {
				return (bool) this.Argv[0];
			}
		}
	}
}
