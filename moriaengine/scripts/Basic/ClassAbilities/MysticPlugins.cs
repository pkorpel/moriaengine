using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1455 - the mystic's profession plugin (mystic.def's professionPluginDef), on #1429's pattern: the catch
	/// field's cooldown (Erebor's tag.catchField_nextds).</summary>
	[Dialogs.ViewableClass]
	public partial class MysticProfessionPlugin {

		/// <summary>The mystic's plugin, null for anyone else.</summary>
		internal static MysticProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as MysticProfessionPlugin;
		}
	}

	/// <summary>#1455 - Erebor's i_catchitem: the mystic holds one monster with the mind, a tick a second. A death lets go
	/// at the next tick, as its @timer saw statf_dead.</summary>
	[Dialogs.ViewableClass]
	public partial class CatchPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_catchItem_");

		internal static CatchPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as CatchPlugin;
		}

		public void On_Timer() {
			if (this.Cont is Character self) {
				MysticMind.CatchTick(self, this);
			}
		}
	}

	public partial class CatchPluginDef {
		public static readonly CatchPluginDef instance =
			(CatchPluginDef) new CatchPluginDef("p_catchItem", "C# scripts", -1).Register();
	}

	/// <summary>#1455 - Erebor's i_memory_catchField with e_catchField: the monsters around the mystic held, a tick a
	/// second; its own @death ends it, as e_catchField's.</summary>
	[Dialogs.ViewableClass]
	public partial class CatchFieldPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_catchField_");

		internal static CatchFieldPlugin Of(Character ch) {
			return (ch == null) ? null : ch.GetPlugin(Key) as CatchFieldPlugin;
		}

		public void On_Timer() {
			if (this.Cont is Character self) {
				MysticMind.CatchFieldTick(self, this);
			}
		}

		public void On_Death() {
			if (this.Cont is Character self) {
				MysticMind.EndCatchField(self, this);
			}
		}
	}

	public partial class CatchFieldPluginDef {
		public static readonly CatchFieldPluginDef instance =
			(CatchFieldPluginDef) new CatchFieldPluginDef("p_catchField", "C# scripts", -1).Register();
	}

	/// <summary>#1455 - Erebor's e_catchedInField with tag.catchedBy and tag.catchPower: a monster held by a catch field.
	/// A blow at it frees it (@aftergetswing, here @getSwingLanding).</summary>
	[Dialogs.ViewableClass]
	public partial class CaughtPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_catchedInField_");

		internal static CaughtPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as CaughtPlugin;
		}

		/// <remarks>Inside its holder's @getSwingLanding this also removes the NPC's ImmobilePlugin and every paralysis
		/// (Erebor's paralyze_remove). Safe while no other NPC plugin answers @getSwingLanding: a plugin removed right
		/// after this one would end the loop (#1387).</remarks>
		public void On_GetSwingLanding(WeaponSwingArgs swing) {
			if (this.Cont is NPC npc) {
				MysticMind.ReleaseFromField(npc, CatchFieldPlugin.Of(this.mystic));
			}
		}
	}

	public partial class CaughtPluginDef {
		public static readonly CaughtPluginDef instance =
			(CaughtPluginDef) new CaughtPluginDef("p_catchedInField", "C# scripts", -1).Register();
	}

	/// <summary>#1455 - Erebor's i_controlnpc: in 5 s the mystic may take the creature over.</summary>
	[Dialogs.ViewableClass]
	public partial class ControlNpcPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_controlNpc_");

		internal static ControlNpcPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as ControlNpcPlugin;
		}

		public void On_Timer() {
			Character self = this.Cont as Character;
			Character creature = this.target;
			this.Delete();
			if (self != null) {
				MysticMind.FinishControl(self, creature);
			}
		}
	}

	public partial class ControlNpcPluginDef {
		public static readonly ControlNpcPluginDef instance =
			(ControlNpcPluginDef) new ControlNpcPluginDef("p_controlNpc", "C# scripts", -1).Register();
	}

	/// <summary>#1457 - Erebor's i_buffSteal on the mystic: state 0 the 3 s wind-up, 1 the 60 s stolen talent (or the
	/// crippling of a fatal failure), 2 the rest. The stolen attack talent (tag.buffDmgBonus) is its bonus: blows and spell
	/// power against monsters, at @swingLanding and @spellPower. Its own @death ends it, as e_mystik's @death consumed
	/// i_buffSteal; its @unAssign (Erebor's @unEquip) gives a held talent back whenever it goes.</summary>
	[Dialogs.ViewableClass]
	public partial class BuffStealPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_buffSteal_");

		internal static BuffStealPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as BuffStealPlugin;
		}

		/// <summary>i_buffSteal's @timer.</summary>
		public void On_Timer() {
			if (!(this.Cont is Player self)) {
				return;
			}
			if (this.state == 0) {
				this.Timer = 1; //a fallback: StealTick sets the next state's time or deletes it
				MysticMind.StealTick(self, this);
			} else if (this.state == 1) {
				MysticMind.Reverse(self, this);
				this.state = 2;
				this.Timer = 50 + (7 * this.power);
			} else {
				this.Delete();
			}
		}

		/// <summary>e_mystik's @death: consume(1 i_buffSteal), in any state; @unAssign gives a held talent back.</summary>
		public void On_Death() {
			this.Delete();
		}

		/// <summary>i_buffSteal's @unEquip: a talent still held goes back.</summary>
		public void On_UnAssign(Character cont) {
			if (this.state == 1) {
				MysticMind.Reverse(cont, this);
				this.state = 2;
			}
		}

		/// <summary>The stolen attack talent on a blow at a monster (magie.scp:176's buffDmgBonus).</summary>
		public void On_SwingLanding(WeaponSwingArgs swing) {
			if ((this.bonus != 0) && (swing.attacker is Player) && !swing.defender.IsPlayerForCombat) {
				swing.DamageAfterAC += (swing.DamageAfterAC / 100) * this.bonus;
			}
		}

		/// <summary>magie.scp:176: the stolen attack talent on a spell's power against a monster.</summary>
		public void On_SpellPower(SpellPowerArgs args) {
			if ((this.bonus != 0) && (args.TargetChar != null) && !args.TargetChar.IsPlayerForCombat) {
				args.SpellPower += (args.SpellPower / 100) * this.bonus;
			}
		}
	}

	public partial class BuffStealPluginDef {
		public static readonly BuffStealPluginDef instance =
			(BuffStealPluginDef) new BuffStealPluginDef("p_buffSteal", "C# scripts", -1).Register();
	}

	/// <summary>#1457 - Erebor's i_buffStealNPC with e_npcChangeDmg: 60 s of a robbed monster, then its talent comes
	/// back. Its blows lose the cut (tag.changeDmgPercent) at @swingLanding, e_npcChangeDmg's @AfterSwing.</summary>
	[Dialogs.ViewableClass]
	public partial class BuffStolenPlugin {
		internal static readonly PluginKey Key = PluginKey.Acquire("_buffStealNpc_");

		internal static BuffStolenPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as BuffStolenPlugin;
		}

		/// <summary>i_buffStealNPC's @timer: f_buffreturnNPC.</summary>
		public void On_Timer() {
			if (this.Cont is Character npc) {
				switch (this.type) {
					case 1:
						npc.SetRealSkillValue((int) SkillName.EvalInt, npc.GetSkill(SkillName.EvalInt) + this.changed);
						break;
					case 3:
						npc.Dex = (short) (npc.Dex + this.changed);
						break;
				}
			}
			this.Delete();
		}

		public void On_SwingLanding(WeaponSwingArgs swing) {
			if (this.cut != 0) {
				swing.DamageAfterAC -= (swing.DamageAfterAC * this.cut) / 100;
			}
		}
	}

	public partial class BuffStolenPluginDef {
		public static readonly BuffStolenPluginDef instance =
			(BuffStolenPluginDef) new BuffStolenPluginDef("p_buffStealNpc", "C# scripts", -1).Register();
	}
}
