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
using System.Collections.Generic;
using System.IO;
using SteamEngine.Common;

//using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	public class LScriptHolder : ScriptHolder, IOpNodeHolder, IUnloadable {
		internal string filename = "<default>";
		internal int line;
		internal OpNode code;
		private Dictionary<string, int> localsNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		
		//internal OpNode nodeToReturn;

		//used by TriggerGroup/GumpDef/templatedef/... loading, and LoadAsFunction here
		public LScriptHolder(TriggerSection input)
			: base(input.TriggerName) {
			this.Compile(input);
		}

		//a little hack for gump response triggers.
		//internal LScriptHolder(TriggerSection input, params string[] addedLocals) : base(input.triggerName) {
		//    //registerNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
		//    for (int i = 0, n = addedLocals.Length; i<n; i++) {
		//        GetRegisterIndex(addedLocals[i]);
		//    }
		//    Compile(input);
		//}

		internal LScriptHolder(string filename)
			: base("temporary")
		{
			this.filename = filename;
		}

		internal LScriptHolder()
			: base("temporary") {
		}

		void IOpNodeHolder.Replace(OpNode oldNode, OpNode newNode) {
			if (this.code == oldNode) {
				this.code = newNode;
			} else {
				throw new SEException("Nothing to replace the node " + oldNode + " at " + this + "  with. This should not happen.");
			}
		}

		internal int GetLocalVarIndex(string name) {
			int index;
			if (!this.localsNames.TryGetValue(name, out index)) {
				index = this.localsNames.Count;
				this.localsNames[name] = index;
			}
			return index;
		}

		internal bool ContainsLocalVarName(string name) {
			return this.localsNames.ContainsKey(name);
		}

		internal int LocalVarsCount {
			get {
				return this.localsNames.Count;
			}
		}

		internal bool containsRandom;
		public bool ContainsRandomExpression {
			get {
				return this.containsRandom;
			}
		}

		internal void Compile(TriggerSection input) {
			this.filename = input.Filename;
			this.line = input.StartLine;
			//if (registerNames == null) {//else it got already created by the constructor
			//	registerNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
			//}
			//Console.WriteLine("compiling text "+input.code);
			using (StringReader reader = new StringReader(input.Code.ToString())) {
				this.code = LScriptMain.TryCompile(this, reader, input.StartLine);
			}
			this.unloaded = (this.code == null);
			//Logger.WriteDebug("the code is: "+code);
		}

		public sealed override object Run(object self, ScriptArgs sa) {
			if (this.unloaded) {
				throw new UnloadedException("Function/trigger " + LogStr.Ident(this.Name) + " is unloaded, can not be run.");
			}
			this.lastRunSuccesful = false;
			try {
				ScriptVars sv = new ScriptVars(sa, self, this.localsNames.Count);
				object retVal = this.code.Run(sv);
				this.lastRunSuccesful = true;
				if (sv.returned) {
					return retVal;
				}
				return null;//we should not randomly return the last expression...
			} catch (Exception e) {
				this.lastRunException = e;
				throw;
			}
		}

		protected sealed override void Error(Exception e) {
			Logger.WriteError(this.filename, this.line, e);
		}

		public void Unload() {
			this.code = null;
			this.localsNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			this.unloaded = true;
		}

		public sealed override bool IsUnloaded {
			get {
				return this.unloaded;
			}
		}

		public sealed override string Description {
			get {
				return this.filename + "(L" + this.line + ")";
			}
		}

	}

	public class ScriptVars {
		internal ScriptArgs scriptArgs;
		internal object self;
		internal object[] localVars;
		internal bool returned;
		internal readonly object defaultObject;
		//internal readonly int uid;

		//private static int uids;

		internal ScriptVars(ScriptArgs scriptArgs, object self, int capacity) {
			this.scriptArgs = scriptArgs;
			this.self = self;
			//this.uid = uids++;
			this.defaultObject = self;
			this.localVars = new object[capacity];
			//this.returned = false;
		}
	}
}