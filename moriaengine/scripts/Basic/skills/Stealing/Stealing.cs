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
	/// <summary>#962 - switched off on Erebor by its skill's own @start action=-1: it asks what to steal, and nothing comes of it.
	/// So taking from a pack you've snooped is always refused, see SnoopingPlugin.</summary>
	[ViewableClass]
	public class StealingSkillDef : SkillDef {

		public StealingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if ((skillSeqArgs.Target1 == null) && (self != null)) {
				self.Target(SingletonScript<Targ_Steal>.Instance);
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}

	public class Targ_Steal : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage(Loc<SnoopingLoc>.Get(self.Language).whatToSteal);
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			return TargetResult.Done;
		}
	}
}
