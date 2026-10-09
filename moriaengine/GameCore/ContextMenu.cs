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

namespace SteamEngine {
	/// <summary>
	/// Collects the entries for a right-click context (popup) menu on a Thing, mirroring the
	/// AosToolTips collector pattern (Thing.GetAosToolTips/On_BuildAosToolTips). Entries carry
	/// their own callback rather than relying on index lookup against a separately-stored list,
	/// since the menu is rebuilt (not cached) on both the request and the selection round-trip -
	/// simpler, and avoids any risk of acting on a stale reference if state changed in between.
	/// </summary>
	[CLSCompliant(false)]
	public sealed class ContextMenu {
		private readonly List<(ushort index, int cliloc, Action<AbstractCharacter> onSelect)> entries =
			new List<(ushort index, int cliloc, Action<AbstractCharacter> onSelect)>(4);

		/// <param name="cliloc">A client localized-string id. Context menu entries are
		/// conventionally drawn from the 3000000+ cliloc range, but this isn't enforced here -
		/// whatever the caller passes is sent as-is.</param>
		/// <param name="onSelect">Invoked with the requesting character if this entry is picked.</param>
		public void Add(int cliloc, Action<AbstractCharacter> onSelect) {
			ushort index = (ushort) (this.entries.Count + 1); //0 is reserved by the protocol for "no selection"
			this.entries.Add((index, cliloc, onSelect));
		}

		public int Count {
			get { return this.entries.Count; }
		}

		public IEnumerable<(ushort index, int cliloc)> Entries {
			get {
				foreach (var entry in this.entries) {
					yield return (entry.index, entry.cliloc);
				}
			}
		}

		internal void InvokeSelected(ushort index, AbstractCharacter requester) {
			foreach (var entry in this.entries) {
				if (entry.index == index) {
					entry.onSelect?.Invoke(requester);
					return;
				}
			}
		}
	}
}
