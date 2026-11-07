/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

//The texts are Erebor's (questy/q006_erioluv_vnuk.scp, q023_kouzelna_dyka.scp, q007_HaradirovoSelatko.scp), with its own markup.

namespace SteamEngine.CompiledScripts {

	/// <summary>#1264 - d_q006_eriol, page for page. Erebor's q026 (healers' hcn poison) and q000 promotion lines aren't here: q026
	/// is step 18 of #1237, and the Grandmaster promotion is the envoys' (GrandmasterQuest).</summary>
	public class D_Q006Eriol : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3070;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int q = Q006.GetStage(player);
			int alchemy = Q006.Alchemy(player);
			if (q == 0) {
				d.BaseButton(1);
			} else {
				d.BaseButton(1, 35, "Povidej si");
			}
			d.BaseButton(3);
			d.BaseButton(5);
			d.BaseButton(6);

			d.Page("Pozdrav");
			if (q == 0) {
				d.SetText("Bud <src.sex(\"zdrav, hrdino\",\"zdrava, hrdinko\")>.");
			} else if (q == 4) {
				d.SetText("Ahoj! Diky tobe jsem chlapce nalezl!");
			} else if (q == 6) {
				d.SetText("Ten lektvar od tebe nepomohl!");
			} else if (q == 8) {
				d.SetText("Posledni lekvar jiz zabral, diky bohu. Vnukovi je trochu lepe.");
			} else if (q == 10) {
				d.SetText("Chlapci je ted mnohem lepe, dekuji.");
			} else {
				d.SetText("Rad te vidim.");
			}
			d.Html();
			if (player.IsGM) {
				d.SetText("*GM info*");
				d.Response(false, false, "q006_GM_Info", true);
				d.Page("q006_GM_Info");
				d.SetText("Stav: " + q + ". 1: 10 pochodni; 3: zmenseny pes (c_dog); 5: 3 i_potion_cureless; 7: 10 i_potion_curegreat; 9: plny " + Q006.ThighBag
					+ " + 3 i_potion_refreshgreater; 11: Qworet; 13: Unas (level " + Q006.UnasMinLevel + "); 14-17: Unasovy vacky; 18: pergamen (tag) Eriolovi; "
					+ "19: hospodsky; 21: 2 i_srdiczko, 1 i_bylina, 3 i_potion_curegreat; 22: konec bez roby (bez Alchemy 100); 23: ceka na i_potion_ocisteni; 24: konec s robou.");
				d.Html();
			}

