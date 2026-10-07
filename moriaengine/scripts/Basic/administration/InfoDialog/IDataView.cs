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
	/// <summary>
	/// Interface for displaying the labels and values of single members of the target
	/// (infoized) object in the dialog
	/// </summary>
	public interface IDataFieldView {
		/// <summary>The name of this field / the label of the button</summary>
		string GetName(object target);

		/// <summary>Is the data read only? - i.e. displaying the settings results?</summary>
		bool ReadOnly { get; }

		/// <summary>Shall this value be displayed with a button?</summary>
		bool IsButtonEnabled { get; }

		/// <summary>The real type of the data field (it needn't necessary be the type of the value...)</summary>
		Type FieldType { get; }

		/// <summary>Take the target object and retreive its member's (for which this interface instance is) value</summary>
		object GetValue(object target);

		/// <summary>Take the target object and set its member's value</summary>
		void SetValue(object target, object value);

		/// <summary>Take the target object and retreive its member's value in the stringified form</summary>
		string GetStringValue(object target);

		/// <summary>Take the stringified value, convert it and set it to the respective member of the target</summary>
		void SetStringValue(object target, string value);

		/// <summary>What will happen when the button is pressed?</summary>
		void OnButton(object target);
	}

	/// <summary>Interface used for all generated DataView classes</summary>
	public interface IDataView {
		/// <summary>This getter will provide us the Type this IDataView is made for</summary>
		Type HandledType { get; }

		/// <summary>If true, subclasses of HandledType will also be handled.</summary>
		bool HandleSubclasses { get; }

		/// <summary>Name that will be displayed in the Info dialog headline - description of the infoized class</summary>
		string GetName(object instance);

		/// <summary>Number of buttons</summary>
		int GetActionButtonsCount(object instance);

		/// <summary>Number of fields</summary>
		int GetFieldsCount(object instance);

		/// <summary>GetPage for data fields</summary>
		IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target);

		/// <summary>GetPage for action buttons</summary>
		IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target);
	}
}