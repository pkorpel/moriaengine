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

using System.Diagnostics.CodeAnalysis;
using SteamEngine.Persistence;

namespace SteamEngine.Timers {

	[SaveableClass, DeepCopyableClass]
	public sealed class TriggerTimer : BoundTimer {

		[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
		[SaveableData, CopyableData]
		public TriggerKey trigger;

		[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
		[SaveableData, CopyableData]
		public string formatString;

		[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
		[SaveableData, CopyableData]
		public object[] args;

		[LoadingInitializer, DeepCopyImplementation]
		public TriggerTimer() {
		}

		public TriggerTimer(TriggerKey trigger, string formatString, object[] args) {
			this.trigger = trigger;
			this.formatString = formatString;
			this.args = args;
		}

		protected override void OnTimeout(TagHolder cont) {
			ScriptArgs sa = new ScriptArgs(this.args);
			sa.FormatString = this.formatString;
			Globals.SetSrc(null);
			((PluginHolder) cont).TryTrigger(this.trigger, sa);
		}
	}
}
