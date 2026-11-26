using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1433 - the thief's profession plugin (thief.def's professionPluginDef), on #1429's pattern. It holds what
	/// Erebor kept in the thief's tags: when hiding began and a backstab under way, the readied throat slit and shield
	/// strike with their cooldowns, and the knife throw's and round strike's cooldowns. e_thief's @AfterSwing is its
	/// @hit. The mask stays on the character (ThiefMask): it swaps the character's own name, karma and realm.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class ThiefProfessionPlugin : ProfessionPlugin {
		[SaveableData, CopyableData]
		internal double hiddenAt = 0;

		[SaveableData, CopyableData]
		internal bool stabbing = false;

		[SaveableData, CopyableData]
		internal bool throatReady = false;

		[SaveableData, CopyableData]
		internal double nextThroat = 0;

		[SaveableData, CopyableData]
		internal bool shieldReady = false;

		[SaveableData, CopyableData]
		internal double nextShield = 0;

		[SaveableData, CopyableData]
		internal double nextThrow = 0;

		[SaveableData, CopyableData]
		internal double nextStrike = 0;

		/// <summary>The thief's plugin, null for anyone else.</summary>
		internal static ThiefProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as ThiefProfessionPlugin;
		}

		/// <summary>#1459 - the shared stun blow (the base's), then the shield strike, once a landed blow's damage is dealt
		/// (#1558: @afterSwing, was @afterHit).</summary>
		public override void On_AfterSwing(WeaponSwingArgs swing) {
			base.On_AfterSwing(swing);
			if (!swing.Dodged && (this.Cont is Player self) && (swing.defender is NPC target) && !target.IsDeleted) {
				ThiefAbilities.TryShieldStrike(self, target);
			}
		}

		/// <summary>e_thief's @AfterSwing, in its order: from behind and the throat slit, the war fork's charge, then its
		/// double swing, which spends the slit. #1558 - @hit, a dodged blow too, as Erebor's @AfterSwing.</summary>
		public void On_Hit(WeaponSwingArgs swing) {
			ThiefAbilities.ModifyOwnBlow(this, swing);
			ThiefAbilities.ChargeWarFork(this, swing);
			ThiefAbilities.AfterFencingBlow(this, swing);
		}
	}

	[PluginDefTemplate]
	public partial class ThiefProfessionPluginDef : ProfessionPluginDef {
	}
}
