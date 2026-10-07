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
	public abstract class CompiledScriptHolder : ScriptHolder {
		private string desc;

		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclassInstances<CompiledScriptHolder>(null, false, false);
		}

		//protected CompiledScriptHolder()
		//    : base() {

		//}

		protected CompiledScriptHolder(string name, string description)
			: base(name) {

			this.desc = description;
		}

		/// <summary>Description provided in any SteamDocAttribute on the SteamFunction</summary>
		public override string Description {
			get {
				return this.desc;
				//return Tools.TypeToString(this.GetType());
			}
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class SteamFunctionAttribute : Attribute {
		private readonly string newFunctionName;

		public SteamFunctionAttribute() {
		}

		public SteamFunctionAttribute(string newFunctionName) {
			this.newFunctionName = newFunctionName;
		}

		public string NewFunctionName {
			get {
				return this.newFunctionName;
			}
		}
	}

	internal sealed class CompiledScriptHolderGenerator : ISteamCSCodeGenerator {
		static List<MethodInfo> compiledSHs = new List<MethodInfo>();

		internal static void AddCompiledSHType(MethodInfo mi) {
			compiledSHs.Add(mi);
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public CodeCompileUnit WriteSources() {
			try {
				CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
				if (compiledSHs.Count > 0) {
					Logger.WriteDebug("Generating compiled ScriptHolders");

					CodeNamespace ns = new CodeNamespace("SteamEngine.CompiledScripts");
					codeCompileUnit.Namespaces.Add(ns);

					foreach (MemberInfo mi in compiledSHs) {
						try {
							SteamFunctionAttribute sfa = Attribute.GetCustomAttribute(mi, typeof(SteamFunctionAttribute)) as SteamFunctionAttribute;
							if (sfa != null) {
								GeneratedInstance gi = new GeneratedInstance((MethodInfo) mi, sfa);
								CodeTypeDeclaration ctd = gi.GetGeneratedType();
								ns.Types.Add(ctd);
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(mi.DeclaringType.Name, mi.Name, e);
							return null;
						}
					}
					Logger.WriteDebug("Done generating " + compiledSHs.Count + " compiled ScriptHolders");
				}
				return codeCompileUnit;
			} finally {
				compiledSHs.Clear();
			}
		}

		public void HandleAssembly(Assembly compiledAssembly) {

		}


		public string FileName {
			get { return "CompiledScriptHolders.Generated.cs"; }
		}

		internal static CodeStatementCollection GenerateMethodInvocation(MethodInfo method, CodeExpression thisInstance, bool thisAsFirstParam) {
			CodeStatementCollection retVal = new CodeStatementCollection();
			//we have "object self" and "ScriptArgs sa"

			ParameterInfo[] pis = method.GetParameters();
			int n = pis.Length;
			CodeExpression[] methodParams = new CodeExpression[n];
			if (n > 0) {

				retVal.Add(new CodeAssignStatement(
					new CodeVariableReferenceExpression("argv"),
					new CodePropertyReferenceExpression(
						new CodeArgumentReferenceExpression("sa"),
						"Argv")));

				int paramOffset = thisAsFirstParam ? 0 : -1;

				for (int i = 0; i < n; i++) {
					ParameterInfo pi = pis[i];
					if ((i == 0) && thisAsFirstParam) {
						methodParams[i] = CastParameter(pi,
								new CodeVariableReferenceExpression("self"));
					} else if ((i == 1 + paramOffset) && (n == 2 + paramOffset) && (typeof(ScriptArgs).IsAssignableFrom(pi.ParameterType))) {
						CodeExpression p = new CodeArgumentReferenceExpression("sa");

						if (typeof(ScriptArgs) != pi.ParameterType) {
							p = new CodeCastExpression(pi.ParameterType, p);
						}

						methodParams[i] = p;
						break;
					} else {
						int index = thisAsFirstParam ? i - 1 : i;
						methodParams[i] = CastParameter(pi,
							new CodeArrayIndexerExpression(
								new CodeVariableReferenceExpression("argv"),
								new CodePrimitiveExpression(index)));
					}
				}
			}

			CodeMethodInvokeExpression cmie = new CodeMethodInvokeExpression(
					thisInstance,
					method.Name,
					methodParams);

			if (method.ReturnType != typeof(void)) {
				retVal.Add(new CodeMethodReturnStatement(cmie));

			} else {
				retVal.Add(cmie);
				retVal.Add(new CodeMethodReturnStatement(
					new CodePrimitiveExpression(null)));
			}

			return retVal;
		}

		private static CodeExpression CastParameter(ParameterInfo pi, CodeExpression input) {
			Type paramType = pi.ParameterType;
			if (ConvertTools.IsNumberType(paramType) || paramType == typeof(DateTime))
			{
				if (paramType.IsEnum) {//Enum.ToObject(type, ConvertTo(Enum.GetUnderlyingType(type), obj));
					Type enumUnderlyingType = Enum.GetUnderlyingType(paramType);
					return new CodeCastExpression(
						paramType,
						GetConvertMethod(enumUnderlyingType, input));
				}
				return GetConvertMethod(paramType, input);
			}
			if (paramType != typeof(object)) {
				return new CodeCastExpression(
					paramType,
					input);
			}
			return input;
		}

		private static CodeMethodInvokeExpression GetConvertMethod(Type convertTo, CodeExpression input) {
			return new CodeMethodInvokeExpression(
				new CodeTypeReferenceExpression(typeof(Convert)),
				"To" + convertTo.Name,
				input);
		}

		private class GeneratedInstance {
			MethodInfo method;
			string name;
			string desc; //obtained from the "summary" or "remark" (or any other SteamDocBaseAttribute)

			internal GeneratedInstance(MethodInfo method, SteamFunctionAttribute sfa) {
				if (!method.IsStatic) {
					throw new SEException("The method with [SteamFunctionAttribute] must be static");
				}

				this.method = method;
				this.name = sfa.NewFunctionName;
				if (string.IsNullOrEmpty(this.name)) {
					this.name = method.Name;
				}
				//SummaryAttribute sma = Attribute.GetCustomAttribute(method, typeof(SummaryAttribute)) as SummaryAttribute;
				//if (sma != null) {
				//    desc = sma.Text;
				//} 
				try {
					this.desc = XmlDocComments.GetSummaryAndRemarks(method.GetComments());
				} catch { }
			}

			internal CodeTypeDeclaration GetGeneratedType() {
				CodeTypeDeclaration codeTypeDeclatarion = new CodeTypeDeclaration("GeneratedScriptHolder_" + this.name);
				codeTypeDeclatarion.TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed;
				codeTypeDeclatarion.BaseTypes.Add(typeof(CompiledScriptHolder));
				codeTypeDeclatarion.IsClass = true;

				codeTypeDeclatarion.Members.Add(this.GenerateConstructor());
				codeTypeDeclatarion.Members.Add(this.GenerateRunMethod());
				return codeTypeDeclatarion;
			}

			private CodeMemberMethod GenerateConstructor() {
				CodeConstructor retVal = new CodeConstructor();
				retVal.Attributes = MemberAttributes.Public;
				retVal.BaseConstructorArgs.Add(new CodePrimitiveExpression(this.name));
				retVal.BaseConstructorArgs.Add(new CodePrimitiveExpression(this.desc));

				retVal.Statements.Add(new CodeMethodInvokeExpression(
					new CodeBaseReferenceExpression(), "RegisterAsFunction"));
				return retVal;
			}

			private CodeMemberMethod GenerateRunMethod() {
				CodeMemberMethod retVal = new CodeMemberMethod();
				retVal.Attributes = MemberAttributes.Public | MemberAttributes.Override;
				retVal.Name = "Run";
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "self"));
				retVal.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ScriptArgs), "sa"));
				retVal.ReturnType = new CodeTypeReference(typeof(object));

				if (this.method.GetParameters().Length > 0) {
					retVal.Statements.Add(new CodeVariableDeclarationStatement(
						typeof(object[]),
						"argv"));
				}

				retVal.Statements.AddRange(
					GenerateMethodInvocation(this.method, new CodeTypeReferenceExpression(this.method.DeclaringType), true));

				return retVal;
			}

			//private static bool StartsWithString(MemberInfo m, object filterCriteria) {
			//    string s = ((string) filterCriteria).ToLower(System.Globalization.CultureInfo.InvariantCulture);
			//    return m.Name.ToLower(System.Globalization.CultureInfo.InvariantCulture).StartsWith(s);
			//}
		}
	}

}