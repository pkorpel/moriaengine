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
	internal class OpNode_TemplateItem : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		private OpNode defNode;
		private OpNode amountNode;
		private readonly bool isnewbie;

		internal OpNode_TemplateItem(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, bool isnewbie, OpNode defNode, OpNode amountNode)
			: base(parent, filename, line, column, origNode) {
			this.isnewbie = isnewbie;
			this.defNode = defNode;
			this.amountNode = amountNode;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			if (this.defNode == oldNode) {
				this.defNode = newNode;
				return;
			}
			if (this.amountNode == oldNode) {
				this.amountNode = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		internal override object Run(ScriptVars vars) {
			Thing t = (Thing) vars.defaultObject;
			vars.self = t;

			IThingFactory tf;
			int amount;
			try {
				tf = (IThingFactory) this.defNode.Run(vars);
				amount = Convert.ToInt32(this.amountNode.Run(vars), CultureInfo.InvariantCulture);
			} finally {
				vars.self = t;
			}

			try {
				AbstractItem i = t.NewItem(tf, amount);
				vars.self = i;
				return i;
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating (TEMPLATE)ITEM expression",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		public object TryRun(ScriptVars vars, object[] results) {
			Thing t = (Thing) vars.defaultObject;
			vars.self = t;
			IThingFactory tf = (IThingFactory) results[0];
			int amount = 1;
			if (results.Length > 1) {
				amount = Convert.ToInt32(results[1], CultureInfo.InvariantCulture);
			}
			try {
				AbstractItem i = t.NewItem(tf, amount);
				vars.self = i;
				return i;
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while evaluating (TEMPLATE)ITEM expression",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return (this.isnewbie ? "ITEMNEWBIE" : "ITEM") + "(" + this.defNode + ", " + this.amountNode + ")";
		}

		public Type ReturnType {
			get {
				return typeof(AbstractItem);
			}
		}
	}
}