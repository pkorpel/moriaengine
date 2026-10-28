/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using SteamEngine.Persistence;
using SteamEngine.Timers;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_selfreser. Default 60
	/// seconds matches the real archive's own `tag(resstimer)` default. Deliberately does NOT
	/// port the archive's "cancel if the corpse was butchered" check - butchering itself isn't
	/// implemented in this engine yet (NPCDef.ButcherResources is data-only, see #586/#587) -
	/// so the respawn always happens once armed, same simplification precedent as other
	/// deliberately-trimmed archive mechanics this session.</summary>
	[ViewableClass]
	public class SelfReserAbilityDef : PassiveAbilityDef {
		private FieldValue delay;

		public SelfReserAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.delay = this.InitTypedField("selfresDelay", new double[] { 60, 60 }, typeof(double[]));
		}

		/// <summary>{min,max} seconds after death before a fresh instance of this NPC respawns
		/// at the death location - deliberately shadows AbilityDef.Cooldown (different shape)
		/// rather than reusing it.</summary>
		public double[] Delay {
			get { return (double[]) this.delay.CurrentValue; }
			set { this.delay.CurrentValue = value; }
		}
	}

	/// <summary>Companion timer for SelfReserAbilityDef/MonsterSpecialAttacks.TryApplySelfRes -
	/// a plain BoundTimer (no .ct template needed, unlike a Plugin subclass) carrying just
	/// enough state (which NPCDef, and where) to respawn a fresh instance of the monster once
	/// it fires. Attached to the Corpse created by the death that armed it, not to any
	/// Character - see TryApplySelfRes's own doc comment for why.</summary>
	[SaveableClass, DeepCopyableClass]
	public class NpcSelfResTimer : BoundTimer {
		[SaveableData, CopyableData]
		public NPCDef npcDef;
		[SaveableData, CopyableData]
		public int x;
		[SaveableData, CopyableData]
		public int y;
		[SaveableData, CopyableData]
		public int z;
		[SaveableData, CopyableData]
		public byte m;

		[DeepCopyImplementation, LoadingInitializer]
		public NpcSelfResTimer() {
		}

		public NpcSelfResTimer(NPCDef npcDef, int x, int y, int z, byte m) {
			this.npcDef = npcDef;
			this.x = x;
			this.y = y;
			this.z = z;
			this.m = m;
		}

		protected override void OnTimeout(TagHolder cont) {
			this.npcDef.Create(this.x, this.y, this.z, this.m);
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static SelfReserAbilityDef selfReserAbilityDef;
		internal static SelfReserAbilityDef SelfReserDef {
			get {
				if (selfReserAbilityDef == null) {
					selfReserAbilityDef = AbstractScript.GetByDefname("a_tevent_e_selfreser") as SelfReserAbilityDef;
				}
				return selfReserAbilityDef;
			}
		}

		private static readonly TimerKey selfResTimerKey = TimerKey.Acquire("_npcSelfResTimer_");

		/// <summary>#selfreser - a while after death, a fresh instance of this exact monster
		/// respawns at the death location. Must be called AFTER corpse.InitFromChar(this),
		/// while `dying`'s own position is still valid (Character.CauseDeath deletes a
		/// non-player NPC right after this point). Attached to the freshly-created corpse
		/// (one per death, no TagHolder.AddTimer key-collision risk) rather than to the
		/// killer, who could in theory die to two self-resing monsters in quick succession -
		/// see TagHolder.AddTimer's own one-timer-per-key-per-object semantics.</summary>
		public static void TryApplySelfRes(Character dying, Corpse corpse) {
			if (corpse == null) {
				return;
			}
			SelfReserAbilityDef def = SelfReserDef;
			if (def == null) {
				return;
			}
			NPC npc = dying as NPC;
			if (npc == null) {
				return;
			}
			Ability ability;
			if (!npc.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = (NPCDef) npc.Def;
			double delay = RollTunable(npcDef.SelfresDelay, def.Delay);
			NpcSelfResTimer timer = new NpcSelfResTimer(npcDef, dying.X, dying.Y, dying.Z, dying.M);
			corpse.AddTimer(selfResTimerKey, timer).DueInSeconds = delay;
		}
	}
}
