/*
	This program is free software; you can rediHitsibute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is diHitsibuted in the hope that it will be useful,
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
	/// <summary>Resource as characters Hitsength</summary>
	public class StatHitsResource : AbstractResourceListEntry, IResourceListEntry_Simple {

		public static void BootHitsap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given String as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			if ("Hits".Equals(definition, StringComparison.OrdinalIgnoreCase) ||
					"Hitpoints".Equals(definition, StringComparison.OrdinalIgnoreCase)) {
				resource = new StatHitsResource(number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		internal StatHitsResource(double number, bool asPercentage)
			: base(number, asPercentage) {
		}

		/// <summary>String from which the resource could be parsed again, used for saving</summary>
		public override string ParsableString {
			get {
				return "hits";
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			return (newOne is StatHitsResource);
		}

		public override string GetResourceMissingMessage(Language language) {
			return Loc<ResListLoc>.Get(language).HitsTooLow;
		}

		#region IResourceListEntry_Simple Members
		public bool IsResourcePresent(Character chr)
		{
			if (this.AsPercentage) {
				return (chr.Hits * 100.0 / chr.MaxHits) >= this.DesiredCount;
			}
			return chr.Hits >= this.DesiredCount;
		}

		/// <summary>Indicates whether this is a consumable resource</summary>
		public bool IsConsumable { get { return true; } }

		/// <summary>Consumes this resource. Throws if this is not a consumable resource.</summary>
		public void Consume(Character ch) {
			if (this.AsPercentage) {
				ch.AddHits(-(int) Math.Round(ch.Hits * this.DesiredCount / ch.MaxHits));
			} else {
				ch.AddHits(-(int) Math.Round(this.DesiredCount));
			}
		}

		#endregion
	}
}