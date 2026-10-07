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

namespace SteamEngine.CompiledScripts.ClassTemplates {
	internal class ClassTemplateSection {
		internal string templateName;
		internal string className;
		internal string defClassName;
		internal string baseClassName;
		internal string baseDefClassName;

		internal int line;

		internal Dictionary<string, ClassTemplateSubSection> subsections = new Dictionary<string, ClassTemplateSubSection>(StringComparer.OrdinalIgnoreCase);

		internal ClassTemplateSection(int line, string templateName, string className, string baseClassName) {
			this.line = line;
			this.templateName = templateName;
			this.className = Utility.Capitalize(className);
			this.defClassName = this.className + "Def";
			this.baseClassName = baseClassName;
			this.baseDefClassName = baseClassName + "Def";
		}

		internal void AddSubSection(ClassTemplateSubSection subSection) {
			string name = subSection.name;
			if (this.subsections.ContainsKey(name)) {
				throw new SEException("The section " + this.className + " already contains a subsection called " + name);
			}
			this.subsections[name] = subSection;
		}

	}

	internal class ClassTemplateSubSection {
		internal string name;

		internal List<ClassTemplateInstanceField> fields = new List<ClassTemplateInstanceField>();

		internal ClassTemplateSubSection(string name) {
			this.name = name;
		}

		internal void AddField(ClassTemplateInstanceField field) {
			this.fields.Add(field);
		}
	}
}