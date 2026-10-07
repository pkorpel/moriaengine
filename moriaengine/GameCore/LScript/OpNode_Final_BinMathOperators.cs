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
	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_AddOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_AddOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal, CultureInfo.InvariantCulture) + Convert.ToDouble(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating + operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) + Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating + operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SubOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_SubOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal, CultureInfo.InvariantCulture) - Convert.ToDouble(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating - operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) - Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating - operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_MulOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_MulOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal, CultureInfo.InvariantCulture) * Convert.ToDouble(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating * operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) * Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating * operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_DivOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_DivOperator_Double(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal, CultureInfo.InvariantCulture) / Convert.ToDouble(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) / Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_DivOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_DivOperator_Int(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVal, CultureInfo.InvariantCulture) / Convert.ToInt64(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) / Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating / operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ModOperator : OpNode_Lazy_BinOperator, ITriable {// %
		internal OpNode_ModOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVal, CultureInfo.InvariantCulture) % Convert.ToDouble(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating % operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) % Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating % operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_BinaryAndOperator : OpNode_Lazy_BinOperator, ITriable {// & as binary operator
		internal OpNode_BinaryAndOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVal, CultureInfo.InvariantCulture) & Convert.ToInt64(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating & operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) & Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating & operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_BinaryOrOperator : OpNode_Lazy_BinOperator, ITriable {// & as binary operator
		internal OpNode_BinaryOrOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVal = this.left.Run(vars);
			object rightVal = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVal, CultureInfo.InvariantCulture) | Convert.ToInt64(rightVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating | operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) | Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating | operator",
				this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
}