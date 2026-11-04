using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1133 - Erebor's PvP team tournament (systems/arena.scp's .pvpturnaj_arena): a GM names a chest of gear for each
	/// profession and sex and puts players into up to 6 teams of 7; a team member's double click on the stone "Cesta na
	/// turnaj" sends what they carry and wear to the bank, gives them a copy of their chest's gear (a robe and a pet figurine
	/// in the team's colour) and takes them to the Nova Arena Equip Room. The user's call: the stone gives once per player and
	/// tournament (Erebor's guard was commented out, so it gave without end); "Novy turnaj" starts the next one.
	/// Fixed from Erebor: a player is in one slot only (Erebor let the same one fill several); every slot shows its name
	/// (two of Erebor's showed another's); a new tournament clears the teams (Erebor never cleared tag pvp_team); no debug lines
	/// for each item given. Not ported: the weapon picker (d_arena_vyber_zbrane), dead on Erebor.</summary>
	[HasSavedMembers]
	public static class PvpTournament {
		public const int Teams = 6, Slots = 7;
		internal static readonly string[] Classes = { "craft", "mag", "mystik", "necro", "priest", "ranger", "shaman", "thief", "war" };
		internal static readonly string[] ClassNames = { "Craft", "Mag", "Mystik", "Necro", "Priest", "Ranger", "Shaman", "Thief", "War" };
		private static readonly int[] teamColors = { 0x0982, 0x0480, 0x0A6F, 0x0481, 0x062A, 0x044C };
		internal static readonly Point4D EquipRoom = new Point4D(2202, 1794, 45, 0);

		[SavedMember]
		private static List<TournamentChest> chests = new List<TournamentChest>();

		[SavedMember]
		private static List<TournamentSlot> slots = new List<TournamentSlot>();

		[SavedMember]
		private static List<Player> equipped = new List<Player>();

		/// <summary>"war_muz": the chest key of a profession and sex.</summary>
		public static string KeyOf(Player player) {
			string cls = null;
			if (Professions.HasProfession(player, Professions.Craftsman)) {
				cls = "craft";
			} else if (Professions.HasProfession(player, Professions.Mage)) {
				cls = "mag";
			} else if (Professions.HasProfession(player, Professions.Mystic)) {
				cls = "mystik";
			} else if (Professions.HasProfession(player, Professions.Necromant)) {
				cls = "necro";
			} else if (Professions.HasProfession(player, Professions.Priest)) {
				cls = "priest";
			} else if (Professions.HasProfession(player, Professions.Ranger)) {
				cls = "ranger";
			} else if (Professions.HasProfession(player, Professions.Shaman)) {
				cls = "shaman";
			} else if (Professions.HasProfession(player, Professions.Thief)) {
				cls = "thief";
			} else if (Professions.HasProfession(player, Professions.Warrior)) {
				cls = "war";
			}
			return (cls == null) ? null : (cls + (player.IsFemale ? "_zena" : "_muz"));
		}

		public static Item ChestOf(string key) {
			TournamentChest chest = chests.Find(c => c.key == key);
			return ((chest == null) || (chest.container == null) || chest.container.IsDeleted) ? null : chest.container;
		}

		internal static void SetChest(string key, Item container) {
			chests.RemoveAll(c => c.key == key);
			chests.Add(new TournamentChest { key = key, container = container });
		}

		public static Player InSlot(int team, int slot) {
			TournamentSlot found = slots.Find(s => (s.team == team) && (s.slot == slot));
			return ((found == null) || (found.player == null) || found.player.IsDeleted) ? null : found.player;
		}

		/// <summary>The player's team, 0 for none (Erebor's tag pvp_team).</summary>
		public static int TeamOf(Character ch) {
			TournamentSlot found = slots.Find(s => s.player == ch);
			return (found == null) ? 0 : found.team;
		}

		/// <summary>The player takes the slot, leaving any other; whoever had it is out.</summary>
		internal static void SetSlot(int team, int slot, Player player) {
			slots.RemoveAll(s => (s.player == player) || ((s.team == team) && (s.slot == slot)));
			slots.Add(new TournamentSlot { team = team, slot = slot, player = player });
		}

		/// <summary>The next tournament: no teams, and everyone may take gear again.</summary>
		internal static void NewTournament() {
			slots.Clear();
			equipped.Clear();
		}

		/// <summary>i_stone_pvp_turnaj's @userdclick and arena_vybavit.</summary>
		internal static void Enter(Player player) {
			int team = TeamOf(player);
			if (team == 0) {
				player.RedMessage("Nejsi clenem zadneho teamu!");
				return;
			}
			string key = KeyOf(player);
			Item chest = (key == null) ? null : ChestOf(key);
			if (!(chest is Container)) {
				player.RedMessage("Nemuzes vstoupit!");
				return;
			}
			if (equipped.Contains(player)) {
				player.RedMessage("Vybavu z turnaje jsi uz " + (player.IsFemale ? "dostala" : "dostal") + ".");
				return;
			}
			equipped.Add(player);
			PvpEventKit.ToBank(player);
			foreach (AbstractItem item in new List<AbstractItem>(chest)) {
				if (!(item is Item source)) {
					continue;
				}
				Item copy = (Item) source.Dupe();
				copy.Cont = player.Backpack;
				if ((copy.Def == ThingDef.GetByDefname("i_robe")) || (copy is PetFigurine)) {
					copy.Color = teamColors[team - 1];
				}
			}
			Logger.WriteInfo(true, "PvP tournament: " + player.Name + " (team " + team + ", " + key + ") took the gear");
			player.Go(EquipRoom);
		}

		[SteamFunction("pvpturnaj_arena")]
		public static void TournamentCommand(Character self) {
			if (self is Player gm) {
				gm.Dialog(gm, SingletonScript<D_PvpTournament>.Instance, new DialogArgs());
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class TournamentChest {
		[SaveableData, CopyableData]
		public string key;

		[SaveableData, CopyableData]
		public Item container;

		[LoadingInitializer, DeepCopyImplementation]
		public TournamentChest() {
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class TournamentSlot {
		[SaveableData, CopyableData]
		public int team;

		[SaveableData, CopyableData]
		public int slot;

		[SaveableData, CopyableData]
		public Player player;

		[LoadingInitializer, DeepCopyImplementation]
		public TournamentSlot() {
		}
	}

	public partial class PvpTournamentStoneDef {
	}

	/// <summary>i_stone_pvp_turnaj.</summary>
	[ViewableClass]
	public partial class PvpTournamentStone {
		public override void On_DClick(AbstractCharacter from) {
			if (from is Player player) {
				PvpTournament.Enter(player);
			}
		}
	}

	/// <summary>d_pvpturnaj_hlavni_vyber: Vybavy (101), Teamy (102), and "Novy turnaj" (103) where Erebor's Codal did nothing.</summary>
	public class D_PvpTournament : CompiledGumpDef {
		internal const int ChestsButton = 101, TeamsButton = 102, NewButton = 103;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(50, 50);
			this.AddPage(0);
			this.AddBackground(0, 0, 260, 130, 5054);
			this.AddLabel(10, 10, 0x481, "Turnaj - Hlavni Vyber");
			this.AddButton(220, 10, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			string[] rows = { "Vybavy", "Teamy", "Novy turnaj" };
			for (int i = 0; i < rows.Length; i++) {
				this.AddButton(10, 40 + (i * 25), 4005, 4007, ChestsButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(45, 40 + (i * 25), 955, rows[i]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm) || !gm.IsGM) {
				return;
			}
			switch (gr.PressedButton) {
				case ChestsButton:
					gm.Dialog(gm, SingletonScript<D_PvpTournamentChests>.Instance, new DialogArgs());
					break;
				case TeamsButton:
					gm.Dialog(gm, SingletonScript<D_PvpTournamentTeams>.Instance, new DialogArgs());
					break;
				case NewButton:
					PvpTournament.NewTournament();
					gm.SysMessage("Novy turnaj: teamy jsou prazdne, vybavu muze kazdy dostat znovu.");
					break;
			}
		}
	}

	/// <summary>d_pvpturnaj_nastaveni_vybavy: Muzi 102-110, Zeny 112-120, each the chest for that profession.</summary>
	public class D_PvpTournamentChests : CompiledGumpDef {
		internal const int MenFirst = 102, WomenFirst = 112;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int rows = PvpTournament.Classes.Length;
			this.SetLocation(50, 50);
			this.AddPage(0);
			this.AddBackground(0, 0, 600, 60 + (rows * 22), 5054);
			this.AddLabel(10, 10, 0x481, "Turnaj - Vybava Hracu");
			this.AddButton(560, 10, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(45, 32, 955, "Muzi");
			this.AddLabel(325, 32, 955, "Zeny");
			for (int i = 0; i < rows; i++) {
				int y = 54 + (i * 22);
				Item men = PvpTournament.ChestOf(PvpTournament.Classes[i] + "_muz"), women = PvpTournament.ChestOf(PvpTournament.Classes[i] + "_zena");
				this.AddButton(10, y, 4005, 4007, MenFirst + i, GumpButtonType.Reply, 0);
				this.AddLabel(45, y, 2301, PvpTournament.ClassNames[i] + ": " + ((men == null) ? "-" : men.Name));
				this.AddButton(290, y, 4005, 4007, WomenFirst + i, GumpButtonType.Reply, 0);
				this.AddLabel(325, y, 2301, PvpTournament.ClassNames[i] + ": " + ((women == null) ? "-" : women.Name));
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm) || !gm.IsGM) {
				return;
			}
			int button = gr.PressedButton;
			int rows = PvpTournament.Classes.Length;
			string key = null;
			if ((button >= MenFirst) && (button < MenFirst + rows)) {
				key = PvpTournament.Classes[button - MenFirst] + "_muz";
			} else if ((button >= WomenFirst) && (button < WomenFirst + rows)) {
				key = PvpTournament.Classes[button - WomenFirst] + "_zena";
			}
			if (key != null) {
				gm.SysMessage("Zamer bednu?");
				gm.Target(SingletonScript<Targ_TournamentChest>.Instance, key);
			}
		}
	}

	/// <summary>i_zamerit_bednu.</summary>
	public class Targ_TournamentChest : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (!(targetted is Container)) {
				self.SysMessage("toto neni kontejner!");
				return TargetResult.RestartTargetting;
			}
			PvpTournament.SetChest((string) parameter, targetted);
			self.Dialog(self, SingletonScript<D_PvpTournamentChests>.Instance, new DialogArgs());
			return TargetResult.Done;
		}
	}

	/// <summary>d_pvpturnaj_nastaveni_teamy: 6 teams of 7, team t's slot n is button 102 + (t-1)*8 + (n-1).</summary>
	public class D_PvpTournamentTeams : CompiledGumpDef {
		internal const int FirstButton = 102;

		internal static int ButtonOf(int team, int slot) {
			return FirstButton + ((team - 1) * 8) + (slot - 1);
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			this.SetLocation(30, 30);
			this.AddPage(0);
			this.AddBackground(0, 0, 690, 60 + (PvpTournament.Slots * 22), 5054);
			this.AddLabel(10, 10, 0x481, "Turnaj - Teamy");
			this.AddButton(650, 10, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			for (int team = 1; team <= PvpTournament.Teams; team++) {
				int x = 10 + ((team - 1) * 112);
				this.AddLabel(x, 32, 955, "Team " + team);
				for (int slot = 1; slot <= PvpTournament.Slots; slot++) {
					int y = 54 + ((slot - 1) * 22);
					Player player = PvpTournament.InSlot(team, slot);
					this.AddButton(x, y, 4005, 4007, ButtonOf(team, slot), GumpButtonType.Reply, 0);
					this.AddLabel(x + 35, y, 2301, (player == null) ? "-" : player.Name);
				}
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm) || !gm.IsGM) {
				return;
			}
			int index = gr.PressedButton - FirstButton;
			if ((index < 0) || ((index % 8) >= PvpTournament.Slots) || ((index / 8) >= PvpTournament.Teams)) {
				return;
			}
			gm.SysMessage("Zamer Hrace?");
			gm.Target(SingletonScript<Targ_TournamentPlayer>.Instance, new[] { (index / 8) + 1, (index % 8) + 1 });
		}
	}

	/// <summary>i_zamerit_hrace.</summary>
	public class Targ_TournamentPlayer : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			if (!(targetted is Player player)) {
				self.SysMessage("Zamer Hrace?");
				return TargetResult.RestartTargetting;
			}
			int[] place = (int[]) parameter;
			PvpTournament.SetSlot(place[0], place[1], player);
			self.Dialog(self, SingletonScript<D_PvpTournamentTeams>.Instance, new DialogArgs());
			return TargetResult.Done;
		}
	}
}
