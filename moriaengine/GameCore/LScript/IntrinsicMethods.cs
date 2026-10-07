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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	public static class IntrinsicMethods { //abstract - i.e. static
		public static ISrc Src {
			get {
				return Globals.Src;
			}
		}

		public static void Throw(Exception e) {
			throw e;
		}

		public static TagHolder LastNew {
			get {
				return Globals.LastNew;
			}
		}

		public static AbstractItem LastNewItem {
			get {
				return Globals.LastNewItem;
			}
		}

		public static AbstractCharacter LastNewChar {
			get {
				return Globals.LastNewChar;
			}
		}

		public static Globals Serv {
			get {
				return Globals.Instance;
			}
		}

		public static void Print(string str) {
			//Console.WriteLine("str.GetType(): "+str.GetType());
			Console.WriteLine(str);
		}

		public static void Echo(string str) {
			Globals.SrcWriteLine(str);
		}

		public static void Show(object o) {
			string asString = Tools.ObjToString(o);
			if (o == null) {
			} else {
				Globals.SrcWriteLine(string.Concat(asString,
					"(", Tools.TypeToString(o.GetType()), ")"));
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static int StrLen(string str) {
			return str.Length;
		}

		public static int StrCmp(string str1, string str2) {
			return StringComparer.Ordinal.Compare(str1, str2);
		}

		public static int StrCmpi(string str1, string str2) {
			return StringComparer.OrdinalIgnoreCase.Compare(str1, str2);
		}

		public static ArrayList List() { //a 'shortcut' for spherescript to make an arraylist
			return new ArrayList();
		}

		public static object Qval(bool test, object obj1, object obj2) {
			return test ? obj1 : obj2;
		}

		public static Thing Finduid(int uid) {
			return Thing.UidGetThing(uid);
		}

		public static int Min(int int1, int int2) {
			return Math.Min(int1, int2);
		}

		public static int Max(int int1, int int2) {
			return Math.Max(int1, int2);
		}

		public static object CreateArray(Type type, int length) {
			return Array.CreateInstance(type, length);
		}

		public static object CreateList(Type type) {
			return Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static object CreateGenericObject(Type type, params Type[] genericParams) {
			return Activator.CreateInstance(type.MakeGenericType(genericParams));
		}
	}
}