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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;

#if TESTRUNUO
using RunUO_Compression = Server.Network.Compression;
#endif

namespace SteamEngine {

	/*
		Unused (potential) flags:
		In 'direction':
			0x08, 0x10, 0x20, and 0x40 (0x80 is Flag_Moving, which is used to signal information about
			movement to NetState). Note that anyone checking the Direction property, or dir, or facing,
			will not see the flags, since they are hidden by those properties.
		'gender':
			This is currently a byte with valid values being 0 or 1, but probably should be eliminated
			and made into a flag on some other variable. One argument for leaving it as a byte would
			be if scripters want to script races with more than two genders (or two genders plus genderless).
			If we do leave it as a byte, then bits 2-7 (0x02 to 0x80) are available for use as flags, but
			we should use the higher ones first in case a scripter does decide to increase the # of genders
			for a specific race. Also, if we do add flags here, gender would have to be hidden behind a
			masking property, like direction is.
	*/

	/**
		A character.
	*/
	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public abstract partial class AbstractCharacter : Thing, ISrc {
		internal const int sentLayers = 25;//0-24

		//In most cases, you should use Flag_* properties to set/get flags. Rarely, you may need to directly modify flags,
		//	in the SE core (not in scripts), which is why this is internal.
		//But whatever you do, NEVER set the disconnected flag by tweaking flags.
		private enum DirectionAndFlag : short {
			Zero = 0,
			DirectionMask = 0x0007,
			DirFlagDisconnected = 0x0008,
			DirFlagMoving = 0x0010,
			DirFlag1 = 0x0020,
			DirFlag2 = 0x0040,
			DirFlag3 = 0x0080,
			DirFlag4 = 0x0100,
			DirFlag5 = 0x0200,
			DirFlag6 = 0x0400,
			DirFlag7 = 0x0800
		}

		private AbstractAccount account;
		private string name;
		private DirectionAndFlag directionAndFlags = DirectionAndFlag.Zero;
		internal ThingLinkedList visibleLayers;//layers 0..24
		internal ThingLinkedList invisibleLayers;//layers (26..29) + (32..max)
		internal ThingLinkedList specialLayer;//layer 30
		internal AbstractItem draggingLayer;

		private static int instances;

		internal CharSyncQueue.CharState syncState; //don't touch

		public static int Instances {
			get {
				return instances;
			}
		}

		protected AbstractCharacter(ThingDef myDef)
			: base(myDef) {
			instances++;
			this.name = myDef.Name;
			Globals.LastNewChar = this;
		}

		protected AbstractCharacter(AbstractCharacter copyFrom)
			: base(copyFrom) { //copying constuctor
			instances++;
			this.account = copyFrom.account;
			this.name = copyFrom.name;
			this.directionAndFlags = copyFrom.directionAndFlags;
			Globals.LastNewChar = this;
			this.GetMap().Add(this);
		}

		//The client apparently doesn't want characters' uids to be flagged with anything.
		[CLSCompliant(false)]
		public sealed override uint FlaggedUid {
			get {
				return (uint) this.Uid;
			}
		}

		public AbstractCharacterDef TypeDef {
			get {
				return (AbstractCharacterDef) this.def;
			}
		}

		public override bool IsPlayer {
			get {
				return (this.Account != null);
			}
		}

		public bool IsLingering {
			get {
				return this.IsPlayer && !this.Flag_Disconnected && this.GameState == null;
			}
		}

		public AbstractAccount Account {
			get {
				return this.account;
			}
		}

		//public GameConn Conn {
		//    get {
		//        if (this.account != null) {
		//            return this.account.Conn;
		//        } else {
		//            return null;
		//        }
		//    }
		//}

		public GameState GameState {
			get
			{
				if (this.account != null) {
					return this.account.GameState;
				}
				return null;
			}
		}

		public bool IsOnline {
			get
			{
				if (this.account != null) {
					return this.account.IsOnline;
				}
				return false;
			}
		}

		public sealed override bool IsChar {
			get {
				return true;
			}
		}

		public sealed override bool CanContain {
			get {
				return true;
			}
		}

		public override string Name {
			get {
				return this.name;
			}
			set {
				this.InvalidateAosToolTips();
				CharSyncQueue.AboutToChangeName(this);
				this.name = value;
			}
		}

		public int MountItem {
			get {
				return this.TypeDef.MountItem;
			}
		}

		public abstract AbstractCharacter Mount { get; set; }

		/// <summary>This is the name that displays in one's Paperdoll, i.e. including his title(s)</summary>
		public abstract string PaperdollName { get; }

		public abstract bool IsFemale { get; }
		public abstract bool IsMale { get; }


		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract bool Flag_Insubst {
			get;
			set;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract bool Flag_WarMode { get; set; }
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract bool Flag_Riding { get; }


		#region Direction byte/Flags
		/// <summary>The Direction this character is facing.</summary>
		public Direction Direction {
			get {
				return (Direction) this.DirectionByte;
			}
			set {
				this.DirectionByte = (byte) value;
			}
		}

		/// <summary>Direction, as a byte.</summary>
		internal byte DirectionByte {
			get {
				return (byte) (this.directionAndFlags & DirectionAndFlag.DirectionMask);
			}
			set {
				DirectionAndFlag newValue = (this.directionAndFlags & ~DirectionAndFlag.DirectionMask) | ((DirectionAndFlag) value & DirectionAndFlag.DirectionMask);
				if (this.directionAndFlags != newValue) {
					CharSyncQueue.AboutToChangeDirection(this, false);
					this.directionAndFlags = newValue;
				}
			}
		}

		protected bool ProtectedFlag1 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag1) == DirectionAndFlag.DirFlag1);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag1;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag1;
				}
			}
		}

		protected bool ProtectedFlag2 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag2) == DirectionAndFlag.DirFlag2);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag2;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag2;
				}
			}
		}

		protected bool ProtectedFlag3 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag3) == DirectionAndFlag.DirFlag3);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag3;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag3;
				}
			}
		}

		protected bool ProtectedFlag4 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag4) == DirectionAndFlag.DirFlag4);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag4;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag4;
				}
			}
		}

		protected bool ProtectedFlag5 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag5) == DirectionAndFlag.DirFlag5);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag5;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag5;
				}
			}
		}

		protected bool ProtectedFlag6 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag6) == DirectionAndFlag.DirFlag6);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag6;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag6;
				}
			}
		}

		protected bool ProtectedFlag7 {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlag7) == DirectionAndFlag.DirFlag7);
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlag7;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlag7;
				}
			}
		}
		#endregion Direction byte/Flags

		#region Login/Logout
		public override bool Flag_Disconnected {
			get {
				return ((this.directionAndFlags & DirectionAndFlag.DirFlagDisconnected) == DirectionAndFlag.DirFlagDisconnected);
			}
		}

		private void SetFlag_Disconnected(bool value) {
			DirectionAndFlag newValue = (value ? (this.directionAndFlags | DirectionAndFlag.DirFlagDisconnected) : (this.directionAndFlags & ~DirectionAndFlag.DirFlagDisconnected));

			if (this.directionAndFlags != newValue) {
				CharSyncQueue.AboutToChangeVisibility(this);
				this.directionAndFlags = newValue;
				if (value) {
					this.GetMap().Disconnected(this);
				} else {
					this.GetMap().Reconnected(this);
				}
			}
		}

		/**
			Only call this on players.
			If this character is logged in, forcefully and immediately logs them out.
			If this character is lingering (in game but disconnected), logs them out fully (making them be no longer in game).
			If this character is already logged out fully, this does nothing.
		*/
		public void LogoutFully() {
			if (this.IsPlayer) {
				if (this.IsLingering) {
					this.SetFlag_Disconnected(true);
				} else if (!this.Flag_Disconnected) {
					this.GameState.Conn.Close("LogoutFully called.");
					this.SetFlag_Disconnected(true);
				}	//else already logged out fully, do nothing
			} else {
				throw new InvalidOperationException("LogoutFully can only be called on player characters");
			}
		}

		/**
			Call this on both players and NPCs
			If this character is logged in, Close their connection, but they stay lingering
			If this character is lingering (in game but disconnected), logs them out fully (making them be no longer in game).
			If this character is already logged out fully, this does nothing.
			If this is a NPC, it simply goes to the Disconnect containers (of map) and gets it's Flag_Disconnected set to True
		*/
		public void Disconnect() {
			if (this.IsPlayer) {
				if (this.IsLingering) {
					this.SetFlag_Disconnected(true);
				} else if (!this.Flag_Disconnected) {
					this.GameState.Conn.Close("Disconnect called.");
				}	//else already logged out fully, do nothing
			} else {
				this.SetFlag_Disconnected(true);
			}
		}

		/**
			Call this on NPCs only
			If this is a NPC, it simply goes out of the Disconnect containers (of map) and gets it's Flag_Disconnected set to False
		*/
		public void Reconnect() {
			if (!this.IsPlayer) {
				this.SetFlag_Disconnected(false);
			} else {
				throw new InvalidOperationException("LogoutFully can only be called on npc characters");
			}
		}

		//is called by GameConn when relinking, which is after the Things have been loaded, but before
		//they're put into map
		internal void ReLinkToGameState() {
			this.directionAndFlags &= ~DirectionAndFlag.DirFlagDisconnected; //we're not disconnected, the Map needs to know
		}

		//Called by GameConn
		internal bool TryLogIn() {
			bool success = this.Trigger_LogIn() != TriggerResult.Cancel;
			if (success) {
				this.SetFlag_Disconnected(false);
			}
			return success;
		}

		//method: Trigger_LogIn
		//this method fires the @login trigger
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal TriggerResult Trigger_LogIn() {
			var result = this.TryCancellableTrigger(TriggerKey.login, null);
			if (result != TriggerResult.Cancel) {
				//@login did not return 1
				try {
					result = this.On_LogIn();
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return result;
		}

		//method: Trigger_LogOut
		//this method fires the @logout trigger
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void Trigger_LogOut() {
			this.TryTrigger(TriggerKey.logout, null);
			try {
				this.On_LogOut();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_LogOut() {

		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_LogIn() {
			return TriggerResult.Continue;
		}
		#endregion Login/Logout

		#region Persistence
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public override void Save(SaveStream output) {
			if (this.account != null) {
				output.WriteValue("account", this.account);
			}
			if (!this.Def.Name.Equals(this.name)) {
				output.WriteValue("name", this.name);
			}
			DirectionAndFlag flagsToSave = this.directionAndFlags;
			if (this.IsPlayer) {
				flagsToSave = flagsToSave | DirectionAndFlag.DirFlagDisconnected;//add the Disconnected flag, makes no sense to save a person "connected"
			}
			if (flagsToSave != 0) {
				output.WriteValue("directionAndFlags", flagsToSave);
			}
			base.Save(output);
		}

		//For loading.
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member"), SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
		public void LoadAccount_Delayed(object resolvedObject, string filename, int line) {
			AbstractAccount acc = (AbstractAccount) resolvedObject;

			int slot;
			if (acc.AttachCharacter(this, out slot)) {
				this.account = acc;

				this.FixDisconnectedFlagOnPlayers();
			} else {
				Logger.WriteError("The character " + this + " declares " + acc + " as it's account, but the Account is already full. Deleting.");
				this.Delete();
			}
		}

		//For loading
		public void FixDisconnectedFlagOnPlayers() {
			if (this.IsPlayer) {
				if (this.GameState == null) {
					this.directionAndFlags |= DirectionAndFlag.DirFlagDisconnected;
				} else {	//there shouldn't actually be anyone connected when this happens, but we check just in case.
					this.directionAndFlags &= ~DirectionAndFlag.DirFlagDisconnected;
				}
			}
		}

		//public void Disconnect() {
		//	if (Conn!=null) {
		//		Conn.Close("The disconnect command was used.");
		//	}
		//}

		//For loading.
		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				case "account":
					ObjectSaver.Load(valueString, this.LoadAccount_Delayed, filename, line);
					break;
				case "name":
					this.name = ConvertTools.LoadSimpleQuotedString(valueString);
					break;
				case "directionandflags":
					this.directionAndFlags = (DirectionAndFlag) ConvertTools.ParseInt16(valueString);
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}
		#endregion Persistence

		#region ISrc, plevels, messages sending

		//ISrc implementation
		public byte Plevel {
			get {
				AbstractAccount a = this.Account;
				if (a != null) {
					return a.PLevel;
				}
				return 0;
			}
			set {
				AbstractAccount a = this.Account;
				if (a != null) {
					a.PLevel = value;
				}//else ?
			}
		}

		public byte MaxPlevel {
			get {
				AbstractAccount a = this.Account;
				if (a != null) {
					return a.MaxPLevel;
				}
				return 0;
			}
		}

		public bool IsPlevelAtLeast(int plevel) {
			return (this.account != null && this.account.PLevel >= plevel);
		}

		public bool IsMaxPlevelAtLeast(int plevel) {
			return (this.account != null && this.account.MaxPLevel >= plevel);
		}

		//ISrc implementation
		public AbstractCharacter Character {
			get {
				return this;
			}
		}

		//ISrc implementation
		public void WriteLine(string line) {
			this.SysMessage(line);
		}

		public Language Language {
			get {
				GameState state = this.GameState;
				if (state != null) {
					return state.Language;
				}
				return Language.Default;
			}
		}

		public void AnnounceBug() {
			this.ClilocSysMessage(501634, 0x21);	//Error!  This is a bug, please report it!
			//0x21 = red, hopefully
		}

		public void SysMessage(string arg) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendSystemMessage(state.Conn, arg, -1);
			}
		}

		public void SysMessage(string arg, int color) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendSystemMessage(state.Conn, arg, color);
			}
		}

		public void ClilocSysMessage(int msg) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, -1, "");
			}
		}

		public void ClilocSysMessage(int msg, string args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, -1, args);
			}
		}
		public void ClilocSysMessage(int msg, params string[] args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, -1, args);
			}
		}

		public void ClilocSysMessage(int msg, int color) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, color, "");
			}
		}

		public void ClilocSysMessage(int msg, int color, string args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, color, args);
			}
		}

		public void ClilocSysMessage(int msg, int color, params string[] args) {
			GameState state = this.GameState;
			if (state != null) {
				PacketSequences.SendClilocSysMessage(state.Conn, msg, color, args);
			}
		}
		#endregion ISrc, messages sending

		#region Movement
		//Used with NetState, but at present this should be set when the character moves (walk/run/fly), and should remain
		//set for the rest of the cycle. Maybe there's a potential use for that in scripts, so this is public.
		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public bool Flag_Moving {
			get {
				return (this.directionAndFlags & DirectionAndFlag.DirFlagMoving) == DirectionAndFlag.DirFlagMoving;
			}
			set {
				if (value) {
					this.directionAndFlags |= DirectionAndFlag.DirFlagMoving;
				} else {
					this.directionAndFlags &= ~DirectionAndFlag.DirFlagMoving;
				}
			}
		}

		private class DefaultMovementSettings : IMovementSettings {
			public bool CanCrossLand { get { return true; } }
			public bool CanSwim { get { return false; } }
			public bool CanCrossLava { get { return false; } }
			public bool CanFly { get { return false; } }
			public bool IgnoreDoors { get { return false; } }
			public int ClimbPower { get { return 2; } } //max positive difference in 1 step
		}

		private static readonly IMovementSettings defaultMovement = new DefaultMovementSettings();

		public virtual IMovementSettings MovementSettings {
			get {
				return defaultMovement;
			}
			set {
				throw new SEException("Not supported");
			}
		}

		//player or npc walking
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public bool WalkRunOrFly(Direction dir, bool running, bool requested) {
			this.ThrowIfDeleted();
			this.Flag_Moving = true;
			dir = dir & Direction.Mask;

			if (this.Direction == dir) { //no dir change = step forward
				Point4D oldPoint = new Point4D(this.point4d);

				Map map = this.GetMap();
				int newZ, newY, newX;

				bool canMoveEverywhere = (this.IsPlayer && this.Plevel >= Globals.PlevelOfGM);
				if (!map.CheckMovement(oldPoint, this.MovementSettings, dir, canMoveEverywhere, out newX, out newY, out newZ)) {
					return false;
				}

				if (TriggerResult.Cancel == this.Trigger_Step(dir, running)) {
					return false;
				}

				//should we really be asking regions, even for npcs? -tar
				Region oldRegion = this.Region;
				Region newRegion = map.GetRegionFor(newX, newY);
				if (oldRegion != newRegion) {
					if (!Region.TryExitAndEnter(oldRegion, newRegion, this)) {
						return false;
					}
				}

				MovementType mt;
				if (running) {
					mt = MovementType.Running;
				} else {
					mt = MovementType.Walking;
				}
				if (requested) {
					mt |= MovementType.RequestedStep;
				}

				//move char, and send 0x77 to players nearby
				CharSyncQueue.AboutToChangePosition(this, mt);

				this.point4d.SetXYZ(newX, newY, newZ);
				this.ChangedP(oldPoint, mt);
			} else { //just changing direction, no steps
				CharSyncQueue.AboutToChangeDirection(this, requested);
				this.Direction = dir;
			}
			return true;
		}

		internal sealed override void SetPosImpl(int x, int y, int z, byte m) {
			if (Map.IsValidPos(x, y, m)) {
				CharSyncQueue.AboutToChangePosition(this, MovementType.Teleporting);
				Point4D oldP = this.P();
				Region oldRegion;
				if (Map.IsValidPos(oldP)) {
					oldRegion = Map.GetMap(oldP.M).GetRegionFor(oldP.X, oldP.Y);
				} else {
					oldRegion = StaticRegion.WorldRegion;
				}
				Region newRegion = Map.GetMap(m).GetRegionFor(x, y);
				if (oldRegion != newRegion) {
					Region.ExitAndEnter(oldRegion, newRegion, this);//forced exit & enter
				}
				this.point4d.SetXYZM(x, y, z, m);
				this.ChangedP(oldP, MovementType.Teleporting);
			} else {
				throw new SEException("Invalid position (" + x + "," + y + " on mapplane " + m + ")");
			}
		}

		private void ChangedP(Point4D oldP, MovementType movementType) {
			Map.ChangedP(this, oldP);
			AbstractCharacter self = this;
			if (self != null) {
				self.Trigger_NewPosition(oldP, movementType);
			}
		}


		private TriggerResult Trigger_Step(Direction dir, bool running) {
			if (TriggerResult.Cancel == this.TryCancellableTrigger(TriggerKey.step, new ScriptArgs(dir, running))) {
				return TriggerResult.Cancel;
			}
			try {
				return this.On_Step(dir, running);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); return TriggerResult.Cancel; }
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_Step(Direction direction, bool running) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public bool IsStandingOn(AbstractItem i) {
			int zdiff = Math.Abs(i.Z - this.Z);
			return (this.X == i.X && this.Y == i.Y && zdiff >= 0 && zdiff <= i.Height);
		}

		//public bool IsStandingOnCheckZOnly(AbstractItem i) {
		//    int zdiff = Math.Abs(i.Z - Z);
		//    Sanity.IfTrueThrow(X != i.X || Y != i.Y, "IsStandingOn_CheckZOnly called when the item is not actually on the same tile. " + this + " is at (" + X + "," + Y + "), and the item (" + i + ") is at (" + i.X + "," + i.Y + ").");
		//    return (zdiff >= 0 && zdiff <= i.Height);
		//}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		internal void Trigger_NewPosition(Point4D oldP, MovementType movementType) {
			this.TryTrigger(TriggerKey.newPosition, new ScriptArgs(oldP));
			try {
				this.On_NewPosition(oldP);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			foreach (AbstractItem itm in this.GetMap().GetItemsInRange(this.X, this.Y, 0)) {
				if (this.IsStandingOn(itm)) {
					itm.Trigger_Step(this, false, movementType);
				}
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_NewPosition(Point4D oldP) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemStep(AbstractItem i, bool repeated, MovementType movementType) {
		}
		#endregion Movement

		#region Gump/Dialog sending
		public Gump SendGump(Thing focus, GumpDef gumpDef, DialogArgs args) {
			GameState state = this.GameState;
			if (state != null) {
				Gump instance = gumpDef.InternalConstruct(focus, this, args);
				if (instance != null) {
					state.SentGump(instance);
					SendGumpMenuDialogPacket p = Pool<SendGumpMenuDialogPacket>.Acquire();
					p.Prepare(instance);
					state.Conn.SendSinglePacket(p);
					return instance;
				}
			}
			return null;
		}

		public void DialogClose(Gump instance, int buttonId) {
			this.DialogClose(instance.Uid, buttonId);
		}

		public void DialogClose(int gumpUid, int buttonId) {
			GameState state = this.GameState;
			if (state != null) {
				CloseGenericGumpOutPacket p = Pool<CloseGenericGumpOutPacket>.Acquire();
				p.Prepare(gumpUid, buttonId);
				state.Conn.SendSinglePacket(p);
			}
		}

		public void DialogClose(GumpDef def, int buttonId) {
			GameState state = this.GameState;
			if (state != null) {
				foreach (Gump gi in state.FindGumpInstances(def)) {
					this.DialogClose(gi.Uid, buttonId);
				}
			}
		}

		/// <summary>
		/// Looks into the dialogs dictionary and finds out whether the specified one is opened (visible) or not
		/// </summary>
		public bool HasOpenedDialog(GumpDef def) {
			GameState state = this.GameState;
			if (state != null) {
				if (state.FindGumpInstances(def).Count != 0) {
					//nasli jsme otevrenou instanci tohoto gumpu
					return true;
				}
			}
			return false;
		}
		#endregion Gump/Dialog sending

		#region Click and Dclick
		//helper method for Trigger_Click
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal sealed override TriggerResult Trigger_SpecificClick(AbstractCharacter clickingChar, ScriptArgs sa) {
			var result = clickingChar.TryCancellableTrigger(TriggerKey.charClick, sa);
			if (result != TriggerResult.Cancel) {

				try {
					result = clickingChar.On_CharClick(this);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
			return result;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_ItemClick(AbstractItem clickedOn) {
			//I clicked an item
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_CharClick(AbstractCharacter clickedOn) {
			//I clicked a char
			return TriggerResult.Continue;
		}

		public void ShowPaperdollToSrc() {
			this.ShowPaperdollTo(Globals.SrcCharacter);
		}

		public void ShowPaperdollTo(AbstractCharacter viewer) {
			if (viewer != null) {
				GameState state = viewer.GameState;
				if (state != null) {
					this.ShowPaperdollTo(viewer, state, state.Conn);
				}
			}
		}

		public void ShowPaperdollTo(AbstractCharacter viewer, GameState viewerState, TcpConnection<GameState> viewerConn) {
			bool canEquip = true;
			if (viewer != this) {
				canEquip = viewer.CanEquipItemsOn(this);
			}
			OpenPaperdollOutPacket packet = Pool<OpenPaperdollOutPacket>.Acquire();
			packet.Prepare(this, canEquip);
			viewerConn.SendSinglePacket(packet);

			if (Globals.UseAosToolTips && viewerState.Version.AosToolTips) {
				Language language = viewerState.Language;
				foreach (AbstractItem equipped in this.VisibleEquip) {
					AosToolTips toolTips = equipped.GetAosToolTips(language);
					if (toolTips != null) {
						toolTips.SendIdPacket(viewerState, viewerConn);
					}
				}
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_ItemDClick(AbstractItem dClicked) {
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual void On_CharDClick(AbstractCharacter dClicked) {
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyItemDClick(DenyClickArgs args) {
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_DenyCharDClick(DenyClickArgs args) {
			//only item has a default implementation... char can be dclicked even if outa range (paperdoll)
			return TriggerResult.Continue;
		}
		#endregion Click and Dclick

		#region Speech
		//this method fires the [speech] triggers
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal SpeechResult Trigger_Hear(SpeechArgs sa) {
			SpeechResult result = (SpeechResult) this.TryCancellableTrigger(TriggerKey.hear, sa);

			if (SpeechResult.ActedUponExclusively != result) {
				try {
					result = this.On_Hear(sa);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}

			return result;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual SpeechResult On_Hear(SpeechArgs args) {
			return SpeechResult.IgnoredOrActedUpon;
		}

		//cancellable because of things like Guild speech, etc.
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal TriggerResult Trigger_Say(string speech, SpeechType type, int[] keywords) {
			if (this.IsPlayer) {
				ScriptArgs sa = new ScriptArgs(speech, type, keywords);
				if (TriggerResult.Cancel != this.TryCancellableTrigger(TriggerKey.say, sa)) {
					try {
						return this.On_Say(speech, type, keywords);
					} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				}
			}
			return TriggerResult.Continue;
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public virtual TriggerResult On_Say(string speech, SpeechType type, int[] keywords) {
			return TriggerResult.Continue;
		}
		#endregion Speech

		#region Status, skills, stats
		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public void ShowStatusBarTo(AbstractCharacter viewer, TcpConnection<GameState> viewerConn) {
			StatusBarInfoOutPacket packet = Pool<StatusBarInfoOutPacket>.Acquire();

			if (this == viewer) {
				packet.Prepare(this, StatusBarType.Me);
			} else if (viewer.CanRename(this)) {
				packet.Prepare(this, StatusBarType.Pet);
			} else {
				packet.Prepare(this, StatusBarType.Other);
			}

			viewerConn.SendSinglePacket(packet);
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public void ShowSkillsTo(TcpConnection<GameState> viewerConn, GameState viewerState) {
			SendSkillsOutPacket packet = Pool<SendSkillsOutPacket>.Acquire();
			packet.PrepareAllSkillsUpdate(this.Skills, viewerState.Version.DisplaySkillCaps);
			viewerConn.SendSinglePacket(packet);
		}

		//public abstract ISkill[] Skills { get; }
		public abstract IEnumerable<ISkill> Skills { get; }
		public abstract void SetRealSkillValue(int id, int value);
		public abstract void SetSkillLockType(int id, SkillLockType type);
		public abstract ISkill GetSkillObject(int id);
		public abstract int GetSkill(int id);

		/// <summary>Gets called by the core when the player presses the skill button/runs the macro</summary>
		public abstract void SelectSkill(AbstractSkillDef skillDef);

		/// <summary>Gets called by the core when the player presses the spell icon/runs the macro</summary>
		public abstract void TryCastSpellFromBook(int spellid);

		public abstract byte StatLockByte { get; }
		public abstract StatLockType StrLock { get; set; }
		public abstract StatLockType DexLock { get; set; }
		public abstract StatLockType IntLock { get; set; }

		public abstract short Hits { get; set; }
		public abstract short MaxHits { get; set; }
		public abstract short Stam { get; set; }
		public abstract short MaxStam { get; set; }
		public abstract short Mana { get; set; }
		public abstract short MaxMana { get; set; }
		public abstract short Str { get; set; }
		public abstract short Dex { get; set; }
		public abstract short Int { get; set; }
		public abstract long Gold { get; }

		//Tithing points can be reduced if you do something to displease your god, like murdering innocents.
		//They can even go negative (but not from using powers, which work only with enough points).
		//You only gain points by sacrificing gold (that balances this with other magic classes since this has no reagent costs).
		public abstract short TithingPoints { get; set; }
		/// <summary>Displays in client status as Physical resistance by default</summary>
		public virtual short StatusArmorClass { get { return 0; } }
		/// <summary>Displays in client status as Energy resistance by default</summary>
		public virtual short StatusMindDefense { get { return 0; } }

		//Resistances do not have effs. Negative values impart penalties.
		/// <summary>Displays in client status as Fire resistance by default</summary>
		public virtual short ExtendedStatusNum01 { get { return 0; } }
		/// <summary>Displays in client status as Cold resistance by default</summary>
		public virtual short ExtendedStatusNum02 { get { return 0; } }
		/// <summary>Displays in client status as Poison resistance by default</summary>
		public virtual short ExtendedStatusNum03 { get { return 0; } }
		/// <summary>Displays in client status as Luck by default</summary>
		public virtual short ExtendedStatusNum04 { get { return 0; } }
		/// <summary>Displays in client status as Min damage by default</summary>
		public virtual short ExtendedStatusNum05 { get { return 0; } }
		/// <summary>Displays in client status as Max damage by default</summary>
		public virtual short ExtendedStatusNum06 { get { return 0; } }
		/// <summary>Displays in client status as Current pet count by default</summary>
		public virtual byte ExtendedStatusNum07 { get { return 0; } }
		/// <summary>Displays in client status as Max pet count by default</summary>
		public virtual byte ExtendedStatusNum08 { get { return 0; } }
		/// <summary>Displays in client status as Stat cap by default</summary>
		public virtual short ExtendedStatusNum09 { get { return 0; } }
		#endregion Status, skills, stats

		#region Resend/Resync/Update
		public sealed override void Resend() {
			CharSyncQueue.Resend(this);
		}

		public void Resync() {
			GameState state = this.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;

				//PacketGroup pg = PacketGroup.AcquireSingleUsePG();

				//pg.AcquirePacket<SeasonalInformationOutPacket>().Prepare(this.Season, this.Cursor);
				//pg.AcquirePacket<SetFacetOutPacket>().Prepare(this.GetMap().Facet);
				//pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, this);
				//pg.AcquirePacket<ClientViewRangeOutPacket>().Prepare(state.UpdateRange);
				////(Not To-do, or to-do much later on): 0xbf map patches (INI flag) (.. We don't need this on custom maps, and it makes it more complicated to load the maps/statics)
				////(TODO): 0x4e and 0x4f personal and global light levels
				//conn.SendPacketGroup(pg);

				PreparedPacketGroups.SendWarMode(conn, this.Flag_WarMode);

				PacketSequences.SendCharInfoWithPropertiesTo(this, state, conn, this);

				this.SendNearbyStuffTo(state, conn);

				foreach (AbstractItem con in OpenedContainers.GetOpenedContainers(this)) {
					if (!con.IsEmpty) {
						PacketSequences.SendContainerContentsWithPropertiesTo(this, state, conn, con);
					}
				}
			}
		}

		public void SendNearbyStuff() {
			GameState state = this.GameState;
			if (state != null) {
				TcpConnection<GameState> conn = state.Conn;
				this.SendNearbyStuffTo(state, conn);
			}
		}

		private void SendNearbyStuffTo(GameState state, TcpConnection<GameState> conn) {
			ImmutableRectangle rect = new ImmutableRectangle(this, this.UpdateRange);
			Map map = this.GetMap();

			foreach (Thing t in map.GetThingsInRectangle(rect)) {
				this.ProcessSendNearbyThing(state, conn, t);
			}

			if (state.AllShow) {
				foreach (Thing thing in map.GetDisconnectsInRectangle(rect)) {
					this.ProcessSendNearbyThing(state, conn, thing);
				}
			}
		}

		private void ProcessSendNearbyThing(GameState state, TcpConnection<GameState> conn, Thing t) {
			AbstractItem item = t as AbstractItem;
			if (item != null) {
				if (this.CanSeeForUpdate(item).Allow) {
					item.GetOnGroundUpdater().SendTo(this, state, conn);
					PacketSequences.TrySendPropertiesTo(state, conn, item);
				}
			} else {
				AbstractCharacter ch = (AbstractCharacter) t;
				if ((this != ch) && this.CanSeeForUpdate(ch).Allow) {
					PacketSequences.SendCharInfoWithPropertiesTo(this, state, conn, ch);
					UpdateCurrentHealthOutPacket packet = Pool<UpdateCurrentHealthOutPacket>.Acquire();
					packet.Prepare(ch.FlaggedUid, ch.Hits, ch.MaxHits, false);
					conn.SendSinglePacket(packet);
				}
			}
		}
		#endregion Resend/Resync/Update


		//TODO?: abstract Damage methods

		/**
			Attaches this character to an account and makes them a player.
			This could be used by the 'control' command.
			Used by playercreation script
				
			@param acc The account to attach them to.
		*/
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public int MakeBePlayer(AbstractAccount acc) {
			int slot;
			if (acc.AttachCharacter(this, out slot)) {
				CharSyncQueue.Resend(this);
				this.account = acc;
				this.GetMap().MadeIntoPlayer(this);
			} else {
				throw new SEException("That account (" + acc + ") is already full.");
			}
			return slot;
		}

		internal sealed override void Trigger_Destroy() {
			AbstractAccount acc = this.Account;
			if (acc != null) {
				GameState state = acc.GameState;
				if ((state != null) && (state.Character == this)) {
					state.Conn.Close("Character being deleted");
				}

				acc.DetachCharacter(this);
			}

			foreach (AbstractItem i in this) {
				i.InternalDeleteNoRFV();//no updates, because it will disappear entirely
			}

			base.Trigger_Destroy();
			instances--;

			if (!this.IsLimbo) {
				this.GetMap().Remove(this);
				MarkAsLimbo(this);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public abstract AbstractItem GetBackpack();

		public void EmptyCont() {
			this.ThrowIfDeleted();
			foreach (AbstractItem i in this) {
				i.InternalDelete();
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", MessageId = "Member")]
		public abstract void Trigger_PlayerAttackRequest(AbstractCharacter target);

		/*
			Method: CanRename
				Determines if this character can rename another character.
			Parameters:
				to - The character to be renamed
			Returns:
				True if 'to' is an NPC with an owner, and this is its owner.
		*/
		public abstract bool CanRename(AbstractCharacter to);


		public void Anim(int animId) {
			this.Anim(animId, 1, false, false, 0x01);
		}
		public void Anim(int animId, byte frameDelay) {
			this.Anim(animId, 1, false, false, frameDelay);
		}

		public void Anim(int animId, bool backwards) {
			this.Anim(animId, 1, backwards, false, 0x01);
		}

		public void Anim(int animId, bool backwards, bool undo) {
			this.Anim(animId, 1, backwards, undo, 0x01);
		}

		public void Anim(int animId, bool backwards, byte frameDelay) {
			this.Anim(animId, 1, backwards, false, frameDelay);
		}

		public void Anim(int animId, bool backwards, bool undo, byte frameDelay) {
			this.Anim(animId, 1, backwards, undo, frameDelay);
		}

		public void Anim(int animId, int numAnims, bool backwards, bool undo, byte frameDelay) {
			CharacterAnimationOutPacket p = Pool<CharacterAnimationOutPacket>.Acquire();
			p.Prepare(this, animId, numAnims, backwards, undo, frameDelay);
			GameServer.SendToClientsWhoCanSee(this, p);
		}

		public abstract HighlightColor GetHighlightColorFor(AbstractCharacter viewer);

		public virtual ICollection<AbstractCharacter> PartyMembers {
			get {
				return EmptyReadOnlyGenericCollection<AbstractCharacter>.instance;
			}
		}

		public void CancelTarget() {
			GameState state = this.GameState;
			if (state != null) {
				PreparedPacketGroups.SendCancelTargettingCursor(state.Conn);
			}
		}

		public sealed override void InvalidateAosToolTips() {
			CharSyncQueue.PropertiesChanged(this);
			base.InvalidateAosToolTips();
		}

		#region Equipped stuff
		public int VisibleCount {
			get
			{
				if (this.visibleLayers == null) {
					return 0;
				}
				return this.visibleLayers.count;
			}
		}

		public IEnumerable<Thing> VisibleEquip {
			get
			{
				if (this.visibleLayers == null) {
					return EmptyReadOnlyGenericCollection<Thing>.instance;
				}
				return this.visibleLayers;
			}
		}

		public int InvisibleCount {
			get {
				int count = 0;
				if (this.invisibleLayers != null) {
					count = this.invisibleLayers.count;
				}
				if (this.specialLayer != null) {
					count += this.specialLayer.count;
				}
				if (this.draggingLayer != null) {
					count++;
				}
				return count;
			}
		}


		public virtual bool CanEquipItemsOn(AbstractCharacter target) {
			return true;
		}

		public sealed override IEnumerator<AbstractItem> GetEnumerator() {
			this.ThrowIfDeleted();
			return new EquipsEnumerator(this);
		}
		#endregion Equipped stuff
	}

	[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
	public class EquipsEnumerator : IEnumerator<AbstractItem> {
		private const int STATE_VISIBLES = 0;
		private const int STATE_DRAGGING = 1;
		private const int STATE_SPECIAL = 2;
		private const int STATE_OTHERS = 3;
		private int state;
		AbstractCharacter cont;
		AbstractItem current;
		AbstractItem next;

		public EquipsEnumerator(AbstractCharacter c) {
			this.cont = c;
			//this.state = STATE_VISIBLES;
		}

		public void Reset() {
			this.state = STATE_VISIBLES;
			this.current = null;
			this.next = null;
		}

		public bool MoveNext() {
			switch (this.state) {
				case STATE_VISIBLES:
					if (this.cont.visibleLayers != null) {
						if (this.current == null) {//it just started
							this.current = (AbstractItem) this.cont.visibleLayers.firstThing;
							if (this.current != null) {
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							}
							this.next = null;
						} else
						{
							if (this.next != null) {
								this.current = this.next;
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							}
							this.current = null;//continue on next state
						}
					}
					this.state = STATE_DRAGGING;
					goto case STATE_DRAGGING;
				case STATE_DRAGGING:
					if ((this.next == null) && (this.current == null)) {//first time
						if (this.cont.draggingLayer != null) {
							this.current = this.cont.draggingLayer;
							if (this.current != null) {
								return true;
							}
						}
					} else {
						this.current = null;//continue on next state
					}
					this.state = STATE_SPECIAL;
					goto case STATE_SPECIAL;
				case STATE_SPECIAL:
					if (this.cont.specialLayer != null) {
						if (this.current == null) {//it just started
							this.current = (AbstractItem) this.cont.specialLayer.firstThing;
							if (this.current != null) {
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							}
							this.next = null;
						} else
						{
							if (this.next != null) {
								this.current = this.next;
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							}
							this.current = null;//continue on next state
						}
					}
					this.state = STATE_OTHERS;
					goto case STATE_OTHERS;
				case STATE_OTHERS:
					if (this.cont.invisibleLayers != null) {
						if (this.current == null) {//it just started
							this.current = (AbstractItem) this.cont.invisibleLayers.firstThing;
							if (this.current != null) {
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							}
							this.next = null;
						} else {
							if (this.next != null) {
								this.current = this.next;
								this.next = (AbstractItem) this.current.nextInList;
								return true;
							}
						}
					}
					break;
			}
			return false;
		}

		[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
		public void Dispose() {
		}

		public AbstractItem Current {
			get {
				return this.current;
			}
		}

		object IEnumerator.Current {
			get {
				return this.current;
			}
		}
	}

	public class SpeechArgs : ScriptArgs {

		public AbstractCharacter Speaker {
			get {
				return (AbstractCharacter) this.Argv[0];
			}
		}

		public string Speech {
			get {
				return Convert.ToString(this.Argv[1]);
			}
		}

		public int ClilocSpeech {
			get {
				return Convert.ToInt32(this.Argv[2]);
			}
		}

		public SpeechType Type {
			get {
				return (SpeechType) Convert.ToInt32(this.Argv[3]);
			}
		}

		public int Color {
			get {
				return Convert.ToInt32(this.Argv[4]);
			}
		}

		public ClientFont Font {
			get {
				return (ClientFont) Convert.ToInt32(this.Argv[5]);
			}
		}

		public string Lang {
			get {
				return Convert.ToString(this.Argv[6]);
			}
		}

		public int[] ClilocKeywords {
			get {
				return (int[]) this.Argv[7];
			}
		}

		public string[] ClilocArgs {
			get {
				return (string[]) this.Argv[8];
			}
		}

		public AbstractCharacter ExclusiveListener {
			get {
				return (AbstractCharacter) this.Argv[9];
			}
			set {
				this.Argv[9] = value;
			}
		}

		public SpeechArgs(AbstractCharacter speaker, string speech, int clilocSpeech,
				SpeechType type, int color, ClientFont font, string lang, int[] clilocKeywords, string[] clilocArgs) :
			base(speaker, speech, clilocSpeech, type, color, font, lang, clilocKeywords, clilocArgs, null) {
		}
	}
}
