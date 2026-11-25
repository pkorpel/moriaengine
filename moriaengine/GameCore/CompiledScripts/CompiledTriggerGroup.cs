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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/*
		Class: CompiledTriggerGroup
			.NET scripts should extend this class, and make use of its features.
			This class provides automatic linking of methods intended for use as triggers
	*/
	public abstract class CompiledTriggerGroup : TriggerGroup {
		public override object Run(object self, TriggerKey tk, ScriptArgs sa) {
			throw new SEException("CompiledTriggerGroup without overriden Run method?! This should not happen.");
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public sealed override object TryRun(object self, TriggerKey tk, ScriptArgs sa) {
			try {
				return this.Run(self, tk, sa);
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
			return null;
		}

		protected override string InternalFirstGetDefname() {
			return this.GetType().Name;
		}

		//public override void Unload() {
		//    //we do nothing. Throwing exception is rude to AbstractScript.UnloadAll
		//    //and doing base.Unload() would be a lie cos we can't really unload.
		//}
	}

	//Implemented by the types which can represent map tiles
	//like t_water and such
	//more in the Map class
	//if someone has a better idea about how to do this ...
	public abstract class GroundTileType : CompiledTriggerGroup {

		public new static GroundTileType GetByDefname(string name) {
			return AbstractScript.GetByDefname(name) as GroundTileType;
		}

		public static bool IsMapTileInRange(int tileId, int aboveOrEqualTo, int below) {
			return (tileId >= aboveOrEqualTo && tileId <= below);
		}

		public abstract bool IsTypeOfMapTile(int mapTileId);
	}
}