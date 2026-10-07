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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_While : OpNode, IOpNodeHolder {
		//accepts
		private OpNode condition;
		private OpNode code;

		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			OpNode_While constructed = new OpNode_While(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);

			Production whileProduction = (Production) code;
			Node conditionNode = whileProduction.GetChildAt(1);
			constructed.condition = LScriptMain.CompileNode(constructed, conditionNode, true);

			Node codeNode = whileProduction.GetChildAt(3);
			if (!IsType(conditionNode, StrictConstants.ENDWHILE)) {
				constructed.code = LScriptMain.CompileNode(constructed, codeNode, true);
			}
			return constructed;
		}

		protected OpNode_While(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (this.condition == oldNode) {
				this.condition = newNode;
			} else if (this.code == oldNode) {
				this.code = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			object retVal = null;
			if (this.code == null) {
				while ((!vars.returned) && (ConvertTools.ToBoolean(this.condition.Run(vars)))) {
					//empty
				}
			} else {
				while ((!vars.returned) && (ConvertTools.ToBoolean(this.condition.Run(vars)))) {
					retVal = this.code.Run(vars);
				}
			}
			return retVal;
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("While (");
			str.Append(this.condition).Append(")").Append(Environment.NewLine);
			if (this.code != null) {
				str.Append(this.code);
			}
			str.Append("Endwhile");
			return str.ToString();
		}
	}
}