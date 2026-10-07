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

namespace SteamEngine.CompiledScripts {
	/// <summary>Resource as AbilityDef</summary>
	public class AbilityResource : AbstractResourceListEntry, IResourceListEntry_Simple {
		private AbilityDef abilityDef;

		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given string as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			AbilityDef abl = AbilityDef.GetByDefname(definition);
			if (abl != null) {
				resource = new AbilityResource(abl, number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		internal AbilityResource(AbilityDef abilityDef, double number, bool asPercentage)
			: base(number, asPercentage) {

			this.abilityDef = abilityDef;

			if (asPercentage) {
				throw new SEException("Cannot use ability as percentage resource");
			}
		}

		public override string ParsableString {
			get {
				return this.abilityDef.PrettyDefname;
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			AbilityResource newResource = newOne as AbilityResource;
			if (newResource != null) {
				return (this.abilityDef == newResource.abilityDef);
			}
			return false;
		}

		public override string GetResourceMissingMessage(Language language) {
			return string.Format(CultureInfo.InvariantCulture,
				Loc<ResListLoc>.Get(language).AbilityTooLow, this.abilityDef.Name);
		}

		#region IResourceListEntry_Simple Members
		public bool IsResourcePresent(Character chr) {
			//method returns number of ability points or 0 if we dont have the ability
			return chr.GetAbility(this.abilityDef) >= this.DesiredCount;
		}

		/// <summary>Indicates whether this is a consumable resource</summary>
		public bool IsConsumable { get { return false; } }

		/// <summary>Consumes this resource. Throws if this is not a consumable resource.</summary>
		public void Consume(Character ch) {
			throw new InvalidOperationException("Abilities can not be consumed as a resource.");
		}
		#endregion
	}
}