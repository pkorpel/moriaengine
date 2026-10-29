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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #799 e_npc_massleap (13 monsters). Archive: when a player hits it, 1 in 14 to mark every
	/// (non-GM) player around; it then teleports onto each of them in turn - the first 3 s
	/// later, then every {3,4} s - and attacks them, skipping anyone it can no longer see.
	/// User decision: faithful teleport hops (unlike e_npc_leaper, which only got a speed boost).
	/// The hop queue is transient and runs off the AI tick (NPCAiTimer.OnTimeout).</summary>
	[ViewableClass]
	public class MassLeapAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue range;
		private FieldValue firstDelay;
		private FieldValue hopDelay;

		public MassLeapAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("massLeapChance", new double[] { 71, 71 }, typeof(double[]));
			this.range = this.InitTypedField("massLeapRange", 12, typeof(int));
			this.firstDelay = this.InitTypedField("massLeapFirstDelay", (double) 3, typeof(double));
			this.hopDelay = this.InitTypedField("massLeapHopDelay", new double[] { 3, 4 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance per landed player hit (archive 1 in 14).</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Players within this many tiles get marked.</summary>
		public int Range {
			get { return (int) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}

		/// <summary>Seconds to the first hop.</summary>
		public double FirstDelay {
			get { return (double) this.firstDelay.CurrentValue; }
			set { this.firstDelay.CurrentValue = value; }
		}

		/// <summary>Seconds between hops.</summary>
		public double[] HopDelay {
			get { return (double[]) this.hopDelay.CurrentValue; }
			set { this.hopDelay.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int TeleportGlowEffect = 0x3728;

		private static MassLeapAbilityDef massLeapAbilityDef;
		internal static MassLeapAbilityDef MassLeapDef {
			get {
				if (massLeapAbilityDef == null) {
					massLeapAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_massleap") as MassLeapAbilityDef;
				}
				return massLeapAbilityDef;
			}
		}

		private class LeapHop {
			internal Character target;
			internal double dueAt;
		}

		private static readonly ConditionalWeakTable<NPC, Queue<LeapHop>> massLeapHops = new ConditionalWeakTable<NPC, Queue<LeapHop>>();

		/// <summary>#799 e_npc_massleap - defender side, after a landed player hit. Returns how many players were marked.</summary>
		public static int TryApplyMassLeap(Character attacker, Character defender) {
			NPC self = defender as NPC;
			MassLeapAbilityDef def = MassLeapDef;
			if ((self == null) || !attacker.IsPlayerForCombat || !HasAbilityDef(self, def) || !self.IsAliveAndValid) {
				return 0;
			}
			Queue<LeapHop> pending;
			if (massLeapHops.TryGetValue(self, out pending) && (pending.Count > 0)) {
				return 0; //already leaping
			}
			if (!RollChance(RollTunable(null, def.Chance))) {
				return 0;
			}
			return ForceMassLeap(self);
		}

		/// <summary>Marks the players around without the chance roll - for tests.</summary>
		public static int ForceMassLeap(NPC self) {
			MassLeapAbilityDef def = MassLeapDef;
			Queue<LeapHop> hops = new Queue<LeapHop>();
			double due = Globals.TimeInSeconds + ((def == null) ? 3 : def.FirstDelay);
			foreach (Character ch in CharsAround(self.GetMap(), self.X, self.Y, (def == null) ? 12 : def.Range)) {
				if (ch.IsPlayerForCombat && !ch.IsGM && ch.IsAliveAndValid) {
					hops.Enqueue(new LeapHop { target = ch, dueAt = due });
					due += (def == null) ? 3 : ScriptUtil.GetRandInRange(def.HopDelay[0], def.HopDelay[1]);
				}
			}
			massLeapHops.AddOrUpdate(self, hops);
			return hops.Count;
		}

		/// <summary>Every AI tick: makes every hop that is due. Returns how many it made.</summary>
		public static int TryMassLeapTick(NPC self) {
			Queue<LeapHop> hops;
			if (!massLeapHops.TryGetValue(self, out hops) || (hops.Count == 0)) {
				return 0;
			}
			int made = 0;
			while ((hops.Count > 0) && (hops.Peek().dueAt <= Globals.TimeInSeconds)) {
				Character target = hops.Dequeue().target;
				if (target.IsDeleted || !target.IsAliveAndValid || target.IsNotVisible || (target.M != self.M) || !self.CanSeeLOS(target).Allow) {
					continue; //archive: out of sight, invisible or hidden - skip them
				}
				EffectFactory.StationaryEffect(self, TeleportGlowEffect, 10, 10);
				self.Go(target.X, target.Y, target.Z, target.M);
				EffectFactory.StationaryEffect(self, TeleportGlowEffect, 10, 10);
				self.Emote("teleports!");
				SwitchTargetTo(self, target);
				made++;
			}
			return made;
		}
	}
}
