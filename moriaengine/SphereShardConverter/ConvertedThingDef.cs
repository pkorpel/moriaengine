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

//
namespace SteamEngine.Converter {
	//	/**
	//	Hardcoded changes to converted scripts:
	//		1) The fishing pole is forced to be twohanded.
	//			(It's detected by checking if an item's layer is 2 and model is 0xdbf. 0xdc0 is a dupeitem of it and so will inherit its twohandedness.)
	//	
	//	*/
	//	
	public class ConvertedThingDef : ConvertedDef {
		protected PropsLine idLine;
		PropsLine defname1, defname2, dupeItemLine;
		//bool ownModel = false;
		bool hasNumericalHeader;
		int modelNum = -1;
		protected ConvertedThingDef modelDef;
		//bool isDupeItem = false;

		public Dictionary<string, ConvertedThingDef> byDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<int, ConvertedThingDef> byModel = new Dictionary<int, ConvertedThingDef>();

		public int Model {
			get {
				if (this.modelNum == -1) {
					if (this.modelDef != null) {
						return this.modelDef.Model;
					}
				} else {
					return this.modelNum;
				}
				this.Error(this.origData.HeaderLine, "ThingDef " + this.headerName + " has no model set...?");
				return -1;
			}
		}

		private static LineImplTask[] firstStageImpl = {
				//the engine's own AbstractDefTriggerGroupHolder.LoadScriptLine already
				//recognizes these field names directly (same mechanism wiring e_banker
				//onto bankers, #509) - a plain pass-through is correct, not a rename.
				//Convert/assign every TEVENTS field regardless of whether the referenced
				//trigger group exists yet (#551 decision #2) - a missing group just logs
				//a warning and no-ops, so this is deliberate future-proofing.
				new LineImplTask("event", WriteAsIsLine),
				new LineImplTask("events", WriteAsIsLine),
				new LineImplTask("tevent", WriteAsIsLine),
				new LineImplTask("tevents", WriteAsIsLine),
				new LineImplTask("speech", WriteAsIsLine),
				new LineImplTask("tspeech", WriteAsIsLine),
				new LineImplTask("triggergroup", WriteAsIsLine),
				new LineImplTask("triggergroups", WriteAsIsLine),
				new LineImplTask("name", WriteInQuotes)
			};

		public ConvertedThingDef(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.firstStageImplementations.Add(firstStageImpl);
		}

		private static void WriteAsIsLine(ConvertedDef def, PropsLine line) {
			def.Set(line);
		}


		public override void FirstStage() {
			base.FirstStage();

			this.dupeItemLine = this.origData.TryPopPropsLine("dupeitem");
			if (this.dupeItemLine != null) {
				int headerNum;
				if (ConvertTools.TryParseInt32(this.headerName, out headerNum)) {
					this.headerName = "0x" + headerNum.ToString("x");
					this.modelNum = headerNum;
				}
			} else {
				bool needsHeader = false;
				this.idLine = this.origData.TryPopPropsLine("id");

				int headerNum;
				if (ConvertTools.TryParseInt32(this.headerName, out headerNum)) {
					if (this.idLine != null) {//it does not mean model...
						needsHeader = true;
					} else {
						this.hasNumericalHeader = true;
						//ownModel = true;
						this.modelNum = headerNum;
						this.byModel[headerNum] = this;
						this.headerName = "0x" + headerNum.ToString("x");
					}
				} else {
					this.byDefname[this.headerName] = this;
				}

				this.defname1 = this.origData.TryPopPropsLine("defname");
				if (this.defname1 != null) {
					if (needsHeader) {
						this.headerName = this.defname1.Value;
						needsHeader = false;
					}
					this.byDefname[this.defname1.Value] = this;
				}

				this.defname2 = this.origData.TryPopPropsLine("defname2");
				if (this.defname2 != null) {
					if (needsHeader) {
						this.headerName = this.defname2.Value;
						needsHeader = false;
					}
					this.byDefname[this.defname2.Value] = this;
				}

				if (needsHeader) {
					//what now? :)
					this.headerName = "i_hadnodefname_0x" + headerNum.ToString("x");
					this.Info(this.origData.HeaderLine, "Has no defname except a number, and model defined elsewhere...");
				}
			}
		}

