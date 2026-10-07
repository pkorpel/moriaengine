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
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_Foreach : OpNode, IOpNodeHolder {
		private int localIndex;
		private string localName;//just for the ToString()
		private OpNode enumerableNode;
		private OpNode blockNode;//can be null

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_Foreach constructed = new OpNode_Foreach(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);

			Production mainProd = (Production) code;
			Production headProd = GetHeaderCode(mainProd.GetChildAt(1));//FOREACH_HEADER_CODE or FOREACH_HEADER_IN_PARENS
			string localName = GetLocalName(headProd.GetChildAt(0));
			constructed.localName = localName;
			constructed.localIndex = constructed.ParentScriptHolder.GetLocalVarIndex(localName);
			constructed.enumerableNode = LScriptMain.CompileNode(constructed, headProd.GetChildAt(2), true);
			if (mainProd.GetChildCount() == 6) {//has the Script node inside?
				constructed.blockNode = LScriptMain.CompileNode(constructed, mainProd.GetChildAt(3), true);
			}
			return constructed;
		}

		private static string GetLocalName(Node node) {
			Token asToken = node as Token;
			if (asToken != null) {
				return asToken.GetImage().Trim();
			}
			return ((Token) node.GetChildAt(2)).GetImage().Trim();
		}

		private static Production GetHeaderCode(Node node)
		{
			if (IsType(node, StrictConstants.FOREACH_HEADER_CODE)) {
				return (Production) node;
			}
			if (IsType(node, StrictConstants.FOREACH_HEADER_IN_PARENS)) {
				return GetHeaderCode(node.GetChildAt(1));
			}
			throw new SEException("Unexpected node. This should not happen.");
		}

		private OpNode_Foreach(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (this.enumerableNode == oldNode) {
				this.enumerableNode = newNode;
				return;
			}
			if (this.blockNode == oldNode) {
				this.blockNode = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars)
		{
			if (this.blockNode != null) {
				object retVal = null;
				IEnumerable enumerable = this.enumerableNode.Run(vars) as IEnumerable;
				if (enumerable != null) {
					IEnumerator enumerator = enumerable.GetEnumerator();
					while ((!vars.returned) && (enumerator.MoveNext())) {
						vars.localVars[this.localIndex] = enumerator.Current;
						retVal = this.blockNode.Run(vars);
					}
				} else {
					throw new InterpreterException("Result of the expression '" + LogStr.Ident(this.enumerableNode) + "' is not an IEnumerable instance",
						this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
				}
				return retVal;
			}
			return null;//if there is no code to run, we dont do anything.
		}

		public override string ToString() {
			return string.Concat("Foreach (", this.localName, " in ", this.enumerableNode, ")",
				Environment.NewLine, this.blockNode, Environment.NewLine, "endforeach");
		}
	}
}