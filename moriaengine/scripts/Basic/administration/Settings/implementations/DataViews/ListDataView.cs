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
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.Dialogs {
	public class ListDataView : ButtonDataFieldView, IDataView {
		public Type HandledType {
			get {
				return typeof(IList);
			}
		}

		public bool HandleSubclasses {
			get {
				return true;
			}
		}

		string IDataView.GetName(object instance) {
			return instance.GetType().Name;
		}

		public int GetActionButtonsCount(object instance) {
			return 1;
		}

		public int GetFieldsCount(object instance) {
			return ((IList) instance).Count + 1;
		}

		public IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target) {
			if (firstLineIndex == 0) {
				yield return CountField.instance;
			} else {
				firstLineIndex--;
			}
			//iterovat budeme nejpozdeji do doby nez dojdou polozky seznamu
			//(iterovani mozno byt prerusovanu kvuli pagingu i jinde...)
			for (int i = firstLineIndex, n = ((IList) target).Count; i < n; i++) {
				yield return new IndexField(i);
			}
		}

		public IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target) {
			yield return this;//nechtelo sem idelat dalsi tridu :)
		}

		private class CountField : ReadOnlyDataFieldView {

			internal static CountField instance = new CountField();

			public override string GetName(object target) {
				return "Count";
			}

			public override Type FieldType {
				get {
					return typeof(int);
				}
			}

			public override object GetValue(object target) {
				return ((IList) target).Count;
			}

			public override string GetStringValue(object target) {
				return ((IList) target).Count.ToString();
			}
		}

		private class IndexField : ReadWriteDataFieldView {
			int index;

			internal IndexField(int index) {
				this.index = index;
			}

			public override string GetName(object target) {
				return "[" + this.index + "]";
			}

			public override Type FieldType {
				get {
					return typeof(object);
				}
			}

			public override object GetValue(object target) {
				IList list = (IList) target;
				int n = list.Count;
				if (this.index < n) {
					return list[this.index];
				}
				return null;
			}

			public override string GetStringValue(object target) {
				IList list = (IList) target;
				int n = list.Count;
				if (this.index < n) {
					return ObjectSaver.Save(list[this.index]);
				}
				return "";
			}

			public override void SetValue(object target, object value) {
				IList list = (IList) target;
				int n = list.Count;
				if (this.index < n) {
					list[this.index] = value;
				}
			}

			public override void SetStringValue(object target, string value) {
				IList list = (IList) target;
				int n = list.Count;
				if (this.index < n) {
					list[this.index] = ObjectSaver.Load(value);
				}
			}
		}

		#region ButtonDataFieldView (Clear)
		public override string GetName(object target) {
			return "Clear";
		}

		public override void OnButton(object target) {
			((IList) target).Clear();
		}
		#endregion ButtonDataFieldView (Clear)
	}
}