using System.Collections.Generic;
using System.ComponentModel;
using SteamEngine.CompiledScripts.Utils;
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

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Utility functions connected with dialogs</summary>
	public static class DialogUtils {

		#region Storing and sorting the principal data list of a dialog
		public static readonly TagKey dataComparerTK = TagKey.Acquire("_data_comparer_");
		public static readonly TagKey dataListTK = TagKey.Acquire("_data_list_");

		public static bool HasDataList<T>(this DialogArgs self) {
			return self.GetTag(dataListTK) is List<T>;
		}

		public static List<T> GetDataList<T>(this DialogArgs self) {
			return self.GetTag(dataListTK) as List<T>;
		}

		public static void SetDataList<T>(this DialogArgs self, List<T> list) {
			self.SetTag(dataListTK, list);
		}

		public static void RemoveDataList(this DialogArgs self) {
			self.RemoveTag(dataListTK);
		}

		public static void SetDataComparerIfNeeded<T>(this DialogArgs self, IComparer<T> comparer) {
			if (!(self.GetTag(dataComparerTK) is IComparer<T>)) {
				self.SetTag(dataComparerTK, comparer);
			}
		}

		public static void SetDataComparerIfNeededLScript<T>(this DialogArgs self, string expression, ListSortDirection direction = ListSortDirection.Ascending) {
			if (!(self.GetTag(dataComparerTK) is IComparer<T>)) {
				LScriptComparer<T> comparer = LScriptComparer<T>.GetComparer(expression, direction);
				self.SetTag(dataComparerTK, comparer);
			}
		}

		public static void SetDataComparer<T>(this DialogArgs self, IComparer<T> comparer) {
			self.SetTag(dataComparerTK, comparer);
		}

		public static IComparer<T> GetDataComparer<T>(this DialogArgs self) {
			return self.GetTag(dataComparerTK) as IComparer<T>;
		}

		public static void SetDataComparerLScript<T>(this DialogArgs self, string expression, ListSortDirection direction = ListSortDirection.Ascending) {
			LScriptComparer<T> comparer = LScriptComparer<T>.GetComparer(expression, direction);
			self.SetDataComparer(comparer);
		}

		public static void SortDataList<T>(this DialogArgs self, bool fallBackToDefault = false) {
			IComparer<T> comparer = self.GetDataComparer<T>();
			if (comparer != null) {
			} else if (fallBackToDefault) {
				comparer = Comparer<T>.Default;
			} else {
				return;
			}

			List<T> list = self.GetDataList<T>();
			if (list != null) {
				list.Sort(comparer);
			}
		}

		public static void SortDataListUsingLscriptExpression<T>(this DialogArgs self, string expression, ListSortDirection direction = ListSortDirection.Ascending) {
			List<T> list = self.GetDataList<T>();
			if (list != null) {
				self.SetDataComparer(SortUsingLscriptExpression(list, expression, direction));
			}
		}

		public static LScriptComparer<T> SortUsingLscriptExpression<T>(List<T> list, string expression, ListSortDirection direction = ListSortDirection.Ascending) {
			LScriptComparer<T> comparer = LScriptComparer<T>.GetComparer(expression, direction);
			list.Sort(comparer);
			return comparer;
		}
		#endregion
	}
}