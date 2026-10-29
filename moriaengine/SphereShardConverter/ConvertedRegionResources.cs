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
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.Converter {

	/// <summary>#813 - sphereregion.scp's [RegionResource x]: one gatherable resource (SKILL range,
	/// AMOUNT pool, REAP item, REGEN time). Becomes [RegionResourceDef x], read by the engine's
	/// RegionResourceDef. REGEN's Sphere expression ("60*60*1") is evaluated to seconds here.</summary>
	public class ConvertedRegionResource : ConvertedDef {
		private static readonly LineImplTask[] firstStageImpl = {
			new LineImplTask("skill", PassThrough),
			new LineImplTask("amount", PassThrough),
			new LineImplTask("reap", PassThrough),
			new LineImplTask("regen", ParseRegen),
		};

		public ConvertedRegionResource(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.headerType = "RegionResourceDef";
			this.firstStageImplementations.Add(firstStageImpl);
		}

		private static void PassThrough(ConvertedDef def, PropsLine line) {
			def.Set(line.Name, line.Value.Trim(), line.Comment);
		}

		private static void ParseRegen(ConvertedDef def, PropsLine line) {
			def.Set("RegenSeconds", EvaluateProduct(line.Value).ToString(CultureInfo.InvariantCulture), line.Comment);
		}

		//Sphere's REGEN is a plain product like 60*60*1 (or a single number)
		internal static int EvaluateProduct(string expression) {
			int result = 1;
			foreach (string factor in expression.Split('*')) {
				result *= ConvertTools.ParseInt32(factor.Trim());
			}
			return result;
		}
	}

	/// <summary>#813 - sphereregion.scp's [RegionType r_x t_type] with RESOURCES= lines: what tiles
	/// of one item type (t_rock/t_tree/t_water/t_grass) yield in the regions listing it, as
	/// weighted RegionResources. Becomes [RegionTypeDef r_x] with ItemType and the weighted lines.
	/// Region types WITHOUT resources are Sphere region events (trigger scripts) - left commented
	/// out here; the engine ports the ones it needs in C#.</summary>
	public class ConvertedRegionType : ConvertedDef {
		private static readonly LineImplTask[] firstStageImpl = {
			new LineImplTask("resources", ParseResources),
		};

		private bool hasResources;

		public ConvertedRegionType(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			string[] header = input.HeaderName.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			this.headerType = "RegionTypeDef";
			this.headerName = header[0];
			if (header.Length > 1) {
				this.Set("ItemType", header[1], "");
			}
			this.firstStageImplementations.Add(firstStageImpl);
		}

		private static void ParseResources(ConvertedDef def, PropsLine line) {
			((ConvertedRegionType) def).hasResources = true;
			def.Set("Resources", line.Value.Trim(), line.Comment);
		}

		public override void FirstStage() {
			base.FirstStage();
			if (!this.hasResources) {
				this.DontDump(); //an event-only region type
			}
		}
	}
}
