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
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.Converter {
	public delegate void LineImpl(ConvertedDef def, PropsLine line);

	public class LineImplTask {
		public readonly string fieldName;
		public readonly LineImpl deleg;

		public LineImplTask(string fieldName, LineImpl deleg) {
			this.fieldName = fieldName;
			this.deleg = deleg;
		}
	}

	public class ConvertedDef {
		protected PropsSection origData;
		//protected string origFile;
		protected List<string> writtenData = new List<string>();
		private ConvertedFile convertedFile;
		public string headerType;
		public string headerName;

		private bool dontDump;

		protected List<LineImplTask[]> firstStageImplementations = new List<LineImplTask[]>();
		protected List<LineImplTask[]> secondStageImplementations = new List<LineImplTask[]>();
		protected List<LineImplTask[]> thirdStageImplementations = new List<LineImplTask[]>();

		public ConvertedDef(PropsSection input, ConvertedFile convertedFile) {
			this.origData = input;
			//this.origFile = input.Filename;
			this.convertedFile = convertedFile;

			this.headerType = input.HeaderType;
			this.headerName = input.HeaderName;
		}

		public void Set(PropsLine line) {
			this.Set(line.Name, line.Value, line.Comment);
		}

		public void Set(string key, string value, string comment) {
			if (string.IsNullOrEmpty(comment)) {
				this.writtenData.Add(string.Format("{0} = {1}", key, value));
			} else {
				this.writtenData.Add(string.Format("{0} = {1} //{2} ", key, value, comment));
			}
		}

		public virtual void Dump(TextWriter writer) {
			writer.WriteLine();
			string header = string.Concat("[", this.headerType, " ", this.headerName, "]");

			if (this.origData.HeaderComment.Length > 0) {
				header = header + " //" + this.origData.HeaderComment;
			}
			if (this.dontDump) {
				header = "//" + header + " //(commented out by Converter) ";
			}
			writer.WriteLine(header);

			foreach (string line in this.writtenData) {
				writer.WriteLine(this.dontDump ? ("//" + line) : line);
			}
		}

		private void BasicStageImpl(List<LineImplTask[]> implementations) {
			foreach (LineImplTask[] arr in implementations) {
				foreach (LineImplTask task in arr) {
					string origKey = task.fieldName;
					LineImpl deleg = task.deleg;

					string key = origKey;
					PropsLine line = this.origData.TryPopPropsLine(key);

					for (int a = 0; (line != null); a++) {
						deleg(this, line);

						key = origKey + a;
						line = this.origData.TryPopPropsLine(key);
					}
				}
			}
		}

		public void DontDump() {
			this.dontDump = true;
		}

		public virtual void FirstStage() {
			bool needspace = false;
			PropsLine line = this.origData.TryPopPropsLine("category");
			if (line != null) {
				this.Set(line); needspace = true;
			}
			line = this.origData.TryPopPropsLine("subsection");
			if (line != null) {
				this.Set(line); needspace = true;
			}
			line = this.origData.TryPopPropsLine("description");
			if (line != null) {
				this.Set(line); needspace = true;
			}
			if (needspace) {
				this.writtenData.Add("");
			}


			this.BasicStageImpl(this.firstStageImplementations);
		}

		public virtual void SecondStage() {
			this.BasicStageImpl(this.secondStageImplementations);
		}

		public virtual void ThirdStage() {
			this.BasicStageImpl(this.thirdStageImplementations);

			foreach (PropsLine line in this.origData.PropsLines) {
				if (line.Name.ToLowerInvariant().StartsWith("tag.")) {
					WriteAsIs(this, line);
				} else {
					WriteAsComment(this, line);
				}
			}
		}

		//public override string tostring() {
		//	return "converteddef";
		//}

		public void Info(int linenum, string message) {
			if (ConverterMain.AdditionalConverterMessages) {
				Console.WriteLine("Info: " + LogStr.FileLine(this.convertedFile.origPath, linenum) + LogStr.Highlight(message));
			}
		}

		public void Warning(int linenum, string message) {
			Logger.WriteWarning(this.convertedFile.origPath, linenum, message);
		}

		public void Error(int linenum, string message) {
			Logger.WriteError(this.convertedFile.origPath, linenum, message);
		}

		//generic line implementations
		protected static string WriteAsIs(ConvertedDef def, PropsLine line) {
			def.Set(line);
			return line.Value;
		}

		protected static void WriteInQuotes(ConvertedDef def, PropsLine line) {
			string value;
			Match ma = ConvertTools.stringRE.Match(line.Value);
			if (ma.Success) {
				value = string.Intern(ma.Groups["value"].Value);
			} else {
				value = line.Value;
			}
			value = "\"" + value + "\"";
			def.Set(line.Name, value, line.Comment);
			//return value;
		}

		//protected static void WriteAsBool(ConvertedDef def, PropsLine line) {
		//    string value;
		//    Match ma = TagMath.stringRE.Match(line.value);
		//    if (ma.Success) {
		//        value = String.Intern(ma.Groups["value"].Value);
		//    } else {
		//        value = line.value;
		//    }
		//    value = "\""+value+"\"";
		//    def.Set(line.name, value, line.comment);
		//}

		protected static void WriteAsComment(ConvertedDef def, PropsLine line) {
			def.Set("//" + line.Name, line.Value, line.Comment + " commented out by Converter");
			//return line.Value;
		}

		protected static void MayBeInt_IgnorePoint(ConvertedDef def, PropsLine line) {
			string retVal = TryNormalizeNumber(line.Value.Replace(".", ""));
			def.Set(line.Name, retVal, line.Comment);
			//return retVal;
		}

		protected static void MayBeHex_IgnorePoint(ConvertedDef def, PropsLine line) {
			string retVal = TryNormalizeNumberAsHex(line.Value.Replace(".", ""));
			def.Set(line.Name, retVal, line.Comment);
			//return retVal;
		}

		public static string TryNormalizeNumber(string input) {
			try {
				object number = ConvertTools.ParseAnyNumber(input);
				input = number.ToString();
			} catch (Exception) {
			}
			return input;
		}

		public static string TryNormalizeNumberAsHex(string input) {
			try {
				object number = ConvertTools.ParseAnyNumber(input);
				long i = Convert.ToInt64(number);
				input = "0x" + i.ToString("x");
			} catch (Exception) {
			}
			return input;
		}
	}
}