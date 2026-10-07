/*
	Sanity.cs
    Copyright (C) 2004 Richard Dillingham and contributors (if any)

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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace SteamEngine.Common {
	public interface IEnumItem {
		long Value { get; }
		string Description { get; }
	}

	public class EnumItem<T> : IEnumItem where T : struct {
		private readonly T value;
		private readonly string description;

		private static EnumItem<T>[] allItems;

		static EnumItem() {
			T[] allValues = (T[]) Enum.GetValues(typeof(T));

			int n = allValues.Length;
			allItems = new EnumItem<T>[n];

			for (int i = 0; i < n; i++) {
				allItems[i] = new EnumItem<T>(allValues[i]);
			}
		}

		private EnumItem(T item) {
			Type enumType = typeof(T);
			if (enumType.BaseType != typeof(Enum)) {
				throw new SEException("T must be of type System.Enum");
			}

			this.value = item;
			this.description = GetEnumDescription(item);
		}

		private static string GetEnumDescription(T value) {
			FieldInfo fi = typeof(T).GetField(value.ToString());

			DescriptionAttribute[] attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(
				typeof(DescriptionAttribute), false);

			if (attributes != null && attributes.Length > 0) {
				return attributes[0].Description;
			}
			return value.ToString();
		}

		public static IList<EnumItem<T>> GetAllItemsAsList() {
			return new ReadOnlyCollection<EnumItem<T>>(allItems);
		}

		public T TValue {
			get {
				return this.value;
			}
		}

		public long Value {
			get {
				return Convert.ToInt64(this.value, CultureInfo.InvariantCulture);
			}
		}

		public string Description {
			get {
				return this.description;
			}
		}
	}
}