using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts.Utils {

	public class LScriptComparer<T> : IComparer<T> {

		#region Utility methods, for dialogs mostly
		public static LScriptComparer<T> SortAndGetComparer(T[] list, object comparerOrExpression) {
			LScriptComparer<T> comparer = AcquireComparer(comparerOrExpression);
			Array.Sort(list, comparer);
			return comparer;
		}

		public static LScriptComparer<T> SortAndGetComparer(List<T> list, object comparerOrExpression) {
			LScriptComparer<T> comparer = AcquireComparer(comparerOrExpression);
			list.Sort(comparer);
			return comparer;
		}
		#endregion Utility methods, for dialogs mostly

		public static LScriptComparer<T> GetComparer(string sortByExpression, ListSortDirection sortDirection) {
			LScriptComparer<T>[] comparers;
			if (cache.TryGetValue(sortByExpression, out comparers)) {
				return comparers[(int) sortDirection];
			}

			comparers = new[] {
				new LScriptComparer<T>(sortByExpression, 1), 
				new LScriptComparer<T>(sortByExpression, -1)};


			cache.Add(sortByExpression, comparers);
			return comparers[(int) sortDirection];
		}


		private static LScriptComparer<T> AcquireComparer(object comparerOrExpression) {
			LScriptComparer<T> comparer = comparerOrExpression as LScriptComparer<T>;
			if (comparer == null) {
				string expression = string.Concat(comparerOrExpression);
				comparer = GetComparer(expression, ListSortDirection.Ascending);
			}
			return comparer;
		}

		private static readonly Dictionary<string, LScriptComparer<T>[]> cache = new Dictionary<string, LScriptComparer<T>[]>(StringComparer.OrdinalIgnoreCase);
		private static readonly IComparer objectComparer = StructuralComparisons.StructuralComparer;

		private readonly ScriptHolder expression;
		private readonly int multiplier;

		private LScriptComparer(string sortByExpression, int multiplier) {
			sortByExpression = string.Concat("return(", sortByExpression, ")");
			this.expression = LScriptMain.GetNewSnippetRunner("<comparer>", 0, sortByExpression);
			this.multiplier = multiplier;
		}

		public int Compare(T x, T y) {
			var a = this.expression.Run(x);
			var b = this.expression.Run(y);

			return objectComparer.Compare(a, b) * this.multiplier;
		}


	}




	#region using Linq.Expression
	//public static IComparer<ObjectType> GetComparer<ObjectType>(string propertyOrFieldName) {
	//    //var e = Expression.Property(
	//    //MemberExpression me = MemberExpression.
	//    //me.Expression
	//    //Expression.Parameter(

	//    ParameterExpression parameter = Expression.Parameter(typeof(ObjectType));
	//    MemberExpression member = Expression.PropertyOrField(parameter, propertyOrFieldName);

	//    Type t;
	//    var pi = member.Member as PropertyInfo;
	//    if (pi != null) {

	//    } else {

	//    }

	//}

	////private static PropertyInfo GetPropertyInfo(Type t, string propertyName) {
	////    PropertyInfo property = t.GetProperty(propertyName, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
	////    if (property == null) {
	////        property = t.GetProperty(propertyName, BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
	////    }
	////    return property;
	////}

	////private static FieldInfo GetFieldInfo(Type t, string fieldName) {
	////    FieldInfo field = t.GetField(fieldName, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
	////    if (field == null) {
	////        field = t.GetField(fieldName, BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
	////    }
	////    return field;
	////}

	//private interface IComparerCreator<ObjectType> {

	//}

	//private class ComparerCreator : IComparerCreator {

	//}
	#endregion using Linq.Expression

}
