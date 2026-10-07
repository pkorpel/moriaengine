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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Abstract class providing basics to display a non editable 'label-value' in the dialog</summary>
	public abstract class ReadOnlyDataFieldView : IDataFieldView {
		/// <summary>There is no button for this dataview field</summary>
		public virtual bool IsButtonEnabled {
			get {
				return false;
			}
		}

		/// <summary>Yes, this dataview field is read only</summary>
		public bool ReadOnly {
			get {
				return true;
			}
		}

		/// <summary>This method is forbidden in this class</summary>
		public void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set a value to the non-editable field"));
		}

		/// <summary>This method is forbidden in this class</summary>
		public void SetStringValue(object target, string value) {
			throw new SEException(LogStr.Error("Cannot convert and set a stringified value to the non-editable field"));
		}

		/// <summary>This field does not have any buttons too - buttons have another type of data view</summary>
		public virtual void OnButton(object target) {
			throw new SEException(LogStr.Error("This dataview cannot have any buttons"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string GetName(object target);
		public abstract Type FieldType { get; }
		public abstract object GetValue(object target);
		public abstract string GetStringValue(object target);
	}
}