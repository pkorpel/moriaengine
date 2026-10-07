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
	/// <summary>Interface for single resource stored in resource lists</summary>
	public interface IResourceListEntry {
		/// <summary>Number specified in the script (85.5 hiding, 5 i_apples etc)</summary>
		double DesiredCount {
			get;
		}

		/// <summary>String from which the resource could be parsed again, used for saving</summary>
		string ParsableString {
			get;
		}

		/// <summary>Check if the 'newOne' is the same resource as the actual one.</summary>
		bool IsSameResource(IResourceListEntry newOne);

		/// <summary>
		/// Determines whether the resource shall be checked / consumed as precents of all available (true) or only 
		/// in absolute values specified (false)
		///  </summary>
		bool AsPercentage { get; }

		/// <summary>In case some resource is missing, the mising item can be used for sending some informational message...</summary>
		string GetResourceMissingMessage(Language language);
	}

	/// <summary>
	/// Interface for resource list items that cannot be multiplicated (e.g. Ability - if the resourcelist 
	/// demands 5 a_warcry then it makes no sense using the reslist repeatedly and demad 10 a_warcry (unlike e.g. i_apple)
	/// </summary>
	public interface IResourceListEntry_Simple : IResourceListEntry {
		/// <summary>
		/// Does the character have this particular resource? (in desired amount). Check only the presence 
		/// do not consume.
		/// </summary>
		bool IsResourcePresent(Character chr);

		/// <summary>Indicates whether this is a consumable resource</summary>
		bool IsConsumable { get; }

		/// <summary>Consumes this resource. Throws if this is not a consumable resource.</summary>
		void Consume(Character ch);
	}

	/// <summary>
	/// Interface for single resource stored in resource lists. These items can be multiplicable - 
	/// e.g. itemdefs, allowing us to say 'how many times the resourcelist has been found at the char's
	/// (usable for crafting more than 1 item at a time e.t.c
	/// </summary>
	public interface IResourceListEntry_ItemCounter : IResourceListEntry {
		/// <summary>
		/// Return the resource counter object for this resource, we are using the Object Pool pattern 
		/// for acquiring and storing desired instances
		/// </summary>
		ItemCounter GetCounter();
	}

	/// <summary>Convenience base class for IResourceListItem implementors.</summary>
	public abstract class AbstractResourceListEntry : IResourceListEntry {
		protected AbstractResourceListEntry(double desiredCount, bool asPercentage) {
			this.DesiredCount = desiredCount;
			this.AsPercentage = asPercentage;
		}

		public bool AsPercentage {
			get;
			private set;
		}

		public double DesiredCount {
			get;
			private set;
		}

		public abstract string ParsableString { get; }

		public abstract bool IsSameResource(IResourceListEntry newOne);

		public abstract string GetResourceMissingMessage(Language language);
	}
}
