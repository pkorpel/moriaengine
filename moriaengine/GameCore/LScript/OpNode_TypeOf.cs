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
using SteamEngine.CompiledScripts;

namespace SteamEngine.LScript {

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Is : OpNode, IOpNodeHolder {
		private Type type;
		internal OpNode opNode;

		internal static OpNode_Is Construct(IOpNodeHolder parent, Node code, int typeNameFromIndex) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			string filename = LScriptMain.GetParentScriptHolder(parent).filename;

			OpNode_Is constructed = new OpNode_Is(
				parent, LScriptMain.GetParentScriptHolder(parent).filename, line, column, code);

			//LScript.DisplayTree(code);

			StringBuilder sb = new StringBuilder();
			for (int i = typeNameFromIndex, n = code.GetChildCount(); i < n; i++) {
				Node node = code.GetChildAt(i);
				sb.Append(((Token) node).GetImage().Trim());
			}
			string typeName = sb.ToString();

			Type type = ClassManager.GetType(typeName);
			if (type == null) {
				type = Type.GetType(typeName, false, true);
			}
			if (type == null) {
				throw new InterpreterException("Type '" + typeName + "' not recognised.",
					line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
			}

			constructed.type = type;

			return constructed;
		}

		protected OpNode_Is(IOpNodeHolder parent, string filename, int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {

		}

		public void Replace(OpNode oldNode, OpNode newNode) {
			if (oldNode == this.opNode) {
				this.opNode = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			object obj = this.opNode.Run(vars);

			return this.type.IsInstanceOfType(obj);
		}

		public override string ToString() {
			return string.Concat(this.opNode.ToString(), " IS ", this.type.Name);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	public static class OpNode_Typeof {
		internal static OpNode Construct(IOpNodeHolder parent, Node code) {
			int line = code.GetStartLine() + LScriptMain.startLine;
			int column = code.GetStartColumn();
			string filename = LScriptMain.GetParentScriptHolder(parent).filename;

			int n = code.GetChildCount();
			if (OpNode.IsType(code.GetChildAt(1), StrictConstants.LEFT_PAREN)) {
				n--;
			}

			StringBuilder sb = new StringBuilder();
			for (int i = 2; i < n; i++) {
				Node node = code.GetChildAt(i);
				sb.Append(((Token) node).GetImage().Trim());
			}
			string typeName = sb.ToString();
			Type type = TryRecognizeType(typeName);
			if (type == null) {
				type = TryRecognizeType(typeName + "`1"); //this is how generic types are internally named
			}
			if (type == null) {
				throw new InterpreterException("Type '" + typeName + "' not recognised.",
					line, column, filename, LScriptMain.GetParentScriptHolder(parent).GetDecoratedName());
			}

			return OpNode_Object.Construct(parent, type);
		}

		private static Type TryRecognizeType(string typeName) {
			Type type = ClassManager.GetType(typeName);
			if (type == null) {
				type = Type.GetType(typeName, false, true);
			}
			return type;
		}
	}


}