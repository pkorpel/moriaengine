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
}
