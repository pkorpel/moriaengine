using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1114 - .gmmenu: the GM commands in groups, each with a line of what it does. A button runs the command as if
	/// it were typed (Commands.PlayerCommand: the same permission check and argument parsing); a command that takes an
	/// argument has a field for it, sent quoted when it's text.</summary>
	public class D_GmCommands : CompiledGumpDef {
		internal enum Arg { None, Number, Text }

		internal sealed class Entry {
			internal readonly string command, description;
			internal readonly Arg arg;

			internal Entry(string command, string description, Arg arg = Arg.None) {
				this.command = command;
				this.description = description;
				this.arg = arg;
			}
		}

		internal sealed class Group {
			internal readonly string title;
			internal readonly Entry[] entries;

			internal Group(string title, params Entry[] entries) {
				this.title = title;
				this.entries = entries;
			}
		}

		internal static readonly Group[] groups = {
			new Group("Moderace",
				new Entry("jail", "zavrit do vezeni (kurzor)"),
				new Entry("forgive", "propustit z vezeni (kurzor)"),
				new Entry("lagr", "do lagru za prohresek (kurzor)"),
				new Entry("afklagr", "do lagru za AFK (kurzor)"),
				new Entry("lagrmenu", "lagr menu postavy (kurzor)"),
				new Entry("lagrpustit", "propustit z lagru (kurzor)"),
				new Entry("lagrodvolat", "o stupen lagru mene (kurzor)"),
				new Entry("afklagrodvolat", "o stupen AFK lagru mene (kurzor)"),
				new Entry("afk", "AFK kontrola hned, volitelne sekund (kurzor)", Arg.Number),
				new Entry("runafk", "AFK kontrola s varovanim (kurzor)"),
				new Entry("forceafk", "AFK kontrola vsech, kdo sbiraji nebo vyrabi"),
				new Entry("afklog", "AFK log, volitelne filtr", Arg.Text),
				new Entry("kick", "vyhodit ze hry (kurzor)"),
				new Entry("blockip", "zablokovat IP adresu")),
			new Group("Hraci",
				new Entry("clients", "hraci ve hre, s tlacitky"),
				new Entry("goname", "jit za postavou podle jmena", Arg.Text),
				new Entry("gouid", "jit k veci podle uid", Arg.Number),
				new Entry("gocli", "jit k n-temu hraci ze seznamu", Arg.Number),
				new Entry("summonto", "privest k sobe (kurzor)"),
				new Entry("summoncage", "privest a zmrazit (kurzor)"),
				new Entry("summondismiss", "poslat zpet, odkud prisel (kurzor)"),
				new Entry("freeze", "zmrazit na miste (kurzor)"),
				new Entry("unfreeze", "rozmrazit (kurzor)"),
				new Entry("info", "info dialog (kurzor)"),
				new Entry("kill", "zabit (kurzor)")),
			new Group("GM",
				new Entry("gm", "GM rezim zapnout / vypnout"),
				new Entry("invis", "neviditelnost zapnout / vypnout"),
				new Entry("invul", "nesmrtelnost zapnout / vypnout"),
				new Entry("tele", "teleport (kurzor)"),
				new Entry("flip", "otocit vec (kurzor)"),
				new Entry("addbooster", "dat booster, druh 0-5 (kurzor)", Arg.Number)),
			new Group("Svet",
				new Entry("settings", "nastaveni serveru"),
				new Entry("save", "ulozit svet"),
				new Entry("setupworld", "doplnit do sveta, co chybi"),
				new Entry("housedoor", "dvere domu (kurzor)"),
				new Entry("housepad", "dlazdice pred dvermi (kurzor)"),
				new Entry("housemove", "presunout cedulku domu (kurzor)")),
		};

		private const int Width = 760, Row = 20, Margin = 12, ColumnWidth = 370, Hue = 955, TitleHue = 0x481, CommandHue = 0x35;
		internal const int CloseButton = 0, FirstCommandButton = 1, FirstEntry = 1;

		/// <summary>The entries in order, so a button's number finds its entry.</summary>
		internal static Entry[] All {
			get {
				System.Collections.Generic.List<Entry> all = new System.Collections.Generic.List<Entry>();
				foreach (Group group in groups) {
					all.AddRange(group.entries);
				}
				return all.ToArray();
			}
		}

		[SteamFunction]
		public static void GmMenu(Player self) {
			self.Dialog(self, SingletonScript<D_GmCommands>.Instance, new DialogArgs());
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int rowsLeft = 0, rowsRight = 0;
			for (int g = 0; g < groups.Length; g++) {
				if ((g % 2) == 0) {
					rowsLeft += groups[g].entries.Length + 2;
				} else {
					rowsRight += groups[g].entries.Length + 2;
				}
			}
			int height = (System.Math.Max(rowsLeft, rowsRight) * Row) + (3 * Margin) + Row;
			this.SetLocation(40, 40);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 9200);
			this.AddLabel(Margin, Margin, TitleHue, "Prikazy GM");
			this.AddButton(Width - Margin - 33, Margin, 0xfb1, 0xfb3, CloseButton, GumpButtonType.Reply, 0);
			int[] y = { Margin + Row + Margin, Margin + Row + Margin };
			int index = 0;
			for (int g = 0; g < groups.Length; g++) {
				int column = g % 2;
				int x = Margin + (column * ColumnWidth);
				this.AddLabel(x, y[column], TitleHue, groups[g].title);
				y[column] += Row;
				foreach (Entry entry in groups[g].entries) {
					this.AddButton(x, y[column], 4005, 4007, FirstCommandButton + index, GumpButtonType.Reply, 0);
					this.AddLabel(x + 35, y[column], CommandHue, "." + entry.command);
					int describeAt = x + 145;
					if (entry.arg != Arg.None) {
						this.AddBackground(x + 145, y[column], 70, Row, 3000);
						this.AddTextEntry(x + 148, y[column], 64, Row, 0, FirstEntry + index, "");
						describeAt = x + 220;
					}
					this.AddLabelCropped(describeAt, y[column], ColumnWidth - (describeAt - x) - 5, Row, Hue, entry.description);
					y[column] += Row;
					index++;
				}
				y[column] += Row;
			}
		}

		/// <summary>The text that would be typed after the dot.</summary>
		internal static string CommandLine(Entry entry, string argument) {
			argument = (argument ?? "").Trim().Trim('"').Trim();
			if ((entry.arg == Arg.None) || (argument.Length == 0)) {
				return entry.command;
			}
			return entry.command + " " + ((entry.arg == Arg.Text) ? ("\"" + argument + "\"") : argument);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm) || (gr.PressedButton == CloseButton)) {
				return;
			}
			Entry[] all = All;
			int index = gr.PressedButton - FirstCommandButton;
			if ((index < 0) || (index >= all.Length) || (gm.GameState == null)) {
				return;
			}
			Commands.PlayerCommand(gm.GameState, CommandLine(all[index], gr.GetTextResponse(FirstEntry + index)));
		}
	}
}
