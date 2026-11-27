using System;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1467 - Erebor's i_hypno_delka: a mystic possessing a monster, with everything needed to come home (the body,
	/// the alter ego, the two bank boxes, the own look, stats, skills and spot, the experience gained meanwhile). Its timer
	/// ends the possession. The mystic doesn't die in the body (@denyDeath refuses, and the timer brings it home), comes home
	/// at a logout (@logout), and earns nothing until then (@gainExperience counts the gain's base, paid on the way home).</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class PossessionPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal NPC monster = null;

		[SaveableData, CopyableData]
		internal NPC ego = null;

		[SaveableData, CopyableData]
		internal Container layersBox = null;

		[SaveableData, CopyableData]
		internal Container packBox = null;

		[SaveableData, CopyableData]
		internal string name = null;

		[SaveableData, CopyableData]
		internal string title = null;

		[SaveableData, CopyableData]
		internal string skills = null;

		[SaveableData, CopyableData]
		internal int model = 0;

		[SaveableData, CopyableData]
		internal int color = 0;

		[SaveableData, CopyableData]
		internal int str = 0;

		[SaveableData, CopyableData]
		internal int dex = 0;

		[SaveableData, CopyableData]
		internal int intel = 0;

		[SaveableData, CopyableData]
		internal int vit = 0;

		[SaveableData, CopyableData]
		internal int hits = 0;

		[SaveableData, CopyableData]
		internal int stam = 0;

		[SaveableData, CopyableData]
		internal int mana = 0;

		[SaveableData, CopyableData]
		internal int x = 0;

		[SaveableData, CopyableData]
		internal int y = 0;

		[SaveableData, CopyableData]
		internal int z = 0;

		[SaveableData, CopyableData]
		internal int m = 0;

		[SaveableData, CopyableData]
		internal int gained = 0;

		[SaveableData, CopyableData]
		internal bool monsterInvulnerable = false;

		/// <summary>#1558 - the death in the body was refused, and the timer, due at once, comes home that way.</summary>
		[SaveableData, CopyableData]
		internal bool diedInTheBody = false;

		[SaveableData, CopyableData]
		internal Character diedBy = null;

		internal static readonly PluginKey Key = PluginKey.Acquire("_hypnoPossession_");

		internal static PossessionPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as PossessionPlugin;
		}

		/// <summary>i_hypno_delka's @timer, or (#1558) a refused death in the body coming home.</summary>
		public void On_Timer() {
			if (this.Cont is Player self) {
				if (this.diedInTheBody) {
					MysticHypnosis.DiedInTheBody(self, this, this.diedBy);
				} else {
					self.SysMessage("Spojeni s monstrem te jiz prilis vycerpalo");
					MysticHypnosis.SwapBack(self, this, 0, false);
				}
			}
		}

		/// <summary>A possessing mystic doesn't die (Erebor's e_hypnotising_mystic @death RETURN 1). #1558 - the deny only refuses
		/// and notes who killed: the timer, due at once, pays the killers as for the monster and brings the mystic home.</summary>
		public TriggerResult On_DenyDeath(DenyDeathArgs args) {
			if (!this.diedInTheBody) {
				this.diedInTheBody = true;
				this.diedBy = args.killedBy;
				this.Timer = 0;
			}
			args.Result = DenyResultMessages.Deny_NoMessage;
			return TriggerResult.Cancel;
		}

		/// <remarks>The swap back removes this plugin and the mystic's paralysis, poison and bleeding inside @logout's loop;
		/// the older plugins still hear @logout (#1468).</remarks>
		public void On_LogOut() {
			if (this.Cont is Player self) {
				if (this.diedInTheBody) {
					MysticHypnosis.DiedInTheBody(self, this, this.diedBy); //#1558 - died in the body just before: paid and penalised as on the timer
				} else {
					MysticHypnosis.SwapBack(self, this, 0, false);
				}
			}
		}

		/// <summary>addexp while possessing only counts, the gain before any bonus; it's paid (with its bonuses) at home.</summary>
		public TriggerResult On_GainExperience(GainExperienceArgs args) {
			this.gained += args.BaseAmount;
			return TriggerResult.Cancel;
		}
	}

	[PluginDefTemplate]
	public partial class PossessionPluginDef : TimerPluginDef {
		public static readonly PossessionPluginDef instance =
			(PossessionPluginDef) new PossessionPluginDef("p_hypnoPossession", "C# scripts", -1).Register();
	}

	/// <summary>#1467 - the alter ego left where the mystic stood (e_hypnotising_mystic_ego, Erebor's tag on the ego), with
	/// its ring of alarm pads looked at every second. A blow or a spell on it, or its death, brings the mystic home.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class AlterEgoPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal Player mystic = null;

		/// <summary>#1558 - the ego's death was refused, and the timer, due at once, brings the mystic home that way.</summary>
		[SaveableData, CopyableData]
		internal bool died = false;

		internal static readonly PluginKey Key = PluginKey.Acquire("_hypnoEgo_");

		internal static AlterEgoPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as AlterEgoPlugin;
		}

		/// <summary>The alarm pads' ring, or (#1558) the ego's refused death bringing the mystic home hurt (the swap back
		/// deletes the ego).</summary>
		public void On_Timer() {
			if (this.died) {
				if (this.mystic != null) {
					MysticHypnosis.SwapBack(this.mystic, 0, true);
				}
				return;
			}
			this.Timer = 1;
			if (this.Cont is NPC ego) {
				MysticHypnosis.Alarm(ego);
			}
		}

		/// <summary>The ego doesn't die (Erebor's e_hypnotising_mystic_ego @death RETURN 1). #1558 - the deny only refuses: the
		/// timer, due at once, brings the mystic home hurt.</summary>
		public TriggerResult On_DenyDeath(DenyDeathArgs args) {
			if (!this.died) {
				this.died = true;
				this.Timer = 0;
			}
			args.Result = DenyResultMessages.Deny_NoMessage;
			return TriggerResult.Cancel;
		}

		/// <summary>A blow at the ego brings the mystic home with half the hits.</summary>
		public void On_GetHit(WeaponSwingArgs swing) {
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

	[PluginDefTemplate]
	public partial class AlterEgoPluginDef : TimerPluginDef {
		public static readonly AlterEgoPluginDef instance =
			(AlterEgoPluginDef) new AlterEgoPluginDef("p_hypnoEgo", "C# scripts", -1).Register();
	}
}
