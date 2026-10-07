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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class SpellEffectItem {
		public static bool CheckPositionForItem(int x, int y, ref int z, Map map, int height, bool checkCharacters) {
			for (int offset = 0; offset < 10; offset++) {
				int offsetZ = z - offset;
				if (map.CanFit(x, y, offsetZ, height, true, checkCharacters)) {
					z = offsetZ;
					return true;
				}
			}
			return false;
		}


		static TimerKey timerKey = TimerKey.Acquire("_spellEffectTimer_");

		public int SpellPower {
			get {
				return this.spellPower;
			}
		}

		public bool Dispellable {
			get {
				return this.dispellable;
			}
		}

		public void Init(int spellPower, TimeSpan duration, bool dispellable) {
			this.spellPower = spellPower;
			this.dispellable = dispellable;

			this.DeleteTimer(timerKey);

			BoundTimer timer = new SpellEffectTimer();
			this.AddTimer(timerKey, timer);
			timer.DueInSpan = duration;
		}

		[SaveableClass]
		[DeepCopyableClass]
		public class SpellEffectTimer : BoundTimer {
			[DeepCopyImplementation]
			[LoadingInitializer]
			public SpellEffectTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				cont.Delete();
			}
		}
	}

	[ViewableClass]
	public partial class SpellEffectItemDef {

		public SpellEffectItem Conjure(IPoint4D target, bool checkCharacters, int spellPower, TimeSpan duration, bool dispellable) {
			int x = target.X;
			int y = target.Y;
			int z = target.Z;
			Map map = target.GetMap();
			if (SpellEffectItem.CheckPositionForItem(x, y, ref z, map, this.Height, checkCharacters)) {
				SpellEffectItem item = (SpellEffectItem) this.Create(x, y, z, map.M);
				item.Init(spellPower, duration, dispellable);
				return item;
			}
			return null;
		}
	}
}