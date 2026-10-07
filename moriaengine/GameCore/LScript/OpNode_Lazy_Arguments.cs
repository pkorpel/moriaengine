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
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal abstract class OpNode_Argument : OpNode, IOpNodeHolder {
		internal OpNode arg;
		internal OpNode nodeIndex;
		internal int intIndex;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int curLine = code.GetStartLine() + LScriptMain.startLine;
			int curCol = code.GetStartColumn();
			string filename = LScriptMain.GetParentScriptHolder(parent).filename;

			//LScript.DisplayTree(code);

			OpNode indexOpNode = null;
			Node indexNode = null;
			int constantIndex = 0;
			int current = 1;//the child index

			Node firstNode = code;

			if (IsType(code, StrictConstants.ARGUMENT)) {
				firstNode = code.GetChildAt(0);
			}
			if (IsType(firstNode, StrictConstants.ARGV)) {
				indexNode = code.GetChildAt(2);
				indexOpNode = LScriptMain.CompileNode(parent, indexNode);//the parent here is false, it will be set to the correct one soon tho. This is for filename resolving and stuff.
				current = 4;
			} else if (HasNumberAttached(firstNode)) {
				constantIndex = GetIndex(LScriptMain.GetString(firstNode).Trim());
			} else {//single word: argo 0, argn 1, argtxt 3, argchk 4, argnum 5
				switch ((StrictConstants) firstNode.GetId()) {
					case StrictConstants.ARGO:
						constantIndex = 0; break;
					case StrictConstants.ARGN:
						constantIndex = 1; break;
					case StrictConstants.ARGTXT:
						constantIndex = 3; break;
					case StrictConstants.ARGCHK:
						constantIndex = 4; break;
					case StrictConstants.ARGNUM:
						constantIndex = 5; break;
					default:
						throw new SEBugException("This should not happen");
				}
			}

			List<OpNode> indicesList = new List<OpNode>();
			OpNode_Lazy_Indexer lastIndex = null;
			while (IsType(code.GetChildAt(current), StrictConstants.INDEXER)) {
				Production indexprod = (Production) code.GetChildAt(current);
				OpNode index = LScriptMain.CompileNode(parent, indexprod.GetChildAt(1));//the parent here is false, it will be set to the correct one soon tho. This is for filename resolving and stuff.
				lastIndex = OpNode_Lazy_Indexer.Construct(parent, indexprod.GetChildAt(1), index, null);
				indicesList.Add(lastIndex);
				current++;
			}
			current++;
			OpNode argOpNode = null;
			Node argNode = code.GetChildAt(current);
			if (argNode != null) {
				if (lastIndex == null) {
					argOpNode = LScriptMain.CompileNode(parent, argNode);//the parent here is false, it will be set to the correct one soon tho. This is for filename resolving and stuff.
				} else if (code.GetChildAt(current) != null) {
					lastIndex.arg = LScriptMain.CompileNode(lastIndex, argNode);
				}
			}

			OpNode_Object constIndexOpNode = indexOpNode as OpNode_Object;
			if (constIndexOpNode != null) { //we check if the indexnode isn't by chance a literal constant
				int retVal;
				if (ConvertTools.TryConvertToInt32(constIndexOpNode.obj, out retVal)) {
					constantIndex = Convert.ToInt32(retVal);
					indexOpNode = null;
				} else {
					Logger.WriteWarning(filename, indexNode.GetStartLine() + LScriptMain.startLine, "ARGV needs a number as it's indexer, '" + constIndexOpNode.obj + "' is not a number.");
				}
			}

			OpNode newNode;
			if (argOpNode == null) {//we construct an arg getter
				if (indexOpNode == null) {//constant index
					newNode = new OpNode_GetArgv_Constant(parent, filename, curLine, curCol, code, constantIndex);
				} else {
					newNode = new OpNode_GetArgv(parent, filename, curLine, curCol, code, indexOpNode);
				}
			} else {//we construct arg setter
				if (indexOpNode == null) {//constant index
					newNode = new OpNode_SetArgv_Constant(parent, filename, curLine, curCol, code, constantIndex, argOpNode);
				} else {
					newNode = new OpNode_SetArgv(parent, filename, curLine, curCol, code, indexOpNode, argOpNode);
				}
			}

			if (indicesList.Count > 0) {
				indicesList.Insert(0, newNode);
				OpNode[] chain = indicesList.ToArray();
				return OpNode_Lazy_ExpressionChain.ConstructFromArray(parent, code, chain);
			}
			return newNode;
		}

		internal OpNode_Argument(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		private static int GetIndex(string name) {
			//if (string.Compare(name, "argo", StringComparison.OrdinalIgnoreCase) == 0) {
			//    return 0;
			//} else if (string.Compare(name, "argn", StringComparison.OrdinalIgnoreCase) == 0) {
			//    return 1;
			//}
			string intStr = name.Substring(4);
			return int.Parse(intStr, NumberStyles.Integer, CultureInfo.InvariantCulture);
		}

		private static bool HasNumberAttached(Node code) {
			return ((IsType(code, StrictConstants.ARGNN)) ||
				(IsType(code, StrictConstants.ARGVN)) || (IsType(code, StrictConstants.ARGON)));
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.nodeIndex == oldNode) {
				this.nodeIndex = newNode;
			} else if (this.arg == oldNode) {
				this.arg = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}
	}
}