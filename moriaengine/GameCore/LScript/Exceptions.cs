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
using SteamEngine.Common;
//using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {
	internal class InterpreterException : SEException {
		private bool hasTrace;

		internal InterpreterException(string s) : base(s) { }
		internal InterpreterException(LogStr s) : base(s) { }

		internal InterpreterException(string s, int line, int col, string filename, string scriptHolderName)
			:
				base(s) {
			this.AddTrace(scriptHolderName, line, filename);
		}

		internal InterpreterException(LogStr s, int line, int col, string filename, string scriptHolderName)
			:
				base(s) {
			this.AddTrace(scriptHolderName, line, filename);
		}

		internal InterpreterException(string s, int line, int col, string filename, string scriptHolderName, Exception innerException)
			:
				base(s, innerException) {
			this.AddTrace(scriptHolderName, line, filename);
		}

		internal InterpreterException(LogStr s, int line, int col, string filename, string scriptHolderName, Exception innerException)
			:
				base(s, innerException) {
			this.AddTrace(scriptHolderName, line, filename);
		}

		private void AddTrace(string triggerName, int line, string fileName) {
			this.hasTrace = true;
			this.AppendNiceMessage(string.Concat(Environment.NewLine, "   at function/trigger ", 
				LogStr.Ident(triggerName), "\t", 
				LogStr.FileLine(fileName, line)));
		}

		internal void AddTrace(OpNode node) {
			this.hasTrace = true;
			this.AddTrace(node.ParentScriptHolder.GetDecoratedName(), node.line, node.filename);
		}

		public override void TryAddFileLineInfo(string filename, int line) {
			if (!this.hasTrace) {
				base.TryAddFileLineInfo(filename, line);
			}
		}
	}

	internal class NameRefException : InterpreterException {
		internal NameRef nameRef;
		internal NameRefException(int line, int col, string filename, NameRef nameRef, string scriptHolderName)
			: base("Badly placed or invalid namespace/class name ('" + LogStr.Ident(nameRef.name) + "').", line, col, filename, scriptHolderName) {
			this.nameRef = nameRef;
		}
	}
}