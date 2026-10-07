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
using System.Collections;
using System.Collections.Generic;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {



	/* sphere Flags:
	 * 0x0001: Stolen (Drop on death)
	 * 0x0002: Decay
	 * 0x0004: Newbied
	 * 0x0008: Always Movable
	 * 0x0010: Never Movable
	 * 0x0020: Magic
	 * 0x0040: Static (For moving stuff to the statics MULs, perhaps?)
	 * 0x0080: Invisible
	 * 0x0100: Ignored by NearbyItems
	 * 0x0200: Blocks LOS
	 * 0x0400: Provides partial cover (Doesn't block LOS but does assess combat penalties)
	 * */

	//public enum ItemFlags : byte {
	//    None = 0x00,
	//    Disconnecned = 0x01, //reserved by core

	//    Newbied = 0x04,

	//    DisplayAsMovable = 0x08, AlwaysMovable = DisplayAsMovable,
	//    NonMovable = 0x10, NeverMovable = NonMovable,

	//    Invisible = 0x80,
	//}

	[ViewableClass]
	public partial class Item : AbstractItem {
		/// <summary>
		/// Consume desired amount of this item, amount cannot go below zero. If resulting amount is 0 
		///  then the item will be deleted. Method returns the actually consumed amount.
		///  </summary>
		public long Consume(long howMany) {
			int prevAmount = this.Amount;

			long resultAmount;
			try {
				resultAmount = checked(prevAmount - howMany);
			} catch (OverflowException) {
				resultAmount = -1; //so we delete
			}

			if (resultAmount < 1) {
				this.Delete();
				return prevAmount;//consumed all of the item (not necesarilly the whole "howMuch")
			}
			this.Amount = (int) resultAmount;
			return howMany; //consumed the desired amount
		}

		public override byte FlagsToSend {
			get {
				int retVal = 0;

				if (this.IsNotVisible) {
					retVal |= 0x80;
				}

				if (this.Flag_DisplayAsMovable) {
					retVal |= 0x20;
				}

				return (byte) retVal;
			}
		}

		public bool Flag_Invisible {
			get {
				return this.ProtectedFlag1;
			}
			set {
				if (this.ProtectedFlag1 != value) {
					if (value) {
						OpenedContainers.SetContainerClosed(this);
						this.RemoveFromView();
					}
					ItemSyncQueue.AboutToChange(this);
					this.ProtectedFlag1 = value;
				}
			}
		}

		public bool Flag_Newbied {
			get {
				return this.ProtectedFlag2;
			}
			set {
				this.ProtectedFlag2 = value;
			}
		}

		public bool Flag_DisplayAsMovable {
			get {
				return this.ProtectedFlag3;
			}
			set {
				if (this.ProtectedFlag3 != value) {
					ItemSyncQueue.AboutToChange(this);
					this.ProtectedFlag3 = value;
				}
			}
		}

		public sealed override bool Flag_NonMovable {
			get {
				return this.ProtectedFlag4;
			}
			set {
				this.ProtectedFlag4 = value;
			}
		}

		public sealed override bool IsNotVisible {
			get {
				return (this.Flag_Invisible || !this.IsInVisibleLayer || this.Flag_Disconnected);
			}
		}

		public virtual bool IsMusicalInstrument {
			get {
				return false;
			}
		}

		/// <summary>Enumerates every item in this item and items in all subcontainers, recurses into infinite depth.</summary>
		public IEnumerable<Item> EnumDeep() {
			this.ThrowIfDeleted();
			IEnumerator e = this.GetEnumerator();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				yield return i;
			}
			e.Reset();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				foreach (Item deep in i.EnumDeep()) {
					yield return deep;
				}
			}
		}

		/// <summary>Enumerates every item in this item and items in all subcontainers, but does not recurse deeper.</summary>
		public IEnumerable<Item> EnumShallow() {
			this.ThrowIfDeleted();
			IEnumerator e = this.GetEnumerator();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				yield return i;
			}
			e.Reset();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				foreach (Item shallow in i) {
					yield return shallow;
				}
			}
		}

		public override float Weight {
			get {
				return this.Def.Weight;
			}
		}

		public override void FixWeight() {
		}

		public override byte DirectionByte {
			get {
				return (byte) this.TypeDef.Light;
			}
		}

		public virtual LightType Light {
			get {
				return this.TypeDef.Light;
			}
		}

		public Item FindType(TriggerGroup type) {
			return this.FindByType(type);
		}

		public Item FindByType(TriggerGroup type) {
			this.ThrowIfDeleted();
			foreach (Item i in this.EnumDeep()) {
				if (i.Type == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindByTypeShallow(TriggerGroup type) {
			this.ThrowIfDeleted();
			foreach (Item i in this.EnumShallow()) {
				if (i.Type == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindByClass(Type type) {
			foreach (Item i in this.EnumDeep()) {
				if (i.GetType() == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindByClassShallow(Type type) {
			foreach (Item i in this.EnumShallow()) {
				if (i.GetType() == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindById(ItemDef def) {
			foreach (Item i in this.EnumDeep()) {
				if (i.Def == def) {
					return i;
				}
			}
			return null;
		}

		public Item FindByIdShallow(ItemDef def) {
			foreach (Item i in this.EnumShallow()) {
				if (i.Def == def) {
					return i;
				}
			}
			return null;
		}

		public override void GetNameCliloc(out int id, out string argument) {
			string name = this.Name;
			int amount = this.Amount;
			id = 1042971;//~1_NOTHING~
			argument = null;
			if (amount <= 1) {
				ItemDispidInfo idi = this.TypeDef.DispidInfo;
				if (idi != null) {
					if (StringComparer.OrdinalIgnoreCase.Equals(name, idi.SingularName)) {
						id = (1020000 + (this.Model & 16383)); //hmmm...
						return;
					}
				}
				argument = name;
			} else {
				id = 1050039;//~1_NUMBER~ ~2_ITEMNAME~
				argument = string.Concat(amount.ToString(), "\t", name);
			}
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			int amount = this.Amount;
			if (this.Amount <= 1) {
				PacketSequences.SendNameFrom(clickerConn, this, this.Name, 0);
			} else {
				PacketSequences.SendNameFrom(clickerConn, this, string.Concat(amount.ToString(), " ", this.Name), 0);
			}
		}

		public virtual bool CanFallToCorpse {
			get {
				//TODO newbie flag etc.
				return true;
			}
		}

		private static TagKey linkTK = TagKey.Acquire("_link_");
		public virtual Thing Link {
			get {
				return this.GetTag(linkTK) as Thing;
			}
			set {
				if (value == null) {
					this.RemoveTag(linkTK);
				} else {
					this.SetTag(linkTK, value);
				}
			}
		}

		public override AbstractItem NewItem(IThingFactory factory, int amount) {
			Thing t = factory.Create(this);
			AbstractItem i = t as AbstractItem;
			if (i != null) {
				if (i.IsStackable) {
					i.Amount = amount;
				}
				return i;
			}
			if (t != null) {
				t.Delete();//we created a character, wtf? :)
			}
			throw new SEException(factory + " did not create an item.");
		}

		//public void PlayDropSound(AbstractCharacter droppingChar) {
		//    ScriptArgs sa = new ScriptArgs(droppingChar);
		//    if (!TryCancellableTrigger(TriggerKey.playDropSound, sa)) {
		//        On_DropSound(droppingChar);
		//    }
		//}

		//public virtual void On_DropSound(AbstractCharacter droppingChar) {
		//    this.SoundTo(this.TypeDef.DropSound, droppingChar);
		//}

		public virtual TriggerResult On_SpellEffect(SpellEffectArgs spellEffectArgs) {
			return TriggerResult.Continue;
		}

		private static TagKey morepTK = TagKey.Acquire("_morep_");

		public virtual Point4D MoreP {
			get {
				return (Point4D) this.GetTag(morepTK);
			}
			set {
				if (value == null) {
					this.RemoveTag(morepTK);
				} else {
					this.SetTag(morepTK, value);
				}
			}
		}

		public virtual void On_Dispell(SpellEffectArgs spellEffectArgs) {
		}

		public int RecursiveCount {
			get {
				int c = 0;

				foreach (Item i in this) {
					c += i.RecursiveCount + 1;
				}

				return c;
			}
		}
	}

	[ViewableClass]
	public partial class ItemDef {

		public bool IsWearableDef {
			get {
				return (this is WearableDef);
			}
		}

		public bool IsDestroyableDef {
			get {
				return (this is DestroyableDef);
			}
		}

		public bool IsWeaponDef {
			get {
				return (this is WeaponDef);
			}
		}

		protected override void On_AfterLoadFromScripts() {
			base.On_AfterLoadFromScripts();

			ResourcesList.ThrowIfNotConsumable(this.Resources);
			//ResourcesList.ThrowIfNotConsumable(this.Resources2);
		}
	}
}