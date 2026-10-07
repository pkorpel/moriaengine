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

namespace SteamEngine.Timers {

	public abstract class BoundTimer : Timer {
		internal WeakReference contRef = new WeakReference(null);

		protected sealed override void OnTimeout() {
			TagHolder cont = this.Cont;
			if (cont != null) {
				if ((this.PeriodInSeconds < 0) && (this.DueInSeconds < 0)) {
					this.DeleteFrom(cont);
				}
				this.OnTimeout(cont);
			} else {
				this.DeleteFrom(cont);
			}
		}

		protected abstract void OnTimeout(TagHolder cont);

		public TagHolder Cont {
			get {
				TagHolder c = this.contRef.Target as TagHolder;
				if (c == null || c.IsDeleted) {
					return null;
				}
				return c;
			}
		}

		public sealed override void Delete() {
			TagHolder c = this.Cont;
			this.DeleteFrom(c);
		}

		private void DeleteFrom(TagHolder c) {
			if (c != null) {
				c.RemoveTimer(this);
			}
			base.Delete();
		}
	}
}
