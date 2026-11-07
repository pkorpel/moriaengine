using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class CollectorDef {
	}

	/// <summary>#1247 - Erebor's t_collector (systems/uzitecne_nezaraditelne_itemy.scp): a bag that counts what its owner takes
	/// from bodies or from nature, with no items made. Aim it at a "Body of Grizzly" and the count goes up; quests read the count.
	/// Its settings are tags: the def's (the converter writes a bag's @create lines there), which an instance tag of the same
	/// name overrides, so an errand can make a bag of its own. The count lives on the bag, as `contentscount`.</summary>
	[ViewableClass]
	public partial class Collector {
		internal static readonly TagKey CountTK = TagKey.Acquire("contentscount");
		internal static readonly TagKey InfoTK = TagKey.Acquire("contentsinfo");
		internal static readonly TagKey TypeTK = TagKey.Acquire("contentstype");
		internal static readonly TagKey MaxTK = TagKey.Acquire("maxcontents");
		internal static readonly TagKey TargetDescTK = TagKey.Acquire("target_desc");
		internal static readonly TagKey TargetIdTK = TagKey.Acquire("target_id");
		internal static readonly TagKey TargetRemoveTK = TagKey.Acquire("target_remove");
		internal static readonly TagKey AbsoluteSuccessTK = TagKey.Acquire("absolute_success");
		internal static readonly TagKey ValueToCheckTK = TagKey.Acquire("value_to_check");
		internal static readonly TagKey FailMsgTK = TagKey.Acquire("fail_msg");
		internal static readonly TagKey RepeatedTK = TagKey.Acquire("repeatedcollecting");
		internal static readonly TagKey ColorTK = TagKey.Acquire("color");
		/// <summary>On a target: taken already, by anyone's bag (Erebor's tag.uncollectable).</summary>
		internal static readonly TagKey UncollectableTK = TagKey.Acquire("uncollectable");

		public int Collected {
			get { return this.Number(CountTK); }
			set { this.SetTag(CountTK, Math.Max(0, value)); }
		}

		/// <summary>0 = no limit.</summary>
		public int MaxCount {
			get { return this.Number(MaxTK); }
		}

		public bool IsFull {
			get { return (this.MaxCount > 0) && (this.Collected >= this.MaxCount); }
		}

		/// <summary>An instance tag, else the def's.</summary>
		internal object Setting(TagKey key) {
			return this.GetTag(key) ?? this.Def.GetTag(key);
		}

		internal string Text(TagKey key) {
			object value = this.Setting(key);
			return (value == null) ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		internal int Number(TagKey key) {
			object value = this.Setting(key);
			if (value == null) {
				return 0;
			}
			return Collectors.ToInt(value);
		}

		/// <summary>"Ve vacku je 3 stehen grizliho".</summary>
		public string Describe() {
			return this.Text(InfoTK) + " " + this.Collected.ToString(CultureInfo.InvariantCulture) + " " + this.Text(TypeTK);
		}

		//overhead from the bag, as its name is: TazUO sends a click on hover, and a journal line each time would flood it (#958)
		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			if (clicker is Character ch) {
				this.ClickMessage(ch, this.Describe());
			}
		}

		public override void On_AosClick(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_AosClick(clicker, clickerState, clickerConn);
			if (clicker is Character ch) {
				this.ClickMessage(ch, this.Describe());
			}
		}

		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if (player == null) {
				return;
			}
			if (player.Flag_Dead) {
				player.RedMessage("Takhle to nepujde");
				return;
			}
			if (this.IsFull) {
				player.SysMessage("Uz se sem vic nevejde.");
				return;
			}
			player.Target(SingletonScript<Targ_Collector>.Instance, this);
		}

		/// <summary>Erebor's @targon_char and @targon_item: what the bag takes from a target, or why not.</summary>
		internal void Collect(Player player, Thing target) {
			if (this.IsFull) { //filled while the cursor was up: Erebor took the body anyway, a bag that can't hold it doesn't here
				player.SysMessage("Uz se sem vic nevejde.");
				return;
			}
			Thing top = target.TopObj();
			if (!player.CanSeeLOS(top).Allow) {
				player.RedMessage("Tam nedosahnes!");
				return;
			}
			if (Collectors.IsTaken(target)) {
				if (target is Character) {
					player.RedMessage("Uz nelze ziskat vice");
				} else {
					player.SysMessage("Co jsi potreboval uz jsi ziskal");
				}
				return;
			}
			int color = this.Number(ColorTK);
			if ((target is Item) && (color != 0) && (target.Color != color)) { //phoenixes of one colour, say
				player.SysMessage("To neni to co bys potreboval");
				return;
			}
			if (target is Item item) {
				CollectionSource source = CollectionSource.Of(item);
				if (source != null) {
					this.CollectFromSource(player, item, source);
					return;
				}
				Corpse corpse = item as Corpse;
				if ((corpse != null) && (corpse.Owner != null) && corpse.Owner.IsPlayer) {
					this.Collected = 0;
					player.RedMessage("Znehodnotil jsi obsah lidskou krvi a musel jsi jej vyhodit");
					player.SysMessage(this.Describe());
					return;
				}
			}
			if (!string.Equals(target.Name, this.Text(TargetIdTK), StringComparison.OrdinalIgnoreCase)) {
				player.SysMessage("To neni to co bys potreboval");
				return;
			}
			bool taken = this.Roll();
			if (taken) {
				this.AddOne(player);
			} else {
				player.RedMessage(QuestDialogContent.ExpandMarkup(this.Text(FailMsgTK), player, "")); //<src.sex(poskodil,poskodila)>
			}
			this.UseUp(target, taken);
			if (taken) {
				player.SysMessage(this.Describe());
			}
		}

		//Erebor: value_to_check >= rand(0..absolute_success)
		internal bool Roll() {
			return this.Number(ValueToCheckTK) >= Globals.dice.Next(Math.Max(0, this.Number(AbsoluteSuccessTK)) + 1);
		}

		private void AddOne(Player player) {
			if (this.IsFull) {
				player.SysMessage("Uz se sem vic nevejde");
				return;
			}
			this.Collected++;
		}

		//a target gone or marked as taken, whether the try worked or not, as Erebor's: one try a body. Repeated collecting
		//spares the mark, except on an item a try failed on (Erebor's item branch marks those always). A player is never
		//removed, only marked: Sphere's remove refuses a player.
		private void UseUp(Thing target, bool taken) {
			bool remove = (this.Number(TargetRemoveTK) == 1) && !((target is Character ch) && ch.IsPlayer);
			bool repeated = (this.Number(RepeatedTK) != 0) && (taken || (target is Character));
			if (remove) {
				target.Delete();
			} else if (!repeated) {
				target.SetTag(UncollectableTK, 1);
			}
		}

		private void CollectFromSource(Player player, Item item, CollectionSource source) {
			if (!string.Equals(source.CollectorId, this.Def.PrettyDefname, StringComparison.OrdinalIgnoreCase)) {
				player.RedMessage("Nemas spravny predmet abys odsud neco mohl ziskat");
				return;
			}
			if (Globals.TimeInSeconds < source.NextPick) {
				player.RedMessage("Musis jeste chvili pockat nez z toho neco ziskas");
				return;
			}
			if (!source.Infinite && (source.ItemsLeft < 1)) {
				player.RedMessage("Zde jiz neni nic co bys mohl nejak vyuzit");
				return;
			}
			if (!this.Roll()) {
				player.RedMessage(QuestDialogContent.ExpandMarkup(this.Text(FailMsgTK), player, ""));
				return;
			}
			this.AddOne(player);
			if (!source.Infinite) {
				source.ItemsLeft--;
			}
			source.NextPick = Globals.TimeInSeconds + source.PickDelay;
			player.SysMessage(this.Describe());
		}
	}

	/// <summary>Erebor's t_collection: something a collector picks from, set by tags on the item (collector_id: the bag's def,
	/// pick_delay: seconds between picks, items_to_pick: how many in all, infinite: 1 for no end).</summary>
	public sealed class CollectionSource {
		internal static readonly TagKey CollectorIdTK = TagKey.Acquire("collector_id");
		internal static readonly TagKey PickDelayTK = TagKey.Acquire("pick_delay");
		internal static readonly TagKey ItemsToPickTK = TagKey.Acquire("items_to_pick");
		internal static readonly TagKey InfiniteTK = TagKey.Acquire("infinite");
		internal static readonly TagKey NextPickTK = TagKey.Acquire("next_pick");

		private readonly Item item;

		private CollectionSource(Item item) {
			this.item = item;
		}

		public static CollectionSource Of(Item item) {
			object id = item.GetTag(CollectorIdTK) ?? item.Def.GetTag(CollectorIdTK);
			return (id == null) ? null : new CollectionSource(item);
		}

		private object Get(TagKey key) {
			return this.item.GetTag(key) ?? this.item.Def.GetTag(key);
		}

		private int Int(TagKey key) {
			return Collectors.ToInt(this.Get(key));
		}

		public string CollectorId {
			get { return Convert.ToString(this.Get(CollectorIdTK), CultureInfo.InvariantCulture); }
		}

		public bool Infinite {
			get { return this.Int(InfiniteTK) == 1; }
		}

		public int PickDelay {
			get { return this.Int(PickDelayTK); }
		}

		public int ItemsLeft {
			get { return this.Int(ItemsToPickTK); }
			set { this.item.SetTag(ItemsToPickTK, value); }
		}

		public double NextPick {
			get {
				object value = this.item.GetTag(NextPickTK);
				try {
					return (value == null) ? 0 : Convert.ToDouble(value, CultureInfo.InvariantCulture);
				} catch (Exception e) when ((e is FormatException) || (e is InvalidCastException) || (e is OverflowException)) {
					return 0;
				}
			}
			set { this.item.SetTag(NextPickTK, value); }
		}
	}

	/// <summary>The cursor a collector gives: Erebor's target(tag.target_desc).</summary>
	public class Targ_Collector : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			Collector bag = parameter as Collector;
			if (bag != null) {
				string desc = bag.Text(Collector.TargetDescTK);
				if (desc.Length > 0) {
					self.SysMessage(desc);
				}
			}
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			Collector bag = parameter as Collector;
			if ((bag != null) && !bag.IsDeleted && (targetted != null)) {
				bag.Collect(self, targetted);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>What quests and errands ask of collectors (Erebor's f_collector_isFull, findid(...).tag.contentscount).</summary>
	public static class Collectors {
		/// <summary>The first bag of def `defname` the player has, in the pack (and its bags) or the bank, or null (Sphere's
		/// findid walks all a character holds).</summary>
		public static Collector FindIn(Character player, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			if (def == null) {
				return null;
			}
			Collector bag = (player.Backpack == null) ? null : Find(player.Backpack, def);
			return bag ?? ((player.Bank == null) ? null : Find(player.Bank, def));
		}

		/// <summary>A tag's number, 0 for anything that isn't one (a GM can set any tag to anything).</summary>
		internal static int ToInt(object value) {
			if (value == null) {
				return 0;
			}
			try {
				return Convert.ToInt32(value, CultureInfo.InvariantCulture);
			} catch (Exception e) when ((e is FormatException) || (e is InvalidCastException) || (e is OverflowException)) {
				return 0;
			}
		}

		private static Collector Find(Container container, ThingDef def) {
			foreach (AbstractItem item in container) {
				if ((item is Collector bag) && (bag.Def == def)) {
					return bag;
				}
				if ((item is Container inside) && (inside.Count > 0)) {
					Collector inner = Find(inside, def);
					if (inner != null) {
						return inner;
					}
				}
			}
			return null;
		}

		/// <summary>Erebor's f_collector_isFull: true when the player has such a bag and it's full.</summary>
		public static bool IsFull(Character player, string defname) {
			Collector bag = FindIn(player, defname);
			return (bag != null) && bag.IsFull;
		}

		public static bool IsTaken(Thing target) {
			return ToInt(target.GetTag(Collector.UncollectableTK)) == 1;
		}
	}
}
