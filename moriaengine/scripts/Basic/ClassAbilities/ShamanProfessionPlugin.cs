using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1442 - the shaman's profession plugin (shaman.def's professionPluginDef), on #1429's pattern: the
	/// cooldowns of voodoo, the pacts and the life pact (Erebor's serv.time tags). The stats ritual and the voodoo shield
	/// are effects on the character, each its own plugin below.</summary>
	[Dialogs.ViewableClass]
	public partial class ShamanProfessionPlugin {

		/// <summary>The shaman's plugin, null for anyone else.</summary>
		internal static ShamanProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as ShamanProfessionPlugin;
		}
	}

	/// <summary>#1442 - i_voodoo_stats and its e_voodoo_stats: the stats ritual in effect, 1 blood to mind, 2 mind to
	/// blood. It changes the character's own stats, so it's on the character, not on the profession plugin: it outlasts a
	/// profession change, and .voodoorovnovaha needs no class. Death puts the stats back, in its own @death.</summary>
	[Dialogs.ViewableClass]
	public partial class VoodooStatsPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_voodooStats_");

		internal static VoodooStatsPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as VoodooStatsPlugin;
		}

		/// <summary>e_voodoo_stats' @death.</summary>
		public void On_Death() {
			if (this.Cont is Player self) {
				ShamanAbilities.ApplyRitual(self, (this.ritual == 1) ? 2 : 1, 1);
				self.SysMessage("Sila krve te se smrti opousti.");
			}
			this.Delete();
		}
	}

	public partial class VoodooStatsPluginDef {
		public static readonly VoodooStatsPluginDef instance =
			(VoodooStatsPluginDef) new VoodooStatsPluginDef("p_voodooStats", "C# scripts", -1).Register();
	}

	/// <summary>#1442 - [function voodooshield]'s shield, as the energy shield (#1401): its pool is EffectPower, 5 a point,
	/// for 30 s; a monster's spell eats the pool first (ShamanAbilities.AbsorbSpell, at its fixed point in
	/// DamageManager.CauseSpellDamage). When the time is up, it's cancelled, or a spell goes through it, it rests 180 s, then
	/// goes. It stays through death, as Erebor's memory item.</summary>
	[Dialogs.ViewableClass]
	public partial class VoodooShieldPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_voodooShield_");
		internal const double UpSeconds = 30, RestSeconds = 180;

		internal static VoodooShieldPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as VoodooShieldPlugin;
		}

		internal bool IsUp {
			get {
				return !this.resting;
			}
		}

		internal int Pool {
			get {
				return (int) this.EffectPower;
			}
			set {
				this.EffectPower = value;
			}
		}

		internal void StartRest() {
			this.resting = true;
			this.EffectPower = 0;
			this.Timer = RestSeconds;
		}

		public override void On_Timer() {
			if (!this.resting) {
				if (this.Cont is Character self) {
					self.SysMessage("Stit se vycerpal.", ClassAbilities.ClassMessageHue);
				}
				this.StartRest();
				return;
			}
			if (this.Cont is Character holder) {
				holder.SysMessage("Nyni muzes pouzit voodoo stit znovu.", ClassAbilities.ClassMessageHue);
			}
			this.Delete();
		}

		public override string EffectName {
			get {
				return "Voodoo stit";
			}
		}

		public override string EffectDetail {
			get {
				return this.resting ? "odpocinek" : this.Pool.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
		}

		/// <summary>Erebor's memory item stays through death, with its timer.</summary>
		public override void On_Death() {
		}

		/// <summary>Its own lines say when it goes.</summary>
		protected override void EffectEndedMessage(Character cont) {
		}
	}

	public partial class VoodooShieldPluginDef {
		public static readonly VoodooShieldPluginDef instance =
			(VoodooShieldPluginDef) new VoodooShieldPluginDef("p_voodooShield", "C# scripts", -1).Register();
	}
}
