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
	public class EnumDataView : IDataView {
		public Type HandledType {
			get {
				return typeof(Enum);
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
			return 0;
		}

		public int GetFieldsCount(object instance) {
			return Enum.GetNames(instance.GetType()).Length + 1; //1 navic pro celkovej pocet polozek
		}

		public IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target) {
			yield return ItemsCountField.instance;
			//iterovat budeme nejpozdeji do doby nez dojdou polozky seznamu
			//(iterovani mozno byt prerusovano kvuli pagingu i jinde...)
			for (int i = firstLineIndex; i < Enum.GetNames(target.GetType()).Length; i++) {
				yield return EnumItemFields.GetInitializedInstance(i, target);
			}
		}

		public IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target) {
			yield break;//nemame action buttony...
		}

		private class ItemsCountField : ReadOnlyDataFieldView {

			internal static ItemsCountField instance = new ItemsCountField();

			public override string GetName(object target) {
				return "Items";
			}

			public override Type FieldType {
				get {
					return typeof(int);
				}
			}

			public override object GetValue(object target) {
				return Enum.GetNames(target.GetType()).Length;
			}

			public override string GetStringValue(object target) {
				return Convert.ToString(Enum.GetNames(target.GetType()).Length);
			}
		}

		private class EnumItemFields : ReadOnlyDataFieldView {
			int index;
			Type targetEnumType;
			Type underlyingType;
			string[] names;
			object target;

			private static EnumItemFields instance;

			internal static EnumItemFields GetInitializedInstance(int index, object target) {
				if (instance == null) {
					//newinstantiation
					instance = new EnumItemFields(index, target);
				} else {
					//reset everything necessary
					instance.Target = target;
					instance.Index = index;
				}
				return instance;
			}

			private EnumItemFields(int index, object target) {
				this.index = index;
				this.Target = target; //this will set additional properties...				
			}

			private int Index {
				get {
					return this.index;
				}
				set {
					this.index = value;
				}
			}

			private object Target {
				get {
					return this.target;
				}
				set {
					this.target = value;
					this.targetEnumType = this.target.GetType();
					this.underlyingType = Enum.GetUnderlyingType(this.targetEnumType);
					this.names = Enum.GetNames(this.targetEnumType);
					Array.Sort(this.names, delegate(string n1, string n2) {
						return n1.CompareTo(n2);
					}
							   );//sort alphabetically
				}
			}

			public override string GetName(object target) {
				return this.names[this.index];
			}

			public override Type FieldType {
				get {
					return Enum.GetUnderlyingType(this.targetEnumType);
				}
			}

			public override object GetValue(object target) {
				int n = this.names.Length;
				if (this.index < n) {
					//get the value for to the current Name in the sorted Names array
					return Enum.Parse(this.targetEnumType, this.names[this.index]);
				}
				return null;
			}

			public override string GetStringValue(object target) {
				int n = this.names.Length;
				if (this.index < n) {
					//return the value casted to basic type (so e.g. number is displayed and not "red" text...)
					object value = Enum.Parse(this.targetEnumType, this.names[this.index]);
					return Convert.ToString(Convert.ChangeType(value, this.underlyingType));
				}
				return "";
			}
		}
	}
}