using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>A guard paid for: Erebor's i_relating_crystal. It stands once a post is chosen.</summary>
	[SaveableClass]
	public class HiredGuard {
		[LoadingInitializer]
		public HiredGuard() {
		}

		[SaveableData]
		public string defname;

		[SaveableData]
		public string name;

		/// <summary>more2, in Globals.TimeInSeconds.</summary>
		[SaveableData]
		public double paidUntil;

		[SaveableData]
		public NPC npc;

		[SaveableData]
		public bool hasPost;

		[SaveableData]
		public int x;

		[SaveableData]
		public int y;

		[SaveableData]
		public int z;

		[SaveableData]
		public byte m;

		/// <summary>When a dead guard's replacement comes.</summary>
		[SaveableData]
		public double respawnAt;

		public bool IsStanding {
			get { return (this.npc != null) && !this.npc.IsDeleted && !this.npc.Flag_Dead; }
		}
	}

	/// <summary>#892 - hired guards: of a town (systems/starostove.scp) or of the realm, the king's and
	/// Marghul's (generals.scp, lordnazguls.scp), which on Erebor were the same code four times.</summary>
	[SaveableClass]
	[ViewableClass]
	public class GuardForce {
		private const double SecondsInDay = 24 * 60 * 60;
		private static readonly TimerKey checkTK = TimerKey.Acquire("_guardForceCheck_");

		[LoadingInitializer]
		public GuardForce() {
		}

		/// <summary>The city its posts are in, and whose table of guards it hires from.</summary>
		[SaveableData]
		public string city;

		[SaveableData]
		public bool isKings;

		[SaveableData]
		public Town town;

		/// <summary>The money of a realm's force; a town's is its treasury.</summary>
		[SaveableData]
		public long funds;

		[SaveableData]
		public List<HiredGuard> guards = new List<HiredGuard>();

		/// <summary>#1116 - a keep's force (Erebor's captain stone): the keep's fort ("Gondor_1"). Its guards are free and
		/// never run out, its posts follow the keep rule, its table is Guard_king_&lt;keep&gt;_n.</summary>
		[SaveableData]
		public string keepFort;

		[SaveableData]
		public Thing keepStone;

		public bool IsKeep {
			get { return this.keepFort != null; }
		}

		public long Treasury {
			get { return (this.town != null) ? this.town.treasury : this.funds; }
			set {
				if (this.town != null) {
					this.town.treasury = value;
				} else {
					this.funds = value;
				}
			}
		}

		public string CityName {
			get { return this.city.Replace('_', ' '); }
		}

		/// <summary>What seznam_guards.scp lets this city hire.</summary>
		public List<string> HireableDefnames() {
			List<string> found = new List<string>();
			string prefix = (this.isKings ? "Guard_king_" : "Guard_") + this.city + "_";
			for (int i = 1; ; i++) {
				Constant constant = Constant.GetByName(prefix + i);
				ThingDef def = (constant != null) ? (constant.Value as ThingDef) : null;
				if (def == null) {
					return found;
				}
				found.Add(def.PrettyDefname);
			}
		}

		/// <summary>#1116 - a keep's guards are free (Erebor's "neplacenych").</summary>
		public int Price(string defname) {
			return this.IsKeep ? 0 : PriceOf(defname);
		}

		public static int PriceOf(string defname) {
			object price = ThingDef.GetByDefname(defname).GetTag(TagKey.Acquire("guardhire"));
			return (price != null) ? Convert.ToInt32(price, CultureInfo.InvariantCulture) : 0;
		}

		public bool TryCharge(Character by, int price) {
			if (this.Treasury < price) {
				by.SysMessage("V pokladne neni dost penez");
				return false;
			}
			this.Treasury -= price;
			return true;
		}

		/// <summary>max_guards_&lt;keep&gt;.</summary>
		public int KeepLimit {
			get {
				Constant limit = Constant.GetByName("max_guards_" + this.city);
				return (limit == null) ? 0 : Convert.ToInt32(limit.Value, CultureInfo.InvariantCulture);
			}
		}

		public void Hire(Character by, string defname) {
			Constant limit = this.isKings ? Constant.GetByName("max_guards_" + this.city) : null;
			if ((limit != null) && (this.guards.Count >= Convert.ToInt32(limit.Value, CultureInfo.InvariantCulture))) {
				by.SysMessage(this.IsKeep ? "Prilis mnoho najatych neplacenych strazi" : "Vic kralovskych strazi zde nelze najmout");
			} else if (this.TryCharge(by, this.Price(defname))) {
				this.guards.Add(new HiredGuard {
					defname = defname,
					name = ThingDef.GetByDefname(defname).Name,
					paidUntil = this.IsKeep ? double.MaxValue : (Globals.TimeInSeconds + (TownSettings.instance.guardPayDays * SecondsInDay))
				});
			}
		}

		public void Extend(Character by, HiredGuard guard) {
			if (this.IsKeep) {
				return;
			}
			if (this.TryCharge(by, PriceOf(guard.defname))) {
				guard.paidUntil += TownSettings.instance.guardPayDays * SecondsInDay;
			}
		}

		public void Dismiss(HiredGuard guard) {
			if (guard.IsStanding) {
				guard.npc.Delete();
			}
			this.guards.Remove(guard);
		}

		/// <summary>i_guard_setp: within the city, and not on top of another post. #1116 - a keep's post: on the keep's own
		/// battlefield, within ConquestSettings.keepGuardReach of its captain stone, keepGuardSpacing from the others.</summary>
		public bool TryPost(Player by, HiredGuard guard, IPoint3D at) {
			Map map = by.GetMap();
			FlaggedRegion area = RegionEventSystem.AreaOf(map.GetRegionFor(new Point2D(at.X, at.Y)));
			if (this.IsKeep) {
				if ((this.keepStone != null) && ((this.keepStone.M != by.M) || (Point2D.GetSimpleDistance(this.keepStone.X, this.keepStone.Y, at.X, at.Y) > ConquestSettings.instance.keepGuardReach))) {
					by.RedMessage("Pevnostniho strazce nelze umistit tak daleko od pevnosti");
					return false;
				}
				if ((area == null) || (area.Battlefield == 0) || (Conquest.FortName(area.Battlefield) != this.keepFort)) {
					by.RedMessage("Tam nemuzes strazce umistit");
					return false;
				}
			} else if ((area == null) || !StringComparer.OrdinalIgnoreCase.Equals(area.City, this.city)) {
				by.RedMessage("Tam nemuzes strazce umistit");
				return false;
			}
			int spacing = this.IsKeep ? ConquestSettings.instance.keepGuardSpacing : TownSettings.instance.guardPostDistance;
			foreach (HiredGuard other in this.guards) {
				if ((other != guard) && other.hasPost && (other.m == by.M) &&
						(Point2D.GetSimpleDistance(other.x, other.y, at.X, at.Y) <= spacing)) {
					by.SysMessage("Nemuzes umistit guarda tak blizko jineho");
					return false;
				}
			}
			guard.x = at.X;
			guard.y = at.Y;
			guard.z = at.Z;
			guard.m = by.M;
			guard.hasPost = true;
			if (guard.IsStanding) {
				guard.npc.Delete();
			}
			this.Spawn(guard);
			return true;
		}

		/// <summary>#1116 - aretheremoreguards: is a guard of the keep standing? (Erebor never looked at the last one hired.)</summary>
		public bool AnyStanding {
			get { return this.guards.Exists(g => g.IsStanding); }
		}

		/// <summary>#1116 - respawnguards, at a war's start: every posted guard that's down stands up at once.</summary>
		public void RespawnAll() {
			foreach (HiredGuard guard in this.guards) {
				if (guard.hasPost && !guard.IsStanding) {
					this.Spawn(guard);
				}
			}
		}

		/// <summary>#1116 - swapGuards, on a capture: each guard becomes its def's guardopposite, standing ones are replaced at
		/// their posts. A guard with no opposite is dismissed (Erebor dropped it from the hire table).</summary>
		public void SwapToOpposite() {
			foreach (HiredGuard guard in new List<HiredGuard>(this.guards)) {
				string opposite = OppositeOf(guard.defname);
				if (opposite == null) {
					this.Dismiss(guard);
					continue;
				}
				bool standing = guard.IsStanding;
				if (standing) {
					guard.npc.Delete();
				}
				guard.defname = opposite;
				guard.name = ThingDef.GetByDefname(opposite).Name;
				if (standing || guard.hasPost) {
					this.Spawn(guard);
				}
			}
		}

		/// <summary>The def's tag.guardopposite (a def read, as Erebor's findres(chardef, ...).tag(guardopposite)).</summary>
		public static string OppositeOf(string defname) {
			object opposite = ThingDef.GetByDefname(defname)?.GetTag(TagKey.Acquire("guardopposite"));
			string name = (opposite is ThingDef def) ? def.PrettyDefname : (opposite as string);
			return ((name != null) && (ThingDef.GetByDefname(name.Trim()) != null)) ? ThingDef.GetByDefname(name.Trim()).PrettyDefname : null;
		}

		private void Spawn(HiredGuard guard) {
			guard.npc = (NPC) ThingDef.GetByDefname(guard.defname).Create(guard.x, guard.y, guard.z, guard.m);
			guard.respawnAt = 0;
		}

		/// <summary>hasbeenpaid, and what Erebor's spawn gem did: unpaid guards go, dead ones come back.</summary>
		public void Check() {
			double now = Globals.TimeInSeconds;
			foreach (HiredGuard guard in new List<HiredGuard>(this.guards)) {
				if (guard.paidUntil < now) {
					this.Dismiss(guard);
				} else if (guard.hasPost && !guard.IsStanding) {
					if (guard.respawnAt == 0) {
						guard.respawnAt = now + (TownSettings.instance.guardRespawnMinutes * 60);
					} else if (now >= guard.respawnAt) {
						this.Spawn(guard);
					}
				}
			}
		}

		/// <summary>The stone carries the force's clock, so a save keeps it.</summary>
		public void EnsureCheckTimer(TagHolder stone) {
			if (stone.GetTimer(checkTK) == null) {
				GuardForceTimer timer = new GuardForceTimer { force = this };
				stone.AddTimer(checkTK, timer);
				timer.DueInSeconds = 60;
				timer.PeriodInSeconds = 60;
			}
		}

		public List<HiredGuard> StandingNear(Character ch, int range) {
			List<HiredGuard> near = new List<HiredGuard>();
			foreach (HiredGuard guard in this.guards) {
				if (guard.IsStanding && (guard.npc.M == ch.M) && (Point2D.GetSimpleDistance(guard.npc.X, guard.npc.Y, ch.X, ch.Y) <= range)) {
					near.Add(guard);
				}
			}
			return near;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class GuardForceTimer : BoundTimer {
		[SaveableData, CopyableData]
		public GuardForce force;

		[DeepCopyImplementation, LoadingInitializer]
		public GuardForceTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (this.force != null) {
				this.force.Check();
			}
		}
	}

	public enum GuardOrder {
		Kill, Stop, Continue, Flee, Come
	}

	/// <summary>The orders of d_town_captain and of Erebor's four sets of commands: kill and come only reach
	/// the guards near whoever orders them.</summary>
	public static class GuardOrders {
		private const int ComeRange = 15;

		private static NPCAiTimer Ai(HiredGuard guard) {
			return NPCAiTimer.GetFor(guard.npc);
		}

		public static void Give(Player player, GuardForce force, GuardOrder order) {
			if (player.Flag_Dead) {
				player.SysMessage("Ve svem stavu nejsi niceho schopen");
			} else if (force.guards.Count == 0) {
				player.SysMessage(force.IsKeep ? "Nemas k dispozici zadne straze" : "Mesto nevlastni zadne straze");
			} else if (order == GuardOrder.Kill) {
				player.Target(SingletonScript<Targ_GuardKill>.Instance, force);
			} else if (order == GuardOrder.Come) {
				foreach (HiredGuard guard in force.StandingNear(player, ComeRange)) {
					Release(guard);
					guard.npc.Go(player.P());
				}
			} else {
				foreach (HiredGuard guard in force.guards) {
					if (guard.IsStanding) {
						Obey(guard, order);
					}
				}
			}
		}

		private static void Release(HiredGuard guard) {
			NPCAiTimer ai = Ai(guard);
			if (ai != null) {
				ai.isHeld = false;
			}
		}

		private static void Obey(HiredGuard guard, GuardOrder order) {
			NPCAiTimer ai = Ai(guard);
			if (order == GuardOrder.Stop) {
				if (ai != null) {
					ai.isHeld = true;
				}
			} else {
				Release(guard);
				if (order == GuardOrder.Flee) {
					guard.npc.Go(new Point4D(guard.x, guard.y, guard.z, guard.m));
				}
			}
		}

		/// <summary>The guards of the city someone stands in, if they may order them: its mayor or Nazgul,
		/// the realm's General or First, or a GM.</summary>
		public static GuardForce TownForce(Player player) {
			FlaggedRegion area = RegionEventSystem.AreaOf(player.Region);
			Town town = ((area != null) && !string.IsNullOrEmpty(area.City)) ? Town.ForCity(area.City) : null;
			if (town == null) {
				player.SysMessage("Musis byt ve meste");
				return null;
			}
			RealmOffice office = RealmOffice.For(town.Realm);
			if (!player.IsGM && (town.mayor != player) && ((office == null) || (office.holder != player))) {
				player.SysMessage("Musis byt ve svem meste!");
				return null;
			}
			return town.Force;
		}

		public static GuardForce RealmForce(Player player) {
			RealmOffice office = RealmOffice.For(player.Realm);
			if ((office == null) || !office.CanEdit(player)) {
				return null;
			}
			return office.Force;
		}

		public static void Order(Character self, bool realm, GuardOrder order) {
			Player player = self as Player;
			GuardForce force = (player == null) ? null : (realm ? RealmForce(player) : TownForce(player));
			if (force != null) {
				Give(player, force, order);
			}
		}

		public static void Control(Character self, bool realm) {
			Player player = self as Player;
			GuardForce force = (player == null) ? null : (realm ? RealmForce(player) : TownForce(player));
			if (force != null) {
				D_GuardCaptain.Show(force, player);
			}
		}
	}

	/// <summary>Erebor's commands, all four sets: town, king's, Mordor town, Marghul's.</summary>
	public static class GuardCommands {
		[SteamFunction] public static void TownControl(Character self) { GuardOrders.Control(self, false); }
		[SteamFunction] public static void TownGuardsKill(Character self) { GuardOrders.Order(self, false, GuardOrder.Kill); }
		[SteamFunction] public static void TownGuardsStop(Character self) { GuardOrders.Order(self, false, GuardOrder.Stop); }
		[SteamFunction] public static void TownGuardsContinue(Character self) { GuardOrders.Order(self, false, GuardOrder.Continue); }
		[SteamFunction] public static void TownGuardsFlee(Character self) { GuardOrders.Order(self, false, GuardOrder.Flee); }
		[SteamFunction] public static void TownGuardsCome(Character self) { GuardOrders.Order(self, false, GuardOrder.Come); }
		[SteamFunction] public static void MordorTownControl(Character self) { GuardOrders.Control(self, false); }
		[SteamFunction] public static void MordorTownGuardsKill(Character self) { GuardOrders.Order(self, false, GuardOrder.Kill); }
		[SteamFunction] public static void MordorTownGuardsStop(Character self) { GuardOrders.Order(self, false, GuardOrder.Stop); }
		[SteamFunction] public static void MordorTownGuardsContinue(Character self) { GuardOrders.Order(self, false, GuardOrder.Continue); }
		[SteamFunction] public static void MordorTownGuardsFlee(Character self) { GuardOrders.Order(self, false, GuardOrder.Flee); }
		[SteamFunction] public static void MordorTownGuardsCome(Character self) { GuardOrders.Order(self, false, GuardOrder.Come); }
		[SteamFunction] public static void GuardsControl(Character self) { GuardOrders.Control(self, true); }
		[SteamFunction] public static void KingGuardsKill(Character self) { GuardOrders.Order(self, true, GuardOrder.Kill); }
		[SteamFunction] public static void KingGuardsStop(Character self) { GuardOrders.Order(self, true, GuardOrder.Stop); }
		[SteamFunction] public static void KingGuardsContinue(Character self) { GuardOrders.Order(self, true, GuardOrder.Continue); }
		[SteamFunction] public static void KingGuardsFlee(Character self) { GuardOrders.Order(self, true, GuardOrder.Flee); }
		[SteamFunction] public static void KingGuardsCome(Character self) { GuardOrders.Order(self, true, GuardOrder.Come); }
		[SteamFunction] public static void MordorGuardsControl(Character self) { GuardOrders.Control(self, true); }
		[SteamFunction] public static void MarghulsGuardsKill(Character self) { GuardOrders.Order(self, true, GuardOrder.Kill); }
		[SteamFunction] public static void MarghulsGuardsStop(Character self) { GuardOrders.Order(self, true, GuardOrder.Stop); }
		[SteamFunction] public static void MarghulsGuardsContinue(Character self) { GuardOrders.Order(self, true, GuardOrder.Continue); }
		[SteamFunction] public static void MarghulsGuardsFlee(Character self) { GuardOrders.Order(self, true, GuardOrder.Flee); }
		[SteamFunction] public static void MarghulsGuardsCome(Character self) { GuardOrders.Order(self, true, GuardOrder.Come); }
	}

	/// <summary>i_guard_targ</summary>
	public class Targ_GuardKill : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			GuardForce force = (GuardForce) parameter;
			Town town = Town.ForCity(force.city);
			if (targetted == self) {
				self.SysMessage("Proc povolavat straze sam na sebe, ze?");
			} else if (!targetted.IsPlayer) {
				self.SysMessage("Straze lze poslat jen na hrace a jejich summony");
			} else if ((self.Realm == Realm.Gondor) && !Notoriety.IsCriminal(targetted) && !Notoriety.IsMurderer(targetted) &&
					(targetted.Realm == Realm.Gondor) && ((town == null) || (town.FindBan(targetted) == null))) {
				self.SysMessage("Na poctive obyvatele gondoru nemuzes volat straze!");
			} else {
				int left = TownSettings.instance.guardsPerKill;
				foreach (HiredGuard guard in force.StandingNear(self, Globals.MaxUpdateRange * 2)) {
					NPCAiTimer ai = NPCAiTimer.GetFor(guard.npc);
					if ((ai != null) && (left > 0)) {
						ai.isHeld = false;
						ai.EngageTarget(targetted);
						left--;
					}
				}
			}
			return TargetResult.Done;
		}
	}

	public class Targ_GuardPost : CompiledTargetDef {
		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			object[] args = (object[]) parameter;
			GuardForce force = (GuardForce) args[0];
			if (force.TryPost(self, (HiredGuard) args[1], targetted)) {
				D_GuardPosts.Show(force, self);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>d_town_captain</summary>
	public class D_GuardCaptain : CompiledGumpDef {
		private static readonly string[] orders = { "Zautocit na cil", "Zastavit", "Pokracovat", "Domu", "Ke mne" };

		internal static void Show(GuardForce force, Player player) {
			player.Dialog(player, SingletonScript<D_GuardCaptain>.Instance, new DialogArgs(force));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(20, 20);
			this.AddPage(0);
			this.AddBackground(0, 0, 220, 150, 2620);
			this.AddLabel(14, 12, 0x481, "Rozkaz strazim:");
			this.AddButton(175, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			for (int i = 0; i < orders.Length; i++) {
				int y = 40 + (i * 20);
				this.AddButton(14, y, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, 955, orders[i]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			if ((gr.PressedButton >= 1) && (gr.PressedButton <= orders.Length)) {
				GuardOrders.Give(player, (GuardForce) args[0], (GuardOrder) (gr.PressedButton - 1));
			}
		}
	}

	/// <summary>d_town_major: where the guards stand.</summary>
	public class D_GuardPosts : CompiledGumpDef {
		private const int Width = 530, PageRows = 13, RowHeight = 20, FirstRow = 58, MoveButton = 1000, DismissButton = 2000, TextHue = 955;

		internal static void Show(GuardForce force, Player player) {
			player.Dialog(player, SingletonScript<D_GuardPosts>.Instance, new DialogArgs(force));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			GuardForce force = (GuardForce) args[0];
			this.SetLocation(20, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, FirstRow + (PageRows * RowHeight) + 20, 2620);
			this.AddLabel(14, 12, 0x481, "Seznam + rozmisteni strazi:");
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(14, 36, 0x481, "Zmen");
			this.AddLabel(54, 36, 0x481, "Zrus");
			this.AddLabel(94, 36, 0x481, "Typ");
			this.AddLabel(300, 36, 0x481, "Pozice");
			this.AddLabel(420, 36, 0x481, "Vysluzba");
			int pages = (force.guards.Count + PageRows - 1) / PageRows;
			for (int page = 0; page < pages; page++) {
				this.AddPage(page + 1);
				if (page > 0) {
					this.AddButton(Width - 25, FirstRow, 0xfa, 0xfb, 0, GumpButtonType.Page, page);
				}
				if (page < (pages - 1)) {
					this.AddButton(Width - 25, FirstRow + ((PageRows - 1) * RowHeight), 0xfc, 0xfd, 0, GumpButtonType.Page, page + 2);
				}
				int last = Math.Min(force.guards.Count, (page + 1) * PageRows);
				for (int i = page * PageRows; i < last; i++) {
					HiredGuard guard = force.guards[i];
					int y = FirstRow + ((i % PageRows) * RowHeight);
					this.AddButton(14, y, 4005, 4007, MoveButton + i, GumpButtonType.Reply, 0);
					this.AddButton(54, y, 4005, 4007, DismissButton + i, GumpButtonType.Reply, 0);
					this.AddLabel(94, y, TextHue, guard.name);
					this.AddLabel(300, y, TextHue, guard.hasPost ? (guard.x + "," + guard.y) : "-");
					this.AddLabel(420, y, TextHue, TownDialogs.TimeLeft(guard.paidUntil));
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			GuardForce force = (GuardForce) args[0];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			int index = button - ((button >= DismissButton) ? DismissButton : MoveButton);
			if ((button < MoveButton) || (index >= force.guards.Count)) {
				return;
			}
			HiredGuard guard = force.guards[index];
			if (button >= DismissButton) {
				D_TownConfirm.Ask(player, "Opravdu chces propustit strazce?", () => {
					force.Dismiss(guard);
					Show(force, player);
				});
			} else {
				player.SysMessage("Kam chces umistit strazce?");
				player.Target(SingletonScript<Targ_GuardPost>.Instance, new object[] { force, guard });
			}
		}
	}

	/// <summary>d_town_recruiter: money in, guards to hire, contracts to extend.</summary>
	public class D_GuardRecruiter : CompiledGumpDef {
		private const int Width = 455, RowHeight = 20, DepositButton = 5, HireButton = 1000, ExtendButton = 2000, DepositEntry = 1, Hue = 955;

		internal static void Show(GuardForce force, Player player) {
			player.Dialog(player, SingletonScript<D_GuardRecruiter>.Instance, new DialogArgs(force));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			GuardForce force = (GuardForce) args[0];
			List<string> hireable = force.HireableDefnames();
			int contractsTop = 64 + (hireable.Count * RowHeight) + 8;
			this.SetLocation(40, 140);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, contractsTop + ((force.guards.Count + 1) * RowHeight) + 16, 2620);
			this.AddLabel(14, 12, 0x481, force.IsKeep ? ("Mozno najmout: " + Math.Max(0, force.KeepLimit - force.guards.Count) + " neplacenych strazi") : ("Najimani - konto: " + force.Treasury + " gp"));
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			if (!force.IsKeep) {
				this.AddLabel(14, 36, 0x481, "Navysit o: ");
				this.AddTextEntry(94, 36, 150, 20, 0x481, DepositEntry, "0");
				this.AddButton(260, 36, 0xfb7, 0xfb9, DepositButton, GumpButtonType.Reply, 0);
			}
			for (int i = 0; i < hireable.Count; i++) {
				int y = 64 + (i * RowHeight);
				this.AddButton(14, y, 4005, 4007, HireButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, Hue, ThingDef.GetByDefname(hireable[i]).Name);
				this.AddLabel(340, y, Hue, force.Price(hireable[i]).ToString(CultureInfo.InvariantCulture));
			}
			for (int i = 0; i < force.guards.Count; i++) {
				int y = contractsTop + (i * RowHeight);
				HiredGuard guard = force.guards[i];
				this.AddButton(14, y, 4005, 4007, ExtendButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, Hue, guard.name);
				this.AddLabel(250, y, Hue, force.Price(guard.defname).ToString(CultureInfo.InvariantCulture));
				this.AddLabel(340, y, Hue, force.IsKeep ? "-" : TownDialogs.TimeLeft(guard.paidUntil));
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			GuardForce force = (GuardForce) args[0];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if (button == 0) {
				return;
			}
			if (button == DepositButton) {
				if (int.TryParse((gr.GetTextResponse(DepositEntry) ?? "").Trim(), out int amount) && (amount > 0) && GoldUtils.Pay(player, amount)) {
					force.Treasury += amount;
				}
			} else if (button >= ExtendButton) {
				if ((button - ExtendButton) < force.guards.Count) {
					force.Extend(player, force.guards[button - ExtendButton]);
				}
			} else {
				List<string> hireable = force.HireableDefnames();
				if ((button - HireButton) < hireable.Count) {
					force.Hire(player, hireable[button - HireButton]);
				}
			}
			Show(force, player);
		}
	}
}
