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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine {

	public partial class Thing {
		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "model"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Dupe(Thing original) {
			original.InvalidateAosToolTips();
		}

		public Thing Dupe() {
			if (this.IsPlayer) {
				throw new SEException("You can not dupe a PC!");
			}
			Thing copy = (Thing) DeepCopyFactory.GetCopy(this);

			AbstractItem copyAsItem = copy as AbstractItem;
			if (copyAsItem != null) {
				Thing cont = this.Cont;
				if (cont == null) {
					MarkAsLimbo(copyAsItem);
					copyAsItem.Trigger_EnterRegion(this.point4d.x, this.point4d.y, this.point4d.z, this.point4d.m);
				} else {
					AbstractItem contItem = cont as AbstractItem;
					if (contItem == null) {
						MarkAsLimbo(copyAsItem);
						copyAsItem.Trigger_EnterChar((AbstractCharacter) cont, this.point4d.z);
					} else {
						MarkAsLimbo(copyAsItem);
						copyAsItem.Trigger_EnterItem(contItem, this.point4d.x, this.point4d.y);
					}
				}
			}

			copy.Trigger_Dupe(this);

			return copy;
		}

		//duplicating some code here, but hey... who will know ;)
		private AbstractItem Dupe(Thing cont) {
			AbstractItem copy = (AbstractItem) DeepCopyFactory.GetCopy(this);

			AbstractItem contItem = cont as AbstractItem;
			if (contItem == null) {
				MarkAsLimbo(copy);
				copy.Trigger_EnterChar((AbstractCharacter) cont, this.point4d.z);
			} else {
				MarkAsLimbo(copy);
				copy.Trigger_EnterItem(contItem, this.point4d.x, this.point4d.y);
			}

			copy.Trigger_Dupe(this);

			return copy;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "model"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void Trigger_Dupe(Thing original) {
			this.TryTrigger(TriggerKey.dupe, new ScriptArgs(original));
			try {
				this.On_Dupe(original);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		internal override bool IsLimbo {
			get {
				return this.point4d.x == 0xffff;
			}
		}

		internal virtual void MakeLimbo() {
			MarkAsLimbo(this);
		}

		internal static void MarkAsLimbo(Thing t) {
			t.point4d.SetXY(0xffff, 0xffff);
		}

		internal abstract void ItemMakeLimbo(AbstractItem i);


		//This verifies that the coordinate specified is inside the appropriate map (by asking Map if it's a valid pos),
		//and then sets
		//CheckVisibility itself only if the character turns instead of moving (Since P isn't updated in that case).
		//this is completely overriden in AbstractCharacter
		internal abstract void SetPosImpl(int x, int y, int z, byte m);
	}

	public partial class AbstractItem {

		public override Thing Cont {
			get {
				this.ThrowIfDeleted();
				ThingLinkedList list = this.contOrTLL as ThingLinkedList;
				if ((list != null) && (list.ContAsThing != null)) {
					return list.ContAsThing;
				}
				return this.contOrTLL as Thing;
			}
			set {
				this.ThrowIfDeleted();
				if (value == null) {
					throw new SEException("value is null");
				}

				AbstractItem contItem = value as AbstractItem;
				if (contItem == null) {
					if (this.IsEquippable) {
						int layer = this.Layer;
						if (layer > 0) {
							this.MakeLimbo();
							this.Trigger_EnterChar((AbstractCharacter) value, layer);
						} else {
							throw new SEException("Item '" + this + "' is equippable, but has Layer not set.");
						}
					} else {
						throw new SEException("Item '" + this + "' is not equippable.");
					}
				} else if (contItem.IsContainer) {
					this.MakeLimbo();
					int x, y;
					contItem.GetRandomXYInside(out x, out y);
					this.Trigger_EnterItem(contItem, x, y);

					contItem.TryStackToAnyInside(this);
				} else {
					throw new SEException("Item '" + value + "' is not a container, it can't contain items.");
				}
			}
		}

		internal override void MakeLimbo() {
			if (!this.IsLimbo) {
				OpenedContainers.SetContainerClosed(this);
				this.RemoveFromView();
				ItemSyncQueue.AboutToChange(this);

				Thing cont = this.Cont;
				if (cont == null) {
					this.Trigger_LeaveGround();
				} else {
					AbstractItem contItem = cont as AbstractItem;
					if (contItem == null) {
						this.Trigger_LeaveChar((AbstractCharacter) cont);
					} else {
						this.Trigger_LeaveItem(contItem);
					}
				}
				base.MakeLimbo();
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void Trigger_LeaveItem(AbstractItem cont) {
			Sanity.IfTrueThrow(cont != this.Cont, "this not in cont.");

			ItemInItemArgs args = new ItemInItemArgs(this, cont);
			ushort x = this.point4d.x;
			ushort y = this.point4d.y;
			this.TryTrigger(TriggerKey.leaveItem, args);
			this.ReturnIntoItemIfNeeded(cont, x, y);
			try {
				this.On_LeaveItem(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.ReturnIntoItemIfNeeded(cont, x, y);
			cont.TryTrigger(TriggerKey.itemLeave, args);
			this.ReturnIntoItemIfNeeded(cont, x, y);
			try {
				cont.On_ItemLeave(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.ReturnIntoItemIfNeeded(cont, x, y);

			cont.ItemMakeLimbo(this);
		}

		private void ReturnIntoItemIfNeeded(AbstractItem originalCont, int x, int y) {
			if (this.Cont != originalCont) {
				Logger.WriteWarning(this + " has been moved in the implementation of one of the @LeaveItem/@EnterItem triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterItem(originalCont, x, y);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void Trigger_LeaveChar(AbstractCharacter cont) {
			Sanity.IfTrueThrow(cont != this.Cont, "this not in cont.");

			int layer = this.point4d.z;
			ItemInCharArgs args = new ItemInCharArgs(this, cont, layer);

			if (this.IsEquippable && this.Layer == layer) {
				this.TryTrigger(TriggerKey.unEquip, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					this.On_Unequip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
				cont.TryTrigger(TriggerKey.itemUnEquip, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					cont.On_ItemUnequip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
			} else {
				this.TryTrigger(TriggerKey.leaveChar, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					this.On_LeaveChar(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
				cont.TryTrigger(TriggerKey.itemLeave, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					cont.On_ItemLeave(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
			}

			cont.ItemMakeLimbo(this);
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Unequip(ItemInCharArgs args) {
		}

		private void ReturnIntoCharIfNeeded(AbstractCharacter originalCont, int layer) {
			if ((this.Cont != originalCont) || this.point4d.z != layer) {
				Logger.WriteWarning(this + " has been moved in the implementation of one of the @LeaveChar triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterChar(originalCont, layer);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void Trigger_LeaveGround() {
			Sanity.IfTrueThrow(this.Cont != null, "this not on ground.");

			if (Map.IsValidPos(this)) {
				Point4D point = new Point4D(this.point4d);
				Map map = Map.GetMap(point.M);
				Region region = map.GetRegionFor(point.X, point.Y);
				ItemOnGroundArgs args = new ItemOnGroundArgs(this, region, point);

				this.TryTrigger(TriggerKey.leaveRegion, args);
				this.ReturnOnGroundIfNeeded(point);
				try {
					this.On_LeaveRegion(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnOnGroundIfNeeded(point);
				Region.Trigger_ItemLeave(args);

				map.Remove(this);
			} //else what?
		}

		private void ReturnOnGroundIfNeeded(Point4D point) {
			if ((this.Cont != null) || (!point.Equals(this))) {
				Logger.WriteWarning(this + " has been moved in the implementation of one of the @Leave/EnterGround triggers. Don't do this. Putting back.");
				this.MakeLimbo();
				this.Trigger_EnterRegion(point.X, point.Y, point.Z, point.M);
			}
		}

		//we expect this to be in limbo, and cont to be really container
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_EnterItem(AbstractItem cont, int x, int y) {
			Sanity.IfTrueThrow(!this.IsLimbo, "this not in Limbo");
			Sanity.IfTrueThrow(!cont.IsContainer, "cont is no Container");
#if TRACE
			{
				int minX, minY, maxX, maxY;
				cont.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
				Sanity.IfTrueThrow(x < minX, "x < minX");
				Sanity.IfTrueThrow(x > maxX, "x > maxX");
				Sanity.IfTrueThrow(y < minY, "y < minY");
				Sanity.IfTrueThrow(y > maxY, "y > maxY");
			}
#endif

			cont.InternalItemEnter(this);
			this.point4d.SetXY(x, y);

			ItemInItemArgs args = new ItemInItemArgs(this, cont);
			this.TryTrigger(TriggerKey.enterItem, args);
			this.ReturnIntoItemIfNeeded(cont, x, y);
			try {
				this.On_EnterItem(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.ReturnIntoItemIfNeeded(cont, x, y);
			cont.TryTrigger(TriggerKey.itemEnter, args);
			this.ReturnIntoItemIfNeeded(cont, x, y);
			try {
				cont.On_ItemEnter(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.ReturnIntoItemIfNeeded(cont, x, y);
		}

		internal void InternalItemEnter(AbstractItem i) {
			this.InvalidateAosToolTips();
			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				tll = new ThingLinkedList(this);
				this.contentsOrComponents = tll;
			}
			tll.Add(i);
			i.contOrTLL = tll;
			this.AdjustWeight(i.Weight);
		}

		//try to add item to some stack inside
		public bool TryStackToAnyInside(AbstractItem toStack) {
			Sanity.IfTrueThrow(!this.IsContainer, "TryStackToAnyInside can only be called on a container");
			Sanity.IfTrueThrow(toStack.Cont != this, "TryStackToAnyInside parameter item must be in the container it's called on");

			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				AbstractItem stackWith = (AbstractItem) tll.firstThing;
				while (stackWith != null) {
					if (stackWith != toStack) {
						if (this.Trigger_StackInCont(toStack, stackWith)) {
							return true;
						}
					}
					stackWith = (AbstractItem) stackWith.nextInList;
				}
			}
			return false;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static bool CouldBeStacked(AbstractItem a, AbstractItem b) {
			return ((a.IsStackable && b.IsStackable) &&
					(a.def == b.def) &&
					(a.Color == b.Color) &&
					(a.Model == b.Model));
		}

		//add "toStack" to this stack, if possible
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal bool Trigger_StackInCont(AbstractItem toStack, AbstractItem waitingStack) {
			Sanity.IfTrueThrow(this != toStack.Cont, "this != toStack.Cont");
			Sanity.IfTrueThrow(this != waitingStack.Cont, "this != waitingStack.Cont");

			if (CouldBeStacked(toStack, waitingStack)) {
				//Amount overflow checking:
				int tmpAmount = waitingStack.Amount;
				try {
					tmpAmount = checked(tmpAmount + toStack.Amount);
				} catch (OverflowException) {
					return false;
				}

				ushort toStackX = toStack.point4d.x;
				ushort toStackY = toStack.point4d.y;
				ItemStackArgs args = new ItemStackArgs(toStack, waitingStack);

				var result = toStack.TryCancellableTrigger(TriggerKey.stackOnItem, args);
				toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
				if (result != TriggerResult.Cancel && waitingStack.Cont == this) {
					try {
						result = toStack.On_StackOnItem(args);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
					if (result != TriggerResult.Cancel && waitingStack.Cont == this) {
						result = waitingStack.TryCancellableTrigger(TriggerKey.itemStackOn, args);
						toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
						if (result != TriggerResult.Cancel && waitingStack.Cont == this) {
							try {
								result = waitingStack.On_ItemStackOn(args);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							if (result != TriggerResult.Cancel && waitingStack.Cont == this) {
								toStack.InternalDelete();
								waitingStack.Amount = tmpAmount;
								return true;
							}
							toStack.ReturnIntoItemIfNeeded(this, toStackX, toStackY);
						}
					}
				}
			}
			return false; //stacking didn't happen
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static bool Trigger_StackOnGround(AbstractItem toStack, AbstractItem waitingStack) {
			Sanity.IfTrueThrow(null != toStack.Cont, "toStack not on ground.");
			Sanity.IfTrueThrow(null != waitingStack.Cont, "waitingStack not on ground.");

			if (CouldBeStacked(toStack, waitingStack)) {

				//Amount overflow checking:
				int tmpAmount = waitingStack.Amount;
				try {
					tmpAmount = checked(tmpAmount + toStack.Amount);
				} catch (OverflowException) {
					return false;
				}

				Point4D toStackPoint = toStack.P();
				ItemStackArgs args = new ItemStackArgs(toStack, waitingStack);

				var result = toStack.TryCancellableTrigger(TriggerKey.stackOnItem, args);
				toStack.ReturnOnGroundIfNeeded(toStackPoint);
				if (result != TriggerResult.Cancel && waitingStack.Cont == null) {
					try {
						result = toStack.On_StackOnItem(args);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					toStack.ReturnOnGroundIfNeeded(toStackPoint);
					if (result != TriggerResult.Cancel && waitingStack.Cont == null) {
						result = waitingStack.TryCancellableTrigger(TriggerKey.itemStackOn, args);
						toStack.ReturnOnGroundIfNeeded(toStackPoint);
						if (result != TriggerResult.Cancel && waitingStack.Cont == null) {
							try {
								result = waitingStack.On_ItemStackOn(args);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							if (result != TriggerResult.Cancel && waitingStack.Cont == null) {
								toStack.InternalDelete();
								waitingStack.Amount = tmpAmount;
								return true;
							}
							toStack.ReturnOnGroundIfNeeded(toStackPoint);
						}
					}
				}
			}
			return false;
		}


		//we expect this to be in limbo and equippable
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_EnterChar(AbstractCharacter cont, int layer) {
			Sanity.IfTrueThrow(!this.IsLimbo, "this not in limbo");
			Sanity.IfTrueThrow(((layer != (int) LayerNames.Dragging) && (!this.IsEquippable)), "this not equippable");
			Sanity.IfTrueThrow(layer == 0, "layer == 0");

			cont.InternalItemEnter(this, layer);

			ItemInCharArgs args = new ItemInCharArgs(this, cont, layer);

			//do we really want this?
			if (this.IsEquippable && this.Layer == layer) {
				this.TryTrigger(TriggerKey.equip, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					this.On_Equip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
				cont.TryTrigger(TriggerKey.itemEquip, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					cont.On_ItemEquip(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
			} else {
				this.TryTrigger(TriggerKey.enterChar, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					this.On_EnterChar(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
				cont.TryTrigger(TriggerKey.itemEnter, args);
				this.ReturnIntoCharIfNeeded(cont, layer);
				try {
					cont.On_ItemEnter(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.ReturnIntoCharIfNeeded(cont, layer);
			}

		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Equip(ItemInCharArgs args) {
		}

		//we expect this to be in limbo
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_EnterRegion(int x, int y, int z, byte m) {
			Sanity.IfTrueThrow(!this.IsLimbo, "Item is supposed to be in Limbo state when Trigger_Enter* called");

			this.point4d.SetXYZM(x, y, z, m);

			Map map = Map.GetMap(m);
			Region region = map.GetRegionFor(x, y);
			Point4D point = new Point4D(x, y, z, m);
			ItemOnGroundArgs args = new ItemOnGroundArgs(this, region, point);
			map.Add(this);

			this.TryTrigger(TriggerKey.enterRegion, args);
			this.ReturnOnGroundIfNeeded(point);
			try {
				this.On_EnterRegion(args);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			this.ReturnOnGroundIfNeeded(point);
			Region.Trigger_ItemEnter(args);
		}

		internal sealed override void SetPosImpl(int x, int y, int z, byte m) {
			if (Map.IsValidPos(x, y, m)) {
				this.MakeLimbo();
				this.Trigger_EnterRegion(x, y, z, m);
			} else {
				throw new SEException("Invalid position (" + x + "," + y + " on mapplane " + m + ")");
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LeaveItem(ItemInItemArgs args) {
			//I am the manipulated item
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemLeave(ItemInItemArgs args) {
			//I am the container
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LeaveChar(ItemInCharArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LeaveRegion(ItemOnGroundArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemEnter(ItemInItemArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_EnterItem(ItemInItemArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_ItemStackOn(ItemStackArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_StackOnItem(ItemStackArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_EnterChar(ItemInCharArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_EnterRegion(ItemOnGroundArgs args) {

		}

		[SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
		public void GetRandomXYInside(out int x, out int y) {
			//todo?: nonconstant bounds for this? or virtual?
			int minX, minY, maxX, maxY;
			this.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
			x = Globals.dice.Next(minX, maxX);
			y = Globals.dice.Next(minY, maxY);
		}

		[SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
		public virtual void GetContainerGumpBoundaries(out int minX, out int minY, out int maxX, out int maxY) {
			minX = 20;
			minY = 20;
			maxX = 120;
			maxY = 120;
		}

		/**
			Drop this item on the ground.
		*/
		public void Drop() {
			Thing cont = this.Cont;
			if (cont != null) {
				this.P(cont.TopPoint);
			}
		}

		//is not public because it leaves the item in "illegal" state...
		internal sealed override void ItemMakeLimbo(AbstractItem i) {
			Sanity.IfTrueThrow(this != i.Cont, "i not in this.");

			ThingLinkedList tll = (ThingLinkedList) i.contOrTLL;
			tll.Remove(i);
			this.InvalidateAosToolTips();//changing Count
			this.AdjustWeight(-i.Weight);
		}

		public void MoveInsideContainer(int x, int y) {
			AbstractItem i = this.Cont as AbstractItem;
			if (i != null) {
				int minX, minY, maxX, maxY;
				i.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
				if (x < minX) {
					x = minX;
				} else if (x > maxX) {
					x = maxX;
				}
				if (y < minY) {
					y = minY;
				} else if (y > maxY) {
					y = maxY;
				}

				if ((this.point4d.x != x) || (this.point4d.y != y)) {
					OpenedContainers.SetContainerClosed(this);//if I am a container too, I get closed also
					ItemSyncQueue.AboutToChange(this);
					this.point4d.SetXY(x, y);
				}
			}//else throw? Probably not so important...
		}

		private void MakePositionInContLegal() {
			AbstractItem i = this.Cont as AbstractItem;
			if (i != null) {
				MutablePoint4D p = this.point4d;

				int minX, minY, maxX, maxY;
				i.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);

				int newX = p.x;
				int newY = p.y;

				if (newX < minX) {
					newX = minX;
				} else if (p.x > maxX) {
					newX = maxX;
				}
				if (newY < minY) {
					newY = minY;
				} else if (newY > maxY) {
					newY = maxY;
				}

				this.point4d.SetXY(newX, newY);
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public void LoadCont_Delayed(object resolvedObj, string filename, int line) {
			if (this.Uid == -1) {
				//I was probably cleared because of failed load. Let me get deleted by garbage collector.
				return;
			}
			if (this.Cont == null) {
				Thing t = resolvedObj as Thing;
				if (t != null)
				{
					if ((t.IsChar) && (this.IsEquippable)) {
						((AbstractCharacter) t).AddLoadedItem(this);
						return;
					}
					if (t.IsContainer) {
						((AbstractItem) t).InternalItemEnter(this);
						this.MakePositionInContLegal();
						return;
					}
				}
				//contOrTLL=null;
				Logger.WriteWarning("The saved cont object (" + resolvedObj + ") for item '" + this + "' is not a valid container. Removing.");
				this.InternalDeleteNoRFV();
				return;
			}
			Logger.WriteWarning("The object (" + resolvedObj + ") is being loaded as cont for item '" + this + "', but it already does have it's cont. This should not happen.");
		}

		/// <summary>
		/// Called when someone is trying to pick me up
		/// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPickup(DenyPickupArgs args) {
			return TriggerResult.Continue;
		}

		/// <summary>
		/// Called when someone is trying to pick up item that is contained in me
		/// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_DenyPickupItemFrom(DenyPickupArgs args) {
		}

		/// <summary>
		/// Called when someone is trying to put me on ground
		/// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPutOnGround(DenyPutOnGroundArgs args) {
			return TriggerResult.Continue;
		}

		/// <summary>
		/// Called when someone is trying to put me in a container
		/// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPutInItem(DenyPutInItemArgs args) {
			return TriggerResult.Continue;
		}

		/// <summary>
		/// Called when someone is trying to put an item in me (I am a container)
		/// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPutItemIn(DenyPutInItemArgs args) {
			return TriggerResult.Continue;
		}

		/// <summary>
		/// Called when I am being put on another item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_PutOnItem(ItemOnItemArgs args) {
			return TriggerResult.Continue;
		}

		/// <summary>
		/// Called when another item is being put on me.
		/// </summary>
		/// <param name="args">The args.</param>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_PutItemOn(ItemOnItemArgs args) {
		}

		/// <summary>
		/// Called when I am being put on a character other than the person wielding me.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_PutOnChar(ItemOnCharArgs args) {
			return TriggerResult.Continue;
		}

		/// <summary>
		/// Called when I am being equipped on a character
		/// </summary>
		/// <param name="args">The args.</param>
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_DenyEquip(DenyEquipArgs args) {
		}

		//should we add reference to the player?
		internal void Trigger_SplitFromStack(AbstractItem stack) {

			ScriptArgs args = new ScriptArgs(stack);
			this.TryTrigger(TriggerKey.splitFromStack, args);

			try {
				this.On_SplitFromStack(stack);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		public virtual void On_SplitFromStack(AbstractItem leftOverStack) {
		}
	}

	public partial class AbstractCharacter {

		internal sealed override void ItemMakeLimbo(AbstractItem i) {
			Sanity.IfTrueThrow(this != i.Cont, "i not in this.");

			if (this.draggingLayer == i) {
				this.draggingLayer = null;
			} else {
				ThingLinkedList tll = (ThingLinkedList) i.contOrTLL;
				tll.Remove(i);
			}

			i.contOrTLL = null;
			this.AdjustWeight(-i.Weight);
		}

		internal void InternalItemEnter(AbstractItem i, int layer) {
			Sanity.IfTrueThrow(!i.IsLimbo, "i not in limbo");
			Sanity.IfTrueThrow(layer == 0, "layer == 0");

			if (layer != (int) LayerNames.Special) {
				if ((layer == (int) LayerNames.Hand1) || (layer == (int) LayerNames.Hand2)) {
					bool twoHanded = i.IsTwoHanded;
					AbstractItem h1 = this.FindLayer(LayerNames.Hand1);
					if (h1 != null) {
						if (twoHanded || (layer == (int) LayerNames.Hand1) || h1.IsTwoHanded) {
							MutablePoint4D p = this.point4d;
							h1.P(p.x, p.y, p.z, p.m);
						}
					}
					AbstractItem h2 = this.FindLayer(LayerNames.Hand2);
					if (h2 != null) {
						if (twoHanded || (layer == (int) LayerNames.Hand2) || h2.IsTwoHanded) {
							MutablePoint4D p = this.point4d;
							h2.P(p.x, p.y, p.z, p.m);
						}
					}
				} else {
					//unequip what's in there and throw it on ground.
					AbstractItem prevItem = this.FindLayer(layer);
					if (prevItem != null) {
						MutablePoint4D p = this.point4d;
						prevItem.P(p.x, p.y, p.z, p.m);
					}
					Sanity.IfTrueThrow(this.FindLayer(layer) != null, "Layer " + layer + " is supposed to be empty after it's contents was removed.");
				}
			}

			if (layer == (int) LayerNames.Dragging) {
				this.draggingLayer = i;
				i.contOrTLL = this;
			} else if (layer == (int) LayerNames.Special) {
				if (this.specialLayer == null) {
					this.specialLayer = new ThingLinkedList(this);
				}
				this.specialLayer.Add(i);
				i.contOrTLL = this.specialLayer;
			} else if (layer < sentLayers) {
				if (this.visibleLayers == null) {
					this.visibleLayers = new ThingLinkedList(this);
				}
				this.visibleLayers.Add(i);
				i.contOrTLL = this.visibleLayers;
			} else {
				if (this.invisibleLayers == null) {
					this.invisibleLayers = new ThingLinkedList(this);
				}
				this.invisibleLayers.Add(i);
				i.contOrTLL = this.invisibleLayers;
			}

			i.point4d.SetXYZ(7000, 0, layer);
			this.AdjustWeight(i.Weight);
		}

		internal void AddLoadedItem(AbstractItem item) {
			int layer = item.Z;
			MarkAsLimbo(item);
			this.InternalItemEnter(item, layer);
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemLeave(ItemInCharArgs args) {

		}

		public bool HasPickedUp(AbstractItem itm) {
			return this.draggingLayer == itm;
		}

		public bool HasPickedUp(int uid) {
			return ((this.draggingLayer != null) && (this.draggingLayer.Uid == uid));
		}

		public AbstractItem FindLayer(LayerNames layer) {
			return this.FindLayer((int) layer);
		}

		public AbstractItem FindLayer(int num) {
			if (num == (int) LayerNames.Dragging) {
				return this.draggingLayer;
			}
			if (num == (int) LayerNames.Special) {
				if (this.specialLayer != null) {
					return (AbstractItem) this.specialLayer.firstThing;
				}
				return null;
			}
			if (num < sentLayers) {
				if (this.visibleLayers != null) {
					return (AbstractItem) this.visibleLayers.FindByZ(num);
				}
			} else if (this.invisibleLayers != null) {
				return (AbstractItem) this.invisibleLayers.FindByZ(num);
			}
			return null;
		}

		public override AbstractItem FindCont(int index) {
			int counter = 0;
			int prevCounter;
			if (this.visibleLayers != null) {
				counter = this.visibleLayers.count;
			}
			if (index < counter) {
				return (AbstractItem) this.visibleLayers[index];
			}
			prevCounter = counter;
			if (this.invisibleLayers != null) {
				counter += this.invisibleLayers.count;
			}
			if (index < counter) {
				return (AbstractItem) this.invisibleLayers[index - prevCounter];
			}
			if (this.draggingLayer != null) {
				if (index == counter) {
					return this.draggingLayer;
				}
				counter++;
			}
			prevCounter = counter;
			if (this.specialLayer != null) {
				counter += this.specialLayer.count;
			}
			if (index < counter) {
				return (AbstractItem) this.specialLayer[index - prevCounter];
			}
			return null;
		}

		//
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void TryEquip(AbstractItem i) {
			i.Cont = this;
		}

		/// <summary>
		/// Tries to have the char drop the item it is dragging. First it tries to put it in backpack, then on ground.
		/// </summary>
		/// <returns>True if it is dragging no item after the method passes.</returns>
		public bool TryGetRidOfDraggedItem() {
			AbstractItem i = this.draggingLayer;
			if (i != null) {
				DenyResult result = this.TryPutItemOnItem(this.GetBackpack());
				if (!result.Allow && this.draggingLayer == i) {//can't put item in his own pack? unprobable but possible.
					GameState state = this.GameState;
					TcpConnection<GameState> conn = null;
					if (state != null) {
						conn = state.Conn;
						result.SendDenyMessage(this, state, conn);
					}

					MutablePoint4D point = this.point4d;
					result = this.TryPutItemOnGround(point.x, point.y, point.z);

					if (!result.Allow && this.draggingLayer == i) {//can't even put item on ground?
						if (state != null) {
							result.SendDenyMessage(this, state, conn);
						}
						//The player is not allowed to put the item anywhere. 
						//Too bad we can't tell the client
					}
				}
			}

			return this.draggingLayer == null;
		}

		private void SendMovingItemAnimation(IPoint4D from, IPoint4D to, AbstractItem i) {
			DraggingOfItemOutPacket p = Pool<DraggingOfItemOutPacket>.Acquire();
			p.Prepare(from, to, i);
			GameServer.SendToClientsWhoCanSee(this, p);
		}

		#region client
		//picks up item. typically called from InPackets. I am the src, the item can be anywhere.
		//will run the @deny triggers
		//CanReach checks are not considered done.
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPickupItem(AbstractItem item, int amountToPick) {
			this.ThrowIfDeleted();
			item.ThrowIfDeleted();

			if (!this.TryGetRidOfDraggedItem()) {
				return DenyResultMessages.Deny_YouAreAlreadyHoldingAnItem;
			}

			DenyResult result = this.CanPickup(item);

			//equip into dragging layer
			if (result.Allow) {
				this.PickupImpl(item, amountToPick);
			}
			return result;
		}

		/// <summary>
		/// Pickups the item. No "Can" checks done, except for throwing away the previously held item - use with care.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="amountToPick">The amount to pick.</param>
		public void PickupItem(AbstractItem item, int amountToPick) {
			this.ThrowIfDeleted();
			item.ThrowIfDeleted();

			if (!this.TryGetRidOfDraggedItem()) {
				throw new SEException("Can't pick up a new item because can't get rid of item currently being held.");
			}

			//equip into dragging layer
			this.PickupImpl(item, amountToPick);
		}

		private void PickupImpl(AbstractItem item, int amountToPick) {
			IPoint4D oldPoint = item.TopObj();
			if (oldPoint != this) {
				if (oldPoint == item) {
					oldPoint = item.P();
				}
			} else {
				oldPoint = null;
			}

			int amountSum = item.Amount;
			if (!item.IsEquipped && amountToPick < amountSum) {
				AbstractItem dupedItem = (AbstractItem) item.Dupe();
				dupedItem.Amount = (amountSum - amountToPick);
				item.Amount = amountToPick;
				item.Trigger_SplitFromStack(dupedItem);
			}

			item.MakeLimbo();
			item.Trigger_EnterChar(this, (int) LayerNames.Dragging);
			if (oldPoint != null) {
				this.SendMovingItemAnimation(oldPoint, this, item);
			}
		}

		public DenyResult CanPickup(AbstractItem item) {
			TriggerResult triggerResult;
			DenyResult denyResult = this.Trigger_DenyPickupItem(item, out triggerResult);
			//default implementation, can be skipped by returning true (cancelling)
			if (triggerResult != TriggerResult.Cancel && (denyResult.Allow)) {
				return this.CanReach(item);
			}
			return denyResult;
		}

		private DenyResult Trigger_DenyPickupItem(AbstractItem item, out TriggerResult result) {
			//@deny triggers
			DenyPickupArgs args = new DenyPickupArgs(this, item);

			result = this.TryCancellableTrigger(TriggerKey.denyPickupItem, args);
			if (result != TriggerResult.Cancel) {
				try {
					result = this.On_DenyPickupItem(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = item.TryCancellableTrigger(TriggerKey.denyPickup, args);
					if (result != TriggerResult.Cancel) {
						try {
							result = item.On_DenyPickup(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

						if (result != TriggerResult.Cancel) {
							Thing c = item.Cont;
							if (c != null) {
								AbstractItem contItem = c as AbstractItem;
								if (contItem != null) {
									result = contItem.TryCancellableTrigger(TriggerKey.denyPickupItemFrom, args);
									if (result != TriggerResult.Cancel) {
										try {
											contItem.On_DenyPickupItemFrom(args);
										} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
									}
								} else {
									AbstractCharacter contChar = (AbstractCharacter) c;
									result = contChar.TryCancellableTrigger(TriggerKey.denyPickupItemFrom, args);
									if (result != TriggerResult.Cancel) {
										try {
											contChar.On_DenyPickupItemFrom(args);
										} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
									}
								}
							} else {
								MutablePoint4D p = item.point4d;
								Region region = Map.GetMap(p.m).GetRegionFor(p.x, p.y);
								region.Trigger_DenyPickupItemFrom(args);
							}
						}
					}
				}
			}

			return args.Result;
		}

		//typically called from InPackets. (I am the src)
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemInItem(AbstractItem targetCont, int x, int y, bool tryStacking) {
			this.ThrowIfDeleted();
			targetCont.ThrowIfDeleted();

			AbstractItem item = this.draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + targetCont + "'");
			}
			item.ThrowIfDeleted();

			if (targetCont.IsContainer) {
				DenyPutInItemArgs args = new DenyPutInItemArgs(this, item, targetCont);

				var result = this.TryCancellableTrigger(TriggerKey.denyPutItemInItem, args);
				if (result != TriggerResult.Cancel) {
					try {
						result = this.On_DenyPutItemInItem(args);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					if (result != TriggerResult.Cancel) {
						result = item.TryCancellableTrigger(TriggerKey.denyPutInItem, args);
						if (result != TriggerResult.Cancel) {
							try {
								result = item.On_DenyPutInItem(args);
							} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							if (result != TriggerResult.Cancel) {
								result = targetCont.TryCancellableTrigger(TriggerKey.denyPutItemIn, args);
								if (result != TriggerResult.Cancel) {
									try {
										result = targetCont.On_DenyPutItemIn(args);
									} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
								}
							}
						}
					}
				}

				DenyResult retVal = args.Result;

				if (result != TriggerResult.Cancel && (retVal.Allow)) {
					retVal = OpenedContainers.HasContainerOpen(this, targetCont);

					if (!retVal.Allow) {//we don't have it open, let's check if we could
						retVal = this.CanPutItemsInContainer(targetCont);//we check if targetCont could be openable (it also does canreach checks)
					}
				}

				if (retVal.Allow) {
					this.PutItemInItemImpl(targetCont, x, y, tryStacking, item);
				}
				return retVal;
			}
			throw new SEException("The item (" + targetCont + ") is not a container");
		}

		/// <summary>
		/// Puts the item in an container. No "Can" checks are done - use with care.
		/// </summary>
		/// <param name="targetCont">The target cont.</param>
		/// <param name="x">The x coordinate inside the target cont.</param>
		/// <param name="y">The y.</param>
		/// <param name="tryStacking">if set to <c>true</c> [try stacking].</param>
		public void PutItemInItem(AbstractItem targetCont, int x, int y, bool tryStacking) {
			this.ThrowIfDeleted();
			targetCont.ThrowIfDeleted();

			AbstractItem item = this.draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + targetCont + "'");
			}
			item.ThrowIfDeleted();

			if (targetCont.IsContainer) {
				this.PutItemInItemImpl(targetCont, x, y, tryStacking, item);
			} else {
				throw new SEException("The item (" + targetCont + ") is not a container");
			}
		}

		private void PutItemInItemImpl(AbstractItem targetCont, int x, int y, bool tryStacking, AbstractItem item) {
			int minX, minY, maxX, maxY;
			targetCont.GetContainerGumpBoundaries(out minX, out minY, out maxX, out maxY);
			if (x < minX) {
				x = minX;
			} else if (x > maxX) {
				x = maxX;
			}
			if (y < minY) {
				y = minY;
			} else if (y > maxY) {
				y = maxY;
			}

			item.MakeLimbo();
			item.Trigger_EnterItem(targetCont, x, y);
			if (tryStacking) {
				targetCont.TryStackToAnyInside(item);
			}
			if (targetCont.TopObj() != this) {
				this.SendMovingItemAnimation(this, targetCont, item);
			}
		}

		//typically called from InPackets. (I am the src)
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemOnItem(AbstractItem target) {
			this.ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem item = this.draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + target + "'");
			}
			item.ThrowIfDeleted();

			if (target.IsContainer) {
				int x, y;
				target.GetRandomXYInside(out x, out y);
				return this.TryPutItemInItem(target, x, y, true);
			} else {
				Thing cont = target.Cont;
				if (cont != null) {
					AbstractItem contAsItem = cont as AbstractItem;
					if (contAsItem != null) {
						MutablePoint4D point = target.point4d;
						DenyResult result = this.TryPutItemInItem(contAsItem, point.x, point.y, false);
						if (result.Allow) {
							if (!contAsItem.Trigger_StackInCont(item, target)) {
								//couldn't stack, let's see if there's some script for the stacking
								this.Trigger_TryPutItemOnItem(target, item);
								return DenyResultMessages.Allow;
							}
						}
						return result;
					} else {
						//we're stacking something on an equipped item? that's pretty weird.
						return DenyResultMessages.Deny_NoMessage;
					}
				} else {
					MutablePoint4D point = target.point4d;
					DenyResult result = this.TryPutItemOnGround(point.x, point.y, point.z);
					if (result.Allow) {
						if (!AbstractItem.Trigger_StackOnGround(item, target)) {
							//couldn't stack, let's see if there's some script for the stacking
							this.Trigger_TryPutItemOnItem(target, item);
							return DenyResultMessages.Allow;
						}
					}
					return result;
				}
			}
		}

		private void Trigger_TryPutItemOnItem(AbstractItem target, AbstractItem item) {
			ItemOnItemArgs args = new ItemOnItemArgs(this, item, target);
			var result = item.TryCancellableTrigger(TriggerKey.putOnItem, args);
			if (result != TriggerResult.Cancel) {
				try {
					result = item.On_PutOnItem(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = target.TryCancellableTrigger(TriggerKey.putItemOn, args);
					if (result != TriggerResult.Cancel) {
						try {
							target.On_PutItemOn(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
		}

		//typically called from InPackets. (I am the src)
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemOnGround(int x, int y, int z) {
			this.ThrowIfDeleted();
			AbstractItem item = this.draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on ground");
			}
			item.ThrowIfDeleted();

			byte m = this.M;
			IPoint4D point = new Point4D(x, y, z, m);
			DenyPutOnGroundArgs args = new DenyPutOnGroundArgs(this, item, point);

			var result = this.TryCancellableTrigger(TriggerKey.denyPutItemOnGround, args);
			if (result != TriggerResult.Cancel) {
				try {
					result = this.On_DenyPutItemOnGround(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = item.TryCancellableTrigger(TriggerKey.denyPutOnGround, args);
					if (result != TriggerResult.Cancel) {
						try {
							result = item.On_DenyPutOnGround(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

						if (result != TriggerResult.Cancel) {
							Region region = Map.GetMap(m).GetRegionFor(x, y);
							result = region.Trigger_DenyPutItemOn(args);
						}
					}
				}
			}

			DenyResult retVal = args.Result;
			point = args.Point.TopPoint;

			//default implementation
			if (result != TriggerResult.Cancel && (retVal.Allow)) {
				retVal = this.CanReachCoordinates(point);
			}

			if (retVal.Allow) {
				item.MakeLimbo();
				item.Trigger_EnterRegion(point.X, point.Y, point.Z, point.M);
				this.SendMovingItemAnimation(this, point, item);
			}

			return retVal;
		}

		//typically called from InPackets. drops the held item on target (I am the src)
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryPutItemOnChar(AbstractCharacter target) {
			this.ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem item = this.draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + target + "'");
			}
			item.ThrowIfDeleted();

			if (target == this) {
				return this.TryPutItemOnItem(this.GetBackpack());
			}
			ItemOnCharArgs args = new ItemOnCharArgs(this, item, target);

			var result = this.TryCancellableTrigger(TriggerKey.putItemOnChar, args);
			if (result != TriggerResult.Cancel) {
				try {
					result = this.On_PutItemOnChar(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = item.TryCancellableTrigger(TriggerKey.putOnChar, args);
					if (result != TriggerResult.Cancel) {
						try {
							result = item.On_PutOnChar(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
						if (result != TriggerResult.Cancel) {
							result = target.TryCancellableTrigger(TriggerKey.putItemOn, args);
							if (result != TriggerResult.Cancel) {
								try {
									result = target.On_PutItemOn(args);
								} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
							}
						}
					}
				}
			}

			if (result != TriggerResult.Cancel) {
				return this.TryPutItemOnItem(target.GetBackpack());
			}

			return DenyResultMessages.Allow;
		}

		//typically called from InPackets. drops the held item on target (I am the src)
		//dropping on ones own paperdoll
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public DenyResult TryEquipItemOnChar(AbstractCharacter target) {
			this.ThrowIfDeleted();
			target.ThrowIfDeleted();
			AbstractItem item = this.draggingLayer;
			if (item == null) {
				throw new SEException("Character '" + this + "' has no item dragged to drop on '" + target + "'");
			}
			item.ThrowIfDeleted();

			if (item.IsEquippable) {
				int layer = item.Layer;
				if ((layer < sentLayers) && (layer > 0)) {
					bool succeededUnequipping = true;
					if (layer != (int) LayerNames.Special) {
						if ((layer == (int) LayerNames.Hand1) || (layer == (int) LayerNames.Hand2)) {
							bool twoHanded = item.IsTwoHanded;
							AbstractItem h1 = this.FindLayer(LayerNames.Hand1);
							if (h1 != null) {
								if (twoHanded || (layer == (int) LayerNames.Hand1) || h1.IsTwoHanded) {
									succeededUnequipping = this.TryUnequip(h1);
								}
							}
							AbstractItem h2 = this.FindLayer(LayerNames.Hand2);
							if (h2 != null) {
								if (twoHanded || (layer == (int) LayerNames.Hand2) || h2.IsTwoHanded) {
									succeededUnequipping = succeededUnequipping && this.TryUnequip(h2);
								}
							}
						} else {
							//unequip what's in there and throw it on ground.
							AbstractItem prevItem = this.FindLayer(layer);
							if (prevItem != null) {
								succeededUnequipping = this.TryUnequip(prevItem);
							}
						}
					}

					if (!succeededUnequipping) {
						return DenyResultMessages.Deny_YouAreAlreadyHoldingAnItem;
					}

					DenyEquipArgs args = new DenyEquipArgs(this, item, target, layer);

					var result = this.TryCancellableTrigger(TriggerKey.denyEquipOnChar, args);
					if (result != TriggerResult.Cancel) {
						try {
							result = this.On_DenyEquipOnChar(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
						if (result != TriggerResult.Cancel) {
							result = target.TryCancellableTrigger(TriggerKey.denyEquip, args);
							if (result != TriggerResult.Cancel) {
								try {
									result = target.On_DenyEquip(args);
								} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
								if (result != TriggerResult.Cancel) {
									result = item.TryCancellableTrigger(TriggerKey.denyEquip, args);
									if (result != TriggerResult.Cancel) {
										try {
											item.On_DenyEquip(args);
										} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
									}
								}
							}
						}
					}

					DenyResult retVal = args.Result;

					if (retVal.Allow) {
						if (this != target) {
							retVal = this.CanReach(target);
						}
					}

					if (retVal.Allow) {
						item.MakeLimbo();
						item.Trigger_EnterChar(target, layer);
						if (this != target) {
							this.SendMovingItemAnimation(this, target, item);
						}
					}

					return retVal;
				}
			}

			return this.TryPutItemOnChar(target);
		}

		private bool TryUnequip(AbstractItem i) {
			DenyResult dr = this.TryPickupItem(i, 1);
			if (dr.Allow) {
				return this.TryGetRidOfDraggedItem();
			}
			return false;
		}

		#endregion client trigger_deny methods


		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemEnter(ItemInCharArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemEquip(ItemInCharArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemUnequip(ItemInCharArgs args) {

		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPutItemOnGround(DenyPutOnGroundArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPickupItem(DenyPickupArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyPutItemInItem(DenyPutInItemArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_DenyPickupItemFrom(DenyPickupArgs args) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_PutItemOn(ItemOnCharArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_PutItemOnChar(ItemOnCharArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyEquip(DenyEquipArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyEquipOnChar(DenyEquipArgs args) {
			return TriggerResult.Continue;
		}
	}

	public class ItemStackArgs : ScriptArgs {
		private readonly AbstractItem manipulatedItem;
		private readonly AbstractItem waitingStack;

		public ItemStackArgs(AbstractItem manipulatedItem, AbstractItem waitingStack)
			: base(manipulatedItem, waitingStack) {
			this.manipulatedItem = manipulatedItem;
			this.waitingStack = waitingStack;
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}

		public AbstractItem WaitingStack {
			get {
				return this.waitingStack;
			}
		}
	}

	public class ItemOnGroundArgs : ScriptArgs {
		private readonly AbstractItem manipulatedItem;
		private readonly Region region;
		private readonly Point4D point;

		public ItemOnGroundArgs(AbstractItem manipulatedItem, Region region, Point4D point)
			: base(manipulatedItem, region, point) {
			this.manipulatedItem = manipulatedItem;
			this.region = region;
			this.point = point;
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}

		public Region Region {
			get {
				return this.region;
			}
		}

		public Point4D Point {
			get {
				return this.point;
			}
		}
	}

	public class ItemInItemArgs : ScriptArgs {
		private readonly AbstractItem manipulatedItem;
		private readonly AbstractItem container;

		public ItemInItemArgs(AbstractItem manipulatedItem, AbstractItem container)
			: base(manipulatedItem, container) {
			this.manipulatedItem = manipulatedItem;
			this.container = container;
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}

		public AbstractItem Container {
			get {
				return this.container;
			}
		}
	}

	public class ItemInCharArgs : ScriptArgs {
		private readonly AbstractItem manipulatedItem;
		private readonly AbstractCharacter cont;
		private readonly int layer;

		public ItemInCharArgs(AbstractItem manipulatedItem, AbstractCharacter cont, int layer)
			: base(manipulatedItem, cont, layer) {
			this.manipulatedItem = manipulatedItem;
			this.cont = cont;
			this.layer = layer;
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}

		public AbstractCharacter Cont {
			get {
				return this.cont;
			}
		}

		public int Layer {
			get {
				return this.layer;
			}
		}
	}

	public class DenyTriggerArgs : ScriptArgs {
		public DenyTriggerArgs(params object[] argv)
			: base(argv) {
			Sanity.IfTrueThrow(!(argv[0] is DenyResult), "argv[0] is not DenyResult");
		}

		public DenyResult Result {
			get {
				return (DenyResult) this.Argv[0];
			}
			set {
				this.Argv[0] = value;
			}
		}
	}

	public class DenyPickupArgs : DenyTriggerArgs {
		private readonly AbstractCharacter pickingChar;
		private readonly AbstractItem manipulatedItem;

		public DenyPickupArgs(AbstractCharacter pickingChar, AbstractItem manipulatedItem)
			: base(DenyResultMessages.Allow, pickingChar, manipulatedItem) {
			this.pickingChar = pickingChar;
			this.manipulatedItem = manipulatedItem;
		}

		public AbstractCharacter PickingChar {
			get {
				return this.pickingChar;
			}
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}
	}

	public class DenyPutInItemArgs : DenyTriggerArgs {
		private readonly AbstractCharacter pickingChar;
		private readonly AbstractItem manipulatedItem;
		private readonly AbstractItem container;

		public DenyPutInItemArgs(AbstractCharacter pickingChar, AbstractItem manipulatedItem, AbstractItem container)
			: base(DenyResultMessages.Allow, pickingChar, manipulatedItem, container) {
			this.pickingChar = pickingChar;
			this.manipulatedItem = manipulatedItem;
			this.container = container;
		}

		public AbstractCharacter PickingChar {
			get {
				return this.pickingChar;
			}
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}

		public AbstractItem Container {
			get {
				return this.container;
			}
		}

	}

	public class DenyPutOnGroundArgs : DenyTriggerArgs {
		private readonly AbstractCharacter puttingChar;
		private readonly AbstractItem manipulatedItem;

		public DenyPutOnGroundArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, IPoint4D point)
			: base(DenyResultMessages.Allow, puttingChar, manipulatedItem, point) {
			this.puttingChar = puttingChar;
			this.manipulatedItem = manipulatedItem;
		}

		public IPoint4D Point {
			get {
				return (IPoint4D) this.Argv[3];
			}
			set {
				this.Argv[3] = value;
			}
		}

		public AbstractCharacter PuttingChar {
			get {
				return this.puttingChar;
			}
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}
	}

	public class ItemOnItemArgs : ScriptArgs {
		private readonly AbstractCharacter puttingChar;
		private readonly AbstractItem manipulatedItem;
		private readonly AbstractItem waitingItem;

		public ItemOnItemArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, AbstractItem waitingItem)
			: base(puttingChar, manipulatedItem, waitingItem) {
			this.puttingChar = puttingChar;
			this.manipulatedItem = manipulatedItem;
			this.waitingItem = waitingItem;
		}

		public AbstractCharacter PuttingChar {
			get {
				return this.puttingChar;
			}
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}

		public AbstractItem WaitingItem {
			get {
				return this.waitingItem;
			}
		}
	}

	public class ItemOnCharArgs : ScriptArgs {
		private readonly AbstractCharacter puttingChar;
		private readonly AbstractCharacter cont;
		private readonly AbstractItem manipulatedItem;

		public ItemOnCharArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, AbstractCharacter cont)
			: base(puttingChar, manipulatedItem, cont) {
			this.puttingChar = puttingChar;
			this.cont = cont;
			this.manipulatedItem = manipulatedItem;
		}

		public AbstractCharacter PuttingChar {
			get {
				return this.puttingChar;
			}
		}

		public AbstractCharacter Cont {
			get {
				return this.cont;
			}
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}
	}

	public class DenyEquipArgs : DenyTriggerArgs {
		private readonly AbstractCharacter puttingChar;
		private readonly AbstractCharacter cont;
		private readonly AbstractItem manipulatedItem;
		private readonly int layer;

		public DenyEquipArgs(AbstractCharacter puttingChar, AbstractItem manipulatedItem, AbstractCharacter cont, int layer)
			: base(DenyResultMessages.Allow, puttingChar, manipulatedItem, cont, layer) {

			this.puttingChar = puttingChar;
			this.cont = cont;
			this.manipulatedItem = manipulatedItem;
			this.layer = layer;
		}

		public AbstractCharacter PuttingChar {
			get {
				return this.puttingChar;
			}
		}

		public AbstractCharacter Cont {
			get {
				return this.cont;
			}
		}

		public AbstractItem ManipulatedItem {
			get {
				return this.manipulatedItem;
			}
		}

		public int Layer {
			get {
				return this.layer;
			}
		}
	}
}