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

namespace SteamEngine.CompiledScripts.Dialogs {

	public class MassSettingsView : IDataView {

		public Type HandledType {
			get {
				return typeof(IMassSettings);
			}
		}

		public bool HandleSubclasses {
			get {
				return true;
			}
		}

		public string GetName(object instance) {
			return ((IMassSettings) instance).Name;
		}

		public int GetActionButtonsCount(object instance) {
			return 0;
		}

		public int GetFieldsCount(object instance) {
			return ((IMassSettings) instance).Count;
		}

		public IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target) {
			IMassSettings holder = (IMassSettings) target;
			for (int i = firstLineIndex, n = holder.Count; i < n; i++) {
				yield return holder.GetFieldView(i);
			}
		}

		public IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target) {
			yield break;
		}

	}
}