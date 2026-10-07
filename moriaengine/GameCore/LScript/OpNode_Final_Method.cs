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
	internal class OpNode_MethodWrapper : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly MethodInfo method;
		private readonly OpNode[] args;

		internal OpNode_MethodWrapper(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, MethodInfo method, params OpNode[] args)
			: base(parent, filename, line, column, origNode) {
			this.args = args;
			this.method = method;
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
				return this.method.Invoke(oSelf, results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling method '" + this.method.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		public object TryRun(ScriptVars vars, object[] results) {
			//Console.WriteLine("OpNode_MethodWrapper results: "+Tools.ObjToString(results));
			try {
				//Console.WriteLine("results[0].GetType(): "+results[0]);
				return this.method.Invoke(vars.self, results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling method '" + this.method.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.AppendFormat("{0} {1}.{2}(", this.method.ReturnType, this.method.DeclaringType, this.method.Name);
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append("))").ToString();
		}

		public Type ReturnType {
			get {
				return this.method.ReturnType;
			}
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_MethodWrapper_Params : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly MethodInfo method;
		private readonly OpNode[] normalArgs;
		private readonly OpNode[] paramArgs;
		private readonly Type paramsElementType;

		internal OpNode_MethodWrapper_Params(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, MethodInfo method, OpNode[] normalArgs, OpNode[] paramArgs, Type paramsElementType)
			: base(parent, filename, line, column, origNode) {
			this.method = method;
			this.normalArgs = normalArgs;
			this.paramArgs = paramArgs;
			this.paramsElementType = paramsElementType;
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
				return this.method.Invoke(oSelf, results);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling method '" + this.method.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		[SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails")]
		public object TryRun(ScriptVars vars, object[] results) {//the dont have the normalargs and paramsargs separated...
			int normalArgsLength = this.normalArgs.Length;
			object[] modifiedResults = new object[normalArgsLength + 1];
			Array.Copy(results, modifiedResults, normalArgsLength);
			try {
				//Console.WriteLine("results[0].GetType(): "+results[0]);
				int paramArrayLength = this.paramArgs.Length;
				Array paramArray = Array.CreateInstance(this.paramsElementType, paramArrayLength);
				for (int i = 0; i < paramArrayLength; i++) {
					paramArray.SetValue(ConvertTools.ConvertTo(this.paramsElementType, results[i + normalArgsLength]), i);
				}
				modifiedResults[normalArgsLength] = paramArray;
				return this.method.Invoke(vars.self, modifiedResults);
			} catch (InterpreterException ie) {
				ie.AddTrace(this);
				throw ie;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling method '" + this.method.Name + "'",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.AppendFormat("{0} {1}.{2}(", this.method.ReturnType, this.method.DeclaringType, this.method.Name);
			for (int i = 0, n = this.normalArgs.Length; i < n; i++) {
				str.Append(this.normalArgs[i]).Append(", ");
			}
			str.Append(Tools.ObjToString(this.paramArgs));
			return str.Append("))").ToString();
		}

		public Type ReturnType {
			get {
				return this.method.ReturnType;
			}
		}
	}

	//a method that has only one parameter, of string type, but typically more opnodes that have to construct the string
	//I'll use it once I get out of my lazyness and implement such thingies for Constructor and SetField
	//the reason is that Lazy_Expression is a bit weird right now, it keeps stringMethods/Constructors/Fields as separate, 
	//but doesnt have separated their arguments results... I dont think anyone understands that anyway (maybe except me :) -tar
	//it would also remove the GOTOs that are inside the resolveas* methods

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_MethodWrapper_String : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		internal readonly MethodInfo method;
		private readonly OpNode[] args;
		private readonly string formatString;

		internal OpNode_MethodWrapper_String(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, MethodInfo method, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.method = method;
			this.args = args;
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
				return this.method.Invoke(oSelf, new object[] { resultString });
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling method '" + this.method.Name + "'", this.line,
					this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			//Console.WriteLine("OpNode_MethodWrapper results: "+Tools.ObjToString(results));
			try {
				string resultString = string.Format(CultureInfo.InvariantCulture, this.formatString, results);
				return this.method.Invoke(vars.self, new object[] { resultString });
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while calling method '" + this.method.Name + "'", this.line,
					this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder str = new StringBuilder("(");
			str.AppendFormat("{0} {1}.{2}((", this.method.ReturnType, this.method.DeclaringType, this.method.Name);
			for (int i = 0, n = this.args.Length; i < n; i++) {
				str.Append(this.args[i]).Append(", ");
			}
			return str.Append(")).TOSTRING())").ToString();
		}

		public Type ReturnType {
			get {
				return this.method.ReturnType;
			}
		}
	}

	//a specialized opnode. not really necesarry to exist...
	[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores"), SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
	internal class OpNode_RunOnArgo : OpNode, IOpNodeHolder, ITriable, IKnownRetType {
		OpNode_MethodWrapper toRun;
		internal OpNode_RunOnArgo(IOpNodeHolder parent, string filename, int line, int column, Node origNode, OpNode_MethodWrapper toRun)
			: base(parent, filename, line, column, origNode) {
			this.toRun = toRun;
		}

		internal override object Run(ScriptVars vars) {
			object origSelf = vars.self;
			try {
				vars.self = vars.scriptArgs.Argv[0];
				return this.toRun.Run(vars);
			} finally {
				vars.self = origSelf;
			}
		}

		[SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.Exception.#ctor(System.String)")]
		public void Replace(OpNode oldNode, OpNode newNode)
		{
			if (this.toRun != oldNode) {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
			this.toRun = (OpNode_MethodWrapper) newNode;
		}

		public object TryRun(ScriptVars vars, object[] results) {
			object origSelf = vars.self;
			try {
				vars.self = vars.scriptArgs.Argv[0];
				return this.toRun.TryRun(vars, results);
			} finally {
				vars.self = origSelf;
			}
		}

		public override string ToString() {
			return "ARGO." + this.toRun;
		}

		public Type ReturnType {
			get {
				return this.toRun.ReturnType;
			}
		}
	}
}