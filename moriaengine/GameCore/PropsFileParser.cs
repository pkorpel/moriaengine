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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine {
	public static class PropsFileParser {
		//possible targets:
		//GameAccount.Load
		//Thing.Load
		//ThingDef.LoadDefsSection
		//Globals.LoadGlobals

		//regular expressions for stream loading
		//[type name]//comment
		internal static readonly Regex headerRE = new Regex(@"^\[\s*(?<type>.*?)(\s+(?<name>.*?))?\s*\]\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		//"triggerkey=@triggername//comment"
		//"triggerkey @triggername//comment"
		//triggerkey can be "ON", "ONTRIGGER", "ONBUTTON", or ""
		internal static readonly Regex triggerRE = new Regex(@"^\s*(?<triggerkey>(on|ontrigger|onbutton))((\s*=\s*)|(\s+))@?\s*(?<triggername>.+?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		//private static int line;

		//private static void Warning(string s) {
		//	Logger.WriteWarning(WorldSaver.CurrentFile,line,s);
		//}
		//
		//private static void Error(string s) {
		//	Logger.WriteError(WorldSaver.CurrentFile,line,s);
		//}

		public static IEnumerable<PropsSection> Load(string filename, StreamReader stream, CanStartAsScript isScript, bool displayPercentage) {
			int line = 0;
			PropsSection curSection = null;
			TriggerSection curTrigger = null; //these are also added to curSection...

			long streamLen = stream.BaseStream.Length;
			long lastSentPercentage = -1;
			string fileNameToDisplay = Path.GetFileName(filename);

			while (true) {
				string curLine = stream.ReadLine();
				line++;
				if (curLine != null) {
					if (displayPercentage) {
						long currentPercentage = (stream.BaseStream.Position * 100) / streamLen;
						if (currentPercentage > lastSentPercentage) {
							Logger.SetTitle(string.Concat("Loading ", fileNameToDisplay, ": ", currentPercentage.ToString(CultureInfo.InvariantCulture), "%"));
							lastSentPercentage = currentPercentage;
						}
					}

					curLine = curLine.Trim();
					if ((curLine.Length == 0) || (curLine.StartsWith("//"))) {
						//it is a comment or a blank line
						if (curTrigger != null) {
							//in script compiler do also blank lines count, so we can`t ignore them.
							curTrigger.Code.AppendLine(curLine);
						}//else the comment gets lost... :\
						continue;
					}
					Match m = headerRE.Match(curLine);
					if (m.Success) {
						if (curSection != null) {//send the last section
							yield return curSection;
						}
						GroupCollection gc = m.Groups;
						curSection = new PropsSection(filename, gc["type"].Value, gc["name"].Value, line, gc["comment"].Value);
						if (isScript(curSection.HeaderType)) {
							//if it is something like [function xxx]
							curTrigger = new TriggerSection(filename, line, curSection.HeaderType, curSection.HeaderName, gc["comment"].Value);
							curSection.AddTrigger(curTrigger);
						} else {
							curTrigger = null;
						}
						continue;
					}
					m = triggerRE.Match(curLine);
					//on=@blah
					if (m.Success) {
						//create a new triggersection
						GroupCollection gc = m.Groups;
						curTrigger = new TriggerSection(filename, line, gc["triggerkey"].Value, gc["triggername"].Value, gc["comment"].Value);
						if (curSection == null) {
							//a trigger section without real section?
							Logger.WriteWarning(filename, line, "No section for this trigger...?");
						} else {
							//Console.WriteLine("Trigger section: " + curTrigger.TriggerName);
							curSection.AddTrigger(curTrigger);
						}
						continue;
					}
					if (curTrigger != null) {
						if (curSection != null) {
							curTrigger.Code.AppendLine(curLine);
						} else {
							//this shouldnt be, a trigger without section...?
							Logger.WriteWarning(filename, line, "Skipping line '" + curLine + "'.");
						}
						continue;
					}
					m = LocStringCollection.valueRE.Match(curLine);
					if (m.Success) {
						if (curSection != null) {
							GroupCollection gc = m.Groups;
							curSection.AddPropsLine(gc["name"].Value, gc["value"].Value, line, gc["comment"].Value);
						} else {
							//this shouldnt be, a property without header...?
							Logger.WriteWarning(filename, line, "No section for this value. Skipping line '" + curLine + "'.");
						}
						continue;
					}
					Logger.WriteError(filename, line, "Unrecognizable data '" + curLine + "'.");
				} else {
					//end of file
					if (curSection != null) {
						yield return curSection;
					}
					break;
				}
			} //end of (while (true)) - for each line of the file

			if (displayPercentage) {
				Logger.SetTitle("");
			}
		}
	}

	public class PropsSection {
		private readonly string headerComment;
		private readonly string headerType;
		private string headerName;//[headerType headerName]
		private readonly int headerLine;
		private readonly string filename;
		private Dictionary<string, PropsLine> props;//table of PropsLines
		private List<TriggerSection> triggerSections;//list of TriggerSections

		internal PropsSection(string filename, string type, string name, int line, string comment) {
			this.filename = filename;
			this.headerType = type;
			this.headerName = name;
			this.headerLine = line;
			this.headerComment = comment;
			this.props = new Dictionary<string, PropsLine>(StringComparer.OrdinalIgnoreCase);
			this.triggerSections = new List<TriggerSection>();
		}

		public string HeaderComment {
			get { return this.headerComment; }
		}

		public string HeaderType {
			get { return this.headerType; }
		}

		public string HeaderName {
			get { return this.headerName; }
			set { this.headerName = value; }
		}

		public int HeaderLine {
			get { return this.headerLine; }
		}

		public string Filename {
			get { return this.filename; }
		} 


		public TriggerSection GetTrigger(int index) {
			return this.triggerSections[index];
		}

		public TriggerSection GetTrigger(string name) {
			foreach (TriggerSection s in this.triggerSections) {
				if (StringComparer.OrdinalIgnoreCase.Equals(name, s.TriggerName)) {
					return s;
				}
			}
			return null;
		}

		public TriggerSection PopTrigger(string name) {
			int i = 0, n = this.triggerSections.Count;
			TriggerSection s = null;
			for (; i < n; i++) {
				s = this.triggerSections[i];
				if (StringComparer.OrdinalIgnoreCase.Equals(name, s.TriggerName)) {
					n = -1;
					break;
				}
			}
			if (n == -1) {
				this.triggerSections.RemoveAt(i);
				return s;
			}
			return null;
		}

		public int TriggerCount {
			get {
				return this.triggerSections.Count;
			}
		}

		internal void AddTrigger(TriggerSection value) {
			this.triggerSections.Add(value);
		}

		internal void AddPropsLine(string name, string value, int line, string comment) {
			PropsLine p = new PropsLine(name, value, line, comment);
			string origKey = name;
			string key = origKey;
			for (int a = 0; this.props.ContainsKey(key); a++) {
				key = origKey + a.ToString(CultureInfo.InvariantCulture);
				//duplicite properties get a counted name
				//like if there is more "events=..." lines, they are in the hashtable with keys
				//events, events0, events1, etc. 
				//these entries wont be probably looked up by their name anyways.
			}
			this.props[key] = p;
		}

		public PropsLine TryPopPropsLine(string name) {
			PropsLine line;
			if (this.props.TryGetValue(name, out line)) {
				this.props.Remove(name);
			}
			return line;
		}

		public PropsLine PopPropsLine(string name) {
			PropsLine line;
			if (this.props.TryGetValue(name, out line)) {
				this.props.Remove(name);
			} else {
				throw new SEException(LogStr.FileLine(this.filename, this.headerLine) + "There is no '" + name + "' line!");
			}
			return line;
		}

		public ICollection<PropsLine> PropsLines {
			get {
				return this.props.Values;
			}
		}

		public override string ToString() {
			return string.Format(CultureInfo.InvariantCulture, 
				"[{0} {1}]", this.headerType, this.headerName);
		}
	}

	public class TriggerSection {
		private readonly string triggerComment;
		private readonly string triggerKey;	//"on", "ontrigger", "onbutton", or just ""
		private readonly int startLine;
		private readonly string triggerName;	//"create", etc
		private readonly string filename;
		private StringBuilder code;	//code

		internal TriggerSection(string filename, int startline, string key, string name, string comment) {
			this.filename = filename;
			this.triggerKey = key;
			this.triggerName = name;
			this.startLine = startline;
			this.code = new StringBuilder();
			this.triggerComment = comment;
		}

		public string TriggerComment {
			get {
				return this.triggerComment;
			}
		}

		public string TriggerKey {
			get {
				return this.triggerKey;
			}
		}

		public string TriggerName {
			get {
				return this.triggerName;
			}
		}
		
		public int StartLine {
			get {
				return this.startLine;
			}
		}

		public string Filename {
			get {
				return this.filename;
			}
		}

		public StringBuilder Code {
			get {
				return this.code;
			}
			set {
				this.code = value;
			}
		}

		public override string ToString() {
			return string.Concat(this.triggerKey, "=@", this.triggerName);
		}
	}

	public class PropsLine {
		private readonly string comment;
		private readonly string name;
		private readonly string value;//name=value
		private readonly int line;

		public PropsLine(string name, string value, int line, string comment) {
			this.name = name;
			this.value = value;
			this.line = line;
			this.comment = comment;
		}

		public string Comment {
			get { return this.comment; }
		}

		public string Name {
			get { return this.name; }
		}

		public string Value {
			get { return this.value; }
		}

		public int Line {
			get { return this.line; }
		} 

		public override string ToString() {
			return this.name + " = " + this.value;
		}
	}
}
