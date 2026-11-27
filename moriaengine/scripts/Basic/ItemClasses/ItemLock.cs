namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - Sphere's locks and keys: a locked door, container or ship part and the key that fits it share a MORE1.
	/// #1558 - split out of the door code, as chests, the lock spells and doors all use it.</summary>
	public partial class Item {
		private static readonly TagKey lockUidTK = TagKey.Acquire("_lock_uid_");

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

		internal static bool HasKeyFor(Character user, Item locked) {
			House house = locked.LockedTo;
			if (house != null) { //Erebor's keys of a house, which friends and up get
				return (house.RankOf(user) >= HouseRank.Friend) || RentedPlaces.GuardOwnerPasses(user, house); //#1017 - t_door_field
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
	}
}
