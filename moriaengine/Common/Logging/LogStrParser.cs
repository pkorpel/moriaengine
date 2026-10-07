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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace SteamEngine.Common {
	public class LogStrParser {
		private ILogStrDisplay display;
		private Stack<LogStyles> styleStack = new Stack<LogStyles>();

		private static char[] separatorArray = { LogStrBase.separatorChar };

		public LogStrParser(ILogStrDisplay display) {
			this.display = display;
		}

		public void ProcessLogStr(LogStr logStr) {
			this.ProcessLogStr(logStr.rawString);
		}

		private LogStyles CurrentStyle {
			get {
				if (this.styleStack.Count > 0) {
					return this.styleStack.Peek();
				}
				return LogStyles.Default;
			}
		}

		public void ProcessLogStr(string logStrEncoded) {
			string[] tokens = logStrEncoded.Split(separatorArray);
			int tokenLen = tokens.Length;
			if (tokenLen > 0) {
				for (int i = 0; i < tokenLen; i++) {
					string token = tokens[i];

					if (string.IsNullOrEmpty(token)) {
						continue;
					}
					if (i % 2 == 1) {
						switch (token[0]) {
							case LogStrBase.eosChar:
								if (this.styleStack.Count > 0) {
									this.styleStack.Pop();
								}
								continue;
							case LogStrBase.titleChar:
								string title = token.Substring(1);
								if (title.Length > 0) {
									this.display.SetTitle(title);
								} else {
									this.display.SetTitleToDefault();
								}
								continue;
							case LogStrBase.styleChar:
								int num = int.Parse(token.Substring(1), CultureInfo.InvariantCulture);
								this.styleStack.Push((LogStyles) num);
								continue;
						}
					}

					this.display.Write(token, LogStrBase.GetLogStyleInfo(this.CurrentStyle));
				}
			}
		}

		private static Regex fileLineRE = new Regex(@"^(?<filename>.+), (?<linenumber>\d+)$",
			//private static Regex compileErrorRE = new Regex(@"^\[csc\](?<filename>.+)$",                   
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		[SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
		public static bool TryParseFileLine(string p, out string filename, out int line) {
			Match m = fileLineRE.Match(p);
			filename = m.Groups["filename"].Value;
			if (m.Success) {
				line = ConvertTools.ParseInt32(m.Groups["linenumber"].Value);
				return true;
			}
			line = 0;
			return false;
		}

		//uses the /./ in LogStr paths to rebuild it from the local . dir
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static string TranslateToLocalPath(string path) {
			int indexOfDot = path.IndexOf(LogStr.separatorAndDot);
			if (indexOfDot > -1) {
				return Path.GetFullPath(path.Substring(indexOfDot+1));
			}
			return path;
		}
	}
}