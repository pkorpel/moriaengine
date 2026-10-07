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
	internal class OpNode_SetTag : OpNode, IOpNodeHolder, ITriable {
		private readonly TagKey name;
		private OpNode arg;

		internal OpNode_SetTag(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name, OpNode arg)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			this.name = TagKey.Acquire(name);
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.arg == oldNode) {
				this.arg = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				object oSelf = vars.self;
				vars.self = vars.defaultObject;
				try {
					th.SetTag(this.name, this.arg.Run(vars));
				} finally {
					vars.self = oSelf;
				}
			} else {
				throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}
			return null;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				th.SetTag(this.name, results[0]);
			} else {
				throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}
			return null;
		}

		public override string ToString() {
			return string.Concat("TAG ", this.name, " = ", this.arg);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GetTag : OpNode, ITriable {
		private readonly TagKey name;

		internal OpNode_GetTag(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.name = TagKey.Acquire(name);
		}

		internal override object Run(ScriptVars vars) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				return th.GetTag(this.name);
			}
			throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
				this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
		}

		public object TryRun(ScriptVars vars, object[] results) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				return th.GetTag(this.name);
			}
			throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
				this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
		}

		public override string ToString() {
			return string.Concat("TAG(", this.name, ")");
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_RemoveTag : OpNode, ITriable {
		private readonly TagKey name;

		internal OpNode_RemoveTag(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.name = TagKey.Acquire(name);
		}

		internal override object Run(ScriptVars vars) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				th.RemoveTag(this.name);
			} else {
				throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}
			return null;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				th.RemoveTag(this.name);
			} else {
				throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
			}
			return null;
		}

		public override string ToString() {
			return string.Concat("TAG.remove(", this.name, ")");
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_TagExists : OpNode, ITriable {
		private readonly TagKey name;

		internal OpNode_TagExists(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, string name)
			: base(parent, filename, line, column, origNode) {
			this.name = TagKey.Acquire(name);
		}

		internal override object Run(ScriptVars vars) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				return th.HasTag(this.name);
			}
			throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
				this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
		}

		public object TryRun(ScriptVars vars, object[] results) {
			TagHolder th = vars.self as TagHolder;
			if (th != null) {
				return th.HasTag(this.name);
			}
			throw new InterpreterException("Tags can be used only on TagHolder and non-null objects.",
				this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName());
		}

		public override string ToString() {
			return string.Concat("TAG.EXISTS(", this.name, ")");
		}
	}
}