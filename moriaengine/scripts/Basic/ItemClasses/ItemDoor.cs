using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - Sphere's doors, by type, as EreborNext's CItem.Use_Door: locking a door is only a change of its
	/// type. A door is its def's model plus an offset, the lowest bit being open.</summary>
	public partial class Item {
		private const int DoorSecondsOpen = 60;
		private const int DoorLockedCliloc = 500821;

		private static readonly TagKey lockUidTK = TagKey.Acquire("_lock_uid_");

		public bool IsDoorOpen {
			get {
				return (this.DoorOffset & DoorOpened) != 0;
			}
		}

		private int DoorOffset {
			get {
				int offset = this.Model - this.TypeDef.Model;
				return ((offset < 0) || (offset > DoorOffsetMax)) ? 0 : offset;
			}
		}

		/// <summary>Sphere's MORE1 of a lock and of the key that fits it. 0 is any key.</summary>
		public int LockUid {
			get {
				object uid = this.GetTag(lockUidTK);
				return (uid == null) ? 0 : (int) uid;
			}
			set {
				if (value == 0) {
					this.RemoveTag(lockUidTK);
				} else {
					this.SetTag(lockUidTK, value);
				}
			}
		}

		/// <summary>A character using a door: a locked one wants a key, and a GM passes after hearing it's locked.</summary>
		public bool TryUseDoor(Character user) {
			if (!this.IsOnGround) {
				return false;
			}
			if ((this.TypeName == "t_door_locked") && !HasKeyFor(user, this)) {
				user.ClilocSysMessage(DoorLockedCliloc);
				if (!user.IsGM) {
					return false;
				}
			}
			this.UseDoor();
			return true;
		}

		internal static bool HasKeyFor(Character user, Item locked) {
			House house = locked.LockedTo;
			if (house != null) { //Erebor's keys of a house, which friends and up get
				return house.RankOf(user) >= HouseRank.Friend;
			}
			return FindKey(user.Backpack, locked.LockUid, 20);
		}

		private static bool FindKey(Container container, int lockUid, int depth) {
			foreach (Item item in container) {
				if ((item.TypeName == "t_key") && ((lockUid == 0) || (item.LockUid == lockUid))) {
					return true;
				}
				Container inner = item as Container;
				if ((inner != null) && (depth > 0) && FindKey(inner, lockUid, depth - 1)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>Swings it, whoever asks. Each offset moves the door to where its open or closed model stands.</summary>
		public void UseDoor() {
			int offset = this.DoorOffset;
			bool closing = (offset & DoorOpened) != 0;
			int dx, dy;
			DoorStep(offset, out dx, out dy);
			this.Sound(DoorSound(this.TypeDef.Model, closing));
			this.Model = this.TypeDef.Model + (closing ? offset - 1 : offset + 1);
			this.P(this.X + dx, this.Y + dy, this.Z);
			if (closing) {
				this.DeleteTimer(DoorCloseTimer.Key);
			} else {
				this.AddTimer(DoorCloseTimer.Key, new DoorCloseTimer()).DueInSeconds = DoorSecondsOpen;
			}
		}

		private static void DoorStep(int offset, out int dx, out int dy) {
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

		private static int DoorSound(int baseModel, bool closing) {
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
			Item door = (Item) cont;
			if (door.IsDoorOpen) {
				door.UseDoor();
			}
		}
	}
}
