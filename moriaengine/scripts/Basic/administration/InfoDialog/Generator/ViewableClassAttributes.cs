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

	/// <summary>Decorate your class by this attribute if you want it to be viewable by info dialogs.</summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ViewableClassAttribute : Attribute {
		/// <summary>The name that will be displayed in the headline of the infodialog</summary>
		private string name;

		public string Name {
			get {
				return this.name;
			}
		}

		//no params constructor
		public ViewableClassAttribute() {
		}

		public ViewableClassAttribute(string name) {
			this.name = name;
		}
	}

	/// <summary>
	/// Decorate a member of the ViewableClass by this attribute if you want to prevent them to be displayed in info dialogs.
	/// all other attributes will be displayed
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class NoShowAttribute : Attribute {
		//no params constructor
	}

	/// <summary>
	/// Decorate a member of the ViewableClass by this attribute if you want it to be infoized and you want
	/// to specify its name explicitely
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class InfoFieldAttribute : Attribute {
		/// <summary>The name of the field which will be displayed in the dialog rather than the fields name itself</summary>
		private string name;

		public string Name {
			get {
				return this.name;
			}
		}

		public InfoFieldAttribute(string name) {
			this.name = name;
		}
	}

	/// <summary>Used in ViewableClasses for methods we want to be available as buttons in the info dialogs.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ButtonAttribute : Attribute {
		/// <summary>The name of the button which will be connected with the method decorated by this attribute</summary>
		private string name;

		public string Name {
			get {
				return this.name;
			}
		}

		//no params constructor
		public ButtonAttribute() {
		}

		public ButtonAttribute(string name) {
			this.name = name;
		}
	}

	/// <summary>
	/// Used for marking classes used as descriptors - see SimpleClassDescriptor for example.
	/// Obligatory constructor parameter is handled type, voluntary is the name of the described class
	/// </summary>
	public class ViewDescriptorAttribute : Attribute {
		private Type handledType;

		/// <summary>The name that will be displayed in the headline of the infodialog</summary>
		private string name;

		/// <summary>Array of field names that wont be generated to the DataView</summary>
		private string[] nonDisplayedFields;

		public Type HandledType {
			get {
				return this.handledType;
			}
		}

		public string Name {
			get {
				return this.name;
			}
		}

		public string[] NonDisplayedFields {
			get {
				return this.nonDisplayedFields;
			}
		}

		public ViewDescriptorAttribute(Type handledType) {
			this.handledType = handledType;
		}

		public ViewDescriptorAttribute(Type handledType, string name) {
			this.handledType = handledType;
			this.name = name;
		}

		public ViewDescriptorAttribute(Type handledType, string name, string[] nonDisplayedFields) {
			this.handledType = handledType;
			this.name = name;
			this.nonDisplayedFields = nonDisplayedFields;
		}
	}

	/// <summary>Used for marking field get method in descriptors</summary>
	public class GetMethodAttribute : Attribute {
		/// <summary>
		/// The name of the field that appears in the info dialog. Obligatory, it will be used for matching get and set 
		///  descriptor method of the same field
		///  </summary>
		private string name;
		private Type fieldType;

		public string Name {
			get {
				return this.name;
			}
		}

		public Type FieldType {
			get {
				return this.fieldType;
			}
		}

		public GetMethodAttribute(string name, Type fieldType) {
			this.name = name;
			this.fieldType = fieldType;
		}
	}

	/// <summary>Used for marking field set method in descriptors</summary>
	public class SetMethodAttribute : Attribute {
		/// <summary>
		/// The name of the field that appears in the info dialog. Obligatory, it will be used for matching get and set 
		///  descriptor method of the same field
		///  </summary>
		private string name;
		private Type fieldType;

		public string Name {
			get {
				return this.name;
			}
		}

		public Type FieldType {
			get {
				return this.fieldType;
			}
		}

		public SetMethodAttribute(string name, Type fieldType) {
			this.name = name;
			this.fieldType = fieldType;
		}
	}

	/// <summary>For comparing collections containing Types</summary>
	public class TypeHierarchyComparer : IComparer<Type> {
		public static TypeHierarchyComparer instance = new TypeHierarchyComparer();

		private TypeHierarchyComparer() {
		}

		public int Compare(Type x, Type y)
		{
			if (x.IsSubclassOf(y)) {
				return 1;
			}
			if (x == y) {
				return 0;
			}
			return -1;
		}
	}
}