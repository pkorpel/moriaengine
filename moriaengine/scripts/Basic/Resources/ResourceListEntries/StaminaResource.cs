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
	/// <summary>Resource as characters Stamina</summary>
	public class StatStamResource : AbstractResourceListEntry, IResourceListEntry_Simple {

		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given string as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			if ("Stam".Equals(definition, StringComparison.OrdinalIgnoreCase) ||
					"Stamina".Equals(definition, StringComparison.OrdinalIgnoreCase)) {
				resource = new StatStamResource(number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		internal StatStamResource(double number, bool asPercentage)
			: base(number, asPercentage) {
		}

		/// <summary>String from which the resource could be parsed again, used for saving</summary>
		public override string ParsableString {
			get {
				return "Stam";
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			return (newOne is StatStamResource);
		}

		public override string GetResourceMissingMessage(Language language) {
			return Loc<ResListLoc>.Get(language).StamTooLow;
		}

		#region IResourceListEntry_Simple Members
		public bool IsResourcePresent(Character chr)
		{
			if (this.AsPercentage) {
				return (chr.Stam * 100.0 / chr.MaxStam) >= this.DesiredCount;
			}
			return chr.Stam >= this.DesiredCount;
		}

		/// <summary>Indicates whether this is a consumable resource</summary>
		public bool IsConsumable { get { return true; } }

		/// <summary>Consumes this resource. Throws if this is not a consumable resource.</summary>
		public void Consume(Character ch) {
			if (this.AsPercentage) {
				ch.AddStamina(-(int) Math.Round(ch.Stam * this.DesiredCount / ch.MaxStam));
			} else {
				ch.AddStamina(-(int) Math.Round(this.DesiredCount));
			}
		}

		#endregion
	}
}