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
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	internal sealed class PluginTriggerGroupGenerator : ISteamCSCodeGenerator {
		static List<Type> pluginTGs = new List<Type>();

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclasses<Plugin>(AddPluginTGType);
		}

		internal static bool AddPluginTGType(Type t) {
			if (!t.IsAbstract) {
				pluginTGs.Add(t);
			}
			return false;
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public CodeCompileUnit WriteSources() {
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
				if (pluginTGs.Count > 0) {
					Logger.WriteDebug("Generating PluginTriggergroups");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (Type decoratedClass in pluginTGs) {
						try {
							GeneratedInstance gi = new GeneratedInstance(decoratedClass);
							if (gi.triggerMethods.Count > 0) {
								CodeTypeDeclaration ctd = gi.GetGeneratedType();
								ns.Types.Add(ctd);
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(decoratedClass.Assembly.GetName().Name, decoratedClass.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating " + pluginTGs.Count + " PluginTriggergroups");
				}
				return codeCompileUnit;
			} finally {
				pluginTGs.Clear();
			}
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}


		public string FileName {
			get { return "PluginTriggerGroups.Generated.cs"; }
		}

		/*
			Constructor: CompiledTriggerGroup
			Creates a triggerGroup named after the class, and then finds and sets up triggers 
			defined in the script (by naming them on_whatever).
		*/
		private class GeneratedInstance {
			internal List<MethodInfo> triggerMethods = new List<MethodInfo>();
			Type pluginType;

			internal CodeTypeDeclaration GetGeneratedType() {
				CodeTypeDeclaration codeTypeDeclaration = new CodeTypeDeclaration("GeneratedPluginTriggerGroup_" + this.pluginType.Name);
				codeTypeDeclaration.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclaration.BaseTypes.Add(typeof(PluginDef.PluginTriggerGroup));
				codeTypeDeclaration.IsClass = true;

				codeTypeDeclaration.Members.Add(this.GenerateRunMethod());
				codeTypeDeclaration.Members.Add(this.GenerateBootstrapMethod(codeTypeDeclaration.Name));

				return codeTypeDeclaration;
			}

			internal GeneratedInstance(Type pluginType) {
				this.pluginType = pluginType;
				MemberTypes memberType = MemberTypes.Method;		//Only find methods.
				BindingFlags bindingAttr = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;

				MemberInfo[] mis = pluginType.FindMembers(memberType, bindingAttr, StartsWithString, "on_");	//Does it's name start with "on_"?
				foreach (MemberInfo m in mis) {
					MethodInfo mi = m as MethodInfo;
					if (mi != null) {
						this.triggerMethods.Add(mi);
					}
				}
			}

			private CodeMemberMethod GenerateRunMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "Run";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Plugin), "self"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TriggerKey), "tk"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScriptArgs), "sa"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				if (this.triggerMethods.Count > 0) {
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning disable 168"));
					retVal.Statements.Add(new CodeVariableDeclarationStatement(
						typeof(object[]),
						"argv"));
					retVal.Statements.Add(new CodeSnippetStatement("#pragma warning restore 168"));

					retVal.Statements.Add(new CodeSnippetStatement("\t\t\tswitch (tk.Uid) {"));
					foreach (MethodInfo mi in this.triggerMethods) {
						TriggerKey tk = TriggerKey.Acquire(mi.Name.Substring(3));
						retVal.Statements.Add(new CodeSnippetStatement("\t\t\t\tcase(" + tk.Uid + "): //" + tk.Name));
						retVal.Statements.AddRange(
							CompiledScriptHolderGenerator.GenerateMethodInvocation(mi,
							new CodeCastExpression(this.pluginType,
								new CodeArgumentReferenceExpression("self")),
							false));

					}
					retVal.Statements.Add(new CodeSnippetStatement("\t\t\t}"));
				}

				retVal.Statements.Add(
					new CodeMethodReturnStatement(
						new CodePrimitiveExpression(null)));

				return retVal;
			}


			private CodeMemberMethod GenerateBootstrapMethod(string typeName) {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Static;
				retVal.Name = "Bootstrap";

				retVal.Statements.Add(
					new CodeMethodInvokeExpression(
						new CodeTypeReferenceExpression(typeof(PluginDef)),
						"RegisterPluginTG",
						new CodeTypeOfExpression(this.pluginType.Name + "Def"),
						new CodeObjectCreateExpression(typeName)
					));

				return retVal;
			}


			private static bool StartsWithString(MemberInfo m, object filterCriteria) {
				string s = ((string) filterCriteria).ToLowerInvariant();
				return m.Name.ToLowerInvariant().StartsWith(s);
			}
		}
	}
}