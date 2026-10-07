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
using System.Reflection;

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>Class for managing all generated dataviews and providing them according to wanted type</summary>
	public static class DataViewProvider {
		public static Hashtable dataViewsForTypes = new Hashtable();

		public static SortedList<Type, IDataView> dataViewsForbaseClasses = new SortedList<Type, IDataView>(TypeHierarchyComparer.instance);

		/// <summary>Will find dataview for given type.</summary>
		public static IDataView FindDataViewByType(Type handledType) {
			IDataView view = (IDataView) dataViewsForTypes[handledType];
			if (view != null) {
				return view;
			}
			foreach (KeyValuePair<Type, IDataView> pair in dataViewsForbaseClasses) {
				if (pair.Key.IsAssignableFrom(handledType)) {
					dataViewsForTypes[handledType] = pair.Value;
					return pair.Value;
				}
			}
			return null;
		}

		/// <summary>Register a new hook to ClassManager - it will send the examined Types here and we will care for next.</summary>
		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<IDataView>(CheckGeneratedDataViewClass);
			//ClassManager.RegisterHook(CheckGeneratedDataViewClass);
		}

		/// <summary>
		/// Method for checking if the given Type is a descendant of IDataView. If so, store it in the map
		/// with the HandledType as Key...
		/// </summary>
		public static bool CheckGeneratedDataViewClass(Type type) {
			if (!type.IsAbstract) {
				//if (typeof(IDataView).IsAssignableFrom(type)) { //this should be managed by the ClassManager :)
				ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
				if (ci != null) {
					IDataView idv = (IDataView) ci.Invoke(new object[0] { });

					if (idv.HandleSubclasses) {
						dataViewsForbaseClasses.Add(idv.HandledType, idv);
					} else {
						dataViewsForTypes.Add(idv.HandledType, idv);
					}

				} else {
					throw new SEException("Non-parametric-constructor of " + type + " cannot be created. IDataView cannot be registered.");
				}
				//}
			}
			return false;
		}
	}
}