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
	public class DictionaryDataView : ButtonDataFieldView, IDataView {
		public Type HandledType {
			get {
				return typeof(IDictionary);
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
			return ((IDictionary) instance).Count + 1;
		}

		public IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target) {
			if (firstLineIndex == 0) {
				yield return CountField.instance;
			} else {
				firstLineIndex--;
			}
			//iterovat budeme nejpozdeji do doby nez dojdou polozky seznamu
			//(iterovani mozno byt prerusovano kvuli pagingu i jinde...)
			int i = 0;
			foreach (object key in ((IDictionary) target).Keys) {
				//jsem-li s pocitadlem konecne nad firstLineIndexem, zacinam vracet fieldy
				if (i >= firstLineIndex) {
					yield return new IndexKeyValue(key);
				}
				i++;
			}
		}

		public IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target) {
			yield return this;//nechtelo se mi delat dalsi tridu :)
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
				return ((IDictionary) target).Count;
			}

			public override string GetStringValue(object target) {
				return ((IDictionary) target).Count.ToString();
			}
		}

		private class IndexKeyValue : ReadWriteDataFieldView {
			object key;

			internal IndexKeyValue(object key) {
				this.key = key;
			}

			public override string GetName(object target) {
				return this.key.ToString();
			}

			public override Type FieldType {
				get {
					return typeof(object);
				}
			}

			public override object GetValue(object target) {
				IDictionary dict = (IDictionary) target;
				return dict[this.key]; //null or correct value				
			}

			public override string GetStringValue(object target) {
				IDictionary dict = (IDictionary) target;
				if (dict[this.key] != null) {
					return ObjectSaver.Save(dict[this.key]);
				}
				return "";
			}

			public override void SetValue(object target, object value) {
				IDictionary dict = (IDictionary) target;
				dict[this.key] = value;
			}

			public override void SetStringValue(object target, string value) {
				IDictionary dict = (IDictionary) target;
				int n = dict.Count;
				dict[this.key] = ObjectSaver.Load(value);
			}
		}

		#region ButtonDataFieldView (Clear)
		public override string GetName(object target) {
			return "Clear";
		}

		public override void OnButton(object target) {
			((IDictionary) target).Clear();
		}
		#endregion ButtonDataFieldView (Clear)
	}
}