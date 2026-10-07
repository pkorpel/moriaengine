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
using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using Jolt;

namespace SteamEngine.CompiledScripts {

	public static class XmlDocComments {

		static ConcurrentDictionary<Assembly, XmlDocCommentReader> readers = new ConcurrentDictionary<Assembly, XmlDocCommentReader>();

		private static XmlDocCommentReader AcquireReaderForAssembly(Assembly assembly) {
			return readers.GetOrAdd(assembly,
				a => new XmlDocCommentReader(a));
		}

		/// <summary>
		/// Retrieves the xml doc comments for a given <see cref="System.Type"/>.
		/// </summary>
		/// 
		/// <param name="type">
		/// The <see cref="System.Type"/> for which the doc comments are retrieved.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="XElement"/> containing the requested XML doc comments,
		/// or NULL if none were found.
		/// </returns>
		public static XElement GetComments(this Type type) {
			return AcquireReaderForAssembly(type.Assembly).GetComments(type);
		}

		/// <summary>
		/// Retrieves the xml doc comments for a given <see cref="System.Reflection.EventInfo"/>.
		/// </summary>
		/// 
		/// <param name="eventInfo">
		/// The <see cref="System.Reflection.EventInfo"/> for which the doc comments are retrieved.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="XElement"/> containing the requested XML doc comments,
		/// or NULL if none were found.
		/// </returns>
		public static XElement GetComments(this EventInfo eventInfo) {
			return AcquireReaderForAssembly(eventInfo.DeclaringType.Assembly).GetComments(eventInfo);
		}

		/// <summary>
		/// Retrieves the xml doc comments for a given <see cref="System.Reflection.FieldInfo"/>.
		/// </summary>
		/// 
		/// <param name="field">
		/// The <see cref="System.Reflection.FieldInfo"/> for which the doc comments are retrieved.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="XElement"/> containing the requested XML doc comments,
		/// or NULL if none were found.
		/// </returns>
		public static XElement GetComments(this FieldInfo field) {
			return AcquireReaderForAssembly(field.DeclaringType.Assembly).GetComments(field);
		}

		/// <summary>
		/// Retrieves the xml doc comments for a given <see cref="System.Reflection.PropertyInfo"/>.
		/// </summary>
		/// 
		/// <param name="property">
		/// The <see cref="System.Reflection.PropertyInfo"/> for which the doc comments are retrieved.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="XElement"/> containing the requested XML doc comments,
		/// or NULL if none were found.
		/// </returns>
		public static XElement GetComments(this PropertyInfo property) {
			return AcquireReaderForAssembly(property.DeclaringType.Assembly).GetComments(property);
		}

		/// <summary>
		/// Retrieves the xml doc comments for a given <see cref="System.Reflection.ConstructorInfo"/>.
		/// </summary>
		/// 
		/// <param name="constructor">
		/// The <see cref="System.Reflection.ConstructorInfo"/> for which the doc comments are retrieved.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="XElement"/> containing the requested XML doc comments,
		/// or NULL if none were found.
		/// </returns>
		public static XElement GetComments(this ConstructorInfo constructor) {
			return AcquireReaderForAssembly(constructor.DeclaringType.Assembly).GetComments(constructor);
		}

		/// <summary>
		/// Retrieves the xml doc comments for a given <see cref="System.Reflection.MethodInfo"/>.
		/// </summary>
		/// 
		/// <param name="method">
		/// The <see cref="System.Reflection.MethodInfo"/> for which the doc comments are retrieved.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="XElement"/> containing the requested XML doc comments,
		/// or NULL if none were found.
		/// </returns>
		public static XElement GetComments(this MethodInfo method) {
			return AcquireReaderForAssembly(method.DeclaringType.Assembly).GetComments(method);
		}

		static XName XName_summary = XName.Get("summary");
		static XName XName_remarks = XName.Get("remarks");

		/// <summary>
		/// Gets the summary and remarks docstrings concatenated
		/// </summary>
		/// <param name="docElement">The doc element.</param>
		/// <returns></returns>
		public static string GetSummaryAndRemarks(XElement docElement) {
			if (docElement != null) {
				var s = docElement.Element(XName_summary);
				var r = docElement.Element(XName_remarks);

				var sString = s != null ? s.Value : null;
				var rString = r != null ? r.Value : null;

				return string.Concat(sString, Environment.NewLine, rString).Trim();
			}
			return "";
		}
	}
}
