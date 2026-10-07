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
using PerCederberg.Grammatica.Parser;
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_LogicalAnd : OpNode_Lazy_BinOperator {
		//gets created from OpNode
		internal OpNode_LogicalAnd(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			bool leftVarBool;

			try {
				leftVarBool = ConvertTools.ToBoolean(leftVar);
				if (!leftVarBool) {
					return false;
				}
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating && operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}

			object righVar = this.right.Run(vars);
			try {
				return ConvertTools.ToBoolean(righVar);
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating && operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_LogicalOr : OpNode_Lazy_BinOperator {
		//gets created from OpNode
		internal OpNode_LogicalOr(IOpNodeHolder parent, Node code)
			: base(parent, code) {
		}

		internal override object Run(ScriptVars vars) {
			object leftVar = this.left.Run(vars);
			bool leftVarBool;

			try {
				leftVarBool = ConvertTools.ToBoolean(leftVar);
				if (leftVarBool) {
					return true;
				}
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating || operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}

			object righVar = this.right.Run(vars);
			try {
				return ConvertTools.ToBoolean(righVar);
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating || operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}
	}
}