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

using System;
using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class DetectHiddenSkillDef : SkillDef {

		public DetectHiddenSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			Character self = skillSeqArgs.Self;
			double range = Math.Round(this.GetEffectForChar(self));

			foreach (Character target in self.GetMap().GetCharsInRange(self.X, self.Y, (int) range)) {
				if (target.Flag_Hidden) {
					if (this.CheckSuccess(self, target.GetSkill(SkillName.Hiding))) {
						skillSeqArgs.Success = true;
						skillSeqArgs.Target1 = target;
					}
				}
			}

			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Character target = (Character) skillSeqArgs.Target1;
			HiddenHelperPlugin ssp = target.GetPlugin(HidingSkillDef.pluginKey) as HiddenHelperPlugin;
			if (ssp != null) {
				if (ssp.hadDetectedMe == null) {
					ssp.hadDetectedMe = new LinkedList<Character>();
				} else if (ssp.hadDetectedMe.Contains(self)) {
					return;
				}
				CharSyncQueue.AboutToChangeVisibility(target);
				ssp.hadDetectedMe.AddFirst(self);
			}
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(500817);//You can see nothing hidden there.
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SysMessage("Detecting Hidden aborted.");
		}
	}
}