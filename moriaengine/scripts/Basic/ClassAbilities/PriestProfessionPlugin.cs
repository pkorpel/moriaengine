namespace SteamEngine.CompiledScripts {

	/// <summary>#1435 - the priest's profession plugin (priest.def's professionPluginDef), on #1429's pattern. It holds the
	/// self-resurrection's cooldown (Erebor's tag.priest_nextselfres). The time of death and the rule that a Resurrection
	/// waits 5 s after one are every player's (e_allplayers) and stay on the character; the life candle is an item.</summary>
	[Dialogs.ViewableClass]
	public partial class PriestProfessionPlugin {

		/// <summary>The priest's plugin, null for anyone else.</summary>
		internal static PriestProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as PriestProfessionPlugin;
		}
	}
}
