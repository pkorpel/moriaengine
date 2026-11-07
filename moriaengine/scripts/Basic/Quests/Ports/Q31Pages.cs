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

//#1274 - q31's dialogs, page for page: Erebor's texts byte for byte (questy/q31_kuchar.scp), generated from the archive as
//q007's were (Q007Pages.cs says how pages and buttons are named). Which dialog a player sees and what the buttons do is in Q31.cs.

namespace SteamEngine.CompiledScripts {

	internal static class Q31Pages {
		//d_q31_kuchar_1
		internal static void Kuchar1(QuestDialogContent d) {
			d.BaseButton(1, 40, "Kdo jsi?");
			d.BaseButton(2, 25, "Muzu pomoci?");
			d.BaseButton(3, 20, "Tve speciality?");
			d.BaseButton(4, 30, "Hledam praci");
			d.BaseButton(5, 25, "Nesu ti...");
			d.BaseButton(6, 30, "Prohlednout");
			d.Page("Pozdrav"); //Erebor's dialog opened on its "Kdo jsi?" page
			d.SetText("Zdravicko, ja jsem kuchar Mrkvicka. Nejlepsi a nejskvelejsi kuchar v okoli *Pratelsky se usmal*.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Zdravicko, ja jsem kuchar Mrkvicka. Nejlepsi a nejskvelejsi kuchar v okoli *Pratelsky se usmal*.");
			d.Html();
			d.Page("Muzu pomoci?");
			d.SetText("Hmno... mohl bys. Mam spoustu zakazek a zrovna si jeden bohac preje obrovske mnozstvi me speciality - ptacich peceni. Kdyby sis chtel trochu vydelat, muzes jich pro me par nachytat. Dobre se ti odmenim ! Co ty na to?");
			d.Html();
			d.SetText("<?src.sex(Rad,Rada)?> to udelam.");
			d.Response(false, false, "p7");
			d.SetText("Ne. Promin, ale pospicham. Urcite se najde nekdo jiny.");
			d.Response(false, false, "p8");
			d.Page("Tve speciality?");
			d.SetText("Oooo toho je spousty. Jidla cizokrajna, mistni speciality... neexistuje nic, co bych nedokazal uvarit. Sbiram take stare recepty, nasi predkove byli skutecne velci gurmani - to se moc nevi. Slysel jsi nekdy o Osvezujicim likeru elfky Ariadne?");
			d.Html();
			d.SetText("Ale ano... neco mi to rika...");
			d.Response(false, false, "p9");
			d.SetText("Ne, povis mi o co jde? Nazev zni zajimave.");
			d.Response(false, false, "p10");
			d.Page("Hledam praci");
			d.SetText("Ja si vystacim sam, velky kuchar nikoho jineho nepotrebuje");
			d.Html();
			d.Page("Nesu ti...");
			d.SetText("Copak mi neses?");
			d.Html();
			d.Page("Prohlednout");
			d.SetText("Kuchar Mrkvicka je trochu pri tele, s veselym oblicejem a vysokym sebevedomim. Jsi si vsak <?src.sex(jisty,jista)?>, ze to je take skvely kuchar.");
			d.Html();
			d.Page("p7");
			d.SetText("To jsem rad, ze nemusim hledat nekoho jineho. Snad to zvladnes. Zabit ptacka prece nemuze byt tak slozite, co rikas? Vezmi si tady tenhle batuzek a prines mi ... hm ... 50 jakychkoli mrtvych ptacku... a pospes si prosim!");
			d.Html();
			d.SetText("Udelam to!");
			d.Response(false, true, "kuchar_1:1");
			d.Page("p8");
			d.SetText("No co se da delat <basefont color=\"#ffff88\">*Zaskaredil se*<basefont color=\"#ffffff\"> ty mas urcite lepsi veci na praci...");
			d.Html();
			d.SetText("Odejit");
			d.Response(false, true, "kuchar_1:2");
			d.Page("p9");
			d.SetText("Vazne? <basefont color=\"#ffff88\">*Zatvaril se pochybovacne*<basefont color=\"#ffffff\"> Tak to mi jiste muzes rict, z ceho se mel napoj skladat?");
			d.Html();
			d.SetText("Vymyslet si nejake slozeni a rict ho kuchari");
			d.Response(false, true, "kuchar_1:3");
			d.SetText("Priznat, ze jsem si <?src.sex(vymyslel,vymyslela)?>");
			d.Response(false, false, "p11");
			d.Page("p10");
			d.SetText("Jiste jiste... je to velmi zajimave, Ariadne byla jedna z nejlepsich kucharek - vymyslela skvele recepty a par z nich by se mohlo vyrovnat snad i mym delikatesam <basefont color=\"#ffff88\">*Zatvaril se zasnene*<basefont color=\"#ffffff\">, jeji nejvetsi dilo byl Osvezujici liker, toho si nejvic povazovala a chranila jeho recept. Pry pomahal regenerovat unavene telo a pri pravidelnych davkach zpusobovalo takrka nezranitelnost. A navic... ten napoj mel naprosto dokonalou chut. Jemnou, podmanivou... <basefont color=\"#ffff88\">*Zasnil se*<basefont color=\"#ffffff\"> Ach, jaka skoda, ze se recept na vzdy ztratil spolu s Ariadne.");
			d.Html();
			d.Page("p11");
			d.SetText("Nic se nedeje... kazdy se dnes snazi zaujmout *Usmal se*");
			d.Html();
		}

		//d_q31_kuchar_2
		internal static void Kuchar2(QuestDialogContent d) {
			d.Page("Pozdrav");
			d.SetText("Ach, to jsi ty. Jsem rad, ze te vidim. Mam ted strasne moc prace. <basefont color=\"#ffff88\">*Rozhodil rukama*<basefont color=\"#ffffff\"> Tak co, uz mas, oc jsem te prosil?");
			d.Html();
			d.SetText("Jiste, tady to mas!");
			d.Response(false, true, "kuchar_2:1");
			d.SetText("Bohuzel jeste ne, ale uz na tom pracuji.");
			d.Response(false, true, "kuchar_2:2");
			d.SetText("Ne. Uz to nechci delat. Dej to nekomu jinemu, jo?");
			d.Response(false, true, "kuchar_2:3");
			d.SetText("!!!VYMENIT NEFUNKCNI PYTLIK ZA NOVY!!!");
			d.Response(false, true, "kuchar_2:4");
		}

		//d_q31_kuchar_3
		internal static void Kuchar3(QuestDialogContent d) {
			d.Page("Pozdrav");
			d.SetText("Aaaa priteli jsem rad ze te zase vidim! Bohuzel si ted s tebou nemohu popovidat mam tady spoustu prace.... zrovna tady jde nekdo s kym potrebuju nutne mluvit... mej se.");
			d.Html();
			d.SetText("*Odejit*");
			d.Response(false, true, "kuchar_3:1");
		}
	}
}
