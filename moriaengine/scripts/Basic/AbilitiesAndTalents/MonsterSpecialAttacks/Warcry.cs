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
	/// #799 e_npc_warcry (11 monsters). Archive: at most once per tag.NPC_Warcry_delay
	/// (deciseconds, default 600 = 60 s) while fighting it cries out (sound 1098): everyone
	/// around but itself is "in shock" and their spellcasting is interrupted (stopmagery). The
	/// archive's range is a runtime setting with no value in the scripts; 8 tiles, like the
	/// other area abilities. Checked once per NPCAiTimer.Fight think-tick.</summary>
	[ViewableClass]
	public class WarcryAbilityDef : PassiveAbilityDef {
		private FieldValue cooldown;
		private FieldValue range;

		public WarcryAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.cooldown = this.InitTypedField("warcryCooldown", new double[] { 600, 600 }, typeof(double[]));
			this.range = this.InitTypedField("warcryRange", 8, typeof(int));
		}

		/// <summary>Deciseconds between cries (archive convention, same as Stun's cooldown).</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>Everyone within this many tiles is shocked.</summary>
		public int Range {
			get { return (int) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int WarcrySound = 1098;

		private static WarcryAbilityDef warcryAbilityDef;
		internal static WarcryAbilityDef WarcryDef {
			get {
				if (warcryAbilityDef == null) {
					warcryAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_warcry") as WarcryAbilityDef;
				}
				return warcryAbilityDef;
			}
		}

		private static readonly TagKey nextWarcryTimeTK = TagKey.Acquire("_npcNextWarcryTime_");

		/// <summary>#799 e_npc_warcry - once per AI tick while fighting. Returns how many were shocked
		/// (-1 when it didn't cry this time).</summary>
		public static int TryApplyWarcry(NPC self) {
			WarcryAbilityDef def = WarcryDef;
			if (!HasAbilityDef(self, def)) {
				return -1;
			}
			object due = self.GetTag(nextWarcryTimeTK);
			if ((due is double) && (Globals.TimeInSeconds < (double) due)) {
				return -1;
			}
			NPCDef npcDef = (NPCDef) self.Def;
			self.SetTag(nextWarcryTimeTK, Globals.TimeInSeconds + RollTunable(npcDef.WarcryCooldown, def.Cooldown) / 10);
			self.Emote("lets out a terrible war cry!");
			self.Sound(WarcrySound);
			int shocked = 0;
			foreach (Character ch in CharsAround(self.GetMap(), self.X, self.Y, def.Range)) {
				if ((ch == self) || ch.IsDeleted || !ch.IsAliveAndValid) {
					continue;
				}
				ch.Trigger_Disrupt();
				if (ch.IsPlayerForCombat) {
					ch.SysMessage("You are in shock!", 0x21);
				}
				shocked++;
			}
			return shocked;
		}
	}
}
