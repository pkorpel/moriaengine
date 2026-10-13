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

		//Values now come straight from GameTextResources (#104, Part B) keyed by
		//{ClassName}_{FieldName}, not from the field's own (not-yet-initialized at this
		//point in construction) value - this used to read `fi.GetValue(this)`, but that ran
		//before this class's own field initializers in the ctor chain anyway.
		protected override IEnumerable<KeyValuePair<string, string>> GetEntriesFromCode() {
			foreach (FieldInfo fi in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (fi.FieldType == typeof(string) && !fi.DeclaringType.IsAbstract) {
					string flatKey = typeof(T).Name + "_" + fi.Name;
					yield return new KeyValuePair<string, string>(fi.Name,
						GameTextResources.GetEntry(flatKey, this.Language));
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
