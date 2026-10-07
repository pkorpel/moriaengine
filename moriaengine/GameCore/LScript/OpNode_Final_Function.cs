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

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_Function : OpNode, IOpNodeHolder, ITriable {
		private readonly ScriptHolder function;
		private readonly OpNode[] args;
		private readonly string formatString;
		private readonly int argsCount;

		internal OpNode_Function(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ScriptHolder function, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.function = function;
			this.formatString = formatString;
			this.argsCount = args.Length;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.args[index] = newNode;
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object[] results = new object[this.argsCount];

			try {
				for (int i = 0; i < this.argsCount; i++) {
					results[i] = this.args[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			ScriptArgs sa = new ScriptArgs(results);
			sa.FormatString = this.formatString;
			try {
				return this.function.Run(oSelf, sa); //I found here being RunAndCatch... why, omg?? can't remember myself :\ -tar
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			ScriptArgs sa = new ScriptArgs(results);
			sa.FormatString = this.formatString;
			return this.function.TryRun(vars.self, sa);
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.AppendFormat("function {0}(", this.function.Name);
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append("))").ToString();
		}
	}
}