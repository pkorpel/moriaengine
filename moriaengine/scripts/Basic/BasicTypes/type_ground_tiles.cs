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

namespace SteamEngine.CompiledScripts {


	//t_rock is in type_buildings_floors.cs instead.

	public class t_dirt : GroundTileType {
		public override bool IsTypeOfMapTile(int mapTileId) {
			return (IsMapTileInRange(mapTileId, 0x0009, 0x0015)) ||
					(IsMapTileInRange(mapTileId, 0x0150, 0x015c));
		}
	}

	public class t_grass : GroundTileType {
		public override bool IsTypeOfMapTile(int mapTileId) {
			//This should be faster than having a bunch of [if (foo) return true;] lines.
			//But perhaps the ranges should be sorted by how many map tiles each has (not tileIDs, the actual number
			//of tiles on the map that are in each range), so that this would be even faster.
			//(so that the range with the most map tiles on the normal UO map would be first, the next most second, etc)
			//TODO: Find out how many tiles of each range are on the standard map 0, the other maps, custom maps, etc,
			//and see if this order can be optimized somehow for all of them (hmm...)
			return (IsMapTileInRange(mapTileId, 0x0003, 0x0006)) ||
					(IsMapTileInRange(mapTileId, 0x007d, 0x008c)) ||
					(IsMapTileInRange(mapTileId, 0x00c0, 0x00db)) ||
					(IsMapTileInRange(mapTileId, 0x00f8, 0x00fb)) ||
					(IsMapTileInRange(mapTileId, 0x015d, 0x0164)) ||
					(IsMapTileInRange(mapTileId, 0x01a4, 0x01a7)) ||
					(IsMapTileInRange(mapTileId, 0x0231, 0x0234)) ||
					(IsMapTileInRange(mapTileId, 0x0239, 0x0243)) ||
					(IsMapTileInRange(mapTileId, 0x0324, 0x032b)) ||
					(IsMapTileInRange(mapTileId, 0x036f, 0x0376)) ||
					(IsMapTileInRange(mapTileId, 0x037b, 0x037e)) ||
					(IsMapTileInRange(mapTileId, 0x03bf, 0x03c6)) ||
					(IsMapTileInRange(mapTileId, 0x03cb, 0x03ce)) ||
					(IsMapTileInRange(mapTileId, 0x0579, 0x0580)) ||
					(IsMapTileInRange(mapTileId, 0x058b, 0x058c)) ||
					(IsMapTileInRange(mapTileId, 0x05e3, 0x0604)) ||
					(IsMapTileInRange(mapTileId, 0x066b, 0x066e)) ||
					(IsMapTileInRange(mapTileId, 0x069d, 0x0684)) ||
					(IsMapTileInRange(mapTileId, 0x0695, 0x069c)) ||
					(IsMapTileInRange(mapTileId, 0x06a1, 0x06c2)) ||
					(IsMapTileInRange(mapTileId, 0x06d2, 0x06d9)) ||
					(IsMapTileInRange(mapTileId, 0x06de, 0x06e1));
		}
	}

	public class t_lava : GroundTileType {
		public override bool IsTypeOfMapTile(int mapTileId) {
			return (IsMapTileInRange(mapTileId, 0x01f4, 0x01f7));
		}
	}

	public class t_water : CompiledTriggerGroup {
		//public override bool IsTypeOfMapTile(int mapTileId) {
		//	return	(IsMapTileInRange(mapTileId, 0x00a8, 0x00ac)) ||
		//			(IsMapTileInRange(mapTileId, 0x0136, 0x0137));
		//}
	}

	public class t_rock : GroundTileType {
		public override bool IsTypeOfMapTile(int mapTileId) {
			//This should be faster than having a bunch of [if (foo) return true;] lines.
			//But perhaps the ranges should be sorted by how many map tiles each has (not tileIDs, the actual number
			//of tiles on the map that are in each range), so that this would be even faster.
			//(so that the range with the most map tiles on the normal UO map would be first, the next most second, etc)
			return (IsMapTileInRange(mapTileId, 0x00dc, 0x00e7)) ||
					(IsMapTileInRange(mapTileId, 0x00ec, 0x00f7)) ||
					(IsMapTileInRange(mapTileId, 0x00fc, 0x0107)) ||
					(IsMapTileInRange(mapTileId, 0x010c, 0x0117)) ||
					(IsMapTileInRange(mapTileId, 0x011e, 0x0129)) ||
					(IsMapTileInRange(mapTileId, 0x0141, 0x0144)) ||
					(IsMapTileInRange(mapTileId, 0x021f, 0x0243)) ||
					(IsMapTileInRange(mapTileId, 0x024a, 0x0257)) ||
					(IsMapTileInRange(mapTileId, 0x0259, 0x026d));
		}
	}
}