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
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_NotOperator : OpNode_Lazy_UnOperator, ITriable { // !
		internal OpNode_NotOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object retVal = this.obj.Run(vars);
			try {
				return !(ConvertTools.ToBoolean(retVal));
			} catch (Exception e) {
				throw new InterpreterException("Expression while evaluating ! operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return !(ConvertTools.ToBoolean(results[0]));
			} catch (Exception e) {
				throw new InterpreterException("Expression while evaluating ! operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_BitComplementOperator : OpNode_Lazy_UnOperator, ITriable { // ~
		internal OpNode_BitComplementOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object retVal = this.obj.Run(vars);
			try {
				return ~(Convert.ToInt64(retVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Expression while evaluating ~ operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return ~(Convert.ToInt64(results[0], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Expression while evaluating ~ operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_MinusOperator : OpNode_Lazy_UnOperator, ITriable { // ~
		internal OpNode_MinusOperator(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object retVal = this.obj.Run(vars);
			try {
				return -(Convert.ToDouble(retVal, CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Expression while evaluating - operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return -(Convert.ToDouble(results[0], CultureInfo.InvariantCulture));
			} catch (Exception e) {
				throw new InterpreterException("Expression while evaluating - operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
}