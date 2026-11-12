using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1467 - Erebor's i_hypno_delka: a mystic possessing a monster, with everything needed to come home (the body,
	/// the alter ego, the two bank boxes, the own look, stats, skills and spot, the experience gained meanwhile). Its timer
	/// ends the possession. The mystic doesn't die in the body (@denyDeath), comes home at a logout (@logout), and earns
	/// nothing until then (@gainExperience counts the gain's base, paid on the way home).</summary>
	[Dialogs.ViewableClass]
	public partial class PossessionPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_hypnoPossession_");

		internal static PossessionPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as PossessionPlugin;
		}

		/// <summary>i_hypno_delka's @timer.</summary>
		public void On_Timer() {
			if (this.Cont is Player self) {
				self.SysMessage("Spojeni s monstrem te jiz prilis vycerpalo");
				MysticHypnosis.SwapBack(self, this, 0, false);
			}
		}

		/// <summary>A possessing mystic doesn't die: the killers are paid as for the monster, and the mystic comes home.</summary>
		public TriggerResult On_DenyDeath(Character killedBy) {
			if (this.Cont is Player self) {
				MysticHypnosis.DiedInTheBody(self, this, killedBy);
			}
			return TriggerResult.Cancel;
		}

		/// <remarks>The swap back removes this plugin and the mystic's paralysis, poison and bleeding inside @logout's loop: if
		/// one of those sat right after this plugin, the older plugins miss @logout (#1468, the loop's gap to fix).</remarks>
		public void On_LogOut() {
			if (this.Cont is Player self) {
				MysticHypnosis.SwapBack(self, this, 0, false);
			}
		}

		/// <summary>addexp while possessing only counts, the gain before any bonus; it's paid (with its bonuses) at home.</summary>
		public TriggerResult On_GainExperience(GainExperienceArgs args) {
			this.gained += args.BaseAmount;
			return TriggerResult.Cancel;
		}
	}

	public partial class PossessionPluginDef {
		public static readonly PossessionPluginDef instance =
			(PossessionPluginDef) new PossessionPluginDef("p_hypnoPossession", "C# scripts", -1).Register();
	}

	/// <summary>#1467 - the alter ego left where the mystic stood (e_hypnotising_mystic_ego, Erebor's tag on the ego), with
	/// its ring of alarm pads looked at every second. A blow or a spell on it, or its death, brings the mystic home.</summary>
	[Dialogs.ViewableClass]
	public partial class AlterEgoPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_hypnoEgo_");

		internal static AlterEgoPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as AlterEgoPlugin;
		}

		/// <summary>The alarm pads' ring.</summary>
		public void On_Timer() {
			this.Timer = 1;
			if (this.Cont is NPC ego) {
				MysticHypnosis.Alarm(ego);
			}
		}

		/// <summary>The ego doesn't die; the mystic comes home hurt.</summary>
		public TriggerResult On_DenyDeath(Character killedBy) {
			if (this.mystic != null) {
				MysticHypnosis.SwapBack(this.mystic, 0, true);
			}
			return TriggerResult.Cancel;
		}

		/// <summary>A blow at the ego brings the mystic home with half the hits.</summary>
		public void On_GetSwingLanding(WeaponSwingArgs swing) {
			if (this.mystic != null) {
				this.mystic.SysMessage("Tvou mysl zalila rezava bolest");
				MysticHypnosis.SwapBack(this.mystic, 2, false);
			}
		}

		/// <summary>e_hypnotising_mystic_ego's @spelleffect: a spell on the ego brings the mystic home, and does nothing.</summary>
		public TriggerResult On_SpellEffect(SpellEffectArgs effectArgs) {
			if (this.mystic != null) {
				this.mystic.SysMessage("Tvou mysli projela vlna energie");
				MysticHypnosis.SwapBack(this.mystic, 2, false);
			}
			return TriggerResult.Cancel;
		}
	}

	public partial class AlterEgoPluginDef {
		public static readonly AlterEgoPluginDef instance =
			(AlterEgoPluginDef) new AlterEgoPluginDef("p_hypnoEgo", "C# scripts", -1).Register();
	}
}
