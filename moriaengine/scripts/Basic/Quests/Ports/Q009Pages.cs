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

//#1270 - q009's dialogs, page for page: Erebor's texts byte for byte (questy/q009_mistr_krejci.scp), generated from the archive once as
//q007's were (Q007Pages.cs says how pages and buttons are named). Her "Obchodovat?" button answered at once: here it's a page of its own
//with that one answer. Which dialog a player sees and what the buttons do is in Q009.cs.

namespace SteamEngine.CompiledScripts {

	internal static class Q009Pages {
		//d_q009_krejci_1
		internal static void Krejci1(QuestDialogContent d) {
			d.BaseButton(1, 57, "Kdo jsi?");
			d.BaseButton(2, 45, "Mohu pomoci?");
			d.BaseButton(3, 45, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(6, 50, "Obchodovat?", QuestDialogContent.ColorBaseButton);
			d.Page("Pozdrav");
			d.SetText("Vitej v mem kramku !");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Talathwen. Jsem zde jedina poradna krejci. Nikdy zbrkle nevyhazuj svuj strhany odev, divil by ses, co vsechno se da spravit nekolika stehy.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("No nevim, jsem v tezke situaci. Kvalitni zbozi si dnes koupi jen malokdo a hrozi mi bankrot. I mistni si uz kupuji sve veci u tech pochybnych prodejcu na trzisti. Je to skupina drzgresli, co svou praci odbydou, a aby ne, vzdyt ji vetsinou ani poradne neznaji. Prodala bych snad cokoliv, abych udrzela rodinny obchod v pohybu, ale nemam ti moc co nabidnout. Snad jen... *povzdechla*");
			d.Html();
			d.SetText("Jsem si jist, ze tvuj obchod je prednejsi.");
			d.Response(false, false, "p5");
			d.SetText("Vsiml jsem si dokonalych tvaru tvych satu. Jejich strih a puvab preci nemuze byt dilem JEN tak nejakeho prstenu!");
			d.Response(true, false, "p6");
			d.Page("-Prohlednout-");
			d.SetText("Trochu vystredne oblecena, zrala zena. Na jeji dnes jiz lehce zvrasnene tvari je videt jiskra mladosti a citis, ze byla kdysi obletovana davy muzu. I kdyz i mene zkusenemu oku nemuze uniknout, ze jeji hrudnik je z velke casti pouze krejcirska kamuflaz.");
			d.Html();
			d.Page("p5");
			d.SetText("Myslim, ze tva odpoved je hnana lacnosti a touhou vynikat. Cloveku jako ty nesverim vse, co davalo memu remeslu smysl a me vuli silu pokracovat!");
			d.Html();
			d.Page("p6");
			d.SetText("Ah *Talathwen je zaskocena, ale potesena tvou lichotkou* .. Jsi dobry clovek, ale presto si nejsem jista, zda by pro tebe mel tento predmet takovou hodnotu na jakou si jej cenim. Ale jsem tedy rozhodnuta. Mas o nej zajem ?");
			d.Html();
			d.SetText("Ano mam !");
			d.Response(false, false, "p8");
			d.SetText("Bohuzel, toto neni nic pro mne...");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Skoda, u tebe by byl jiste v dobrych rukou... Kdyby jsi o nej mel precijen nekdy zajem, zastav se !");
			d.Html();
			d.Page("p8");
			d.SetText("Dobra tedy ! Prines mi sek na 5M zlatych a prsten muze byt tvuj!");
			d.Html();
			d.SetText("*souhlasit*");
			d.Response(false, true, "krejci_1:1", true);
			d.SetText("Uff ! To je trochu moc, nemyslis ?");
			d.Response(false, false, "p9");
			d.Page("p9");
			d.SetText("Ne, jsem si vedoma zavratnosti teto castky, avsak nepolevim. Je to rodinny klenot a ja jej prodam jen tehdy, bude-li pro mne jeho cena znamenat naprostou jistotu pro muj obchod.");
			d.Html();
			d.SetText("*souhlasit*");
			d.Response(false, true, "krejci_1:1", true);
			d.SetText("Bohuzel, toto neni nic pro mne...");
			d.Response(false, false, "p7");
			d.Page("Obchodovat?");
			d.SetText("Obchodovat?");
			d.Response(false, true, "krejci_1:2");
		}

		//d_q009_krejci_2
		internal static void Krejci2(QuestDialogContent d) {
			d.BaseButton(3, 45, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(6, 50, "Obchodovat?", QuestDialogContent.ColorBaseButton);
			d.Page("Pozdrav");
			d.SetText("Rada te tu opet vidim. Pamatuj, neprodam ten prsten za mene, jak 5M. Staci donest sek a bude tvuj.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Trochu vystredne oblecena, zrala zena. Na jeji dnes jiz lehce zvrasnene tvari je videt jiskra mladosti a citis, ze byla kdysi obletovana davy muzu. I kdyz i mene zkusenemu oku nemuze uniknout, ze jeji hrudnik je z velke casti pouze krejcirska kamuflaz.");
			d.Html();
			d.Page("Obchodovat?");
			d.SetText("Obchodovat?");
			d.Response(false, true, "krejci_2:2");
		}

		//d_q009_krejci_3
		internal static void Krejci3(QuestDialogContent d) {
			d.BaseButton(3, 45, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(6, 50, "Obchodovat?", QuestDialogContent.ColorBaseButton);
			d.Page("Pozdrav");
			d.SetText("Rada te tu opet vidim. Koupis tedy ten prsten?");
			d.Html();
			d.SetText("Mam tu pro tebe ten sek.");
			d.Response(false, false, "p3");
			d.Page("-Prohlednout-");
			d.SetText("Trochu vystredne oblecena, zrala zena. Na jeji dnes jiz lehce zvrasnene tvari je videt jiskra mladosti a citis, ze byla kdysi obletovana davy muzu. I kdyz i mene zkusenemu oku nemuze uniknout, ze jeji hrudnik je z velke casti pouze krejcirska kamuflaz.");
			d.Html();
			d.Page("p3");
			d.SetText("Vyborne ! Nech mne tedy zmerit tvuj prst a zitra si pro ten prsten prijd. Musi ti presne padnout, jinak je k nicemu.");
			d.Html();
			d.SetText("*Predat krejci sek na 5 000 000 gp.*");
			d.Response(false, true, "krejci_3:1", true);
			d.SetText("Jak mi muzes zarucit, ze s temi penezi nezmizis ?");
			d.Response(false, false, "p4");
			d.Page("p4");
			d.SetText("Mas jen me slovo a jistotu, ze opustit toto staveni by pro mne bylo tezsi nez smrt.");
			d.Html();
			d.SetText("*Predat krejci sek na 5 000 000 gp.*");
			d.Response(false, true, "krejci_3:1", true);
			d.Page("Obchodovat?");
			d.SetText("Obchodovat?");
			d.Response(false, true, "krejci_3:2");
		}

		//d_q009_krejci_4
		internal static void Krejci4(QuestDialogContent d) {
			d.BaseButton(3, 45, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(6, 50, "Obchodovat?", QuestDialogContent.ColorBaseButton);
			d.Page("Pozdrav");
			d.SetText("Rada te tu opet vidim. Ten prsten jeste neni hotovy, stav se pozdeji.");
			d.Html();
			d.SetText("*Odejit*");
			d.Response(false, true, "krejci_4:1", true);
			d.Page("-Prohlednout-");
			d.SetText("Trochu vystredne oblecena, zrala zena. Na jeji dnes jiz lehce zvrasnene tvari je videt jiskra mladosti a citis, ze byla kdysi obletovana davy muzu. I kdyz i mene zkusenemu oku nemuze uniknout, ze jeji hrudnik je z velke casti pouze krejcirska kamuflaz.");
			d.Html();
			d.Page("Obchodovat?");
			d.SetText("Obchodovat?");
			d.Response(false, true, "krejci_4:2");
		}

		//d_q009_krejci_5
		internal static void Krejci5(QuestDialogContent d) {
			d.BaseButton(3, 45, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(6, 50, "Obchodovat?", QuestDialogContent.ColorBaseButton);
			d.Page("Pozdrav");
			d.SetText("Aaah vyborne ! Tva odmena je hotova, tady vezmi si ji. Jeste ti vysvetlim, jak jej pouzit a bude to vse.");
			d.Html();
			d.SetText("*Prevzit prsten a vyslechnout si podrobnosti o jeho pouziti*");
			d.Response(false, true, "krejci_5:1", true);
			d.Page("-Prohlednout-");
			d.SetText("Trochu vystredne oblecena, zrala zena. Na jeji dnes jiz lehce zvrasnene tvari je videt jiskra mladosti a citis, ze byla kdysi obletovana davy muzu. I kdyz i mene zkusenemu oku nemuze uniknout, ze jeji hrudnik je z velke casti pouze krejcirska kamuflaz.");
			d.Html();
			d.Page("Obchodovat?");
			d.SetText("Obchodovat?");
			d.Response(false, true, "krejci_5:2");
		}

		//d_q009_krejci_6
		internal static void Krejci6(QuestDialogContent d) {
			d.BaseButton(3, 45, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(6, 50, "Obchodovat?", QuestDialogContent.ColorBaseButton);
			d.Page("Pozdrav");
			d.SetText("Vitej v mem kramku! Jsem ti stale vdecna za tvou velkorysou nabidku za prsten. Muj obchod se od te doby postavil na nohy.");
			d.Html();
			d.SetText("*Odejit*");
			d.Response(false, true, "krejci_6:1", true);
			d.Page("-Prohlednout-");
			d.SetText("Trochu vystredne oblecena, zrala zena. Na jeji dnes jiz lehce zvrasnene tvari je videt jiskra mladosti a citis, ze byla kdysi obletovana davy muzu. I kdyz i mene zkusenemu oku nemuze uniknout, ze jeji hrudnik je z velke casti pouze krejcirska kamuflaz.");
			d.Html();
			d.Page("Obchodovat?");
			d.SetText("Obchodovat?");
			d.Response(false, true, "krejci_6:2");
		}
	}
}
