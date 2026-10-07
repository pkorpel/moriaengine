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
using SteamEngine.Timers;

namespace SteamEngine.LScript {
	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_AddTriggerTimer : OpNode_Lazy_AddTimer {
		private readonly TriggerKey triggerKey;

		internal OpNode_AddTriggerTimer(IOpNodeHolder parent, string filename,
					int line, int column, Node origNode, TriggerKey triggerKey)
			: base(parent, filename, line, column, origNode) {
			this.triggerKey = triggerKey;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int argsCount = this.args.Length;
			object[] results = new object[argsCount];
			object secondsVal;
			try {
				secondsVal = this.secondsNode.Run(vars);
				for (int i = 0; i < argsCount; i++) {
					results[i] = this.args[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			try {
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				TriggerTimer timer = new TriggerTimer(this.triggerKey, this.formatString, results);
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.name, timer);
				return timer;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding TriggerTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("AddTimer(");
			sb.Append("(").Append(this.name.Name).Append(", ").Append(this.secondsNode);
			sb.Append("@").Append(this.triggerKey.Name).Append(", ");
			int n = this.args.Length;
			if (n > 0) {
				sb.Append(", ");
				for (int i = 0; i < n; i++) {
					sb.Append(this.args[i]).Append(", ");
				}
			}
			return sb.Append(")").ToString();
		}
	}

	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_AddMethodTimer : OpNode, ITriable, IOpNodeHolder {
		private readonly TimerKey timerKey;
		internal readonly MethodInfo method;
		private OpNode secondsNode;
		private readonly OpNode[] args;

		internal OpNode_AddMethodTimer(IOpNodeHolder parent, string filename, int line, int column, Node origNode,
				TimerKey timerKey, MethodInfo method, OpNode secondsNode, params OpNode[] args)
			: base(parent, filename, line, column, origNode) {
			this.timerKey = timerKey;
			this.method = method;
			this.args = args;
			this.secondsNode = secondsNode;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int argsCount = this.args.Length;
			object[] results = new object[argsCount];
			object secondsVal;
			try {
				for (int i = 0; i < argsCount; i++) {
					results[i] = this.args[i].Run(vars);
				}
				secondsVal = this.secondsNode.Run(vars);
			} finally {
				vars.self = oSelf;
			}

			try {
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				MethodTimer timer = new MethodTimer(this.method, results);
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding MethodTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object secondsVal;
			try {
				secondsVal = this.secondsNode.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				MethodTimer timer = new MethodTimer(this.method, results);
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding MethodTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index >= 0) {
				this.args[index] = newNode;
			} else if (this.secondsNode == oldNode) {
				this.secondsNode = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("AddTimer(");
			sb.Append("(").Append(this.timerKey.Name).Append(", ").Append(this.secondsNode);
			sb.Append(this.method.Name).Append(", ");
			int n = this.args.Length;
			if (n > 0) {
				sb.Append(", ");
				for (int i = 0; i < n; i++) {
					sb.Append(this.args[i]).Append(", ");
				}
			}
			return sb.Append(")").ToString();
		}
	}


	//"string" version... concatenates all it's arguments into one string.
	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_AddMethodTimer_String : OpNode, ITriable, IOpNodeHolder {
		private readonly TimerKey timerKey;
		internal readonly MethodInfo method;
		private OpNode secondsNode;
		private readonly OpNode[] args;
		private readonly string formatString;

		internal OpNode_AddMethodTimer_String(IOpNodeHolder parent, string filename, int line, int column, Node origNode,
				TimerKey timerKey, MethodInfo method, OpNode secondsNode, OpNode[] args, string formatString)
			: base(parent, filename, line, column, origNode) {
			this.timerKey = timerKey;
			this.method = method;
			this.args = args;
			this.secondsNode = secondsNode;
			this.formatString = formatString;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int argsCount = this.args.Length;
			object[] results = new object[argsCount];
			object secondsVal;
			try {
				for (int i = 0; i < argsCount; i++) {
					results[i] = this.args[i].Run(vars);
				}
				secondsVal = this.secondsNode.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			string resultString = string.Format(CultureInfo.InvariantCulture, this.formatString, results);

			try {
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				MethodTimer timer = new MethodTimer(this.method, new object[] { resultString });
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding MethodTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object secondsVal;
			try {
				secondsVal = this.secondsNode.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				string resultString = string.Format(CultureInfo.InvariantCulture, this.formatString, results);
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				MethodTimer timer = new MethodTimer(this.method, new object[] { resultString });
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding MethodTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index >= 0) {
				this.args[index] = newNode;
			} else if (this.secondsNode == oldNode) {
				this.secondsNode = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("AddTimer_String(");
			sb.Append("(").Append(this.timerKey.Name).Append(", ").Append(this.secondsNode);
			sb.Append(this.method.Name).Append(", ");
			int n = this.args.Length;
			if (n > 0) {
				sb.Append(", ");
				for (int i = 0; i < n; i++) {
					sb.Append(this.args[i]).Append(", ");
				}
			}
			return sb.Append(")").ToString();
		}
	}

	//"params" version: to handle methods with params argument
	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_AddMethodTimer_Params : OpNode, ITriable, IOpNodeHolder {
		private readonly TimerKey timerKey;
		internal readonly MethodInfo method;
		private OpNode secondsNode;
		private readonly OpNode[] normalArgs;
		private readonly OpNode[] paramArgs;
		private readonly Type paramsElementType;

		internal OpNode_AddMethodTimer_Params(IOpNodeHolder parent, string filename, int line, int column, Node origNode,
				TimerKey timerKey, MethodInfo method, OpNode secondsNode, OpNode[] normalArgs, OpNode[] paramArgs, Type paramsElementType)
			: base(parent, filename, line, column, origNode) {
			this.timerKey = timerKey;
			this.method = method;
			this.normalArgs = normalArgs;
			this.paramArgs = paramArgs;
			this.paramsElementType = paramsElementType;
			this.secondsNode = secondsNode;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			int normalArgsLength = this.normalArgs.Length;
			object[] results = new object[normalArgsLength + 1];
			object secondsVal;
			try {
				for (int i = 0; i < normalArgsLength; i++) {
					results[i] = this.normalArgs[i].Run(vars);
				}
				int paramArrayLength = this.paramArgs.Length;
				Array paramArray = Array.CreateInstance(this.paramsElementType, paramArrayLength);
				for (int i = 0; i < paramArrayLength; i++) {
					paramArray.SetValue(this.paramArgs[i].Run(vars), i);
				}
				results[normalArgsLength] = paramArray;
				secondsVal = this.secondsNode.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				MethodTimer timer = new MethodTimer(this.method, results);
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding MethodTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object secondsVal;
			try {
				secondsVal = this.secondsNode.Run(vars);
			} finally {
				vars.self = oSelf;
			}

			int normalArgsLength = this.normalArgs.Length;
			object[] modifiedResults = new object[normalArgsLength + 1];
			Array.Copy(results, modifiedResults, normalArgsLength);
			try {
				//Console.WriteLine("results[0].GetType(): "+results[0]);
				int paramArrayLength = this.paramArgs.Length;
				Array paramArray = Array.CreateInstance(this.paramsElementType, paramArrayLength);
				Array.Copy(results, normalArgsLength, paramArray, 0, paramArrayLength);
				modifiedResults[normalArgsLength] = paramArray;

				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				MethodTimer timer = new MethodTimer(this.method, modifiedResults);
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding MethodTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
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

		public override string ToString() {
			StringBuilder sb = new StringBuilder("AddTimer_Params(");
			sb.Append("(").Append(this.timerKey.Name).Append(", ").Append(this.secondsNode);
			sb.Append(this.method.Name).Append(", ");
			for (int i = 0, n = this.normalArgs.Length; i < n; i++) {
				sb.Append(this.normalArgs[i]).Append(", ");
			}
			sb.Append(Tools.ObjToString(this.paramArgs));

			return sb.Append(")").ToString();
		}
	}


	[SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
	internal class OpNode_AddFunctionTimer : OpNode, ITriable, IOpNodeHolder {
		private readonly TimerKey timerKey;
		private readonly ScriptHolder function;
		private OpNode secondsNode;
		private readonly OpNode[] args;
		private readonly string formatString;
		private int argsCount;

		internal OpNode_AddFunctionTimer(IOpNodeHolder parent, string filename, int line, int column, Node origNode,
				TimerKey timerKey, ScriptHolder function, string formatString, OpNode secondsNode, params OpNode[] args)
			: base(parent, filename, line, column, origNode) {
			this.timerKey = timerKey;
			this.function = function;
			this.formatString = formatString;
			this.args = args;
			this.secondsNode = secondsNode;
			this.argsCount = args.Length;
		}

		internal override object Run(ScriptVars vars) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object[] results = new object[this.argsCount];
			object secondsVal;
			try {
				secondsVal = this.secondsNode.Run(vars);
				for (int i = 0; i < this.argsCount; i++) {
					results[i] = this.args[i].Run(vars);
				}
			} finally {
				vars.self = oSelf;
			}
			try {
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				FunctionTimer timer = new FunctionTimer(this.function, this.formatString, results);
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding FunctionTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public object TryRun(ScriptVars vars, object[] results) {
			object oSelf = vars.self;
			vars.self = vars.defaultObject;
			object secondsVal;
			try {
				secondsVal = this.secondsNode.Run(vars);
			} finally {
				vars.self = oSelf;
			}
			try {
				double seconds = Convert.ToDouble(secondsVal, CultureInfo.InvariantCulture);
				FunctionTimer timer = new FunctionTimer(this.function, this.formatString, results);
				timer.DueInSeconds = seconds;
				((PluginHolder) vars.self).AddTimer(this.timerKey, timer);
				return timer;
			} catch (InterpreterException) {
				throw;
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				throw new InterpreterException("Exception while adding FunctionTimer",
					this.line, this.column, this.filename, this.ParentScriptHolder.GetDecoratedName(), e);
			}
		}

		public virtual void Replace(OpNode oldNode, OpNode newNode) {
			int index = Array.IndexOf(this.args, oldNode);
			if (index >= 0) {
				this.args[index] = newNode;
			} else if (this.secondsNode == oldNode) {
				this.secondsNode = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("AddTimer(");
			sb.Append("(").Append(this.timerKey.Name).Append(", ").Append(this.secondsNode);
			sb.Append(this.function.Name).Append(", ");
			int n = this.args.Length;
			if (n > 0) {
				sb.Append(", ");
				for (int i = 0; i < n; i++) {
					sb.Append(this.args[i]).Append(", ");
				}
			}
			return sb.Append(")").ToString();
		}
	}
}