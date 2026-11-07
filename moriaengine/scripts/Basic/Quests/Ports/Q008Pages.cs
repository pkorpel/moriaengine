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

//#1274 - q008's dialogs, page for page: Erebor's texts byte for byte (questy/q008_black.scp), generated from the archive as
//q007's were (Q007Pages.cs says how pages and buttons are named). Which dialog a player sees and what the buttons do is in Q008.cs.

namespace SteamEngine.CompiledScripts {

	internal static class Q008Pages {
		//d_q008_black_oldman_1
		internal static void Ancam1(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Hmm.. Co pohledava nekdo jako ty na tomto ostrove?");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Ancam. Jsem jen stary chovatel a milovnik koni.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Hmm.. Ted mi nijak nepomuzes. Prozatim zde mam vse, co pro svuj chov potrebuji.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velmi stary a urostly muz. Jazykem neprilis obratny, zato velmi zdatny akrobat a krotitel zvirat. Jiste rozumi lesum a zveri mnohem vice, nez dobremu jidlu a piti.");
			d.Html();
		}

		//d_q008_black_oldman_2
		internal static void Ancam2(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 53, "Mustang");
			d.Page("Pozdrav");
			d.SetText("Hmm.. Co pohledava nekdo jako ty na tomto ostrove?");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Ancam. Jsem jen stary chovatel a milovnik koni.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Hmm.. Ted mi nijak nepomuzes. Prozatim zde mam vse, co pro svuj chov potrebuji.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
			d.Page("Mustang");
			d.SetText("Ah .. zajimas se o to zvire ? Smim-li se chlubit, pak je to jiste nejlepsi kus, jaky jsem kdy odchoval.");
			d.Html();
			d.SetText("Chtel bych to zvire koupit.");
			d.Response(false, false, "p6");
			d.SetText("To jiste je. Co muze clovek jako ja udelat, aby od tebe to zvire ziskal?");
			d.Response(false, false, "p7");
			d.SetText("Prodej mi toho kone!");
			d.Response(false, false, "p8");
			d.Page("p6");
			d.SetText("To zvire neni na prodej! Je to to nejlepsi, co jsem kdy ve svem vybehu choval, a neprodam ho jen tak! Zadne penize mi nemohou nahradit jeho jedinecnost.");
			d.Html();
			d.SetText("Co tedy mohu udelat, abych od tebe to zvire ziskal?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Hmm... Odchovat tohoto hrebce mne stalo mnoho namahy a trpelivosti. Leta jsem zkousel ruzne druhy bylin, abych jimi dodal vse potrebne k rozvinuti jeho sily. Ted jsem si jist, jake prisady jsou potreba a s nimi budu schopen odchovat dalsi takovy kus. Ve velke zime se temto bylinam dari. Na ostrovech pokrytych snehem zije spousta zvlastnich tvoru. Jdi tam, sbirej vse, co maji u sebe, a to mi pote dones. Kdyz doneses vseho dostatek k odchovu dalsiho statneho hrebce, pak ti dovolim vstoupit do ohrady, kde budes moci predvest sve umeni a pokusit se ochocit toto hrde zvire.");
			d.Html();
			d.SetText("Dobra, pokusim se tam vypravit.");
			d.Response(false, false, "p9");
			d.Page("p8");
			d.SetText("Nebud tak lacny cizince ! Tva hamiznost se mi prici. Mas-li na mne jakoukoliv prosbu, radim ti dobre - netroufej si ke mne znovu hovorit se stejnou drzosti!");
			d.Html();
			d.SetText("Kazdy ma svou cenu. Toho kone chci!");
			d.Response(false, true, "ancam_2:2");
			d.SetText("Co tedy mohu udelat, abych od tebe to zvire ziskal?");
			d.Response(false, false, "p7");
			d.Page("p9");
			d.SetText("Vyborne! Tady mas bednu. Vloz do ni vsechno, co u tech bestii najdes. Jen tak budes mit jistotu, ze doneses vse co potrebuji.");
			d.Html();
			d.SetText("*Prevzit bednu*");
			d.Response(false, true, "ancam_2:1", true);
			d.SetText("Nemohl by jsi mi priblizit, jake prisady po mne chces?");
			d.Response(false, false, "p10");
			d.Page("p10");
			d.SetText("Nic na svete mne nedonuti prozradit me tajemstvi. Suroviny, ktere potrebuji jsou vsak vzacne a sehnat se daji jen na ostrovech pokrytych snehem.. Jsou k nerozeznani od tech, ktere jiste znas, nejspise mezi nimi neshledas rozdil. Lisi se predevsim obsahem nekterych latek. Bohuzel ty ucinne latky mohou vyvolavat u nekterych tvoru kteri je u sebe nosi plachost. Budis ti tedy dobrou radou, aby jsi po ostrove patral pokud mozno sam a ta stvoreni nevyplasil.");
			d.Html();
			d.SetText("*Prevzit bednu*");
			d.Response(false, true, "ancam_2:1", true);
		}

		//d_q008_black_oldman_3
		internal static void Ancam3(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 59, "Nesu ti");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Vyborne. Kone jsou zdravi, nemohu si stezovat.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ano prines mi ty prisady a uvidime, jak budu schopen pomoci ja tobe.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Hmm? Aaah v te bedne jiste... Ukaz mi, co jsi nasbiral.");
			d.Html();
			d.SetText("*Ukazat bednu*");
			d.Response(false, false, "p6", true);
			d.Page("p6");
			d.SetText("Aha.. ahh.. ne ... ne kdepak tady toho neni dost. S timhle nevystacim. Takhle silny kun roste dlouho a potrebuji toho vice.");
			d.Html();
			d.SetText("Nemohl by jsi mi priblizit, jake prisady po mne chces?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Nic na svete mne nedonuti prozradit me tajemstvi. Suroviny, ktere potrebuji jsou vsak vzacne a sehnat se daji jen na ostrovech pokrytych snehem.. Jsou k nerozeznani od tech, ktere jiste znas, nejspise mezi nimi neshledas rozdil. Lisi se predevsim obsahem nekterych latek. Bohuzel ty ucinne latky mohou vyvolavat u nekterych tvoru kteri je u sebe nosi plachost. Budis ti tedy dobrou radou, aby jsi po ostrove patral pokud mozno sam a ta stvoreni nevyplasil.");
			d.Html();
		}

		//d_q008_black_oldman_4
		internal static void Ancam4(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 59, "Nesu ti");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Vyborne. Kone jsou zdravi, nemohu si stezovat.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ano prines mi ty prisady a uvidime, jak budu schopen pomoci ja tobe.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Hmm? Aaah v te bedne jiste... Ukaz mi, co jsi nasbiral.");
			d.Html();
			d.SetText("*Ukazat bednu*");
			d.Response(false, false, "p6", true);
			d.Page("p6");
			d.SetText("Aha.. ahh.. ne ... ne kdepak, ale ano ! Je to tady uz to vdim a je toho dost! Vyborne, vypada to, ze jsi se sveho tezkeho ukolu chopil pevne a nepustilo te to. Dej mi tedy cas a ja vychovam dalsiho skveleho hrebce. Prijd za mnou, az s tim budu hotov a ja te pustim do ohrady, kde se pokusis o jeho zkroceni.");
			d.Html();
			d.SetText("*souhlasit*");
			d.Response(false, true, "ancam_4:1", true);
		}

		//d_q008_black_oldman_5
		internal static void Ancam5(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Zadna slava... To zvire zere jako dive. Budu rad, kdyz z toho, co jsi mi prinesl, vychovam jeden kus...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ah... Jsi dobry clovek, ale ted je rada na mne, abych ti oplatil tvou sluzbu jak jsem slibil.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
		}

		//d_q008_black_oldman_6
		internal static void Ancam6(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 53, "Mustang");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Zadna slava... To zvire zere jako dive. Nezbylo mi vubec nic na odchov dalsiho hrebce.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ah... Jsi dobry clovek, ale ted je rada na mne, abych ti oplatil tvou sluzbu, jak jsem slibil.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
			d.Page("Mustang");
			d.SetText("Tak tedy, jak jsem ti jiz slibil, mas ted moznost vstoupit do ohrady a podmanit si vuli toho zvirete. Pamatuj si vsak, ze mas jen jednu sanci. Pokud v souboji s nim prohrajes, nedovolim ti se k nemu znovu priblizit. Vyhrajes-li, je tvuj. Ted jdi !");
			d.Html();
			d.SetText("*pokracovat*");
			d.Response(false, true, "ancam_6:1", true);
		}

		//d_q008_black_oldman_7
		internal static void Ancam7(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 59, "Nesu ti");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Vyborne. Kone jsou zdravi, nemohu si stezovat.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ano prines mi ty prisady a uvidime, jak budu schopen pomoci ja tobe.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Hmm? Aaah v te bedne jiste... Ukaz mi, co jsi nasbiral.");
			d.Html();
			d.SetText("*Ukazat bednu*");
			d.Response(false, false, "p6", true);
			d.Page("p6");
			d.SetText("Aha.. ahh.. ne ... ne kdepak, ale ano ! Je to tady uz to vdim a je toho dost! Vyborne, vypada to, ze jsi se sveho tezkeho ukolu chopil pevne a nepustilo te to...");
			d.Html();
			d.SetText("*pokracovat*");
			d.Response(false, false, "p7", true);
			d.Page("p7");
			d.SetText("Tak tedy, jak jsem ti jiz slibil, mas ted moznost vstoupit do ohrady a podmanit si vuli toho zvirete. Pamatuj si vsak, ze mas jen jednu sanci. Pokud v souboji s nim prohrajes, nedovolim ti se k nemu znovu priblizit. Vyhrajes-li, je tvuj. Ted jdi !");
			d.Html();
			d.SetText("*souhlasit*");
			d.Response(false, true, "ancam_7:1", true);
		}

		//d_q008_black_oldman_8
		internal static void Ancam8(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 53, "Mustang");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Zadna slava... To zvire zere jako dive. Nezbylo mi vubec nic na odchov dalsiho hrebce.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ah... Jsi dobry clovek, ale ted je rada na mne, abych ti oplatil tvou sluzbu, jak jsem slibil.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
			d.Page("Mustang");
			d.SetText("Ano jiste. Nevedel jsem, kdy se sem k nemu vratis, tak jsem ho pustil na pastvinu. Nehledej ho! Nikdy bys neuspel. Vrat se pozdeji. Az se ke mne vrati, muzes se pokusit o jeho ochoceni.");
			d.Html();
		}

		//d_q008_black_oldman_9
		internal static void Ancam9(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Skvele! Kone jsou zdravi, nemohu si stezovat.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Hmm.. Ted mi nijak nepomuzes. Proatim zde mam vse, co pro svuj chov potrebuji.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velmi stary a urostly muz. Jazykem neprilis obratny, zato velmi zdatny akrobat a krotitel zvirat. Jiste rozumi lesum a zveri mnohem vice, nez dobremu jidlu a piti.");
			d.Html();
		}

		//d_q008_black_oldman_10
		internal static void Ancam10(QuestDialogContent d) {
			d.BaseButton(1, 45, "Jak se dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 53, "Mustang");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim.");
			d.Html();
			d.Page("Jak se dari?");
			d.SetText("Vyborne. Kone jsou zdravi, nemohu si stezovat.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Hmm.. Ted mi nijak nepomuzes. Prozatim zde mam vse, co pro svuj chov potrebuji.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary a urostly muz. Jazykem neprilis obratny, zato velmi zrucny akrobat a krotitel zvirat. Uz na prvni pohled je z nej mnohem vice citit laska k lesum a zveri, nez k dobremu jidlu a piti.");
			d.Html();
			d.Page("Mustang");
			d.SetText("Ah .. Nemas jeste dost hmm? *usmal se*");
			d.Html();
			d.SetText("Ano. Chci dalsiho Mustanga.");
			d.Response(false, true, "ancam_10:2");
			d.SetText("Tva zvirata jsou opravdu nadherna! Rad bych mel tu cest osedlat dalsiho cerneho mustanga.");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Libis se mi. Stejne jsem potreboval dalsi byliny. Tedy dobra dohodneme se. Dam ti dalsi bednu. Vydej se znovu za dobrodruzstvim a prines mi dalsi prisady.");
			d.Html();
			d.SetText("*Souhlasit a prijmout bednu*");
			d.Response(false, true, "ancam_10:1", true);
			d.SetText("Nemohl by jsi mi priblizit, jake prisady po mne chces?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Nic na svete mne nedonuti prozradit me tajemstvi. Suroviny, ktere potrebuji jsou vsak vzacne a sehnat se daji jen na ostrovech pokrytych snehem.. Jsou k nerozeznani od tech, ktere jiste znas, nejspise mezi nimi neshledas rozdil. Lisi se predevsim obsahem nekterych latek. Bohuzel ty ucinne latky mohou vyvolavat u nekterych tvoru kteri je u sebe nosi plachost. Budis ti tedy dobrou radou, aby jsi po ostrove patral pokud mozno sam a ta stvoreni nevyplasil.");
			d.Html();
			d.SetText("*Souhlasit a prijmout bednu*");
			d.Response(false, true, "ancam_10:1", true);
		}

		//d_q008_black_oldman_no_tamer
		internal static void AncamNoTamer(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Hmm.. Co pohledava nekdo jako ty na tomto ostrove?");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Ancam. Jsem jen stary chovatel a milovnik koni.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Hmm.. Ty mi nijak nepomuzes. Nevypadas, ze by jsi o zviratech vedel dost.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velmi stary a urostly muz. Jazykem neprilis obratny, zato velmi zdatny akrobat a krotitel zvirat. Jiste rozumi lesum a zveri mnohem vice, nez dobremu jidlu a piti.");
			d.Html();
		}
	}
}
