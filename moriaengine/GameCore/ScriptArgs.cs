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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace SteamEngine {
	public class ScriptArgs {
		private string formatArgs;
		private readonly object[] argv;
		private string args;

		private static object[] zeroArray = new object[0];

		//public object[] Argv { get {
		//                           return argv;
		//                       } 
		//}

		//		[Obsolete("no more used, could have caused badly recognized calls", true)]
		//		public ScriptArgs(string formatArgs, params object[] argv) {//function parameters
		//			this.argv = argv;
		//			this.formatArgs = formatArgs;
		//		}

		public string FormatString {
			get {
				return this.formatArgs;
			}
			internal set {
				this.formatArgs = value;
				this.args = null;
			}
		}

		public ScriptArgs(params object[] argv) {//core trigger`s parameters
			if (argv != null) {
				this.argv = argv;
			} else {
				this.argv = zeroArray;
			}
		}

		public string Args {
			get {
				if (this.args == null) {
					if (this.formatArgs == null) {
						if (this.argv.Length > 0) {//we fake the args of the function, in case it was actually called from compiled code
							StringBuilder sb = new StringBuilder();
							for (int i = 0, n = this.argv.Length - 1; i < n; i++) {
								sb.Append("{" + i + "}, ");
							}
							sb.Append("{" + (this.argv.Length - 1) + "}");
							this.formatArgs = sb.ToString();
						} else {
							this.formatArgs = "";
						}
					}
					//Console.WriteLine("format string: '{0}', with {1} args", formatArgs, argv.Length);
					this.args = string.Format(CultureInfo.InvariantCulture,
						this.formatArgs, this.argv);
				}
				return this.args;
			}
		}


		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public object[] Argv {
			get { 
				return this.argv; 
			}
		} 

		//a little hack because of Dialogs. No more needed, yay!
		//public void InsertArgo(object obj) {
		//    string ignoreTheWarning = Args;//instantiate the Args, so it doesnt get affected by this; mono compiler knows we dont use the variable ;) thus the name of it
		//    int argvLength = argv.Length;
		//    object[] newArray = new object[argvLength+1];
		//    Array.Copy(argv, 0, newArray, 1, argvLength);
		//    newArray[0] = obj;
		//    this.argv = newArray;
		//}
	}
}