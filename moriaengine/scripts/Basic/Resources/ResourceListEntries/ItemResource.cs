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
	/// <summary>Resource as ItemDef</summary>
	public class ItemResource : AbstractResourceListEntry, IResourceListEntry_ItemCounter {
		private readonly ItemDef itemDef;


		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given string as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			ItemDef idef = ThingDef.GetByDefname(definition) as ItemDef;
			if (idef != null) {
				resource = new ItemResource(idef, number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		internal ItemResource(ItemDef itemDef, double number, bool asPercentage)
			: base(number, asPercentage) {
			this.itemDef = itemDef;
		}

		public ItemDef ItemDef {
			get { return this.itemDef; }
		}

		public override string ParsableString {
			get {
				return this.itemDef.PrettyDefname;
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			ItemResource asIR = newOne as ItemResource;
			if (asIR != null) {
				return (this.itemDef == asIR.itemDef);
			}
			return false;
		}

		public override string GetResourceMissingMessage(Language language) {
			return string.Format(CultureInfo.InvariantCulture,
				Loc<ResListLoc>.Get(language).SkillTooLow, this.itemDef.PluralName);
		}

		#region IResourceListEntry_ItemCounter Members
		public ItemCounter GetCounter() {
			ItemCounter_ByItemDef ic = Pool<ItemCounter_ByItemDef>.Acquire();//get from pool
			ic.ListEntry = this;
			return ic;
		}
		#endregion
	}

	/// <summary>Counter of triggergroups resources</summary>
	public class ItemCounter_ByItemDef : ItemCounter<ItemResource> {
		internal override bool IsCorresponding(Item itm) {
			return (itm.TypeDef == this.ListEntry.ItemDef);
		}
	}
}