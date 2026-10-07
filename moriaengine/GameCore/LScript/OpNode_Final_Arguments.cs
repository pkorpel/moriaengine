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
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GetArgv : OpNode_Argument {
		internal OpNode_GetArgv(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode, OpNode nodeIndex)
			: base(parent, filename, line, column, origNode) {
			this.nodeIndex = nodeIndex;
			nodeIndex.parent = this;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object indexVal;
			try {
				indexVal = this.nodeIndex.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				return vars.scriptArgs.Argv[Convert.ToInt32(indexVal, CultureInfo.InvariantCulture)];
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting ARG",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Concat("ARGV(", this.nodeIndex, ")");
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GetArgv_Constant : OpNode_Argument {
		internal OpNode_GetArgv_Constant(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode, int intIndex)
			: base(parent, filename, line, column, origNode) {

			this.intIndex = intIndex;
		}

		internal override object Run(ScriptVars vars) {
			try {
				return vars.scriptArgs.Argv[this.intIndex];
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting ARG",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Concat("ARGV", this.intIndex);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SetArgv : OpNode_Argument {
		internal OpNode_SetArgv(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode, OpNode nodeIndex, OpNode arg)
			: base(parent, filename, line, column, origNode) {

			this.nodeIndex = nodeIndex;
			nodeIndex.parent = this;
			this.arg = arg;
			arg.parent = this;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object indexVal;
			object argVal;
			try {
				indexVal = this.nodeIndex.Run(vars);
				argVal = this.arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				vars.scriptArgs.Argv[Convert.ToInt32(indexVal, CultureInfo.InvariantCulture)] = argVal;
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting ARG",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Concat("ARGV(", this.nodeIndex, ") = ", this.arg);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SetArgv_Constant : OpNode_Argument {
		internal OpNode_SetArgv_Constant(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode, int intIndex, OpNode arg)
			: base(parent, filename, line, column, origNode) {

			this.intIndex = intIndex;
			this.arg = arg;
			arg.parent = this;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object argVal;
			try {
				argVal = this.arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				vars.scriptArgs.Argv[this.intIndex] = argVal;
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting ARG",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Concat("ARGV", this.intIndex, " = ", this.arg);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GetArgs : OpNode {
		internal OpNode_GetArgs(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		internal override object Run(ScriptVars vars) {
			return vars.scriptArgs.Args;
		}

		public override string ToString() {
			return "ARGS";
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ArgvCount : OpNode {
		internal OpNode_ArgvCount(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode)
			: base(parent, filename, line, column, origNode) {
		}

		internal override object Run(ScriptVars vars) {
			return vars.scriptArgs.Argv.Length;
		}

		public override string ToString() {
			return "ARGVCOUNT";
		}
	}
}