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
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {



	/// <summary>Sphere-style item ATTR bitflags (#215/#217) - real hex values, confirmed against
	/// SphereServer's own sphere_defs.scp ([DEFNAME attr_flags]) and wiki.spherecommunity.net.
	/// Named exactly as Sphere names them (snake_case, "attr_" prefix) for direct 1:1 .def-content
	/// compatibility rather than C#-idiomatic PascalCase. Set via a ".def" "ATTR = attr_x|attr_y"
	/// line (parsed through the same generic FieldValue/ConvertTools.ConvertTo pipeline already
	/// used for every other [Flags]-enum .def field in this engine - that pipeline already
	/// replaces '|' with ',' before Enum.Parse, so no custom parsing code was needed here).
	///
	/// Real moriaengine bit-mapping note (#215): this engine's old LScript constants
	/// (scripts/Basic/defs.scp) had a non-standard "attr_stolen" at bit 0x01, where real Sphere
	/// defines "attr_identified" instead and doesn't define attr_stolen as an ATTR bit at all.
	/// Fixed here - attr_identified correctly occupies 0x01, attr_stolen is gone (zero real
	/// content used either value).</summary>
	[Flags]
	public enum ItemAttributes {
		none             = 0x00000000,
		attr_identified  = 0x00000001,
		attr_decay       = 0x00000002,
		attr_newbie      = 0x00000004,
		attr_move_always = 0x00000008,
		attr_move_never  = 0x00000010,
		attr_magic       = 0x00000020,
		attr_owned       = 0x00000040,
		attr_invis       = 0x00000080,
		attr_cursed      = 0x00000100,
		attr_blessed     = 0x00000400,
		attr_static      = 0x00008000,
		attr_exceptional = 0x00010000,
		attr_enchanted   = 0x00020000,
		attr_imbued      = 0x00040000,
		attr_questitem   = 0x00080000,
		attr_insured     = 0x00100000,
		attr_nodroptrade = 0x00200000,
		attr_norepair    = 0x00400000,
		attr_lockeddown  = 0x00800000,
		attr_secure      = 0x01000000,
		attr_reforged    = 0x02000000,
		attr_opened      = 0x04000000,
	}

	[ViewableClass]
	public partial class Item : AbstractItem {
		/// <summary>Sphere-style ATTR bitflags (#215/#217), per-instance and independently
		/// overridable from the ItemDef's own AttributesDefault once seeded (see On_Create).</summary>
		public ItemAttributes Attributes {
			get {
				return this.attributes;
			}
			set {
				this.attributes = value;
			}
		}

		public bool HasAttribute(ItemAttributes flag) {
			return (this.attributes & flag) == flag;
		}

		/// <summary>#223 - attr_owned's own owner-UID link. Named OwnedBy, not Owner - House and
		/// Corpse (both Item subclasses) already each have their own, semantically different
		/// "Owner" (who owns the house / whose body is this), and a same-named property here
		/// would silently shadow both (confirmed by the CS0108 warnings this caused on a first
		/// attempt named Owner - fixed by picking a distinct name instead of hiding theirs).</summary>
		private static readonly TagKey ownedByTK = TagKey.Acquire("_itemOwner_");
		public Character OwnedBy {
			get {
				return this.GetTag(ownedByTK) as Character;
			}
			set {
				this.SetTag(ownedByTK, value);
			}
		}

		public void SetAttribute(ItemAttributes flag, bool value) {
			if (value) {
				this.attributes |= flag;
			} else {
				this.attributes &= ~flag;
			}
			if ((flag == ItemAttributes.attr_decay) && value) {
				ItemDecaySystem.EnsureSweepRunning(); //#219 - lazily self-installs on first real use
			}
			if ((flag == ItemAttributes.attr_static) || (flag == ItemAttributes.attr_move_never) || (flag == ItemAttributes.attr_move_always)) {
				this.RecomputeNonMovable(); //#220
			}
			if (flag == ItemAttributes.attr_invis) {
				this.Flag_Invisible = value; //#224 - the only write path Flag_Invisible ever gets
			}
		}

		/// <summary>#224 - attr_invis's own per-viewer exception list, matching real Sphere's
		/// own "TAG.SeenBy_[CharUID]" pattern exactly rather than inventing a different one (per
		/// the issue's explicit instruction) - a dynamically-keyed TagKey per viewer UID, same
		/// mechanism every other tag in this engine already uses, just computed instead of a
		/// static literal.</summary>
		public bool IsSeenBy(Character viewer) {
			if (viewer == null) {
				return false;
			}
			return this.GetTag(TagKey.Acquire("_seenBy_" + viewer.Uid)) != null;
		}

		public void GrantVisibilityTo(Character viewer) {
			this.SetTag(TagKey.Acquire("_seenBy_" + viewer.Uid), true);
		}

		public void RevokeVisibilityFrom(Character viewer) {
			this.RemoveTag(TagKey.Acquire("_seenBy_" + viewer.Uid));
		}

		/// <summary>#220 - Flag_NonMovable had no write path at all before this (confirmed via
		/// grep: zero consumers anywhere, not even a read path - the issue's own "already read
		/// by real movement/pickup checks" framing didn't hold up, see On_DenyPickup below,
		/// newly added in the same pass). attr_move_always is an explicit override and wins over
		/// attr_static/attr_move_never if somehow both are set on the same item.</summary>
		private void RecomputeNonMovable() {
			bool nonMovable = this.HasAttribute(ItemAttributes.attr_static) || this.HasAttribute(ItemAttributes.attr_move_never);
			if (this.HasAttribute(ItemAttributes.attr_move_always)) {
				nonMovable = false;
			}
			this.Flag_NonMovable = nonMovable;
		}

		/// <summary>#220/#221 - the actual read path for Flag_NonMovable (none existed before
		/// #220) plus attr_lockeddown/attr_secure's own, narrower "owner/co-owner only" pickup
		/// restriction inside a house (#221) - friends can use the house but, per real UO's own
		/// lockdown rules, can't move another player's locked-down belongings.
		/// DenyResultMessages.Deny_YouCannotPickThatUp/Deny_ThatIsLocked are both real, already
		/// pre-existing Sphere/UO clilocs that had never been referenced anywhere in this engine
		/// before #220/#221.</summary>
		public override TriggerResult On_DenyPickup(DenyPickupArgs args) {
			if (this.Flag_NonMovable) {
				args.Result = DenyResultMessages.Deny_YouCannotPickThatUp;
				return TriggerResult.Cancel;
			}
			if (this.HasAttribute(ItemAttributes.attr_lockeddown) || this.HasAttribute(ItemAttributes.attr_secure)) {
				House house = House.FindHouseAt(this);
				if ((house != null) && !house.CanMoveLockedItems(args.PickingChar as Character)) {
					args.Result = DenyResultMessages.Deny_ThatIsLocked;
					return TriggerResult.Cancel;
				}
			}
			if (this.HasAttribute(ItemAttributes.attr_owned) && (this.OwnedBy != null)) {
				Character picker = args.PickingChar as Character;
				if ((picker != this.OwnedBy) && ((picker == null) || !picker.IsGM)) {
					args.Result = DenyResultMessages.Deny_ThatDoesNotBelongToYou;
					return TriggerResult.Cancel;
				}
			}
			return base.On_DenyPickup(args);
		}

		/// <summary>#223 - attr_nodroptrade's "can't be dropped on the ground" half. The other
		/// half (can't be placed into a secure-trade window) is a separate check inside
		/// SecureTrade.AddItem - that path deliberately bypasses the normal deny-trigger chain
		/// entirely (see its own doc comment), so this hook alone wouldn't have caught it.</summary>
		public override TriggerResult On_DenyPutOnGround(DenyPutOnGroundArgs args) {
			if (this.HasAttribute(ItemAttributes.attr_nodroptrade)) {
				args.Result = new DenyResult_StringSysMessage("You cannot drop this item.");
				return TriggerResult.Cancel;
			}
			return base.On_DenyPutOnGround(args);
		}

		/// <summary>#221 - attr_secure's "access restricted to owner/co-owners/friends" half
		/// (attr_lockeddown alone does NOT restrict access/viewing, only movement - see
		/// On_DenyPickup above). Called on the CONTAINER when someone tries to take an item OUT
		/// of it, so this only matters for secure items that are themselves containers.
		/// DenyResultMessages.Deny_ContainerClosed is a real, already pre-existing cliloc that
		/// fits this denial well ("You cannot peek into the container.").</summary>
		public override void On_DenyPickupItemFrom(DenyPickupArgs args) {
			if (this.HasAttribute(ItemAttributes.attr_secure)) {
				House house = House.FindHouseAt(this);
				if ((house != null) && !house.HasAccess(args.PickingChar as Character)) {
					args.Result = DenyResultMessages.Deny_ContainerClosed;
					return;
				}
			}
			base.On_DenyPickupItemFrom(args);
		}

		/// <summary>#222/#224 - magic/crafting-quality flags are flags+display only for now (no
		/// crafting-bonus/item-upgrade system exists to attach a real mechanical effect to,
		/// confirmed with the user as acceptable scope) - a name prefix is the same "sensible
		/// minimal display effect" a real UO client already shows for "exceptional" crafted
		/// items, reusing the existing name/cliloc pipeline rather than a new one. Precedence
		/// (highest first) matters only for an item carrying more than one of these flags at
		/// once, which real content shouldn't normally do - reforged implies the most rework,
		/// so it wins.
		///
		/// #224's own shallow tie-in: attr_exceptional is a visible craftsmanship mark, shown
		/// regardless (matches real UO - exceptional quality is visible without identifying
		/// anything). The other 4 are magical-property markers, hidden until attr_identified is
		/// set - this IS attr_identified's entire "reveals more detail" effect for this pass,
		/// deliberately not a new Identification-skill mechanic (per the issue's own scoping
		/// note).</summary>
		private string QualityPrefix {
			get {
				bool identified = this.HasAttribute(ItemAttributes.attr_identified);
				if (identified && this.HasAttribute(ItemAttributes.attr_reforged)) {
					return "reforged";
				}
				if (identified && this.HasAttribute(ItemAttributes.attr_imbued)) {
					return "imbued";
				}
				if (identified && this.HasAttribute(ItemAttributes.attr_enchanted)) {
					return "enchanted";
				}
				if (this.HasAttribute(ItemAttributes.attr_exceptional)) {
					return "exceptional";
				}
				if (identified && this.HasAttribute(ItemAttributes.attr_magic)) {
					return "magic";
				}
				return null;
			}
		}

		public override string Name {
			get {
				string prefix = this.QualityPrefix;
				if (prefix == null) {
					return base.Name;
				}
				return prefix + " " + base.Name;
			}
			set {
				base.Name = value;
			}
		}

		/// <summary>Seeds this instance's own Attributes (Item.ct's "attributes" Vars field,
		/// per-instance and independently overridable afterwards) from its ItemDef's configured
		/// default (#217) - same "copy a Def-level default into the instance once at creation"
		/// pattern NPC.On_Create already uses for DefaultMovementSettings.</summary>
		public override void On_Create() {
			base.On_Create();
			this.attributes = this.TypeDef.AttributesDefault;
			if (this.HasAttribute(ItemAttributes.attr_decay)) {
				ItemDecaySystem.EnsureSweepRunning(); //#219
			}
			this.RecomputeNonMovable(); //#220
			if (this.HasAttribute(ItemAttributes.attr_invis)) {
				this.Flag_Invisible = true; //#224
			}
		}

		/// <summary>
		/// Consume desired amount of this item, amount cannot go below zero. If resulting amount is 0
		///  then the item will be deleted. Method returns the actually consumed amount.
		///  </summary>
		public long Consume(long howMany) {
			int prevAmount = this.Amount;

			long resultAmount;
			try {
				resultAmount = checked(prevAmount - howMany);
			} catch (OverflowException) {
				resultAmount = -1; //so we delete
			}

			if (resultAmount < 1) {
				this.Delete();
				return prevAmount;//consumed all of the item (not necesarilly the whole "howMuch")
			}
			this.Amount = (int) resultAmount;
			return howMany; //consumed the desired amount
		}

		public override byte FlagsToSend {
			get {
				int retVal = 0;

				if (this.IsNotVisible) {
					retVal |= 0x80;
				}

				if (this.Flag_DisplayAsMovable) {
					retVal |= 0x20;
				}

				return (byte) retVal;
			}
		}

		public bool Flag_Invisible {
			get {
				return this.ProtectedFlag1;
			}
			set {
				if (this.ProtectedFlag1 != value) {
					if (value) {
						OpenedContainers.SetContainerClosed(this);
						this.RemoveFromView();
					}
					ItemSyncQueue.AboutToChange(this);
					this.ProtectedFlag1 = value;
				}
			}
		}

		public bool Flag_Newbied {
			get {
				return this.ProtectedFlag2;
			}
			set {
				this.ProtectedFlag2 = value;
			}
		}

		public bool Flag_DisplayAsMovable {
			get {
				return this.ProtectedFlag3;
			}
			set {
				if (this.ProtectedFlag3 != value) {
					ItemSyncQueue.AboutToChange(this);
					this.ProtectedFlag3 = value;
				}
			}
		}

		public sealed override bool Flag_NonMovable {
			get {
				return this.ProtectedFlag4;
			}
			set {
				this.ProtectedFlag4 = value;
			}
		}

		public sealed override bool IsNotVisible {
			get {
				return (this.Flag_Invisible || !this.IsInVisibleLayer || this.Flag_Disconnected);
			}
		}

		public virtual bool IsMusicalInstrument {
			get {
				return false;
			}
		}

		/// <summary>Enumerates every item in this item and items in all subcontainers, recurses into infinite depth.</summary>
		public IEnumerable<Item> EnumDeep() {
			this.ThrowIfDeleted();
			IEnumerator e = this.GetEnumerator();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				yield return i;
			}
			e.Reset();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				foreach (Item deep in i.EnumDeep()) {
					yield return deep;
				}
			}
		}

		/// <summary>Enumerates every item in this item and items in all subcontainers, but does not recurse deeper.</summary>
		public IEnumerable<Item> EnumShallow() {
			this.ThrowIfDeleted();
			IEnumerator e = this.GetEnumerator();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				yield return i;
			}
			e.Reset();
			while (e.MoveNext()) {
				Item i = (Item) e.Current;
				foreach (Item shallow in i) {
					yield return shallow;
				}
			}
		}

		public override float Weight {
			get {
				return this.Def.Weight;
			}
		}

		public override void FixWeight() {
		}

		public override byte DirectionByte {
			get {
				return (byte) this.TypeDef.Light;
			}
		}

		public virtual LightType Light {
			get {
				return this.TypeDef.Light;
			}
		}

		public Item FindType(TriggerGroup type) {
			return this.FindByType(type);
		}

		public Item FindByType(TriggerGroup type) {
			this.ThrowIfDeleted();
			foreach (Item i in this.EnumDeep()) {
				if (i.Type == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindByTypeShallow(TriggerGroup type) {
			this.ThrowIfDeleted();
			foreach (Item i in this.EnumShallow()) {
				if (i.Type == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindByClass(Type type) {
			foreach (Item i in this.EnumDeep()) {
				if (i.GetType() == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindByClassShallow(Type type) {
			foreach (Item i in this.EnumShallow()) {
				if (i.GetType() == type) {
					return i;
				}
			}
			return null;
		}

		public Item FindById(ItemDef def) {
			foreach (Item i in this.EnumDeep()) {
				if (i.Def == def) {
					return i;
				}
			}
			return null;
		}

		public Item FindByIdShallow(ItemDef def) {
			foreach (Item i in this.EnumShallow()) {
				if (i.Def == def) {
					return i;
				}
			}
			return null;
		}

		public override void GetNameCliloc(out int id, out string argument) {
			string name = this.Name;
			int amount = this.Amount;
			id = 1042971;//~1_NOTHING~
			argument = null;
			if (amount <= 1) {
				ItemDispidInfo idi = this.TypeDef.DispidInfo;
				if (idi != null) {
					if (StringComparer.OrdinalIgnoreCase.Equals(name, idi.SingularName)) {
						id = (1020000 + (this.Model & 16383)); //hmmm...
						return;
					}
				}
				argument = name;
			} else {
				id = 1050039;//~1_NUMBER~ ~2_ITEMNAME~
				argument = string.Concat(amount.ToString(), "\t", name);
			}
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			int amount = this.Amount;
			if (this.Amount <= 1) {
				PacketSequences.SendNameFrom(clickerConn, this, this.Name, 0);
			} else {
				PacketSequences.SendNameFrom(clickerConn, this, string.Concat(amount.ToString(), " ", this.Name), 0);
			}
		}

		/// <summary>Sphere-style ATTR death-drop rules (#217/#218). Only ever evaluated from
		/// Corpse's own "move items off a dying character" pass, so consuming attr_insured
		/// here (it protects exactly once, then clears itself) is safe - this getter has no
		/// other caller that could burn it accidentally.</summary>
		public virtual bool CanFallToCorpse {
			get {
				if (this.HasAttribute(ItemAttributes.attr_cursed)) {
					return true; //cursed always drops, overrides newbie/blessed/insured
				}
				if (this.HasAttribute(ItemAttributes.attr_newbie) || this.HasAttribute(ItemAttributes.attr_blessed)) {
					return false;
				}
				if (this.HasAttribute(ItemAttributes.attr_insured)) {
					this.SetAttribute(ItemAttributes.attr_insured, false); //consumed - protects once
					return false;
				}
				return true;
			}
		}

		private static TagKey linkTK = TagKey.Acquire("_link_");
		public virtual Thing Link {
			get {
				return this.GetTag(linkTK) as Thing;
			}
			set {
				if (value == null) {
					this.RemoveTag(linkTK);
				} else {
					this.SetTag(linkTK, value);
				}
			}
		}

		public override AbstractItem NewItem(IThingFactory factory, int amount) {
			Thing t = factory.Create(this);
			AbstractItem i = t as AbstractItem;
			if (i != null) {
				if (i.IsStackable) {
					i.Amount = amount;
				}
				return i;
			}
			if (t != null) {
				t.Delete();//we created a character, wtf? :)
			}
			throw new SEException(factory + " did not create an item.");
		}

		//public void PlayDropSound(AbstractCharacter droppingChar) {
		//    ScriptArgs sa = new ScriptArgs(droppingChar);
		//    if (!TryCancellableTrigger(TriggerKey.playDropSound, sa)) {
		//        On_DropSound(droppingChar);
		//    }
		//}

		//public virtual void On_DropSound(AbstractCharacter droppingChar) {
		//    this.SoundTo(this.TypeDef.DropSound, droppingChar);
		//}

		public virtual TriggerResult On_SpellEffect(SpellEffectArgs spellEffectArgs) {
			return TriggerResult.Continue;
		}

		private static TagKey morepTK = TagKey.Acquire("_morep_");

		public virtual Point4D MoreP {
			get {
				return (Point4D) this.GetTag(morepTK);
			}
			set {
				if (value == null) {
					this.RemoveTag(morepTK);
				} else {
					this.SetTag(morepTK, value);
				}
			}
		}

		public virtual void On_Dispell(SpellEffectArgs spellEffectArgs) {
		}

		public int RecursiveCount {
			get {
				int c = 0;

				foreach (Item i in this) {
					c += i.RecursiveCount + 1;
				}

				return c;
			}
		}
	}

	[ViewableClass]
	public partial class ItemDef {

		public bool IsWearableDef {
			get {
				return (this is WearableDef);
			}
		}

		public bool IsDestroyableDef {
			get {
				return (this is DestroyableDef);
			}
		}

		public bool IsWeaponDef {
			get {
				return (this is WeaponDef);
			}
		}

		protected override void On_AfterLoadFromScripts() {
			base.On_AfterLoadFromScripts();

			ResourcesList.ThrowIfNotConsumable(this.Resources);
			//ResourcesList.ThrowIfNotConsumable(this.Resources2);
		}
	}
}