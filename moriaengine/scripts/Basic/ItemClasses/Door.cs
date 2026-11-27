using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1558 - the def of a door: the converter makes every t_door, t_door_open and t_door_locked def one, and the
	/// dupes of their models too.</summary>
	[ViewableClass]
	[ThingDefTemplate]
	public partial class DoorDef : ItemDef {
	}

	/// <summary>#941 - Sphere's doors, as EreborNext's CItem.Use_Door: locking a door is only a change of its type. A door is
	/// its def's model plus an offset, the lowest bit being open. #1558 - a class of its own, asked like anything else on a
	/// double click: a locked door wants its key in On_DenyDClick (so a quest's @denyDClick handler that allows and cancels
	/// opens it without one), and swings in On_DClick. The client's OpenDoor macro is a double click too (Character).</summary>
	[ViewableClass]
	[ThingTemplate]
	public partial class Door : Item {
		private const int SecondsOpen = 60;
		internal const int LockedCliloc = 500821;
		private const int OffsetMax = 0xF, Opened = 0x1, RightLeft = 0x2, InOut = 0x4, Align = 0x8;

		public bool IsOpen {
			get {
				return (this.Offset & Opened) != 0;
			}
		}

		public bool IsLocked {
			get {
				return this.TypeName == "t_door_locked";
			}
		}

		/// <summary>What the OpenDoor macro looks for: an open door's type stays t_door, only t_door_open is left out.</summary>
		public bool IsMacroDoor {
			get {
				string type = this.TypeName;
				return (type == "t_door") || (type == "t_door_locked");
			}
		}

		private int Offset {
			get {
				int offset = this.Model - this.TypeDef.Model;
				return ((offset < 0) || (offset > OffsetMax)) ? 0 : offset;
			}
		}

		/// <summary>A locked door wants a key, and a GM passes after hearing it's locked (Sphere's Use_Item for t_door_locked,
		/// whether the door stands open or not).</summary>
		public override void On_DenyDClick(DenyClickArgs args) {
			Character user = args.ClickingChar as Character;
			if ((user != null) && this.IsLocked && this.IsOnGround && !HasKeyFor(user, this)) {
				if (!user.IsGM) {
					args.Result = new DenyResult_ClilocSysMessage(LockedCliloc);
					return;
				}
				user.ClilocSysMessage(LockedCliloc);
			}
			base.On_DenyDClick(args);
		}

		/// <summary>The door asks for a key itself, so a house lock doesn't refuse it the way it refuses other things.</summary>
		protected override bool HouseLockRefusesDClick {
			get {
				return false;
			}
		}

		public override void On_DClick(AbstractCharacter user) {
			if (this.IsOnGround) {
				this.Swing();
			}
		}

		/// <summary>Swings it, whoever asks. Each offset moves the door to where its open or closed model stands.</summary>
		public void Swing() {
			int offset = this.Offset;
			bool closing = (offset & Opened) != 0;
			int dx, dy;
			Step(offset, out dx, out dy);
			this.Sound(SoundOf(this.TypeDef.Model, closing));
			this.Model = this.TypeDef.Model + (closing ? offset - 1 : offset + 1);
			this.P(this.X + dx, this.Y + dy, this.Z);
			if (closing) {
				this.DeleteTimer(DoorCloseTimer.Key);
			} else {
				this.AddTimer(DoorCloseTimer.Key, new DoorCloseTimer()).DueInSeconds = SecondsOpen;
			}
		}

		/// <summary>#938 - Sphere's FLIP on a door turns it by its offset from the base model: ALIGN, HAND and SWING flip one bit
		/// of it, a plain flip takes the next pair.</summary>
		public override void Flip(string command) {
			this.Model = this.TypeDef.Model + NextOffset(this.Offset, command);
		}

		private static int NextOffset(int offset, string command) {
			switch ((command ?? "").ToUpperInvariant()) {
				case "ALIGN":
					return offset ^ Align;
				case "HAND":
					return offset ^ RightLeft;
				case "SWING":
					return offset ^ InOut;
				default:
					return ((offset & ~Opened) + 2) % 16;
			}
		}

		/// <summary>A house lock makes a door t_door_locked (friends and up open it), as Sphere's locked multi doors.</summary>
		internal override void LockTo(House house, HouseRank level, Character person) {
			base.LockTo(house, level, person);
			if (this.TypeName == "t_door") {
				this.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door_locked");
			}
		}

		internal override void Unlock() {
			base.Unlock();
			if (this.TypeName == "t_door_locked") {
				this.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door");
			}
		}

		/// <summary>A house or rented place given up: its doors stay, open to anyone.</summary>
		internal void TakeOffLock() {
			this.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door");
			this.LockUid = 0;
		}

		private static void Step(int offset, out int dx, out int dy) {
			dx = 0;
			dy = 0;
			switch (offset) {
				case 0:
					dx = -1;
					dy = 1;
					break;
				case 1:
					dx = 1;
					dy = -1;
					break;
				case 2:
					dx = 1;
					dy = 1;
					break;
				case 3:
					dx = -1;
					dy = -1;
					break;
				case 4:
					dx = -1;
					break;
				case 5:
					dx = 1;
					break;
				case 6:
					dx = 1;
					dy = -1;
					break;
				case 7:
					dx = -1;
					dy = 1;
					break;
				case 8:
					dx = 1;
					dy = 1;
					break;
				case 9:
					dx = -1;
					dy = -1;
					break;
				case 10:
					dx = 1;
					dy = -1;
					break;
				case 11:
					dx = -1;
					dy = 1;
					break;
				case 14:
					dy = -1;
					break;
				case 15:
					dy = 1;
					break;
			}
		}

		private static int SoundOf(int baseModel, bool closing) {
			switch (baseModel) {
				case 0xE8:
				case 0x314:
				case 0x324:
				case 0x334:
				case 0x344:
				case 0x354:
					return closing ? 0x2E : 0x2F;
				case 0x675:
				case 0x685:
				case 0x6C5:
				case 0x824:
				case 0x84C:
					return closing ? 0xF3 : 0xEB;
				default:
					return closing ? 0xF1 : 0xEA;
			}
		}
	}

	/// <summary>An open door swings shut by itself.</summary>
	[SaveableClass, DeepCopyableClass]
	public class DoorCloseTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_door_close_");

		[LoadingInitializer, DeepCopyImplementation]
		public DoorCloseTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Door door = cont as Door;
			if ((door != null) && door.IsOpen) {
				door.Swing();
			}
		}
	}
}
