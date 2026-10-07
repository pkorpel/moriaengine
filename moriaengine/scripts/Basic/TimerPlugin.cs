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

namespace SteamEngine.CompiledScripts {

	public class TimerPluginDef : PluginDef {

		public TimerPluginDef(string defname, string filename, int headerLine)
			:
				base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new TimerPlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(TimerPluginDef), typeof(TimerPlugin));
		}
	}

	[DeepCopyableClass]
	[SaveableClass]
	public class TimerPlugin : Plugin {
		[DeepCopyImplementation]
		public TimerPlugin(TimerPlugin copyFrom)
			:
				base(copyFrom) {
		}

		[LoadingInitializer]
		public TimerPlugin() {
		}

		private new TimerPlugin Def {
			get {
				return this.Def;
			}
		}

		static TimerKey timerKey = TimerKey.Acquire("_pluginTimer_");

		public double Timer {
			get {
				BoundTimer timer = this.GetTimer(timerKey);
				if (timer != null) {
					return timer.DueInSeconds;
				}
				return -1;
			}
			set {
				BoundTimer timer = this.GetTimer(timerKey);
				if (timer == null) {
					timer = new PluginTimer();
					this.AddTimer(timerKey, timer);
				}
				timer.DueInSeconds = value;
			}
		}

		public BoundTimer TimerObject {
			get {
				return this.GetTimer(timerKey);
			}
		}

		[SaveableClass]
		[DeepCopyableClass]
		public class PluginTimer : BoundTimer {
			[DeepCopyImplementation]
			[LoadingInitializer]
			public PluginTimer() {
			}

			static TriggerKey timer = TriggerKey.Acquire("timer");

			protected override void OnTimeout(TagHolder cont) {
				object scriptedRetVal, compiledRetVal;
				((TimerPlugin) cont).TryRun(timer, null, out scriptedRetVal, out compiledRetVal);
			}
		}
	}
}
