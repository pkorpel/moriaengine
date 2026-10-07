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

	/// <summary>Abstract class providing basics to display a '[button]-label' in the dialog</summary>
	public abstract class ButtonDataFieldView : IDataFieldView {
		/// <summary>There is actually a button present for this dataview field</summary>
		public bool IsButtonEnabled {
			get {
				return true;
			}
		}

		/// <summary>Nothing to write or set here...</summary>
		public bool ReadOnly {
			get {
				return true;
			}
		}

		public Type FieldType {
			get {
				throw new SEException(LogStr.Error("This property is not provided for button fields"));
			}
		}

		/// <summary>This method is forbidden in this class, there is nothing to set</summary>
		public void SetValue(object target, object value) {
			throw new SEException(LogStr.Error("Cannot set any value to a buttonized dataview field"));
		}

		/// <summary>This method is forbidden in this class, there is nothing to get</summary>
		public object GetValue(object target) {
			throw new SEException(LogStr.Error("Cannot get any value from the buttonized dataview field"));
		}

		/// <summary>This method is forbidden in this class, there is nothing to set</summary>
		public void SetStringValue(object target, string value) {
			throw new SEException(LogStr.Error("Cannot convert and set any stringified value to a buttonized dataview field"));
		}

		/// <summary>This method is forbidden in this class, there is nothing to get</summary>
		public string GetStringValue(object target) {
			throw new SEException(LogStr.Error("Cannot get and convert any value from the buttonized dataview field"));
		}

		//all other properties/methods will be implemented in child classes later
		public abstract string GetName(object target);
		public abstract void OnButton(object target);

	}

}