			if (q == 0) {
				d.Page("Kdo jsi?");
				d.SetText("Jmenuji se <name>, jsem mistni ranhojic. Omlouvam se za spech, ale prave pomaham sve dceri nalezt jejiho ztraceneho syna.");
				d.Html();
				d.SetText("Pokracuj");
				d.Response(false, false, "gq006_hearBeginingStory");
			} else {
				d.Page("Povidej si");
				if (q == 2) {
					d.SetText("Dekuji ti za pochodne, ale hodil by se mi jeste pes, aby mi pomohl chlapce najit. Nejlepsi by byl cerstve ochoceny, vycviceny a magicky zmenseny. Ne zadny toulavy pes z okoli.");
					d.Html();
					d.SetText("Hmm, pokusim se tedy sehnat psa.");
					d.Response(false, true, "gq006_acceptSubquest_2");
				} else if (q == 4) {
					d.SetText("Chlapec je cely bledy. Nevim, zda bude v poradku. Snad by pomohly tri slabsi protijedy.");
					d.Html();
					d.SetText("Hned pro ne zajdu.");
					d.Response(false, true, "gq006_acceptSubquest_3");
				} else if (q == 6) {
					d.SetText("Chlapcuv stav se rychle zhorsuje. Neni cas na experimenty, prines mi nejsilnejsi protijed, co sezenes a radsi rovnou deset kusu!");
					d.Html();
					d.SetText("Bezim pro ne.");
					d.Response(false, true, "gq006_acceptSubquest_4");
				} else if (q == 8) {
					d.SetText("Chlapcuv stav se zlepsil, ale je stale velmi slaby. Prines mi prosim deset cerstvych stehen z grizliho do tohto batohu a tri silne lektvary osvezeni.");
					d.Html();
					d.SetText("Pokusim se to sehnat");
					d.Response(false, true, "gq006_acceptSubquest_5");
				} else if (q == 10) {
					d.SetText("Chlapec se zda byt v poradku, ale neustale opakuje neco o prizracich. Nevim, co ma na mysli. Mluvi velmi zmatene. Promluv si o tom prosim se samanem Qworetem. Najdes ho nekde pobliz Osamele Hory a Dolu.");
					d.Html();
					d.SetText("Jdu za nim.");
					d.Response(false, true, "gq006_acceptSubquest_6");
				} else if (q == 11) {
					d.SetText("Poslal jsem te za Qworetem, uz jsi za nim <src.sex(\"byl\",\"byla\")>?");
					d.Html();
				} else if (q == 12) {
					d.SetText("Tak povidej, co ti Qworet rekl?");
					d.Html();
					d.SetText("*povedet...*");
					d.Response(false, false, "gq006_toldQworetStory", true);
				} else if ((q >= 13) && (q < 18)) {
					d.SetText("Na co cekas? Poslal jsem te za Unasem.");
					d.Html();
				} else if (q == 18) {
					if (Q006.HasParchment(player)) {
						d.SetText("Tak povidej, co ti Unas sdelil...");
						d.Html();
						d.SetText("*podat pergamen a povedet...*");
						d.Response(false, false, "gq006_givePergamen", true);
					} else {
						d.SetText("Na co cekas? Poslal jsem te za Unasem.");
						d.Html();
					}
				} else if (q == 19) {
					d.SetText("Na co cekas? Poslal jsem te za hospodskym.");
					d.Html();
				} else if (q == 20) {
					d.SetText("Tak povidej, co rekl hospodsky?");
					d.Html();
					d.SetText("Lektvar Ocisteni...");
					d.Response(false, false, "gq006_tellAboutPotion");
				} else if (q == 22) {
					if (alchemy < 1000) {
						d.SetText("Prisady na lektvar jsi mi uz <src.sex(\"prinesl\",\"prinesla\")>. No, jeste nejsi dostatecne <src.sex(\"zrucny\",\"zrucna\")> na jeho zhotoveni. Bohuzel, pokud neovladas alchemii, budu muset pockat na nekoho jineho.");
						d.Html();
					} else {
						d.SetText("Vyborne! Vidim, ze uz jsi dostatecne <src.sex(\"zrucny\",\"zrucna\")> na vyrobu lektvaru. Tady, vezmi si prisady a udelej Lektvar Ocisteni.");
						d.Html();
						d.SetText("*vzit prisady*");
						d.Response(false, true, "gq006_takePurityPotionResources"); //Erebor's response led to a page it never wrote, so it closed
					}
				} else if (q == Q006.Done) {
					d.SetText("Rad bych si s tebou povidal, ale ted se musim venovat svemu vnukovi.");
					d.Html();
				} else {
					d.SetText("Ted nemam cas si povidat!");
					d.Html();
				}
			}

			d.Page("Obchodovat");
			if ((q == Q006.Done) && (alchemy > 999)) {
				d.SetText("Mohu ti prodat novou robu za 2 000 000 zlataku.");
				d.Html();
				d.SetText("To beru!");
				d.Response(false, true, "gq006_acceptRobe");
			} else {
				d.SetText("Promin, ale nemam ti co nabidnout.");
				d.Html();
			}

