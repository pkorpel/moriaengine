using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#920 - the bits of Erebor's tag.gateFlags that have something to act on here.</summary>
	[Flags]
	public enum VarGateFlags {
		None = 0,
		NpcGoThrough = 0x1,
		GhostGoThrough = 0x4,
		ExpGate = 0x8,
		RequiresLevel = 0x10,
		RemoveCursor = 0x20,
		RemovePvpTools = 0x40,
		RemoveMount = 0x80,
		RemoveBank = 0x100,
		RemoveItems = 0x200,
		AddItems = 0x400,
		RunFunctions = 0x800,
		Guarded = 0x1000,
		HasPlugins = 0x2000,
		Sideways = 0x4000,
		AddItemTimeLimit = 0x8000,
		RecallPort = 0x10000
	}

	/// <summary>A line of a gate's remove, add or function list. Kind is 0 item, 1 type, 2 event when
	/// removing, 0 item, 1 event when adding.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class VarGateEntry {
		public const int RemoveItem = 0, RemoveType = 1, RemoveEvent = 2, AddItem = 0, AddEvent = 1;

		[SaveableData, CopyableData]
		public string name = "";

		[SaveableData, CopyableData]
		public int amount;

		[SaveableData, CopyableData]
		public int kind;

		/// <summary>Removing: one can pass without having the thing.</summary>
		[SaveableData, CopyableData]
		public bool optional;

		/// <summary>Removing or adding: only counted, nothing changes hands. A function: it can stop the walker.</summary>
		[SaveableData, CopyableData]
		public bool checkOnly;

		[LoadingInitializer, DeepCopyImplementation]
		public VarGateEntry() {
		}
	}

	/// <summary>The Time Seal's state for one realm: when the seal ends, how many have gone through.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class VarGateSeal {
		[SaveableData, CopyableData]
		public int realm;

		[SaveableData, CopyableData]
		public double until;

		[SaveableData, CopyableData]
		public int through;

		[LoadingInitializer, DeepCopyImplementation]
		public VarGateSeal() {
		}
	}

	/// <summary>f_playerTime: a player who has had what this gate gives, until when.</summary>
	[SaveableClass, DeepCopyableClass]
	public sealed class VarGateRecord {
		[SaveableData, CopyableData]
		public Character player;

		[SaveableData, CopyableData]
		public bool isExp;

		[SaveableData, CopyableData]
		public double until;

		[LoadingInitializer, DeepCopyImplementation]
		public VarGateRecord() {
		}
	}

	/// <summary>#920 - Erebor's i_varGate (systems/variableGate.scp). Its @Step, in its order: first
	/// everything that can stop the walker, then everything that takes from them or gives to them.</summary>
	[ViewableClass]
	public partial class VarGate {
		private static readonly TagKey spawnedCountTK = TagKey.Acquire("spawnedCount");
		private static readonly int[] stepX = { 0, 1, 1, 1, 0, -1, -1, -1 };
		private static readonly int[] stepY = { -1, -1, 0, 1, 1, 1, 0, -1 };
		private static readonly HashSet<string> pvpToolTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"t_container", "t_allpotions", "t_bandage", "t_jewelry", "t_bandage_blood", "t_water_wash", "t_bottle_empty", "t_speedpotions", "t_potion_bomba"
		};

		internal VarGateFlags Flags {
			get {
				return (VarGateFlags) this.gateFlags;
			}
			set {
				this.gateFlags = (int) value;
			}
		}

		internal bool Has(VarGateFlags flag) {
			return (this.Flags & flag) != 0;
		}

		internal List<VarGateEntry> Removals {
			get {
				return this.removals ?? (this.removals = new List<VarGateEntry>());
			}
		}

		internal List<VarGateEntry> Additions {
			get {
				return this.additions ?? (this.additions = new List<VarGateEntry>());
			}
		}

		internal List<VarGateEntry> Functions {
			get {
				return this.functions ?? (this.functions = new List<VarGateEntry>());
			}
		}

		internal List<Thing> Guards {
			get {
				return this.guards ?? (this.guards = new List<Thing>());
			}
		}

		public bool GoHome {
			get {
				return this.goHome;
			}
			set {
				this.goHome = value;
			}
		}

		public override void On_Create() {
			base.On_Create();
			this.SetAttribute(ItemAttributes.attr_move_never, true); //not the flag alone: this also stops its decay
		}

		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if ((player != null) && player.IsGM) {
				D_VarGate.Show(player, this);
			}
		}

		public override void On_Step(AbstractCharacter stepping, bool repeated, MovementType movementType) {
			base.On_Step(stepping, repeated, movementType);
			Character ch = stepping as Character;
			if ((ch == null) || repeated) {
				return;
			}
			if (ch.IsGM) {
				this.PassGM(ch);
			} else if (!this.goHome && !this.Has(VarGateFlags.RecallPort) && !this.HasTarget) {
				Logger.WriteWarning("VarGate " + this.FlaggedUid + " at " + this.P() + " has no target.");
				ch.SysMessage(Word(ch, "Zhoupl", "Zhoupla") + " jsi se v magickem viru, ale nic se nestalo");
			} else if (this.CanPass(ch)) {
				this.TakeFrom(ch);
				this.GiveTo(ch);
				this.Send(ch);
			}
		}

		private bool HasTarget {
			get {
				Point4D target = this.MoreP;
				return (target != null) && ((target.X != 0) || (target.Y != 0));
			}
		}

		private static string Word(Character ch, string male, string female) {
			return ch.IsFemale ? female : male;
		}

		private void PassGM(Character ch) {
			if (this.goHome) {
				GoHomeOf(ch);
			} else if (this.Has(VarGateFlags.RecallPort)) {
				ch.SysMessage("GM: Gate nastavena na RECALL runu!");
			} else if (this.HasTarget) {
				ch.Go(100, 100);
				ch.Go(this.MoreP);
			}
		}

		private static void GoHomeOf(Character ch) {
			Point4D home = (ch as Player)?.Home;
			if (home != null) {
				ch.Go(home);
			}
		}

		/// <summary>src.move: back to the tile they came from.</summary>
		private static void Bounce(Character ch) {
			int back = ((int) ch.Direction + 4) % 8;
			ch.Go(ch.X + stepX[back], ch.Y + stepY[back]);
		}

		private static int LevelOf(Character ch) {
			return (ch as Player)?.Level ?? 0;
		}

		private bool CanPass(Character ch) {
			if (this.Has(VarGateFlags.Guarded) && this.IsAGuardAlive) {
				ch.SysMessage("Neprojdes, dokud je strazce pruchodu na zivu.");
				return false;
			}
			if (!ch.IsPlayer && !this.Has(VarGateFlags.NpcGoThrough)) {
				return false;
			}
			if (ch.Flag_Dead && !this.Has(VarGateFlags.GhostGoThrough)) {
				ch.RedMessage("Musis byt nazivu.");
				return false;
			}
			if (this.Has(VarGateFlags.RequiresLevel) && !this.HasRequiredLevel(ch)) {
				return false;
			}
			if ((this.Has(VarGateFlags.RemoveItems) && !this.PassesRemovals(ch)) ||
					(this.Has(VarGateFlags.AddItems) && !this.PassesAdditions(ch)) ||
					(this.Has(VarGateFlags.RunFunctions) && !this.PassesFunctions(ch)) ||
					(this.Has(VarGateFlags.HasPlugins) && this.hasTimeSeal && !this.PassesTimeSeal(ch))) {
				Bounce(ch);
				return false;
			}
			return true;
		}

		private bool IsAGuardAlive {
			get {
				foreach (Thing gem in this.Guards) {
					if ((gem != null) && !gem.IsDeleted && (TagMath.IGetTag(gem, spawnedCountTK) > 0)) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>f_vg_gotReqLevel: between two levels, or above one. A maximum under the minimum is
		/// no maximum.</summary>
		private bool HasRequiredLevel(Character ch) {
			int level = LevelOf(ch);
			if (this.reqMinLevel <= this.reqMaxLevel) {
				if ((level >= this.reqMinLevel) && (level <= this.reqMaxLevel)) {
					return true;
				}
				ch.RedMessage("Brana te odmita.");
				return false;
			}
			if (level > this.reqMinLevel) {
				return true;
			}
			ch.RedMessage("Nemas dostatek sily projit touto branou.");
			return false;
		}

		/// <summary>All one carries, in the bags too, but not in the bank.</summary>
		internal static List<Item> Carried(Thing holder) {
			List<Item> found = new List<Item>();
			Collect(holder, found);
			return found;
		}

		private static void Collect(Thing holder, List<Item> found) {
			foreach (AbstractItem abstractItem in holder) {
				Item item = abstractItem as Item;
				if ((item != null) && !item.IsInLimbo && !(item is BankBox)) {
					found.Add(item);
					if (item is Container) {
						Collect(item, found);
					}
				}
			}
		}

		internal static string TypeOf(Item item) {
			TriggerGroup type = (item.Def as AbstractItemDef)?.Type;
			return (type != null) ? type.Defname : "";
		}

		/// <summary>By the def, as a def's name needn't be the one it's asked for by: gold is [ItemDef 0eed] + DEFNAME.</summary>
		internal static bool Matches(Item item, string name, bool byType) {
			if (byType) {
				return string.Equals(TypeOf(item), name, StringComparison.OrdinalIgnoreCase);
			}
			return item.Def == ThingDef.GetByDefname(name);
		}

		internal static int CountOf(Character ch, string name, bool byType) {
			int count = 0;
			foreach (Item item in Carried(ch)) {
				if (Matches(item, name, byType)) {
					count += item.Amount;
				}
			}
			return count;
		}

		private static bool HasEvent(Character ch, string name) {
			TriggerGroup group = TriggerGroup.GetByDefname(name);
			return (group != null) && ch.HasTriggerGroup(group);
		}

		private bool PassesRemovals(Character ch) {
			foreach (VarGateEntry entry in this.Removals) {
				if (entry.optional) {
					continue;
				}
				if (entry.kind == VarGateEntry.RemoveEvent) {
					if (!HasEvent(ch, entry.name)) {
						ch.RedMessage("Nejsi " + Word(ch, "hoden", "hodna") + " projit dal.");
						return false;
					}
				} else {
					int count = CountOf(ch, entry.name, entry.kind == VarGateEntry.RemoveType);
					if (entry.checkOnly ? (count > entry.amount) : (count < entry.amount)) {
						ch.RedMessage("Brana te odmita prijmout.");
						return false;
					}
				}
			}
			return true;
		}

		private bool PassesAdditions(Character ch) {
			foreach (VarGateEntry entry in this.Additions) {
				if (entry.checkOnly) {
					bool has = (entry.kind == VarGateEntry.AddEvent) ? HasEvent(ch, entry.name) : (CountOf(ch, entry.name, false) >= entry.amount);
					if (!has) {
						ch.RedMessage("Brana te odmita prijmout.");
						return false;
					}
				}
			}
			return true;
		}

		private object RunFunction(Character ch, VarGateEntry entry) {
			ScriptHolder function = ScriptHolder.GetFunction(entry.name);
			if (function == null) {
				Logger.WriteWarning("VarGate " + this.FlaggedUid + ": no function " + entry.name);
				return null;
			}
			return function.TryRun(ch, this);
		}

		private bool PassesFunctions(Character ch) {
			foreach (VarGateEntry entry in this.Functions) {
				if (entry.checkOnly) {
					object result = this.RunFunction(ch, entry);
					if ((result != null) && ConvertTools.ToBoolean(result)) {
						return false;
					}
				}
			}
			return true;
		}

		private VarGateSeal SealOf(int realm) {
			if (this.seals == null) {
				this.seals = new List<VarGateSeal>();
			}
			foreach (VarGateSeal seal in this.seals) {
				if (seal.realm == realm) {
					return seal;
				}
			}
			VarGateSeal added = new VarGateSeal();
			added.realm = realm;
			this.seals.Add(added);
			return added;
		}

		/// <summary>f_vg_timeSeal_Conditions. The first to pass seals the gate for delay + length seconds:
		/// during the delay others follow, up to maxPlayers, then it's shut for the length. Each realm has
		/// its own seal, unless same realm is on: then only the first one's realm passes.</summary>
		private bool PassesTimeSeal(Character ch) {
			double now = Globals.TimeInSeconds;
			int realm = (int) ch.Realm;
			if (this.sealSameRealm) {
				if (now > this.SealOf(this.sealFirstRealm).until) {
					return true;
				}
				if (realm != this.sealFirstRealm) {
					ch.SysMessage("Brana te odmita!");
					return false;
				}
			} else if (now > this.SealOf(realm).until) {
				return true;
			}
			VarGateSeal seal = this.SealOf(realm);
			if ((now < (seal.until - this.sealLength)) && ((this.sealMaxPlayers == 0) || (this.sealMaxPlayers > seal.through))) {
				return true;
			}
			ch.SysMessage("Brana te odmita!");
			return false;
		}

		private void PassedTimeSeal(Character ch) {
			double now = Globals.TimeInSeconds;
			int realm = (int) ch.Realm;
			VarGateSeal seal = this.SealOf(realm);
			if (now > seal.until) {
				seal.through = 1;
				seal.until = now + this.sealDelay + this.sealLength;
				if (this.sealSameRealm) {
					this.sealFirstRealm = realm;
				}
			} else {
				seal.through++;
			}
		}

		internal void ResetTimeSeal() {
			this.seals = null;
		}

		private void TakeFrom(Character ch) {
			if (this.Has(VarGateFlags.RemoveCursor)) {
				ch.FindLayer(LayerNames.Dragging)?.Delete();
			}
			if (this.Has(VarGateFlags.RemovePvpTools)) {
				RemovePvpTools(ch);
			}
			if (this.Has(VarGateFlags.RemoveMount)) {
				Character mount = ch.Mount as Character;
				if (mount != null) {
					ch.Dismount();
					mount.Delete();
				}
			}
			if (this.Has(VarGateFlags.RemoveBank)) {
				foreach (Item item in new List<AbstractItem>(ch.Bank).ConvertAll(i => (Item) i)) {
					item.Delete();
				}
			}
			if (this.Has(VarGateFlags.RemoveItems)) {
				foreach (VarGateEntry entry in this.Removals) {
					if (!entry.checkOnly) {
						this.Remove(ch, entry);
					}
				}
			}
		}

		/// <summary>f_smazat_murdoc_pvp, only what's in the pack: weapons and armor stay, and what's worn.</summary>
		private static void RemovePvpTools(Character ch) {
			Container pack = ch.Backpack;
			foreach (Item item in Carried(pack)) {
				if (!item.IsDeleted && (pvpToolTypes.Contains(TypeOf(item)) || (item is Container) || Matches(item, "i_robe", false) || Matches(item, "i_cape", false))) {
					item.Delete();
				}
			}
		}

		/// <summary>An amount of 0 takes all of it.</summary>
		private void Remove(Character ch, VarGateEntry entry) {
			if (entry.kind == VarGateEntry.RemoveEvent) {
				TriggerGroup group = TriggerGroup.GetByDefname(entry.name);
				if (group != null) {
					ch.RemoveTriggerGroup(group);
				}
				return;
			}
			long left = entry.amount;
			foreach (Item item in Carried(ch)) {
				if (!item.IsDeleted && Matches(item, entry.name, entry.kind == VarGateEntry.RemoveType)) {
					if (entry.amount == 0) {
						item.Delete();
					} else if (left > 0) {
						left -= item.Consume(left);
					}
				}
			}
		}

		private void GiveTo(Character ch) {
			if (this.Has(VarGateFlags.RunFunctions)) {
				foreach (VarGateEntry entry in this.Functions) {
					if (!entry.checkOnly) {
						this.RunFunction(ch, entry);
					}
				}
			}
			if (this.Has(VarGateFlags.AddItems)) {
				this.AddItems(ch);
			}
			if (this.Has(VarGateFlags.HasPlugins) && this.hasTimeSeal) {
				this.PassedTimeSeal(ch);
			}
			if (this.Has(VarGateFlags.ExpGate) && (ch is Player player)) {
				this.GiveExperience(player);
			}
		}

		/// <summary>f_playerTime_hasRecord: whether a record of this player is still running. Ended ones go.</summary>
		private bool HasRecord(Character ch, bool isExp) {
			if (this.records == null) {
				return false;
			}
			double now = Globals.TimeInSeconds;
			this.records.RemoveAll(record => (record.until < now) || (record.player == null) || record.player.IsDeleted);
			return this.records.Exists(record => (record.player == ch) && (record.isExp == isExp));
		}

		private void AddRecord(Character ch, bool isExp, double seconds) {
			if (this.records == null) {
				this.records = new List<VarGateRecord>();
			}
			VarGateRecord record = new VarGateRecord();
			record.player = ch;
			record.isExp = isExp;
			record.until = Globals.TimeInSeconds + seconds;
			this.records.Add(record);
		}

		private void AddItems(Character ch) {
			if (this.Has(VarGateFlags.AddItemTimeLimit)) {
				if (this.HasRecord(ch, false)) {
					return;
				}
				this.AddRecord(ch, false, this.addItemSeconds);
			}
			foreach (VarGateEntry entry in this.Additions) {
				if (entry.checkOnly) {
					continue;
				}
				if (entry.kind == VarGateEntry.AddEvent) {
					TriggerGroup group = TriggerGroup.GetByDefname(entry.name);
					if (group != null) {
						ch.AddTriggerGroup(group);
					}
				} else {
					IThingFactory def = ThingDef.GetByDefname(entry.name) as IThingFactory;
					if (def != null) {
						ch.Backpack.NewItem(def, entry.amount);
					}
				}
			}
		}

		/// <summary>f_varGate_lvlToGetExp: all of it between the levels, a part of it within the decay
		/// ranges under and over them, nothing further out. No maximum is no limit at all.</summary>
		internal int ExperienceFor(Player player) {
			if (this.expMaxLevel == 0) {
				return this.expAmount;
			}
			int level = player.Level;
			if (level < (this.expMinLevel - this.expDecayLow)) {
				player.SysMessage("Jsi prilis " + Word(player, "nezkuseny", "nezkusena") + " na to, aby jsi si z poslednich udalosti neco " + Word(player, "odnesl", "odnesla") + ".");
				return 0;
			}
			if (level < this.expMinLevel) {
				int low = this.expDecayLow + 1;
				return (this.expAmount / low) * (low - (this.expMinLevel - level));
			}
			if (level <= this.expMaxLevel) {
				return this.expAmount;
			}
			if (level <= (this.expMaxLevel + this.expDecayHigh)) {
				int high = this.expDecayHigh + 1;
				return (this.expAmount / high) * (high - (level - this.expMaxLevel));
			}
			player.SysMessage("Tady te jiz nic noveho nepotka. Posledni udalosti pro tebe byly pouhou rutinou.");
			return 0;
		}

		private void GiveExperience(Player player) {
			int experience = this.ExperienceFor(player);
			if (experience == 0) {
				return;
			}
			if (this.expMinutes > 0) {
				if (this.HasRecord(player, true)) {
					player.SysMessage("Jeste nemuzes dostat experience.");
					return;
				}
				this.AddRecord(player, true, this.expMinutes * 60);
			}
			LevelingSystem.AddExperience(player, experience);
		}

		private void Send(Character ch) {
			if (this.goHome) {
				GoHomeOf(ch);
			} else if (this.Has(VarGateFlags.RecallPort)) {
				Item rune = Carried(ch).Find(item => (item is RecallRune) || Matches(item, "i_rune_base", false));
				if ((rune == null) || (rune.MoreP == null) || (rune.MoreP.X == 0)) {
					GoHomeOf(ch);
				} else {
					ch.Go(rune.MoreP);
				}
			} else {
				if (this.Has(VarGateFlags.Sideways)) { //a client forgets the items of the old mapplane on the way through 10,10
					ch.Go(this.MoreP);
					ch.Go(10, 10);
				}
				ch.Go(this.MoreP);
			}
		}
	}

	/// <summary>#920 - the gate functions of Erebor's that have what they need here. A gate runs them on
	/// the walker; true stops them.</summary>
	public static class VarGateFunctions {
		[SteamFunction]
		public static bool f_vg_bookDeny(Character self) {
			if (self.IsGM) {
				return false;
			}
			foreach (Item item in VarGate.Carried(self)) {
				if (VarGate.Matches(item, "t_pergamen_forma", true) || VarGate.Matches(item, "t_book", true) ||
						VarGate.Matches(item, "i_pergamen", false) || VarGate.Matches(item, "i_pergamen_esence", false)) {
					self.SysMessage("Nesmis projit, pokud neses knihu,pergamen nebo nejakou jeho cast.");
					return true;
				}
			}
			return false;
		}

		[SteamFunction]
		public static bool f_vg_mountDeny(Character self) {
			if ((self.Mount != null) && !self.IsGM) {
				self.SysMessage("V sedle se sem nedostanes");
				return true;
			}
			return false;
		}
	}
}
