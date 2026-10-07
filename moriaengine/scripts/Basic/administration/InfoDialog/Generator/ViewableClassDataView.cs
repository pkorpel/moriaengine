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

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>
	/// The ancestor of all generated classes that manage and return the paged data to display.
	/// It implements two interfaces - first for paging the data fields, second for paging the action buttons
	/// both types will be available by two similar GetPage methods.
	/// </summary>
	public abstract class ViewableClassDataView : IDataView {
		/// <summary>
		/// Implement the method to retF:\Development\SE\moriaengine\scripts\Basic\administration\InfoDialog\DataView.csurn an initialized instance of AbstractPage. This 
		/// should be then used in foreach block or somehow (as an IEnumerable) for iterating through the data fields
		/// </summary>
		public abstract IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target);

		/// <summary>Similar as the previous method but for iterating over the action buttons pages</summary>
		public abstract IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target);

		//these three interface properties will be implemented in children
		public abstract Type HandledType { get; }

		public bool HandleSubclasses {
			get {
				return false;
			}
		}

		public abstract string GetName(object instance);

		public abstract int GetActionButtonsCount(object instance);

		public abstract int GetFieldsCount(object instance);


		/// <summary>
		/// This will be used to return the desired page of objects to be displayed
		/// it will also hold all the IDataFieldViews belonging to the ViewableClass
		/// </summary>
		public abstract class AbstractPage<T> : IEnumerable<T>, IEnumerator<T> {
			//increased everytime the MoveNext method will be invoked
			protected int nextIndex;
			protected object target; //the parent object we are making info/settings on
			//this is the current field we are displaying - it will be used in Enumerators methods
			protected T current;

			/// <summary>
			/// This method will be used by IPageableCollection to prepare the Enumerator
			/// - set the starting index and the reference object from which we possibly can obtain some
			/// necessary inforamtion such as upper bound of iteration... if needed
			/// </summary>
			public AbstractPage(int startIndex, object target) {
				//initialize indices and prepare for usage
				this.nextIndex = startIndex;
				this.target = target;
			}

			#region IEnumerable<T> Members
			/// <summary>Interface method used for iterating - it will return itself, but prepared for iterating</summary>
			public IEnumerator<T> GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerable Members
			/// <summary>Interface method used for iterating - it will return itself, but prepared for iterating</summary>
			IEnumerator IEnumerable.GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerator<T> Members
			/// <summary>Yet another interface property - returns the prepared field for displaying</summary>
			public T Current {
				get {
					return this.current;
				}
			}
			#endregion

			#region IDisposable Members
			/// <summary>Do nothing, we don't need to dispose anything in some special way</summary>
			public void Dispose() {
				//we dont care
			}
			#endregion

			#region IEnumerator Members

			/// <summary>
			/// This is the most important method - it will ensure the iterating on the fields 
			/// belonging to the desired page
			/// </summary>
			public abstract bool MoveNext();

			public void Reset() {
				throw new SEException("The method or operation is not implemented.");
			}

			object IEnumerator.Current {
				get {
					return this.current;
				}
			}
			#endregion
		}
	}
}