			d.Page("Nesu ti");
			switch (q) {
				case 1:
					Ask(d, "Mas ty pochodne?", Q006.Count(player, "i_torch") >= 10, "Mam je.", "gq006_haveSubQuestResources_1", "Bohuzel jeste ne.");
					break;
				case 3:
					Ask(d, "Tak co? Podarilo se ti sehnat mi psa?", Q006.FindDog(player) != null, "Ano, mam ho.", "gq006_haveSubQuestResources_2", "Bohuzel jeste ne");
					break;
				case 5:
					Ask(d, "Tak co?!? Povidej! Mas ty protijedy?", Q006.Count(player, "i_potion_cureless") >= 3, "Ano, mam je.", "gq006_haveSubQuestResources_3", "Bohuzel jeste ne");
					break;
				case 7:
					Ask(d, "Tak co?!? Povidej! Mas ty protijedy?", Q006.Count(player, "i_potion_curegreat") >= 10, "Ano, mam je.", "gq006_haveSubQuestResources_4", "Bohuzel jeste ne");
					break;
				case 9:
					Ask(d, "Mas vse, co jsem po tobe chtel?", HasThighsAndRefresh(player), "Ano, mam to.", "gq006_haveSubQuestResources_5", "Bohuzel jeste ne");
					break;
				case 21:
					Ask(d, "Mas ty prisady?", Q006.HasPotionIngredients(player), "Ano, mam to.", "gq006_haveSubQuestResources_7", "Bohuzel jeste ne");
					break;
				case 23:
					Ask(d, "Uz jsi <src.sex(\"namichal\",\"namichala\")> ten lektvar?", Q006.Count(player, "i_potion_ocisteni") >= 1, "Ano, tady je.", "gq006_havePurityPotion", "Jeste ne, delam na tom.");
					break;
				default:
					d.SetText("Co pro me mas ?");
					d.Html();
					break;
			}

			d.Page("Prohlednout");
			string look = "<def_questDialog_colorTextEmote>*Vidis vousateho starce v spinave robe, ktera na sobe nese znamky letiteho noseni.";
			if (q == 4) {
				look += " Vypada ustarane.";
			} else if (q == 6) {
				look += " Vypada velmi ustarane.";
			} else if ((q == 10) || (q == 11)) {
				look += " Vypada velmi zamyslene.";
			} else if ((q == 18) || (q == 19)) {
				look += " Tvari se nedockave.";
			}
			d.SetText(look + "*");
			d.Html();

