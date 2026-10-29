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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #799 e_duplicator (13 monsters). Archive: each time it is hit it splits ("The creature
	/// splits in two!") - a fresh copy of the same creature appears next to it and attacks the
	/// same target - until it has made tag.dupecount copies. A copy never splits itself. The
	/// archive's unset dupecount is 0 (never splits), kept as the default: 7 of the 13 real
	/// monsters set it.</summary>
	[ViewableClass]
	public class DuplicatorAbilityDef : PassiveAbilityDef {
		private FieldValue dupeCount;

		public DuplicatorAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.dupeCount = this.InitTypedField("duplicatorDupeCount", 0, typeof(int));
		}

		/// <summary>Copies one monster may make when the species sets no DupeCount.</summary>
		public int DupeCount {
			get { return (int) this.dupeCount.CurrentValue; }
			set { this.dupeCount.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static DuplicatorAbilityDef duplicatorAbilityDef;
		internal static DuplicatorAbilityDef DuplicatorDef {
			get {
				if (duplicatorAbilityDef == null) {
					duplicatorAbilityDef = AbstractScript.GetByDefname("a_tevent_e_duplicator") as DuplicatorAbilityDef;
				}
				return duplicatorAbilityDef;
			}
		}

		//copies made so far by an original; -1 marks a copy (never splits)
		private static readonly TagKey dupedCountTK = TagKey.Acquire("_npcDupedCount_");

		/// <summary>#799 e_duplicator - defender side, after a landed swing. Returns the new copy, if any.</summary>
		public static NPC TryApplyDuplicator(Character attacker, Character defender) {
			NPC self = defender as NPC;
			DuplicatorAbilityDef def = DuplicatorDef;
			if ((self == null) || !HasAbilityDef(self, def) || !self.IsAliveAndValid) {
				return null;
			}
			object tag = self.GetTag(dupedCountTK);
			int duped = (tag is int) ? (int) tag : 0;
			NPCDef npcDef = (NPCDef) self.Def;
			int allowed = (npcDef.DupeCount != null) ? NPCDef.RollStatRange(npcDef.DupeCount) : def.DupeCount;
			if ((duped < 0) || (duped >= allowed)) {
				return null;
			}
			self.SetTag(dupedCountTK, duped + 1);

			NPC copy = (NPC) npcDef.Create(self.X + Globals.dice.Next(-1, 2), self.Y + Globals.dice.Next(-1, 2), self.Z, self.M);
			copy.SetTag(dupedCountTK, -1);
			NPCAiTimer ai = NPCAiTimer.GetFor(self);
			SwitchTargetTo(copy, ((ai != null) && (ai.Target != null)) ? ai.Target : attacker);
			attacker.SysMessage("The creature splits in two!");
			return copy;
		}
	}
}
