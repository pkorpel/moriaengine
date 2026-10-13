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
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Permanent regression guard for #104 (Part B): every live *Loc class's field and every
	/// live ScriptedLoc entry should resolve to a real value (from GameText.resx/.Czech.resx)
	/// for both languages, not the "<LocalisationEntryNotAvailable>" sentinel - that sentinel
	/// means a resx key is missing (e.g. a field got renamed without updating the resx).
	/// </summary>
	public static class LocalizationVerificationTests {
		private const string Sentinel = "<LocalisationEntryNotAvailable>";

		[SteamFunction]
		public static void TestVerifyAllLocEntries(Character self) {
			int n = Tools.GetEnumLength<Language>();
			int checked_ = 0;
			int missing = 0;

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
				Type[] types;
				try {
					types = asm.GetTypes();
				} catch (ReflectionTypeLoadException e) {
					types = Array.FindAll(e.Types, t => t != null);
				}

				foreach (Type type in types) {
					if (type.IsAbstract || !IsCompiledLocSubclass(type)) {
						continue;
					}

					MethodInfo getMethod = typeof(Loc<>).MakeGenericType(type).GetMethod("Get",
						BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Language) }, null);

					for (int i = 0; i < n; i++) {
						Language lang = (Language) i;
						object instance = getMethod.Invoke(null, new object[] { lang });
						foreach (var kvp in (LocStringCollection) instance) {
							checked_++;
							if (kvp.Value == Sentinel) {
								missing++;
								Console.WriteLine("LOC_VERIFY_MISSING:" + type.Name + ":" + lang + ":" + kvp.Key);
							}
						}
					}
				}
			}

			for (int i = 0; i < n; i++) {
				Language lang = (Language) i;
				foreach (string defname in ScriptedLocResxBridge.KnownDefnames) {
					LocStringCollection loc = LocManager.GetLoc(defname, lang);
					if (loc == null) {
						missing++;
						Console.WriteLine("LOC_VERIFY_MISSING:" + defname + ":" + lang + ":(not registered)");
						continue;
					}
					foreach (var kvp in loc) {
						checked_++;
						if (kvp.Value == Sentinel) {
							missing++;
							Console.WriteLine("LOC_VERIFY_MISSING:" + defname + ":" + lang + ":" + kvp.Key);
						}
					}
				}
			}

			Console.WriteLine("LOC_VERIFY: checked=" + checked_ + " missing=" + missing);
		}

		private static bool IsCompiledLocSubclass(Type type) {
			for (Type t = type.BaseType; t != null; t = t.BaseType) {
				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CompiledLocStringCollection<>)) {
					return true;
				}
			}
			return false;
		}
	}
}
