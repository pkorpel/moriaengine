using System;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's keep blacklist (systems/velitele_pevnosti.scp's i_keep_ban, d_captain_executioner): the
	/// captain names players of the keep's realm whom its guards are to keep out. The keep's guards attack them and warn them
	/// off ("Aaaa ..., mame prikaz nevpustit te do pevnosti, strez se vstoupit!").
	/// Fixed from Erebor: it works. Erebor's guards found their keep by a region tag jmeno_mesta no keep area had, so it most
	/// likely never fired; here a guard knows its keep, and the list holds characters, not names. The "already listed" line
	/// names the target (Erebor's named the captain).</summary>
	public static class KeepBlacklist {
		/// <summary>The keep whose guards this NPC is, null for none.</summary>
		public static KeepState KeepOfGuard(NPC guard) {
			foreach (string fort in new[] { "Gondor_1", "Gondor_2", "Mordor_1", "Mordor_2" }) {
				KeepState keep = Conquest.Keep(fort);
				if ((keep.guards != null) && keep.guards.guards.Exists(g => g.npc == guard)) {
					return keep;
				}
			}
			return null;
		}

		public static TownBan Find(KeepState keep, Character ch) {
			return keep.bans.Find(b => b.who == ch);
		}

		/// <summary>isbannedplayer's keep part.</summary>
		public static bool IsBannedFor(NPC guard, Character ch) {
			KeepState keep = KeepOfGuard(guard);
			return (keep != null) && (Find(keep, ch) != null);
		}

		/// <summary>keep_blacklist / mordorkeep_blacklist: a captain's target.</summary>
		internal static void StartBan(Player player) {
			KeepState keep = KeepCaptains.KeepOfCaptain(player);
			if ((keep == null) || (player.Realm != keep.owner)) {
				player.SysMessage("Nejsi kapitanem pevnosti");
				return;
			}
			player.SysMessage("Koho chces pridat na blacklist?");
			player.Target(SingletonScript<Targ_KeepBan>.Instance, keep);
		}

		/// <summary>i_keep_ban's @targon_char.</summary>
		internal static void Ban(Player by, KeepState keep, Character target) {
			string refused = null;
			RealmOffice gondor = RealmOffice.For(Realm.Gondor);
			if ((target.Realm != Realm.Gondor) && (target.Realm != Realm.Mordor)) {
				refused = "To co jsi zameril neni vhodne pro blacklist";
			} else if (target.IsGM) {
				refused = "GMka ne!";
			} else if (target.Realm != keep.owner) {
				refused = "Tohoto ani neni potreba blacklistovat";
			} else if ((gondor != null) && (gondor.holder == target)) {
				refused = "Generala Gondoru nemuzes pridat na blacklist";
			} else if (NazgulRings.IsNazgul(target) || NazgulRings.IsFirst(target)) {
				refused = "To je tvuj nadrizeny!";
			} else if (KeepCaptains.IsCaptain(target)) {
				refused = "Kapitany na blacklist nedavej";
			} else if (Town.OfRuler(target) != null) {
				refused = "Starosty nemuzes pridat na blacklist";
			} else if (Find(keep, target) != null) {
				refused = target.Name + " uz jednou byl pridan na blacklist teto pevnosti";
			}
			if (refused != null) {
				by.SysMessage(refused);
			} else {
				keep.bans.Add(new TownBan { who = target, name = target.Name, reason = "" });
				Logger.WriteInfo(true, by.Name + " blacklisted " + target.Name + " from the keep " + keep.fort);
				by.SysMessage(target.Name + " uspesne pridan na cernou listinu pevnosti");
				target.RedMessage("Byl jsi dan na cernou listinu pevnosti " + KeepCaptains.KeepName(keep.fort));
			}
			D_KeepBans.Show(keep, by);
		}

		internal static void Remove(Player by, KeepState keep, TownBan ban) {
			keep.bans.Remove(ban);
			Logger.WriteInfo(true, by.Name + " took " + ban.name + " off the blacklist of the keep " + keep.fort);
			if ((ban.who != null) && !ban.who.IsDeleted) {
				ban.who.SysMessage(ban.name + " byl odstranen z cerne listiny pevnosti " + KeepCaptains.KeepName(keep.fort));
			}
		}

		[SteamFunction("keep_blacklist")]
		public static void KeepBlacklistCommand(Character self) {
			if (self is Player player) {
				StartBan(player);
			}
		}

		[SteamFunction("mordorkeep_blacklist")]
		public static void MordorKeepBlacklistCommand(Character self) {
			if (self is Player player) {
				StartBan(player);
			}
		}
	}

	/// <summary>i_keep_ban: a player, or a player's severed head.</summary>
	public class Targ_KeepBan : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			KeepBlacklist.Ban(self, (KeepState) parameter, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (targetted.GetTag(CorpseCarving.HeadOfTK) is Character owner) {
				KeepBlacklist.Ban(self, (KeepState) parameter, owner);
			} else {
				self.SysMessage(targetted.Def.PrettyDefname.StartsWith("i_flesh_head", StringComparison.OrdinalIgnoreCase) ? "To neni hlava hrace" : "Zameruj lidi, nebo jejich hlavy");
				D_KeepBans.Show((KeepState) parameter, self);
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("Zameruj lidi.");
			D_KeepBans.Show((KeepState) parameter, self);
			return TargetResult.Done;
		}
	}

	/// <summary>d_captain_executioner: "Nepratel : n", a row per name with its remove button (100+i), "Pridat cloveka" (3).</summary>
	public class D_KeepBans : CompiledGumpDef {
		private const int Width = 260, PageRows = 7, RowHeight = 20, FirstRow = 40, FirstRowButton = 100, AddButton = 3, TextHue = 2301;

		internal static void Show(KeepState keep, Player player) {
			player.Dialog(player, SingletonScript<D_KeepBans>.Instance, new DialogArgs(keep));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			KeepState keep = (KeepState) args[0];
			int bottom = FirstRow + (PageRows * RowHeight);
			this.SetLocation(120, 260);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, bottom + 36, 2620);
			this.AddLabel(14, 12, 0x481, "Nepratel : " + keep.bans.Count);
			this.AddButton(Width - 45, 12, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddButton(14, bottom + 8, 4005, 4007, AddButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, bottom + 8, 955, "Pridat cloveka");
			int pages = Math.Max(1, (keep.bans.Count + PageRows - 1) / PageRows);
			for (int page = 0; page < pages; page++) {
				this.AddPage(page + 1);
				if (page > 0) {
					this.AddButton(Width - 25, FirstRow, 0xfa, 0xfb, 0, GumpButtonType.Page, page);
				}
				if (page < (pages - 1)) {
					this.AddButton(Width - 25, FirstRow + ((PageRows - 1) * RowHeight), 0xfc, 0xfd, 0, GumpButtonType.Page, page + 2);
				}
				int last = Math.Min(keep.bans.Count, (page + 1) * PageRows);
				for (int i = page * PageRows; i < last; i++) {
					int y = FirstRow + ((i % PageRows) * RowHeight);
					this.AddButton(14, y, 4005, 4007, FirstRowButton + i, GumpButtonType.Reply, 0);
					this.AddLabel(50, y, TextHue, keep.bans[i].name);
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			KeepState keep = (KeepState) args[0];
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if (!KeepCaptains.IsChief(player, keep)) {
				return;
			}
			if (button == AddButton) {
				player.SysMessage("Koho chces pridat na blacklist?");
				player.Target(SingletonScript<Targ_KeepBan>.Instance, keep);
			} else if ((button >= FirstRowButton) && ((button - FirstRowButton) < keep.bans.Count)) {
				KeepBlacklist.Remove(player, keep, keep.bans[button - FirstRowButton]);
				Show(keep, player);
			}
		}
	}
}
