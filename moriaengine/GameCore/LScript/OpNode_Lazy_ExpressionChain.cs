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
using System.Text;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Lazy_ExpressionChain : OpNode, IOpNodeHolder {
		//accepts DottedExpressionChain
		private OpNode[] chain; //expression1.expression2.exp....

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_Lazy_ExpressionChain constructed = new OpNode_Lazy_ExpressionChain(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			OpNode_Is opnodeIs = null;

			List<OpNode> argsList = new List<OpNode>();
			for (int i = 0, n = code.GetChildCount(); i < n; i += 2) {//step is 2, we are skipping dots
				if (i > 0) {
					Node dotOrIsNode = code.GetChildAt(i - 1);//DOT or OP_IS
					if (IsType(dotOrIsNode, StrictConstants.OP_IS)) {
						opnodeIs = OpNode_Is.Construct(parent, code, i);
						break;
					}
				}

				Node node = code.GetChildAt(i);
				argsList.Add(LScriptMain.CompileNode(constructed, node, true));//mustEval always true
			}
			//in case that one of the members of the chain is also ExpressionChain (a new chain of indexers or something like that)...
			//in fact this wont happen in 99% cases, but we want perfectness :)
			List<OpNode> finalArgsList = new List<OpNode>();
			for (int i = 0, n = argsList.Count; i < n; i++) {
				OpNode node = argsList[i];
				OpNode_Lazy_ExpressionChain nodeAsExpChain = node as OpNode_Lazy_ExpressionChain;
				if (nodeAsExpChain != null) {					
					for (int ii = 0, nn = nodeAsExpChain.chain.Length; ii < nn; ii++) {
						finalArgsList.Add(nodeAsExpChain.chain[ii]);
					}
				} else {
					finalArgsList.Add(argsList[i]);
				}
			}
			OpNode[] array = finalArgsList.ToArray();
			constructed.chain = array;
			foreach (OpNode opNode in constructed.chain) {
				opNode.parent = constructed;
			}

			if (opnodeIs != null) {
				constructed.parent = opnodeIs;
				opnodeIs.opNode = constructed;
				return opnodeIs;
			}
			return constructed;
		}

		internal static OpNode ConstructFromArray(IOpNodeHolder parent, Node code, OpNode[] chain) {
			//the chain was already made by Lazy_Expression - chain of indexers
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_Lazy_ExpressionChain constructed = new OpNode_Lazy_ExpressionChain(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);
			constructed.chain = chain;
			foreach (OpNode opNode in chain) {
				opNode.parent = constructed;
			}
			return constructed;
		}

		protected OpNode_Lazy_ExpressionChain(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.chain, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.chain[index] = newNode;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			try {
				for (int i = 0; i < this.chain.Length; ) {
					try {
						vars.self = this.chain[i].Run(vars);
						i++;
					} catch (NameRefException nre) {
						//Console.WriteLine("caught NameRefException: "+nre.nameRef.name);
						//Console.WriteLine("chain before: "+Tools.ObjToString(chain));
						this.ChainExceptIndex(i);
						//Console.WriteLine("chain after: "+Tools.ObjToString(chain));
						vars.self = nre.nameRef;
					}
				}
				return vars.self;
			} finally {
				vars.self = oSelf;
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder();
			for (int i = 0, n = this.chain.Length; i < n; i++) {
				str.Append(this.chain[i]).Append(".");
			}
			return str.ToString();
		}

		private void ChainExceptIndex(int index) {
			OpNode[] newChain = new OpNode[this.chain.Length - 1];
			for (int i = 0, j = 0, n = this.chain.Length; i < n; i++) {
				if (i != index) {
					newChain[j] = this.chain[i];
					j++;
				}
			}
			this.chain = newChain;
		}

		internal int GetChildIndex(OpNode possibleChild) {
			return Array.IndexOf(this.chain, possibleChild);
		}
	}

	internal abstract class NameRef {
		internal string name;
		internal NameRef(string name) {
			this.name = name;
		}
	}

	internal class NameSpaceRef : NameRef {
		internal NameSpaceRef(string name) : base(name) { }
	}

	internal class ClassNameRef : NameRef {
		internal ClassNameRef(string name)
			: base(name) {
		}
	}
}