		public override void SecondStage() {
			base.SecondStage();
			if (this.idLine != null) {
				int idNum;
				if (ConvertTools.TryParseInt32(this.idLine.Value, out idNum)) {
					if (this.byModel.TryGetValue(idNum, out this.modelDef)) {
						this.Set("model", this.modelDef.PrettyDefname, this.idLine.Comment);
						//Info(idLine.line, "ID Written as "+modelDef.PrettyDefname);
					} else {
						this.Set("model", "0x" + idNum.ToString("x"), this.idLine.Comment);
						this.modelNum = idNum;
					}
				} else {
					this.Set("model", this.idLine.Value, this.idLine.Comment);
					this.byDefname.TryGetValue(this.idLine.Value, out this.modelDef);
				}
			}

			if (this.dupeItemLine != null) {
				int dupeItemNum;
				bool dupeItemSet = false;
				if (ConvertTools.TryParseInt32(this.dupeItemLine.Value, out dupeItemNum)) {
					ConvertedThingDef dupeItemDef;
					if (this.byModel.TryGetValue(dupeItemNum, out dupeItemDef)) {
						this.Set(this.dupeItemLine.Name, dupeItemDef.PrettyDefname, this.dupeItemLine.Comment);
						dupeItemSet = true;
						//Info(dupeItemLine.line, "DupeItem Written as "+dupeItemDef.PrettyDefname);
					}
				}
				if (!dupeItemSet) {
					MayBeHex_IgnorePoint(this, this.dupeItemLine);
				}
			}
		}

