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
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1280 - one of Barad-dur's rival captains: who he is and his band (a Reputation faction).</summary>
	public sealed class Captain {
		public readonly string Key, Name, Faction, Def;

		internal Captain(string key, string name, string faction, string def) {
			this.Key = key;
			this.Name = name;
			this.Faction = faction;
			this.Def = def;
		}
	}

	/// <summary>#1280 - a captain's standing in the camp, saved: his power (1-5), this week's done orders, wins and losses.</summary>
	[SaveableClass, DeepCopyableClass]
	public class CaptainState {
		[SaveableData, CopyableData]
		public string key = "";

		[SaveableData, CopyableData]
		public int power = 2;

		[SaveableData, CopyableData]
		public int points;

		[SaveableData, CopyableData]
		public int wins;

		[SaveableData, CopyableData]
		public int losses;

		[LoadingInitializer, DeepCopyImplementation]
		public CaptainState() {
		}
	}

	/// <summary>#1280 - an order a captain gave, what he wants and by when.</summary>
	public enum CaptainOrderKind {
		Heads, Tribute, Iron
	}

	/// <summary>
	/// #1280 - Gorbag and Shagrat, Barad-dur's rival captains (design section 9.4, "nemesis lite"). A Mordor hero serves one; serving
	/// raises his band and lowers the rival's (Reputation's faction rivalry). The captain gives his own orders, each with a deadline:
	/// heads of Gondor's people, a tribute, iron for the forges. Done orders earn him points, and each week the captains settle it:
	/// the one with more rises a rank, the other falls, and Mordor's chat hears of it. A captain's man taking an order may draw the
	/// rival's cutthroats; if they kill him, their captain rises and taunts him next time.
	/// </summary>
	[HasSavedMembers]
	public static class RivalCaptains {
		public static readonly Captain[] All = {
			new Captain("gorbag", "Gorbag", "bd_gorbag", "c_warcamp_gorbag"),
			new Captain("shagrat", "Shagrat", "bd_shagrat", "c_warcamp_shagrat")
		};

		public const string CutthroatDef = "c_warcamp_cutthroat", IronDef = "i_ingot_iron";
		public const int MinPower = 1, MaxPower = 5;

		[SavedMember]
		private static List<CaptainState> states = new List<CaptainState>();

		[SavedMember]
		private static DateTime weekStart;

		internal static readonly TagKey SideTK = TagKey.Acquire("_campCaptain_");
		internal static readonly TagKey BetrayedTK = TagKey.Acquire("_campBetrayed_");
		internal static readonly TagKey OrderTK = TagKey.Acquire("_campOrder_");
		internal static readonly TagKey NemesisTK = TagKey.Acquire("_campNemesis_");
		internal static readonly TagKey NemesisCountTK = TagKey.Acquire("_campNemesisCount_");
		internal static readonly TagKey CutthroatOfTK = TagKey.Acquire("_cutthroatOf_");
		internal static readonly TagKey CutthroatsFromTK = TagKey.Acquire("_cutthroatsFrom_");
		internal static readonly TagKey CutthroatTargetTK = TagKey.Acquire("_cutthroatTarget_");
		internal static readonly TagKey CutthroatUntilTK = TagKey.Acquire("_cutthroatUntil_");
		internal static readonly TagKey LastOrderEndTK = TagKey.Acquire("_campOrderEnd_");
		private const int HuntRange = 24;

		/// <summary>For a test: the clock the captains go by.</summary>
		internal static DateTime? clock;

		public static DateTime Now {
			get { return clock ?? DateTime.UtcNow; }
		}

		internal static WarCampSettings Settings {
			get { return WarCampSettings.instance; }
		}

		public static Captain Get(string key) {
			foreach (Captain captain in All) {
				if (captain.Key == key) {
					return captain;
				}
			}
			return null;
		}

		public static Captain Of(Thing npc) {
			foreach (Captain captain in All) {
				if ((npc != null) && (npc.Def == ThingDef.GetByDefname(captain.Def))) {
					return captain;
				}
			}
			return null;
		}

		public static Captain RivalOf(Captain captain) {
			return (captain == All[0]) ? All[1] : All[0];
		}

		public static CaptainState State(Captain captain) {
			CaptainState state = states.Find(s => s.key == captain.Key);
			if (state == null) {
				state = new CaptainState { key = captain.Key };
				states.Add(state);
			}
			return state;
		}

		public static string Title(int power) {
			switch (Math.Max(MinPower, Math.Min(MaxPower, power))) {
				case 1: return "pobocnik";
				case 2: return "kapitan";
				case 3: return "velitel";
				case 4: return "nacelnik";
				default: return "valecny pan";
			}
		}

		public static string TitledName(Captain captain) {
			return captain.Name + ", " + Title(State(captain).power);
		}

		#region the player's side
		public static Captain SideOf(Player player) {
			return Get(player.GetTag(SideTK) as string);
		}

		/// <summary>"Povest" line's end in the journal: whom the player serves in the camp.</summary>
		internal static string ServingLine(Player player, string city) {
			Captain side = WarCamp.IsCamp(city) ? SideOf(player) : null;
			return (side == null) ? "" : ", slouzis: " + side.Name;
		}

		/// <summary>The player serves this captain now. The first time, his band and the camp rise (the rival's band falls by half);
		/// a switch costs instead: the old band and the camp fall (the new band gains half), an order from the old one fails, and the
		/// old one remembers. So going back and forth only ever loses standing.</summary>
		internal static void Serve(Player player, Captain captain) {
			Captain old = SideOf(player);
			if (old == captain) {
				return;
			}
			CaptainOrder order = PeekOrder(player);
			if (order != null) {
				Fail(player, order, "Rozkaz od " + order.Captain.Name + " propada.");
			}
			player.SetTag(SideTK, captain.Key);
			if (old != null) {
				player.SetTag(BetrayedTK, old.Key);
				player.RedMessage(old.Name + " ti to nezapomene.");
				Reputation.Change(player, WarCamp.City, -Settings.serveFavour, old.Faction);
			} else {
				Reputation.Change(player, WarCamp.City, Settings.serveFavour, captain.Faction);
			}
		}
		#endregion the player's side

		#region orders
		/// <summary>The player's order as saved, run out or not.</summary>
		internal static CaptainOrder PeekOrder(Player player) {
			return CaptainOrder.Parse(player.GetTag(OrderTK) as string);
		}

		/// <summary>The player's running order; one that ran out fails here, with its loss.</summary>
		internal static CaptainOrder CurrentOrder(Player player) {
			CaptainOrder order = PeekOrder(player);
			if ((order != null) && (order.Expires <= Now)) {
				Fail(player, order, "Rozkaz od " + order.Captain.Name + " vyprsel.");
				return null;
			}
			return order;
		}

		/// <summary>Minutes before the captain gives this player another order: a pause after each order done or failed.</summary>
		internal static int CooldownLeft(Player player) {
			object end = player.GetTag(LastOrderEndTK);
			if (end == null) {
				return 0;
			}
			double left = (new DateTime(Convert.ToInt64(end, CultureInfo.InvariantCulture)).AddMinutes(Settings.captainOrderCooldownMinutes) - Now).TotalMinutes;
			return (left <= 0) ? 0 : (int) Math.Ceiling(left);
		}

		/// <summary>A new order from the captain, sized by his power; the rival may send cutthroats.</summary>
		internal static CaptainOrder Give(Player player, Captain captain, int roll) {
			int power = State(captain).power;
			CaptainOrder order = new CaptainOrder { CaptainKey = captain.Key, ExpiresTicks = Now.AddMinutes(Settings.captainOrderMinutes).Ticks };
			if (roll < 20) {
				order.Kind = CaptainOrderKind.Heads;
				order.Count = (power >= 4) ? 2 : 1;
			} else if (roll < 60) {
				order.Kind = CaptainOrderKind.Tribute;
				order.Count = Settings.tributePerPower * power;
			} else {
				order.Kind = CaptainOrderKind.Iron;
				order.Count = Settings.ironBase + (Settings.ironPerPower * power);
			}
			player.SetTag(OrderTK, order.ToString());
			player.SysMessage("Novy rozkaz od " + captain.Name + ": " + order.Task + " Mas na to " + Settings.captainOrderMinutes + " minut.");
			if (Globals.dice.Next(100) < Settings.cutthroatChance) {
				SendCutthroats(player, RivalOf(captain), Settings.cutthroatDelaySeconds);
			}
			return order;
		}

		/// <summary>Heads of Gondor's people in the pack: a Gondor player's, not the hero's own.</summary>
		internal static List<Item> GondorHeads(Player player) {
			List<Item> heads = new List<Item>();
			if (player.Backpack != null) {
				CollectHeads(player.Backpack, player, heads);
			}
			return heads;
		}

		private static void CollectHeads(Container container, Player player, List<Item> heads) {
			foreach (AbstractItem item in container) {
				if ((item is Item head) && CorpseCarving.IsHead(head)) {
					Character of = CorpseCarving.HeadOf(head);
					if ((of != null) && (of != player) && ((of.Realm == Realm.Gondor) || (of.Realm == Realm.GondorOutcast))) {
						heads.Add(head);
					}
				} else if ((item is Container inside) && (inside.Count > 0)) {
					CollectHeads(inside, player, heads);
				}
			}
		}

		internal static bool CanFinish(Player player, CaptainOrder order) {
			switch (order.Kind) {
				case CaptainOrderKind.Heads:
					return GondorHeads(player).Count >= order.Count;
				case CaptainOrderKind.Tribute:
					return GoldUtils.CanPay(player, order.Count);
				default:
					ItemDef iron = ThingDef.GetByDefname(IronDef) as ItemDef;
					return (iron != null) && (player.Backpack != null) && (Errands.CountIn(player.Backpack, iron) >= order.Count);
			}
		}

		/// <summary>The order handed in: what it wants is taken, the captain's band and the camp think better of the hero, the
		/// captain earns a point and pays (a tribute isn't paid back in gold).</summary>
		internal static bool Finish(Player player, CaptainOrder order) {
			if (!CanFinish(player, order)) {
				return false;
			}
			switch (order.Kind) {
				case CaptainOrderKind.Heads:
					List<Item> heads = GondorHeads(player);
					for (int i = 0; i < order.Count; i++) {
						heads[i].Delete();
					}
					break;
				case CaptainOrderKind.Tribute:
					if (!GoldUtils.Pay(player, order.Count)) {
						return false;
					}
					break;
				default:
					Errands.Consume(player.Backpack, (ItemDef) ThingDef.GetByDefname(IronDef), order.Count);
					break;
			}
			player.RemoveTag(OrderTK);
			player.SetTag(LastOrderEndTK, Now.Ticks);
			Captain captain = order.Captain;
			CaptainState state = State(captain);
			state.points++;
			int gold = (order.Kind == CaptainOrderKind.Tribute) ? 0 : Settings.captainOrderGold * state.power;
			int exp = Settings.captainOrderExp * state.power;
			if (gold > 0) {
				GoldUtils.Grant(player, (uint) gold);
			}
			LevelingSystem.AddExperience(player, exp);
			Reputation.Change(player, WarCamp.City, Settings.captainOrderFavour, captain.Faction);
			player.SysMessage("Rozkaz splnen. " + captain.Name + " je spokojen.", ClassAbilities.ClassMessageHue);
			return true;
		}

		/// <summary>The order given up or run out: the captain's band and the camp think worse of the hero.</summary>
		internal static void Fail(Player player, CaptainOrder order, string message) {
			player.RemoveTag(OrderTK);
			player.SetTag(LastOrderEndTK, Now.Ticks);
			Captain captain = order.Captain;
			if (captain != null) {
				Reputation.Change(player, WarCamp.City, -Settings.captainOrderFavour, captain.Faction);
			}
			player.RedMessage(message);
		}
		#endregion orders

		#region the weekly settlement
		/// <summary>Each week the captains settle it: more done orders wins, the winner rises a rank and the loser falls; a tie
		/// changes nothing. Points start again either way. Worked out when anyone talks to a captain.</summary>
		internal static void Settle() {
			if (weekStart == default(DateTime)) {
				weekStart = Now;
				return;
			}
			if ((Now - weekStart).TotalDays < Settings.challengeDays) {
				return;
			}
			CaptainState a = State(All[0]), b = State(All[1]);
			if (a.points != b.points) {
				Captain winner = (a.points > b.points) ? All[0] : All[1];
				Captain loser = RivalOf(winner);
				CaptainState won = State(winner), lost = State(loser);
				won.power = Math.Min(MaxPower, won.power + 1);
				won.wins++;
				lost.power = Math.Max(MinPower, lost.power - 1);
				lost.losses++;
				ChatSystem.Announce(ChatChannel.Mordor, WarCamp.City, winner.Name + " porazil " + loser.Name + "a v souboji o Sauronovu prizen. "
					+ winner.Name + " je nyni " + Title(won.power) + ", " + loser.Name + " jen " + Title(lost.power) + ".");
			}
			a.points = 0;
			b.points = 0;
			weekStart = Now;
		}

		/// <summary>For a test or a GM: the week's start, so a settlement can be due.</summary>
		internal static void SetWeekStart(DateTime start) {
			weekStart = start;
		}
		#endregion the weekly settlement

		#region cutthroats and the nemesis
		/// <summary>The rival's cutthroats come for the hero after a delay.</summary>
		internal static void SendCutthroats(Player player, Captain from, int delaySeconds) {
			player.SetTag(CutthroatsFromTK, from.Key);
			CutthroatTimer timer = player.GetTimer(CutthroatTimer.Key) as CutthroatTimer;
			if (timer == null) {
				timer = new CutthroatTimer();
				player.AddTimer(CutthroatTimer.Key, timer);
			}
			timer.DueInSeconds = Math.Max(0, delaySeconds);
		}

		/// <summary>The cutthroats appear beside a hero still in Mordor's service, online and alive, and go for them; they leave
		/// after a while. Returns how many came.</summary>
		internal static List<NPC> Ambush(Player player) {
			List<NPC> came = new List<NPC>();
			Captain from = Get(player.GetTag(CutthroatsFromTK) as string);
			player.RemoveTag(CutthroatsFromTK);
			if ((from == null) || player.Flag_Disconnected || player.Flag_Dead || player.IsDeleted || (player.Realm != Realm.Mordor)
					|| player.Flag_Invulnerable || Jail.IsJailed(player) || Arena.IsIn(player) || (SideOf(player) == from)) {
				return came; //nobody hunts a jailed, invulnerable or arena hero, nor one who serves the sender now
			}
			NPCDef def = ThingDef.GetByDefname(CutthroatDef) as NPCDef;
			if (def == null) {
				return came;
			}
			Map map = player.GetMap();
			for (int i = 0; i < Settings.cutthroatCount; i++) {
				int[] at = WorldSpots.NearestStandable(map, player.X + ((i % 2 == 0) ? 3 : -3), player.Y + i, 3);
				if (at == null) {
					continue;
				}
				NPC cutthroat = (NPC) def.Create(at[0], at[1], at[2], player.M);
				cutthroat.Name = "Hrdlorez " + from.Name + "uv";
				cutthroat.SetTag(CutthroatOfTK, from.Key);
				cutthroat.SetTag(CutthroatTargetTK, player);
				cutthroat.SetTag(CutthroatUntilTK, Globals.TimeInSeconds + Settings.cutthroatStaySeconds);
				CutthroatLeaveTimer leave = new CutthroatLeaveTimer();
				cutthroat.AddTimer(CutthroatLeaveTimer.Key, leave);
				leave.PeriodInSeconds = CutthroatLeaveTimer.CheckSeconds;
				leave.DueInSeconds = CutthroatLeaveTimer.CheckSeconds;
				NPCAiTimer.GetFor(cutthroat)?.EngageTarget(player);
				came.Add(cutthroat);
			}
			if (came.Count > 0) {
				player.RedMessage(from.Name + " posila sve hrdlorezy!");
			}
			return came;
		}

		/// <summary>A cutthroat calm towards everyone else (they are AggroMode Calm) leaves when its hero is dead, gone, offline,
		/// on another map or far away, or its time is up.</summary>
		internal static bool ShouldLeave(NPC cutthroat) {
			Player target = cutthroat.GetTag(CutthroatTargetTK) as Player;
			object until = cutthroat.GetTag(CutthroatUntilTK);
			return (target == null) || target.IsDeleted || target.Flag_Dead || target.Flag_Disconnected || (target.M != cutthroat.M)
				|| (Point2D.GetSimpleDistance(target, cutthroat) > HuntRange) || (until == null) || (Globals.TimeInSeconds >= Convert.ToDouble(until, CultureInfo.InvariantCulture));
		}

		/// <summary>The cutthroats hunting this hero leave: their work is done.</summary>
		internal static void Dismiss(Player player) {
			List<Character> leaving = new List<Character>();
			foreach (Character ch in player.GetMap().GetCharsInRange(player.X, player.Y, HuntRange)) {
				if ((ch.GetTag(CutthroatOfTK) != null) && (ch.GetTag(CutthroatTargetTK) == player)) {
					leaving.Add(ch);
				}
			}
			foreach (Character ch in leaving) {
				ch.Delete();
			}
		}

		/// <summary>A hero's death by the cutthroats sent after them: their captain rises a rank and remembers the hero, once an
		/// ambush, as they leave at once. A bystander's death counts nothing.</summary>
		internal static void OnPlayerDeath(Player player, Character killedBy) {
			if ((killedBy == null) || killedBy.IsDeleted || (killedBy.GetTag(CutthroatTargetTK) != player)) {
				return;
			}
			Captain captain = Get(killedBy.GetTag(CutthroatOfTK) as string);
			if (captain == null) {
				return;
			}
			Dismiss(player);
			CaptainState state = State(captain);
			state.power = Math.Min(MaxPower, state.power + 1);
			player.SetTag(NemesisTK, captain.Key);
			player.SetTag(NemesisCountTK, Collectors.ToInt(player.GetTag(NemesisCountTK)) + 1);
			ChatSystem.Announce(ChatChannel.Mordor, WarCamp.City, "Hrdlorezove " + captain.Name + "a poslali " + player.Name + " do prachu. "
				+ captain.Name + " je nyni " + Title(state.power) + ".");
		}
		#endregion cutthroats and the nemesis

		#region talking
		/// <summary>A captain's double click: the week settles, only Mordor's own get a word.</summary>
		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			Settle();
			string refusal = Refusal(player);
			if (refusal != null) {
				npc.Say(refusal);
				return;
			}
			CurrentOrder(player); //a run-out order fails before the dialog shows
			npc.Dialog(player, SingletonScript<D_WarCaptain>.Instance, npc);
		}

		internal static string Refusal(Player player) {
			if ((player.Realm == Realm.Gondor) || (player.Realm == Realm.GondorOutcast)) {
				return "Gondorska krysa v tabore? Zmiz, nez te nechame stahnout z kuze!";
			}
			if (player.Realm != Realm.Mordor) {
				return "S vyvrheli a tulaky nemluvim.";
			}
			return null;
		}

		internal static string Greeting(Captain captain, Player player) {
			Captain side = SideOf(player);
			if ((player.GetTag(NemesisTK) as string) == captain.Key) {
				return "Zase ty, " + player.Name + "? Moji hrdlorezove te posledne nechali lezet v prachu. Rad to zopakuju!";
			}
			if (side == captain) {
				return "Muj vojak. Co tu postavas? Valka neceka.";
			}
			if ((player.GetTag(BetrayedTK) as string) == captain.Key) {
				return "Zradce! Ty sis vybral " + RivalOf(captain).Name + "a. To ti nezapomenu.";
			}
			if (side != null) {
				return "Zmiz mi z oci, " + side.Name + "uv psiku.";
			}
			return "Hledas pana, cerve? Slouzit mne znamena slavu a koist. A " + RivalOf(captain).Name + " je jen ubohy pes.";
		}
		#endregion talking
	}

	/// <summary>#1280 - a captain's order, kept on the player as one tag ("key|kind|count|expires").</summary>
	public sealed class CaptainOrder {
		public string CaptainKey;
		public CaptainOrderKind Kind;
		public int Count;
		public long ExpiresTicks;

		public Captain Captain {
			get { return RivalCaptains.Get(this.CaptainKey); }
		}

		public DateTime Expires {
			get { return new DateTime(this.ExpiresTicks); }
		}

		public string Task {
			get {
				switch (this.Kind) {
					case CaptainOrderKind.Heads:
						return "Prines " + ((this.Count == 1) ? "hlavu" : this.Count.ToString(CultureInfo.InvariantCulture) + " hlavy") + " nekoho z Gondoru.";
					case CaptainOrderKind.Tribute:
						return "Odved tlupe " + this.Count.ToString(CultureInfo.InvariantCulture) + " zlatych.";
					default:
						return "Prines kovarnam " + this.Count.ToString(CultureInfo.InvariantCulture) + " zeleznych ingotu.";
				}
			}
		}

		public override string ToString() {
			return this.CaptainKey + "|" + ((int) this.Kind).ToString(CultureInfo.InvariantCulture) + "|" + this.Count.ToString(CultureInfo.InvariantCulture)
				+ "|" + this.ExpiresTicks.ToString(CultureInfo.InvariantCulture);
		}

		internal static CaptainOrder Parse(string text) {
			string[] parts = (text == null) ? null : text.Split('|');
			if ((parts == null) || (parts.Length != 4) || (RivalCaptains.Get(parts[0]) == null)) {
				return null;
			}
			int kind, count;
			long expires;
			if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out kind)
					|| !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
					|| !long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out expires)
					|| !Enum.IsDefined(typeof(CaptainOrderKind), kind)) {
				return null; //a tag spoiled by hand is no order
			}
			return new CaptainOrder { CaptainKey = parts[0], Kind = (CaptainOrderKind) kind, Count = count, ExpiresTicks = expires };
		}
	}

	/// <summary>#1280 - the rival's cutthroats arrive.</summary>
	[SaveableClass, DeepCopyableClass]
	public class CutthroatTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_campCutthroats_");

		[LoadingInitializer, DeepCopyImplementation]
		public CutthroatTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Player player) {
				RivalCaptains.Ambush(player);
			}
		}
	}

	/// <summary>#1280 - every few seconds: a cutthroat whose hunt is over leaves.</summary>
	[SaveableClass, DeepCopyableClass]
	public class CutthroatLeaveTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_campCutthroatLeaves_");

		[LoadingInitializer, DeepCopyImplementation]
		public CutthroatLeaveTimer() {
		}

		internal const int CheckSeconds = 5;

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is NPC npc) && !npc.IsDeleted && RivalCaptains.ShouldLeave(npc)) {
				npc.Delete();
			}
		}
	}

	/// <summary>#1280 - a rival captain's dialog (one class for both: the captain comes from the NPC's def).</summary>
	public class D_WarCaptain : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3111;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			Captain captain = RivalCaptains.Of(npc);
			if (captain == null) {
				return;
			}
			Captain side = RivalCaptains.SideOf(player);
			CaptainState state = RivalCaptains.State(captain);
			d.BaseButton(1);
			if (side != captain) {
				d.BaseButton(2, 30, "Slouzit ti");
			} else {
				d.BaseButton(4, 40, "Rozkaz");
			}
			d.BaseButton(6);
			d.Page("Pozdrav");
			d.SetText(RivalCaptains.Greeting(captain, player));
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Jsem " + RivalCaptains.TitledName(captain) + " a vedu svou tlupu. Mam za sebou " + state.wins.ToString(CultureInfo.InvariantCulture)
				+ " vitezstvi a " + state.losses.ToString(CultureInfo.InvariantCulture) + " porazek. Tento tyden moji lide splnili "
				+ state.points.ToString(CultureInfo.InvariantCulture) + " rozkazu. " + RivalCaptains.RivalOf(captain).Name + " je jen "
				+ RivalCaptains.Title(RivalCaptains.State(RivalCaptains.RivalOf(captain)).power) + ".");
			d.Html();
			if (side != captain) {
				d.Page("Slouzit ti");
				d.SetText("Chces slouzit me? " + RivalCaptains.RivalOf(captain).Name + " te pak bude nenavidet. A ja nemam rad, kdyz me nekdo zklame.");
				d.Html();
				d.SetText("Budu slouzit tobe.");
				d.Response(false, true, "captain_serve");
			} else {
				d.Page("Rozkaz");
				CaptainOrder order = RivalCaptains.PeekOrder(player);
				int wait = RivalCaptains.CooldownLeft(player);
				if (((order == null) || (order.Expires <= RivalCaptains.Now)) && (wait > 0)) {
					d.SetText("Ted pro tebe nic nemam. Vrat se za " + wait.ToString(CultureInfo.InvariantCulture) + " minut.");
					d.Html();
				} else if ((order == null) || (order.Expires <= RivalCaptains.Now)) {
					d.SetText("Mam pro tebe rozkaz. Splnis ho vcas, nebo se mi neukazuj.");
					d.Html();
					d.SetText("Rozumim.");
					d.Response(false, true, "captain_order");
				} else {
					int left = Math.Max(1, (int) Math.Ceiling((order.Expires - RivalCaptains.Now).TotalMinutes));
					d.SetText(order.Task + " Zbyva " + left.ToString(CultureInfo.InvariantCulture) + " minut.");
					d.Html();
					if (RivalCaptains.CanFinish(player, order)) {
						d.SetText("Splneno.");
						d.Response(false, true, "captain_done");
					}
					d.SetText("Vzdavam to.");
					d.Response(false, true, "captain_giveup");
				}
			}
			d.Page("Prohlednout");
			d.SetText("<def_questDialog_colorTextEmote>*Mohutny skret v cerne zbroji s rudym okem na stitu. Kolem neho postavaji jeho nejvernejsi.*");
			d.Html();
		}

		protected override void OnButton(string button, Character npc, Player player) {
			Captain captain = RivalCaptains.Of(npc);
			if ((captain == null) || (RivalCaptains.Refusal(player) != null)) {
				return;
			}
			bool his = RivalCaptains.SideOf(player) == captain;
			switch (button) {
				case "captain_serve":
					if (!his) {
						RivalCaptains.Serve(player, captain);
						npc.Say("Dobre. Ted patris me. Nezklam me.");
					}
					break;
				case "captain_order":
					if (his && (RivalCaptains.CurrentOrder(player) == null) && (RivalCaptains.CooldownLeft(player) == 0)) {
						RivalCaptains.Give(player, captain, Globals.dice.Next(100));
						npc.Say("Hni sebou!");
					}
					break;
				case "captain_done":
					CaptainOrder order = his ? RivalCaptains.CurrentOrder(player) : null;
					if ((order != null) && (order.Captain == captain)) {
						if (RivalCaptains.Finish(player, order)) {
							npc.Say("Dobra prace. Takove chci videt vic.");
						} else {
							npc.Say("Tohle ze ma byt splneny rozkaz? Nedelej si ze me blazny.");
						}
					}
					break;
				case "captain_giveup":
					CaptainOrder given = his ? RivalCaptains.CurrentOrder(player) : null;
					if ((given != null) && (given.Captain == captain)) {
						RivalCaptains.Fail(player, given, "Vzdal jsi rozkaz od " + captain.Name + ".");
						npc.Say("Slaboch. Priste se snaz vic.");
					}
					break;
			}
		}
	}

	/// <summary>#1280 - a captain's double click.</summary>
	public class E_WarCaptain : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				RivalCaptains.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1280 - attaches the captains' double click to their defs.</summary>
	public static class WarCampInit {
		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			foreach (Captain captain in RivalCaptains.All) {
				QuestNpc.Attach(captain.Def, SingletonScript<E_WarCaptain>.Instance);
			}
		}
	}
}
