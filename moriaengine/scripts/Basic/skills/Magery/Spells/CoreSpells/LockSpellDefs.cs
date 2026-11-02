using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1001 - Sphere's CItem::SetMagicLock. Skill plays no part, and without the key only a ship's side locks.</summary>
	[ViewableClass]
	public class MagicLockSpellDef : SpellDef {
		private const int CantLockThat = 501666;

		public MagicLockSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			Character caster = spellEffectArgs.Caster;
			LockSpellLoc texts = Loc<LockSpellLoc>.Get(caster.Language);
			if (CraftingSkillDef.IsOfType(target, LockKinds.Locked)) {
				caster.SysMessage(texts.AlreadyLocked);
			} else if (CraftingSkillDef.IsOfType(target, "t_ship_side")) {
				LockKinds.Set(target, "t_ship_side_locked");
				caster.SysMessage(texts.LockedShip);
			} else if (CraftingSkillDef.IsOfType(target, "t_container")) {
				if (Item.HasKeyFor(caster, target)) {
					LockKinds.Set(target, "t_container_locked");
					caster.SysMessage(texts.LockedContainer);
				} else {
					caster.SysMessage(texts.NoContainerKey);
				}
			} else if (CraftingSkillDef.IsOfType(target, "t_door")) {
				if (Item.HasKeyFor(caster, target)) {
					LockKinds.Set(target, "t_door_locked");
					caster.SysMessage(texts.LockedDoor);
				} else {
					caster.SysMessage(texts.NoDoorKey);
				}
			} else if (CraftingSkillDef.IsOfType(target, "t_ship_hold")) {
				if (Item.HasKeyFor(caster, target)) {
					LockKinds.Set(target, "t_ship_hold_lock");
					caster.SysMessage(texts.LockedHold);
				} else {
					caster.SysMessage(texts.NoHoldKey);
				}
			} else {
				caster.ClilocSysMessage(CantLockThat);
			}
		}
	}

	/// <summary>#1001 - Sphere's CItem::Use_LockPick by spell: the power against the lock's Erebor tag. With the shard's
	/// MAGICUNLOCKDOOR=900 a door gives way once in 900 tries. A key in your pack means no spell is needed, and nothing happens.</summary>
	[ViewableClass]
	public class UnlockSpellDef : SpellDef {
		private const int CantUnlockThat = 501666, Variance = 100, MagicUnlockDoor = 900, Permille = 1000;

		public UnlockSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_EffectItem(Item target, SpellEffectArgs spellEffectArgs) {
			Character caster = spellEffectArgs.Caster;
			LockSpellLoc texts = Loc<LockSpellLoc>.Get(caster.Language);
			if (!CraftingSkillDef.IsOfType(target, LockKinds.Locked)) {
				caster.ClilocSysMessage(CantUnlockThat);
				return;
			}
			if ((target.TopObj() is Character holder) && (holder != caster)) {
				caster.SysMessage(texts.NotWhereItIs);
				return;
			}
			if (Item.HasKeyFor(caster, target)) {
				caster.SysMessage(texts.HaveTheKey);
				return;
			}
			bool isDoor = CraftingSkillDef.IsOfType(target, "t_door_locked");
			int chance = MovementStamina.SCurve(spellEffectArgs.SpellPower - LockpickTimer.DifficultyOf(target), Variance);
			if ((isDoor && (Globals.dice.Next(MagicUnlockDoor) != 0)) || (Globals.dice.Next(Permille) > chance)) {
				caster.SysMessage(isDoor ? texts.FailedDoor : texts.FailedContainer);
				return;
			}
			if (CraftingSkillDef.IsOfType(target, "t_container_locked")) {
				LockKinds.Set(target, "t_container");
				caster.SysMessage(texts.UnlockedContainer);
			} else if (isDoor) {
				LockKinds.Set(target, "t_door");
				caster.SysMessage(texts.UnlockedDoor);
			} else if (CraftingSkillDef.IsOfType(target, "t_ship_hold_lock")) {
				LockKinds.Set(target, "t_ship_hold");
				caster.SysMessage(texts.UnlockedHold);
			} else {
				LockKinds.Set(target, "t_ship_side");
				caster.SysMessage(texts.UnlockedShip);
			}
		}
	}

	internal static class LockKinds {
		internal static readonly string[] Locked = { "t_container_locked", "t_door_locked", "t_ship_hold_lock", "t_ship_side_locked" };

		internal static void Set(Item item, string type) {
			item.Type = (TriggerGroup) TriggerGroup.GetByDefname(type);
		}
	}

	internal class LockSpellLoc : CompiledLocStringCollection<LockSpellLoc> {
		public string AlreadyLocked = "It is already locked";
		public string NoContainerKey = "You don't have the key to this container.";
		public string NoDoorKey = "You don't have the key to this door.";
		public string NoHoldKey = "You don't have the key to the hold.";
		public string LockedContainer = "You lock the container.";
		public string LockedDoor = "You lock the door.";
		public string LockedHold = "You lock the hold.";
		public string LockedShip = "You lock the ship.";
		public string NotWhereItIs = "You cannot unlock that where it is!";
		public string HaveTheKey = "You have the key for this";
		public string FailedDoor = "You failed to unlock the door.";
		public string FailedContainer = "You failed to unlock the container.";
		public string UnlockedContainer = "You unlock the container.";
		public string UnlockedDoor = "You unlock the door.";
		public string UnlockedHold = "You unlock the hold.";
		public string UnlockedShip = "You unlock the ship.";
	}
}
