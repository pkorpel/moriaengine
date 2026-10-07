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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Class containing all main crafting categories (according to the crafting skills).
	/// Each skill has one main category for its items and possible subcategories
	/// </summary>
	[HasSavedMembers]
	public static class CraftmenuContents {
		[SavedMember]
		private static readonly Dictionary<SkillName, CraftmenuCategory> mainCategories = new Dictionary<SkillName, CraftmenuCategory>();

		public static Dictionary<SkillName, CraftmenuCategory> MainCategories {
			get {
				if (mainCategories.Count == 0) {//yet empty - first access, initialize it
					for (int i = 0, n = AbstractSkillDef.SkillsCount; i < n; i++) {
						CraftingSkillDef csk = AbstractSkillDef.GetById(i) as CraftingSkillDef;
						if (csk != null) {
							//add only Crafting skills...
							mainCategories[(SkillName) i] = new CraftmenuCategory(csk);
						}
					}
				}
				return mainCategories;
			}
		}

		//method for lazy loading the ICraftmenuElement parental info, used only when accessing some element's parent which is yet null
		internal static void TryLoadParents() {
			foreach (CraftmenuCategory mainCat in mainCategories.Values) { //these dont have Parents...
				ResolveChildParent(mainCat);
				mainCat.Parent = mainCat; //main categories will have themselves as parents...
				mainCat.categorySkill = (CraftingSkillDef) AbstractSkillDef.GetByKey(mainCat.Name); //main categories have the skill Key as their name
				mainCat.isLoaded = true; //loaded, now if the parental reference is null then the category should be taken as deleted
			}
		}

		//for the given category iterate through all of its children and set itself as parent to them
		//continue recursively into subcategories
		private static void ResolveChildParent(CraftmenuCategory ofWhat) {
			foreach (ICraftmenuElement elem in ofWhat.Contents) {
				CraftmenuCategory elemCat = elem as CraftmenuCategory;
				if (elemCat != null) {//we have the subcategory - resolve parents inside
					elemCat.Parent = ofWhat; //set the subcategory's parent
					elemCat.isLoaded = true; //loaded, now if the parental reference is null then the category should be taken as deleted
					ResolveChildParent(elemCat); //and recurse into children
				} else {//we have the item
					((CraftmenuItem) elem).Parent = ofWhat;
				}
			}
		}
	}

	/// <summary>One line in the craftmenu - can be either the category of items or the particular item(def) itself</summary>
	public interface ICraftmenuElement {
		string Name {
			get;
		}

		bool IsCategory {
			get;
		}

		CraftmenuCategory Parent {
			get;
		}

		void Remove();

		void Bounce(AbstractItem whereto);
	}


	/// <summary>
	/// Craftmenu category class. This is the entity where all Items from the menu are stored as well 
	/// as it is a container for another subcategories
	/// </summary>
	[ViewableClass]
	[SaveableClass]
	public class CraftmenuCategory : ICraftmenuElement, IDeletable {
		internal bool isLoaded;

		//lazily loaded reference to the crafting skill connected to this category (main categories only)
		internal CraftingSkillDef categorySkill;

		[SaveableData]
		public string name; //name of the category
		[SaveableData]
		public List<ICraftmenuElement> contents = new List<ICraftmenuElement>(); //itemdefs and subcategorties contained in the category
		private CraftmenuCategory parent;

		[LoadingInitializer]
		public CraftmenuCategory() {
		}

		public CraftmenuCategory(string name) {
			this.name = name;
		}

		//constructor used for main categories only
		internal CraftmenuCategory(CraftingSkillDef csd)
			: this(csd.Key) {
			this.categorySkill = csd;
		}

		/// <summary>Get name compound also from the possible parent's name (if any)</summary>
		public string FullName {
			get {
				this.ThrowIfDeleted();
				if (this.Parent == this) {//main Categories have Parental reference on themselves
					return this.name;
				}
				return this.Parent.FullName + "->" + this.name;
			}
		}

		public List<ICraftmenuElement> Contents {
			get {
				return this.contents;
			}
		}

		public bool IsLoaded {
			get {
				return this.isLoaded;
			}
		}

		/// <summary>Return the (grand)parent from this category that lies on the first hierarchy level</summary>
		public CraftmenuCategory MainParent {
			get
			{
				if (this.Parent == this) {
					return this;
				}
				return this.Parent.MainParent;
			}
		}

		/// <summary>Return the category's skill which is used for crafting items from it</summary>
		public CraftingSkillDef CategorySkill {
			get {
				return this.MainParent.categorySkill;
			}
		}

		public override string ToString() {
			return "CraftmenuCategory " + this.Name;
		}

		#region ICraftmenuElement Members
		public string Name {
			get {
				return this.name;
			}
		}

		public bool IsCategory {
			get {
				return true;
			}
		}

		public CraftmenuCategory Parent {
			get {
				if (!this.isLoaded) {
					//if the parent is null and we aren't working with one of the main categories, try load all craftmenu elements' parental hierarchy
					CraftmenuContents.TryLoadParents();
				}
				return this.parent;
			}
			internal set {
				this.parent = value;
			}
		}

		/// <summary>Category cleaning method - clear the contents and remove from the parent</summary>
		public void Remove() {
			if (this.Parent != null) {
				this.Parent.Contents.Remove(this); //remove from the parent's hierarchy list
			}
			foreach (ICraftmenuElement subElem in this.contents) {
				subElem.Remove();//remove every element in the removed category (incl. subcategories)
			}
			this.Delete(); //will clear the reference to the parent and disable the overall usage as favourite category etc.
		}

		/// <summary>After removing the category from the craftmenu, create a pouch for it, put it into the specified location and bounce all inside items into it</summary>
		public void Bounce(AbstractItem whereto) {
			Item newPouch = (Item) ThingDef.GetByDefname("i_pouch").Create(whereto);
			newPouch.Name = this.Name;
			foreach (ICraftmenuElement innerElem in this.Contents) {
				innerElem.Bounce(newPouch);
			}
		}
		#endregion

		#region IDeletable Members

		public bool IsDeleted {
			get {
				return (this.isLoaded && this.parent == null);//has been loaded but the parent is null? (this can happen only if the category was deleted)
			}
		}

		public void Delete() {
			//method is called when the category is removed from the categories hierarchy
			//but there can exist references on it (favourite categroy etc...) so we need to mark it somehow
			//in order to disable its usage anymore
			this.isLoaded = true; //consider it as loaded (but in time of deleting it should be loaded anyways)
			this.parent = null;
		}
		#endregion

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public void ThrowIfDeleted() {
			if (this.IsDeleted) {
				throw new DeletedException("Invalid usage of deleted Craftmenu Category (" + this + ")");
			}
		}
	}

	/// <summary>Craftmenu item class. This is the entity representing a single Item in the craftmenu</summary>
	[SaveableClass]
	[ViewableClass]
	public class CraftmenuItem : ICraftmenuElement {
		[SaveableData]
		public ItemDef itemDef;
		private CraftmenuCategory parent;

		[LoadingInitializer]
		public CraftmenuItem() {
		}

		public CraftmenuItem(ItemDef itemDef) {
			this.itemDef = itemDef;
		}

		#region ICraftmenuElement Members
		public string Name {
			get {
				return this.itemDef.Name;
			}
		}

		public bool IsCategory {
			get {
				return false;
			}
		}

		/// <summary>CraftmenuItem must always be in some CraftmenuCategory!</summary>
		public CraftmenuCategory Parent {
			get {
				if (this.parent == null) {
					//if the parent is null try to load the parental hierarchy
					CraftmenuContents.TryLoadParents();
				}
				return this.parent;
			}
			internal set {
				this.parent = value;
			}
		}

		/// <summary>Remove the item from the parent's list</summary>
		public void Remove() {
			if (this.Parent != null) {
				this.Parent.Contents.Remove(this);
				this.parent = null;
			}
		}

		/// <summary>Bouncing of the item means creating an instance and putting to the specified location</summary>
		public void Bounce(AbstractItem whereto) {
			Item newItm = (Item) this.itemDef.Create(whereto);
		}
		#endregion
	}

	/// <summary>Target for targetting single items or containers containing items to be added to the craftmenu (to the selected category)</summary>
	public class Targ_Craftmenu : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamìø pøedmìt pøípadnì konejner s pøedmìty pro pøidání do craftmenu.");
			base.On_Start(self, parameter);
		}

		//put the given item into the category. if the item is a non-empty container, make it a subcategory
		//and add its contents recursively
		private void Encategorize(Item oneItm, CraftmenuCategory whereto) {
			if (oneItm.IsContainer && oneItm.Count > 0) {//make it a subcategory
				//use the container's name for the category name - it is up to user to name all containers properly..
				CraftmenuCategory newSubcat = new CraftmenuCategory(oneItm.Name);
				newSubcat.Parent = whereto;
				whereto.Contents.Add(newSubcat);
				foreach (Item inner in oneItm) {
					this.Encategorize(inner, newSubcat);//proceed with every found item
				}
			} else {//normal item or an empty container
				CraftmenuItem newItem = new CraftmenuItem((ItemDef) oneItm.Def);//add the contained items
				newItem.Parent = whereto;
				whereto.Contents.Add(newItem);
			}
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			CraftmenuCategory catToPut = (CraftmenuCategory) parameter;

			this.Encategorize(targetted, catToPut);

			//reopen the dialog on the stored position
			Dictionary<CraftingSkillDef, CraftmenuCategory> lastPosDict = (Dictionary<CraftingSkillDef, CraftmenuCategory>) self.GetTag(D_Craftmenu.TkLastCat);
			CraftmenuCategory prevCat = null;
			if (lastPosDict != null) {
				prevCat = lastPosDict[catToPut.CategorySkill];
			}
			if (prevCat != null) {//not null means that the category was not deleted and can be accessed again
				self.Dialog(SingletonScript<D_Craftmenu>.Instance, new DialogArgs(prevCat));
			} else {//null means that it either not existed (the tag) or the category was deleted from the menu
				self.Dialog(SingletonScript<D_CraftmenuCategories>.Instance);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>Class for describing one of the selected items and its count to be crafted</summary>
	public class CraftingSelection {
		private readonly ItemDef itemDef;
		private int count;

		public CraftingSelection(ItemDef itemDef, int count) {
			this.itemDef = itemDef;
			this.count = count;
		}

		public ItemDef ItemDef {
			get {
				return this.itemDef;
			}
		}

		public int Count {
			get {
				return this.count;
			}
			set {
				this.count = value;
			}
		}
	}

	/// <summary>
	/// Class encapsulating one instance of the crafting 'order list' - the queue of the CraftingSelections 
	/// and the required used skill.
	/// </summary>
	public class CraftingOrder {
		private readonly CraftingSkillDef craftingSkill;
		private SimpleQueue<CraftingSelection> selectionQueue;

		public CraftingOrder(CraftingSkillDef craftingSkill, SimpleQueue<CraftingSelection> selectionQueue) {
			this.craftingSkill = craftingSkill;
			this.selectionQueue = selectionQueue;
		}

		public CraftingSkillDef CraftingSkill {
			get {
				return this.craftingSkill;
			}
		}

		public SimpleQueue<CraftingSelection> SelectionQueue {
			get {
				return this.selectionQueue;
			}
		}
	}
}