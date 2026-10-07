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

namespace SteamEngine.CompiledScripts.ClassTemplates {
	internal class PluginDefTemplate : ClassDefTemplate {
		internal PluginDefTemplate(ClassTemplateSection section, ClassTemplateSubSection subSection)
			: base(section, subSection) {
		}

		protected override void Process() {
			base.Process();
			this.Create();
			this.Bootstrap();
		}

		private void Create() {
			CodeMemberMethod create = new CodeMemberMethod();
			create.Name = "CreateImpl";
			create.Attributes = MemberAttributes.Family | MemberAttributes.Override; ;
			create.ReturnType = new CodeTypeReference(typeof(Plugin));
			create.Statements.Add(
				new CodeMethodReturnStatement(
					new CodeObjectCreateExpression(this.section.className
					)
				)
			);
			this.generatedType.Members.Add(create);
		}

		private void Bootstrap() {
			CodeMemberMethod init = new CodeMemberMethod();
			init.Name = "Bootstrap";
			init.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			//if (!section.baseClassName.Equals("Plugin")) {
			init.Attributes |= MemberAttributes.New;
			//}
			//init.Statements.Add(new CodeSnippetStatement("ThingDef.RegisterThingDef(typeof("+name+"Def), \""+name+"\");"));
			init.Statements.Add(
				new CodeMethodInvokeExpression(
					new CodeMethodReferenceExpression(
						new CodeTypeReferenceExpression(typeof(PluginDef)),
						"RegisterPluginDef"),
					new CodeTypeOfExpression(this.section.defClassName),
					new CodeTypeOfExpression(this.section.className)));
			this.generatedType.Members.Add(init);
		}
	}
}