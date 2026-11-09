using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Networking;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#892 - a realm's highest office and the realm's blacklist, which Erebor kept in a chest of
	/// crystals (var.realmbancont). Saved with the stone, as a town is.</summary>
	[SaveableClass]
	[ViewableClass]
	public class RealmOffice {
		private static readonly List<RealmOffice> all = new List<RealmOffice>();

		[SaveableData]
		public Realm realm;

		[SaveableData]
		public Item stone;

		[SaveableData]
		public Character holder;

		[SaveableData]
		public List<TownBan> bans = new List<TownBan>();

		[SaveableData]
		public GuardForce force;

		/// <summary>The king's or Marghul's guards, of the capital.</summary>
		public GuardForce Force {
			get {
				if (this.force == null) {
					this.force = new GuardForce { city = (this.realm == Realm.Gondor) ? TownSetup.GeneralCity : TownSetup.MordorCapital, isKings = true };
				}
				this.force.EnsureCheckTimer(this.stone);
				return this.force;
			}
		}

		[LoadingInitializer]
		public RealmOffice() {
			all.Add(this);
		}

		public static RealmOffice For(Realm realm) {
			foreach (RealmOffice office in all) {
				if (office.realm == realm) {
					return office;
				}
			}
			return null;
		}

		public static RealmOffice OfStone(Item stone) {
			foreach (RealmOffice office in all) {
				if (office.stone == stone) {
					return office;
				}
			}
			return null;
		}

		public static RealmOffice ForStone(Item stone, Realm realm) {
			RealmOffice office = For(realm) ?? new RealmOffice { realm = realm };
			office.stone = stone;
			return office;
		}

		public void Remove() {
			all.Remove(this);
		}

		public string RealmName {
			get { return (this.realm == Realm.Gondor) ? "Gondor" : "Mordor"; }
		}

		public TownBan FindBan(Character ch) {
			foreach (TownBan ban in this.bans) {
				if ((ban.who == ch) && (ban.name == ch.Name)) {
					return ban;
				}
			}
			return null;
		}

		/// <summary>A realm's guards know its blacklist, wherever they stand.</summary>
		public static bool IsBannedFor(NPC guard, Character ch) {
			RealmOffice office = For(guard.Realm);
			return (office != null) && (office.FindBan(ch) != null);
		}

		public bool CanEdit(Character ch) {
			return ch.IsGM || ((ch == this.holder) && (ch.Realm == this.realm));
		}

		/// <summary>i_targ_gen_make</summary>
		public void Appoint(Character by, Character ch) {
			if (this.realm == Realm.Mordor) {
				this.AppointFirst(by, ch);
			} else if (ch.Realm != this.realm) {
				by.SysMessage("Zameruj pouze hrace " + this.RealmName + "skeho realmu!");
			} else if (ch.IsGM) {
				by.SysMessage("GM nepotrebuji byt generalove");
			} else {
				this.Dismiss();
				KeepCaptains.DismissFrom(ch); //#1116 - a General is no captain
				this.holder = ch;
				PowerBadge.GiveTo(ch, "i_generals_bracelet");
				PowerBadge.SetTitle(ch, "General ");
				ch.SysMessage("Stal jsi se generalem Gondoru");
				by.SysMessage("Novy general uspesne jmenovan");
			}
		}

		/// <summary>i_targ_nazg_make: only a Nazgul can be the First.</summary>
		private void AppointFirst(Character by, Character ch) {
			if (ch.IsGM) {
				by.SysMessage("GM nepotrebuji byt nazgulove");
			} else if (ch.Realm != Realm.Mordor) {
				by.SysMessage("Zameruj pouze hrace Mordorskeho realmu!");
			} else if (!NazgulRings.IsNazgul(ch)) {
				by.SysMessage("Zvol pouze Nazgula!");
			} else {
				this.Dismiss();
				this.MakeFirst(ch);
				by.SysMessage("Novy prvni Nazgul uspesne jmenovan");
			}
		}

		/// <summary>The leading ring for the ring they had: their city is left without a Nazgul.</summary>
		public void MakeFirst(Character ch) {
			NazgulRings.TakeAway(NazgulRings.RingOf(ch), ch);
			KeepCaptains.DismissFrom(ch); //#1116 - nor is the First
			this.holder = ch;
			NazgulRings.Give(ch, null);
			NazgulGate.RefreshAll();
		}

		/// <summary>sesad_generala, sesad_lordnazgula</summary>
		public void Dismiss() {
			if ((this.realm == Realm.Mordor) && (this.holder != null) && !this.holder.IsDeleted) {
				PowerBadge ring = NazgulRings.RingOf(this.holder);
				if (ring != null) {
					this.holder.RedMessage("Marghul rozhodl o tvem sesazeni z funkce prvniho Nazgula");
					NazgulRings.TakeAway(ring, this.holder);
				}
				this.holder = null;
				return;
			}
			if ((this.holder != null) && !this.holder.IsDeleted) {
				PowerBadge.ClearTitle(this.holder);
				Uniform.Clear(this.holder); //#1307 - the office's look goes with it (Refresh does nothing once the badge is gone)
				PowerBadge badge = PowerBadge.Find(this.holder);
				if (badge != null) {
					badge.Delete();
				}
			}
			this.holder = null;
		}

		public override string ToString() {
			return "RealmOffice " + this.realm;
		}
	}

	[ViewableClass]
	public partial class RealmStoneDef {
	}

	/// <summary>i_generalstone and i_lordnazgulsstone: who holds the realm's office, and its blacklist.
	/// Which of the two goes by the realm of the map area it stands in.</summary>
	[ViewableClass]
	public partial class RealmStone {
		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if ((player == null) || MysticHypnosis.RefusesMenu(player)) { //#1070 - not while possessing
				return;
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(this.Region);
			if ((area == null) || (area.Realm == Realm.None)) {
				player.RedMessage("Realm nevhodny pro mesto, kontaktuj GM, kamen by zde nemel byt!");
			} else if (RegionEventSystem.AreaOf(player.Region) != area) {
				player.SysMessage("Musis byt v prostoru mesta!");
			} else {
				RealmOffice office = RealmOffice.ForStone(this, area.Realm);
				if (area.Realm == Realm.Gondor) {
					player.SysMessage((office.holder != null) ? ("Generalem je " + office.holder.Name) : "General Gondoru neni urcen");
				} else {
					player.SysMessage((office.holder != null) ? ("Nositelem prstenu je " + office.holder.Name) : "Funkce prvniho nazgula neni obsazena");
				}
				D_RealmBans.Show(office, player);
			}
		}

		public override void On_Destroy() {
			RealmOffice office = RealmOffice.OfStone(this);
			if (office != null) {
				office.Remove();
			}
			base.On_Destroy();
		}

		public override void Save(SaveStream output) {
			base.Save(output);
			RealmOffice office = RealmOffice.OfStone(this);
			if (office != null) {
				ObjectSaver.Save(office);
			}
		}
	}

	/// <summary>i_target_ban</summary>
	public class Targ_RealmBan : CompiledTargetDef, IRememberedTarget {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			RealmOffice office = (RealmOffice) parameter;
			if (targetted.IsGM) {
				self.SysMessage("GMka ne!");
			} else if (!self.IsGM && (targetted.Realm != self.Realm)) {
				self.SysMessage("Tohoto neni potreba blacklistovat");
			} else if (office.FindBan(targetted) != null) {
				self.SysMessage(targetted.Name + " uz jednou byl pridan na globalni blacklist");
			} else {
				D_TownInput.Ask(self, "Udej duvod pro blacklist:", "", reason => {
					office.bans.Add(new TownBan { who = targetted, name = Remember.NameFor(self, targetted), reason = reason }); //#905 realmban_him
					self.SysMessage(targetted.Name + " uspesne pridan na globalni cernou listinu");
					DelayedMessages.SendRed(targetted, "Byl jsi dan na globalni cernou listinu"); //#901
					D_RealmBans.Show(office, self);
				});
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return CorpseCarving.TargetPlayerByHead(self, targetted, "Zameruj lidi, nebo jejich hlavy", "To neni hlava hrace", owner => this.On_TargonChar(self, owner, parameter)); //#906
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("Zameruj lidi.");
			return TargetResult.Done;
		}
	}

	public class Targ_RealmAppoint : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			((RealmOffice) parameter).Appoint(self, targetted);
			return TargetResult.Done;
		}
	}

	/// <summary>d_general_executioner: the realm's blacklist for all to read. The office holder, on their
	/// own realm's stone, edits it, and a GM appoints and dismisses.</summary>
	public class D_RealmBans : CompiledGumpDef {
		private const int Width = 550, PageRows = 10, RowHeight = 20, FirstRow = 40, FirstRowButton = 100, AddBanButton = 3, OfficeButton = 4, TextHue = 2301,
			CaptainButton = 5, PostsButton = 6, RecruiterButton = 7;

		internal static void Show(RealmOffice office, Player player) {
			player.Dialog(player, SingletonScript<D_RealmBans>.Instance, new DialogArgs(office));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			RealmOffice office = (RealmOffice) args[0];
			Character viewer = (Character) sendTo;
			bool canEdit = office.CanEdit(viewer);
			int bottom = FirstRow + (PageRows * RowHeight);
			this.SetLocation(100, 100);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, bottom + 56, 2620);
			this.AddLabel(14, 12, 0x481, "Vyvrzencu z " + office.RealmName + "skeho realmu : " + office.bans.Count);
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			if (canEdit) {
				this.AddButton(14, bottom + 8, 4005, 4007, AddBanButton, GumpButtonType.Reply, 0);
				this.AddLabel(50, bottom + 8, 955, "Pridat hrace na blacklist");
			}
			if (viewer.IsGM) {
				this.AddButton(14, bottom + 28, 4005, 4007, OfficeButton, GumpButtonType.Reply, 0);
				this.AddLabel(50, bottom + 28, 955, (office.holder != null) ? "Sesadit z funkce" : "Uvest do funkce");
			}
			if (canEdit) {
				this.AddButton(280, bottom + 8, 4005, 4007, CaptainButton, GumpButtonType.Reply, 0);
				this.AddLabel(316, bottom + 8, 955, "Pokyny strazim");
				this.AddButton(280, bottom + 28, 4005, 4007, PostsButton, GumpButtonType.Reply, 0);
				this.AddLabel(316, bottom + 28, 955, "Rozmistit straze");
				this.AddButton(420, bottom + 8, 4005, 4007, RecruiterButton, GumpButtonType.Reply, 0);
				this.AddLabel(456, bottom + 8, 955, "Najimat straze");
			}
			int pages = (office.bans.Count + PageRows - 1) / PageRows;
			for (int page = 0; page < pages; page++) {
				this.AddPage(page + 1);
				if (page > 0) {
					this.AddButton(Width - 25, FirstRow, 0xfa, 0xfb, 0, GumpButtonType.Page, page);
				}
				if (page < (pages - 1)) {
					this.AddButton(Width - 25, FirstRow + ((PageRows - 1) * RowHeight), 0xfc, 0xfd, 0, GumpButtonType.Page, page + 2);
				}
				int last = Math.Min(office.bans.Count, (page + 1) * PageRows);
				for (int i = page * PageRows; i < last; i++) {
					int y = FirstRow + ((i % PageRows) * RowHeight);
					if (canEdit) {
						this.AddButton(14, y, 4005, 4007, FirstRowButton + i, GumpButtonType.Reply, 0);
					}
					this.AddLabel(50, y, TextHue, office.bans[i].name);
					this.AddLabel(200, y, TextHue, "Duvod: " + office.bans[i].reason);
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			RealmOffice office = (RealmOffice) args[0];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if ((button == CaptainButton) && office.CanEdit(player)) {
				D_GuardCaptain.Show(office.Force, player);
			} else if ((button == PostsButton) && office.CanEdit(player)) {
				D_GuardPosts.Show(office.Force, player);
			} else if ((button == RecruiterButton) && office.CanEdit(player)) {
				D_GuardRecruiter.Show(office.Force, player);
			} else if ((button == AddBanButton) && office.CanEdit(player)) {
				player.SysMessage("Koho chces pridat na blacklist?");
				player.Target(SingletonScript<Targ_RealmBan>.Instance, office);
			} else if ((button == OfficeButton) && player.IsGM) {
				if (office.holder != null) {
					D_TownConfirm.Ask(player, (office.realm == Realm.Gondor) ? "Opravdu si prejes sesadit z funkce generala Gondoru?"
						: "Opravdu si prejes sesadit z funkce prvniho Nazgula?", () => office.Dismiss());
				} else {
					player.SysMessage((office.realm == Realm.Gondor) ? "Zamer budouciho generala Gondoru" : "Zamer budouciho prvniho Nazgula");
					player.Target(SingletonScript<Targ_RealmAppoint>.Instance, office);
				}
			} else if ((button >= FirstRowButton) && office.CanEdit(player) && ((button - FirstRowButton) < office.bans.Count)) {
				TownBan ban = office.bans[button - FirstRowButton];
				office.bans.RemoveAt(button - FirstRowButton);
				DelayedMessages.Send(ban.who, ban.name + " byl odstranen z globalni cerne listiny"); //#901
				Show(office, player);
			}
		}
	}

	[ViewableClass]
	public partial class PowerBadgeDef {
	}

	/// <summary>i_generals_bracelet, and the Rings of Power: what marks an office, can't be taken off by
	/// hand, and asks for a uniform (Uniform).</summary>
	[ViewableClass]
	public partial class PowerBadge {
		private static readonly TagKey realNameTK = TagKey.Acquire("_realNameBeforeTitle_");

		public static PowerBadge Find(Character ch) {
			foreach (AbstractItem item in ch) {
				if (item is PowerBadge badge) {
					return badge;
				}
			}
			return null;
		}

		public static PowerBadge GiveTo(Character ch, string defname) {
			PowerBadge badge = (PowerBadge) ThingDef.GetByDefname(defname).Create(ch);
			badge.SetAttribute(ItemAttributes.attr_move_never, true);
			return badge;
		}

		/// <summary>#1037 - a ring of power or a bracelet stays on the body through death, the user's call:
		/// Sphere moves worn attr_move_never items to the pack. Only losing the office takes it off,
		/// and RingOf only looks at the body, so anywhere else it would stop counting.</summary>
		public override bool CanFallToCorpse {
			get { return false; }
		}

		/// <summary>#1037 - puts back on what a death moved to the pack before the rule above.</summary>
		public static void PutBackOn(Character ch) {
			AbstractItem pack = ch.Backpack;
			List<PowerBadge> inPack = new List<PowerBadge>();
			foreach (AbstractItem item in pack) {
				if (item is PowerBadge badge) {
					inPack.Add(badge);
				}
			}
			foreach (PowerBadge badge in inPack) {
				badge.Cont = ch;
			}
		}

		public static void SetTitle(Character ch, string title) {
			ch.SetTag(realNameTK, ch.Name);
			ch.Name = title + ch.Name;
		}

		public static void ClearTitle(Character ch) {
			string realName = ch.GetTag(realNameTK) as string;
			if (realName != null) {
				ch.Name = realName;
				ch.RemoveTag(realNameTK);
			}
		}
	}

	/// <summary>t_uniform: an office's clothes. Enough pieces on and they take its colors and looks; off,
	/// each goes back to what it was. An office ability asks for this.</summary>
	public static class Uniform {
		private static readonly TagKey storedColorTK = TagKey.Acquire("_uniStoreColor_");
		private static readonly TagKey storedModelTK = TagKey.Acquire("_uniStoreModel_");
		private static readonly TimerKey refreshTK = TimerKey.Acquire("_uniformRefresh_");
		private static readonly TagKey storedMountColorTK = TagKey.Acquire("_uniStoreMountColor_");
		internal static readonly TagKey MountItemTK = TagKey.Acquire("_uniMountItem_");
		private const int LayerMount = 25;
		internal const int UniformHorseItem = 0x3E9F; //#908 - i_mt_horse_tan, the look of every office's horse

		private sealed class Piece {
			internal int layer, color;
			internal string needsModel, makesModel;
		}

		private static Piece P(int layer, int color, string needsModel = null, string makesModel = null) {
			return new Piece { layer = layer, color = color, needsModel = needsModel, makesModel = makesModel };
		}

		private static readonly Piece[] generalPieces = {
			P(22, 0x481, "i_robe"), P(20, 0x481, "i_cape"), P(7, 0x481), P(3, 0x481, null, "i_boots_calf"), P(LayerMount, 0x481), P(6, 0x481, null, "i_hat_floppy")
		};

		private static readonly Piece[] ringPieces = {
			P(22, 1, "i_robe", "i_deathshroud"), P(20, 0x692, "i_cape"), P(7, 1), P(3, 1, null, "i_boots_calf"), P(LayerMount, 0x690)
		};

		private static readonly Piece[] leadingRingPieces = {
			P(22, 1, "i_robe", "i_deathshroud"), P(20, 0x689, "i_cape"), P(7, 1), P(3, 1, null, "i_boots_calf"), P(LayerMount, 0x690)
		};

		private static Piece[] PiecesOf(PowerBadge badge) {
			if (NazgulRings.RingOf((Character) badge.Cont) == null) {
				return generalPieces;
			}
			return NazgulRings.IsLeading(badge) ? leadingRingPieces : ringPieces;
		}

		/// <summary>The office is going: all it did to the clothes goes too.</summary>
		public static void Clear(Character ch) {
			PowerBadge badge = PowerBadge.Find(ch);
			if (badge != null) {
				foreach (Piece piece in PiecesOf(badge)) {
					AbstractItem item = (piece.layer != LayerMount) ? ch.FindLayer(piece.layer) : null;
					if (item != null) {
						Restore(item);
					}
				}
			}
			RestoreMount(ch, ch.Mount as Character);
		}

		/// <summary>#908 - the mount back to its own color and look (the uniform broke, the office went, the rider got off).</summary>
		public static void RestoreMount(Character rider, Character mount) {
			if ((mount == null) || ((mount.GetTag(storedMountColorTK) == null) && (mount.GetTag(MountItemTK) == null))) {
				return;
			}
			if ((rider != null) && (rider.Mount == mount)) {
				CharSyncQueue.AboutToChangeMount(rider);
			}
			object color = mount.GetTag(storedMountColorTK);
			if (color != null) {
				mount.Color = Convert.ToInt32(color);
				mount.RemoveTag(storedMountColorTK);
			}
			mount.RemoveTag(MountItemTK);
		}

		//Erebor's layer 25 uni_layer: the horse in the office's color, looking like i_mt_horse_tan
		private static void DressMount(Character rider, Character mount, int color) {
			CharSyncQueue.AboutToChangeMount(rider);
			mount.SetTag(storedMountColorTK, mount.Color);
			mount.Color = color;
			mount.SetTag(MountItemTK, UniformHorseItem);
		}

		private static bool IsWorn(Character ch, Piece piece) {
			if (piece.layer == LayerMount) {
				return ch.Mount != null;
			}
			AbstractItem item = ch.FindLayer(piece.layer);
			if (item == null) {
				return false;
			}
			return (piece.needsModel == null) || (StoredModelOf(item) == ((ItemDef) ThingDef.GetByDefname(piece.needsModel)).Model);
		}

		private static int StoredModelOf(AbstractItem item) {
			object stored = item.GetTag(storedModelTK);
			return (stored != null) ? Convert.ToInt32(stored) : item.Model;
		}

		/// <summary>tag.hasuniform; every piece counts as needed, as uni_needed says on all of Erebor's offices.</summary>
		public static bool Has(Character ch) {
			PowerBadge badge = PowerBadge.Find(ch);
			if (badge == null) {
				return false;
			}
			foreach (Piece piece in PiecesOf(badge)) {
				if (!IsWorn(ch, piece)) {
					return false;
				}
			}
			return true;
		}

		public static void Restore(AbstractItem item) {
			object color = item.GetTag(storedColorTK);
			if (color != null) {
				item.Color = Convert.ToInt32(color);
				item.RemoveTag(storedColorTK);
			}
			object model = item.GetTag(storedModelTK);
			if (model != null) {
				item.Model = Convert.ToInt32(model);
				item.RemoveTag(storedModelTK);
			}
		}

		/// <summary>After anything changed on the character: the clothes as they should now look.</summary>
		public static void Refresh(Character ch) {
			PowerBadge badge = PowerBadge.Find(ch);
			if (badge == null) {
				return;
			}
			bool complete = Has(ch);
			foreach (Piece piece in PiecesOf(badge)) {
				if ((piece.layer == LayerMount) && (ch.Mount is Character mount)) {
					RestoreMount(ch, mount);
					if (complete) {
						DressMount(ch, mount, piece.color);
					}
					continue;
				}
				AbstractItem item = (piece.layer != LayerMount) ? ch.FindLayer(piece.layer) : null;
				if (item == null) {
					continue;
				}
				Restore(item);
				if (complete) {
					if (piece.color != 0) {
						item.SetTag(storedColorTK, item.Color);
						item.Color = piece.color;
					}
					if (piece.makesModel != null) {
						item.SetTag(storedModelTK, (int) item.Model);
						item.Model = ((ItemDef) ThingDef.GetByDefname(piece.makesModel)).Model;
					}
				}
			}
		}

		/// <summary>i_uni_unequiptimer: a moment later, when the equipping is over.</summary>
		public static void RefreshSoon(Character ch) {
			if ((PowerBadge.Find(ch) != null) && (ch.GetTimer(refreshTK) == null)) {
				ch.AddTimer(refreshTK, new UniformRefreshTimer()).DueInSeconds = 0.1;
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class UniformRefreshTimer : BoundTimer {
		[DeepCopyImplementation, LoadingInitializer]
		public UniformRefreshTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Uniform.Refresh((Character) cont);
		}
	}

	/// <summary>What .honor and .fear leave on someone: i_honorcounter, i_fearcounter.</summary>
	[SaveableClass, DeepCopyableClass]
	public class RealmStatEffect : BoundTimer {
		/// <summary>#1307 - whether .honor or .fear is changing the character's stats right now.</summary>
		internal static bool IsOn(Character ch) {
			return (ch.GetTimer(RealmAbilities.HonorTK) != null) || (ch.GetTimer(RealmAbilities.FearTK) != null);
		}

		[SaveableData, CopyableData]
		public int amount;

		[DeepCopyImplementation, LoadingInitializer]
		public RealmStatEffect() {
		}

		public RealmStatEffect(int amount) {
			this.amount = amount;
		}

		internal static void Change(Character ch, int by) {
			ch.Str = (short) (ch.Str + by);
			ch.Dex = (short) (ch.Dex + by);
			ch.Int = (short) (ch.Int + by);
			if (ch is Player player) {
				player.Vit = (short) (player.Vit + by);
			}
		}

		protected override void OnTimeout(TagHolder cont) {
			Character ch = (Character) cont;
			Change(ch, -this.amount);
			ch.SysMessage((this.amount > 0) ? "Moc povzbuzeni vyprchala" : "Paralyzujici strach pominul");
		}
	}

	public static class RealmAbilities {
		internal static readonly TimerKey HonorTK = TimerKey.Acquire("_realmHonor_");
		private static readonly TagKey honorReadyTK = TagKey.Acquire("_honorReadyAt_");

		internal static readonly TimerKey FearTK = TimerKey.Acquire("_realmFear_");
		private static readonly TagKey fearReadyTK = TagKey.Acquire("_fearReadyAt_");
		private const int FearSound = 598;

		/// <summary>fear, makeme_numb: everyone not of Mordor around a Nazgul, more so around the First.</summary>
		[SteamFunction]
		public static void Fear(Character self, ScriptArgs sa) {
			if (!(NazgulRings.IsNazgul(self) && Uniform.Has(self)) && !self.IsGM) {
				self.SysMessage("Tve vzezreni u nikoho hruzu nevzbuzuje");
				return;
			}
			if (IsWaiting(self, fearReadyTK)) {
				self.SysMessage("Jsi jeste vycerpan z predchoziho pouziti sve schopnosti");
				return;
			}
			RealmSettings settings = RealmSettings.instance;
			bool first = NazgulRings.IsFirst(self);
			int penalty = first ? settings.firstFearPenalty : settings.fearPenalty;
			self.Sound(FearSound);
			self.SetTag(fearReadyTK, Globals.TimeInSeconds + settings.fearDelaySeconds);
			foreach (AbstractCharacter around in self.GetMap().GetPlayersInRange(self.X, self.Y, first ? settings.firstFearDistance : settings.fearDistance)) {
				Character ch = around as Character;
				if ((ch != null) && ch.IsOnline && (ch.Realm != Realm.Mordor) && (ch.GetTimer(FearTK) == null)) {
					RealmStatEffect.Change(ch, -penalty);
					ch.AddTimer(FearTK, new RealmStatEffect(-penalty)).DueInSeconds = settings.fearEffectSeconds;
					ch.SysMessage("Jsi ochromen strachem");
				}
			}
		}

		private static bool IsWaiting(Character ch, TagKey readyAt) {
			object ready = ch.GetTag(readyAt);
			return (ready != null) && (Convert.ToDouble(ready) > Globals.TimeInSeconds);
		}

		/// <summary>honor, makeme_encouraged: everyone not of Mordor around the General.</summary>
		[SteamFunction]
		public static void Honor(Character self, ScriptArgs sa) {
			RealmOffice office = RealmOffice.For(Realm.Gondor);
			bool isGeneral = (office != null) && (office.holder == self) && Uniform.Has(self);
			if (!isGeneral && !self.IsGM) {
				self.SysMessage("Nejsi schopen jakkoliv povzbudit sve bojovniky");
				return;
			}
			if (IsWaiting(self, honorReadyTK) && !self.IsGM) {
				self.SysMessage("Jsi jeste vycerpan z predchoziho pouziti sve schopnosti");
				return;
			}
			RealmSettings settings = RealmSettings.instance;
			self.SetTag(honorReadyTK, Globals.TimeInSeconds + settings.honorDelaySeconds);
			foreach (AbstractCharacter around in self.GetMap().GetPlayersInRange(self.X, self.Y, settings.honorDistance)) {
				Character ch = around as Character;
				if ((ch != null) && ch.IsOnline && (ch.Realm != Realm.Mordor) && (ch.GetTimer(HonorTK) == null)) {
					RealmStatEffect.Change(ch, settings.honorBonus);
					ch.AddTimer(HonorTK, new RealmStatEffect(settings.honorBonus)).DueInSeconds = settings.honorEffectSeconds;
					ch.SysMessage("Citis se povzbuzen");
				}
			}
		}
	}
}
