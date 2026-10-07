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

using System.Diagnostics.CodeAnalysis;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SetArg : OpNode, IOpNodeHolder, ITriable {
		private readonly int registerIndex;
		private readonly string name;
		private OpNode arg;

		internal OpNode_SetArg(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name, OpNode arg)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			this.name = name;
			this.registerIndex = this.ParentScriptHolder.GetLocalVarIndex(name);
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.arg == oldNode) {
				this.arg = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			try {
				vars.localVars[this.registerIndex] = this.arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			return null;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			vars.localVars[this.registerIndex] = results[0];
			return null;
		}

		public override string ToString() {
			return string.Concat("ARG ", this.name, " = ", this.arg);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GetArg : OpNode, ITriable {
		private readonly int registerIndex;
		private readonly string name;

		internal OpNode_GetArg(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.registerIndex = this.ParentScriptHolder.GetLocalVarIndex(name);
			this.name = name;
		}

		internal override object Run(ScriptVars vars) {
			return vars.localVars[this.registerIndex];
		}

		public object TryRun(ScriptVars vars, object[] results) {
			return vars.localVars[this.registerIndex];
		}

		public override string ToString() {
			return string.Concat("ARG(", this.name, ")");
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ArgExists : OpNode, ITriable {
		private readonly int registerIndex;
		private readonly string name;

		internal OpNode_ArgExists(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.registerIndex = this.ParentScriptHolder.GetLocalVarIndex(name);
			this.name = name;
		}

		internal override object Run(ScriptVars vars) {
			return vars.localVars[this.registerIndex] == null;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			return vars.localVars[this.registerIndex] == null;
		}

		public override string ToString() {
			return string.Concat("ARG.EXISTS(", this.name, ")");
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_RemoveArg : OpNode, ITriable {
		private readonly int registerIndex;
		private readonly string name;

		internal OpNode_RemoveArg(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.registerIndex = this.ParentScriptHolder.GetLocalVarIndex(name);
			this.name = name;
		}

		internal override object Run(ScriptVars vars) {
			vars.localVars[this.registerIndex] = null;
			return null;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			vars.localVars[this.registerIndex] = null;
			return null;
		}

		public override string ToString() {
			return string.Concat("ARG.remove(", this.name, ")");
		}
	}
}