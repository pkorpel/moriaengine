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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class Door : Item {
		private static ushort[] baseModels = {
            0xE8,     //secret stone door		
            0x314,    //secret stone door
            0x324,    //secret stone door
            0x334,    //secret wooden door - chestnut
            0x344,    //secret wooden door - spruce
            0x354,    //secret stone door
            0x675,    //iron door
            0x685,    //iron bar door
            0x695,    //rattan door
            0x6a5,    //dark wooden door
            0x6b5,    //wooden door
            0x6c5,    //iron door
            0x6d5,    //light wooden door
            0x6e5,    //dark wooden door with iron
            0x824,    //iron bar gate external
            0x839,    //light wooden short door
            0x84c,    //iron bar gate external
            0x866,    //dark wooden short door
            0x1fed   //barred metal door
            //0x190e    //bar door - irregular doors, only 2 models.
        };

		private ushort baseModel;

		public override void On_DClick(AbstractCharacter user) {
			if (!this.IsOpen) {
				this.Trigger_Open((Character) user);
			} else {
				this.Trigger_Close((Character) user);
			}
		}

		public static readonly TriggerKey tkOpen = TriggerKey.Acquire("open");
		public static readonly TriggerKey tkClose = TriggerKey.Acquire("close");
		public static readonly TriggerKey tkDenyOpen = TriggerKey.Acquire("denyOpen");
		public static readonly TriggerKey tkDenyOpenDoor = TriggerKey.Acquire("denyOpenDoor");
		public static readonly TriggerKey tkDenyClose = TriggerKey.Acquire("denyClose");
		public static readonly TriggerKey tkDenyCloseDoor = TriggerKey.Acquire("denyCloseDoor");

		private void Trigger_Open(Character user) {
			DenySwitchDoorArgs args = new DenySwitchDoorArgs(user, this);

			var triggerResult = user.TryCancellableTrigger(tkDenyOpenDoor, args);
			if (triggerResult != TriggerResult.Cancel) {
				try {
					triggerResult = user.On_DenyOpenDoor(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (triggerResult != TriggerResult.Cancel) {
					triggerResult = this.TryCancellableTrigger(tkDenyOpen, args);
					if (triggerResult != TriggerResult.Cancel) {
						try {
							this.On_DenyOpen(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}

			DenyResult denyResult = args.Result;

			if (denyResult.Allow) {
				this.SetOpen();
			} else {
				denyResult.SendDenyMessage(user);
			}
		}

		private void Trigger_Close(Character user) {
			DenySwitchDoorArgs args = new DenySwitchDoorArgs(user, this);

			var triggerResult = user.TryCancellableTrigger(tkDenyCloseDoor, args);
			if (triggerResult != TriggerResult.Cancel) {
				try {
					triggerResult = user.On_DenyCloseDoor(args);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (triggerResult != TriggerResult.Cancel) {
					triggerResult = this.TryCancellableTrigger(tkDenyClose, args);
					if (triggerResult != TriggerResult.Cancel) {
						try {
							this.On_DenyClose(args);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}

			DenyResult denyResult = args.Result;

			if (denyResult.Allow) {
				this.SetClose();
			} else {
				denyResult.SendDenyMessage(user);
			}
		}

		public virtual void On_DenyOpen(DenySwitchDoorArgs args) {
		}

		public virtual void On_DenyClose(DenySwitchDoorArgs args) {
		}

		public void SetOpen() {
			if (!this.IsOpen) {           //For case of opening opened doors.
				switch (this.RD) {
					case 0:
						this.X--;
						this.Y++;
						break;
					case 2:
						this.X++;
						this.Y++;
						break;
					case 4:
						this.X--;
						break;
					case 6:
						this.X++;
						this.Y--;
						break;
					case 8:
						this.X++;
						this.Y++;
						break;
					case 10:
						this.X++;
						this.Y--;
						break;
					case 12:
						break;
					case 14:
						this.Y--;
						break;
				}
				this.Model++;
				//this.baseModel++;

				try {
					this.On_Open();
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.TryTrigger(tkOpen, null);
			}
		}

		public virtual void On_Open() {
		}

		public void SetClose() {
			if (this.IsOpen) {            //For case of closing closed doors.
				switch (this.RD) {
					case 1:
						this.X++;
						this.Y--;
						break;
					case 3:
						this.X--;
						this.Y--;
						break;
					case 5:
						this.X++;
						break;
					case 7:
						this.X--;
						this.Y++;
						break;
					case 9:
						this.X--;
						this.Y--;
						break;
					case 11:
						this.X--;
						this.Y++;
						break;
					case 13:
						break;
					case 15:
						this.Y++;
						break;
				}
				this.Model--;
				//this.baseModel--;

				try {
					this.On_Close();
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				this.TryTrigger(tkClose, null);
			}
		}

		public virtual void On_Close() {
		}

		/// <summary>Is doors orthogonal(kolmé) on which direction?</summary>
		public Direction DoorDirection {
			get {
				this.SetBaseDoorModelOrThrow();
				switch (this.RD / 4) {
					case 0:
						return Direction.North;
					case 1:
						return Direction.South;
					case 2:
						return Direction.West;
					case 3:
						return Direction.East;
				}
				throw new SEException(this + " is Door with incompatible model.");
			}
		}

		public bool IsOpen {
			get {
				this.SetBaseDoorModelOrThrow();
				if ((this.RD % 2) == 0) {
					return false;
				}
				return true;
			}
		}

		public DoorRotation DoorRotation {
			get {
				this.SetBaseDoorModelOrThrow();
				if (((this.RD / 2) % 2) == 0) {
					return DoorRotation.Left;
				}
				return DoorRotation.Right;
			}
		}

		public ushort BaseDoorModel {
			get {
				this.SetBaseDoorModelOrThrow();
				return this.baseModel;
			}
		}

		private int RD {
			get {
				return this.Model - this.baseModel;
			}
		}

		private void SetBaseDoorModelOrThrow() {
			int model = this.Model;
			switch (model) {
				case 0x190e://bar door.
				case 0x190f:
					this.baseModel = 0x190e;
					return;
			}

			if ((model < this.baseModel) || (model > (this.baseModel + 15))) {
				for (int i = 0, n = baseModels.Length; i < n; i++) {
					ushort bm = baseModels[i];
					if ((model >= bm) && (model <= (bm + 15))) {
						this.baseModel = bm;
						return;
					}
				}
				throw new SEException(this + " is Door with incompatible model.");
			}
		}
	}

	/// <summary>Do doors rotate by left or right side?</summary>
	public enum DoorRotation {
		Left,
		Right
	}

	public class DenySwitchDoorArgs : DenyTriggerArgs {
		public readonly Character user;
		public readonly Door door;

		public DenySwitchDoorArgs(Character user, Door door)
			: base(DenyResultMessages.Allow, user, door) {

			this.user = user;
			this.door = door;
		}
	}

	public class SwitchDoorArgs : ScriptArgs {
		public readonly Character user;
		public readonly Door door;

		public SwitchDoorArgs(Character user, Door door)
			: base(user, door) {

			this.user = user;
			this.door = door;
		}
	}
}