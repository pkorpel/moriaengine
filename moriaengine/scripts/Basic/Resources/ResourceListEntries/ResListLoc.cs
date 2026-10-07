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

using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	public class ResListLoc : CompiledLocStringCollection<ResListLoc> {
		public string NotEnoughItems = "Máš málo {0}";
		public string NotEnoughItemsOfType = "Máš málo pøedmìtù typu {0}";
		public string SkillTooLow = "Umení {0} ovládáš pøíliš špatnì";
		public string AbilityTooLow = "Schopnost {0} ovládáš pøíliš špatnì";
		public string DexTooLow = "Nejsi dost obratný";
		public string IntTooLow = "Nejsi dost inteligentní";
		public string VitTooLow = "Nejsi dost vitální";
		public string StrTooLow = "Nejsi dost silný";
		public string HitsTooLow = "Jsi pøíliš zranìný (málo hitpointù)";
		public string ManaTooLow = "Máš málo many";
		public string StamTooLow = "Jsi pøíliš vyèerpaný (málo staminy)";
		public string LevelTooLow; 

	}
}