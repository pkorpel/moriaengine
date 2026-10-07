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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Persistence;

//will be alternative to Timer which runs triggers...
//this runs "hardcoded" methods via MethodInfo
namespace SteamEngine.Timers {

	[SaveableClass, DeepCopyableClass]
	public class MethodTimer : BoundTimer {

		[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
		public MethodInfo method;

		[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
		[SaveableData, CopyableData]
		public object[] args;

		protected sealed override void OnTimeout(TagHolder cont) {
			this.method.Invoke(cont, BindingFlags.Default, null, this.args, null);
		}

		[LoadingInitializer]		
		public MethodTimer() {
		}

		[DeepCopyImplementation]
		public MethodTimer(MethodTimer copyFrom) {
			this.method = copyFrom.method;
		}

		public MethodTimer(MethodInfo method, object[] args) {
			this.method = method;
			this.args = args;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Save]
		public override void Save(SaveStream output) {
			StringBuilder sb = new StringBuilder(this.method.DeclaringType.ToString());
			sb.Append(".").Append(this.method.Name);
			sb.Append("(");
			ParameterInfo[] pars = this.method.GetParameters();
			if (pars.Length > 0) {
				foreach (ParameterInfo pi in pars) {
					sb.Append(pi.ParameterType);
					sb.Append(", ");
				}
				sb.Length -= 2;
			}
			sb.Append(")");
			output.WriteLine("method=" + sb);
			base.Save(output);
		}

		private static Regex methodSignRE = new Regex(@"^\s*(?<type>[a-zA-Z0-9\.]+)\.(?<method>[a-zA-Z0-9]+)\((([a-zA-Z0-9\.]+)(\,\s*)?)*\)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), LoadLine]
		public override void LoadLine(string filename, int line, string name, string value) {
			if (name.Equals("method")) {
				//Console.WriteLine("loading method with string: "+value);

				Match m = methodSignRE.Match(value);
				if (m.Success) {
					GroupCollection gc = m.Groups;
					Type type = Type.GetType(gc["type"].Value, true, true); //true true: throw exception, case insensitive
					string methodName = gc["method"].Value;
					CaptureCollection cc = gc[2].Captures;
					int ccCount = cc.Count;
					Type[] paramTypes = new Type[ccCount];
					for (int i = 0; i < ccCount; i++) {
						paramTypes[i] = Type.GetType(cc[i].Value, true, true);
					}
					MethodInfo mi = type.GetMethod(methodName, paramTypes);
					if (mi != null) {
						this.method = MemberWrapper.GetWrapperFor(mi);
					} else {
						throw new SEException("Unrecognized method.");
					}
				} else {
					throw new SEException("The value has unparsable format");
				}
			}
			base.LoadLine(filename, line, name, value);
		}
	}
}
