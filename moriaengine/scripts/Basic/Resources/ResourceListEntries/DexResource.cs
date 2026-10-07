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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>Resource as characters Dexterity</summary>
	public class StatDexResource : AbstractResourceListEntry, IResourceListEntry_Simple {

		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given string as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			if ("dex".Equals(definition, StringComparison.OrdinalIgnoreCase) ||
					"dexterity".Equals(definition, StringComparison.OrdinalIgnoreCase)) {
				resource = new StatDexResource(number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		private StatDexResource(double number, bool asPercentage)
			: base(number, asPercentage) {

			if (asPercentage) {
				throw new SEException("Cannot use dexterity as percentage resource");
			}
		}

		/// <summary>String from which the resource could be parsed again, used for saving</summary>
		public override string ParsableString {
			get {
				return "dex";
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			return (newOne is StatDexResource);
		}

		public override string GetResourceMissingMessage(Language language) {
			return Loc<ResListLoc>.Get(language).DexTooLow;
		}

		#region IResourceListEntry_Simple Members
		public bool IsResourcePresent(Character chr) {
			return chr.Dex >= this.DesiredCount;
		}

		/// <summary>Indicates whether this is a consumable resource</summary>
		public bool IsConsumable { get { return false; } }

		/// <summary>Consumes this resource. Throws if this is not a consumable resource.</summary>
		public void Consume(Character ch) {
			throw new InvalidOperationException("Dexterity can not be consumed as a resource.");
		}
		#endregion
	}
}