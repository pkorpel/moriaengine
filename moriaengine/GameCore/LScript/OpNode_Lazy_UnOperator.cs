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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Lazy_UnOperator : OpNode, IOpNodeHolder {
		//accepts SimpleCodeBody, CodeBody
		internal OpNode obj;
		internal Node operatorNode;

		private object result;

		internal static OpNode_Lazy_UnOperator Construct(IOpNodeHolder parent, Node code) {
			return new OpNode_Lazy_UnOperator(parent, code);
		}

		internal OpNode_Lazy_UnOperator(IOpNodeHolder parent, Node code)
			:
				base(parent, LScriptMain.GetParentScriptHolder(parent).filename, code.GetStartLine() + LScriptMain.startLine, code.GetStartColumn(), code) {

			if (code.GetChildCount() == 3) {
				this.operatorNode = code.GetChildAt(1);
				this.obj = LScriptMain.CompileNode(this, code.GetChildAt(2));
			} else if (code.GetChildCount() == 2) {
				this.operatorNode = code.GetChildAt(0);
				this.obj = LScriptMain.CompileNode(this, code.GetChildAt(1));
			} else {
				throw new SEException("Wrong number of child nodes. This should not happen.");
			}
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (this.obj == oldNode) {
				this.obj = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			string opString = LScriptMain.GetString(this.operatorNode).Trim();
			this.result = this.obj.Run(vars);

			if (this.result == null) {
				throw new InterpreterException("Operand of the unary operator '" + opString + "' is null.",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}


			ITriable newNode = null;
			switch (opString) {
				case ("+"):
					if (ConvertTools.IsNumberType(this.result.GetType())) {
						//+ does nothing to numbers
						this.ReplaceSelf(this.obj);
						return this.result;
					}
					newNode = this.FindOperatorMethod("op_UnaryPlus");
					break;
				case ("-"):
					if (ConvertTools.IsNumberType(this.result.GetType())) {
						newNode = new OpNode_MinusOperator(this.parent, this.OrigNode);
					} else {
						newNode = this.FindOperatorMethod("op_UnaryNegation");
					}
					break;
				case ("!"):
					if ((ConvertTools.IsNumberType(this.result.GetType())) || (this.result is bool)) {
					} else {
						newNode = this.FindOperatorMethod("op_LogicalNot");
					}
					newNode = new OpNode_NotOperator(this.parent, this.OrigNode);
					break;
				case ("~"):
					if (ConvertTools.IsNumberType(this.result.GetType())) {
						newNode = new OpNode_BitComplementOperator(this.parent, this.OrigNode);
					} else {
						newNode = this.FindOperatorMethod("op_OnesComplement");
					}
					break;
				default:
					throw new InterpreterException("Operator " + opString + " unkown or not implemented.",
						this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}
			if (newNode != null) {
				object retVal;
				OpNode_Lazy_UnOperator newNodeAsUnOp = newNode as OpNode_Lazy_UnOperator;
				if (newNodeAsUnOp != null) {
					newNodeAsUnOp.obj = this.obj;
				}
				OpNode newNodeAsOpNode = (OpNode) newNode;
				this.ReplaceSelf(newNodeAsOpNode);
				retVal = newNode.TryRun(vars, new[] {this.result });
				if (this.obj is OpNode_Object) {
					//operand is constant -> result is also constant
					OpNode constNode = OpNode_Object.Construct(this.parent, retVal);
					this.parent.Replace(newNodeAsOpNode, constNode);
				}
				return retVal;
			}
			throw new InterpreterException("Operator " + LogStr.Number(opString) + " is not applicable to this operand.",
				this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
		}

		private OpNode_MethodWrapper FindOperatorMethod(string methodName)
		{
			if (this.result != null) {
                List<MethodInfo> matches = new List<MethodInfo>();

				Type type = this.result.GetType();
				MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
				foreach (MethodInfo mi in methods) {
					if (mi.Name.Equals(methodName)) {
						ParameterInfo[] pars = mi.GetParameters();
						if (pars.Length == 1) {
							if (MemberResolver.IsCompatibleType(pars[0].ParameterType, this.result)) {
								matches.Add(mi);
							}
						}
					}
				}
				if (matches.Count == 1) {
					MethodInfo method = MemberWrapper.GetWrapperFor(matches[0]);
					OpNode_MethodWrapper newNode = new OpNode_MethodWrapper(this.parent, this.filename, this.line, this.column, this.OrigNode, method, this.obj);
					return newNode;
				}
				if (matches.Count > 1) {
					//List<MethodInfo> resolvedAmbiguities;
					//if (TryResolveAmbiguity(ambiguities, results, out resolvedAmbiguities)) {

					//}

					StringBuilder sb = new StringBuilder("Ambiguity detected when resolving operator. There were following possibilities:");
					foreach (MethodInfo mi in matches) {
						sb.Append(Environment.NewLine);
						sb.AppendFormat("{0} {1}.{2}", mi.ReturnType, mi.DeclaringType, mi);
					}
					throw new InterpreterException(sb.ToString(),
						this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
				}
				return null;
			}
			throw new InterpreterException("The operand is a null reference.",
				this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
		}


		public override string ToString() {
			StringBuilder str = new StringBuilder("( ");
			str.Append(LScriptMain.GetString(this.operatorNode).Trim()).Append(" ");
			str.Append(this.obj).Append(")");
			return str.ToString();
		}
	}
}