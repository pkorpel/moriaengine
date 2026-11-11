namespace SteamEngine.CompiledScripts {

	/// <summary>#1431 - the ranger's profession plugin (ranger.def's professionPluginDef), on #1429's pattern. It holds the
	/// cooldowns of knockback, split shot and crushing arrow (Erebor's serv.time tags) and the killing spree's last target.
	/// The spree adds to the ranger's blows in @swingLanding and gains its charges in @kill. What the ranger's commands do
	/// stays in RangerAbilities; the critical shot comes from the counter the classes share.</summary>
	[Dialogs.ViewableClass]
	public partial class RangerProfessionPlugin {

		/// <summary>The ranger's plugin, null for anyone else.</summary>
		internal static RangerProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as RangerProfessionPlugin;
		}

		/// <summary>e_killingSpreeCharge on the ranger's blow, after the critical shot.</summary>
		public void On_SwingLanding(WeaponSwingArgs swing) {
			RangerAbilities.ApplySpree(this, swing);
		}

		/// <summary>Erebor's @finalBlow: a monster the ranger killed charges the spree.</summary>
		public void On_Kill(Character victim) {
			if (victim is NPC npc) {
				RangerAbilities.OnKill(this, npc);
			}
		}
	}
}
