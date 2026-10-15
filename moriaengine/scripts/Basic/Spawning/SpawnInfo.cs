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

using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	[SaveableClass, DeepCopyableClass]
	public sealed class SpawnInfo {

		[SaveableData, CopyableData]
		public IThingFactory SpawnDef;

		[SaveableData, CopyableData]
		public short Amount;

		[SaveableData, CopyableData]
		public short MinTime;

		[SaveableData, CopyableData]
		public short MaxTime;

		[SaveableData, CopyableData]
		public short Homedist;


		[LoadingInitializer, DeepCopyImplementation]
		public SpawnInfo() {
		}

		public SpawnInfo(short amount, IThingFactory spawnDef, short minTime, short maxTime, short homedist) {
			this.Amount = amount;
			this.SpawnDef = spawnDef;
			this.MinTime = minTime;
			this.MaxTime = maxTime;
			this.Homedist = homedist;
		}

		/// <summary>Null-safe display name for the spawn dialog's Baseid field - SpawnDef is null
		/// whenever the GM typed a chardef name that doesn't resolve (AbstractDef.GetByDefname
		/// returning null), which the dialog must still be able to redisplay without a NRE.</summary>
		public string SpawnDefDisplayName {
			get {
				ThingDef def = this.SpawnDef as ThingDef;
				return def == null ? "" : def.PrettyDefname;
			}
		}

		/// <summary>True iff SpawnDef resolved to an actual chardef - checked here (short-circuit
		/// in plain C#) rather than as "SpawnDef == 0 || !SpawnDef.IsCharDef" in LScript, since
		/// LScript's "||" evaluates both operands unconditionally and a null SpawnDef would throw
		/// a NullReferenceException on the second operand.</summary>
		public bool HasValidCharDef {
			get {
				ThingDef def = this.SpawnDef as ThingDef;
				return def != null && def.IsCharDef;
			}
		}
	}
}