			this.BuildAnswers(d, player, q, alchemy);
		}

		private static bool HasThighsAndRefresh(Player player) {
			return (Q006.Count(player, "i_potion_refreshgreater") >= 3) && Collectors.IsFull(player, Q006.ThighBag);
		}

		//a "Nesu ti" page: the question, "I have it" if they do, "not yet" anyway
		private static void Ask(QuestDialogContent d, string question, bool has, string yes, string yesPage, string notYet) {
			d.SetText(question);
			d.Html();
			if (has) {
				d.SetText(yes);
				d.Response(false, false, yesPage);
			}
			d.SetText(notYet);
			d.Response(false, false, "gq006_noQuestResources");
		}

		//the pages the responses above lead to
		private void BuildAnswers(QuestDialogContent d, Player player, int q, int alchemy) {
			switch (q) {
				case 0:
					d.Page("gq006_hearBeginingStory");
					d.SetText("Mozna by jsi mi <src.sex(\"mohl\",\"mohla\")> pomoci. Potreboval bych donest deset pochodni.");
					d.Html();
					d.SetText("Prinesu je");
					d.Response(false, true, "gq006_acceptSubquest_1");
					d.SetText("Lituji, ale na to nemam cas. Mejte se.");
					d.Response(false, true, "gq006_emptyExit");
					break;
				case 1:
					Give(d, Q006.Count(player, "i_torch") >= 10, "Dekuji ti. Snad mi to v hledani pomuze.", "*podat pochodne*", "gq006_giveSubQuestResources_1",
						"Pospes prosim, noc se blizi!", "Pokusim se.");
					break;
				case 3:
					Give(d, Q006.FindDog(player) != null, "Dekuji ti. Snad mi to v hledani pomuze.", "*predat psa*", "gq006_giveSubQuestResources_2",
						"Pokud mi nechces pomoci, tak to rekni.", "Nene, pokusim se ho sehnat.");
					break;
				case 5:
					Give(d, Q006.Count(player, "i_potion_cureless") >= 3, "Dekuji, hned bezim za chlapcem.", "*podat protijedy*", "gq006_giveSubQuestResources_3",
						"Prosim, neni casu na zbyt.", "Jdu je hned sehnat.");
					break;
				case 7:
					Give(d, Q006.Count(player, "i_potion_curegreat") >= 10, "Dekuji, hned bezim za chlapcem.", "*podat protijedy*", "gq006_giveSubQuestResources_4",
						"Prosim, pospes. Je mu zle!", "Bezim je hned sehnat.");
					break;
				case 9:
					Give(d, HasThighsAndRefresh(player), "Dekuji.", "*podat vacek a lektvary*", "gq006_giveSubQuestResources_5",
						"Pospes prosim.", "Bezim pro to.");
					break;
				case 12:
					d.Page("gq006_toldQworetStory");
					d.SetText("Obaval jsme se, ze mi saman prilis nepomuze. Budes muset za magem Unasem - ten mi dluzi sluzbicku. Posledni leta pry studuje davne narody u Rhunskych mocalu.");
					d.Html();
					d.SetText("Jedu za nim.");
					d.Response(false, true, "q006_goToUnas");
					break;
				case 18:
					if (Q006.HasParchment(player)) {
						d.Page("gq006_givePergamen");
						d.SetText("Dekuji. Mohylovi duchove... S tim si asi neporadime bez pomoci. Stav se za hospodskym, pritelem alchymisty Terlaka. Ten by mohl vedet, co s tim.");
						d.Html();
						d.SetText("Dobre, pujdu za nim.");
						d.Response(false, true, "q006_goToInnkeeper");
					}
					break;
				case 20:
					d.Page("gq006_tellAboutPotion");
					d.SetText("Hmm, Lektvar Ocisteni? To mi ale musis prinest vsechno, co hospodsky rekl.");
					d.Html();
					d.SetText("Prinesu to.");
					d.Response(false, true, "gq006_acceptSubquest_7");
					break;
				case 21:
					if (Q006.HasPotionIngredients(player)) {
						d.Page("gq006_haveSubQuestResources_7");
						if (alchemy >= 1000) {
							d.SetText("Vidim, ze jsi dostatecne <src.sex(\"zrucny\",\"zrucna\")> na zhotoveni lektvaru. Vezmi si ty prisady a umichej Lektvar Ocisteni.");
							d.Html();
							d.SetText("*pustit se do vyroby lektvaru*");
							d.Response(false, true, "gq006_createPurityPotion", true);
						} else {
							d.SetText("Bohuzel nejsi dostatecne <src.sex(\"zrucny\",\"zrucna\")> na zhotoveni lektvaru. Ale i tak ti dekuji. Pokud by tva znalost alchemie dosahla vrcholu, prijd za mnou. Zatim mi dej prisady.");
							d.Html();
							d.SetText("*podat prisady*");
							d.Response(false, true, "gq006_giveSubQuestResources_7", true);
						}
					}
					NotYet(d, "Pospes.", "Prinesu to.");
					break;
				case 23:
					Give(d, Q006.Count(player, "i_potion_ocisteni") >= 1, "Vyborne! Podej mi lektvar a vezmi si svou odmenu.", "*podat Lektvar Ocisteni*", "gq006_givePurityPotion",
						"Pospes.", "Pokusim se.");
					break;
			}
		}

		//the page a "have it" response leads to, and the "not yet" page
		private static void Give(QuestDialogContent d, bool has, string thanks, string hand, string button, string hurry, string will) {
			if (has) {
				d.Page(button.Replace("give", "have"));
				d.SetText(thanks);
				d.Html();
				d.SetText(hand);
				d.Response(false, true, button, true);
			}
			NotYet(d, hurry, will);
		}

		private static void NotYet(QuestDialogContent d, string hurry, string will) {
			d.Page("gq006_noQuestResources");
			d.SetText(hurry);
			d.Html();
			d.SetText(will);
			d.Response(false, true, "gq006_emptyExit");
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int q = Q006.GetStage(player);
			switch (q) {
				case 0:
					if (button == "gq006_acceptSubquest_1") {
						npc.Say("Dekuji! Doufam, ze se brzy vratis.");
						Q006.Advance(player, 1);
					}
					break;
				case 1:
					if ((button == "gq006_giveSubQuestResources_1") && (Q006.Count(player, "i_torch") >= 10)) {
						Q006.Consume(player, "i_torch", 10);
						Q006.Reward(player, 50);
						Q006.Advance(player, 2);
					}
					break;
				case 2:
					if (button == "gq006_acceptSubquest_2") {
						Q006.Advance(player, 3);
					}
					break;
				case 3:
					if (button == "gq006_giveSubQuestResources_2") {
						PetFigurine dog = Q006.FindDog(player);
						if (dog != null) {
							dog.Delete();
							Q006.Reward(player, 100);
							Q006.Advance(player, 4);
						}
					}
					break;
				case 4:
					if (button == "gq006_acceptSubquest_3") {
						Q006.Advance(player, 5);
					}
					break;
				case 5:
					if ((button == "gq006_giveSubQuestResources_3") && (Q006.Count(player, "i_potion_cureless") >= 3)) {
						Q006.Consume(player, "i_potion_cureless", 3);
						Q006.Reward(player, 50);
						Q006.Advance(player, 6);
					}
					break;
				case 6:
					if (button == "gq006_acceptSubquest_4") {
						Q006.Advance(player, 7);
					}
					break;
				case 7:
					if ((button == "gq006_giveSubQuestResources_4") && (Q006.Count(player, "i_potion_curegreat") >= 10)) {
						Q006.Consume(player, "i_potion_curegreat", 10);
						Q006.Reward(player, 125);
						Q006.Advance(player, 8);
					}
					break;
				case 8:
					if (button == "gq006_acceptSubquest_5") {
						Q006.Give(player, Q006.ThighBag);
						Q006.Advance(player, 9);
					}
					break;
				case 9:
					if ((button == "gq006_giveSubQuestResources_5") && HasThighsAndRefresh(player)) {
						Q006.Consume(player, "i_potion_refreshgreater", 3);
						Collectors.FindIn(player, Q006.ThighBag).Delete();
						Q006.Reward(player, 200);
						Q006.Advance(player, 10);
					}
					break;
				case 10:
					if (button == "gq006_acceptSubquest_6") {
						Q006.Advance(player, 11);
					}
					break;
				case 12:
					if (button == "q006_goToUnas") {
						Q006.Reward(player, 100);
						Q006.Advance(player, 13);
					}
					break;
				case 18:
					if ((button == "q006_goToInnkeeper") && Q006.HasParchment(player)) { //the parchment goes on with the player, to the innkeeper
						Q006.Reward(player, 300);
						Q006.Advance(player, 19);
					}
					break;
				case 20:
					if (button == "gq006_acceptSubquest_7") {
						Q006.Advance(player, 21);
						Q006.Reward(player, 100);
					}
					break;
				case 21:
					if (!Q006.HasPotionIngredients(player)) {
						break;
					}
					if (button == "gq006_giveSubQuestResources_7") {
						Q006.Consume(player, "i_potion_curegreat", 3);
						Q006.Consume(player, "i_srdiczko", 2);
						Q006.Consume(player, "i_bylina", 1);
						Q006.Advance(player, Q006.DoneWithoutRobe);
						Q006.Reward(player, 2000);
					} else if ((button == "gq006_createPurityPotion") && (Q006.Alchemy(player) >= 1000)) { //the ingredients stay with the player
						Q006.Advance(player, 23);
						Q006.Reward(player, 2000);
					}
					break;
				case 22:
					if ((button == "gq006_takePurityPotionResources") && (Q006.Alchemy(player) >= 1000)) {
						Q006.Give(player, "i_potion_curegreat", 3);
						Q006.Give(player, "i_srdiczko", 2);
						Q006.Give(player, "i_bylina");
						Q006.Advance(player, 23);
					}
					break;
				case 23:
					if ((button == "gq006_givePurityPotion") && (Q006.Count(player, "i_potion_ocisteni") >= 1)) {
						Q006.Consume(player, "i_potion_ocisteni", 1);
						Q006.GiveRobe(player);
						Q006.Advance(player, Q006.Done);
					}
					break;
				case Q006.Done:
					if ((button == "gq006_acceptRobe") && (Q006.Alchemy(player) > 999)) {
						if (GoldUtils.Pay(player, Q006.RobePrice)) {
							Q006.GiveRobe(player);
						} else {
							npc.Say("Snazis se me okrast?");
						}
					}
					break;
			}
		}
	}

	/// <summary>#1264 - d_q023_ShamanQworet with only q006's line: his own quest, the magic dagger (q023), isn't ported.</summary>
	public class D_Q006Qworet : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			d.BaseButton(1);
			d.BaseButton(2, 30, "Muzu pomoct?");
			d.BaseButton(6);
			d.Page("Pozdrav");
			d.SetText("Zdravim.");
			d.Html();
			if (Q006.GetStage(player) == 11) {
				d.SetText("*rict o Eriolovom vnukovy a prizracich*");
				d.Response(false, false, "q006_eriol");
				d.Page("q006_eriol");
				d.SetText("Pozdravuj ode mne Eriola, ale prilis te nepotesim, o prizracich v podstate nic nevim, na to jsou lepsi magove nez samani. Oni zkoumaji etericke bytosti velice radi. Ja uz musim jit, mej se.");
				d.Html();
				d.SetText("*odejit*");
				d.Response(false, true, "q006_leave");
			}
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Qworet.");
			d.Html();
			d.Page("Muzu pomoct?");
			d.SetText("Zatim pro tebe nemam nic dalsiho.");
			d.Html();
			d.Page("Prohlednout");
			d.SetText("Vidis Shamana Qworeta....");
			d.Html();
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if ((button == "q006_leave") && (Q006.GetStage(player) == 11)) {
				Q006.Reward(player, 50);
				Q006.Advance(player, 12);
			}
		}
	}

	/// <summary>#1264 - d_q023_CarodejUnas with q006's pages (f_q006_Unas_*); his q023 and q026 lines aren't ported.</summary>
	public class D_Q006Unas : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3129;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int q = Q006.GetStage(player);
			bool essences = (q > 13) && (q < 18);
			for (int i = 1; i <= 6; i++) {
				d.BaseButton(i);
			}
			d.Page("Pozdrav");
			d.SetText("Bud <src.sex(\"zdrav\",\"zdrava\")>!");
			d.Html();
			if (q == 13) {
				d.SetText("*Povedet o Eriolovi, jeho vnukovi a prizracich*");
				d.Response(false, false, "q006_tellStory", true);
				d.Page("q006_tellStory");
				if (player.Level >= Q006.UnasMinLevel) {
					d.SetText("Eriolovi pomoc dluzim, to mas pravdu, ale tobe ne! Pomohu ti, pokud mi prineses dvacet zakladni esenci z ledoveho elementala!");
					d.Html();
					d.SetText("Prinesu je!");
					d.Response(false, true, "q006_esentials");
				} else {
					d.SetText("Eriolovi pomoc dluzim, to mas pravdu, ale tobe ne! Pomohu ti, pokud nejdriv pomuzes ty me. Ale zatim ses moc slaby. Prijd az zesilis.");
					d.Html();
					d.SetText("Dobre, ja se vratim.");
					d.Response(false, true, "q006_idleExit");
				}
			}
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Unas. Jsem kouzelnik.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Nepotrebuji pomoc.");
			d.Html();
			d.Page("Obchodovat");
			d.SetText("Nemam nic na prodej...");
			d.Html();
			d.Page("Hledam praci");
			d.SetText(essences ? "Praci uz jsi dostal, tak ji udelej!" : "Ted pro tebe nemam zadnou praci.");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Co mi neses?");
			d.Html();
			d.Page("Prohlednout");
			d.SetText("Vidis Carodeja Unase... Nicim zvlastnime nevynika.");
			d.Html();
			if (!essences) {
				return;
			}
			d.Page("Nesu ti");
			d.SetText("Ohledne tech esenci...");
			d.Response(false, false, "q006_aboutEssentials");
			d.Page("q006_aboutEssentials");
			d.SetText("Mas ty esence?");
			d.Html();
			if (Collectors.IsFull(player, Q006.EssenceBagFor(q))) {
				d.SetText("Ano, mam je.");
				d.Response(false, false, "q006_haveResources");
				d.Page("q006_haveResources");
				if (q == 14) {
					d.SetText("Dekuji, ale to neni vse! Nyni je tvym ukolem sehnat mi dvacet zakladnich esenci kamennych elementalu.");
				} else if (q == 15) {
					d.SetText("Dekuji, ale to neni vse! Nyni je tvym ukolem sehnat mi dvacet zakladnich esenci elementalu zeme.");
				} else if (q == 16) {
					d.SetText("Dekuji, ale to neni vse! Nyni je tvym ukolem sehnat mi dvacet zakladnich esenci sneznich elementalu.");
				} else {
					d.SetText("Dekuji, zde mas pergamen, ktery bude Eriola zajimat. Ten chlapec prisel do styku s Mohylovymi duchy!");
				}
				d.Html();
				if (q == 17) {
					d.SetText("*Odevzdat esence a vzit pergamen*");
					d.Response(false, true, "q006_takePergamen", true);
				} else {
					d.SetText("*Odevzdat esence a prijmout dalsi ukol*");
					d.Response(false, true, "q006_esentials", true);
				}
			} else {
				d.SetText("Ne, jeste to nemam.");
				d.Response(false, false, "q006_haveNoResources");
				d.Page("q006_haveNoResources");
				d.SetText("Nebudu cekat vecne!");
				d.Html();
				d.SetText("Prinesu je!");
				d.Response(false, true, "q006_idleExit");
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int q = Q006.GetStage(player);
			if ((q == 13) && (button == "q006_esentials") && (player.Level >= Q006.UnasMinLevel)) {
				Q006.Give(player, Q006.EssenceBags[0]);
				Q006.Advance(player, 14);
				return;
			}
			string bag = Q006.EssenceBagFor(q);
			if ((bag == null) || !Collectors.IsFull(player, bag)) { //Erebor's "double check 4 collector"
				return;
			}
			if ((q < 17) && (button == "q006_esentials")) {
				Collectors.FindIn(player, bag).Delete();
				Q006.Give(player, Q006.EssenceBags[q - 13]);
				Q006.Advance(player, q + 1);
				Q006.Reward(player, 500);
			} else if ((q == 17) && (button == "q006_takePergamen")) {
				Collectors.FindIn(player, bag).Delete();
				Q006.SetParchment(player, true);
				Q006.Advance(player, 18);
				Q006.Reward(player, 500);
			}
		}
	}

	/// <summary>#1264 - d_q007_hospodsky: for now only its greeting and Eriol's line (q006); q007 is step 14 of #1237.</summary>
	public partial class D_Q007Hospodsky : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			d.Page("Pozdrav");
			d.SetText("Vitej v me krcme namorniku!");
			d.Html();
			if ((Q006.GetStage(player) == 19) && Q006.HasParchment(player)) {
				d.SetText("*povedet o Eriolovy, pergamenu...*");
				d.Response(false, false, "q006_innkeeper");
				d.Page("q006_innkeeper");
				d.SetText("Muj znamy Eriol ma problemy s Mohylovymi duchy? To je nemile, tem je lepsi se vyhybat! Kazdopadne chlapci by mohl pomoci lektvar Ocisteni. Na nej je treba dve srdce ptaka krvavce, tri nejsilnejsi protijedy a vzacna bylina. Rekni to Eriolovy on uz bude vedet co delat.");
				d.Html();
				d.SetText("Dekuji, reknu mu to.");
				d.Response(false, true, "q006_toldInnkeeper");
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if ((button == "q006_toldInnkeeper") && (Q006.GetStage(player) == 19) && Q006.HasParchment(player)) {
				Q006.SetParchment(player, false);
				Q006.Advance(player, 20);
				Q006.Reward(player, 300);
			}
		}
	}
}
