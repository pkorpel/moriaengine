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
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal abstract class OpNode_Switch : OpNode, IOpNodeHolder {
		protected OpNode switchNode;
		protected Hashtable cases;
		protected OpNode defaultNode;

		private class TempParent : LScriptHolder, IOpNodeHolder {
			internal TempParent(string filename) : base(filename) {
			}

			void IOpNodeHolder.Replace(OpNode oldNode, OpNode newNode) {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
		internal class NullOpNode : OpNode {
			internal NullOpNode()
				: base(null, null, -1, -1, null) {
			}
			internal override object Run(ScriptVars vars) {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member")]
		internal static readonly NullOpNode nullOpNodeInstance = new NullOpNode();

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			string filename = LScriptMain.GetParentScriptHolder(parent).filename;

			//LScript.DisplayTree(code);

			Production switchProd = (Production)code;
			int caseBlocksCount = switchProd.GetChildCount() - 4;
			if (caseBlocksCount == 0) {//we just run the expression
				return LScriptMain.CompileNode(parent, switchProd.GetChildAt(1));
			}
			OpNode switchNode = LScriptMain.CompileNode(parent, switchProd.GetChildAt(1));//the parent here is fake, it will be set to the correct one soon tho. This is for filename resolving and stuff.
			OpNode defaultNode = null;
			ArrayList tempCases = new ArrayList();
			Hashtable cases = new Hashtable(StringComparer.OrdinalIgnoreCase);
			bool isString = false;
			bool isInteger = false;
			for (int i = 0; i < caseBlocksCount; i++) {
				Production caseProd = (Production)switchProd.GetChildAt(i + 3);
				Node caseValue = caseProd.GetChildAt(1);
				object key = null;
				bool isDefault = false;
				if (IsType(caseValue, StrictConstants.DEFAULT)) {//default
					isDefault = true;
				} else {
					OpNode caseValueNode = LScriptMain.CompileNode(new TempParent(filename), caseValue);//the parent here is fake, it doesn't matter tho.
					key = caseValueNode.Run(new ScriptVars(null, new object(), 0));
					try {
						key = ConvertTools.ToInt32(key);
						isInteger = true;
					} catch {
						key = key as string;
						if (key != null) {
							isString = true;
						}
					}
					if (key == null) {
						throw new InterpreterException("The expression in a Case must be either convertible to an integer, or a string.",
							caseProd.GetStartLine() + LScriptMain.startLine, caseProd.GetStartColumn(),
							filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
					}
				}
				if (caseProd.GetChildCount() > 3) {
					OpNode caseCode = null;
					if (caseProd.GetChildCount() == 6) {//has script
						caseCode = LScriptMain.CompileNode(parent, caseProd.GetChildAt(3));//the parent here is false, it will be set to the correct one soon tho. This is for filename resolving and stuff.
					} else {
						caseCode = nullOpNodeInstance;
					}

					if (tempCases.Count > 0) {
						foreach (object tempKey in tempCases) {
							AddToCases(cases, tempKey, caseCode, line, filename);
						}
						tempCases.Clear();
					}
					if (isDefault) {
						defaultNode = caseCode;
					} else {
						AddToCases(cases, key, caseCode, line, filename);
					}
					//else only has "break" in it
				} else if (isDefault) {
					throw new InterpreterException("The Default block must have some code.",
						line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
				} else {
					tempCases.Add(key);
				}
			}
			OpNode_Switch constructed;
			if (isString && isInteger) {
				throw new InterpreterException("All cases must be either integers or strings.",
					line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
			}
			if (isInteger) {
				constructed = new OpNode_Switch_Integer(
					parent, filename, line, column, code);
			} else {
				constructed = new OpNode_Switch_String(
					parent, filename, line, column, code);
			}
			constructed.switchNode = switchNode;
			constructed.cases = cases;
			constructed.defaultNode = defaultNode;
			switchNode.parent = constructed;
			if (defaultNode != null) {
				defaultNode.parent = constructed;
			}
			foreach (DictionaryEntry entry in cases) {
				if (entry.Value != null) {
					((OpNode)entry.Value).parent = constructed;
				}
			}
			return constructed;
		}

		private static void AddToCases(Hashtable cases, object key, OpNode code, int line, string file) {
			if (cases.Contains(key)) {
				Logger.WriteWarning(file, line, "The case key " + LogStr.Ident(key) + " is duplicate. Only the first occurence is valid.");
			} else {
				cases[key] = code;
			}
		}

		protected OpNode_Switch(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (this.switchNode == oldNode) {
				this.switchNode = newNode;
				return;
			}
			if (this.defaultNode == oldNode) {
				this.defaultNode = newNode;
				return;
			}

			bool foundSome = false;
			foreach (object key in this.cases.Keys) {
				if (key == oldNode) {
					this.cases[key] = newNode;
					foundSome = true;
				}
			}
			if (!foundSome) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal abstract override object Run(ScriptVars vars);

		public override string ToString() {
			StringBuilder str = new StringBuilder("Switch (");
			str.Append(this.switchNode).Append(")").Append(Environment.NewLine);
			foreach (DictionaryEntry entry in this.cases) {
				str.Append("case (").Append(entry.Key).Append(")").Append(Environment.NewLine);
				str.Append(entry.Value);
			}
			str.Append("endswitch");
			return str.ToString();
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Switch_String : OpNode_Switch {
		internal OpNode_Switch_String(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		internal override object Run(ScriptVars vars) {
			object value = string.Concat(this.switchNode.Run(vars));
			OpNode node = (OpNode) this.cases[value];
			if (node != nullOpNodeInstance) {
				if (node == null) {
					node = this.defaultNode;
				}
				if ((node != nullOpNodeInstance) && (node != null)) {
					return node.Run(vars);
				}
			}
			return null;
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Switch_Integer : OpNode_Switch {
		internal OpNode_Switch_Integer(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		internal override object Run(ScriptVars vars) {
			object value;
			try {
				value = Convert.ToInt32(this.switchNode.Run(vars), CultureInfo.InvariantCulture);
			} catch (Exception e) {
				throw new InterpreterException("Exception while parsing integer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
			OpNode node = (OpNode) this.cases[value];
			if (node != nullOpNodeInstance) {
				if (node == null) {
					node = this.defaultNode;
				}
				if ((node != nullOpNodeInstance) && (node != null)) {
					return node.Run(vars);
				}
			}
			return null;
		}
	}


}