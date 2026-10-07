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

	public class ContainerView : ButtonDataFieldView, IDataView {
		static Dictionary<Type, ThingAsContainer> viewHelpers = new Dictionary<Type, ThingAsContainer>();

		Type IDataView.HandledType {
			get {
				return typeof(ThingAsContainer);
			}
		}

		bool IDataView.HandleSubclasses {
			get {
				return true;
			}
		}

		string IDataView.GetName(object instance) {
			return ((ThingAsContainer) instance).thing.Name + " (edit)";
		}

		int IDataView.GetActionButtonsCount(object instance) {
			var c = ((ThingAsContainer) instance);

			return 1 + c.infoView.GetActionButtonsCount(c.thing);
		}

		int IDataView.GetFieldsCount(object instance) {
			var c = ((ThingAsContainer) instance);

			var asItem = c.thing as Item;
			if (asItem != null) {
				return asItem.Count;
			}
			var asChar = ((Character) c.thing);
			return asChar.VisibleCount + asChar.InvisibleCount;
		}

		IEnumerable<IDataFieldView> IDataView.GetDataFieldsPage(int firstLineIndex, object target) {
			var c = ((ThingAsContainer) target);

			int i = 0;
			foreach (Item item in c.thing) {
				if (i < firstLineIndex) {
					continue;
				}

				yield return new ItemInContainerFieldView
				{
					realTarget = item,
					index = i
				};

				i++;
			}
		}

		private class ItemInContainerFieldView : ReadOnlyDataFieldView {
			internal Item realTarget;
			internal int index;

			public override string GetName(object target) {
				return "[" + this.index + "]";
			}

			public override void OnButton(object target) {
				Edit(this.realTarget);
			}

			public override Type FieldType {
				get { return this.realTarget.GetType(); }
			}

			public override object GetValue(object target) {
				return this.realTarget;
			}

			public override string GetStringValue(object target) {
				return this.realTarget.Name;
			}
		}

		IEnumerable<ButtonDataFieldView> IDataView.GetActionButtonsPage(int firstLineIndex, object target) {
			if (firstLineIndex == 0) {
				yield return this;
			}

			var c = ((ThingAsContainer) target);
			foreach (var a in c.infoView.GetActionButtonsPage(firstLineIndex, c.thing)) {
				yield return new ButtonAdapter { wrappedButton = a };
			}
		}

		private class ButtonAdapter : ButtonDataFieldView {
			internal ButtonDataFieldView wrappedButton;

			public override string GetName(object target) {
				var c = ((ThingAsContainer) target);

				return this.wrappedButton.GetName(c.thing);
			}

			public override void OnButton(object target) {
				var c = ((ThingAsContainer) target);

				this.wrappedButton.OnButton(c.thing);
			}
		}



		/// <summary>
		/// Displays an info dialog modified as to display contained items instead of property values
		/// the name "Edit" comes from sphereserver where it did a similar thing (only worse :)
		/// </summary>
		[SteamFunction]
		public static void Edit(Thing self) {

			Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new ThingAsContainer(self));
		}

		public class ThingAsContainer {
			internal readonly IDataView infoView;
			internal readonly Thing thing;

			public ThingAsContainer(Thing thing) {
				this.infoView = DataViewProvider.FindDataViewByType(thing.GetType());
				this.thing = thing;
			}
		}

		#region "Info" action button impl
		public override void OnButton(object target) {
			Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, ((ThingAsContainer) target).thing);
		}

		public override string GetName(object instance) {
			return "Info";
		}
		#endregion
	}
}