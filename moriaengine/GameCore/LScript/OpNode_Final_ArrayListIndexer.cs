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
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ArrayListIndex : OpNode, IOpNodeHolder {
		private OpNode arg;
		private OpNode index;

		internal OpNode_ArrayListIndex(IOpNodeHolder parent, string filename,
				int line, int column, Node origNode, OpNode arg, OpNode index)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			this.index = index;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.arg == oldNode) {
				this.arg = newNode;
			} else if (this.index == oldNode) {
				this.index = oldNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object indexVal;
			object argVal;
			try {
				indexVal = this.index.Run(vars);
				argVal = this.arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				ArrayList list = (ArrayList) oSelf;
				int resIndex = Convert.ToInt32(indexVal, CultureInfo.InvariantCulture);
				if (resIndex == list.Count) {
					list.Add(argVal);
				} else {
					while (list.Count <= resIndex) {
						list.Add(null);
					}
					list[resIndex] = argVal;
				}
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating [] operator",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		//public object TryRun(ScriptVars vars, object[] results) {
		//	ArrayList list = (ArrayList) vars.self;
		//	object oSelf = vars.self;
		//	vars.self = vars.defaultObject;
		//	int resIndex = Convert.ToInt32(results[0]);
		//	while (resIndex < list.Count) {
		//		list.Add(null);
		//	}
		//	list[resIndex] = results[1];
		//	vars.self = oSelf;
		//	return null;
		//}

		public override string ToString()
		{
			if (this.arg == null) {
				return string.Format(CultureInfo.InvariantCulture, 
					"(ArrayList)[{0}]", this.index);
			}
			return string.Format(CultureInfo.InvariantCulture, 
				"(ArrayList)[{0}] = {1}", this.index, this.arg);
		}
	}
}