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
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	//[Flags]
	//public enum CharacterFlags : short {
	//    None = 0, Zero = None,
	//    Disconnected = 0x01,
	//}

	[ViewableClass]
	public partial class Character : AbstractCharacter {
		//removed, reworked to use the Skills togeter with abilities in one distionary
		//Skill[] skills;//this CAN be null, altough it usually isn't

		///// <summary>Dictionary of character's (typically player's) abilities</summary>
		//private Dictionary<AbilityDef, Ability> abilities = null;

		/// <summary>
		/// Dictionary of character's skills and abilities. Key is (ability/skill)def, value is instance of 
		/// Ability/Skill object of the desired entity
		/// </summary>
		private Dictionary<AbstractDef, object> skillsabilities;

		private CharModelInfo charModelInfo;

		#region Flags
		public sealed override byte FlagsToSend {
			get {
				int ret = 0;

				if (this.Flag_GreenHealthBar) {
					ret |= 0x04;
				}
				if (this.IsNotVisible) {
					ret |= 0x80;
				}
				if (this.Flag_WarMode) {
					ret |= 0x41; //both 0x40 (for aos clients) and 0x01 (for older clients?) hope it won't break much
				}

				//if Female
				//	|= 0x02;

				//if Blessed || YellowHealthbar
				//	|= 0x08;


				return (byte) ret;
			}
		}

		public bool Flag_Dead {
			get {
				return this.ProtectedFlag1;
			}
			private set {
				this.ProtectedFlag1 = value;
			}
		}

		public override bool Flag_Insubst {
			get {
				return this.ProtectedFlag2;
			}
			set {
				if (this.ProtectedFlag2 != value) {
					CharSyncQueue.AboutToChangeVisibility(this);
					this.ProtectedFlag2 = value;
				}
			}
		}

		public bool Flag_InvisByMagic {
			get {
				return this.ProtectedFlag3;
			}
			set {
				if (this.ProtectedFlag3 != value) {
					CharSyncQueue.AboutToChangeVisibility(this);
					this.ProtectedFlag3 = value;
					this.Trigger_VisibilityChange();
				}
			}
		}

		public bool Flag_Hidden {
			get {
				return this.ProtectedFlag4;
			}
			set {
				if (this.ProtectedFlag4 != value) {
					CharSyncQueue.AboutToChangeVisibility(this);
					this.ProtectedFlag4 = value;
					this.Trigger_VisibilityChange();
				}
			}
		}

		public override bool Flag_WarMode {
			get {
				return this.ProtectedFlag5;
			}
			set {
				if (this.ProtectedFlag5 != value) {
					CharSyncQueue.AboutToChangeFlags(this);
					this.ProtectedFlag5 = value;
					this.Trigger_WarModeChange();
				}
			}
		}

		private static TriggerKey warModeChangeTK = TriggerKey.Acquire("warModeChange");
		public void Trigger_WarModeChange() {
			this.TryTrigger(warModeChangeTK, null);
			try {
				this.On_WarModeChange();
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		public bool Flag_GreenHealthBar {
			get {
				return this.ProtectedFlag7;
			}
			set {
				if (this.ProtectedFlag7 != value) {
					CharSyncQueue.AboutToChangeFlags(this);
					this.ProtectedFlag7 = value;
				}
			}
		}

		//use these counter methods to properly set the greenhealthbar flag from multiple sources

		private static TagKey poisonCounterTK = TagKey.Acquire("_poison_counter_");
		public void AddPoisonCounter() {
			this.SetTag(poisonCounterTK, Convert.ToInt32(this.GetTag(poisonCounterTK)) + 1);
			this.Flag_GreenHealthBar = true;
		}

		public void RemovePoisonCounter() {
			int val = Convert.ToInt32(this.GetTag(poisonCounterTK)) - 1;
			if (val <= 0) {
				this.Flag_GreenHealthBar = false;
				this.RemoveTag(poisonCounterTK);
			} else {
				this.SetTag(poisonCounterTK, val);
			}
		}

		#endregion Flags

		#region Mount/Rider stuff
		//Flag_Riding = true, mountorrider must not be null -> I'm riding
		//Flag_Riding = false, mountorrider != null -> someone's on my back (and I'm insubst)
		//Flag_Riding = false, mountorrider == null -> I'm neither riding nor being ridden

		//Flag_Riding is readonly and has a setter method,s because scripts should not be able to set it,
		//(they should set Mount instead) but it needs to be settable from within Character itself.
		public override bool Flag_Riding {
			get {
				return this.ProtectedFlag6;
			}
		}

		private void SetFlag_Riding(bool value) {
			this.ProtectedFlag6 = value;
		}

		public virtual bool IsMountable {
			get {
				return this.MountItem > 0;
			}
		}

		public virtual bool IsMountableBy(Character chr) {
			return !this.IsPlayer && //players are never mountable
				this.IsMountable &&
				chr.CanReach(this).Allow &&
				this.IsPetOf(chr);
		}

		/// <summary>The character riding this one, or null if this character doesn't have a rider.</summary>
		public AbstractCharacter Rider {
			get {
				if (!this.Flag_Riding) { //otherwise I'm the rider
					return this.mountorrider;
				}
				return null;
			}
		}

		/// <summary>The mount this character is riding, or null if this character isn't riding a mount.</summary>
		public sealed override AbstractCharacter Mount {
			get {
				if (this.Flag_Riding) {
					return this.mountorrider;
				}
				return null;
			}
			set {
				if ((this.mountorrider != null) && (!this.Flag_Riding)) {
					throw new SEException("You can't set Mount of something that's being ridden!");
				}
				if (value == null || value.IsDeleted) {
					//automatically call Dismount if 'mount=null;' is done.
					this.Dismount();
				} else {

					Character newMount = (Character) value;
					if ((newMount.mountorrider != null) && (!newMount.Flag_Riding)) {
						throw new SEException("You can't set Mount to something that's being ridden!");
					}
					if (newMount.IsPlayer) {
						throw new SEException("Players can't be mounted!");
					}

					if (this.mountorrider != newMount) {
						if (this.mountorrider != null) {
							this.Dismount();
						} else {
							CharSyncQueue.AboutToChangeMount(this);
						}

						newMount.Dismount();

						this.mountorrider = newMount;
						this.SetFlag_Riding(true);
						newMount.mountorrider = this;
						this.mountorrider.Disconnect();
					} //else nothing changes
				}
			}
		}

		public void Dismount() {
			if (this.mountorrider != null) {
				if (this.Flag_Riding) {
					CharSyncQueue.AboutToChangeMount(this);

					//move it to where we are
					this.mountorrider.P(this);
					this.mountorrider.Direction = this.Direction;

					Sanity.IfTrueThrow(this.mountorrider.mountorrider != this, "this.mountorrider.mountorrider != this");
					Sanity.IfTrueThrow(this.mountorrider.Flag_Riding, "this.mountorrider.Flag_Riding on Dismount()");

					//set it's rider to null
					this.mountorrider.mountorrider = null;
					this.mountorrider.Reconnect();

					this.SetFlag_Riding(false);
					this.mountorrider = null;
				} else {
					throw new SEException("You can't call Dismount() on a char that's being ridden. Call it on it's Rider.");
				}
			}

			Sanity.IfTrueThrow(this.Flag_Riding, "Flag_Riding and this.mountorrider == null");
		}

		public override void On_AfterLoad() {
			//try to fix mounts
			if (this.mountorrider != null) {
				if (this.mountorrider.mountorrider == this) {
					if (this.Flag_Riding == this.mountorrider.Flag_Riding) { //both riders or both mounts = wrong
						if (this.IsPlayer) { //I am the rider
							if (!this.mountorrider.IsPlayer) {
								this.SetFlag_Riding(true);
								this.mountorrider.SetFlag_Riding(false);
								this.mountorrider.Disconnect();
							} else { //mount would be a player. Not gonna happen
								this.ForgetMountValues();
							}
						} else if (!this.mountorrider.IsPlayer) { //both are NPCs
							//we use flag-disconnected as clue
							if (this.Flag_Disconnected != this.mountorrider.Flag_Disconnected) {
								Logger.WriteWarning("Fixed mount states of 2 NPCs ('" + this + "' and '" + this.mountorrider + "') according to their Flag_Disconnected");
								this.SetFlag_Riding(!this.Flag_Disconnected);
								this.mountorrider.SetFlag_Riding(!this.mountorrider.Flag_Disconnected);
							} else { //disconnected state the same, i.e. useless for us
								//this is an error
								this.ForgetMountValues(); //the other side should get fixed in their own instance of this method
							}
						}
					}
				} else { //other side is not pointing to us. They might be in fact not broken so we leave them alone.
					this.ForgetMountValues();
				}
			} else if (this.Flag_Riding) {
				Logger.WriteWarning("Fixed mistakenly positive Flag_Riding of '" + this + "'.");
				this.SetFlag_Riding(false);
			}

			base.On_AfterLoad();
		}

		private void ForgetMountValues() {
			Logger.WriteError("Unfixable mount persistence error of '" + this + "'. Set as dismounted and reconnected.");
			this.SetFlag_Riding(false);
			this.mountorrider = null;
			if (!this.IsPlayer) {
				this.Reconnect();
			}
		}

		public override void On_Destroy() {
			if (this.mountorrider != null) {
				if (this.Flag_Riding) {//I am the rider, riding a NPC - delete it
					this.mountorrider.Delete();
					this.SetFlag_Riding(false);
					this.mountorrider = null;
				} else {//I am the mount
					this.mountorrider.Dismount();
				}
			}
			base.On_Destroy();
		}
		#endregion Mount/Rider stuff

		#region Pets / ownership
		public bool IsOwnerOf(Character cre) {
			return cre.IsPetOf(this);
		}

		public bool IsPetOf(Character cre) {
			//TODO
			return cre.IsGM;
		}

		public override bool CanEquipItemsOn(AbstractCharacter chr) {
			if (chr == this) {
				return true;
			}
			Character target = (Character) chr;
			return (this.IsPetOf(target)) && (this.CanReach(target).Allow);
		}

		//public override bool CanEquip(AbstractItem i) {
		//    return true;
		//}

		public override bool CanRename(AbstractCharacter beingRenamed) {
			return (this.IsGM) ||
				((this.IsPlayer) && (beingRenamed != this) && this.IsOwnerOf((Character) beingRenamed)); //can't rename self
		}
		#endregion Pets / ownership

		#region Visibility

		private static TriggerKey visibilityChangeTK = TriggerKey.Acquire("visibilityChange");
		public void Trigger_VisibilityChange() {
			this.TryTrigger(visibilityChangeTK, null);
			this.On_VisibilityChange();
		}

		public virtual void On_VisibilityChange() {
		}

		public sealed override bool IsNotVisible {
			get {
				return (this.Flag_InvisByMagic || this.Flag_Hidden || this.Flag_Insubst || this.Flag_Disconnected);
			}
		}

		public override bool CanSeeVisibility(Thing target) {
			Item targetAsItem = target as Item;
			if (targetAsItem != null) {
				return this.CanSeeVisibility(targetAsItem);
			}
			Character targetAsChar = target as Character;
			if (targetAsChar != null) {
				return this.CanSeeVisibility(targetAsChar);
			}
			return false;//it was null
		}

		public bool CanSeeVisibility(Character target) {
			if (target == null) {
				return false;
			}
			if (target.IsDeleted) {
				return false;
			}
			//character invisibility has 4 possible reasons: Flag_InvisByMagic || Flag_Hidden || Flag_Insubst || Flag_Disconnected
			if (target.Flag_Disconnected) {
				//TODO: "allshow" mode for GMs
				return false;
			}
			if (target.Flag_Insubst) {//ghosts, insubst GMs
				if (this.IsGM) {
					return this.Plevel > target.Plevel; //can see other GMs only if they have lower plevel
				}
				return false;
			}
			if (target.Flag_InvisByMagic) {
				return this.IsGM;
			}
			if (target.Flag_Hidden) {
				if (this.IsGM) {
					return true;
				}
				HiddenHelperPlugin ssp = target.GetPlugin(HidingSkillDef.pluginKey) as HiddenHelperPlugin;
				return ((ssp != null) &&
				        (ssp.hadDetectedMe != null) &&
				        (ssp.hadDetectedMe.Contains(this)));
			}
			return true;
		}

		public bool CanSeeVisibility(Item target) {
			if (target == null) {
				return false;
			}
			if (target.IsDeleted) {
				return false;
			}
			if (target.IsNotVisible) {
				if (!target.Flag_Disconnected) {
					return this.IsGM;
				}
				return false;
			}
			return true;
		}
		#endregion Visibility

		#region CanSee... / Check...
		public bool CanSeeLOSMessage(IPoint3D target) {
			DenyResult result = this.CanSeeLOS(target);
			if (result.Allow) {
				return true;
			}
			result.SendDenyMessage(this);
			return false;
		}

		public bool IsAliveAndValid {
			get {
				return this.CheckAlive().Allow;
			}
		}

		public DenyResult CheckAlive() {
			if ((this.Flag_Disconnected) || (this.IsDeleted)) {
				return DenyResultMessages.Deny_NoMessage;
			}
			if (this.Flag_Dead) {
				return DenyResultMessages_Character.Deny_IAmDeadAndCannotDoThat;
			}
			if (this.Flag_Insubst && !this.IsGM) {
				return DenyResultMessages_Character.Deny_YoureAGhostAndCantDoThat;
			}
			return DenyResultMessages.Allow;
		}

		public bool CheckAliveWithMessage() {
			DenyResult result = this.CheckAlive();
			if (result.Allow) {
				return true;
			}
			result.SendDenyMessage(this);
			return false;
		}

		public virtual void On_WarModeChange() {
		}

		public DenyResult CanInteractWith(IPoint3D target) {
			if (!this.IsGM) {
				DenyResult result = this.CheckAlive();
				if (!result.Allow) {
					return result;
				}

				Character targetAsChar = target as Character;
				if (targetAsChar != null) {
					if (targetAsChar.Flag_Dead) {
						return DenyResultMessages_Character.Deny_TargetIsDead;
					}
					if (targetAsChar.Flag_Insubst) {
						return DenyResultMessages_Character.Deny_TargetIsInsubst;
					}
				}

				result = this.CanSeeLOS(target);
				if (!result.Allow) {
					return result;
				}
			}

			return DenyResultMessages.Allow;
		}

		public bool CanInteractWithMessage(IPoint3D target) {
			DenyResult result = this.CanInteractWith(target);
			if (result.Allow) {
				return true;
			}
			result.SendDenyMessage(this);
			return false;
		}

		public bool CanPickUpWithMessage(Item target) {
			DenyResult result = this.CanPickup(target);
			if (result.Allow) {
				return true;
			}
			result.SendDenyMessage(this);
			return false;
		}

		public bool CanReachWithMessage(Thing target) {
			DenyResult result = this.CanReach(target);
			if (result.Allow) {
				return true;
			}
			result.SendDenyMessage(this);
			return false;
		}

		public override DenyResult CanPutItemsInContainer(AbstractItem targetContainer) {
			if (this.IsGM) {
				return DenyResultMessages.Allow;
			}

			if (this.Flag_Disconnected) {
				return DenyResultMessages.Deny_NoMessage;
			}

			//TODO zamykani kontejneru

			DenyResult result = DenyResultMessages.Allow;

			Thing c = targetContainer.Cont;
			if (c != null) {
				Item contAsItem = c as Item;
				if (contAsItem != null) {
					return OpenedContainers.HasContainerOpen(this, contAsItem);
				}
				if (c != this) {
					result = this.CanReach(c);
					if (result.Allow) {
						Character contAsChar = (Character) c;
						if (!contAsChar.IsPetOf(this)) {//not my pet or myself
							return DenyResultMessages.Deny_ThatDoesNotBelongToYou;
						}
					}
				}
			} else {
				return this.CanReachCoordinates(targetContainer);
			}

			//equipped in visible layer?
			if (targetContainer.Z > (int) LayerNames.Mount) {
				return DenyResultMessages.Deny_ThatIsInvisible;
			}

			return result;
		}


		#endregion CanSee... / Check...

		#region StatLock
		public sealed override byte StatLockByte {
			get {
				return this.statLockByte;
			}
		}

		public override StatLockType StrLock {
			get {
				return (StatLockType) ((this.statLockByte >> 4) & 0x3);
			}
			set {
				if (value != this.StrLock) {
					this.statLockByte = (byte) ((this.statLockByte & 0xCF) + ((((byte) value) & 0x3) << 4));
					PacketSequences.SendStatLocks(this);
				}
			}
		}

		public override StatLockType DexLock {
			get {
				return (StatLockType) ((this.statLockByte >> 2) & 0x3);
			}
			set {
				if (value != this.DexLock) {
					this.statLockByte = (byte) ((this.statLockByte & 0xF3) + ((((byte) value) & 0x3) << 2));
					PacketSequences.SendStatLocks(this);
				}
			}
		}

		public override StatLockType IntLock {
			get {
				return (StatLockType) ((this.statLockByte) & 0x3);
			}
			set {
				if (value != this.IntLock) {
					this.statLockByte = (byte) ((this.statLockByte & 0xFC) + ((((byte) value) & 0x3)));
					PacketSequences.SendStatLocks(this);
				}
			}
		}
		#endregion StatLock

		#region status properties
		public override short Hits {
			get {
				return this.hitpoints;
			}
			set {
				if (value != this.hitpoints) {
					if (!this.Flag_Dead && value < 1) {
						this.CauseDeath((Character) Globals.SrcCharacter);
					} else {
						CharSyncQueue.AboutToChangeHitpoints(this);
						this.hitpoints = value;

						//try the hitpoints regeneration
						RegenerationPlugin.TryInstallPlugin(this, this.hitpoints, this.MaxHits, this.hitsRegenSpeed);
					}
				}
			}
		}

		public override short MaxHits {
			get {
				throw new SEException("The method or operation is not implemented.");
			}
			set {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		public override short Mana {
			get {
				return this.mana;
			}
			set {
				if (value != this.mana) {
					CharSyncQueue.AboutToChangeMana(this);
					this.mana = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.mana, this.MaxMana, this.manaRegenSpeed);

					//meditation finish
					if (this.mana >= this.MaxMana) {
						this.DeletePlugin(MeditationPlugin.meditationPluginKey);
					}
				}
			}
		}


		public override short MaxStam {
			get {
				throw new SEException("The method or operation is not implemented.");
			}
			set {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		public override short Stam {
			get {
				return this.stamina;
			}
			set {
				if (value != this.stamina) {
					CharSyncQueue.AboutToChangeStamina(this);
					this.stamina = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.stamina, this.MaxStam, this.stamRegenSpeed);
				}
			}
		}

		public override short MaxMana {
			get {
				throw new SEException("The method or operation is not implemented.");
			}
			set {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		public override short Str {
			get {
				return this.strength;
			}
			set {
				if (value != this.strength) {
					CharSyncQueue.AboutToChangeStats(this);
					this.InvalidateCombatWeaponValues();
					this.strength = value;
				}
			}
		}

		public override short Dex {
			get {
				return this.dexterity;
			}
			set {
				if (value != this.dexterity) {
					CharSyncQueue.AboutToChangeStats(this);
					this.InvalidateCombatWeaponValues();
					this.dexterity = value;
				}
			}
		}

		public override short Int {
			get {
				return this.intelligence;
			}
			set {
				if (value != this.intelligence) {
					CharSyncQueue.AboutToChangeStats(this);
					this.InvalidateCombatWeaponValues();
					this.intelligence = value;
				}
			}
		}

		public override short TithingPoints {
			get {
				return this.tithingPoints;
			}
			set {
				if (value != this.tithingPoints) {
					CharSyncQueue.AboutToChangeStats(this);
					this.tithingPoints = value;
				}
			}
		}

		//public override short ExtendedStatusNum01 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum02 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum03 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum04 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum05 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum06 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override byte ExtendedStatusNum07 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override byte ExtendedStatusNum08 {
		//    get {
		//        return 0;
		//    }
		//}

		//public override short ExtendedStatusNum09 {
		//    get {
		//        return 0;
		//    }
		//}

		public override long Gold {
			get {
				return 0;
			}
		}
		#endregion status properties

		#region resisty
		private static TagKey resistMagicTK = TagKey.Acquire("_resist_magic_");
		private static TagKey resistFireTK = TagKey.Acquire("_resist_fire_");
		private static TagKey resistElectricTK = TagKey.Acquire("_resist_electric_");
		private static TagKey resistAcidTK = TagKey.Acquire("_resist_acid_");
		private static TagKey resistColdTK = TagKey.Acquire("_resist_cold_");
		private static TagKey resistPoisonTK = TagKey.Acquire("_resist_poison_");
		private static TagKey resistMysticalTK = TagKey.Acquire("_resist_mystical_");
		private static TagKey resistPhysicalTK = TagKey.Acquire("_resist_physical_");
		private static TagKey resistSlashingTK = TagKey.Acquire("_resist_slashing_");
		private static TagKey resistStabbingTK = TagKey.Acquire("_resist_stabbing_");
		private static TagKey resistBluntTK = TagKey.Acquire("_resist_blunt_");
		private static TagKey resistArcheryTK = TagKey.Acquire("_resist_archery_");
		private static TagKey resistBleedTK = TagKey.Acquire("_resist_bleed_");
		private static TagKey resistSummonTK = TagKey.Acquire("_resist_summon_");
		private static TagKey resistDragonTK = TagKey.Acquire("_resist_dragon_");
		private static TagKey resistParalyseTK = TagKey.Acquire("_resist_paralyse_");

		public int ResistMagic {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistMagicTK));
				return dynamicPart + this.TypeDef.ResistMagic;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistMagic;
				if (dynamicPart != 0) {
					this.SetTag(resistMagicTK, dynamicPart);
				} else {
					this.RemoveTag(resistMagicTK);
				}
			}
		}

		public int ResistFire {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistFireTK));
				return dynamicPart + this.TypeDef.ResistFire;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistFire;
				if (dynamicPart != 0) {
					this.SetTag(resistFireTK, dynamicPart);
				} else {
					this.RemoveTag(resistFireTK);
				}
			}
		}

		public int ResistElectric {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistElectricTK));
				return dynamicPart + this.TypeDef.ResistElectric;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistElectric;
				if (dynamicPart != 0) {
					this.SetTag(resistElectricTK, dynamicPart);
				} else {
					this.RemoveTag(resistElectricTK);
				}
			}
		}

		public int ResistAcid {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistAcidTK));
				return dynamicPart + this.TypeDef.ResistAcid;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistAcid;
				if (dynamicPart != 0) {
					this.SetTag(resistAcidTK, dynamicPart);
				} else {
					this.RemoveTag(resistAcidTK);
				}
			}
		}

		public int ResistCold {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistColdTK));
				return dynamicPart + this.TypeDef.ResistCold;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistCold;
				if (dynamicPart != 0) {
					this.SetTag(resistColdTK, dynamicPart);
				} else {
					this.RemoveTag(resistColdTK);
				}
			}
		}

		public int ResistPoison {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistPoisonTK));
				return dynamicPart + this.TypeDef.ResistPoison;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistPoison;
				if (dynamicPart != 0) {
					this.SetTag(resistPoisonTK, dynamicPart);
				} else {
					this.RemoveTag(resistPoisonTK);
				}
			}
		}

		public int ResistMystical {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistMysticalTK));
				return dynamicPart + this.TypeDef.ResistMystical;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistMystical;
				if (dynamicPart != 0) {
					this.SetTag(resistMysticalTK, dynamicPart);
				} else {
					this.RemoveTag(resistMysticalTK);
				}
			}
		}

		public int ResistPhysical {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistPhysicalTK));
				return dynamicPart + this.TypeDef.ResistPhysical;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistPhysical;
				if (dynamicPart != 0) {
					this.SetTag(resistPhysicalTK, dynamicPart);
				} else {
					this.RemoveTag(resistPhysicalTK);
				}
			}
		}

		public int ResistSlashing {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistSlashingTK));
				return dynamicPart + this.TypeDef.ResistSlashing;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistSlashing;
				if (dynamicPart != 0) {
					this.SetTag(resistSlashingTK, dynamicPart);
				} else {
					this.RemoveTag(resistSlashingTK);
				}
			}
		}

		public int ResistStabbing {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistStabbingTK));
				return dynamicPart + this.TypeDef.ResistStabbing;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistStabbing;
				if (dynamicPart != 0) {
					this.SetTag(resistStabbingTK, dynamicPart);
				} else {
					this.RemoveTag(resistStabbingTK);
				}
			}
		}

		public int ResistBlunt {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistBluntTK));
				return dynamicPart + this.TypeDef.ResistBlunt;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistBlunt;
				if (dynamicPart != 0) {
					this.SetTag(resistBluntTK, dynamicPart);
				} else {
					this.RemoveTag(resistBluntTK);
				}
			}
		}

		public int ResistArchery {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistArcheryTK));
				return dynamicPart + this.TypeDef.ResistArchery;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistArchery;
				if (dynamicPart != 0) {
					this.SetTag(resistArcheryTK, dynamicPart);
				} else {
					this.RemoveTag(resistArcheryTK);
				}
			}
		}

		public int ResistBleed {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistBleedTK));
				return dynamicPart + this.TypeDef.ResistBleed;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistBleed;
				if (dynamicPart != 0) {
					this.SetTag(resistBleedTK, dynamicPart);
				} else {
					this.RemoveTag(resistBleedTK);
				}
			}
		}

		public int ResistSummon {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistSummonTK));
				return dynamicPart + this.TypeDef.ResistSummon;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistSummon;
				if (dynamicPart != 0) {
					this.SetTag(resistSummonTK, dynamicPart);
				} else {
					this.RemoveTag(resistSummonTK);
				}
			}
		}

		public int ResistDragon {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistDragonTK));
				return dynamicPart + this.TypeDef.ResistDragon;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistDragon;
				if (dynamicPart != 0) {
					this.SetTag(resistDragonTK, dynamicPart);
				} else {
					this.RemoveTag(resistDragonTK);
				}
			}
		}

		public int ResistParalyse {
			get {
				int dynamicPart = Convert.ToInt32(this.GetTag(resistParalyseTK));
				return dynamicPart + this.TypeDef.ResistParalyse;
			}
			set {
				int dynamicPart = value - this.TypeDef.ResistParalyse;
				if (dynamicPart != 0) {
					this.SetTag(resistParalyseTK, dynamicPart);
				} else {
					this.RemoveTag(resistParalyseTK);
				}
			}
		}
		#endregion

		#region regenerace
		/// <summary>How many hitpoints is regenerated in one second</summary>
		public double HitsRegenSpeed {
			get {
				return this.hitsRegenSpeed;
			}
			set {
				this.hitsRegenSpeed = value;

				//check the regeneration
				RegenerationPlugin.TryInstallPlugin(this, this.hitpoints, this.MaxHits, this.hitsRegenSpeed);
			}
		}

		/// <summary>How many stamina points is regenerated in one second</summary>
		public double ManaRegenSpeed {
			get {
				return this.manaRegenSpeed;
			}
			set {
				this.manaRegenSpeed = value;

				//check the regeneration
				RegenerationPlugin.TryInstallPlugin(this, this.mana, this.MaxMana, this.manaRegenSpeed);
			}
		}

		/// <summary>How many mana points is regenerated in one second</summary>
		public double StamRegenSpeed {
			get {
				return this.stamRegenSpeed;
			}
			set {
				this.stamRegenSpeed = value;

				//check the regeneration
				RegenerationPlugin.TryInstallPlugin(this, this.stamina, this.MaxStam, this.stamRegenSpeed);
			}
		}
		#endregion regenerace

		#region Death/Resurrection
		public void Kill() {
			//TODO effect?
			this.CauseDeath((Character) Globals.SrcCharacter);
		}

		private static TagKey DeathTimeTK = TagKey.Acquire("_deathTime_");

		public virtual void On_Death(Character killedBy) {
			//stop regenerating
			this.DeletePlugin(RegenerationPlugin.regenerationsPluginKey);
			this.SetTag(DeathTimeTK, Globals.TimeAsSpan);
		}

		private static TriggerKey deathTK = TriggerKey.Acquire("death");

		public void CauseDeath(Character killedBy) {
			if (!this.Flag_Dead) {

				this.Trigger_HostileAction(killedBy);
				this.Trigger_Disrupt();

				this.AbortSkill();
				this.Dismount();
				SoundCalculator.PlayDeathSound(this);

				this.Trigger_Death(killedBy);

				CharSyncQueue.AboutToChangeHitpoints(this);
				this.hitpoints = 0;

				CorpseDef cd = this.TypeDef.CorpseDef;
				Corpse corpse = null;
				if (cd != null) {
					corpse = (Corpse) cd.Create((IPoint4D) this);
					//NetState.ProcessThing(corpse);
				}

				GameState state = this.GameState;
				TcpConnection<GameState> conn = null;
				if (state != null) {
					conn = state.Conn;
					PreparedPacketGroups.SendYouAreDeathMessage(conn);
				}

				PacketGroup pg = null;
				foreach (TcpConnection<GameState> viewerConn in this.GetMap().GetConnectionsWhoCanSee(this)) {
					if (conn != viewerConn) {
						if (pg == null) {
							pg = PacketGroup.AcquireMultiUsePG();
							pg.AcquirePacket<DisplayDeathActionOutPacket>().Prepare(this.FlaggedUid, corpse);
						}
						viewerConn.SendPacketGroup(pg);
					}
				}

				if (corpse != null) {
					corpse.InitFromChar(this);
				}

				if (this.IsPlayer) {
					this.OModel = this.Model;
					this.OColor = this.Color;
					this.Model = 0x192; //make me ghost
					this.Color = 0; //?
					this.Flag_Insubst = true;
					this.Flag_Dead = true;
				} else {
					this.Delete();
				}

				//NetState.ProcessThing(this);

				if (pg != null) {
					pg.Dispose();
				}
			}
		}

		private void Trigger_Death(Character killedBy) {
			this.TryTrigger(deathTK, new ScriptArgs(killedBy));
			try {
				this.On_Death(killedBy);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		/// <summary>Resne. Pokud je mrtva postava v blizkosti tela(max 1 policko), tak to lootne i telo</summary>
		public void Resurrect() {
			Corpse c = null;
			foreach (Thing nearbyThing in this.GetMap().GetThingsInRange(this.X, this.Y, 1)) {
				c = nearbyThing as Corpse;
				if (c != null)
				{
					if (c.Owner == this) {
						break;
					}
					c = null;
				}
			}

			this.Resurrect(c);
		}

		/// <summary>
		/// Resne a pokud je telo tak i lootne telo.
		/// </summary>
		/// <param name="c">Telo jehoz majitel je this. Muze byt null.</param>
		/// <remarks>
		/// Vzajemna pozice mezi telem a mrtve postavy neni kontrolovana
		/// </remarks>
		public void Resurrect(Corpse c) {
			if (this.Flag_Dead) {
				if (c != null && !c.IsDeleted) {
					c.ReturnStuffToChar(this);
				}

				this.Hits = 1;
				this.Model = this.OModel;
				this.ReleaseOModelTag();
				this.Color = this.OColor;
				this.ReleaseOColorTag();
				this.Flag_Insubst = false;
				this.Flag_Dead = false;

				GameState state = this.GameState;
				if (state != null) {
					PreparedPacketGroups.SendResurrectMessage(state.Conn);
				}
			}
		}

		private static TagKey oColorTK = TagKey.Acquire("_ocolor_");
		public int OColor {
			get {
				object o = this.GetTag(oColorTK);
				if (o != null) {
					return Convert.ToInt32(o);
				}
				return this.Color;
			}
			set {
				this.SetTag(oColorTK, value);
			}
		}

		private void ReleaseOColorTag() {
			this.RemoveTag(oColorTK);
		}

		private static TagKey oModelTK = TagKey.Acquire("_omodel_");
		public int OModel {
			get {
				object o = this.GetTag(oModelTK);
				if (o != null) {
					return Convert.ToInt32(o);
				}
				return this.Model;
			}
			set {
				this.SetTag(oModelTK, value);
			}
		}

		private void ReleaseOModelTag() {
			this.RemoveTag(oModelTK);
		}
		#endregion Death/Resurrection

		#region Go() overrides
		public void Go(Region reg) {
			this.P(reg.P);
			this.Fix();
			//Update();
		}

		public void Go(Point2D pnt) {
			this.P(pnt.X, pnt.Y);
			this.Fix();
			//Update();
		}

		public void Go(Point3D pnt) {
			this.P(pnt.X, pnt.Y, pnt.Z);
			this.Fix();
			//Update();
		}

		public void Go(Point4D pnt) {
			this.P(pnt.X, pnt.Y, pnt.Z, pnt.M);
			this.Fix();
			//Update();
		}

		public void Go(IPoint2D pnt) {
			this.P(pnt.X, pnt.Y);
			this.Fix();
			//Update();
		}

		public void Go(IPoint3D pnt) {
			this.P(pnt.X, pnt.Y, pnt.Z);
			this.Fix();
			//Update();
		}

		public void Go(IPoint4D pnt) {
			this.P(pnt.X, pnt.Y, pnt.Z, pnt.M);
			this.Fix();
			//Update();
		}

		public void Go(int x, int y) {
			this.P(x, y);
			this.Fix();
			//Update();
		}

		public void Go(int x, int y, int z) {
			this.P(x, y, z);
			this.Fix();
			//Update();
		}

		public void Go(int x, int y, int z, byte m) {
			this.P(x, y, z, m);
			this.Fix();
			//Update();
		}
		#endregion Go() overrides

		private Dictionary<AbstractDef, object> SkillsAbilities {
			get {
				if (this.skillsabilities == null) {
					this.skillsabilities = new Dictionary<AbstractDef, object>();
				}
				return this.skillsabilities;
			}
		}

		#region Skills
		/// <summary>Enumerator of all character's skills</summary>
		public override IEnumerable<ISkill> Skills {
			get {
				return new SkillsEnumerator(this.skillsabilities);
			}
		}

		/// <summary>Return the appropriate Skill instance by given ID, or null if given skill values are default (value 0 and lock up).</summary>
		public override ISkill GetSkillObject(int id) {
			if (this.skillsabilities != null) {
				AbstractSkillDef def = AbstractSkillDef.GetById(id);
				object retVal = null;
				if (this.skillsabilities.TryGetValue(def, out retVal)) {
					return (ISkill) retVal;
				}
			}
			return null;
		}

		/// <summary>Return the appropriate Skill instance by given def, or null if given skill values are default (value 0 and lock up).</summary>
		public Skill GetSkillObject(AbstractSkillDef def) {
			if (this.skillsabilities != null) {
				object retVal = null;
				if (this.skillsabilities.TryGetValue(def, out retVal)) {
					return (Skill) retVal;
				}
			}
			return null;
		}

		public override void SetRealSkillValue(int id, int value) {
			AbstractSkillDef def = AbstractSkillDef.GetById(id);
			this.AcquireSkillObject(def).RealValue = value;
		}

		public override void SetSkillLockType(int id, SkillLockType type) {
			AbstractSkillDef def = AbstractSkillDef.GetById(id);
			this.AcquireSkillObject(def).Lock = type;
		}

		public void SetRealSkillValue(AbstractSkillDef def, int value) {
			this.AcquireSkillObject(def).RealValue = value;
		}

		/// <summary>Change the 'modified' value of a skill.</summary>
		public void ModifySkillValue(int id, int difference) {
			AbstractSkillDef def = AbstractSkillDef.GetById(id);
			this.AcquireSkillObject(def).ModifyValue(difference);
		}

		/// <summary>Change the 'modified' value of a skill.</summary>
		public void ModifySkillValue(SkillName id, int difference) {
			this.ModifySkillValue((int) id, difference);
		}

		/// <summary>Change the 'modified' value of a skill.</summary>
		public void ModifySkillValue(AbstractSkillDef def, int difference) {
			this.AcquireSkillObject(def).ModifyValue(difference);
		}

		/// <summary>Get modified value of the skill.</summary>
		public override int GetSkill(int id) {
			ISkill skl = this.GetSkillObject(id);
			if (skl != null) {
				return skl.ModifiedValue;
			}
			return 0;
		}

		/// <summary>Get modified value of the skill.</summary>
		public int GetSkill(SkillName id) {
			return this.GetSkill((int) id);
		}

		/// <summary>Get modified value of the skill.</summary>
		public int GetSkill(AbstractSkillDef skillDef) {
			Skill skl = this.GetSkillObject(skillDef);
			if (skl != null) {
				return skl.ModifiedValue;
			}
			return 0;
		}

		public int GetModifiedSkillValue(int id) {
			return this.GetSkill(id);
		}

		public int GetModifiedSkillValue(SkillName id) {
			return this.GetSkill(id);
		}

		public int GetModifiedSkillValue(AbstractSkillDef skillDef) {
			return this.GetSkill(skillDef);
		}

		public int GetRealSkillValue(int id) {
			ISkill skl = this.GetSkillObject(id);
			if (skl != null) {
				return skl.ModifiedValue;
			}
			return 0;
		}

		public int GetRealSkillValue(SkillName id) {
			return this.GetSkill((int) id);
		}

		public int GetRealSkillValue(AbstractSkillDef skillDef) {
			Skill skl = this.GetSkillObject(skillDef);
			if (skl != null) {
				return skl.ModifiedValue;
			}
			return 0;
		}

		/// <summary>Get value of the lock type of skill with given ID, if the skill is not present return default</summary>
		public SkillLockType GetSkillLockType(int id) {
			ISkill skl = this.GetSkillObject(id);
			if (skl != null) {
				return skl.Lock;
			}
			return SkillLockType.Up; //default value
		}

		public SkillLockType GetSkillLockType(SkillName id) {
			return this.GetSkillLockType((int) id);
		}

		public SkillLockType GetSkillLockType(AbstractSkillDef skillDef) {
			Skill skl = this.GetSkillObject(skillDef);
			if (skl != null) {
				return skl.Lock;
			}
			return SkillLockType.Up; //default value
		}

		//instantiate new skill object, if needed
		private Skill AcquireSkillObject(AbstractSkillDef skillDef) {
			Sanity.IfTrueSay(skillDef == null, "skillDef == null");
			Skill skill;
			if (!this.HasSkill(skillDef, out skill)) {
				skill = new Skill(skillDef.Id, this);
				this.SkillsAbilities.Add(skillDef, skill);
			}
			return skill;
		}

		private bool HasSkill(AbstractSkillDef skillDef, out Skill skill) {
			if (this.skillsabilities != null) {
				object o;
				if (this.skillsabilities.TryGetValue(skillDef, out o)) {
					skill = (Skill) o;
					return true;
				}
			}
			skill = null;
			return false;
		}

		internal void InternalRemoveSkill(int id) {
			AbstractSkillDef aDef = AbstractSkillDef.GetById(id);
			this.SkillsAbilities.Remove(aDef);
		}

		/// <summary>Sphere's command for starting a skill</summary>
		public void Skill(int skillId) {
			this.SelectSkill(skillId);
		}

		/// <summary>Start a skill.</summary>
		public void SelectSkill(SkillName skillName) {
			this.SelectSkill((SkillDef) AbstractSkillDef.GetById((int) skillName));
		}

		/// <summary>Start a skill.</summary>
		public void SelectSkill(int skillId) {
			this.SelectSkill((SkillDef) AbstractSkillDef.GetById(skillId));
		}

		/// <summary>
		/// Start a skill. 
		/// Is also called when client does the useskill macro
		/// </summary>
		public override void SelectSkill(AbstractSkillDef skillDef) {
			this.SelectSkill((SkillDef) skillDef);
		}

		/// <summary>Start a skill.</summary>
		public void SelectSkill(SkillDef skillDef) {
			if (skillDef != null) {
				SkillSequenceArgs args = SkillSequenceArgs.Acquire(this, skillDef);
				args.PhaseSelect();
			}
		}

		public void SelectSkill(SkillSequenceArgs skillSeqArgs) {
			Sanity.IfTrueThrow(skillSeqArgs.Self != this, "skillSeqArgs.Self != this");
			skillSeqArgs.PhaseSelect();
		}

		public SkillDef CurrentSkill {
			get {
				SkillSequenceArgs ssa = SkillSequenceArgs.GetSkillSequenceArgs(this);
				if (ssa != null) {
					return ssa.SkillDef;
				}
				return null;
			}
		}

		public SkillSequenceArgs CurrentSkillArgs {
			get {
				return SkillSequenceArgs.GetSkillSequenceArgs(this);
			}
		}

		public SkillSequenceArgs.SkillStrokeTimer CurrentSkillTimer {
			get {
				return SkillSequenceArgs.GetSkillSequenceTimer(this);
			}
		}

		public SkillName CurrentSkillName {
			get {
				SkillSequenceArgs ssa = SkillSequenceArgs.GetSkillSequenceArgs(this);
				if (ssa != null) {
					return (SkillName) ssa.SkillDef.Id;
				}
				return SkillName.None;
			}
		}

		public void AbortSkill() {
			SkillSequenceArgs.AbortSkill(this);
		}

		public virtual TriggerResult On_SkillSelect(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		public virtual TriggerResult On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		public virtual void On_SkillAbort(SkillSequenceArgs skillSeqArgs) {
		}

		public virtual TriggerResult On_SkillFail(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		public virtual TriggerResult On_SkillStroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		public virtual TriggerResult On_SkillSuccess(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		//public virtual bool On_SkillGain(int id, object arg) {
		//	return false;
		//}
		//
		//public virtual bool On_SkillMakeItem(int id, AbstractItem item) {
		//	return false;
		//}
		#endregion Skills

		#region abilities
		public void ActivateAbility(AbilityDef aDef) {
			aDef.Activate(this);
		}

		/// <summary>Enumerator of all character's abilities</summary>
		public IEnumerable<Ability> Abilities {
			get {
				return new AbilitiesEnumerator(this.skillsabilities);
			}
		}

		/// <summary>Check if character has the desired ability (according to the ability def)</summary>
		public bool HasAbility(AbilityDef aDef, out Ability ability) {
			if (this.skillsabilities != null) {
				object o;
				if (this.skillsabilities.TryGetValue(aDef, out o)) {
					ability = (Ability) o; //found ability, cast the return value
					return true;
				}
			}
			ability = null;
			return false;
		}

		public Ability GetAbilityObject(AbilityDef aDef) {
			object retVal = null;
			if (this.skillsabilities != null) {
				this.skillsabilities.TryGetValue(aDef, out retVal);
			}
			return (Ability) retVal; //either null or Ability instance if the player has it
		}

		/// <summary>Get number of modified points the character has for specified AbilityDef. Equals getting the ModifiedPoints property value on the Ability object directly.</summary>
		public int GetAbility(AbilityDef aDef) {
			Ability retAb = this.GetAbilityObject(aDef);
			return (retAb == null ? 0 : retAb.ModifiedPoints); //either null or Ability.Points if the player has it
		}

		/// <summary>Modifies the points of the Ability by given diference. Equals calling ModifyPoints on the Ability object directly.</summary>
		public void ModifyAbilityPoints(AbilityDef aDef, int difference) {
			Ability ab = this.AcquireAbilityObject(aDef);
			ab.ModifyPoints(difference);
		}

		/// <summary>Set specified number of real points the character has for specified AbilityDef. Equals setting the RealPoints property on the Ability object directly.</summary>
		public void SetRealAbilityPoints(AbilityDef aDef, int points) {
			Ability ab = this.AcquireAbilityObject(aDef);
			ab.RealPoints = points;
		}

		private Ability AcquireAbilityObject(AbilityDef def) {
			Sanity.IfTrueSay(def == null, "def == null");
			Ability ab;
			if (!this.HasAbility(def, out ab)) {
				ab = new Ability(def, this);
				this.SkillsAbilities.Add(def, ab);
			}
			return ab;
		}

		internal void InternalRemoveAbility(AbilityDef aDef) {
			this.SkillsAbilities.Remove(aDef);
		}

		internal virtual void On_AbilityValueChanged(AbilityDef aDef, Ability ab, int previousValue) {
		}

		internal virtual TriggerResult On_DenyActivateAbility(DenyAbilityArgs args) {
			return TriggerResult.Continue;
		}

		internal virtual TriggerResult On_ActivateAbility(AbilityDef aDef, Ability ab) {
			return TriggerResult.Continue;
		}

		//internal virtual bool On_DeactivateAbility(ActivableAbilityDef aDef, Ability ab) {
		//    return false;
		//}

		//internal virtual void On_AbilityDeactivate(AbilityDef aDef, Ability ab) {
		//}
		#endregion abilities

		#region roles
		/// <summary>Check if character has been cast to the given role</summary>
		public bool HasRole(Role role) {
			return RolesManagement.HasRole(this, role);
		}

		//these triggers might get alive if they prove to be needed. For now I dont think so
		///// <summary>Called after the character has been cast to some role (the role is already in his assignedRoles list</summary>
		//internal virtual void On_RoleAssign(Role role) {
		//}

		///// <summary>Called after the character has been cast to some role (the role is already in his assignedRoles list</summary>
		//internal virtual void On_RoleUnAssign(Role role) {
		//}
		#endregion roles

		#region Spell cast / effect
		public override void TryCastSpellFromBook(int spellid) {
			MagerySkillDef.TryCastSpellFromBook(this, spellid);
		}

		public void TryCastSpellFromBook(SpellDef spellDef) {
			MagerySkillDef.TryCastSpellFromBook(this, spellDef);
		}

		public virtual TriggerResult On_SpellEffect(SpellEffectArgs spellEffectArgs) {
			return TriggerResult.Continue;
		}

		public virtual TriggerResult On_CauseSpellEffect(IPoint3D target, SpellEffectArgs spellEffectArgs) {
			return TriggerResult.Continue;
		}
		#endregion Spell cast / effect

		public override string PaperdollName {
			get {
				if (!string.IsNullOrEmpty(this.title)) {
					return string.Concat(this.Name, ", ", this.title);
				}
				return this.Name;
			}
		}

		public string Title {
			get {
				return this.title;
			}
			set {
				this.title = value;
			}
		}

		public void AddHits(int howMany) {
			int newCount = this.Hits + howMany;
			this.Hits = (short) Math.Min(0, Math.Max(this.MaxHits, newCount));
		}

		public void AddMana(int howMany) {
			int newCount = this.Mana + howMany;
			this.Mana = (short) Math.Min(0, Math.Max(this.MaxMana, newCount));
		}

		public void AddStamina(int howMany) {
			int newCount = this.Stam + howMany;
			this.Stam = (short) Math.Min(0, Math.Max(this.MaxStam, newCount));
		}

		private static TriggerKey disruptionTK = TriggerKey.Acquire("disruption");
		public void Trigger_Disrupt() {
			this.TryTrigger(disruptionTK, null);
			this.On_Disruption();
		}

		public virtual void On_Disruption() {
			if (this.CurrentSkillName == SkillName.Magery) {
				this.AbortSkill();
			}
		}

		private static TriggerKey hostileActionTK = TriggerKey.Acquire("hostileAction");
		public void Trigger_HostileAction(Character enemy) {
			ScriptArgs sa = new ScriptArgs(enemy);
			this.TryTrigger(hostileActionTK, sa);
			this.On_HostileAction(enemy);
		}

		public virtual void On_HostileAction(Character enemy) {

		}

		private static ContainerDef backpackDef;
		private AbstractItem AddBackpack() {
			this.ThrowIfDeleted();
			if (backpackDef == null) {
				backpackDef = ThingDef.FindItemDef(0xe75) as ContainerDef;
				if (backpackDef == null) {
					throw new SEException("Unable to find itemdef 0xe75 in scripts.");
				}
				if (backpackDef.Layer != (int) LayerNames.Pack) {
					throw new SEException("Wrong layer of backpack itemdef.");
				}
			}

			AbstractItem i = (AbstractItem) backpackDef.Create(this);
			if (i == null) {
				throw new SEException("Unable to create backpack.");
			}
			return i;
		}

		public sealed override AbstractItem GetBackpack() {
			AbstractItem foundPack = this.FindLayer(LayerNames.Pack);
			if (foundPack == null) {
				foundPack = this.AddBackpack();
			}
			return foundPack;
		}

		public Container Backpack {
			get {
				return (Container) this.GetBackpack();
			}
		}

		public BankBox Bank {
			get {
				AbstractItem foundBankbox = this.FindLayer(LayerNames.Bankbox);

				if (foundBankbox == null) {
					var def = SingletonScript<BankBoxDef>.Instance;
					if (def == null) {
						throw new SEException("Unable to find a BankBoxDef in scripts.");
					}
					if (def.Layer != (int) LayerNames.Bankbox) {
						throw new SEException("Wrong layer of bankbox itemdef.");
					}

					AbstractItem i = (AbstractItem) def.Create(this);
					if (i == null) {
						throw new SEException("Unable to create bankbox.");
					}
				}
				return (BankBox) foundBankbox;
			}
		}

		public sealed override AbstractItem NewItem(IThingFactory arg, int amount) {
			return this.Backpack.NewItem(arg, amount);
		}

		public Equippable NewEquip(IThingFactory factory) {
			Thing t = factory.Create(this);
			Equippable i = t as Equippable;
			if (i != null) {
				if (i.Cont != this) {
					i.Delete();
					throw new SEException("'" + i + "' ended not equipped on the char... Wtf?");
				}
				return i;
			}
			if (t != null) {
				t.Delete();//we created something else
			}
			throw new SEException(factory + " did not create an equippable item.");
		}

		public override void On_Dupe(Thing model) {
			Character copyFrom = (Character) model;

			if (copyFrom.skillsabilities != null) {
				foreach (object o in copyFrom.skillsabilities.Values) {
					Ability a = o as Ability;
					if (a != null) {
						this.SkillsAbilities.Add(a.Def,
							new Ability(a, this)); //add copy of the Ability object to the duped char's storage
					} else {
						Skill s = (Skill) o;
						this.SkillsAbilities.Add(AbstractSkillDef.GetById(s.Id),
							new Skill(s, this)); //add copy of the Skill object to the duped char's storage
					}
				}
			}
		}

		#region Persistence
		public override void On_Save(SaveStream output) {
			if (this.skillsabilities != null) {
				foreach (object o in this.skillsabilities.Values) {
					Ability a = o as Ability;
					if (a != null) {
						output.WriteLine(string.Concat(a.Def.PrettyDefname, "=", a.GetSaveString()));
					} else {
						Skill s = (Skill) o;
						output.WriteLine(string.Concat(s.Def.PrettyDefname, "=", s.GetSaveString()));
					}
				}
			}
			base.On_Save(output);
		}

		public override void On_Load(PropsSection input) {
			List<PropsLine> linesList = new List<PropsLine>(input.PropsLines); //can't iterate over the property itself, popping the lines would break the iteration
			foreach (PropsLine line in linesList) {
				AbilityDef abDef = AbilityDef.GetByDefname(line.Name);
				if (abDef != null) {
					input.PopPropsLine(line.Name);
					Ability ab = this.AcquireAbilityObject(abDef);
					if (!ab.LoadSavedString(line.Value)) {
						Logger.WriteError(input.Filename, line.Line, "Unrecognised ability value format.");
					}
				} else {
					AbstractSkillDef skillDef = AbstractSkillDef.GetByDefname(line.Name);
					if (skillDef != null) {
						input.PopPropsLine(line.Name);
						Skill skill = this.AcquireSkillObject(skillDef);
						if (!skill.LoadSavedString(line.Value)) {
							Logger.WriteError(input.Filename, line.Line, "Unrecognised skill value format.");
						}
					}
				}
			}
			base.On_Load(input);
		}
		#endregion Persistence

		/// <summary>Check if the current character has plevel greater than 1 (is more than player)</summary>
		public bool IsGM {
			get {
				AbstractAccount acc = this.Account;
				if (acc != null) {
					return acc.PLevel > 1;
				}
				return false;
			}
		}

		//method: On_DClick
		//Character`s implementation of @Dclick trigger, 
		//basic implementation = paperdoll sending and horse mounting
		public override void On_DClick(AbstractCharacter from) {
			if (from != null && from.IsPlayer) {
				//PC
				if (from == this && this.Mount != null) {
					this.Dismount();
				} else {
					if (from != this && this.IsMountableBy((Character) from)) {
						from.Mount = this;
					} else { //TODO? paperdoll only for humanoids? show backpack of pets?
						this.ShowPaperdollTo(from);
					}
				}
			}
		}

		public override TriggerResult On_DenyItemDClick(DenyClickArgs args) {
			DenyResult dr = this.CanReach(args.Target);
			args.Result = dr;
			return dr.Allow ? TriggerResult.Continue : TriggerResult.Cancel;
		}

		public override void On_ItemDClick(AbstractItem dClicked) {

			//TODO? maybe not in all cases (healing?)? maybe not at all?
			this.Trigger_Disrupt();

			base.On_ItemDClick(dClicked);
		}

		public override HighlightColor GetHighlightColorFor(AbstractCharacter viewer) {
			//TODO
			return HighlightColor.NoColor;
		}

		#region Weight

		float weight;

		public override float Weight {
			get {
				return this.weight;
			}
		}

		public override void FixWeight() {
			CharSyncQueue.AboutToChangeStats(this);
			float w = this.Def.Weight;
			foreach (AbstractItem i in this) {
				if (i != null) {
					i.FixWeight();
					w += i.Weight;
				}
			}
			this.weight = w;
		}

		protected override void AdjustWeight(float adjust) {
			CharSyncQueue.AboutToChangeStats(this);
			this.weight += adjust;
		}
		#endregion Weight

		/// <summary>Message displayed in red - used for importatnt system or ingame messages (warnings, errors etc)</summary>
		public void RedMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Red);
		}

		/// <summary>Message displayed in red - used for importatnt system or ingame messages (warnings, errors etc)</summary>
		public void RedMessageCliloc(int arg) {
			this.ClilocSysMessage(arg, (int) Hues.Red);
		}

		/// <summary>Message displayed in blue - used for ingame purposes</summary>
		public void BlueMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Blue);
		}

		/// <summary>Message displayed in green - used for ingame purposes</summary>
		public void GreenMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Green);
		}

		/// <summary>Message displayed in green - used for ingame purposes</summary>
		public void InfoMessage(string arg) {
			this.SysMessage(arg, (int) Hues.Info);
		}

		public Item Hair {
			get {
				return (Item) this.FindLayer(LayerNames.Hair);
			}
		}

		public Item Beard {
			get {
				return (Item) this.FindLayer(LayerNames.Beard);
			}
		}

		#region combat
		public override void Trigger_PlayerAttackRequest(AbstractCharacter target) {
			if (this == target || target == null) {
				return;
			}

			//TODO? triggers

			WeaponSkillTargetQueuePlugin.AddTarget(this, (Character) target);
		}

		public virtual void On_SkillChange(Skill skill, int oldModifiedValue) {
			switch (skill.Name) {
				case SkillName.Parry: //Efficiency of shield
					this.InvalidateCombatArmorValues();
					break;
				case SkillName.Tactics: //Attack
				case SkillName.Anatomy: //Attack
				case SkillName.ArmsLore: //Attack
				case SkillName.SpiritSpeak: //MindPower
				case SkillName.EvalInt: //MindPower
					this.InvalidateCombatWeaponValues();
					break;
			}
		}

		CombatCalculator.CombatWeaponValues combatWeaponValues;
		CombatCalculator.CombatArmorValues combatArmorValues;
		internal Projectile weaponProjectile;

		public double ArmorMaterial {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.material;
			}
		}

		public int ArmorClassVsP {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.armorVsP;
			}
		}

		public int ArmorClassVsM {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.armorVsM;
			}
		}

		public override short StatusArmorClass {
			get {
				this.AcquireCombatArmorValues();
				return (short) ((this.combatArmorValues.armorVsP + this.combatArmorValues.armorVsM) / 2);
			}
		}

		private static TagKey armorClassModifierTK = TagKey.Acquire("_armor_class_modifier_");
		public int ArmorClassModifier {
			get {
				return Convert.ToInt32(this.GetTag(armorClassModifierTK));
			}
			set {
				this.InvalidateCombatArmorValues();
				if (value != 0) {
					this.SetTag(armorClassModifierTK, value);
				} else {
					this.RemoveTag(armorClassModifierTK);
				}
			}
		}

		public int MindDefenseVsP {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.mindDefenseVsP;
			}
		}

		public int MindDefenseVsM {
			get {
				this.AcquireCombatArmorValues();
				return this.combatArmorValues.mindDefenseVsM;
			}
		}

		public override short StatusMindDefense {
			get {
				this.AcquireCombatArmorValues();
				return (short) ((this.combatArmorValues.mindDefenseVsP + this.combatArmorValues.mindDefenseVsM) / 2);
			}
		}

		private static TagKey mindDefenseModifierTK = TagKey.Acquire("_mind_defense_modifier_");
		public int MindDefenseModifier {
			get {
				return Convert.ToInt32(this.GetTag(mindDefenseModifierTK));
			}
			set {
				this.InvalidateCombatArmorValues();
				if (value != 0) {
					this.SetTag(mindDefenseModifierTK, value);
				} else {
					this.RemoveTag(mindDefenseModifierTK);
				}
			}
		}

		public void InvalidateCombatWeaponValues() {
			if (this.combatWeaponValues != null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatWeaponValues = null;
			}
		}

		public void InvalidateCombatArmorValues() {
			if (this.combatArmorValues != null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatArmorValues = null;
			}
		}

		private void AcquireCombatWeaponValues() {
			if (this.combatWeaponValues == null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatWeaponValues = CombatCalculator.CalculateCombatWeaponValues(this);
			}
		}

		private void AcquireCombatArmorValues() {
			if (this.combatArmorValues == null) {
				CharSyncQueue.AboutToChangeStats(this);
				this.combatArmorValues = CombatCalculator.CalculateCombatArmorValues(this);
			}
		}

		public override void On_ItemEnter(ItemInCharArgs args) {
			if (args.ManipulatedItem is Wearable) {
				this.InvalidateCombatArmorValues();
			} else if (args.ManipulatedItem is Weapon) {
				this.InvalidateCombatWeaponValues();
			}
			base.On_ItemEnter(args);
		}

		public override void On_ItemLeave(ItemInCharArgs args) {
			if (args.ManipulatedItem is Wearable) {
				this.InvalidateCombatArmorValues();
			} else if (args.ManipulatedItem is Weapon) {
				this.InvalidateCombatWeaponValues();
			}
			base.On_ItemLeave(args);
		}

		public virtual bool IsPlayerForCombat {
			get {
				//TODO: false for hypnomystic
				return this.IsPlayer;
			}
		}

		public virtual CharacterDef DefForCombat {
			get {
				//TODO: monster def for hypnomystic
				return this.TypeDef;
			}
		}

		public Weapon Weapon {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.weapon;
			}
		}

		public int WeaponAttackVsP {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.attackVsP;
			}
		}

		public int WeaponAttackVsM {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.attackVsM;
			}
		}

		public int WeaponPiercing {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.piercing;
			}
		}

		public WeaponType WeaponType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.weaponType;
			}
		}

		public DamageType WeaponDamageType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.damageType;
			}
		}

		public WeaponAnimType WeaponAnimType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.weaponAnimType;
			}
		}

		public Projectile WeaponProjectile {
			get {
				if ((this.weaponProjectile != null) &&
						(this.weaponProjectile.IsDeleted ||
						(this.weaponProjectile.TopObj() != this) ||
						(this.weaponProjectile.Amount < 1))) {
					this.weaponProjectile = null;//we had ammo but now don't have it anymore
					this.InvalidateCombatWeaponValues();
				} else if ((this.weaponProjectile == null) && (this.combatWeaponValues != null) &&
						(this.combatWeaponValues.projectileType != ProjectileType.None)) {
					this.InvalidateCombatWeaponValues();//we have no ammo but we should, let's look for it
				}
				this.AcquireCombatWeaponValues();
				return this.weaponProjectile;
			}
		}

		public int WeaponProjectileAnim {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.projectileAnim;
			}
		}

		public ProjectileType WeaponProjectileType {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.projectileType;
			}
		}

		public int WeaponRangeVsM {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.rangeVsM;
			}
		}

		public int WeaponRangeVsP {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.rangeVsP;
			}
		}

		private static TagKey weaponRangeModifierTK = TagKey.Acquire("_weapon_range_modifier_");
		public int WeaponRangeModifier {
			get {
				return Convert.ToInt32(this.GetTag(weaponRangeModifierTK));
			}
			set {
				this.InvalidateCombatWeaponValues();
				if (value != 0) {
					this.SetTag(weaponRangeModifierTK, value);
				} else {
					this.RemoveTag(weaponRangeModifierTK);
				}
			}
		}

		public int WeaponStrikeStartRange {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.strikeStartRange;
			}
		}

		public int WeaponStrikeStopRange {
			get {
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.strikeStopRange;
			}
		}

		public TimeSpan WeaponDelay {
			get {
				//TODO: mana-dependant for mystic
				this.AcquireCombatWeaponValues();
				return this.combatWeaponValues.delay;
			}
		}

		public int MindPowerVsP {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.mindPowerVsP;
			}
		}

		public int MindPowerVsM {
			get {
				this.AcquireCombatWeaponValues();
				return (int) this.combatWeaponValues.mindPowerVsM;
			}
		}

		public TriggerResult On_BeforeSwing(WeaponSwingArgs args) {
			return TriggerResult.Continue;
		}

		public TriggerResult On_BeforeGetSwing(WeaponSwingArgs args) {
			return TriggerResult.Continue;
		}

		public void On_CauseDamage(DamageArgs args) {
		}

		public void On_Damage(DamageArgs args) {
		}

		public void On_AfterSwing(WeaponSwingArgs args) {
		}

		public void On_AfterGetSwing(WeaponSwingArgs args) {
		}

		/// <summary>hodi zbran do batohu</summary>
		public void DisArm() {
			Weapon w = this.Weapon;
			if (w != null)
				w.Cont = this.Backpack;
		}
		#endregion combat

		public override void On_LogOut() {
			this.AbortSkill();
			DialogStacking.ClearDialogStack(this);
			base.On_LogOut();
		}


		public CharModelInfo CharModelInfo {
			get {
				int model = this.Model;
				if ((this.charModelInfo == null) || (this.charModelInfo.model != model)) {
					this.charModelInfo = CharModelInfo.GetByModel(model);
				}
				return this.charModelInfo;
			}
		}

		/**
			These are flags which specify what kind of model this is, and what anims it has.
		*/
		public uint AnimsAvailable {
			get {
				return this.CharModelInfo.AnimsAvailable;
			}
		}

		public bool IsHuman {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Human) == CharAnimType.Human;
			}
		}

		public bool IsAnimal {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Animal) == CharAnimType.Animal;
			}
		}

		public bool IsMonster {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Monster) == CharAnimType.Monster;
			}
		}

		public Gender Gender {
			get {
				return this.CharModelInfo.gender;
			}
		}

		public override bool IsMale {
			get {
				return this.CharModelInfo.isMale;
			}
		}

		public override bool IsFemale {
			get {
				return this.CharModelInfo.isFemale;
			}
		}

		public virtual TriggerResult On_DenyOpenDoor(DenySwitchDoorArgs args) {
			return TriggerResult.Continue;
		}

		public virtual TriggerResult On_DenyCloseDoor(DenySwitchDoorArgs args) {
			return TriggerResult.Continue;
		}

		public Party Party {
			get {
				return Party.GetParty(this);
			}
		}

		public override ICollection<AbstractCharacter> PartyMembers {
			get {
				Party p = Party.GetParty(this);
				if (p != null) {
					return (ICollection<AbstractCharacter>) p.Members;
				}
				return EmptyReadOnlyGenericCollection<AbstractCharacter>.instance;
			}
		}

		public virtual void On_Dispell(SpellEffectArgs spellEffectArgs) {
		}
	}

	[ViewableClass]
	public partial class CharacterDef {
		private CharModelInfo charModelInfo;

		private CorpseDef corpseDef;
		public CorpseDef CorpseDef {
			get {
				if (this.corpseDef == null) {
					this.corpseDef = FindItemDef(this.CorpseModel) as CorpseDef;
				}
				return this.corpseDef;
			}
		}

		public CharModelInfo CharModelInfo {
			get {
				int model = this.Model;
				if ((this.charModelInfo == null) || (this.charModelInfo.model != model)) {
					this.charModelInfo = CharModelInfo.GetByModel(model);
				}
				return this.charModelInfo;
			}
		}

		public bool IsHuman {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Human) == CharAnimType.Human;
			}
		}

		public bool IsAnimal {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Animal) == CharAnimType.Animal;
			}
		}

		public bool IsMonster {
			get {
				return (this.CharModelInfo.charAnimType & CharAnimType.Monster) == CharAnimType.Monster;
			}
		}

		public Gender Gender {
			get {
				return this.CharModelInfo.gender;
			}
		}

		public override bool IsMale {
			get {
				return this.CharModelInfo.isMale;
			}
		}

		public override bool IsFemale {
			get {
				return this.CharModelInfo.isFemale;
			}
		}

	}

	public static class DenyResultMessages_Character {
		public static readonly DenyResult Deny_IAmDeadAndCannotDoThat =
			new DenyResult_ClilocSysMessage(1019048, 0x3B2); // I am dead and cannot do that.

		public static readonly DenyResult Deny_YoureAGhostAndCantDoThat =
			new DenyResult_ClilocSysMessage(500590, 0x3B2);  //You're a ghost, and can't do that.

		public static readonly DenyResult Deny_TargetIsDead =
			new CompiledLocDenyResult<CharacterLoc>("TargetIsDead");

		public static readonly DenyResult Deny_TargetIsInsubst =
			new CompiledLocDenyResult<CharacterLoc>("TargetIsInsubst");
	}

	public class CharacterLoc : CompiledLocStringCollection<CharacterLoc> {
		public string TargetIsDead = "The target is dead.";
		public string TargetIsInsubst = "The target is insubstantial.";
	}
}