		public override void ThirdStage() {
			bool defnameWritten = false;
			if (this.defname1 != null) {
				if (!StringComparer.OrdinalIgnoreCase.Equals(this.headerName, this.defname1.Value)) {
					defnameWritten = true;
					this.Set(this.defname1);
				}
			}
			if (this.defname2 != null) {
				if (defnameWritten) {
					this.Warning(this.defname2.Line, "Defname2 ignored. In steamengine, defs can have mostly 1 alternative defname.");
				} else if (!StringComparer.OrdinalIgnoreCase.Equals(this.headerType, this.defname2.Value)) {
					this.Set("defname", this.defname2.Value, this.defname2.Comment);
				}
			}

			base.ThirdStage();

			TriggerSection createTrigger = this.origData.GetTrigger("create");
			if (createTrigger != null) {
				int lineNum = createTrigger.StartLine;
				using (StringReader reader = new StringReader(createTrigger.Code.ToString())) {
					string line;
					while ((line = reader.ReadLine()) != null) {
						string lowered = line.ToLowerInvariant();
						if (lowered.StartsWith("item ") || lowered.StartsWith("item=") || lowered.StartsWith("item(")) {
							break; //start of loot section, probably
						}
						//#563 - itemnewbie lines (starting-equipment grants) are NOT the loot
						//section - a chardef often has several of these in a row (e.g. a
						//ranger's bow THEN its arrows), all of which need to reach
						//ProcessCreateTriggerLine, not just the first one
						lineNum++;
						//#656 - tagRE tried FIRST, not as a fallback: tagRE only ever matches a real
						//"tag(" / "tag." prefixed line, so trying it first never steals anything from
						//nameValueRE. The old fallback order was backwards for exactly the lines tagRE
						//is meant for - nameValueRE's separator is "=" OR any whitespace, so a tag-call
						//line with a trailing inline comment (e.g. "tag(resist_bleed,300)  //krvaci", a
						//near-universal archive convention) satisfies nameValueRE at the whitespace
						//before "//" first, swallowing the entire "tag(resist_bleed,300)" call as one
						//opaque name with an empty value - silently dropping the tag with no warning,
						//before tagRE ever got a chance to parse it correctly.
						Match m = tagRE.Match(line);
						if (m.Success) {
							this.ProcessCreateTriggerLine("tag."+m.Groups["name"].Value.ToLowerInvariant(),
								m.Groups["value"].Value, m.Groups["comment"].Value, lineNum);
						} else {
							m = nameValueRE.Match(line);
							if (m.Success) {
								this.ProcessCreateTriggerLine(m.Groups["name"].Value.ToLowerInvariant(),
									m.Groups["value"].Value, m.Groups["comment"].Value, lineNum);
							} //else ignore (?)
						}
					}
				}

				//#667-adjacent - this engine's own LScript interpreter can execute a SIMPLE
				//ON=@X trigger body directly at runtime (confirmed: moriaengine's own race-
				//selection dialog is wired up via exactly "on=@dclick / src.Dialog(
				//d_raceclassentry)"), but this converter had no mechanism at all to emit ANY
				//trigger's body except @Create's, which is read above and fully consumed into
				//specific fields line by line - never written back out verbatim.
				//
				//Deliberately keyed by DEFNAME, not just trigger name - a blanket "pass
				//through every @dclick body found" would ALSO resurrect e.g. aldaron.scp's
				//i_shooter_medkit, whose @dclick body uses real if/endif control flow and
				//<src.flags> bracket expansion this engine's own LScript support has no
				//"if"/"endif" handling for at all (confirmed: grep GameCore/LScript/ for
				//either keyword finds nothing) - dumping that verbatim would very likely
				//reintroduce a real boot/parse error, not fix anything. Add a defname here
				//ONLY after confirming its specific trigger body is simple enough for this
				//engine to actually run (plain single-statement calls like src.Dialog(...),
				//not conditionals/loops/bracket-macro control flow).
				string[] passthroughNames;
				if (passthroughTriggers.TryGetValue(this.PrettyDefname, out passthroughNames)) {
					foreach (string passthroughName in passthroughNames) {
						TriggerSection section = this.origData.GetTrigger(passthroughName);
						if (section != null) {
							this.EmitVerbatimTrigger(passthroughName, section);
						}
					}
				}
			}
		}

		//defname -> trigger names this engine's LScript support can actually run, verified
		//simple enough case by case - see the long comment above. Currently just the #667
		//race-class stone.
		private static readonly Dictionary<string, string[]> passthroughTriggers =
			new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
				{ "i_stone_raceclass", new[] { "dclick" } }
			};

		private void EmitVerbatimTrigger(string triggerName, TriggerSection section) {
			this.writtenData.Add("");
			this.writtenData.Add("ON=@" + triggerName);
			using (StringReader reader = new StringReader(section.Code.ToString())) {
				string line;
				while ((line = reader.ReadLine()) != null) {
					string trimmed = line.Trim();
					if (trimmed.Length > 0) {
						this.writtenData.Add(trimmed);
					}
				}
			}
		}

		private static readonly Regex nameValueRE = new Regex(@"^\s*(?<name>.*?)((\s*=\s*)|(\s+))(?<value>.*?)\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private static Regex tagRE = new Regex(@"^tag((\.)|(\())(?<name>.+?)\s*((=)|(,))\s*(?<value>.+?)\s*\)?\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		protected virtual void ProcessCreateTriggerLine(string name, string value, string comment, int lineNum) {
			switch (name) {
				case "color":
					this.Set("Color", value, comment);
					break;
			}
		}

		public string PrettyDefname {
			get {
				if (!this.hasNumericalHeader) {
					return this.headerName;
				}
				if (this.defname1 != null) {
					return this.defname1.Value;
				}
				if (this.defname2 != null) {
					return this.defname2.Value;
				}

				//it's numeric, so...
				return "0x" + ConvertTools.ParseInt64(this.headerName).ToString("x");
			}
		}
	}
}