using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1070 - Sphere's [NAMES x] lists (spherename.scp), converted as [NameList x]: a random name from one,
	/// as Sphere's "#NAMES_HUMANMALE" in a name, or the mask's suggested name.</summary>
	public sealed class NameList : IUnloadable {
		private static readonly Dictionary<string, NameList> byName = new Dictionary<string, NameList>(StringComparer.OrdinalIgnoreCase);
		private static readonly Regex tokenRE = new Regex(@"#(NAMES_\w+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		private readonly List<string> names = new List<string>();
		private bool unloaded;

		public static void Bootstrap() {
			ScriptLoader.RegisterScriptType("NameList", LoadFromScripts, false);
		}

		private static IUnloadable LoadFromScripts(PropsSection input) {
			NameList list = new NameList();
			foreach (PropsLine line in input.PropsLines) {
				string name = line.Value.Trim().Trim('"');
				if (name.Length > 0) {
					list.names.Add(name);
				}
			}
			byName[input.HeaderName] = list;
			return list;
		}

		/// <summary>A random name of the list, or null if there's no such list.</summary>
		public static string Random(string listName) {
			if (byName.TryGetValue(listName, out NameList list) && !list.unloaded && (list.names.Count > 0)) {
				return list.names[Globals.dice.Next(list.names.Count)];
			}
			return null;
		}

		/// <summary>Every "#NAMES_x" in the text becomes a random name of that list.</summary>
		public static string Resolve(string text) {
			if (string.IsNullOrEmpty(text) || (text.IndexOf('#') < 0)) {
				return text;
			}
			return tokenRE.Replace(text, m => Random(m.Groups[1].Value) ?? m.Value).Trim();
		}

		public void Unload() {
			this.unloaded = true;
		}

		public bool IsUnloaded {
			get { return this.unloaded; }
		}
	}
}
