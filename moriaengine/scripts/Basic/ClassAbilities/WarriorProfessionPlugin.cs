namespace SteamEngine.CompiledScripts {

	/// <summary>#1429 - the warrior's profession plugin (warrior.def's professionPluginDef), the original code's own way
	/// to give a profession its behaviour, as mage's p_profession_mage. It holds the stance and the cooldowns
	/// (Erebor's tag.defending and serv.time tags) and changes the blows through @swingLanding and @getSwingLanding,
	/// raised where Erebor's @AfterSwing and @aftergetswing ran: before the dodge. What the warrior's commands do stays
	/// in WarriorAbilities.</summary>
	[Dialogs.ViewableClass]
	public partial class WarriorProfessionPlugin {

		/// <summary>The warrior's plugin, null for anyone else.</summary>
		internal static WarriorProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as WarriorProfessionPlugin;
		}

		/// <summary>The warrior's own blow (e_war's double swing, the stance), after the critical blow in
		/// ClassAbilities.ModifyOwnBlow.</summary>
		public void On_SwingLanding(WeaponSwingArgs swing) {
			WarriorAbilities.ModifyOwnBlow(this, swing);
		}

		/// <summary>A blow at the warrior: the stance, then the shield every class may learn.</summary>
		public override void On_GetSwingLanding(WeaponSwingArgs swing) {
			WarriorAbilities.ModifyIncomingBlow(this, swing);
			base.On_GetSwingLanding(swing);
		}
	}
}
