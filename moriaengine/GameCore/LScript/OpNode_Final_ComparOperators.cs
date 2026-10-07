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
	internal class OpNode_EqualsOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualsOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			return Equals(this.left.Run(vars), this.right.Run(vars));
		}

		public object TryRun(ScriptVars vars, object[] results) {
			return Equals(results[0], results[1]);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_EqualsNotOperator : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualsNotOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			return !Equals(this.left.Run(vars), this.right.Run(vars));
		}

		public object TryRun(ScriptVars vars, object[] results) {
			return !Equals(results[0], results[1]);
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_EqualityOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualityOperator_Double(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar, CultureInfo.InvariantCulture) == Convert.ToDouble(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) == Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_EqualityOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_EqualityOperator_Int(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar, CultureInfo.InvariantCulture) == Convert.ToInt64(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) == Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating == operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_InEqualityOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_InEqualityOperator_Double(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar, CultureInfo.InvariantCulture) != Convert.ToDouble(rightVar, CultureInfo.InvariantCulture));
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) != Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_InEqualityOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_InEqualityOperator_Int(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar, CultureInfo.InvariantCulture) != Convert.ToInt64(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) != Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating != operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_LessThanOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOperator_Double(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar, CultureInfo.InvariantCulture) < Convert.ToDouble(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) < Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GreaterThanOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOperator_Double(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar, CultureInfo.InvariantCulture) > Convert.ToDouble(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) > Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_LessThanOrEqualOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOrEqualOperator_Double(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar, CultureInfo.InvariantCulture) <= Convert.ToDouble(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) <= Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GreaterThanOrEqualOperator_Double : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOrEqualOperator_Double(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToDouble(leftVar, CultureInfo.InvariantCulture) >= Convert.ToDouble(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToDouble(results[0], CultureInfo.InvariantCulture) >= Convert.ToDouble(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_LessThanOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOperator_Int(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar, CultureInfo.InvariantCulture) < Convert.ToInt64(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) < Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating < operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GreaterThanOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOperator_Int(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar, CultureInfo.InvariantCulture) > Convert.ToInt64(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) > Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating > operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_LessThanOrEqualOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_LessThanOrEqualOperator_Int(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar, CultureInfo.InvariantCulture) <= Convert.ToInt64(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) <= Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating <= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GreaterThanOrEqualOperator_Int : OpNode_Lazy_BinOperator, ITriable {
		internal OpNode_GreaterThanOrEqualOperator_Int(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			object rightVar = this.right.Run(vars);
			try {
				return (Convert.ToInt64(leftVar, CultureInfo.InvariantCulture) >= Convert.ToInt64(rightVar, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return (Convert.ToInt64(results[0], CultureInfo.InvariantCulture) >= Convert.ToInt64(results[1], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating >= operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
}


