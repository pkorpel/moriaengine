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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	partial class WeaponSkillTargetTrackerPlugin {
		HashSet<Character> attackers = new HashSet<Character>();

		private static PluginKey weaponSkillTargetPK = PluginKey.Acquire("_weaponSkillTarget_");

		public static void InstallTargetTracker(Character defender, Character attacker) {
			WeaponSkillTargetTrackerPlugin p = defender.GetPlugin(weaponSkillTargetPK) as WeaponSkillTargetTrackerPlugin;
			if (p == null) {
				p = (WeaponSkillTargetTrackerPlugin) defender.AddNewPlugin(weaponSkillTargetPK, WeaponSkillTargetTrackerPluginDef.instance);
			}
			p.attackers.Add(attacker);
		}

		public static void UnInstallTargetTracker(Character self, Character attacker) {
			WeaponSkillTargetTrackerPlugin p = self.GetPlugin(weaponSkillTargetPK) as WeaponSkillTargetTrackerPlugin;
			if (p != null) {
				p.attackers.Remove(attacker);
				if (p.attackers.Count == 0) {
					p.Delete();
				}
			}
		}

		private void CheckAttackers() {
			List<Character> toBeDeleted = null;
			foreach (Character attacker in this.attackers) {
				SkillSequenceArgs skillSeq = SkillSequenceArgs.GetSkillSequenceArgs(attacker);
				if ((skillSeq != null) && (skillSeq.SkillDef is WeaponSkillDef) && (skillSeq.Target1 != this.Cont)) {//attacker still attacking :)
					skillSeq.PhaseStroke();
				} else {//the attacker is not attacking us, let's stop tracking.
					if (toBeDeleted == null) {
						toBeDeleted = new List<Character>();
					}
					toBeDeleted.Add(attacker);
				}
			}
			if (toBeDeleted != null) {
				foreach (Character delete in toBeDeleted) {
					this.attackers.Remove(delete);
				}
			}
			if (this.attackers.Count == 0) {
				this.Delete();
			}
		}

		public void On_Step() {
			this.CheckAttackers();
		}

		public void On_VisibilityChange() {
			this.CheckAttackers();
		}

		public void On_NewPosition() {
			this.CheckAttackers();
		}

		public void On_Death() {
			this.Delete();
		}
	}

	[ViewableClass]
	partial class WeaponSkillTargetTrackerPluginDef {
		public static WeaponSkillTargetTrackerPluginDef instance = (WeaponSkillTargetTrackerPluginDef) 
			new WeaponSkillTargetTrackerPluginDef("p_weaponSkillTargetTracker", "C# scripts", -1).Register();
	}
}
