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

using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Resource as TriggerGroup (used only for 'type' checking of char's items), we dont use 
	/// resource triggergroups for checking character's available triggergroups!
	/// </summary>
	public class TriggerGroupResource : AbstractResourceListEntry, IResourceListEntry_ItemCounter {
		public readonly TriggerGroup triggerGroup;


		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given string as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			TriggerGroup tg = TriggerGroup.GetByDefname(definition);
			if (tg != null) {
				resource = new TriggerGroupResource(tg, number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		internal TriggerGroupResource(TriggerGroup triggerGroup, double number, bool asPercentage)
			: base(number, asPercentage) {
			this.triggerGroup = triggerGroup;
		}

		public override string ParsableString {
			get {
				return this.triggerGroup.PrettyDefname;
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			TriggerGroupResource newResource = newOne as TriggerGroupResource;
			if (newResource != null) {
				return (this.triggerGroup == newResource.triggerGroup);
			}
			return false;
		}

		public override string GetResourceMissingMessage(Language language) {
			return string.Format(CultureInfo.InvariantCulture,
				Loc<ResListLoc>.Get(language).NotEnoughItemsOfType, ItemTypeNames.GetPrettyName(this.triggerGroup));
		}


		#region IResourceListItemMultiplicable Members
		public ItemCounter GetCounter() {
			TriggerGroupsCounter tgc = Pool<TriggerGroupsCounter>.Acquire();//get from pool
			tgc.ListEntry = this;
			return tgc;
		}
		#endregion
	}

	/// <summary>Counter of triggergroups resources</summary>
	public class TriggerGroupsCounter : ItemCounter<TriggerGroupResource> {
		internal override bool IsCorresponding(Item itm) {
			return (itm.Type == this.ListEntry.triggerGroup);
		}
	}
}