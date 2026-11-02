using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - GM tools for what .SetupHouses can't know: where a sign or rent chest really belongs, and the doors,
	/// which Erebor's GMs placed by hand and aren't in the archive.</summary>
	public static class HouseCommands {
		private const string DefaultDoor = "i_0x675";

		[SteamFunction]
		public static void HouseMove(Character self) {
			Player player = self as Player;
			if (player != null) {
				player.SysMessage("Kterou vyvesku nebo truhlu presunout?");
				player.Target(SingletonScript<Targ_HouseMove>.Instance);
			}
		}

		/// <summary>A door of a house: the dialog, or straight to the spot with a door's defname. Lock it from the sign.</summary>
		[SteamFunction]
		public static void HouseDoor(Character self, ScriptArgs sa) {
			Player player = self as Player;
			if (player != null) {
				if ((sa == null) || string.IsNullOrEmpty(sa.Args)) {
					D_HouseDoors.Show(player);
				} else {
					Targ_HouseDoor.Ask(player, new HouseDoorChoice(sa.Args.Trim('"', ' '), 0));
				}
			}
		}
	}

	/// <summary>First the thing, with no parameter, then where it goes.</summary>
	public class Targ_HouseMove : CompiledTargetDef {
		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			if (parameter != null) {
				return this.MoveTo(self, targetted, (Item) parameter);
			}
			if ((targetted.Type is t_housemenu) || (targetted is RentChest)) {
				self.SysMessage("Kam?");
				self.Target(this, targetted);
			} else {
				self.SysMessage("To neni vyveska ani truhla domu.");
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return (parameter == null) ? TargetResult.RestartTargetting : this.MoveTo(self, targetted, (Item) parameter);
		}

		private TargetResult MoveTo(Player self, IPoint3D where, Item piece) {
			if (!piece.IsDeleted) {
				piece.P(where.X, where.Y, where.Z, self.M);
				self.SysMessage("Presunuto.");
			}
			return TargetResult.Done;
		}
	}

	/// <summary>Which door, and which way it faces: an offset from its def's model.</summary>
	public sealed class HouseDoorChoice {
		internal readonly string defname;
		internal readonly int offset;

		internal HouseDoorChoice(string defname, int offset) {
			this.defname = defname;
			this.offset = offset;
		}
	}

	/// <summary>One door to a go, the user's call: a double door is the command twice.</summary>
	public class Targ_HouseDoor : CompiledTargetDef {
		internal static void Ask(Player player, HouseDoorChoice choice) {
			player.SysMessage("Kam dvere postavit?");
			player.Target(SingletonScript<Targ_HouseDoor>.Instance, choice);
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			HouseDoorChoice choice = (HouseDoorChoice) parameter;
			if (House.FindHouseAt(new Point4D(targetted.X, targetted.Y, targetted.Z, self.M)) == null) {
				self.SysMessage("Tady neni zadny dum.");
				return TargetResult.Done;
			}
			ItemDef def = ThingDef.GetByDefname(choice.defname) as ItemDef;
			if (def == null) {
				self.SysMessage("Takova vec neexistuje: " + choice.defname);
				return TargetResult.Done;
			}
			Item door = (Item) def.Create(targetted.X, targetted.Y, targetted.Z, self.M);
			if (!door.IsDoor) {
				self.SysMessage(choice.defname + " nejsou dvere.");
				door.Delete();
				return TargetResult.Done;
			}
			door.Model = def.Model + choice.offset;
			door.SetAttribute(ItemAttributes.attr_move_never, true);
			return TargetResult.Done;
		}
	}

	/// <summary>Every door there is, in pictures. The labels are ours: the defs name them in English or not at all.</summary>
	public class D_HouseDoors : CompiledGumpDef {
		private const int Columns = 5, CellWidth = 110, CellHeight = 110, FirstRow = 40, Hue = 2301;

		internal static readonly string[,] Kinds = {
			{ "i_door_wood", "Drevene 1" }, { "i_door_wood_4", "Drevene 2" }, { "i_door_wood_2", "Drevene 3" }, { "i_door_wood_3", "Drevene 4" },
			{ "i_door_rattan", "Bambusove" }, { "i_door_metal", "Kovove 1" }, { "i_door_metal_2", "Kovove 2" }, { "i_door_metal_bar", "Mrizove 1" },
			{ "i_door_metal_bar_2", "Mrizove 2" }, { "i_gate_wood", "Drev. branka 1" }, { "i_gate_wood_3", "Drev. branka 2" }, { "i_gate_iron", "Zel. branka 1" },
			{ "i_gate_iron_2", "Zel. branka 2" }, { "i_door_secret_wood_1", "Tajne drevo 1" }, { "i_door_secret_wood_2", "Tajne drevo 2" },
			{ "i_door_secret_stone_1", "Tajne kamen 1" }, { "i_door_secret_stone_2", "Tajne kamen 2" }, { "i_door_secret_stone_3", "Tajne kamen 3" },
			{ "i_door_secret_stone_4", "Tajne kamen 4" }
		};

		public static void Show(Player player) {
			player.Dialog(player, SingletonScript<D_HouseDoors>.Instance, new DialogArgs());
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int kinds = Kinds.GetLength(0);
			int rows = (kinds + Columns - 1) / Columns;
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, (Columns * CellWidth) + 20, FirstRow + (rows * CellHeight) + 10, 2620);
			this.AddLabel(14, 12, 0x481, "Jake dvere postavit?");
			for (int i = 0; i < kinds; i++) {
				int x = 14 + ((i % Columns) * CellWidth), y = FirstRow + ((i / Columns) * CellHeight);
				this.AddItem(x + 20, y, ThingDef.GetByDefname(Kinds[i, 0]).Model);
				this.AddButton(x, y + 86, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
				this.AddLabel(x + 28, y + 86, Hue, Kinds[i, 1]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			int kind = gr.PressedButton - 1;
			if ((kind >= 0) && (kind < Kinds.GetLength(0))) {
				D_HouseDoorFacing.Show((Player) gi.Cont, kind);
			}
		}
	}

	/// <summary>The 8 ways a door can stand, closed. .flip still turns one that's placed.</summary>
	public class D_HouseDoorFacing : CompiledGumpDef {
		private const int Facings = 8, Columns = 4, CellWidth = 90, CellHeight = 110, FirstRow = 40, BackButton = 100;

		public static void Show(Player player, int kind) {
			player.Dialog(player, SingletonScript<D_HouseDoorFacing>.Instance, new DialogArgs(kind));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int kind = (int) args[0];
			int model = ThingDef.GetByDefname(D_HouseDoors.Kinds[kind, 0]).Model;
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, (Columns * CellWidth) + 20, FirstRow + (2 * CellHeight) + 34, 2620);
			this.AddLabel(14, 12, 0x481, D_HouseDoors.Kinds[kind, 1] + ": jak maji stat?");
			for (int facing = 0; facing < Facings; facing++) {
				int x = 14 + ((facing % Columns) * CellWidth), y = FirstRow + ((facing / Columns) * CellHeight);
				this.AddItem(x + 20, y, model + (facing * 2));
				this.AddButton(x, y + 86, 4005, 4007, facing + 1, GumpButtonType.Reply, 0);
			}
			this.AddButton(14, FirstRow + (2 * CellHeight) + 6, 4014, 4016, BackButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, FirstRow + (2 * CellHeight) + 6, 2301, "Zpet");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			int kind = (int) args[0];
			int button = gr.PressedButton;
			if (button == BackButton) {
				D_HouseDoors.Show(player);
			} else if ((button >= 1) && (button <= Facings)) {
				Targ_HouseDoor.Ask(player, new HouseDoorChoice(D_HouseDoors.Kinds[kind, 0], (button - 1) * 2));
			}
		}
	}
}
