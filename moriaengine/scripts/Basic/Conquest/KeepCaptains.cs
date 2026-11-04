using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	public partial class CaptainStoneDef {
	}

	/// <summary>#1116 - Erebor's keep captain stone (t_captainmenu, t_mordorcaptainmenu in systems/velitele_pevnosti*.scp). A
	/// double click names the keep's captain, offers a GM to appoint or dismiss one, and opens the keep's guard dialogs to its
	/// chiefs: the captain, the holder realm's General or First Nazgul, a GM. It carries the clock of the keep's guards.
	/// Fixed from Erebor: the stone serves whoever holds the keep. Erebor's stone kept its realm's type, so a captain of the
	/// realm that took the keep could never open it.</summary>
	[ViewableClass]
	public partial class CaptainStone {
		private static readonly TagKey fortTK = TagKey.Acquire("_captainStoneFort_");

		public string Fort {
			get { return this.GetTag(fortTK) as string; }
		}

		public void SetFort(string fort) {
			this.SetTag(fortTK, fort);
			KeepState keep = Conquest.Keep(fort);
			keep.captainStone = this;
			GuardForce force = KeepCaptains.ForceOf(keep);
			force.keepStone = this;
			force.EnsureCheckTimer(this);
		}

		public override void On_DClick(AbstractCharacter from) {
			string fort = this.Fort;
			if ((fort == null) || !(from is Player player)) {
				return;
			}
			KeepState keep = Conquest.Keep(fort);
			string keepName = KeepCaptains.KeepName(fort);
			if (keep.captain != null) {
				player.SysMessage("Pevnost ridi " + keep.captain.Name);
				if (player.IsGM) {
					D_TownConfirm.Ask(player, "Opravdu si prejes sesadit z funkce kapitana pevnosti?", () => KeepCaptains.Dismiss(keep));
				}
			} else {
				player.SysMessage("Kapitan pevnosti " + keepName + " neni urcen");
				if (player.IsGM) {
					D_TownConfirm.Ask(player, "Prejes si dosadit noveho kapitana pevnosti " + keepName + "?", () => {
						player.SysMessage("Zamer budouciho kapitana pevnosti");
						player.Target(SingletonScript<Targ_KeepCaptain>.Instance, keep);
					});
				}
			}
			if (KeepCaptains.IsChief(player, keep)) {
				if (!KeepCaptains.InKeep(player, fort)) {
					player.RedMessage("Musis byt v prostoru pevnosti!");
					return;
				}
				KeepCaptains.OpenMenu(player, keep);
			}
		}
	}

	/// <summary>#1116 - Erebor's keep captains (systems/velitele_pevnosti*.scp): the captain of a keep (its KeepState and the
	/// player's tag), appointed by a GM at the stone or made of the war lord who takes a keep; the keep's free guards
	/// (GuardForce with keepFort); the .keep* commands.</summary>
	public static class KeepCaptains {
		private static readonly TagKey keepTK = TagKey.Acquire("_keepCaptainOf_");

		/// <summary>keep_name_&lt;fort&gt;: the keep's name in Erebor's tables (Guard_king_&lt;name&gt;_n, max_guards_&lt;name&gt;).</summary>
		public static string KeepName(string fort) {
			switch (fort) {
				case "Gondor_1":
					return "Osgiliath_Zapadni_Breh";
				case "Gondor_2":
					return "Vychodni_Marka";
				case "Mordor_1":
					return "Osgiliath_Vychodni_Breh";
				case "Mordor_2":
					return "Minas_Morgul";
				default:
					return fort;
			}
		}

		/// <summary>Erebor's tag captain: a captain of some keep.</summary>
		public static bool IsCaptain(Character ch) {
			return ch.GetTag(keepTK) is Thing stone && !stone.IsDeleted;
		}

		public static GuardForce ForceOf(KeepState keep) {
			if (keep.guards == null) {
				keep.guards = new GuardForce { city = KeepName(keep.fort), isKings = true, keepFort = keep.fort };
			}
			return keep.guards;
		}

		/// <summary>aretheremoreguards: a guard of the keep still stands, so its palantir can't be breached.</summary>
		public static bool AnyGuardAlive(string fort) {
			KeepState keep = Conquest.Keep(fort);
			return (keep.guards != null) && keep.guards.AnyStanding;
		}

		/// <summary>respawnGuards, at the start of a war on the keep.</summary>
		public static void RespawnGuards(string fort) {
			KeepState keep = Conquest.Keep(fort);
			if (keep.guards != null) {
				keep.guards.RespawnAll();
			}
		}

		/// <summary>Is the player in one of the keep's areas?</summary>
		public static bool InKeep(Character ch, string fort) {
			FlaggedRegion area = RegionEventSystem.AreaOf(ch.Region);
			return (area != null) && (area.Battlefield != 0) && (Conquest.FortName(area.Battlefield) == fort);
		}

		/// <summary>The keep's captain, its holder realm's General or First Nazgul, or a GM - and of the holder realm.</summary>
		public static bool IsChief(Player player, KeepState keep) {
			if (player.IsGM) {
				return true;
			}
			if (player.Realm != keep.owner) {
				return false;
			}
			RealmOffice office = RealmOffice.For(keep.owner);
			return (keep.captain == player) || ((office != null) && (office.holder == player));
		}

		/// <summary>f_captainmenu: orders, the recruiter, the posts and the blacklist.</summary>
		internal static void OpenMenu(Player player, KeepState keep) {
			GuardForce force = ForceOf(keep);
			D_GuardCaptain.Show(force, player);
			D_GuardRecruiter.Show(force, player);
			D_GuardPosts.Show(force, player);
			D_KeepBans.Show(keep, player);
		}

		/// <summary>Who Erebor wouldn't make a captain: what to tell them, or null.</summary>
		private static string Refusal(Character ch, bool takenByWar) {
			if (ch.IsGM) {
				return takenByWar ? "GM nepotrebuji byt kapitani." : "GM nepotrebuji byt kapitani";
			}
			if (Town.OfRuler(ch) != null) {
				return takenByWar ? "Jako starosta nemuzes byt zaroven kapitanem." : "Starostu kapitanem nedelej, ma sve prace dost";
			}
			RealmOffice gondor = RealmOffice.For(Realm.Gondor);
			if ((gondor != null) && (gondor.holder == ch)) {
				return takenByWar ? "Jako general nemuzes byt degradovan na pouheho kapitana." : "Generala Gondoru kapitanem delati netreba";
			}
			if (IsCaptain(ch)) {
				return takenByWar ? "Jako kapitan se nemuzes stat kapitanem jine pevnosti." : "Kapitan nemuze byt kapitanem v jine pevnosti";
			}
			if (NazgulRings.IsNazgul(ch) || NazgulRings.IsFirst(ch)) {
				return takenByWar ? "Jako nazgul nemuzes byt kapitanem." : "Nazguly kapitanem nedelej";
			}
			return null;
		}

		/// <summary>kapitanmake(1): a GM's appointment.</summary>
		internal static void Appoint(Player gm, KeepState keep, Character ch) {
			string refused = Refusal(ch, false);
			if (refused != null) {
				(IsCaptain(ch) ? ch : gm).SysMessage(refused);
				return;
			}
			Make(keep, ch);
			ch.SysMessage("Byl jsi jmenovan kapitanem pevnosti " + KeepName(keep.fort));
			gm.SysMessage("Novy kapitan uspesne jmenovan");
		}

		/// <summary>makePlayerCaptain: the war lord who took the keep.</summary>
		internal static void MakeWarLordCaptain(KeepState keep, Player lord) {
			string refused = Refusal(lord, true);
			if (refused != null) {
				lord.SysMessage(refused);
				return;
			}
			Make(keep, lord);
			RealmUniform.Grant(lord, 12, lord); //#1133 - f_uniformRealm_set(12)
			lord.SysMessage(Conquest.Word(lord, "Byl jsi jmenovan kapitanem", "Byla jsi jmenovana kapitankou") + " pevnosti " + KeepName(keep.fort) + ".");
		}

		private static void Make(KeepState keep, Character ch) {
			if (keep.captain != null) {
				Dismiss(keep);
			}
			keep.captain = ch as Player;
			ch.SetTag(keepTK, keep.captainStone);
			PowerBadge.SetTitle(ch, "Kapitan ");
			Logger.WriteInfo(true, ch.Name + " is the captain of " + keep.fort);
		}

		/// <summary>kapitanmake(0).</summary>
		public static void Dismiss(KeepState keep) {
			Player captain = keep.captain;
			keep.captain = null;
			if ((captain == null) || captain.IsDeleted) {
				return;
			}
			captain.RemoveTag(keepTK);
			PowerBadge.ClearTitle(captain);
			RealmUniform.Remove(captain); //#1133 - kapitanmake(0) takes the uniform too
			captain.RedMessage("Byl jsi odvolan z funkce kapitana pevnosti " + KeepName(keep.fort));
		}

		/// <summary>Becoming the General or the First Nazgul ends a captaincy, as on Erebor.</summary>
		public static void DismissFrom(Character ch) {
			KeepState keep = KeepOfCaptain(ch);
			if (keep != null) {
				Dismiss(keep);
			}
		}

		internal static KeepState KeepOfCaptain(Character ch) {
			if ((ch.GetTag(keepTK) is CaptainStone stone) && !stone.IsDeleted && (stone.Fort != null)) {
				KeepState keep = Conquest.Keep(stone.Fort);
				return (keep.captain == ch) ? keep : null;
			}
			return null;
		}

		/// <summary>handleConqueredFort's part: the captain goes, the guards turn to the winner's, the war lord takes over.</summary>
		internal static void OnCapture(string fort, Player lord) {
			KeepState keep = Conquest.Keep(fort);
			Dismiss(keep);
			if (keep.guards != null) {
				keep.guards.SwapToOpposite();
			}
			if ((lord != null) && (keep.captainStone != null)) {
				MakeWarLordCaptain(keep, lord);
			}
		}

		/// <summary>getNearestKeepName and ismyconquered: a captain's own keep, else the keep whose stone is nearest; the keep
		/// must be the player's realm's (a GM's anyway). Null and told why when there's none.</summary>
		private static KeepState KeepFor(Player player) {
			KeepState keep = KeepOfCaptain(player);
			if (keep == null) {
				int best = int.MaxValue;
				foreach (string fort in new[] { "Gondor_1", "Gondor_2", "Mordor_1", "Mordor_2" }) {
					KeepState candidate = Conquest.Keep(fort);
					Thing stone = candidate.captainStone;
					if ((stone != null) && !stone.IsDeleted && (stone.M == player.M)) {
						int distance = Point2D.GetSimpleDistance(stone, player);
						if (distance < best) {
							best = distance;
							keep = candidate;
						}
					}
				}
			}
			if (keep == null) {
				player.SysMessage("Zadna pevnost zde");
				return null;
			}
			if (!player.IsGM && (keep.owner != player.Realm)) {
				player.RedMessage("Nemuzes ovladat pevnost, kterou jsi nedobyl");
				return null;
			}
			return keep;
		}

		/// <summary>ischiefinkeep: the captain anywhere, the General, the First or a GM inside the keep.</summary>
		private static KeepState ChiefKeep(Player player) {
			KeepState keep = KeepFor(player);
			if ((keep == null) || !IsChief(player, keep)) {
				if (keep != null) {
					player.SysMessage("Nemas k dispozici zadne straze");
				}
				return null;
			}
			if ((keep.captain != player) && !InKeep(player, keep.fort)) {
				player.SysMessage("Musis byt v pevnosti");
				return null;
			}
			return keep;
		}

		internal static void Control(Character self) {
			if ((self is Player player) && (ChiefKeep(player) is KeepState keep)) {
				GuardForce force = ForceOf(keep);
				D_GuardPosts.Show(force, player);
				D_GuardCaptain.Show(force, player);
				D_KeepBans.Show(keep, player);
			}
		}

		internal static void Order(Character self, GuardOrder order) {
			if (!(self is Player player) || !(ChiefKeep(player) is KeepState keep)) {
				return;
			}
			if ((order == GuardOrder.Come) && !InKeep(player, keep.fort)) {
				player.SysMessage((keep.captain == player) ? "Straze prijdou jen v ramci tve pevnosti" : "Kralovske straze muzes povolat jen v ramci pevnosti");
				return;
			}
			GuardOrders.Give(player, ForceOf(keep), order);
		}
	}

	/// <summary>Erebor's keep commands, the Gondor and the Mordor set; either works for whoever holds the keep.</summary>
	public static class KeepCommands {
		[SteamFunction] public static void KeepControl(Character self) { KeepCaptains.Control(self); }
		[SteamFunction] public static void KeepGuardsStop(Character self) { KeepCaptains.Order(self, GuardOrder.Stop); }
		[SteamFunction] public static void KeepGuardsContinue(Character self) { KeepCaptains.Order(self, GuardOrder.Continue); }
		[SteamFunction] public static void KeepGuardsFlee(Character self) { KeepCaptains.Order(self, GuardOrder.Flee); }
		[SteamFunction] public static void KeepGuardsCome(Character self) { KeepCaptains.Order(self, GuardOrder.Come); }
		[SteamFunction] public static void MordorKeepControl(Character self) { KeepCaptains.Control(self); }
		[SteamFunction] public static void MordorKeepGuardsStop(Character self) { KeepCaptains.Order(self, GuardOrder.Stop); }
		[SteamFunction] public static void MordorKeepGuardsContinue(Character self) { KeepCaptains.Order(self, GuardOrder.Continue); }
		[SteamFunction] public static void MordorKeepGuardsFlee(Character self) { KeepCaptains.Order(self, GuardOrder.Flee); }
		[SteamFunction] public static void MordorKeepGuardsCome(Character self) { KeepCaptains.Order(self, GuardOrder.Come); }
	}

	/// <summary>i_targ_cpt_mk: the GM's target for a new captain. "Zameruj lidi" for anything else.</summary>
	public class Targ_KeepCaptain : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!(targetted is Player)) {
				self.SysMessage("Zameruj lidi");
				return TargetResult.RestartTargetting;
			}
			KeepCaptains.Appoint(self, (KeepState) parameter, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.SysMessage("Zameruj lidi");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("Zameruj lidi");
			return TargetResult.RestartTargetting;
		}
	}
}
