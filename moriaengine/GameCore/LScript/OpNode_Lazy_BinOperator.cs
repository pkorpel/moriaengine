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
using System.Globalization;
using System.Reflection;
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Lazy_BinOperator : OpNode, IOpNodeHolder {
		internal OpNode left;
		internal OpNode right;

		private object leftResult;
		private object rightResult;

		internal static OpNode_Lazy_BinOperator Construct(IOpNodeHolder parent, Node code) {
			return new OpNode_Lazy_BinOperator(parent, code);
		}

		internal OpNode_Lazy_BinOperator(IOpNodeHolder parent, Node code)
			:
			base(parent, LScriptMain.GetParentScriptHolder(parent).filename, code.GetStartLine() + LScriptMain.startLine,
			code.GetStartColumn(), code) {
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (this.left == oldNode) {
				this.left = newNode;
			} else if (this.right == oldNode) {
				this.right = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		internal override object Run(ScriptVars vars) {
			string opString = LScriptMain.GetString(this.OrigNode).Trim().ToLowerInvariant();
			this.leftResult = this.left.Run(vars);
			this.rightResult = this.right.Run(vars);

			try {
				ITriable newNode = null;

				switch (opString) {
					case ("+"):
						if (this.OperandsAreNumbers()) {
							newNode = new OpNode_AddOperator(this.parent, this.OrigNode);
						} else if ((this.leftResult is string) || (this.rightResult is string)) {
							newNode = new OpNode_ConcatOperator(this.parent, this.OrigNode);
						} else {
							newNode = this.FindOperatorMethod("op_Addition");
						}
						break;
					case ("-"):
						if (this.OperandsAreNumbers()) {
							newNode = new OpNode_SubOperator(this.parent, this.OrigNode);
						} else {
							newNode = this.FindOperatorMethod("op_Subtraction");
						}
						break;
					case ("/"):
						if (this.OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_DivOperator_Double(this.parent, this.OrigNode);
							} else {
								newNode = new OpNode_DivOperator_Int(this.parent, this.OrigNode);
							}
						} else {
							newNode = this.FindOperatorMethod("op_Division");
						}
						break;
					case ("div"):
						if (this.OperandsAreNumbers()) {
							newNode = new OpNode_DivOperator_Int(this.parent, this.OrigNode);
						}
						break;
					case ("*"):
						if (this.OperandsAreNumbers()) {
							newNode = new OpNode_MulOperator(this.parent, this.OrigNode);
						} else {
							newNode = this.FindOperatorMethod("op_Multiply");
						}
						break;
					case ("%"):
						if (this.OperandsAreNumbers()) {
							newNode = new OpNode_ModOperator(this.parent, this.OrigNode);
						} else {
							newNode = this.FindOperatorMethod("op_Modulus");
						}
						break;
					case ("&"):
						if (this.OperandsAreNumbers()) {
							newNode = new OpNode_BinaryAndOperator(this.parent, this.OrigNode);
						} else {
							newNode = this.FindOperatorMethod("op_BitwiseAnd");
						}
						break;
					case ("|"):
						if (this.OperandsAreNumbers()) {
							newNode = new OpNode_BinaryOrOperator(this.parent, this.OrigNode);
						} else {
							newNode = this.FindOperatorMethod("op_BitwiseOr");
						}
						break;
					case ("=="):
						if (this.OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_EqualityOperator_Double(this.parent, this.OrigNode);
							} else {
								newNode = new OpNode_EqualityOperator_Int(this.parent, this.OrigNode);
							}
						} else {
							newNode = this.FindOperatorMethod("op_Equality");
							if (newNode == null) {
								newNode = new OpNode_EqualsOperator(this.parent, this.OrigNode);
							}
						}
						break;
					case ("!="):
						if (this.OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_InEqualityOperator_Double(this.parent, this.OrigNode);
							} else {
								newNode = new OpNode_InEqualityOperator_Int(this.parent, this.OrigNode);
							}
						} else {
							newNode = this.FindOperatorMethod("op_Inequality");
							if (newNode == null) {
								newNode = new OpNode_EqualsNotOperator(this.parent, this.OrigNode);
							}
						}
						break;
					case ("<="):
						if (this.OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_LessThanOrEqualOperator_Double(this.parent, this.OrigNode);
							} else {
								newNode = new OpNode_LessThanOrEqualOperator_Int(this.parent, this.OrigNode);
							}
						} else {
							newNode = this.FindOperatorMethod("op_LessThanOrEqual");
						}
						break;
					case (">="):
						if (this.OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_GreaterThanOrEqualOperator_Double(this.parent, this.OrigNode);
							} else {
								newNode = new OpNode_GreaterThanOrEqualOperator_Int(this.parent, this.OrigNode);
							}
						} else {
							newNode = this.FindOperatorMethod("op_GreaterThanOrEqual");
						}
						break;
					case ("<"):
						if (this.OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_LessThanOperator_Double(this.parent, this.OrigNode);
							} else {
								newNode = new OpNode_LessThanOperator_Int(this.parent, this.OrigNode);
							}
						} else {
							newNode = this.FindOperatorMethod("op_LessThan");
						}
						break;
					case (">"):
						if (this.OperandsAreNumbers()) {
							if (Globals.ScriptFloats) {
								newNode = new OpNode_GreaterThanOperator_Double(this.parent, this.OrigNode);
							} else {
								newNode = new OpNode_GreaterThanOperator_Int(this.parent, this.OrigNode);
							}
						} else {
							newNode = this.FindOperatorMethod("op_GreaterThan");
						}
						break;
					default:
						throw new SEException("Operator " + opString + " unkown or not implemented.");
				}

				if (newNode != null) {
					object retVal;
					OpNode_Lazy_BinOperator newNodeAsBinOp = newNode as OpNode_Lazy_BinOperator;
					if (newNodeAsBinOp != null) {
						newNodeAsBinOp.left = this.left;
						newNodeAsBinOp.right = this.right;
					}
					OpNode newNodeAsON = (OpNode) newNode;
					this.ReplaceSelf(newNodeAsON);
					retVal = newNode.TryRun(vars, new[] {this.leftResult, this.rightResult });
					if ((this.left is OpNode_Object) && (this.right is OpNode_Object)) {
						//both operands are constant -> result is also constant
						OpNode constNode = OpNode_Object.Construct(this.parent, retVal);
						this.parent.Replace(newNodeAsON, constNode);
					}
					return retVal;
				}
				throw new SEException(string.Format(CultureInfo.InvariantCulture, 
					"Operator {0} is not applicable to these operands(type {1} and {2}).",
					opString,
					(this.leftResult == null ? "<null>" : Tools.TypeToString(this.leftResult.GetType())),
					(this.rightResult == null ? "<null>" :  Tools.TypeToString(this.rightResult.GetType()))));
			} catch (InterpreterException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating binary operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		private bool OperandsAreNumbers() {
			if (this.leftResult == null) {
				if (this.rightResult == null) {
					return false;
				}
				return ConvertTools.IsNumberType(this.rightResult.GetType());
			}
			if (this.rightResult == null) {
				return ConvertTools.IsNumberType(this.leftResult.GetType());
			}
			return ((ConvertTools.IsNumberType(this.leftResult.GetType())) && (ConvertTools.IsNumberType(this.rightResult.GetType())));
		}

		private OpNode_MethodWrapper FindOperatorMethod(string methodName)
		{
			if (this.leftResult == null) {
				if (this.rightResult == null) {
					return null;
				}
				return this.FindOperatorMethodOnType(methodName, this.rightResult.GetType());
			}
			if (this.rightResult == null) {
				return this.FindOperatorMethodOnType(methodName, this.leftResult.GetType());
			}
			OpNode_MethodWrapper method = this.FindOperatorMethodOnType(methodName, this.leftResult.GetType());
			if (method != null) {
				return method;
			}
			return this.FindOperatorMethodOnType(methodName, this.rightResult.GetType());
		}

		private OpNode_MethodWrapper FindOperatorMethodOnType(string methodName, Type type) {
			List<MethodInfo> matches = new List<MethodInfo>();

			MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
			foreach (MethodInfo mi in methods) {
				if (mi.Name.Equals(methodName)) { //true for case insensitive
					ParameterInfo[] pars = mi.GetParameters();
					if (pars.Length == 2) {
						if (MemberResolver.IsCompatibleType(pars[0].ParameterType, this.leftResult)
								&& MemberResolver.IsCompatibleType(pars[1].ParameterType, this.rightResult)) {
							matches.Add(mi);
						}
					}
				}
			}
			if (matches.Count == 1) {
				MethodInfo method = MemberWrapper.GetWrapperFor(matches[0]);
				OpNode_MethodWrapper newNode = new OpNode_MethodWrapper(this.parent, this.filename,
					this.line, this.column, this.OrigNode, method, this.left, this.right);
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

		internal override string OrigString {
			get {
				return this.left.OrigString + LScriptMain.GetString(this.OrigNode) + this.left.OrigString;
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(this.left);
			str.Append(" ").Append(LScriptMain.GetString(this.OrigNode).Trim()).Append(" ");
			str.Append(this.right);
			str.Append(")");
			return str.ToString();
		}
	}
}