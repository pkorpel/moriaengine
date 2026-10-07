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
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Abstract class - parent of counter classes for multiplicable resources (such as Items, TriggerGroups) 
	/// used for storing information about found char's corresponding items and their amount 
	/// allowing us also to consume the desired amount of found items
	/// </summary>
	public abstract class ItemCounter : Poolable {
		public IResourceListEntry_ItemCounter listEntry;

		private List<Item> foundItems = new List<Item>();
		private int foundCount; //how many occurences have been found so far? (counted from amounts of found items)


		/// <summary>
		/// Check if the given item corresponds to this particular resource item (i.e. is of the same 
		/// "itemdef, has the desired triggergroup (as type) etc)
		/// </summary>
		internal abstract bool IsCorresponding(Item itm);

		/// <summary>Add the item to the found items list and count its amount for resource's multiplicity determining</summary>
		internal void IncludeItem(Item itm) {
			this.foundItems.Add(itm); //add to the list
			this.foundCount += itm.Amount;
		}

		/// <summary>How many times can we consume the desired amount of resources?</summary>
		internal double Multiplicity {
			get {
				return (this.foundCount / this.DesiredCount);
			}
		}

		/// <summary>
		/// Look to the foundItems list and consume desiredCount*howManyTimes items found inside. 
		/// These should be available via their 'amounts'
		/// </summary>
		internal void ConsumeItems(double howManyTimes) {
			long toConsume = (long) (this.DesiredCount * howManyTimes);
			foreach (Item itm in this.foundItems) {
				//try consume as much as possible of this item
				long wasConsumed = itm.Consume(toConsume);
				toConsume -= wasConsumed;
				if (toConsume < 1) {
					break; //desired amount has been already consumed, stop iterating
				}
			}
		}

		/// <summary>
		/// Check how many items we have available for consuming and prepare a random number to consume 
		/// (but do not take more than the desired count in the resource list). 
		/// Then consume it
		/// </summary>
		internal void ConsumeSomeItems() {
			int available = (int) Math.Min(this.foundCount, this.DesiredCount); //we can consume at most the really desired count (but no more)
			long toConsume = (long) Math.Round((new Random().NextDouble()) * available);
			foreach (Item itm in this.foundItems) {
				//try consume as much as possible of this item
				long wasConsumed = itm.Consume(toConsume);
				toConsume -= wasConsumed;
				if (toConsume == 0) {
					break; //desired amount has been already consumed, stop iterating
				}
			}
		}

		//clear the desired count, the found count information and clear the 
		//list of found items
		protected override void On_Reset() {
			base.On_Reset();
			this.listEntry = null;
			this.foundCount = 0;
			this.foundItems.Clear();//clear the list (but let it keep its actual size!)
		}

		public double DesiredCount {
			get {
				return this.listEntry.DesiredCount;
			}
		}
	}

	public abstract class ItemCounter<T> : ItemCounter where T : IResourceListEntry_ItemCounter {

		/// <summary>ListItem this resource counter is for.</summary>
		internal T ListEntry {
			get {
				return (T) this.listEntry;
			}
			set {
				this.listEntry = value;
			}
		}
	}
}
