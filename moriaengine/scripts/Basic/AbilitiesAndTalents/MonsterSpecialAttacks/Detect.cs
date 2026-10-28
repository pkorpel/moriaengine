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

using SteamEngine.Common;
using SteamEngine.Persistence;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_detect. Deliberately
	/// simplified from the real archive's own ground-trap-item mechanic (drops an item that
	/// reveals whoever steps near it) to a direct periodic area reveal while actively fighting -
	/// functionally equivalent for the common case, no new ground-trap item type needed. No
	/// real per-species tuning tag was found in the archive for this one (the pulse count/
	/// interval look hardcoded there too) - no NPCDef override exists for this ability,
	/// unlike the others in this batch, since there's no real per-species data to recover.
	/// </summary>
	[ViewableClass]
	public class DetectAbilityDef : PassiveAbilityDef {
		private FieldValue range;
		private FieldValue interval;

		public DetectAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.range = this.InitTypedField("detectRange", (double) 8, typeof(double));
			this.interval = this.InitTypedField("detectInterval", (double) 5, typeof(double));
		}

		/// <summary>Radius (tiles) around this NPC that gets force-revealed each pulse.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}

		/// <summary>Seconds between reveal pulses.</summary>
		public double Interval {
			get { return (double) this.interval.CurrentValue; }
			set { this.interval.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static DetectAbilityDef detectAbilityDef;
		internal static DetectAbilityDef DetectDef {
			get {
				if (detectAbilityDef == null) {
					detectAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_detect") as DetectAbilityDef;
				}
				return detectAbilityDef;
			}
		}

		private static readonly TagKey nextDetectTimeTK = TagKey.Acquire("_npcNextDetectTime_");

		/// <summary>#detect - periodically force-reveals any hidden/invisible character within
		/// range, while this NPC is actively fighting (called from NPCAiTimer.Fight, which
		/// already only runs then) - see DetectAbilityDef's own doc comment for why this is a
		/// deliberate simplification of the real archive's ground-trap-item mechanic.</summary>
		public static void TryApplyDetect(NPC self) {
			DetectAbilityDef def = DetectDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			if (Globals.TimeInSeconds < NextDetectTime(self)) {
				return;
			}
			SetNextDetectTime(self, Globals.TimeInSeconds + def.Interval);

			int range = (int) def.Range;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if ((target != null) && (target != self) && !target.IsDeleted) {
					target.Flag_Hidden = false;
					target.Flag_InvisByMagic = false;
				}
			}
		}

		private static double NextDetectTime(NPC self) {
			object tag = self.GetTag(nextDetectTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextDetectTime(NPC self, double value) {
			self.SetTag(nextDetectTimeTK, value);
		}
	}
}
