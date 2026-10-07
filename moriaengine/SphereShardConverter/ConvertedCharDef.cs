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


	public class ConvertedCharDef : ConvertedThingDef {
		public static Dictionary<string, ConvertedThingDef> charsByDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<int, ConvertedThingDef> charsByModel = new Dictionary<int, ConvertedThingDef>();
		//by model and by defnames


		private static LineImplTask[] firstStageImpl = {
				new LineImplTask("anim", MayBeHex_IgnorePoint), 
//TODO:
				new LineImplTask("npc", WriteAsComment),
				new LineImplTask("brain", WriteAsComment)
				


			};

		private static LineImplTask[] thirdStageImpl = {
			new LineImplTask("sound", HandleSound),
			new LineImplTask("mountid", HandleMountId),
			new LineImplTask("mountitem", HandleMountId),
			new LineImplTask("tag.mountid", HandleMountId)
																			  
		};

		public ConvertedCharDef(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.byModel = charsByModel;
			this.byDefname = charsByDefname;

			this.firstStageImplementations.Add(firstStageImpl);
			this.thirdStageImplementations.Add(thirdStageImpl);
		}

		public override void ThirdStage() {
			base.ThirdStage();

			switch (this.PrettyDefname.ToLowerInvariant()) {
				//case "c_man_gm":
				case "c_man":
				case "c_woman":
					this.headerType = "PlayerDef";
					break;
				default:
					this.headerType = "NPCDef";
					break;
			}
		}

		private static void HandleMountId(ConvertedDef def, PropsLine line) {
			int num = -1;
			if (!ConvertTools.TryParseInt32(line.Value, out num)) {
				ConvertedThingDef i;
				if (ConvertedItemDef.itemsByDefname.TryGetValue(line.Value, out i)) {
					num = i.Model;
				}
			}
			if (num != -1) {
				string retVal = "0x" + num.ToString("x");
				def.Set("MountItem", retVal, line.Comment);
				//return retVal;
			} else {
				def.Warning(line.Line, "Unresolvable MountItem model");
			}
			//return "";
		}


		private static void HandleSound(ConvertedDef def, PropsLine line) {
			string retVal = line.Value;
			int num;
			if (ConvertTools.TryParseInt32(line.Value, out num)) {
				retVal = "0x" + num.ToString("x");
				def.Set("AngerSound", retVal, line.Comment);
				def.Set("IdleSound", "0x" + (num + 1).ToString("x"), "");
				def.Set("AttackSound", "0x" + (num + 2).ToString("x"), "");
				def.Set("HurtSound", "0x" + (num + 3).ToString("x"), "");
				def.Set("DeathSound", "0x" + (num + 4).ToString("x"), "");
			} else {
				def.Set("AngerSound", retVal, line.Comment);
				def.Set("IdleSound", retVal + " + 1", "");
				def.Set("AttackSound", retVal + " + 2", "");
				def.Set("HurtSound", retVal + " + 3", "");
				def.Set("DeathSound", retVal + " + 4", "");
			}
			//return retVal;
		}
	}
}
