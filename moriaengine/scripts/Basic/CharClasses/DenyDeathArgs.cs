namespace SteamEngine.CompiledScripts {

	/// <summary>#1558 - @denyDeath's args: argv[0] the result, argv[1] the killer (null when nobody is named). A handler
	/// refuses by cancelling or setting Result; it doesn't act (a refusal's consequences go elsewhere, see
	/// PossessionPlugin). A refused death leaves the character alive at 1 hit point, or at what a handler set.</summary>
	public class DenyDeathArgs : DenyTriggerArgs {
		public readonly Character killedBy;

		public DenyDeathArgs(Character killedBy)
			: base(DenyResultMessages.Allow, killedBy) {
			this.killedBy = killedBy;
		}
	}
}
