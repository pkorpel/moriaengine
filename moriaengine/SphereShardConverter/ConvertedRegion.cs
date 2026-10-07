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
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.Converter {

	public class ConvertedRegion : ConvertedDef {
		private static Dictionary<string, ConvertedRegion> regionsByDefname = new Dictionary<string, ConvertedRegion>(StringComparer.OrdinalIgnoreCase);
		private static List<ConvertedRegion> allRegions = new List<ConvertedRegion>();
		
		private List<ImmutableRectangle> rectangles = new List<ImmutableRectangle>();
		private Point2D[] points;//corners of the rectangles. its not too accurate, but who cares... :)
		private byte mapplane;
		private int hierarchyIndex = -1;
		private ConvertedRegion[] parents;

		private bool mapplaneSet;
		private PropsLine mapplaneLine;



		private static LineImplTask[] firstStageImpl = {
//				new LineImplTask("event", new LineImpl(WriteAsTG)), 
//				new LineImplTask("events", new LineImpl(WriteAsTG)), 
//				new LineImplTask("tevent", new LineImpl(WriteAsTG)), 
//				new LineImplTask("tevents", new LineImpl(WriteAsTG)), 
//				new LineImplTask("resources", new LineImpl(WriteAsTG)), 

				new LineImplTask("p", ParseP), 
				new LineImplTask("rect", ParseRect), 
				new LineImplTask("m", ParseMapplane), 
				new LineImplTask("mapplane", ParseMapplane), 

				new LineImplTask("flags", ParseFlags), 
				new LineImplTask("flag_announce", ParseFlags), 
				new LineImplTask("flag_antimagic_all", ParseFlags), 
				new LineImplTask("flag_antimagic_damage", ParseFlags), 
				new LineImplTask("flag_antimagic_gate", ParseFlags), 
				new LineImplTask("flag_antimagic_recallin", ParseFlags), 
				new LineImplTask("flag_antimagic_recallout", ParseFlags), 
				new LineImplTask("flag_antimagic_teleport", ParseFlags), 
				new LineImplTask("flag_arena", ParseFlags), 
				new LineImplTask("flag_guarded", ParseFlags), 
				new LineImplTask("flag_instalogout", ParseFlags), 
				new LineImplTask("flag_nobuilding", ParseFlags), 
				new LineImplTask("flag_nobuilding", ParseFlags), 
				new LineImplTask("flag_nodecay", ParseFlags), 
				new LineImplTask("flag_nopvp", ParseFlags), 
				new LineImplTask("flag_roof", ParseFlags), 
				new LineImplTask("flag_safe", ParseFlags), 
				new LineImplTask("flag_ship", ParseFlags), 
				new LineImplTask("flag_underground", ParseFlags), 
				new LineImplTask("flagsafe", ParseFlags), 
				new LineImplTask("guarded", ParseFlags), 
				new LineImplTask("nopvp", ParseFlags), 
				new LineImplTask("nodecay", ParseFlags), 
				new LineImplTask("nobuild", ParseFlags), 
				new LineImplTask("underground", ParseFlags) 
		};


		static Regex nonCharacterSplitRE = new Regex(@"[^\w]+", RegexOptions.Compiled);

		public ConvertedRegion(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.firstStageImplementations.Add(firstStageImpl);

			this.Set("createdat", ObjectSaver.Save(DateTime.Now), "");

			string name = input.HeaderName;
			this.Set("Name", "\"" + name + "\"", "");
			if (StringComparer.OrdinalIgnoreCase.Equals(name, "%servname%")) {
				//headerType = "WorldRegion";
				this.hierarchyIndex = 0;
				//} else {
				//headerType = "Region";
			}
			this.headerType = "Region";
			//todo: make this strip all non-ascii characters
			string[] splitted = nonCharacterSplitRE.Split(name);
			splitted[0] = "a_" + splitted[0];
			name = string.Join("_", splitted);//we make "a_local_mine" out of "local mine"
			string defname = name;
			int toAdd = 2;
			while (regionsByDefname.ContainsKey(defname)) {
				defname = name + "_" + toAdd;
				toAdd++;
			}
			regionsByDefname[defname] = this;
			this.headerName = defname;
			allRegions.Add(this);

			PropsLine defnameLine = input.TryPopPropsLine("defname");
			if (defnameLine != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(defnameLine.Value, "a_world")) {
					//headerType = "Worldregion";
					this.hierarchyIndex = 0;
				}
				regionsByDefname.Remove(this.headerName);
				this.headerName = defnameLine.Value;
				regionsByDefname[defnameLine.Value] = this;//this could overwrite something, but that is not our fault I think :)
			}
		}

		private static void ParseRect(ConvertedDef def, PropsLine line) {
			Match m = Region.rectRE.Match(line.Value);
			if (m.Success) {
				GroupCollection gc = m.Groups;
				//Console.WriteLine("args: "+args);
				//Console.WriteLine("parsed as: {0}, {1}, {2}, {3}", gc["x1"], gc["y1"], gc["x2"], gc["y2"]);
				ushort x1 = ConvertTools.ParseUInt16(gc["x1"].Value);
				ushort y1 = ConvertTools.ParseUInt16(gc["y1"].Value);
				ushort x2 = ConvertTools.ParseUInt16(gc["x2"].Value);
				ushort y2 = ConvertTools.ParseUInt16(gc["y2"].Value);
				ushort minX;
				ushort maxX;
				ushort minY;
				ushort maxY;
				if (x1 < x2) {
					minX = x1;
					maxX = x2;
				} else {
					minX = x2;
					maxX = x1;
				}
				if (y1 < y2) {
					minY = y1;
					maxY = y2;
				} else {
					minY = y2;
					maxY = y1;
				}
				maxX--; maxY--; //this is because sphere has weird system of rectangle coordinates
				string retVal = string.Format("{0},{1},{2},{3}", minX, minY, maxX, maxY);
				def.Set("Rect", retVal, line.Comment);
				((ConvertedRegion) def).rectangles.Add(new ImmutableRectangle(minX, minY, maxX, maxY));
				//return retVal;
			} else {
				def.Warning(line.Line, "Unrecognized Rectangle format ('" + line.Value + "')");
			}
			//return "";
		}

		private static void ParseMapplane(ConvertedDef def, PropsLine line) {
			ConvertedRegion r = (ConvertedRegion) def;
			r.mapplane = ConvertTools.ParseByte(line.Value);
			r.mapplaneSet = true;
			r.mapplaneLine = line;
			//return "";
		}

		private static Point4DSaveImplementor pImplementor = new Point4DSaveImplementor();

		private static void ParseP(ConvertedDef def, PropsLine line) {
			ConvertedRegion r = (ConvertedRegion) def;
			Point4D p = Point4D.Parse(line.Value);
			if (!r.mapplaneSet) {
				r.mapplane = p.M;
			}
			string retVal = pImplementor.Save(p);
			def.Set("Spawnpoint", retVal, line.Comment);
			//return retVal;
		}

		private static void ParseFlags(ConvertedDef def, PropsLine line) {
			string name = line.Name;
			switch (line.Name.ToLowerInvariant()) {
				case "flagsafe":
					name = "Flag_safe";
					break;
				case "guarded":
					name = "flag_guarded";
					break;
				case "nopvp":
					name = "Flag_nopvp";
					break;
				case "nodecay":
					name = "Flag_nodecay";
					break;
				case "nobuild":
					name = "Flag_nobuild";
					break;
				case "underground":
					name = "Flag_underground";
					break;
			}

			int value = ConvertTools.ParseInt32(line.Value);
			if (value != 0) {//it is flagged region
				//if (def.headerType.StartsWith("World")) {
				//    def.headerType = "WorldFlaggedRegion";
				//} else {
				def.headerType = "FlaggedRegion";
				//}
				def.Set(name, line.Value, line.Comment);
				//return line.Value;
			}
			//return "";
		}

		public override void SecondStage() {
			int rectanglesCount = this.rectangles.Count;
			this.points = new Point2D[rectanglesCount * 4];
			for (int i = 0; i < rectanglesCount; i++) {
				ImmutableRectangle rect = this.rectangles[i];
				this.points[(i * 4) + 0] = new Point2D(rect.MinX, rect.MinY);//left lower
				this.points[(i * 4) + 1] = new Point2D(rect.MinX, rect.MaxY);//left upper
				this.points[(i * 4) + 2] = new Point2D(rect.MaxX, rect.MaxY);//right upper
				this.points[(i * 4) + 3] = new Point2D(rect.MaxX, rect.MinY);//right lower
			}

			List<DictionaryEntry> temp = new List<DictionaryEntry>();
			foreach (ConvertedRegion reg in allRegions) {
				if (this.HasSameMapplane(reg)) {
					int contained = reg.ContainsPoints(this.points);
					temp.Add(new DictionaryEntry(contained, reg));
				}
			}
			int tempCount = temp.Count;
			int highestResult = 0;
			int occurences = 0;
			for (int i = 0; i < tempCount; i++) {
				DictionaryEntry entry = temp[i];
				int result = (int) entry.Key;
				ConvertedRegion p = (ConvertedRegion) entry.Value;
				if ((this != p) && this.HasSameMapplane(p)) {
					if (result > highestResult) {
						occurences = 0;
						highestResult = result;
					}
					if (result == highestResult) {
						occurences++;
					}
				}
			}
			if ((occurences == 0) && (this.hierarchyIndex != 0)) {
				this.Warning(this.origData.HeaderLine, "Region " + this.headerName + " has no parents!");
			}
			this.parents = new ConvertedRegion[occurences];
			int index = 0;
			for (int i = 0; i < tempCount; i++) {
				DictionaryEntry entry = temp[i];
				int result = (int) entry.Key;
				ConvertedRegion p = (ConvertedRegion) entry.Value;
				if ((result == highestResult) && (p != this) && this.HasSameMapplane(p)) {
					this.parents[index] = p;
					index++;
				}
			}

			//Console.WriteLine("possible parents for "+this.headerName+" are "+Globals.ObjToString(parents));
		}

		public override void ThirdStage() {
			if (this.mapplaneLine != null) {
				this.Set("Mapplane", this.mapplane.ToString(), this.mapplaneLine.Comment);
			} else {
				this.Set("Mapplane", this.mapplane.ToString(), "");
			}
		}

		private bool HasSameMapplane(ConvertedRegion reg) {
			if (this.mapplane == reg.mapplane) {
				return true;
			}
			if (this.hierarchyIndex == 0) {
				return true;
			}
			if (reg.hierarchyIndex == 0) {
				return true;
			}
			return false;
		}

		private int ContainsPoints(Point2D[] ps) {
			int counter = 0;
			for (int i = 0, n = ps.Length; i < n; i++) {
				Point2D p = ps[i];
				foreach (ImmutableRectangle rect in this.rectangles) {
					if (rect.Contains(p)) {
						counter++;
					}
				}
			}
			return counter;
		}

		private bool TryDefinitiveParent() {
			if (this.hierarchyIndex != -1) {
				return true;
			}
			int highestHierarchyIndex = -2;
			int highestHierarchyIndexAt = -1;
			for (int i = 0, n = this.parents.Length; i < n; i++) {
				ConvertedRegion reg = this.parents[i];
				if (reg.hierarchyIndex == -1) {
					return false;
				}
				if (reg.hierarchyIndex > highestHierarchyIndex) {
					highestHierarchyIndex = reg.hierarchyIndex;
					highestHierarchyIndexAt = i;
				}
			}
			ConvertedRegion definitiveParent = this.parents[highestHierarchyIndexAt];
			this.parents = new[] { definitiveParent };
			this.hierarchyIndex = definitiveParent.hierarchyIndex + 1;
			this.Set("Parent", "(" + definitiveParent.headerName + ")", "calculated by Converter");
			//Console.WriteLine("Parent for "+this.headerName+"set to "+definitiveParent.headerName);
			return true;
		}

		//internal static void ResolveRegionsHierarchy() {
		public static void SecondStageFinished() {
			List<ConvertedRegion> temp = new List<ConvertedRegion>(allRegions);//copy list of all regions
			int lastCount = -1;
			while (temp.Count > 0) {
				if (lastCount == temp.Count) {
					//this will probably never happen
					Logger.WriteError("Region hierarchy not completely resolvable - No regions converted!");
					Logger.WriteInfo(ConverterMain.AdditionalConverterMessages, "These are the unresolved ones:");
					foreach (ConvertedRegion reg in temp) {
						reg.Info(reg.origData.HeaderLine, reg + ", possible parents: " + Tools.ObjToString(reg.parents));
					}

					foreach (ConvertedRegion reg in allRegions) {
						reg.DontDump();
					}
					return;
				}
				lastCount = temp.Count;
				for (int i = 0; i < temp.Count; ) {
					ConvertedRegion r = temp[i];
					if (r.TryDefinitiveParent()) {
						temp.RemoveAt(i);
					} else {
						i++;
					}
				}
			}
		}
		//		
		public override string ToString() {
			return "ConvertedRegion " + this.headerName + "(" + this.hierarchyIndex + ")";
		}
	}
}