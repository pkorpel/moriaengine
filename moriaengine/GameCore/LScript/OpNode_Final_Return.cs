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
using System.Globalization;
using System.Text;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Return : OpNode, IOpNodeHolder {
		private OpNode arg;

		internal OpNode_Return(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, OpNode arg)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			//ParentScriptHolder.nodeToReturn = arg;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.arg != oldNode) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.arg = newNode;
			//ParentScriptHolder.nodeToReturn = newNode;
		}

		internal override object Run(ScriptVars vars) {
			object retVal;
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			try {
				retVal = this.arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			vars.returned = true;
			return retVal;
		}

		public override string ToString() {
			return "return(" + this.arg + ")";
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_Return_String : OpNode, IOpNodeHolder {
		private readonly OpNode[] args;
		private readonly string formatString;

		internal OpNode_Return_String(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.formatString = formatString;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index >= 0) {
				this.args[index] = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		internal override object Run(ScriptVars vars) {
			int argsCount = this.args.Length;
			object[] results = new object[argsCount];
			for (int i = 0; i < argsCount; i++) {
				results[i] = this.args[i].Run(vars);
			}
			string resultString = string.Format(CultureInfo.InvariantCulture, this.formatString, results);
			vars.returned = true;
			return resultString;
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.AppendFormat("return((");
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append(").TOSTRING())").ToString();
		}
	}
}