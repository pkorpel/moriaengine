using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1431 - the ranger's profession plugin (ranger.def's professionPluginDef), on #1429's pattern. It holds the
	/// cooldowns of knockback, split shot and crushing arrow (Erebor's serv.time tags) and the killing spree's last target.
	/// The spree adds to the ranger's blows in @hit and gains its charges in @kill. What the ranger's commands do
	/// stays in RangerAbilities; the critical shot comes from the counter the classes share.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class RangerProfessionPlugin : ProfessionPlugin {
		[SaveableData, CopyableData]
		internal double nextKnockback = 0;

		[SaveableData, CopyableData]
		internal double nextSplitShot = 0;

		[SaveableData, CopyableData]
		internal double nextCrushingArrow = 0;

		[SaveableData, CopyableData]
		internal Character spreeTarget = null;

		/// <summary>The ranger's plugin, null for anyone else.</summary>
		internal static RangerProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as RangerProfessionPlugin;
		}

		/// <summary>e_killingSpreeCharge on the ranger's blow, after the critical shot. #1558 - @hit, a dodged blow too, as
		/// Erebor's @AfterSwing.</summary>
		public void On_Hit(WeaponSwingArgs swing) {
			RangerAbilities.ApplySpree(this, swing);
		}

		/// <summary>Erebor's @finalBlow: a monster the ranger killed charges the spree.</summary>
		public void On_Kill(Character victim) {
			if (victim is NPC npc) {
				RangerAbilities.OnKill(this, npc);
			}
		}
	}

	[PluginDefTemplate]
	public partial class RangerProfessionPluginDef : ProfessionPluginDef {
	}
}
