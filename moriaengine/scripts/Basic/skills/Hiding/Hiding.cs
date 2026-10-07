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
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class HidingSkillDef : SkillDef {

		public HidingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static PluginKey pluginKey = PluginKey.Acquire("hiddenHelper");

		public static PluginDef p_hiddenHelper;
		public static PluginDef P_HiddenHelper {
			get {
				if (p_hiddenHelper == null) {
					p_hiddenHelper = PluginDef.GetByDefname("p_hiddenHelper");
				}
				return p_hiddenHelper;
			}
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: various state checks...
			return TriggerResult.Continue; //continue to @start
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue; //continue to delay, then @stroke
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			//todo: various state checks...
			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));

			return TriggerResult.Continue; //continue to @success or @fail
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Hide(skillSeqArgs.Self);
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(501241);//You can't seem to hide here.
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			GameState state = skillSeqArgs.Self.GameState;
			if (state != null) {
				state.WriteLine(Loc<HidingLoc>.Get(state.Language).HidingAborted);
			}
		}

		[SteamFunction]
		public static void Hide(Character self) {
			self.ClilocSysMessage(501240);//You have hidden yourself well.
			self.Flag_Hidden = true;
			self.AddPlugin(pluginKey, P_HiddenHelper.Create());
		}

		[SteamFunction]
		public static void UnHide(Character self) {
			if (self.Flag_Hidden) {
				self.ClilocSysMessage(501242); //You are no longer hidden.
				self.Flag_Hidden = false;
			}
			self.DeletePlugin(pluginKey);
		}
	}

	[ViewableClass]
	public partial class HiddenHelperPlugin {
	}

	[ViewableClass]
	public partial class HiddenHelperPluginDef {
	}

	public class HidingLoc : CompiledLocStringCollection<HidingLoc> {
		internal readonly string HidingAborted = "Hiding aborted.";
	}
}