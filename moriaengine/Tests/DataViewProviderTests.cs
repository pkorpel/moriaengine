using System;
using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;
using Xunit;

namespace SteamEngine.Tests {
	public class DataViewProviderTests {
		private interface IShape { }

		private class Shape : IShape { }

		private class Square : Shape { }

		private sealed class SmallSquare : Square { }

		private sealed class BaseView : IDataView {
			public BaseView(Type handledType) {
				HandledType = handledType;
			}

			public Type HandledType { get; }
			public bool HandleSubclasses => true;
			public string GetName(object instance) => HandledType.Name;
			public int GetActionButtonsCount(object instance) => 0;
			public int GetFieldsCount(object instance) => 0;
			public IEnumerable<IDataFieldView> GetDataFieldsPage(int firstLineIndex, object target) => Array.Empty<IDataFieldView>();
			public IEnumerable<ButtonDataFieldView> GetActionButtonsPage(int firstLineIndex, object target) => Array.Empty<ButtonDataFieldView>();
		}

		[Fact]
		public void FindDataViewByType_PrefersTheMostDerivedBaseView() {
			// Registered general-first and specific-first, so the order can't decide it.
			var shapeView = new BaseView(typeof(Shape));
			var squareView = new BaseView(typeof(Square));
			var interfaceView = new BaseView(typeof(IShape));
			var views = new IDataView[] { interfaceView, squareView, shapeView };
			DataViewProvider.dataViewsForbaseClasses.AddRange(views);
			try {
				Assert.Same(squareView, DataViewProvider.FindDataViewByType(typeof(SmallSquare)));
				Assert.Same(shapeView, DataViewProvider.FindDataViewByType(typeof(Shape)));
			} finally {
				foreach (var view in views) {
					DataViewProvider.dataViewsForbaseClasses.Remove(view);
				}
				DataViewProvider.dataViewsForTypes.Remove(typeof(SmallSquare));
				DataViewProvider.dataViewsForTypes.Remove(typeof(Shape));
			}
		}
	}
}
