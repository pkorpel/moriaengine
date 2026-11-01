using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - locking things to a house (f_lock, t_lockeditem). A locked thing can't be moved or decay, using it wants
	/// a rank, or being the person it's locked to. A door becomes t_door_locked and opens for friends up.</summary>
	public partial class House {
		private static readonly TagKey lockedItemsTK = TagKey.Acquire("_house_locked_items_");

		public List<Item> LockedItems {
			get {
				List<Item> list = this.GetTag(lockedItemsTK) as List<Item>;
				if (list == null) {
					list = new List<Item>();
					this.SetTag(lockedItemsTK, list);
				}
				list.RemoveAll(item => item.IsDeleted);
				return list;
			}
		}

		/// <summary>The things not to be locked: ingots, logs, clothes, figurines, chairs, ore, rocks, runes.</summary>
		public static bool IsLockable(Item item) {
			switch ((item.Type == null) ? null : item.Type.Defname.ToLowerInvariant()) {
				case "t_ingot":
				case "t_log":
				case "t_clothing":
				case "t_figurine":
				case "t_chair":
				case "t_ore":
				case "t_arock":
				case "t_rune":
					return false;
			}
			return true;
		}

		/// <summary>On the ground in the region; its own sign may stand outside, on the doorstep.</summary>
		internal bool HoldsForLocking(Item item) {
			if (!item.IsOnGround) {
				return false;
			}
			return t_housemenu.IsSignOf(item, this) || (item.GetMap().GetRegionFor(item) == this.Area);
		}

		public void Lock(Item item, HouseRank level, Character person) {
			item.LockTo(this, level, person);
			this.LockedItems.Remove(item);
			this.LockedItems.Add(item);
			if (this.Blocked) {
				item.BlockLocked();
			}
		}

		public void Unlock(Item item) {
			this.LockedItems.Remove(item);
			item.Unlock();
		}

		private void UnlockAll() {
			foreach (Item item in new List<Item>(this.LockedItems)) {
				this.Unlock(item);
			}
		}

		private void SetLockedBlocked(bool blocked) {
			foreach (Item item in this.LockedItems) {
				if (blocked) {
					item.BlockLocked();
				} else {
					item.UnblockLocked();
				}
			}
		}
	}

	public partial class Item {
		private const int BlockedColor = 1;

		private static readonly TagKey lockedToTK = TagKey.Acquire("_house_locked_to_");
		private static readonly TagKey lockLevelTK = TagKey.Acquire("_house_lock_level_");
		private static readonly TagKey lockPersonTK = TagKey.Acquire("_house_lock_person_");
		private static readonly TagKey lockStoredAttrTK = TagKey.Acquire("_house_lock_attr_");
		private static readonly TagKey lockStoredColorTK = TagKey.Acquire("_house_lock_color_");

		public House LockedTo {
			get {
				House house = this.GetTag(lockedToTK) as House;
				return ((house == null) || house.IsDeleted) ? null : house;
			}
		}

		public HouseRank LockLevel {
			get {
				object level = this.GetTag(lockLevelTK);
				return (level == null) ? HouseRank.Public : (HouseRank) (int) level;
			}
		}

		public Character LockPerson {
			get {
				return this.GetTag(lockPersonTK) as Character;
			}
		}

		internal void LockTo(House house, HouseRank level, Character person) {
			this.SetTag(lockedToTK, house);
			this.SetTag(lockLevelTK, (int) level);
			if (person != null) {
				this.SetTag(lockPersonTK, person);
			}
			this.SetTag(lockStoredAttrTK, (int) this.Attributes);
			this.SetAttribute(ItemAttributes.attr_move_always, false);
			this.SetAttribute(ItemAttributes.attr_move_never, true);
			this.SetAttribute(ItemAttributes.attr_decay, false);
			if (this.TypeName == "t_door") {
				this.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door_locked");
			}
		}

		internal void Unlock() {
			object attributes = this.GetTag(lockStoredAttrTK);
			this.RemoveTag(lockedToTK);
			this.RemoveTag(lockLevelTK);
			this.RemoveTag(lockPersonTK);
			this.RemoveTag(lockStoredAttrTK);
			this.UnblockLocked();
			if (attributes != null) {
				ItemAttributes stored = (ItemAttributes) (int) attributes;
				foreach (ItemAttributes flag in new[] { ItemAttributes.attr_move_always, ItemAttributes.attr_move_never, ItemAttributes.attr_decay }) {
					this.SetAttribute(flag, (stored & flag) != 0);
				}
			}
			if (this.TypeName == "t_door_locked") {
				this.Type = (TriggerGroup) TriggerGroup.GetByDefname("t_door");
			}
		}

		internal void BlockLocked() {
			if (this.GetTag(lockStoredColorTK) == null) {
				this.SetTag(lockStoredColorTK, (int) this.Color);
				this.Color = BlockedColor;
			}
		}

		internal void UnblockLocked() {
			object color = this.GetTag(lockStoredColorTK);
			if (color != null) {
				this.Color = (int) color;
				this.RemoveTag(lockStoredColorTK);
			}
		}

		/// <summary>The rank a locked thing wants: a personal one, the rank of its person and one more.</summary>
		private HouseRank LockRankFor(House house) {
			Character person = this.LockPerson;
			return (person == null) ? this.LockLevel : (HouseRank) (((int) house.RankOf(person)) + 1);
		}

		/// <summary>Whether it's locked against this character: f_lockeditem, a locked door having its own key rule.
		/// A blocked house stops even the person it's locked to.</summary>
		public bool IsLockedAgainst(Character ch) {
			House house = this.LockedTo;
			if (house == null) {
				return false;
			}
			HouseRank rank = house.RankOf(ch);
			if (rank == HouseRank.Blocked) {
				return true;
			}
			return (ch != this.LockPerson) && (rank < this.LockRankFor(house));
		}

		/// <summary>In the targeting: may this rank touch the lock on it at all.</summary>
		internal bool CanChangeLock(House house, Character ch, HouseRank rank) {
			return (ch == this.LockPerson) || (rank >= this.LockRankFor(house));
		}

		public override void On_DenyDClick(DenyClickArgs args) {
			Character ch = args.ClickingChar as Character;
			if ((ch != null) && !this.IsDoor && this.IsLockedAgainst(ch)) {
				args.Result = new DenyResult_StringSysMessage("Nemas na to pravo.");
				return;
			}
			base.On_DenyDClick(args);
		}
	}

	/// <summary>i_target_house with more1 12: the second choice of the target is taking a lock off.</summary>
	public class Targ_HouseLock : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			House house = (House) parameter;
			HouseRank rank = house.RankOf(self);
			bool isSign = targetted.Type is t_housemenu;
			if (isSign ? (rank < t_housemenu.LevelOf(targetted)) : !targetted.CanChangeLock(house, self, rank)) {
				self.RedMessage("Nemas na to pravo.");
			} else if ((targetted.LockedTo == house) && !isSign) {
				house.Unlock(targetted);
				self.SysMessage("Vec odemcena");
			} else if (targetted.LockedTo != null) {
				self.SysMessage("Toto nemuzes zamknout.");
			} else if (!House.IsLockable(targetted) && !self.IsGM) {
				self.SysMessage("Toto nemuzes zamknout.");
			} else if (house.HoldsForLocking(targetted)) {
				D_HouseLockLevel.Show(self, house, targetted, rank);
			} else {
				self.SysMessage("Vec musi byt v domu nebo v jeho blizkosti.");
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("Nezameruj postavy.");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}
	}

	/// <summary>d_statask: which level. Nobody gets a level above their own rank offered.</summary>
	public class D_HouseLockLevel : CompiledGumpDef {
		private const int Hue = 955, PersonalButton = 6;
		private static readonly string[] Levels = { "Verejna", "Pritel domu", "Spoluvlastnik", "Vlastnik", "GM" };

		public static void Show(Player player, House house, Item item, HouseRank rank) {
			player.Dialog(player, SingletonScript<D_HouseLockLevel>.Instance, new DialogArgs(house, item, rank));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int rank = (int) (HouseRank) args[2];
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, 300, 110 + (rank * 20), 2620);
			this.AddLabel(20, 20, Hue, "Na jakou uroven chces vec zamknout?");
			this.AddButton(20, 50, 2104, 2103, PersonalButton, GumpButtonType.Reply, 0);
			this.AddLabel(40, 45, Hue, "Tvoje osobni");
			for (int level = 0; level <= System.Math.Max(rank, 1) && level < Levels.Length; level++) {
				int y = 70 + (level * 20);
				this.AddButton(20, y, 2104, 2103, level + 1, GumpButtonType.Reply, 0);
				this.AddLabel(40, y - 5, Hue, Levels[level]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			House house = (House) args[0];
			Item item = (Item) args[1];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if ((button == 0) || house.IsDeleted || item.IsDeleted || !house.HoldsForLocking(item)) {
				return;
			}
			bool isSign = item.Type is t_housemenu;
			if (button == PersonalButton) {
				if (isSign) {
					player.SysMessage("Nemuzes zamknout vyvesku domu na osobu.");
				} else {
					house.Lock(item, HouseRank.Public, player);
					player.SysMessage("Vec zamknuta");
				}
			} else if (isSign) {
				t_housemenu.SetLevel(item, (HouseRank) (button - 1));
			} else {
				house.Lock(item, (HouseRank) (button - 1), null);
				player.SysMessage("Vec zamknuta");
			}
		}
	}
}
