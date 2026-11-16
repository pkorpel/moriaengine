using System;
using System.Collections.Generic;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1455 - the mystic's profession plugin (mystic.def's professionPluginDef), on #1429's pattern: the catch
	/// field's cooldown (Erebor's tag.catchField_nextds) and the hypnosis' retry delay (#1461).</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class MysticProfessionPlugin : ProfessionPlugin {
		[SaveableData, CopyableData]
		internal double nextCatchField = 0;

		[SaveableData, CopyableData]
		internal double nextHypnosis = 0;

		/// <summary>The mystic's plugin, null for anyone else.</summary>
		internal static MysticProfessionPlugin Of(Character ch) {
			return ch.GetPlugin(professionKey) as MysticProfessionPlugin;
		}
	}

	/// <summary>#1461 - Erebor's i_hypno_timer with e_hypnotizing: the concentration before the swap. A step (@step, here
	/// @stepTaken) or a blow (@aftergetswing, here @getSwingLanding) breaks it.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class ConcentrationPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal NPC monster = null;

		internal static readonly PluginKey Key = PluginKey.Acquire("_hypnoConcentration_");

		internal static ConcentrationPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as ConcentrationPlugin;
		}

		/// <summary>i_hypno_timer's @timer: the swap is tried.</summary>
		public void On_Timer() {
			Player self = this.Cont as Player;
			NPC target = this.monster;
			this.Delete();
			if (self != null) {
				MysticHypnosis.Concentrated(self, target);
			}
		}

		public void On_StepTaken(bool running) {
			if (this.Cont is Character self) {
				self.RedMessage("Prerusil jsi sve soustredeni, hypnoza se nezdarila");
			}
			this.Delete();
		}

		public void On_GetSwingLanding(WeaponSwingArgs swing) {
			if (this.Cont is Character self) {
				self.RedMessage("Utok prerusil tve soustredeni, hypnoza se nezdarila");
			}
			this.Delete();
		}
	}

	[PluginDefTemplate]
	public partial class ConcentrationPluginDef : TimerPluginDef {
		public static readonly ConcentrationPluginDef instance =
			(ConcentrationPluginDef) new ConcentrationPluginDef("p_hypnoConcentration", "C# scripts", -1).Register();
	}

	/// <summary>#1455 - Erebor's i_catchitem: the mystic holds one monster with the mind, a tick a second. A death lets go
	/// at the next tick, as its @timer saw statf_dead.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class CatchPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal Character monster = null;

		[SaveableData, CopyableData]
		internal int ticks = 0;

		[SaveableData, CopyableData]
		internal int monsterExp = 0;

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

	[PluginDefTemplate]
	public partial class CatchPluginDef : TimerPluginDef {
		public static readonly CatchPluginDef instance =
			(CatchPluginDef) new CatchPluginDef("p_catchItem", "C# scripts", -1).Register();
	}

	/// <summary>#1455 - Erebor's i_memory_catchField with e_catchField: the monsters around the mystic held, a tick a
	/// second; its own @death ends it, as e_catchField's.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class CatchFieldPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal int effort = 0;

		[SaveableData, CopyableData]
		internal List<NPC> held = null;

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

	[PluginDefTemplate]
	public partial class CatchFieldPluginDef : TimerPluginDef {
		public static readonly CatchFieldPluginDef instance =
			(CatchFieldPluginDef) new CatchFieldPluginDef("p_catchField", "C# scripts", -1).Register();
	}

	/// <summary>#1455 - Erebor's e_catchedInField with tag.catchedBy and tag.catchPower: a monster held by a catch field.
	/// A blow at it frees it (@aftergetswing, here @getSwingLanding).</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class CaughtPlugin : Plugin {
		[SaveableData, CopyableData]
		internal Character mystic = null;

		[SaveableData, CopyableData]
		internal int power = 0;

		internal static readonly PluginKey Key = PluginKey.Acquire("_catchedInField_");

		internal static CaughtPlugin Of(Character ch) {
			return ch.GetPlugin(Key) as CaughtPlugin;
		}

		/// <remarks>Inside its holder's @getSwingLanding this also removes the NPC's ImmobilePlugin and every paralysis
		/// (Erebor's paralyze_remove); the trigger still reaches the NPC's other plugins (#1468).</remarks>
		public void On_GetSwingLanding(WeaponSwingArgs swing) {
			if (this.Cont is NPC npc) {
				MysticMind.ReleaseFromField(npc, CatchFieldPlugin.Of(this.mystic));
			}
		}
	}

	[PluginDefTemplate]
	public partial class CaughtPluginDef : PluginDef {
		public static readonly CaughtPluginDef instance =
			(CaughtPluginDef) new CaughtPluginDef("p_catchedInField", "C# scripts", -1).Register();
	}

	/// <summary>#1455 - Erebor's i_controlnpc: in 5 s the mystic may take the creature over.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class ControlNpcPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal Character target = null;

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

	[PluginDefTemplate]
	public partial class ControlNpcPluginDef : TimerPluginDef {
		public static readonly ControlNpcPluginDef instance =
			(ControlNpcPluginDef) new ControlNpcPluginDef("p_controlNpc", "C# scripts", -1).Register();
	}

	/// <summary>#1457 - Erebor's i_buffSteal on the mystic: state 0 the 3 s wind-up, 1 the 60 s stolen talent (or the
	/// crippling of a fatal failure), 2 the rest. The stolen attack talent (tag.buffDmgBonus) is its bonus: blows and spell
	/// power against monsters, at @swingLanding and @spellPower. Its own @death ends it, as e_mystik's @death consumed
	/// i_buffSteal; its @unAssign (Erebor's @unEquip) gives a held talent back whenever it goes.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class BuffStealPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal Character target = null;

		[SaveableData, CopyableData]
		internal int type = 0;

		[SaveableData, CopyableData]
		internal int power = 0;

		[SaveableData, CopyableData]
		internal int changed = 0;

		[SaveableData, CopyableData]
		internal int state = 0;

		[SaveableData, CopyableData]
		internal bool success = false;

		[SaveableData, CopyableData]
		internal int bonus = 0;

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

	[PluginDefTemplate]
	public partial class BuffStealPluginDef : TimerPluginDef {
		public static readonly BuffStealPluginDef instance =
			(BuffStealPluginDef) new BuffStealPluginDef("p_buffSteal", "C# scripts", -1).Register();
	}

	/// <summary>#1457 - Erebor's i_buffStealNPC with e_npcChangeDmg: 60 s of a robbed monster, then its talent comes
	/// back. Its blows lose the cut (tag.changeDmgPercent) at @swingLanding, e_npcChangeDmg's @AfterSwing.</summary>
	[Dialogs.ViewableClass]
	[PluginTemplate]
	public partial class BuffStolenPlugin : TimerPlugin {
		[SaveableData, CopyableData]
		internal int type = 0;

		[SaveableData, CopyableData]
		internal int changed = 0;

		[SaveableData, CopyableData]
		internal int cut = 0;

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

	[PluginDefTemplate]
	public partial class BuffStolenPluginDef : TimerPluginDef {
		public static readonly BuffStolenPluginDef instance =
			(BuffStolenPluginDef) new BuffStolenPluginDef("p_buffStealNpc", "C# scripts", -1).Register();
	}

	[PluginDefTemplate]
	public partial class MysticProfessionPluginDef : ProfessionPluginDef {
	}
}
