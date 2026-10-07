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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	//this is actually just a static class, for convenience using the same constructing signature as the other, _real_ , OpNodes
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	public static class OpNode_Code {
		//accepts Code, SimpleCode

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			Production prod = (Production) code;
			return Construct(parent, prod.children);
		}

		private static OpNode Construct(IOpNodeHolder parent, ArrayList children) {
			if (children.Count == 1) {
				return LScriptMain.CompileNode(parent, (Node) children[0]);
			}

			int highestPriority = -1;
			int opIndex = -1;
			for (int i = 1, n = children.Count; i < n; i += 2) {//step is 2, we are looking just for the operators
				Node node = (Node) children[i];
				string op = LScriptMain.GetString(node).Trim().ToLowerInvariant();
				if (IsComparing(op)) {
					opIndex = i;
					break;
				}
				int priority = OperatorPriority(op);
				if (priority >= highestPriority) {
					opIndex = i;
					highestPriority = priority;
				}
			}
			if (opIndex == -1) {
				throw new SEException("No operator found... this should not happen!");
			}

			OpNode_Lazy_BinOperator constructed = null;
			Node operatorNode = (Node) children[opIndex];
			if (OpNode.IsType(operatorNode, StrictConstants.OP_AND)) {
				constructed = new OpNode_LogicalAnd(parent, operatorNode);
			} else if (OpNode.IsType(operatorNode, StrictConstants.OP_OR)) {
				constructed = new OpNode_LogicalOr(parent, operatorNode);
			} else {
				//Console.WriteLine("BinOperator type: "+operatorNode);
				constructed = OpNode_Lazy_BinOperator.Construct(parent, operatorNode);
			}

			ArrayList listLeft = children.GetRange(0, opIndex);
			ArrayList listRight = children.GetRange(opIndex + 1, children.Count - opIndex - 1);
			constructed.left = Construct(constructed, listLeft);
			constructed.right = Construct(constructed, listRight);

			return constructed;
		}

		internal static bool IsComparing(string op) {
			if (op.Length == 2) {
				if (op[1] == '=') {
					return true;
				}
			} else {
				char first = op[0];
				if ((first == '<') || (first == '>')) {
					return true;
				}
			}
			return false;
		}

		internal static int OperatorPriority(string op) {
			switch (op) {
				case "||":
					return 5;
				case "&&":
					return 5;
				case "+":
					return 4;
				case "-":
					return 3;
				case "*":
					return 2;
				case "/":
					return 2;
				case "div":
					return 2;
				case "%":
					return 2;
				case "&":
					return 1;
				case "~":
					return 1;
				case "|":
					return 1;
				default:
					throw new SEException("unknown operator '" + op + "'");
			}
		}
	}
}



