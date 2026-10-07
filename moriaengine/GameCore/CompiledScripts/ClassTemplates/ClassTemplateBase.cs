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

using System.CodeDom;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts.ClassTemplates {
	internal abstract class ClassTemplateBase {

		internal static void ProcessSection(ClassTemplateSection section, CodeCompileUnit ccu) {
			ClassTemplateSubSection defss = null;
			ClassTemplateSubSection ss = null;

			foreach (ClassTemplateSubSection entry in section.subsections.Values) {
				switch (entry.name.ToLowerInvariant()) {
					case "def":
					case "defvars":
					case "defs":
						defss = entry;
						break;
					case "var":
					case "vars":
					case "tags":
						ss = entry;
						break;
					default:
						throw new ScriptException("Unknown section type '" + entry.name + ". Valid section types: (def, defvars, or defs), (var, vars, or tags)");
				}
			}

			foreach (ClassTemplateBase ctb in GetImplementations(section, defss, ss)) {
				ccu.Namespaces[0].Types.Add(ctb.GeneratedType);
			}
		}

		//TODO? make this somehow generic if there's more than just things and plugins?
		private static IEnumerable<ClassTemplateBase> GetImplementations(ClassTemplateSection section, ClassTemplateSubSection defss, ClassTemplateSubSection ss) {
			switch (section.templateName.ToLowerInvariant()) {
				case "thingtemplate":
				case "thing":
					yield return new ThingDefTemplate(section, defss);
					yield return new ThingTemplate(section, ss);
					break;
				case "plugintemplate":
				case "plugin":
					yield return new PluginDefTemplate(section, defss);
					yield return new PluginTemplate(section, ss);
					break;
				default:
					throw new SEException("Unknown classtemplate " + section.templateName);
			}
		}


		protected ClassTemplateSection section;
		protected ClassTemplateSubSection subSection;

		protected CodeTypeDeclaration generatedType;

		internal ClassTemplateBase(ClassTemplateSection section, ClassTemplateSubSection subSection) {
			this.section = section;
			this.subSection = subSection;
		}

		internal CodeTypeDeclaration GeneratedType {
			get {
				if (this.generatedType == null) {
					this.Process();
				}
				return this.generatedType;
			}
		}

		protected virtual void Process() {
		}
	}
}