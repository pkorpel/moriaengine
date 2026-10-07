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

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Lazy_QuotedString : OpNode, IOpNodeHolder, IKnownRetType {
		private OpNode[] evals;
		//or OpNode_Lazy_EvalExpression (<...>) - these get later replaced, of course.
		private object[] results;
		private string formatString;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			ArrayList children = ((Production) code).children;
			object[] nodes = new object[children.Count - 2]; //minus first and last quote
			for (int i = 1, n = children.Count - 1; i < n; i++) {
				nodes[i - 1] = children[i];
			}
			return ConstructFromArray(parent, code, nodes);
		}

		//nw when I look at it, I'm quite sure this could be yet optimised :)
		internal static OpNode ConstructFromArray(IOpNodeHolder parent, Node code, object[] nodes) {
			//the nodes can be both OpNodes or Nodes (parser nodes), but nothing else
			bool isConstant = true;
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_Lazy_QuotedString constructed = new OpNode_Lazy_QuotedString(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			ArrayList nodesList = new ArrayList();
			for (int i = 0, n = nodes.Length; i < n; i++) {
				object nodeAsObject = nodes[i];
				if (nodeAsObject is OpNode) {
					nodesList.Add(nodeAsObject);
				} else {
					Node node = (Node) nodeAsObject;
					if ((IsType(node, StrictConstants.STRONG_EVAL_EXPRESSION))
							|| (IsType(node, StrictConstants.EVAL_EXPRESSION))) {
						nodesList.Add(LScriptMain.CompileNode(constructed, node));
						isConstant = false;
					} else {
						nodesList.Add(nodeAsObject);
					}
				}
			}

			StringBuilder formatBuf = new StringBuilder();
			ArrayList evalsList = new ArrayList();
			for (int i = 0, n = nodesList.Count; i < n; ) {
				if (nodesList[i] is OpNode_Lazy_EvalExpression) {
					isConstant = false;
					int curEval = evalsList.Add(nodesList[i]);
					formatBuf.Append("{").Append(curEval.ToString(CultureInfo.InvariantCulture)).Append("}"); //creates {x} in the formatstring
					i++;
				} else {
					while ((i < n) && (!(nodesList[i] is OpNode_Lazy_EvalExpression))) {
						if (nodesList[i] is OpNode) {
							formatBuf.Append(((OpNode) nodesList[i]).OrigString);
						} else {
							formatBuf.Append(LScriptMain.GetString((Node) nodesList[i]));
						}
						i++;
					}
				}
			}
			constructed.formatString = formatBuf.ToString();
			if (isConstant) {//a little shortcut
				return OpNode_Object.Construct(parent, constructed.formatString);
			}
			constructed.evals = new OpNode[evalsList.Count];
			evalsList.CopyTo(constructed.evals);
			constructed.results = new object[evalsList.Count];
			return constructed;
		}

		protected OpNode_Lazy_QuotedString(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.evals, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.evals[index] = newNode;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			try {
				for (int i = 0, n = this.evals.Length; i < n; i++) {
					this.results[i] = this.evals[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			return string.Format(CultureInfo.InvariantCulture, this.formatString, this.results);
		}

		public override string ToString() {
			object[] strings = new object[this.evals.Length];
			for (int i = 0, n = this.evals.Length; i < n; i++) {
				strings[i] = this.evals[i].ToString();
			}
			return string.Format(CultureInfo.InvariantCulture, 
				"\"" + this.formatString + "\"", strings);
		}

		public Type ReturnType {
			get {
				return typeof(string);
			}
		}
	}
}