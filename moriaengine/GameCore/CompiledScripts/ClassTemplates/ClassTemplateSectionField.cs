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
using System.CodeDom;
using System.Diagnostics.CodeAnalysis;

namespace SteamEngine.CompiledScripts.ClassTemplates {

	internal class ClassTemplateInstanceField {

		internal string capName;
		internal string uncapName;
		internal string value;
		internal string typeString;
		internal Type type;
		internal bool needsCopying;
		internal bool isStatic;

		//<access>(public)|(private)|(protected)|(internal))\s+)?
		//(?<static>static\s+)?
		internal MemberAttributes access;

		internal ClassTemplateInstanceField(string access, string isStatic, string type, string name, string value) {
			this.typeString = type;
			this.uncapName = Utility.Uncapitalize(name);
			this.capName = Utility.Capitalize(name);
			this.value = value;
			this.access = ParseAccess(access);
			this.SetType(type);
			if (isStatic.Length > 0) {
				this.isStatic = true;
			}
		}

		private void SetType(string typeName) {
			switch (typeName.ToLowerInvariant()) {
				case "bool": {
						typeName = "Boolean";
						break;
					}
				case "byte": {
						typeName = "Byte";
						break;
					}
				case "sbyte": {
						typeName = "SByte";
						break;
					}
				case "ushort": {
						typeName = "UInt16";
						break;
					}
				case "short": {
						typeName = "Int16";
						break;
					}
				case "uint": {
						typeName = "UInt32";
						break;
					}
				case "int": {
						typeName = "Int32";
						break;
					}
				case "ulong": {
						typeName = "UInt64";
						break;
					}
				case "long": {
						typeName = "Int64";
						break;
					}
				case "float": {
						typeName = "Single";
						break;
					}
				case "double": {
						typeName = "Double";
						break;
					}
				case "decimal": {
						typeName = "Decimal";
						break;
					}
				case "char": {
						typeName = "Char";
						break;
					}
			}
			this.type = ClassManager.GetType(typeName);
			if (this.type == null) {
				this.type = Type.GetType(typeName, false, true);
				if (this.type == null) {
					this.type = Type.GetType("System." + typeName, false, true);
					if (this.type == null) {
						this.type = Type.GetType("SteamEngine." + typeName, false, true);
						if (this.type == null) {
							this.type = Type.GetType("System.Collections." + typeName, false, true);
						}
					}
				}
			}

			if (this.type != null) {
				if (typeof(Thing).IsAssignableFrom(this.type)) {
					this.needsCopying = false;
				} else {
					this.needsCopying = !DeepCopyFactory.IsNotCopied(this.type);
				}
				this.typeString = this.type.Name;
			} else {
				//Console.WriteLine("unrecognized type "+typeName);
				this.typeString = typeName;
				this.needsCopying = true;
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "s")]
		private static MemberAttributes ParseAccess(string s) {
			MemberAttributes ret = MemberAttributes.Final;
			switch (s.ToLowerInvariant()) {
				case "public":
					ret |= MemberAttributes.Public;
					break;
				case "protected":
					ret |= MemberAttributes.Family;
					break;
				case "internal":
					ret |= MemberAttributes.Assembly;
					break;
				case "private":
					ret |= MemberAttributes.Private;
					break;
			}
			return ret;
		}
	}
}