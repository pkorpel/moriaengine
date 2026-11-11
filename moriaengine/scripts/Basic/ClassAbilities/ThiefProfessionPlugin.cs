namespace SteamEngine.CompiledScripts {

	/// <summary>#1433 - the thief's profession plugin (thief.def's professionPluginDef), on #1429's pattern. It holds what
	/// Erebor kept in the thief's tags: when hiding began and a backstab under way, the readied throat slit and shield
	/// strike with their cooldowns, and the knife throw's and round strike's cooldowns. e_thief's @AfterSwing is its
	/// @swingLanding. The mask stays on the character (ThiefMask): it swaps the character's own name, karma and realm.</summary>
	[Dialogs.ViewableClass]
	public partial class ThiefProfessionPlugin {

		/// <summary>The thief's plugin, null for anyone else.</summary>
		internal static ThiefProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as ThiefProfessionPlugin;
		}

		/// <summary>e_thief's @AfterSwing, in its order: from behind and the throat slit, the war fork's charge, then its
		/// double swing, which spends the slit.</summary>
		public void On_SwingLanding(WeaponSwingArgs swing) {
			ThiefAbilities.ModifyOwnBlow(this, swing);
			ThiefAbilities.ChargeWarFork(this, swing);
			ThiefAbilities.AfterFencingBlow(this, swing);
		}
	}
}
