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
using System.Reflection;
using System.Text;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_SetField : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly FieldInfo field;
		private OpNode arg;

		internal OpNode_SetField(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, FieldInfo field, OpNode arg)
			: base(parent, filename, line, column, origNode) {
			this.arg = arg;
			this.field = field;
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
			object result;
			try {
				result = this.arg.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				this.field.SetValue(oSelf, result);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + this.field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				this.field.SetValue(vars.self, results[0]);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + this.field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Format(CultureInfo.InvariantCulture, 
			"({0} {1}.{2} = {3})", this.field.FieldType, this.field.DeclaringType, this.field.Name, this.arg);
		}

		public Type ReturnType {
			get {
				return typeof(void);
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_GetField : OpNode, ITriable, IKnownRetType {
		private readonly FieldInfo field;

		internal OpNode_GetField(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, FieldInfo field)
			: base(parent, filename, line, column, origNode) {
			this.field = field;
		}

		internal override object Run(ScriptVars vars) {
			try {
				return this.field.GetValue(vars.self);
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting field '" + this.field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return this.field.GetValue(vars.self);
			} catch (Exception e) {
				throw new InterpreterException("Exception while getting field '" + this.field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			return string.Format(CultureInfo.InvariantCulture, 
				"({0} {1}.{2})", this.field.FieldType, this.field.DeclaringType, this.field.Name);
		}

		public Type ReturnType {
			get {
				return this.field.FieldType;
			}
		}
	}


	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_InitField_String : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly FieldInfo field;
		private OpNode[] args;
		private readonly string formatString;

		internal OpNode_InitField_String(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, FieldInfo field, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.field = field;
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
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int argsCount = this.args.Length;
			object[] results = new object[argsCount];
			try {
				for (int i = 0; i < argsCount; i++) {
					results[i] = this.args[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			try {
				string resultString = string.Format(CultureInfo.InvariantCulture, this.formatString, results);
				this.field.SetValue(oSelf, resultString);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + this.field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			try {
				string resultString = string.Format(CultureInfo.InvariantCulture, this.formatString, results);
				this.field.SetValue(vars.self, resultString);
				return null;
			} catch (Exception e) {
				throw new InterpreterException("Exception while setting field '" + this.field.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.AppendFormat("({0} {1}.{2} = (", this.field.FieldType, this.field.DeclaringType, this.field.Name);
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append(").TOSTRING())").ToString();
		}

		public Type ReturnType {
			get {
				return typeof(void);
			}
		}
	}
}