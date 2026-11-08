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
			if ((player == null) || (gem == null) || npc.Flag_Dead || player.Flag_Dead) {
				return false;
			}
			if (Fever.TryCure(npc, player)) {
				return true; //#1286 - a sick resident: the cure, not the talk
			}
			if (Errands.CanTalk(player, npc)) {
				Open(player, npc);
			}
			return true;
		}

		internal static void Open(Player player, NPC npc) {
			D_ResidentTalk dialog = SingletonScript<D_ResidentTalk>.Instance;
			player.DialogClose(dialog, 0);
			player.Dialog(player, dialog, new DialogArgs(npc));
		}

		/// <summary>#1280 - whether the resident serves in Barad-dur's war camp.</summary>
		internal static bool IsCamp(Item gem) {
			CityVenue work = (gem == null) ? null : Residents.WorkplaceOf(gem);
			return (work != null) && WarCamp.IsCamp(work.City);
		}

		internal static string Greeting(Player player, Item gem) {
			int friendship = Residents.Friendship(player, Residents.IdOf(gem));
			if (IsCamp(gem)) {
				return WarCamp.Greeting(player, friendship); //#1280 - a superior's favour, not a friend's welcome
			}
			if (friendship >= 7) {
				return "Priteli! " + player.Name + ", rad te vidim. Pojd dal.";
			}
			if (friendship >= 3) {
				return "Zdravim te, " + player.Name + ". Co te ke mne privadi?";
			}
			return CityFlavour.StrangerGreeting(CityFlavour.CityOf(gem)) ?? ErrandContext.Pick("Co si prejes?", "Ano? Potrebujes neco?", "Dobry den. Jak ti mohu pomoci?"); //#1282
		}
	}

	/// <summary>Buttons: 1 asks, 0 goes; a step's line is FirstStepButton + its errand's id * 10 + the answer, so a button still
	/// names the right errand when the list changed while the dialog was open; a venue's offers (#1254) are VenueActions' 50-99.</summary>
	public class D_ResidentTalk : CompiledGumpDef {
		internal const int AskButton = 1, FirstStepButton = 100;
		private const int Width = 380, TitleHue = 95, TextHue = 955, SayHue = 68, OfferHue = 53, Row = 24;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			NPC npc = (NPC) args[0];
			Item gem = Residents.GemOf(npc);
			List<Errand> here = Errands.At(player, gem);
			int lines = 0;
			foreach (Errand errand in here) {
				lines += (errand.Current.Kind == ErrandStepKind.Choose) ? errand.Current.choices.Count : 1;
			}
			List<VenueAction> offers = VenueActions.For(player, npc, gem);
			lines += offers.Count;
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
				int first = FirstStepButton + (here[i].id * 10);
				if (step.Kind == ErrandStepKind.Choose) {
					for (int c = 0; c < step.choices.Count; c++) {
						this.AddButton(16, y, 0xFA5, 0xFA7, first + c, GumpButtonType.Reply, 0);
						this.AddLabel(52, y + 2, SayHue, step.choices[c]);
						y += Row;
					}
				} else {
					this.AddButton(16, y, 0xFA5, 0xFA7, first, GumpButtonType.Reply, 0);
					this.AddLabel(52, y + 2, SayHue, string.IsNullOrEmpty(step.say) ? here[i].name : step.say);
					y += Row;
				}
			}
			foreach (VenueAction offer in offers) {
				this.AddButton(16, y, 0xFA5, 0xFA7, VenueActions.ButtonOf(offer), GumpButtonType.Reply, 0);
				this.AddLabel(52, y + 2, OfferHue, offer.Label);
				y += Row;
			}
			this.AddButton(16, y, 0xFA5, 0xFA7, AskButton, GumpButtonType.Reply, 0);
			this.AddLabel(52, y + 2, TextHue, ResidentTalk.IsCamp(gem) ? "Rozkaz?" : "Potrebujes neco?"); //#1280 - orders in the camp
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
			if ((gem == null) || !Errands.CanTalk(player, npc)) {
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
				D_ErrandOffer offer = SingletonScript<D_ErrandOffer>.Instance;
				player.DialogClose(offer, 0); //one offer at a time: its errand travels in its own args
				player.Dialog(player, offer, new DialogArgs(npc, errand));
				return;
			}
			if (VenueActions.IsButton(button)) {
				VenueAction action = VenueActions.ByButton(button, player, npc, gem);
				if (action != null) {
					action.Do(player, npc, gem);
				}
				return;
			}
			if (button >= FirstStepButton) {
				Errand errand = Errands.ById(player, (button - FirstStepButton) / 10);
				int choice = (button - FirstStepButton) % 10;
				if ((errand != null) && (errand.Current != null)) {
					bool isChoice = errand.Current.Kind == ErrandStepKind.Choose;
					Errands.Do(player, errand, npc, isChoice ? choice : -1);
				}
			}
		}
	}

	/// <summary>The giver's offer: what they want, and whether the player takes it. args: the giver, the errand offered (made, not
	/// taken; it lives while this dialog does and is never saved).</summary>
	public class D_ErrandOffer : CompiledGumpDef {
		internal const int AcceptButton = 1, RefuseButton = 2;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			Errand errand = args[1] as Errand;
			this.SetLocation(60, 60);
			this.AddPage(0);
			this.AddBackground(0, 0, 380, 220, 2620);
			this.AddLabel(16, 12, 95, (errand == null) ? "" : errand.name);
			this.AddHtml(16, 40, 348, 120, "<basefont color=#FFFFFF>" + ((errand == null) ? "" : errand.offer) + "</basefont>", false, true);
			bool order = (errand != null) && WarCamp.IsCamp(errand.city);
			this.AddButton(16, 175, 0xFA5, 0xFA7, AcceptButton, GumpButtonType.Reply, 0);
			this.AddLabel(52, 177, 68, order ? "Rozumim." : "Rad pomuzu.");
			if (order) { //#1280 - an order can be refused, at a price; closing the dialog only puts it off
				this.AddButton(200, 175, 0xFB1, 0xFB3, RefuseButton, GumpButtonType.Reply, 0);
				this.AddLabel(236, 177, 955, "Odmitnout.");
			} else {
				this.AddButton(200, 175, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
				this.AddLabel(236, 177, 955, "Ted ne.");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			NPC npc = args[0] as NPC;
			if (player == null) {
				return;
			}
			Errand errand = args[1] as Errand;
			if ((errand != null) && (gr.PressedButton == RefuseButton) && WarCamp.IsCamp(errand.city)) {
				WarCamp.OnRefused(player, errand.city);
				if ((npc != null) && !npc.IsDeleted) {
					npc.Say("Tak ty odmitas rozkaz? To si zapamatuju.");
				}
				return;
			}
			if ((errand == null) || (gr.PressedButton != AcceptButton) || !Errands.CanTalk(player, npc)) {
				return;
			}
			if (!Errands.Accept(player, errand)) {
				player.SysMessage("Ukol uz nelze prijmout.");
			} else if ((npc != null) && !npc.IsDeleted) {
				npc.Say(WarCamp.IsCamp(errand.city) ? "Hni sebou." : "Diky. Spoleham na tebe.");
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
			for (int i = 0; (i < active.Count) && (i < 3); i++) { //three fit the window; the rest show once these are done
				Errand errand = active[i];
				ErrandStep step = errand.Current;
				int minutes = Math.Max(0, (int) (errand.Expires - Errands.Now).TotalMinutes);
				gump.AddLabel(85, y, HeadHue, CityJournalPage.Cut(errand.name, 20) + " (" + (errand.index + 1).ToString(CultureInfo.InvariantCulture) + "/" +
					errand.steps.Count.ToString(CultureInfo.InvariantCulture) + ")");
				gump.AddHtml(85, y + 18, width - 100, 36, "<basefont color=#FFFFFF size=2>" + ((step == null) ? "" : step.task) + "</basefont>", false, false);
				gump.AddLabel(85, y + 54, WhereHue, CityJournalPage.Cut((step == null) ? "" : Errands.Where(step), 24));
				gump.AddLabel(width - 40, y, TextHue, minutes.ToString(CultureInfo.InvariantCulture) + "m");
				gump.AddButton(width - 10, y + 20, 0x15E1, 0x15E5, FirstButton + (errand.id * 2), GumpButtonType.Reply, 0); //the arrow
				gump.AddButton(width - 10, y + 48, 0xFB1, 0xFB3, FirstButton + (errand.id * 2) + 1, GumpButtonType.Reply, 0); //give up
				y += Block;
			}
		}

		private static void OnButton(Player player, int button) {
			Errand errand = Errands.ById(player, (button - FirstButton) / 2);
			if (errand != null) {
				if (((button - FirstButton) % 2) == 0) {
					Errands.PointTo(player, errand);
				} else {
					Errands.GiveUp(player, errand);
				}
			}
			QuestJournal.Open(player, D_QuestJournal.SectionErrands);
		}
	}
}
