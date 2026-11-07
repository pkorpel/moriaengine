using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1250 - a resident's dialog on a double click: a greeting by friendship, a line for each errand step the player
	/// has with them, "Potrebujes neco?", and goodbye.</summary>
	public static class ResidentTalk {
		/// <summary>From Character.On_DClick: true when the NPC is a resident and the dialog opened.</summary>
		public static bool TryDClick(NPC npc, Character from) {
			Player player = from as Player;
			Item gem = (npc == null) ? null : Residents.GemOf(npc);
			if ((player == null) || (gem == null) || npc.Flag_Dead) {
				return false;
			}
			Open(player, npc);
			return true;
		}

		internal static void Open(Player player, NPC npc) {
			D_ResidentTalk dialog = SingletonScript<D_ResidentTalk>.Instance;
			player.DialogClose(dialog, 0);
			player.Dialog(player, dialog, new DialogArgs(npc));
		}

		internal static string Greeting(Player player, Item gem) {
			int friendship = Residents.Friendship(player, Residents.IdOf(gem));
			if (friendship >= 7) {
				return "Priteli! " + player.Name + ", rad te vidim. Pojd dal.";
			}
			if (friendship >= 3) {
				return "Zdravim te, " + player.Name + ". Co te ke mne privadi?";
			}
			return ErrandContext.Pick("Co si prejes?", "Ano? Potrebujes neco?", "Dobry den. Jak ti mohu pomoci?");
		}
	}

	public class D_ResidentTalk : CompiledGumpDef {
		internal const int AskButton = 1, FirstStepButton = 100;
		private const int Width = 380, TitleHue = 95, TextHue = 955, SayHue = 68, Row = 24;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			NPC npc = (NPC) args[0];
			Item gem = Residents.GemOf(npc);
			List<Errand> here = Errands.At(player, gem);
			int lines = 0;
			foreach (Errand errand in here) {
				lines += (errand.Current.Kind == ErrandStepKind.Choose) ? errand.Current.choices.Count : 1;
			}
			int height = 120 + ((lines + 2) * Row);
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 2620);
			CityVenue work = Residents.WorkplaceOf(gem);
			this.AddLabel(16, 12, TitleHue, Residents.NameOf(gem) + ((work == null) ? "" : ", " + work.Label));
			this.AddHtml(16, 40, Width - 32, 50, "<basefont color=#FFFFFF>" + ResidentTalk.Greeting(player, gem) + "</basefont>", false, false);
			int y = 100;
			for (int i = 0; i < here.Count; i++) {
				ErrandStep step = here[i].Current;
				if (step.Kind == ErrandStepKind.Choose) {
					for (int c = 0; c < step.choices.Count; c++) {
						this.AddButton(16, y, 0xFA5, 0xFA7, FirstStepButton + (i * 10) + c, GumpButtonType.Reply, 0);
						this.AddLabel(52, y + 2, SayHue, step.choices[c]);
						y += Row;
					}
				} else {
					this.AddButton(16, y, 0xFA5, 0xFA7, FirstStepButton + (i * 10), GumpButtonType.Reply, 0);
					this.AddLabel(52, y + 2, SayHue, string.IsNullOrEmpty(step.say) ? here[i].name : step.say);
					y += Row;
				}
			}
			this.AddButton(16, y, 0xFA5, 0xFA7, AskButton, GumpButtonType.Reply, 0);
			this.AddLabel(52, y + 2, TextHue, "Potrebujes neco?");
			y += Row;
			this.AddButton(16, y, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(52, y + 2, TextHue, "Sbohem.");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC npc = args[0] as NPC;
			int button = gr.PressedButton;
			if ((player == null) || (npc == null) || npc.IsDeleted || (button == 0)) {
				return;
			}
			Item gem = Residents.GemOf(npc);
			if (gem == null) {
				return;
			}
			if (button == AskButton) {
				CityVenue work = Residents.WorkplaceOf(gem);
				string why = Errands.WhyNot(player, gem, (work == null) ? null : work.City);
				Errand errand = (why == null) ? Errands.Make(player, gem) : null;
				if (errand == null) {
					npc.Say(why ?? "Ted pro tebe nic nemam.");
					return;
				}
				Errands.SetOffer(player, errand);
				player.Dialog(player, SingletonScript<D_ErrandOffer>.Instance, new DialogArgs(npc));
				return;
			}
			if (button >= FirstStepButton) {
				List<Errand> here = Errands.At(player, gem);
				int index = (button - FirstStepButton) / 10;
				int choice = (button - FirstStepButton) % 10;
				if (index < here.Count) {
					Errand errand = here[index];
					bool isChoice = errand.Current.Kind == ErrandStepKind.Choose;
					Errands.Do(player, errand, npc, isChoice ? choice : -1);
				}
			}
		}
	}

	/// <summary>The giver's offer: what they want, and whether the player takes it.</summary>
	public class D_ErrandOffer : CompiledGumpDef {
		internal const int AcceptButton = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			NPC npc = (NPC) args[0];
			Errand errand = Errands.PeekOffer(player);
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 380, 220, 2620);
			this.AddLabel(16, 12, 95, (errand == null) ? "" : errand.name);
			this.AddHtml(16, 40, 348, 120, "<basefont color=#FFFFFF>" + ((errand == null) ? "" : errand.offer) + "</basefont>", false, true);
			this.AddButton(16, 175, 0xFA5, 0xFA7, AcceptButton, GumpButtonType.Reply, 0);
			this.AddLabel(52, 177, 68, "Rad pomuzu.");
			this.AddButton(200, 175, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
			this.AddLabel(236, 177, 955, "Ted ne.");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC npc = args[0] as NPC;
			if (player == null) {
				return;
			}
			Errand errand = Errands.TakeOffer(player);
			if ((errand == null) || (gr.PressedButton != AcceptButton)) {
				return;
			}
			if (!Errands.Accept(player, errand)) {
				player.SysMessage("Ukol uz nelze prijmout.");
			} else if ((npc != null) && !npc.IsDeleted) {
				npc.Say("Diky. Spoleham na tebe.");
			}
		}
	}

	/// <summary>The journal's Ukoly tab: each taken errand with its giver, the step it's at, whom to see and where, the time
	/// left; a button for the quest arrow and one to give it up (design section 8).</summary>
	public static class ErrandJournalPage {
		internal const int FirstButton = QuestJournalPages.FirstButton;
		private const int HeadHue = 95, TextHue = 955, WhereHue = 68, Block = 82;

		public static void Bootstrap() {
			QuestJournalPages.Register(D_QuestJournal.SectionErrands, Add, OnButton);
		}

		private static void Add(CompiledGumpDef gump, Player player, int width, int height) {
			List<Errand> active = Errands.Active(player);
			if (active.Count == 0) {
				gump.AddHtml(80, height / 2, width - 80, 40, "<center><basefont color=#00F0FA>Ukoly ti daji lide ve meste. Mluv s nimi.</basefont></center>", false, false);
				return;
			}
			int y = 135;
			for (int i = 0; i < active.Count; i++) {
				Errand errand = active[i];
				ErrandStep step = errand.Current;
				int minutes = Math.Max(0, (int) (errand.Expires - Errands.Now).TotalMinutes);
				gump.AddLabel(85, y, HeadHue, CityJournalPage.Cut(errand.name, 20) + " (" + (errand.index + 1).ToString(CultureInfo.InvariantCulture) + "/" +
					errand.steps.Count.ToString(CultureInfo.InvariantCulture) + ")");
				gump.AddHtml(85, y + 18, width - 90, 36, "<basefont color=#FFFFFF size=2>" + ((step == null) ? "" : step.task) + "</basefont>", false, false);
				gump.AddLabel(85, y + 54, WhereHue, CityJournalPage.Cut((step == null) ? "" : Errands.Where(step), 24));
				gump.AddLabel(width - 40, y, TextHue, minutes.ToString(CultureInfo.InvariantCulture) + "m");
				gump.AddButton(width - 10, y + 20, 0x15E1, 0x15E5, FirstButton + (i * 2), GumpButtonType.Reply, 0); //the arrow
				gump.AddButton(width - 10, y + 48, 0xFB1, 0xFB3, FirstButton + (i * 2) + 1, GumpButtonType.Reply, 0); //give up
				y += Block;
			}
		}

		private static void OnButton(Player player, int button) {
			List<Errand> active = Errands.Active(player);
			int index = (button - FirstButton) / 2;
			if (index < active.Count) {
				if (((button - FirstButton) % 2) == 0) {
					Errands.PointTo(player, active[index]);
				} else {
					Errands.GiveUp(player, active[index]);
				}
			}
			QuestJournal.Open(player, D_QuestJournal.SectionErrands);
		}
	}
}
