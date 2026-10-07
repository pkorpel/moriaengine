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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine {

	[CLSCompliant(false)]
	public interface ICorpseEquipInfo {
		uint FlaggedUid { get; }
		int Layer { get; }
		int Color { get; }
		int Model { get; }
	}

	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public abstract partial class AbstractItem : Thing, ICorpseEquipInfo {

		private static int instances;
		private static readonly List<TriggerGroup> registeredTGs = new List<TriggerGroup>();


		public static int Instances {
			get {
				return instances;
			}
		}

		private int amount;
		private string name;

		private enum InternalFlag : byte {
			SyncMask = (ItemSyncQueue.ItemSyncFlags.Resend | ItemSyncQueue.ItemSyncFlags.ItemUpdate | ItemSyncQueue.ItemSyncFlags.Property),
			ItemFlag1 = 0x08,
			ItemFlag2 = 0x10,
			ItemFlag3 = 0x20,
			ItemFlag4 = 0x40,
			ItemFlag5 = 0x80
		}

		//ItemSyncQueue.SyncFlags: 1, 2, 4
		//Don't set the disconnected flag (0x01) by tweaking flags.
		private InternalFlag flags;


		private TriggerGroup type;
		internal object contentsOrComponents;

		#region Constructors
		protected AbstractItem(ThingDef myDef)
			: base(myDef) {
			instances++;
			this.type = ((AbstractItemDef) myDef).Type;
			this.amount = 1;

			//MoveInsideContainer((ushort) Globals.dice.Next(20,100),(ushort) Globals.dice.Next(20,100));
			//cont.InternalAddItem(this);//also resends
			Globals.LastNewItem = this;
		}

		protected AbstractItem(AbstractItem copyFrom)
			: base(copyFrom) { //copying constuctor
			instances++;
			this.name = copyFrom.name;
			this.flags = copyFrom.flags;
			this.amount = copyFrom.amount;
			this.type = copyFrom.type;
			Globals.LastNewItem = this;
		}
		#endregion Constructors

		internal ItemSyncQueue.ItemSyncFlags SyncFlags {
			get {
				return (ItemSyncQueue.ItemSyncFlags) (this.flags & InternalFlag.SyncMask);
			}
			set {
				this.flags = ((this.flags & ~InternalFlag.SyncMask) | ((InternalFlag) value & InternalFlag.SyncMask));
			}
		}

		public AbstractItemDef TypeDef {
			get {
				return (AbstractItemDef) this.Def;
			}
		}

		public TriggerGroup Type {
			get {
				return this.type;
			}
			set {
				this.type = value;
			}
		}

		public virtual byte DirectionByte {
			get {
				return 0;
			}
		}

		public bool IsStackable {
			get {
				return ((AbstractItemDef) this.Def).IsStackable;
			}
		}

		/// <summary>
		/// Gets the count of items inside this item, if it is a container.
		/// </summary>
		public int Count {
			get {
				ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
				if (tll == null) {
					return 0;
				}
				return tll.count;
			}
		}

		public bool IsEmpty {
			get {
				return (this.Count == 0);
			}
		}

		public int Amount {
			get {
				return this.amount;
			}
			set {
				if (value != this.amount) {
					this.InvalidateAosToolTips();
					ItemSyncQueue.AboutToChange(this);
					Thing c = this.Cont;
					if (c != null) {
						c.AdjustWeight(this.def.Weight * (value - this.amount));
					}
					this.amount = value;
				}
			}
		}

		protected internal override void AdjustWeight(float adjust) {
			Thing c = this.Cont;
			if (c != null) {
				c.AdjustWeight(adjust);
			}
		}

		[CLSCompliant(false)]
		public ushort ShortAmount {
			get {
				return (ushort) Math.Min(ushort.MaxValue, this.amount);
			}
		}

		public virtual bool IsTwoHanded {
			get {
				return false;
			}
		}

		[CLSCompliant(false)]
		public sealed override uint FlaggedUid {
			get {
				return (uint) (this.Uid | 0x40000000);
			}
		}

		//public override bool Flag_Disconnected {
		//    get {
		//        return ((this.flags & itemFlagDisconnected) == itemFlagDisconnected);
		//    }
		//    internal set {				
		//        byte newFlags;
		//        if (value) {
		//            newFlags = this.flags | itemFlagDisconnected;
		//        } else {
		//            newFlags = this.flags & ~itemFlagDisconnected;
		//        }

		//        if (this.flags != newFlags) {
		//            if (value) {
		//                OpenedContainers.SetContainerClosed(this);
		//                this.RemoveFromView();
		//            }
		//            ItemSyncQueue.AboutToChange(this);
		//            this.flags = newFlags;
		//            if (value) {
		//                this.GetMap().Disconnected(this);
		//            } else {
		//                this.GetMap().Reconnected(this);
		//            }
		//        }
		//    }
		//}

		protected bool ProtectedFlag1 {
			get {
				return ((this.flags & InternalFlag.ItemFlag1) == InternalFlag.ItemFlag1);
			}
			set {
				if (value) {
					this.flags |= InternalFlag.ItemFlag1;
				} else {
					this.flags &= ~InternalFlag.ItemFlag1;
				}
			}
		}

		protected bool ProtectedFlag2 {
			get {
				return ((this.flags & InternalFlag.ItemFlag2) == InternalFlag.ItemFlag2);
			}
			set {
				if (value) {
					this.flags |= InternalFlag.ItemFlag2;
				} else {
					this.flags &= ~InternalFlag.ItemFlag2;
				}
			}
		}

		protected bool ProtectedFlag3 {
			get {
				return ((this.flags & InternalFlag.ItemFlag3) == InternalFlag.ItemFlag3);
			}
			set {
				if (value) {
					this.flags |= InternalFlag.ItemFlag3;
				} else {
					this.flags &= ~InternalFlag.ItemFlag3;
				}
			}
		}

		protected bool ProtectedFlag4 {
			get {
				return ((this.flags & InternalFlag.ItemFlag4) == InternalFlag.ItemFlag4);
			}
			set {
				if (value) {
					this.flags |= InternalFlag.ItemFlag4;
				} else {
					this.flags &= ~InternalFlag.ItemFlag4;
				}
			}
		}

		protected bool ProtectedFlag5 {
			get {
				return ((this.flags & InternalFlag.ItemFlag5) == InternalFlag.ItemFlag5);
			}
			set {
				if (value) {
					this.flags |= InternalFlag.ItemFlag5;
				} else {
					this.flags &= ~InternalFlag.ItemFlag5;
				}
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract bool Flag_NonMovable {
			get;
			set;
		}

		public bool IsInVisibleLayer {
			get {
				if (this.point4d.x == 7000) {
					return (this.point4d.z < 26);
				}
				return true;
			}
		}

		public virtual int Layer {
			get {
				return (int) LayerNames.None;
			}
		}

		public virtual short Gump {
			get {
				return 0;
			}
		}

		public override Region Region {
			get {
				Thing top = this.TopObj();
				return this.GetMap().GetRegionFor(top.point4d);
			}
		}

		//commands:
		public sealed override void Resend() {
			ItemSyncQueue.Resend(this);
		}

		public override AbstractItem FindCont(int index) {
			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				return null;
			}
			return (AbstractItem) tll[index];
		}

		public void OpenTo(AbstractCharacter viewer) {
			GameState viewerState = viewer.GameState;
			this.OpenTo(viewer, viewerState, viewerState.Conn);
		}

		public void OpenTo(AbstractCharacter viewer, GameState viewerState, TcpConnection<GameState> viewerConn) {
			if (this.IsContainer) {
				this.ThrowIfDeleted();
				viewer.ThrowIfDeleted();

				//send container
				OpenedContainers.SetContainerOpened(viewer, this);
				DrawContainerOutPacket packet = Pool<DrawContainerOutPacket>.Acquire();
				packet.PrepareContainer(this.FlaggedUid, this.Gump);
				viewerConn.SendSinglePacket(packet);

				PacketSequences.SendContainerContentsWithPropertiesTo(viewer, viewerState, viewerConn, this);

				this.Trigger_ContainerOpen(viewer, viewerState, viewerConn);
			} else {
				throw new SEException("The item (" + this + ") is not a container");
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private void Trigger_ContainerOpen(AbstractCharacter viewer, GameState viewerState, TcpConnection<GameState> viewerConn) {
			ScriptArgs sa = new ScriptArgs(viewer, viewerState, viewerConn);
			this.TryTrigger(TriggerKey.containerOpen, sa);
			try {
				this.On_ContainerOpen(viewer, viewerState, viewerConn);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ContainerOpen(AbstractCharacter viewer, GameState viewerState, TcpConnection<GameState> viewerConn) {
		}

		//[Obsolete("Use the alternative from Networking namespace", false)]
		//public virtual void On_ContainerOpen(GameConn viewerConn) {

		//}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public virtual ItemOnGroundUpdater GetOnGroundUpdater() {
			ItemOnGroundUpdater iogu = ItemOnGroundUpdater.GetFromCache(this);
			if (iogu == null) {
				iogu = new ItemOnGroundUpdater(this);
			}
			return iogu;
		}

		internal sealed override void Trigger_Destroy() {
			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				tll.BeingDeleted();
			}
			base.Trigger_Destroy();
			this.MakeLimbo();
			instances--;
		}

		public void EmptyCont() {
			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll != null) {
				tll.Empty();
			}
		}

		public override string Name {
			get
			{
				if (this.name == null) {
					return this.Amount > 1 ? this.TypeDef.PluralName : this.TypeDef.Name;
				}
				return this.name;
			}
			set {
				this.InvalidateAosToolTips();
				if (!string.IsNullOrEmpty(value)) {
					this.name = string.Intern(value);
				} else {
					this.name = null;
				}
			}
		}

		public override void On_Create() {
			AbstractItemDef aidef = (AbstractItemDef) this.def;
			AbstractItemDef dupe = aidef.DupeItem;
			if (dupe != null) {
				this.def = dupe;
			}
			this.Type = aidef.Type;
			base.On_Create();
		}

		public void Flip() {
			this.Model = this.TypeDef.GetNextFlipModel(this.Model);
		}

		public new static void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}

		public override void Trigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.Run(this, tk, sa);
			}
			base.TryTrigger(tk, sa);
			if (this.type != null) {
				this.type.Run(this, tk, sa);
			}
		}

		public override void TryTrigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				tg.TryRun(this, tk, sa);
			}
			base.TryTrigger(tk, sa);
			if (this.type != null) {
				this.type.TryRun(this, tk, sa);
			}
		}

		public override TriggerResult CancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				if (TagMath.Is1(tg.Run(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}

			if (TriggerResult.Cancel != base.CancellableTrigger(tk, sa)) {
				if (this.type != null) {
					if (TagMath.Is1(this.type.Run(this, tk, sa))) {
						return TriggerResult.Cancel;
					}
				}
			} else {
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		public override TriggerResult TryCancellableTrigger(TriggerKey tk, ScriptArgs sa) {
			this.ThrowIfDeleted();
			for (int i = 0, n = registeredTGs.Count; i < n; i++) {
				TriggerGroup tg = registeredTGs[i];
				if (TagMath.Is1(tg.TryRun(this, tk, sa))) {
					return TriggerResult.Cancel;
				}
			}

			if (TriggerResult.Cancel != base.TryCancellableTrigger(tk, sa)) {
				if (this.type != null) {
					if (TagMath.Is1(this.type.TryRun(this, tk, sa))) {
						return TriggerResult.Cancel;
					}
				}
			} else {
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		internal sealed override TriggerResult Trigger_SpecificClick(AbstractCharacter clickingChar, ScriptArgs sa) {
			//helper method for Trigger_Click
			var result = clickingChar.TryCancellableTrigger(TriggerKey.itemClick, sa);
			if (result != TriggerResult.Cancel) {
				result = clickingChar.On_ItemClick(this);
			}
			return result;
		}

		[SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#"), SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		internal void Trigger_Step(AbstractCharacter steppingChar, bool repeated, MovementType movementType) {
			this.ThrowIfDeleted();
			ScriptArgs sa = new ScriptArgs(steppingChar, this, repeated, movementType);

			steppingChar.TryTrigger(TriggerKey.itemStep, sa);
			steppingChar.On_ItemStep(this, repeated, movementType);//sends true if repeated=1
			this.TryTrigger(TriggerKey.step, sa);
			this.On_Step(steppingChar, repeated, movementType);
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_Step(AbstractCharacter stepping, bool repeated, MovementType movementType) {
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override void Save(SaveStream output) {
			base.Save(output);
			AbstractItemDef def = this.TypeDef;
			if ((this.name != null) && (!def.Name.Equals(this.name))) {
				output.WriteValue("name", this.name);
			}
			Thing c = this.Cont;
			if (c != null) {
				output.WriteValue("cont", c);
			}
			if (this.amount != 1) {
				output.WriteValue("amount", this.amount);
			}
			if (this.flags != 0) {
				output.WriteValue("flags", this.flags);
			}
			if (this.type != def.Type) {
				output.WriteLine("type=" + this.type.Defname);
			}
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "cont":
					ObjectSaver.Load(valueString, this.LoadCont_Delayed, filename, line);
					break;
				case "p":
					base.LoadLine(filename, line, valueName, valueString);//loads the position
					if (this.IsInContainer) {
						this.MakePositionInContLegal();
					}
					break;

				case "name":
					this.name = string.Intern(ConvertTools.LoadSimpleQuotedString(valueString));
					break;
				case "amount":
					this.amount = ConvertTools.ParseInt32(valueString);
					break;
				case "flags":
					this.flags = (InternalFlag) ConvertTools.ParseByte(valueString);
					break;
				case "type":
					this.type = TriggerGroup.GetByDefname(valueString);
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}


		public sealed override bool IsItem {
			get {
				return true;
			}
		}

		public sealed override bool IsEquipped {
			get {
				return (this.Cont is AbstractCharacter);
			}
		}

		public sealed override bool IsOnGround {
			get {
				return (this.Cont == null);
			}
		}

		public sealed override bool IsInContainer {
			get {
				Thing c = this.Cont;
				return (c != null && c.IsItem);
			}
		}

		public sealed override Thing TopObj() {
			Thing c = this.Cont;
			if (c != null) {
				return c.TopObj();
			}
			return this;
		}

		//returns true if this is in given container or its subcontainers
		public bool IsWithinCont(Thing container) {
			Thing myCont = this.Cont;
			if (myCont == container) {
				return true;
			}
			if (myCont != null) {
				if (myCont.IsItem) {
					return ((AbstractItem) myCont).IsWithinCont(container);
				}
			}
			return false;
		}

		public sealed override IEnumerator<AbstractItem> GetEnumerator() {
			this.ThrowIfDeleted();
			ThingLinkedList tll = this.contentsOrComponents as ThingLinkedList;
			if (tll == null) {
				return EmptyReadOnlyGenericCollection<AbstractItem>.instance;
			}
			return tll.GetItemEnumerator();
		}

		public sealed override void InvalidateAosToolTips() {
			ItemSyncQueue.PropertiesChanged(this);
			base.InvalidateAosToolTips();
		}

		//if true, this is mutually exclusive with other items that return true here, to fit on the same map spot (Map.CanFit)
		public virtual bool BlocksFit {
			get {
				return false;
			}
		}
	}
}
