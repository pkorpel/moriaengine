using System;
using System.Collections.Generic;
using System.Reflection;

namespace SteamEngine.Common {

	//children are supposed to be simple classes with some string fields (public or internal, doesn't really matter) representing the localised messages.
	//Then it should be used with the Loc<> class

	//the system will then make sure it's available in all languages and corresponding txt files are created and maintained in the \Language\ subdir
	public abstract class CompiledLocStringCollection<T> : LocStringCollection
		where T : CompiledLocStringCollection<T> {
		internal static Language languageBeingActivated;

		protected CompiledLocStringCollection()
			: base(languageBeingActivated,
				  assemblyName: GetAssemblyTitle(typeof(T).Assembly),
				  defname: typeof(T).Name) {
			this.SetFieldsFromContents();
		}

		private void SetFieldsFromContents() {
			foreach (var fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (fi.FieldType == typeof(string) && !fi.DeclaringType.IsAbstract) {
					fi.SetValue(this, string.Intern(this.GetEntry(fi.Name)));
				}
			}
		}

		protected override IEnumerable<KeyValuePair<string, string>> GetEntriesFromCode() {
			foreach (FieldInfo fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (fi.FieldType == typeof(string) && !fi.DeclaringType.IsAbstract) {
					yield return new KeyValuePair<string, string>(fi.Name,
						(string) fi.GetValue(this));
				}
			}
		}

		private static string GetAssemblyTitle(Assembly assembly) {
			AssemblyTitleAttribute titleAttr = (AssemblyTitleAttribute) Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute));
			if (titleAttr != null) {
				return titleAttr.Title;
			}
			return assembly.GetName().Name;
		}
	}
}
