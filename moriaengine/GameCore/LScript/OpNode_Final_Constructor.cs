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
using SteamEngine.Common;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ConstructorWrapper : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly ConstructorInfo ctor;
		private readonly OpNode[] args;

		internal OpNode_ConstructorWrapper(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ConstructorInfo ctor, OpNode[] args)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.ctor = ctor;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.args[index] = newNode;
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
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
				return this.ctor.Invoke(results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + Tools.TypeToString(this.ctor.DeclaringType) + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				return this.ctor.Invoke(results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + Tools.TypeToString(this.ctor.DeclaringType) + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(this.ctor.DeclaringType).Append(".ctor(");
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append("))").ToString();
		}

		public Type ReturnType {
			get {
				return this.ctor.DeclaringType;
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ConstructorWrapper_Params : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly ConstructorInfo ctor;
		private readonly OpNode[] normalArgs;
		private readonly OpNode[] paramArgs;
		private readonly Type paramsElementType;

		internal OpNode_ConstructorWrapper_Params(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ConstructorInfo ctor, OpNode[] normalArgs, OpNode[] paramArgs, Type paramsElementType)
			: base(parent, filename, line, column, origNode) {
			this.normalArgs = normalArgs;
			this.paramArgs = paramArgs;
			this.paramsElementType = paramsElementType;
			this.ctor = ctor;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.normalArgs, oldNode);
			if (index >= 0) {
				this.normalArgs[index] = newNode;
				return;
			}
			index = Array.IndexOf(this.paramArgs, oldNode);
			if (index >= 0) {
				this.paramArgs[index] = newNode;
				return;
			}
			throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int normalArgsLength = this.normalArgs.Length;
			object[] results = new object[normalArgsLength + 1];
			try {
				for (int i = 0; i < normalArgsLength; i++) {
					results[i] = this.normalArgs[i].Run(vars);
				}
				int paramArrayLength = this.paramArgs.Length;
				Array paramArray = Array.CreateInstance(this.paramsElementType, paramArrayLength);
				for (int i = 0; i < paramArrayLength; i++) {
					paramArray.SetValue(ConvertTools.ConvertTo(this.paramsElementType, this.paramArgs[i].Run(vars)), i);
				}
				results[normalArgsLength] = paramArray;
			} finally {
				vars.self = oSelf;
			}
			try {
				return this.ctor.Invoke(results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + Tools.TypeToString(this.ctor.DeclaringType) + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		public object TryRun(ScriptVars vars, object[] results) {
			int normalArgsLength = this.normalArgs.Length;
			object[] modifiedResults = new object[normalArgsLength + 1];
			Array.Copy(results, modifiedResults, normalArgsLength);
			try {
				int paramArrayLength = this.paramArgs.Length;
				Array paramArray = Array.CreateInstance(this.paramsElementType, paramArrayLength);
				for (int i = 0; i < paramArrayLength; i++) {
					paramArray.SetValue(ConvertTools.ConvertTo(this.paramsElementType, results[i + normalArgsLength]), i);
				}
				modifiedResults[normalArgsLength] = paramArray;
				return this.ctor.Invoke(modifiedResults);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + Tools.TypeToString(this.ctor.DeclaringType) + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(this.ctor.DeclaringType).Append(".ctor(");
			for (int i = 0, n = this.normalArgs.Length; i < n; i++) {
				str.Append(this.normalArgs[i]).Append(", ");
			}
			str.Append(Tools.ObjToString(this.paramArgs));
			return str.Append("))").ToString();
		}

		public Type ReturnType {
			get {
				return this.ctor.DeclaringType;
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_ConstructorWrapper_String : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly ConstructorInfo ctor;
		private readonly string formatString;
		private readonly OpNode[] args;

		internal OpNode_ConstructorWrapper_String(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, ConstructorInfo ctor, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.ctor = ctor;
			this.formatString = formatString;
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index < 0) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.args[index] = newNode;
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
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
				return this.ctor.Invoke(BindingFlags.Default, null, new object[] { resultString }, null);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + Tools.TypeToString(this.ctor.DeclaringType) + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		public object TryRun(ScriptVars vars, object[] results) {
			try {
				string resultString = string.Format(CultureInfo.InvariantCulture, this.formatString, results);
				return this.ctor.Invoke(new object[] { resultString });
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling constructor '" + Tools.TypeToString(this.ctor.DeclaringType) + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.Append(this.ctor.DeclaringType).Append(".ctor(");
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append(").TOSTRING())").ToString();
		}

		public Type ReturnType {
			get {
				return this.ctor.DeclaringType;
			}
		}
	}
}