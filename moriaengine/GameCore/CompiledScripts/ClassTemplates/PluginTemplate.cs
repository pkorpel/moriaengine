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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts.ClassTemplates {
	internal class PluginTemplate : ClassTemplate {
		internal PluginTemplate(ClassTemplateSection section, ClassTemplateSubSection subSection)
			: base(section, subSection) {
		}

		protected override void Process() {
			base.Process();
			this.LoadSaveAttributes();
			this.DefaultConstructor();
		}

		[SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "stack0")]
		private void LoadSaveAttributes() {
			foreach (CodeTypeMember member in this.generatedType.Members) {
				CodeMemberMethod method = member as CodeMemberMethod;
				if (method != null) {
					switch (method.Name.ToLowerInvariant()) {
						case "save":
							method.CustomAttributes.Add(new CodeAttributeDeclaration(
								new CodeTypeReference(typeof(SaveAttribute))));
							break;
						case "loadline":
							method.CustomAttributes.Add(new CodeAttributeDeclaration(
								new CodeTypeReference(typeof(LoadLineAttribute))));
							break;
					}
				}
			}
		}

		private void DefaultConstructor() {
			CodeConstructor constructor = new CodeConstructor();
			constructor.CustomAttributes.Add(new CodeAttributeDeclaration(
				new CodeTypeReference(typeof(LoadingInitializerAttribute))));
			constructor.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			this.generatedType.Members.Add(constructor);
		}
	}
}