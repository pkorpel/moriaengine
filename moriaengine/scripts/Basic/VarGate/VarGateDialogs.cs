using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	public enum VarGateList {
		Remove,
		Add,
		Function
	}

	/// <summary>What Erebor's gate dialogs share.</summary>
	public abstract class VarGateDialog : CompiledGumpDef {
		protected const int TitleHue = 42, LabelHue = 0x481, TextHue = 2301, EntryHue = 52, RowHeight = 20;
		protected const int OkButton = 1, CancelButton = 0, NewButton = 2, EditButton = 100, DeleteButton = 1000;

		protected void Frame(int width, int height, string title) {
			this.SetLocation(15, 5);
			this.AddPage(0);
			this.AddBackground(0, 0, width, height, 2620);
			this.AddLabel(14, 12, TitleHue, title);
		}

		protected void OkCancel(int y, int okButton) {
			this.AddButton(14, y, 0xfb7, 0xfb9, okButton, GumpButtonType.Reply, 0);
			this.AddButton(47, y, 0xfb1, 0xfb3, CancelButton, GumpButtonType.Reply, 0);
		}

		protected static int Number(string text, int fallback) {
			return int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
		}

		/// <summary>"a,b" or "a"; the second number is 0 if there's none.</summary>
		protected static void Pair(string text, out int first, out int second) {
			string[] parts = (text ?? "").Split(',');
			first = Number(parts[0], 0);
			second = (parts.Length > 1) ? Number(parts[1], 0) : 0;
		}
	}

	/// <summary>d_varGate. Whichever button but cancel is pressed, the checkboxes and the entries are kept.</summary>
	public class D_VarGate : VarGateDialog {
		private const int Width = 700;
		private const int PositionButton = 2, RemoveButton = 4, AddButton_ = 5, FunctionButton = 6, GuardButton = 7, PluginButton = 8;
		private const int ExpEntry = 100, ExpLevelEntry = 101, ExpDecayEntry = 102, ReqLevelEntry = 103;

		private static readonly VarGateFlags[] checkFlags = {
			VarGateFlags.NpcGoThrough, VarGateFlags.HypnoGoThrough, VarGateFlags.GhostGoThrough, VarGateFlags.ExpGate, VarGateFlags.RequiresLevel, VarGateFlags.Sideways,
			VarGateFlags.RecallPort, VarGateFlags.RemoveCursor, VarGateFlags.RemovePvpTools, VarGateFlags.RemoveMount, VarGateFlags.RemoveBank,
			VarGateFlags.RemoveItems, VarGateFlags.AddItems, VarGateFlags.RunFunctions, VarGateFlags.Guarded, VarGateFlags.HasPlugins
		};

		public static void Show(Player player, VarGate gate) {
			player.Dialog(player, SingletonScript<D_VarGate>.Instance, new DialogArgs(gate));
		}

		private void Check(VarGate gate, VarGateFlags flag, int y, string text) {
			this.Check(gate, flag, 14, y, text);
		}

		private void Check(VarGate gate, VarGateFlags flag, int x, int y, string text) {
			this.AddCheck(x, y, 210, 211, gate.Has(flag), (int) flag);
			this.AddLabel(x + 26, y, TextHue, text);
		}

		private void Entry(int y, string label, int entry, string value) {
			this.AddLabel(300, y, LabelHue, label);
			this.AddTextEntry(530, y, 150, RowHeight, EntryHue, entry, value);
		}

		private void Menu(VarGate gate, VarGateFlags flag, int y, string text, string countLabel, int count, int button) {
			this.Check(gate, flag, y, text);
			this.AddLabel(300, y, LabelHue, countLabel + " " + count);
			this.AddButton(520, y, 0xfbd, 0xfbf, button, GumpButtonType.Reply, 0);
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			VarGate gate = (VarGate) args[0];
			this.Frame(Width, 500, "Variable Gate");
			int y = 36;
			this.AddLabel(14, y, LabelHue, "MoreP:");
			this.AddLabel(70, y, TextHue, gate.Has(VarGateFlags.RecallPort) ? "RECALL" : ((gate.MoreP != null) ? gate.MoreP.ToString() : "0,0"));
			this.AddButton(300, y, 0xfa5, 0xfa7, PositionButton, GumpButtonType.Reply, 0);
			this.AddLabel(335, y, LabelHue, "Nastav na moji pozici");
			y += 28;
			this.Check(gate, VarGateFlags.NpcGoThrough, y, "NPC muze projit.");
			this.Check(gate, VarGateFlags.RecallPort, 300, y, "Recall Home");
			this.Check(gate, VarGateFlags.HypnoGoThrough, y + RowHeight, "Hypno-NPC muze projit."); //#1070
			this.Check(gate, VarGateFlags.GhostGoThrough, y + (2 * RowHeight), "Duchove mohou projit.");
			y += 3 * RowHeight;
			this.Check(gate, VarGateFlags.ExpGate, y, "Toto je ExpGate.");
			this.Entry(y, "Experience 'exp, cas/min':", ExpEntry, gate.expAmount + "," + gate.expMinutes);
			this.Entry(y + RowHeight, "do 'min' nebo 'min,max' lvl:", ExpLevelEntry, gate.expMinLevel + "," + gate.expMaxLevel);
			this.Entry(y + (2 * RowHeight), "range padu exp. 'low,hi' lvlu:", ExpDecayEntry, gate.expDecayLow + "," + gate.expDecayHigh);
			y += 3 * RowHeight;
			this.Check(gate, VarGateFlags.RequiresLevel, y, "Specificky level je vyzadovan.");
			this.Entry(y, "Level 'min' nebo 'min,max':", ReqLevelEntry, gate.reqMinLevel + "," + gate.reqMaxLevel);
			y += RowHeight;
			this.Check(gate, VarGateFlags.Sideways, y, "Mezi-port pres souradnice 10 10");
			y += 28;
			this.AddLabel(14, y, TitleHue, "Odstran Menu:");
			y += RowHeight;
			this.Check(gate, VarGateFlags.RemoveCursor, y, "Odstran kurzor.");
			this.Check(gate, VarGateFlags.RemovePvpTools, y + RowHeight, "Odstran PVP vybavu.");
			this.Check(gate, VarGateFlags.RemoveMount, y + (2 * RowHeight), "Odstran kone.");
			this.Check(gate, VarGateFlags.RemoveBank, y + (3 * RowHeight), "Odstran bank box.");
			this.Menu(gate, VarGateFlags.RemoveItems, y + (4 * RowHeight), "Odstran Object menu.", "Pocet Itemu/Eventu:", gate.Removals.Count, RemoveButton);
			y += 5 * RowHeight;
			y += 8;
			this.AddLabel(14, y, TitleHue, "Pridej Menu:");
			y += RowHeight;
			this.Menu(gate, VarGateFlags.AddItems, y, "Pridej Object menu.", "Pocet Predmetu/Eventu:", gate.Additions.Count, AddButton_);
			this.Menu(gate, VarGateFlags.RunFunctions, y + RowHeight, "Pridej Funkci menu.", "Pocet Funkci:", gate.Functions.Count, FunctionButton);
			this.Menu(gate, VarGateFlags.Guarded, y + (2 * RowHeight), "Pridej Strazce menu.", "Pocet Spawnu:", gate.Guards.Count, GuardButton);
			this.Menu(gate, VarGateFlags.HasPlugins, y + (3 * RowHeight), "Pridej Plugin menu.", "Pocet Pluginu:", gate.hasTimeSeal ? 1 : 0, PluginButton);
			this.OkCancel(466, OkButton);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			VarGate gate = (VarGate) args[0];
			if (gr.PressedButton == CancelButton) {
				player.SysMessage("Nastaveni nebylo ulozeno.");
				return;
			}
			VarGateFlags flags = gate.Flags & VarGateFlags.AddItemTimeLimit;
			foreach (VarGateFlags flag in checkFlags) {
				if (gr.IsSwitched((int) flag)) {
					flags |= flag;
				}
			}
			gate.Flags = flags;
			if (gate.Has(VarGateFlags.ExpGate)) {
				Pair(gr.GetTextResponse(ExpEntry), out gate.expAmount, out gate.expMinutes);
				Pair(gr.GetTextResponse(ExpLevelEntry), out gate.expMinLevel, out gate.expMaxLevel);
				Pair(gr.GetTextResponse(ExpDecayEntry), out gate.expDecayLow, out gate.expDecayHigh);
			}
			if (gate.Has(VarGateFlags.RequiresLevel)) {
				Pair(gr.GetTextResponse(ReqLevelEntry), out gate.reqMinLevel, out gate.reqMaxLevel);
			}
			switch (gr.PressedButton) {
				case PositionButton:
					gate.MoreP = player.P();
					Show(player, gate);
					break;
				case RemoveButton:
					D_VarGateList.Show(player, gate, VarGateList.Remove);
					break;
				case AddButton_:
					D_VarGateList.Show(player, gate, VarGateList.Add);
					break;
				case FunctionButton:
					D_VarGateList.Show(player, gate, VarGateList.Function);
					break;
				case GuardButton:
					D_VarGateGuards.Show(player, gate);
					break;
				case PluginButton:
					D_VarGatePlugins.Show(player, gate);
					break;
			}
		}
	}

	/// <summary>d_varGate_advRemoveItem, d_varGate_advAddItem, d_varGate_advAddFunction.</summary>
	public class D_VarGateList : VarGateDialog {
		private const int Width = 640;
		private const int TimeLimitCheck = 1, TimeLimitEntry = 1;

		public static void Show(Player player, VarGate gate, VarGateList list) {
			player.Dialog(player, SingletonScript<D_VarGateList>.Instance, new DialogArgs(gate, list));
		}

		internal static List<VarGateEntry> Entries(VarGate gate, VarGateList list) {
			switch (list) {
				case VarGateList.Remove:
					return gate.Removals;
				case VarGateList.Add:
					return gate.Additions;
				default:
					return gate.Functions;
			}
		}

		private static string Title(VarGateList list) {
			switch (list) {
				case VarGateList.Remove:
					return "Variable Gate - Odstran Object Menu";
				case VarGateList.Add:
					return "Variable Gate - Pridej Object Menu";
				default:
					return "Variable Gate - Pridej Funkci Menu";
			}
		}

		private static string Describe(VarGateEntry entry, VarGateList list) {
			if (list == VarGateList.Function) {
				return entry.checkOnly ? "zastavuje" : "";
			}
			string amount = entry.amount + "x";
			if (list == VarGateList.Remove) {
				return amount + " " + new[] { "item", "type", "event" }[entry.kind] + (entry.optional ? " jdi" : "") + (entry.checkOnly ? " kontrola" : "");
			}
			return amount + " " + new[] { "item", "event" }[entry.kind] + (entry.checkOnly ? " kontrola" : "");
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			VarGate gate = (VarGate) args[0];
			VarGateList list = (VarGateList) args[1];
			List<VarGateEntry> entries = Entries(gate, list);
			this.Frame(Width, 130 + (entries.Count * RowHeight), Title(list));
			int y = 36;
			this.AddButton(14, y, 0xfbd, 0xfbf, NewButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, y, LabelHue, "Pridej novy radek");
			y += RowHeight;
			if (list == VarGateList.Add) {
				this.AddCheck(14, y, 210, 211, gate.Has(VarGateFlags.AddItemTimeLimit), TimeLimitCheck);
				this.AddLabel(40, y, LabelHue, "Pridej casove omezeni/s:");
				this.AddTextEntry(230, y, 100, RowHeight, EntryHue, TimeLimitEntry, gate.addItemSeconds.ToString(CultureInfo.InvariantCulture));
			}
			y += RowHeight + 4;
			for (int i = 0; i < entries.Count; i++) {
				this.AddButton(14, y, 0xfb4, 0xfb6, EditButton + i, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, TextHue, entries[i].name);
				this.AddLabel(330, y, TextHue, Describe(entries[i], list));
				this.AddButton(600, y, 0xfb1, 0xfb3, DeleteButton + i, GumpButtonType.Reply, 0);
				y += RowHeight;
			}
			this.OkCancel(y + 10, OkButton);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			VarGate gate = (VarGate) args[0];
			VarGateList list = (VarGateList) args[1];
			List<VarGateEntry> entries = Entries(gate, list);
			int button = gr.PressedButton;
			if (button == CancelButton) {
				D_VarGate.Show(player, gate);
				return;
			}
			if (list == VarGateList.Add) {
				if (gr.IsSwitched(TimeLimitCheck)) {
					int seconds = Number(gr.GetTextResponse(TimeLimitEntry), 0);
					if (seconds <= 0) {
						player.RedMessage("Nastavene casove omezeni musi byt kladne cele cislo!");
						Show(player, gate, list);
						return;
					}
					gate.addItemSeconds = seconds;
					gate.Flags |= VarGateFlags.AddItemTimeLimit;
				} else {
					gate.addItemSeconds = 0;
					gate.Flags &= ~VarGateFlags.AddItemTimeLimit;
				}
			}
			if (button == OkButton) {
				D_VarGate.Show(player, gate);
			} else if (button == NewButton) {
				D_VarGateEntry.Show(player, gate, list, -1, new VarGateEntry());
			} else if (button >= DeleteButton) {
				if ((button - DeleteButton) < entries.Count) {
					entries.RemoveAt(button - DeleteButton);
				}
				Show(player, gate, list);
			} else if ((button >= EditButton) && ((button - EditButton) < entries.Count)) {
				D_VarGateEntry.Show(player, gate, list, button - EditButton, entries[button - EditButton]);
			}
		}
	}

	/// <summary>d_varGate_setRemovingItem, d_varGate_setAddingItem, d_varGate_setAddingFunction.</summary>
	public class D_VarGateEntry : VarGateDialog {
		private const int Width = 700;
		private const int NameEntry = 1, AmountEntry = 2, KindEntry = 3, OptionalCheck = 10, CheckOnlyCheck = 11;

		public static void Show(Player player, VarGate gate, VarGateList list, int index, VarGateEntry shown) {
			player.Dialog(player, SingletonScript<D_VarGateEntry>.Instance, new DialogArgs(gate, list, index, shown));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			VarGateList list = (VarGateList) args[1];
			VarGateEntry shown = (VarGateEntry) args[3];
			this.Frame(Width, 180, (list == VarGateList.Function) ? "Variable Gate - Nastav Pridani Funkce Menu" :
				((list == VarGateList.Remove) ? "Variable Gate - Nastav Odstranovany Item Menu" : "Variable Gate - Nastav Pridavany Item Menu"));
			int y = 40;
			this.AddLabel(14, y, LabelHue, (list == VarGateList.Function) ? "Jmeno Function:" : "ItemID / Type / Event:");
			this.AddTextEntry(160, y, 250, RowHeight, EntryHue, NameEntry, shown.name);
			if (list == VarGateList.Function) {
				this.AddCheck(14, y + RowHeight, 210, 211, shown.checkOnly, CheckOnlyCheck);
				this.AddLabel(40, y + RowHeight, TextHue, "Funkce muze zamezit vstupu.");
			} else {
				this.AddTextEntry(420, y, 20, RowHeight, EntryHue, KindEntry, shown.kind.ToString(CultureInfo.InvariantCulture));
				this.AddLabel(445, y, TextHue, (list == VarGateList.Remove) ? "itemdef(0), typedef(1) nebo event(2)" : "itemdef(0) nebo event(1)");
				this.AddLabel(14, y + RowHeight, LabelHue, "Pocet Itemu:");
				this.AddTextEntry(160, y + RowHeight, 250, RowHeight, EntryHue, AmountEntry, shown.amount.ToString(CultureInfo.InvariantCulture));
				this.AddLabel(420, y + RowHeight, TextHue, (list == VarGateList.Remove) ? "0 pro odebrani vseho" : "Dej hraci tento pocet predmetu.");
				if (list == VarGateList.Remove) {
					this.AddCheck(14, y + (2 * RowHeight), 210, 211, shown.optional, OptionalCheck);
					this.AddLabel(40, y + (2 * RowHeight), TextHue, "Lze projit bez odpovidajiciho mnozstvi predmetu.");
				}
				this.AddCheck(14, y + (3 * RowHeight), 210, 211, shown.checkOnly, CheckOnlyCheck);
				this.AddLabel(40, y + (3 * RowHeight), TextHue, (list == VarGateList.Remove) ? "Jen kontrola: gate nepusti ty, kteri maji vice nez tento pocet." :
					"Jen kontrola: nikdo nic nedostane, gate nepusti ty, kteri maji mene.");
			}
			this.OkCancel(150, OkButton);
		}

		/// <summary>What Erebor's button handlers checked, and names, which it left to fail at the gate.</summary>
		private static string Problem(VarGateEntry entry, VarGateList list) {
			if (list == VarGateList.Function) {
				return (ScriptHolder.GetFunction(entry.name) == null) ? "Takova funkce neexistuje!" : null;
			}
			int kinds = (list == VarGateList.Remove) ? 2 : 1;
			if ((entry.kind < 0) || (entry.kind > kinds)) {
				return "Hodnota za jmenem objektu musi byt v rozmezi 0-" + kinds + "!";
			}
			if ((list == VarGateList.Add) && (entry.amount == 0)) {
				return "Musis nastavit pocet pridavanych itemu !";
			}
			bool isItem = entry.kind == 0;
			if (isItem) {
				return (ThingDef.GetByDefname(entry.name) is ItemDef) ? null : "Zadane ID neodpovida zadnemu predmetu ! Oprav jmeno itemu.";
			}
			return (TriggerGroup.GetByDefname(entry.name) == null) ? "Takovy typedef nebo event neexistuje!" : null;
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			VarGate gate = (VarGate) args[0];
			VarGateList list = (VarGateList) args[1];
			int index = (int) args[2];
			if (gr.PressedButton == CancelButton) {
				D_VarGateList.Show(player, gate, list);
				return;
			}
			VarGateEntry entry = new VarGateEntry();
			entry.name = (gr.GetTextResponse(NameEntry) ?? "").Trim();
			entry.amount = Number(gr.GetTextResponse(AmountEntry), 0);
			entry.kind = Number(gr.GetTextResponse(KindEntry), -1);
			if (list == VarGateList.Function) {
				entry.kind = 0;
			}
			entry.optional = gr.IsSwitched(OptionalCheck);
			entry.checkOnly = gr.IsSwitched(CheckOnlyCheck);
			string problem = Problem(entry, list);
			if (problem != null) {
				player.RedMessage(problem);
				Show(player, gate, list, index, entry);
				return;
			}
			List<VarGateEntry> entries = D_VarGateList.Entries(gate, list);
			if ((index >= 0) && (index < entries.Count)) {
				entries[index] = entry;
			} else {
				entries.Add(entry);
			}
			D_VarGateList.Show(player, gate, list);
		}
	}

	/// <summary>d_guarditAdv: the spawns of the gate's guards. A target, not Erebor's typed uid.</summary>
	public class D_VarGateGuards : VarGateDialog {
		private const int GoButton = 100;

		public static void Show(Player player, VarGate gate) {
			player.Dialog(player, SingletonScript<D_VarGateGuards>.Instance, new DialogArgs(gate));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			VarGate gate = (VarGate) args[0];
			gate.Guards.RemoveAll(gem => (gem == null) || gem.IsDeleted);
			this.Frame(366, 110 + (gate.Guards.Count * RowHeight), "Advanced Guard System");
			int y = 36;
			this.AddButton(14, y, 0xfbd, 0xfbf, NewButton, GumpButtonType.Reply, 0);
			this.AddLabel(50, y, LabelHue, "Pridej spawn (zamer ho)");
			y += RowHeight + 4;
			for (int i = 0; i < gate.Guards.Count; i++) {
				this.AddLabel(14, y, TextHue, "Spawn " + i + ": " + gate.Guards[i].Name);
				this.AddButton(280, y, 0xfa5, 0xfa7, GoButton + i, GumpButtonType.Reply, 0);
				this.AddButton(320, y, 0xfb1, 0xfb3, DeleteButton + i, GumpButtonType.Reply, 0);
				y += RowHeight;
			}
			this.OkCancel(y + 10, OkButton);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			VarGate gate = (VarGate) args[0];
			int button = gr.PressedButton;
			if (button == NewButton) {
				player.Target(SingletonScript<Targ_VarGateGuard>.Instance, gate);
			} else if (button >= DeleteButton) {
				if ((button - DeleteButton) < gate.Guards.Count) {
					gate.Guards.RemoveAt(button - DeleteButton);
				}
				Show(player, gate);
			} else if (button >= GoButton) {
				if ((button - GoButton) < gate.Guards.Count) {
					player.Go(gate.Guards[button - GoButton].P());
				}
				Show(player, gate);
			} else {
				D_VarGate.Show(player, gate);
			}
		}
	}

	public class Targ_VarGateGuard : CompiledTargetDef {
		private static readonly TagKey spawnedCountTK = TagKey.Acquire("spawnedCount");

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			VarGate gate = (VarGate) parameter;
			if (targetted.HasTag(spawnedCountTK)) {
				gate.Guards.Add(targetted);
			} else {
				self.RedMessage("To neni spawn.");
			}
			D_VarGateGuards.Show(self, gate);
			return TargetResult.Done;
		}
	}

	/// <summary>d_varGate_advAddPlugin. The Time Seal is the one plugin Erebor finished.</summary>
	public class D_VarGatePlugins : VarGateDialog {
		public static void Show(Player player, VarGate gate) {
			player.Dialog(player, SingletonScript<D_VarGatePlugins>.Instance, new DialogArgs(gate));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			VarGate gate = (VarGate) args[0];
			this.Frame(420, 130, "Variable Gate - Pridej Plugin Menu");
			int y = 40;
			if (gate.hasTimeSeal) {
				this.AddButton(14, y, 0xfb4, 0xfb6, EditButton, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, TextHue, "Time Seal");
				this.AddButton(380, y, 0xfb1, 0xfb3, DeleteButton, GumpButtonType.Reply, 0);
			} else {
				this.AddButton(14, y, 0xfbd, 0xfbf, NewButton, GumpButtonType.Reply, 0);
				this.AddLabel(50, y, LabelHue, "Pridej Time Seal");
			}
			this.OkCancel(96, OkButton);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			VarGate gate = (VarGate) args[0];
			switch (gr.PressedButton) {
				case NewButton:
					gate.hasTimeSeal = true;
					gate.sealDelay = 10;
					gate.sealLength = 60;
					gate.sealMaxPlayers = 0;
					gate.sealSameRealm = false;
					D_VarGateTimeSeal.Show(player, gate);
					break;
				case EditButton:
					D_VarGateTimeSeal.Show(player, gate);
					break;
				case DeleteButton:
					gate.hasTimeSeal = false;
					gate.ResetTimeSeal();
					Show(player, gate);
					break;
				default:
					D_VarGate.Show(player, gate);
					break;
			}
		}
	}

	/// <summary>d_vg_timeSeal.</summary>
	public class D_VarGateTimeSeal : VarGateDialog {
		private const int HelpButton = 2, ResetButton = 3, LengthEntry = 1, DelayEntry = 2, PlayersEntry = 3, SameRealmCheck = 1;

		private const string Help = "Doba zablokovani /s: Je doba, po jakou bude gate nepruchozi. Od kdy bude gate nepruchozi zavisi na dalsim nastaveni<br><br>" +
			"Cas na pruchod /s: Je doba v sekundach, po kterou mohou hraci zkouset projit branou, nez se zcela uzavre. Tento cas se pocita od chvile vstupu prvniho hrace.<br><br>" +
			"Pocet hracu: Je fakticky pocet hracu, kteri mohou v dobe otevreni brany projit za sebou. Nulova hodnota udava neomezeny pocet hracu.<br><br>" +
			"Stejny Realm: Pokud je prislusny box zaskrtly, projdou dale jen ti hraci, kteri jsou stejneho realmu jako prvni.<br>" +
			"Tlacitko v pravem dolnim rohu resetuje timer.";

		public static void Show(Player player, VarGate gate) {
			player.Dialog(player, SingletonScript<D_VarGateTimeSeal>.Instance, new DialogArgs(gate, false));
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			VarGate gate = (VarGate) args[0];
			bool help = (bool) args[1];
			this.Frame(300, help ? 400 : 190, "VarGate's Time Seal plugin");
			this.AddButton(265, 12, 0xfa5, 0xfa7, HelpButton, GumpButtonType.Reply, 0);
			int y = 40;
			this.AddLabel(14, y, LabelHue, "Doba zablokovani /s:");
			this.AddTextEntry(170, y, 80, RowHeight, EntryHue, LengthEntry, gate.sealLength.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(14, y + RowHeight, LabelHue, "Cas na pruchod /s:");
			this.AddTextEntry(170, y + RowHeight, 80, RowHeight, EntryHue, DelayEntry, gate.sealDelay.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(14, y + (2 * RowHeight), LabelHue, "Pocet hracu:");
			this.AddTextEntry(170, y + (2 * RowHeight), 80, RowHeight, EntryHue, PlayersEntry, gate.sealMaxPlayers.ToString(CultureInfo.InvariantCulture));
			this.AddLabel(14, y + (3 * RowHeight), LabelHue, "Stejny Realm:");
			this.AddCheck(170, y + (3 * RowHeight), 210, 211, gate.sealSameRealm, SameRealmCheck);
			this.OkCancel(150, OkButton);
			this.AddButton(255, 150, 0xfbd, 0xfbf, ResetButton, GumpButtonType.Reply, 0);
			if (help) {
				this.AddHtml(14, 185, 272, 200, Help, true, true);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			VarGate gate = (VarGate) args[0];
			switch (gr.PressedButton) {
				case CancelButton:
					D_VarGatePlugins.Show(player, gate);
					return;
				case HelpButton:
					player.Dialog(player, this, new DialogArgs(gate, true));
					return;
				case ResetButton:
					gate.ResetTimeSeal();
					Show(player, gate);
					return;
			}
			int length = Number(gr.GetTextResponse(LengthEntry), -1);
			int delay = Number(gr.GetTextResponse(DelayEntry), -1);
			int players = Number(gr.GetTextResponse(PlayersEntry), -1);
			gate.sealSameRealm = gr.IsSwitched(SameRealmCheck);
			bool failed = false;
			if (length > 0) {
				gate.sealLength = length;
			} else {
				player.RedMessage("Doba zablokovani musi byt kladne cele cislo!");
				failed = true;
			}
			if (delay >= 0) {
				gate.sealDelay = delay;
			} else {
				player.RedMessage("Cas na pruchod nesmi byt zaporny!");
				failed = true;
			}
			if (players >= 0) {
				gate.sealMaxPlayers = players;
			} else {
				player.RedMessage("Pocet hracu nesmi byt zaporny!");
				failed = true;
			}
			if (failed) {
				Show(player, gate);
			} else {
				D_VarGatePlugins.Show(player, gate);
			}
		}
	}
}
