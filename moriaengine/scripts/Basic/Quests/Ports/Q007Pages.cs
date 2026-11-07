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

//#1266 - q007's dialogs, page for page: Erebor's texts byte for byte (questy/q007_HaradirovoSelatko.scp), generated from the archive
//once, as the Grandmaster dialogs were, and meant to be read like normal code. Page 1 of each is "Pozdrav", a page a big button opens
//is named like its label, the rest "p<Erebor's page number>"; an exit button is "<dialog>:<Erebor's button number>". Which of them a
//player sees and what the buttons do is in Q007Dialogs.cs. The innkeeper's is written by hand there: it decides as it goes.

namespace SteamEngine.CompiledScripts {

	internal static class Q007Pages {
		//d_q007_haradir
		internal static void Haradir(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno je Haradir. Lovim s nekolika prateli vsechno, co se da na severu sehnat.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Aaah.. *usmal se* .. Cestujes rad ?");
			d.Html();
			d.SetText("Mohu ti jen rici, ze me nohy poslapaly mnoho cest mezi kolosy Modrych hor a brehem Nurnskeho more.");
			d.Response(true, false, "p5");
			d.SetText("Ja mam radsi hospodu za rohem, praci ve sve knihovne a sluzebnou, ktera mi nakoupi.");
			d.Response(true, false, "p6");
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("p5");
			d.SetText("Hmm... Chces-li mi opravdu pomoci, pak ti nebude tva vytrvalost velkym prinosem. Vis.. *utichl a uprel svuj pohled daleko pred sebe* ..mam tu na starost nekolik lidi a nemohu je opustit. Uz davno si zaslouzili mnohem vice, nez jen tuhe maso lednich medvedu. Kdysi jsem hodne cestoval, mozna jako ty... *usmal se a opet obratil svuj zrak na tebe* ... a mnoho casu jsem stravil s elfy z Sedych pristavu. Cistili jsme ostrovy blizko pobrezi od ruznych besitii. Na jednom ostrove jsem se setkal s hospodskym. Ten chudak mel obrovske problemy s nevidanymi tvory. Mel jsem odsud, z domova, mnoho zkusenosti s bojem na snehu a proto jsem souhlasil, ze mu pomohu. Beztak to byl duvod, proc jsme se mnoho let plavili po mori. Nekolik tydnu jsem i s posadkou lodi Malle Aiwiva cistil cely ostrov. To, co jsem tam videl, nikdy nezapomenu. Stiny v mlze... stale ukryty v neprostupnem snehu, jejich krik ! Ale to neni dulezite, to je jiz davno.");
			d.Html();
			d.SetText("Prosim pokracuj...");
			d.Response(false, false, "p7");
			d.Page("p6");
			d.SetText("*usmal se* To je nevidana statecnost. Ovsem cas na knihy a jidlo za rohem ti mohu zarucit, budes-li mi chtit pomoci.<br> Vis.. *utichl a uprel svuj pohled daleko pred sebe* ..mam tu na starost nekolik lidi a nemohu je opustit. Uz davno si zaslouzili mnohem vice, nez jen tuhe maso lednich medvedu. Kdysi jsem hodne cestoval, prevracet se doma na lavici mne nikdy neuspokojovalo ... *opet obratil svuj zrak na tebe a usmal se* ... a mnoho casu jsem stravil s elfy z Sedych pristavu. Cistili jsme ostrovy blizko pobrezi od ruznych besitii. Na jednom ostrove jsem se setkal s hospodskym. Ten chudak mel obrovske problemy s nevidanymi tvory. Mel jsem odsud, z domova, mnoho zkusenosti s bojem na snehu a proto jsem souhlasil, ze mu pomohu. Beztak to byl duvod, proc jsme se mnoho let plavili po mori. Nekolik tydnu jsem i s posadkou lodi Malle Aiwiva cistil cely ostrov. To, co jsem tam videl, nikdy nezapomenu. Stiny v mlze... stale ukryty v neprostupnem snehu, jejich krik ! Ale to neni dulezite, to je jiz davno.");
			d.Html();
			d.SetText("Prosim pokracuj...");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("To opravdu neni dulezite... V kazdem pripade mi ten hospodsky zustal dluzen sluzbicku. Proto jsem ho tenkrat pozadal, aby mi poslal pecene sele. U Mandose! Jak on umi ta mala stvoreni vyudit! Ten vsivak vsak neposlal nic a dokonce mi zaprel svuj slib i penize, ktere jsem mu za nej dal. Ja vsak nemohu opustit svou rodinu a jit si to s nim vyridit. Prosim, jdi za nim a prines mi od nej sele, ktere mi slibil. Budu ti zavazan.<br>*Haradir se na tebe podival s velkym ocekavanim*");
			d.Html();
			d.SetText("Mas me slovo, ze se za tim hospodskym vypravim.");
			d.Response(false, true, "haradir:1");
		}

		//d_q007_haradir2
		internal static void Haradir2(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 55, "Selatko...");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prozatim tady vychazime akorat. Co nalovime prodame nebo snime. Neni cas se hnout z mista.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Uz jsi vyrazil za tim hospodskym? Nic jineho me netrapi. Tady si vse zaridim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("Selatko...");
			d.SetText("Uz mas to selatko?! *nadsene vykrikl*");
			d.Html();
			d.SetText("Je mi lito... Nevzpomenes si, na kterem ostrove najdu toho hospodskeho?");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("To opravdu nevim. Ja se v lodnich mapach nevyznam. Byl jsem prilis mlady a zajimal jsem se jen o boj... abych byl lepsi nez ostatni. Neprislo mi dulezite, kde jsem a s kym se utkam.");
			d.Html();
			d.SetText("Jak jsi ale s tim hospodskym celou dobu komunikoval?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Rekl mi to Nemethen, na lodi to byl muj nejlepsi pritel. Vlastne jen diky nemu s tebou ted mohu mluvit. Obcas sem zamnou zasel. Ja jsem uz s plavbami skoncil, ale jeho nesmrtelnost mu davala vic casu a vytrvalosti. Pozadal jsem ho proto, aby obcas zavital za hospodskym a pripomnel mu jeho slib. Od Nemethena take vim, ze mne hospodsky zaprel.");
			d.Html();
			d.SetText("A kde najdu Nemethena ted?");
			d.Response(false, false, "p8");
			d.Page("p8");
			d.SetText("*smutne sklonil hlavu* V Anduine proteklo mnoho vody od doby, kdy jsem ho naposledy videl. Kdyz se semnou naposledy loucil, tvaril se velmi nervozne... Kdyby jsi o nem na svych cestach slysel, prosim dej mi o nem vedet!");
			d.Html();
			d.SetText("Muzes se spolehnout..");
			d.Response(false, true, "haradir2:1");
		}

		//d_q007_haradir3
		internal static void Haradir3(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 55, "Selatko...");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prozatim tady vychazime akorat. Co nalovime prodame nebo snime. Neni cas se hnout z mista.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Uz jsi vyrazil za tim hospodskym? Nic jineho me netrapi. Tady si vse zaridim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("Selatko...");
			d.SetText("Uz mas to selatko?! *nadsene vykrikl*");
			d.Html();
			d.SetText("Ano byl jsem u toho hospodskeho. Jsem si jist, ze to byl on, ale tvrdi, ze ti nikdy zadne sele neslibil.");
			d.Response(true, false, "p6");
			d.Page("p6");
			d.SetText("Stale mne dokaze prekvapit jeho lakota. Ja bych ho!... *zatnul pest*... Poslouchej, mozna je jeste jedna moznost, jak toho stareho hospodare presvedcit, aby splnil svuj zavazek. Poslouchej...");
			d.Html();
			d.SetText("Pokracuj...");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Za tech par tydnu, ktere jsem stravil na ostrove jsem nekolikrat videl velmi puvabnou zenu. Zdalo se, ze ma na hospodskeho obrovsky vliv, ale nebylo mi jasne proc ji porad tak podstrojuje. Mam pocit, ze ji je za neco velmi zavazan. Jeji jmeno je Rachel a podle vseho je to carodejnice.");
			d.Html();
			d.SetText("Dobra, pokusim se ji najit.");
			d.Response(false, true, "haradir3:1");
			d.SetText("Nevis, kde je Rachel ted?");
			d.Response(false, false, "p8");
			d.Page("p8");
			d.SetText("Bohuzel nemam tuseni. Tenkrat prijela svou vlastni lodi... pobyla tam par dni a opet zmizela na mori.");
			d.Html();
			d.SetText("Dobra, pokusim se ji najit.");
			d.Response(false, true, "haradir3:1");
		}

		//d_q007_haradir4
		internal static void Haradir4(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 55, "Selatko...");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prozatim tady vychazime akorat. Co nalovime prodame nebo snime. Neni cas se hnout z mista.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Uz jsi vyrazil za tim hospodskym? Nic jineho me netrapi. Tady si vse zaridim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("Selatko...");
			d.SetText("Uz mas to selatko?! *nadsene vykrikl*");
			d.Html();
			d.SetText("Nevis, kde je Rachel ted?");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Bohuzel nemam tuseni. Tenkrat prijela svou vlastni lodi... pobyla tam par dni a opet zmizela na mori.");
			d.Html();
		}

		//d_q007_haradir5
		internal static void Haradir5(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 55, "Selatko...");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prozatim tady vychazime akorat. Co nalovime prodame nebo snime. Neni cas se hnout z mista.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Uz jsi vyrazil za tim hospodskym? Nic jineho me netrapi. Tady si vse zaridim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("Selatko...");
			d.SetText("Uz mas to selatko?! *nadsene vykrikl*");
			d.Html();
			d.SetText("Bohuzel, ale uz jsem nasel Rachel. Snad mi pomuze...");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Vyborne! Doufal jsem, ze bude vedet, jak s nim pohnout. Snad je jeji vliv vetsi nez jeho chamtivost.");
			d.Html();
		}

		//d_q007_haradir6
		internal static void Haradir6(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 55, "Selatko...");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prozatim tady vychazime akorat. Co nalovime prodame nebo snime. Neni cas se hnout z mista.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Uz jsi vyrazil za tim hospodskym? Nic jineho me netrapi. Tady si vse zaridim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("Selatko...");
			d.SetText("Uz mas to selatko?! *nadsene vykrikl*");
			d.Html();
			d.SetText("Tak jsem toho hospodskeho presvedcil.");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Tesim se, az mi to selatko doneses. Dlouho s nim neotalej at je cerstve. Uz se nemuzu te lahudky dockat a doufam, ze pratelum zachutna tak, jako kdysi mne.");
			d.Html();
		}

		//d_q007_haradir7
		internal static void Haradir7(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 55, "Selatko...");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prozatim tady vychazime akorat. Co nalovime prodame nebo snime. Neni cas se hnout z mista.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Uz jsi vyrazil za tim hospodskym? Nic jineho me netrapi. Tady si vse zaridim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("Selatko...");
			d.SetText("Uz mas to selatko?! *nadsene vykrikl*");
			d.Html();
			d.SetText("Donesl jsem ti to selatko.");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Vyborne! Tady, dej mi ho sem prosim.");
			d.Html();
			d.SetText("*Predat selatko Haradirovi*");
			d.Response(false, true, "haradir7:1", true);
		}

		//d_q007_haradir8
		internal static void Haradir8(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 52, "Informace");
			d.Page("Pozdrav");
			d.SetText("Take te zdravim!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prozatim tady vychazime akorat. Co nalovime prodame nebo snime. Neni cas se hnout z mista.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Dekuji, ale uz jsi mi <?src.sex(\"pomohl\",\"pomohla\")?> dost. Nic dalsiho pro me udelat nemuzes.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlady, urostly muz. Pres svuj zjevne nevysoky vek se chova velmi jiste a rozvazne. Nevypada nijak agresivne, ale rozzurit ho nebude nejlepsi cestou k zachovani sve tvare bez jizev.");
			d.Html();
			d.Page("Informace");
			d.SetText("Mam pro tebe dva pribehy. Kazdy se tyka jedne vyznamne postavy. S tvymi zkusenostmi by se ti mohly informace v nich obsazene hodit.");
			d.Html();
			d.SetText("Povez mi o Inscripterovi.");
			d.Response(false, false, "p6");
			d.SetText("Povez mi o Kovari.");
			d.Response(false, false, "p7");
			d.Page("p6");
			d.SetText("Dnes je pro ucedniky magie tezke ziskat i nejzakladnejsi dovednosti i kdyz je v dnesni magii jen zlomek toho, co ovladali nejvetsi z carodeju minulych veku. V soucasnem uceni je proto snadne rozlisit magii do kruhu. Pokud se v tomto oboru vyznas jiste vis, ze osmy kruh je bezne nedosazitelny a k jeho ovladnuti je potreba vice nez jen studium. Jak se pres tuto hranici dostat vi jen nekolik malo starcu. Jeden z nich zije ve svem sklepeni kdesi v Severni Spousti. Je velice vzdelany a dokaze odhadnout lidi. Casto o ostatnich vi vice, nez oni sami. Vyhledej ho a uvidi-li v tobe dobreho cloveka, muze ti pomoci se studiem magickych formuli.");
			d.Html();
			d.SetText("Povez mi o Kovari.");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Z dob, kdy se kovaly ty nejvzacnejsi rudy a vznikaly zbroje tak pevne, ze jimi neproletel zadny sip a palice orku na nich nezanechaly stopy, a presto tak lehke, ze ji oblekla i nejjemnejsi divka, z te doby zustalo jen malo moudrych, kteri dosud znaji tajemstvi, jak tyto rudy zpracovavat. Vim vsak o jednom kovari, ktery zna tato tajemstvi. Je opravdovym mistrem sveho remesla a mas-li sam pocit, ze uz ve svem uceni nemuzes dosahnout lepsich vysledku, on ti jiste bude schopen povedet vic. Je to velky samotar, vzdy sem prijde, nakoupi nejake zasoby jidla a kozesin, a zase zmizi cestou k Zeleznym horam. Je to ale veliky dobrak a ver mi, ze se mu zminim o tvych skutcich. Najit ho vsak budes muset sam. Cekat tu na nej by byla ztrata casu. Nekdy se neukaze nekolik mesicu a nikdy se zde prilis nezdrzi.");
			d.Html();
			d.SetText("Povez mi o Inscripterovi.");
			d.Response(false, false, "p6");
		}

		//d_q007_carodejnice
		internal static void Carodejnice(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Abrasum, Xerdum RhoOooOon Huron! ...<br>Mor Gul .... AAAAAh !  Proc mne rusis! Klid se mi z cesty, kvuli tobe jsem znicila elixir!");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se rachel. Vic nepotrebujes vedet.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ano. Zmiz mi z oci!");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlada a jiste puvabna zena. Ovsem stit jejiho puvabu prilepen na odpor ke vsemu okolo mi rika, ze je to uplne jedno.<br>Neustale neco micha a snazi se na to plne soustredit. Zda se, ze tva pritomnost ji v tom nepomaha.");
			d.Html();
		}

		//d_q007_carodejnice2
		internal static void Carodejnice2(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 45, "Hospodsky");
			d.Page("Pozdrav");
			d.SetText("Abrasum, Xerdum RhoOooOon Huron! ...<br>Mor Gul .... AAAAAh !  Proc mne rusis! Klid se mi z cesty, kvuli tobe jsem znicila elixir!");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se rachel. Vic nepotrebujes vedet.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ano. Zmiz mi z oci!");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlada a jiste puvabna zena. Ovsem stit jejiho puvabu prilepen na odpor ke vsemu okolo mi rika, ze je to uplne jedno.<br>Neustale neco micha a snazi se na to plne soustredit. Zda se, ze tva pritomnost ji v tom nepomaha.");
			d.Html();
			d.Page("Hospodsky");
			d.SetText("Hmmm ?");
			d.Html();
			d.SetText("Haradir si mysli, ze by jsi mohla presvedcit Hospodskeho z Ledoveho ostrova splnit jeho slib.");
			d.Response(true, false, "p6");
			d.Page("p6");
			d.SetText("Toho stareho lakotu? AAaaAhahahaha... Je to velice troufale zadat mne o takovou sluzbu. Mozna bych vedela, jak pohnout jeho sebevedomim. Nemam cas starat se o hlouposti. Udelej mi laskavost a pomohu ti.");
			d.Html();
			d.SetText("Dobra, co po mne chces?");
			d.Response(false, false, "p8");
			d.SetText("Nemyslim si, ze bych mel cas delat poskoka kdejake namyslene huse...");
			d.Response(false, true, "carodejnice2:2");
			d.SetText("Jakto, ze mas na toho Hospodskeho takovy vliv?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Nepotrebujes prilis bystrosti k odhaleni skutecnosti, ze pro jedno sele je to az moc povyku, je to tak? Nebud prekvapeny vim presne co Haradirovi slibil. Upeci sele nebo ho hodit do udirny umi kazdy nemehlo. Co vsak dela jeho selatka vyjmecnymi, to je spravna smes koreni. On sam nevi cim ta neboha stvoreni cpe, nezna recept. Dal jiz neni treba vysvetlovat ...");
			d.Html();
			d.SetText("Dobra, co po mne chces?");
			d.Response(false, false, "p8");
			d.SetText("Nemyslim si, ze bych mel cas delat poskoka kdejake namyslene huse...");
			d.Response(false, true, "carodejnice2:2");
			d.Page("p8");
			d.SetText("Potrebuji na sve lektvary spoustu ruznych prisad. Hodne casu travim na ostrovech, kde se daji snadneji nalezt vzacnejsi byliny. Bohuzel vsak existuje prisada, ktere zde mam opravdovy nedostatek. Jsou to kridla netopyru (batwings). Dones mi tisic jejich kridel a pak ti pomohu s hospodskym.");
			d.Html();
			d.SetText("Dobre, udelam to.");
			d.Response(false, true, "carodejnice2:1");
		}

		//d_q007_carodejnice3
		internal static void Carodejnice3(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 45, "Hospodsky");
			d.Page("Pozdrav");
			d.SetText("Abrasum, Xerdum RhoOOoOooO OooOon Huron! ...<br>AAAAAh !  Zase ty!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prave jsem diky tobe zkazila dalsi lektvar... *nenavistne se na tebe podivala*");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Prines mi 1000 netopyrich kridel (batwings), jak jsme se dohodli.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlada a jiste puvabna zena. Ovsem stit jejiho puvabu prilepen na odpor ke vsemu okolo mi rika, ze je to uplne jedno.<br>Neustale neco micha a snazi se na to plne soustredit. Zda se, ze tva pritomnost ji v tom nepomaha.");
			d.Html();
			d.Page("Hospodsky");
			d.SetText("Hmmm ?");
			d.Html();
			d.SetText("Nemohla by jsi toho hospodskeho zatim presvedcit, nez nasbiram ta kridla?");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Ne, nemohla. Nezdrzuj me...");
			d.Html();
		}

		//d_q007_carodejnice4
		internal static void Carodejnice4(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 45, "Hospodsky");
			d.Page("Pozdrav");
			d.SetText("Abrasum, Xerdum RhoOooOon Huron! ...<br>AAAAAh !  Zase ty!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prave jsem diky tobe zkazila dalsi lektvar... *nenavistne se na tebe podivala*");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Prines mi 1000 netopyrich kridel (batwings), jak jsme se dohodli.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlada a jiste puvabna zena. Ovsem stit jejiho puvabu prilepen na odpor ke vsemu okolo mi rika, ze je to uplne jedno.<br>Neustale neco micha a snazi se na to plne soustredit. Zda se, ze tva pritomnost ji v tom nepomaha.");
			d.Html();
			d.Page("Hospodsky");
			d.SetText("Hmmm ?");
			d.Html();
			d.SetText("Mam tady ty kridla, jak jsme se dohodli.");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Vyborne! Tak mi je dej. S tim hospodskym se netrap. Rekni mu, ze jsem te poslala. On si nemuze dovolit ti neverit jeho kuchyne ho zivi. A ted uz jdi mam sve prace dost.");
			d.Html();
			d.SetText("*Provest vymenu*");
			d.Response(false, true, "carodejnice4:1", true);
			d.SetText("Celou dobu mi stacilo namluvit mu, ze me posilas?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("*Usmala se* ... Nekdy je usudek dost chaby, i kdyz se vse zda byt logicke a spravne, je to tak? Jdi uz, mam praci...");
			d.Html();
			d.SetText("*Provest vymenu*");
			d.Response(false, true, "carodejnice4:1", true);
		}

		//d_q007_carodejnice5
		internal static void Carodejnice5(QuestDialogContent d) {
			d.BaseButton(1, 29, "Jak se ti dari?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Abrasum, Xerdum RhoOooOon Huron! ...<br>AAAAAh !  Zase ty!");
			d.Html();
			d.Page("Jak se ti dari?");
			d.SetText("Prave jsem diky tobe zkazila dalsi lektvar... *nenavistne se na tebe podivala*");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Ano. Zmiz mi z oci!");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Mlada a jiste puvabna zena. Ovsem stit jejiho puvabu prilepen na odpor ke vsemu okolo mi rika, ze je to uplne jedno.<br>Neustale neco micha a snazi se na to plne soustredit. Zda se, ze tva pritomnost ji v tom nepomaha.");
			d.Html();
		}

		//d_q007_kovar
		internal static void Kovar(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Jak jsi se sem dostal ? ... Nemam na tebe cas, kovu Mithrillovou zbroj a potrebuji se soustredit.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary kovar, ktery potrebuje klid ke sve praci...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("*usmal se* Dekuji, ale myslim, ze ma moudrost prekona tve mladicke nadseni.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velice stary kovar. V jeho rukou je vsak stale videt obrovska sila. Jak tu tak pri sve praci obiha kolem kovadliny s kladivem v ruce, vsimas si jeho nezvykle obratnosti. Jiz na prvni pohled tak tento kovar sve remeslo dela ohromne dlouho.");
			d.Html();
		}

		//d_q007_kovar2
		internal static void Kovar2(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Aaa. To musis byt ty, o kom mi Haradir vypravel! Vymamil jsi pro nej od hospodskeho selatko je to tak? Vitam te tu. Jsem rad, ze se jeste najdou lide, jako jsi ty.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary kovar, ktery miluje svou praci a samotu...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("*usmal se* Dekuji, ale myslim, ze ma moudrost prekona tve mladicke nadseni. Kdyby jsi si vsak nekdy myslel, ze jsi tam venku nabyl veskerych znalosti kovarskeho remesla, prijd zamnou a ja ti ukazu, jak hluboka nevedomost venku panuje.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velice stary kovar. V jeho rukou je vsak stale videt obrovska sila. Jak tu tak pri sve praci obiha kolem kovadliny s kladivem v ruce, vsimas si jeho nezvykle obratnosti. Jiz na prvni pohled tak tento kovar sve remeslo dela ohromne dlouho.");
			d.Html();
		}

		//d_q007_kovar3
		internal static void Kovar3(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Aaa ty jsi ten, co pomohl Haradirovi ziskat selatko! Je skvele, ze jsou tu stale lide, jako ty.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary kovar, ktery miluje svou praci a samotu...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("*Usmal se* Dekuji, ale myslim, ze ma moudrost prekona tve mladicke nadseni. Kdyby jsi si vsak nekdy myslel, ze jsi tam venku nabyl veskerych znalosti kovarskeho remesla, prijd zamnou a ja ti ukazu, jak hluboka nevedomost venku panuje.");
			d.Html();
			d.SetText("Ano, presne takovy pocit mam! Jsem si temer jist, ze vim o kovarine uplne vse.");
			d.Response(false, false, "p5");
			d.Page("-Prohlednout-");
			d.SetText("Velice stary kovar. V jeho rukou je vsak stale videt obrovska sila. Jak tu tak pri sve praci obiha kolem kovadliny s kladivem v ruce, vsimas si jeho nezvykle obratnosti. Jiz na prvni pohled tak tento kovar sve remeslo dela ohromne dlouho.");
			d.Html();
			d.Page("p5");
			d.SetText("Ano ... Jiste .. tak mi ukaz co umis...");
			d.Html();
			d.SetText("*Predvest svoje schopnosti*");
			d.Response(false, false, "p6", true);
			d.Page("p6");
			d.SetText("- po jistem case -<br>Vyborne ! Prekonal jsi veskere me predstavy ! Uff ... skoro ani nevim, kde bych ti co vytkl. Tva prace je opravdu mistrovska, ale vim o necem, co by ti mohlo pomoci. Znam tajemstvi, jak vytvorit kladivo, kteremu podlehne kazdy kov. S jeho pomoci budes moci pracovat v tak obrovskem zaru, ze ti podlehne i kov ,jako je Mithrill. K vyrobe takoveho kladiva je vsak potreba zvlastnich surovin, jejichz vzacnost by prekonala mithrill samotny.");
			d.Html();
			d.SetText("Co je to za suroviny?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Libis se mi. Jsi primy a nebojis se. Jde o tri kameny, jez jsou jeden bez druheho k nicemu. Spoji-li se ale jejich sily je jejich moc obrovska. Jde o Meteorit, Mesicni kamen a kamen Krve. Nevim ale, kde by jsi mohl najit jediny z nich, natoz pak vsechny. Jsem si ale jist, ze informace o tom, kde je najdes je nekde na prodej. To vsak ponecham na tobe. Dones mi je a ja ti pak budu schopen vyrobit to kladivo.");
			d.Html();
			d.SetText("Dobra, pokusim se ti ty kameny prinest.");
			d.Response(false, true, "kovar3:1");
		}

		//d_q007_kovar4
		internal static void Kovar4(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 57, "Kameny");
			d.Page("Pozdrav");
			d.SetText("Rad te opet vidim. Co te sem privadi ?");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary kovar, ktery miluje svou praci a samotu...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("*Usmal se* Dekuji, ale myslim, ze ma moudrost prekona tve mladicke nadseni.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velice stary kovar. V jeho rukou je vsak stale videt obrovska sila. Jak tu tak pri sve praci obiha kolem kovadliny s kladivem v ruce, vsimas si jeho nezvykle obratnosti. Jiz na prvni pohled tak tento kovar sve remeslo dela ohromne dlouho.");
			d.Html();
			d.Page("Kameny");
			d.SetText("Hmm ?");
			d.Html();
			d.SetText("Opravdu nevis, kde bych ty kameny mohl ziskat?");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Je mi lito.");
			d.Html();
		}

		//d_q007_kovar5
		internal static void Kovar5(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 57, "Kameny");
			d.Page("Pozdrav");
			d.SetText("Rad te opet vidim. Co te sem privadi ?");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary kovar, ktery miluje svou praci a samotu...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("*Usmal se* Dekuji, ale myslim, ze ma moudrost prekona tve mladicke nadseni.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velice stary kovar. V jeho rukou je vsak stale videt obrovska sila. Jak tu tak pri sve praci obiha kolem kovadliny s kladivem v ruce, vsimas si jeho nezvykle obratnosti. Jiz na prvni pohled tak tento kovar sve remeslo dela ohromne dlouho.");
			d.Html();
			d.Page("Kameny");
			d.SetText("Hmm ?");
			d.Html();
			d.SetText("Tady, prinesl jsem ty kameny.");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Dobra, podej mi je at se mohu pustit do prace.");
			d.Html();
			d.SetText("*Predat kameny*");
			d.Response(false, true, "kovar5:1", true);
		}

		//d_q007_kovar6
		internal static void Kovar6(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Rad te opet vidim. Co te sem privadi ?");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary kovar, ktery miluje svou praci a samotu...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("*Usmal se* Dekuji, ale myslim, ze ma moudrost prekona tve mladicke nadseni.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Velice stary kovar. V jeho rukou je vsak stale videt obrovska sila. Jak tu tak pri sve praci obiha kolem kovadliny s kladivem v ruce, vsimas si jeho nezvykle obratnosti. Jiz na prvni pohled tak tento kovar sve remeslo dela ohromne dlouho.");
			d.Html();
		}

		//d_q007_maginscript
		internal static void Maginscript(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Jak jsi se sem dostal ? Nemam na tebe cas, mam pred sebou jeste spoustu knih.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary mag, ktery potrebuje klid ke sve praci...");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Dekuji, ale myslim, ze si vystacim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Na prvni pohled mag, jakych jsi videl spoustu. Stary, vekem strhany a presto velmi dobre upraveny. Na cele ma vyrytu moudrost v nekolika vlnach vrasek. Ze vseho kolem se da usuzovat, ze tento mag ma zajem predevsim o magicke svitky a knihy.<br>Neco v chovani toho starika ti napovida, ze by jsi na nej nemel utocit...");
			d.Html();
		}

		//d_q007_maginscript2
		internal static void Maginscript2(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Aaa. To musis byt ty, o kom mi Haradir vypravel! Vymamil jsi pro nej od hospodskeho selatko je to tak? Vitam te tu.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary mag, ktery potrebuje klid ke sve praci. Pokud by jsi ale nekdy mel touhu naucit zjistit vice o magickych svitcich prijd zamnou a pomohu ti. Nejprve se ale musis sam naucit zaklady s temi ti ja pomoci nemohu. Pouze tva vlastni pile te muze prenest pres tuto prekazku.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Dekuji, ale myslim, ze si vystacim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Na prvni pohled mag, jakych jsi videl spoustu. Stary, vekem strhany a presto velmi dobre upraveny. Na cele ma vyrytu moudrost v nekolika vlnach vrasek. Ze vseho kolem se da usuzovat, ze tento mag ma zajem predevsim o magicke svitky a knihy.<br>Neco v chovani toho starika ti napovida, ze by jsi na nej nemel utocit...");
			d.Html();
		}

		//d_q007_maginscript3
		internal static void Maginscript3(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Aaa. To musis byt ty, o kom mi Haradir vypravel! Vymamil jsi pro nej od hospodskeho selatko je to tak? Vitam te tu.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary mag, ktery potrebuje klid ke sve praci. Citim v tobe velkou moudrost. Myslim ze psani magickych svitku ti neni uplne cizi. Mozna bych ti mohl pomoci ja, pokud bys mel zajem.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Dekuji, ale myslim, ze si vystacim sam. Aaah ale citim kolem tebe obrovskou silu vzdelani. Myslim ze bych ti mohl pomoci ja.");
			d.Html();
			d.SetText("V cem tva pomoc spociva?");
			d.Response(false, false, "p5");
			d.Page("-Prohlednout-");
			d.SetText("Na prvni pohled mag, jakych jsi videl spoustu. Stary, vekem strhany a presto velmi dobre upraveny. Na cele ma vyrytu moudrost v nekolika vlnach vrasek. Ze vseho kolem se da usuzovat, ze tento mag ma zajem predevsim o magicke svitky a knihy.<br>Neco v chovani toho starika ti napovida, ze by jsi na nej nemel utocit...");
			d.Html();
			d.Page("p5");
			d.SetText("Myslim, ze tva znalost magickych svitku dosahla takove urovne, ze je na case naucit se dalsimu kruhu.. *tajemne se usmal* ..Nezistne jsi pomohl Haradirovi, pak ti jiste mohu tyto vedomosti predat, ale nebude to az tak snadne. Budes potrebovat Magickou knihu Inscripce, aby jsi mohl do svitku vkladat magicke formule osmeho kruhu. Mohu ti takovou napsat. Znam pismo i texty, ktere v takove knize musi byt. Aby vsak byla pouzitelna, je treba do ni vlozit magickou silu tri kamenu. Prines mi ty kameny a ja ti obstaram tu knihu.");
			d.Html();
			d.SetText("Co je to za kameny?");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Jde o velice vzacne artefakty. Nektere z nich se na zemi bezne nevyskytuji. Moudri se domnivaji, ze dva z nich spadly z nebes, posledni vznikl pri obrovskem uvolneni energie na konci tretiho veku. Pravda o nich je vsak zahalena hustou mlhou casu a tajemstvi. Jiz prvni spisy o kamenech mluvi neprilis jasne. Avsak jejich moc byla popsana celkem podrobne a mnoho magu jejich sily vyuzivalo pri vyrobe magickych predmetu. Dones mi tedy tyto tri kameny - Meteorit, Mesicni Kamen a Krvavy Kamen. Pamatuj, ze bude-li chybet jediny kamen, nebudou mit ostatni zadnou moc.");
			d.Html();
			d.SetText("Dobra, pokusim se je sehnat.");
			d.Response(false, true, "maginscript3:1");
			d.SetText("Kde ty kameny mohu ziskat?");
			d.Response(false, false, "p7");
			d.Page("p7");
			d.SetText("Bohuzel ti nemohu rici, kde presne je muzes sehnat. Ovsem nekolik takovych kamenu bylo uloupeno. Urcite se da nekde koupit informace, kde se daji sehnat.");
			d.Html();
			d.SetText("Dobra, pokusim se je sehnat.");
			d.Response(false, true, "maginscript3:1");
		}

		//d_q007_maginscript4
		internal static void Maginscript4(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 57, "Kameny");
			d.Page("Pozdrav");
			d.SetText("Opet te tu vitam.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary mag, ktery potrebuje klid ke sve praci. Citim v tobe velkou moudrost. Myslim ze psani magickych svitku ti neni uplne cizi. Mozna bych ti mohl pomoci ja, pokud bys mel zajem.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Dekuji, ale myslim, ze si vystacim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Na prvni pohled mag, jakych jsi videl spoustu. Stary, vekem strhany a presto velmi dobre upraveny. Na cele ma vyrytu moudrost v nekolika vlnach vrasek. Ze vseho kolem se da usuzovat, ze tento mag ma zajem predevsim o magicke svitky a knihy.<br>Neco v chovani toho starika ti napovida, ze by jsi na nej nemel utocit...");
			d.Html();
			d.Page("Kameny");
			d.SetText("Hmmm?");
			d.Html();
			d.SetText("Kde ty kameny mohu ziskat?");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Bohuzel ti nemohu rici, kde presne je muzes sehnat. Ovsem nekolik takovych kamenu bylo uloupeno. Urcite se da nekde koupit informace, kde se daji sehnat.");
			d.Html();
		}

		//d_q007_maginscript5
		internal static void Maginscript5(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 57, "Kameny");
			d.Page("Pozdrav");
			d.SetText("Opet te tu vitam.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite. Jsem jen stary mag, ktery potrebuje klid ke sve praci. Citim v tobe velkou moudrost. Myslim ze psani magickych svitku ti neni uplne cizi. Mozna bych ti mohl pomoci ja, pokud bys mel zajem.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Dekuji, ale myslim, ze si vystacim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Na prvni pohled mag, jakych jsi videl spoustu. Stary, vekem strhany a presto velmi dobre upraveny. Na cele ma vyrytu moudrost v nekolika vlnach vrasek. Ze vseho kolem se da usuzovat, ze tento mag ma zajem predevsim o magicke svitky a knihy.<br>Neco v chovani toho starika ti napovida, ze by jsi na nej nemel utocit...");
			d.Html();
			d.Page("Kameny");
			d.SetText("Hmmm?");
			d.Html();
			d.SetText("Tady, prinesl jsem ty kameny.");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Dobra, podej mi je at se mohu pustit do prace.");
			d.Html();
			d.SetText("*Predat kameny*");
			d.Response(false, true, "maginscript5:1", true);
		}

		//d_q007_maginscript6
		internal static void Maginscript6(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("Rad te tu vidim priteli!");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno neni dulezite.");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Dekuji, ale myslim, ze si vystacim sam.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Na prvni pohled mag, jakych jsi videl spoustu. Stary, vekem strhany a presto velmi dobre upraveny. Na cele ma vyrytu moudrost v nekolika vlnach vrasek. Ze vseho kolem se da usuzovat, ze tento mag ma zajem predevsim o magicke svitky a knihy.<br>Neco v chovani toho starika ti napovida, ze by jsi na nej nemel utocit...");
			d.Html();
		}

		//d_q007_divous
		internal static void Divous(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("*Hic* Hahaa witam wsechny cizinceee ! Kdopak se to sem waali ii ? *Hic* Jehee.. *usmal se* ..wy se tady nemate radi...");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Ja jsem Diwous... *Hic*");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Jo... *Hic* .. Nejaky cas se snazim sehnat Knihu Inscripce. Techto wzacnych knih je *Hic* pra-amalo a jaaa bez ni nemohu post-oupit w uceni. Nemam sice moc co za ni nabidnoot, akorat takowehle pero. *Hic* Kdyz jsem byl mladsi, wyhral jsem ho nad nejakym magem... Hehe to byla tenkrat zabava.. *jeho tvar se pokusila o skodolibe gesto* .. a jak se wydesene sklebil a chtel ho *Hic* zpet! Hehehe *Hic* hee-eh .. no kdyby ho tolik nechtel, snad bych mu ho i dal, stejne newim, k cemu to je. Nemohl jsem wsak *Hic* jeho narku odolat... a ziiiral na svou sazku w mych rukou *Hic* ... No co, kdybys mel nekdy tu knihu, dej wedet...");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Stary ozrala... Zda se, ze byval, nebo nedej Mandos stale JE ucnem magie.");
			d.Html();
		}

		//d_q007_divous2
		internal static void Divous2(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("*Hic* Hahaa witam wsechny cizince ! Kdopak se to sem waali ii ? *Hic* Jehee.. *usmal se* ..wy se tady nemate radi...");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Ja jsem Diwous... *Hic*");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Jo... *Hic* .. Nejaky cas se snazim sehnat Knihu Inscripce. Techto wzacnych knih je *Hic* pra-amalo a jaaa bez ni nemohu post-oupit w uceni. Nemam sice moc co za ni nabidnoot, akorat takowehle pero. *Hic* Kdyz jsem byl mladsi, wyhral jsem ho nad nejakym magem... Hehe to byla tenkrat zabava.. *jeho tvar se pokusila o skodolibe gesto* .. a jak se wydesene sklebil a chtel ho *Hic* zpet! Hehehe *Hic* hee-eh .. no kdyby ho tolik nechtel, snad bych mu ho i dal, stejne newim, k cemu to je. Nemohl jsem wsak *Hic* jeho narku odolat... a ziiiral na svou sazku w mych rukou *Hic* ... No co, kdybys mel nekdy tu knihu, dej wedet...");
			d.Html();
			d.SetText("Co tahle kniha? Neni to takova, jakou jsi chtel?");
			d.Response(false, false, "p5");
			d.Page("-Prohlednout-");
			d.SetText("Stary ozrala... Zda se, ze byval, nebo nedej Mandos stale JE ucnem magie.");
			d.Html();
			d.Page("p5");
			d.SetText("Ano ! To je presne ona! Kde jsi ji *Hic* nasel ? Dej mi ji ! Musim ji mit ...");
			d.Html();
			d.SetText("Dobra, vymenim ji za to pero, ktere ti dal ten mag pred lety.");
			d.Response(false, true, "divous2:1");
			d.SetText("Na tu knihu zapomen! Nesverim ji kdejeke chatre...");
			d.Response(false, true, "divous2:2");
		}

		//d_q007_divous3
		internal static void Divous3(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.Page("Pozdrav");
			d.SetText("*Hic* Hahaa witam wsechny cizince ! Kdopak se to sem waali ii ? *Hic* Jehee.. *usmal se* ..wy se tady nemate radi...");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Ja jsem Diwous... *Hic*");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Jaa uz mam wsechno *Hic*...");
			d.Html();
			d.SetText("Nevymenil by jsi zpet Knihu Inscripce za to pero, co jsi mi dal? Nejak se mi nehodi");
			d.Response(false, false, "p6");
			d.SetText("Nemel by jsi zacit studovat?");
			d.Response(false, false, "p5");
			d.Page("-Prohlednout-");
			d.SetText("Stary ozrala... Zda se, ze byval, nebo nedej Mandos stale JE ucnem magie.");
			d.Html();
			d.Page("p5");
			d.SetText("Nesmysl! Na to je spousta casu ... Tady mam jeste dneska nejakou zabawu *Hic*");
			d.Html();
			d.SetText("Nevymenil by jsi zpet Knihu Inscripce za to pero, co jsi mi dal? Nejak se mi nehodi");
			d.Response(false, false, "p6");
			d.Page("p6");
			d.SetText("Wymenit tu knihu!? *Hic* To nepripada w uwahu... Shanel jsem ji leta...");
			d.Html();
			d.SetText("Nemel by jsi zacit studovat?");
			d.Response(false, false, "p5");
		}

		//d_q007_rodrigo
		internal static void Rodrigo(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 60, "Nabidka");
			d.Page("Pozdrav");
			d.SetText("Tohle neni bezpecne misto... Co tu pohledavas.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno je Rodrigo. Jsem hrdym muzem mori a narokuji si dan z prevozu na lodich od Umbarskeho pristavu po Sede...");
			d.Html();
			d.SetText("Nestacilo by rici, \"jsem pirat\" ?");
			d.Response(false, false, "p6");
			d.Page("Mohu pomoci?");
			d.SetText("Pomoci mne? Takova suchozemska krysa jako ty? Na to zapomen, od tebe nic nechci...");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Celkem upraveny, opaleny muz v kozene uniforme. Absence haku misto jedne ruky a papouska na rameni je jedine, co kazi dokonaly obrazek zaniceneho namornika. Podle drzych gest a jizev na tvari to ale zadny dobrak nebude.");
			d.Html();
			d.Page("Nabidka");
			d.SetText("Jo muzu ti neco nabidnout... Zaplat mi 10 tisic zlatek a mozna budu mit nejakou informaci, ktera te bude zajimat.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo:1", true);
			d.SetText("Nejsem prilis pri penezich, nesla by ta informace ziskat levneji?");
			d.Response(false, false, "p7");
			d.Page("p6");
			d.SetText("Ne.");
			d.Html();
			d.Page("p7");
			d.SetText("Ne.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo:1", true);
			d.SetText("Ja to jen zkusil ...");
			d.Response(false, false, "p8");
			d.Page("p8");
			d.SetText("Jo... To klidne zkousej.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo:1", true);
			d.SetText("Vypada to ze jsi opravdu silny, musel jsi na sobe hodne drit co ?");
			d.Response(false, false, "p9");
			d.Page("p9");
			d.SetText("Jo.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo:1", true);
			d.SetText("No a mohl by jsi mi tu informaci tedy prodat levneji? Co takhle 8 tisic?");
			d.Response(false, false, "p10");
			d.Page("p10");
			d.SetText("Jo.");
			d.Html();
			d.SetText("*Souhlasit a predat 8 tisic gp*");
			d.Response(false, true, "rodrigo:2", true);
			d.SetText("mno .. a co 6k ?");
			d.Response(false, false, "p11");
			d.Page("p11");
			d.SetText("Ne. 10 tisic.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo:1", true);
			d.SetText("Ja to jen zkusil ...");
			d.Response(false, false, "p8");
		}

		//d_q007_rodrigo2
		internal static void Rodrigo2(QuestDialogContent d) {
			d.BaseButton(1, 55, "Kdo jsi?");
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 60, "Nabidka");
			d.Page("Pozdrav");
			d.SetText("Tohle neni bezpecne misto... Co tu pohledavas.");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno je Rodrigo. Jsem hrdym muzem mori a narokuji si dan z prevozu na lodich od Umbarskeho pristavu po Sede...");
			d.Html();
			d.SetText("Nestacilo by rici, \"jsem pirat\" ?");
			d.Response(false, false, "p6");
			d.Page("Mohu pomoci?");
			d.SetText("Pomoci mne? Takova suchozemska krysa jako ty? Na to zapomen, od tebe nic nechci...");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("Celkem upraveny, opaleny muz v kozene uniforme. Absence haku misto jedne ruky a papouska na rameni je jedine, co kazi dokonaly obrazek zaniceneho namornika. Podle drzych gest a jizev na tvari to ale zadny dobrak nebude.");
			d.Html();
			d.Page("Nabidka");
			d.SetText("Jo muzu ti neco nabidnout... Zaplat mi 10 tisic zlatek a mozna budu mit nejakou informaci, ktera te bude zajimat.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo2:1", true);
			d.SetText("Nejsem prilis pri penezich, nesla by ta informace ziskat levneji?");
			d.Response(false, false, "p7");
			d.SetText("Chci se jenom na neco doptat. Proc bych mel platit znovu?!");
			d.Response(false, false, "p12");
			d.Page("p6");
			d.SetText("Ne.");
			d.Html();
			d.Page("p7");
			d.SetText("Ne.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo2:1", true);
			d.SetText("Ja to jen zkusil ...");
			d.Response(false, false, "p8");
			d.Page("p8");
			d.SetText("Jo... To klidne zkousej.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo2:1", true);
			d.SetText("Vypada to ze jsi opravdu silny, musel jsi na sobe hodne drit co ?");
			d.Response(false, false, "p9");
			d.Page("p9");
			d.SetText("Jo.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo2:1", true);
			d.SetText("No a mohl by jsi mi tu informaci tedy prodat levneji? Co takhle 8 tisic?");
			d.Response(false, false, "p10");
			d.Page("p10");
			d.SetText("Jo.");
			d.Html();
			d.SetText("*Souhlasit a predat 8 tisic gp*");
			d.Response(false, true, "rodrigo2:2", true);
			d.SetText("mno .. a co 6k ?");
			d.Response(false, false, "p11");
			d.Page("p11");
			d.SetText("Ne. 10 tisic.");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo2:1", true);
			d.SetText("Ja to jen zkusil ...");
			d.Response(false, false, "p8");
			d.Page("p12");
			d.SetText("Co kdybych mel nejake nove informace? hmm ? Zaplat a reknu ti, co chces vedet...");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo2:1", true);
			d.SetText("Nejsem prilis pri penezich, nesla by ta informace ziskat levneji?");
			d.Response(false, false, "p7");
			d.SetText("A mas nejake nove informace?");
			d.Response(false, false, "p13");
			d.Page("p13");
			d.SetText("Zaplat a uvidis...");
			d.Html();
			d.SetText("*Souhlasit a predat 10 tisic gp*");
			d.Response(false, true, "rodrigo2:1", true);
			d.SetText("Nejsem prilis pri penezich, nesla by ta informace ziskat levneji?");
			d.Response(false, false, "p7");
			d.SetText("To je marny ja jdu pryc...");
			d.Response(false, true, "rodrigo2:3");
		}

		//d_q007_rodrigo_informace
		internal static void RodrigoInformace(QuestDialogContent d) {
			d.BaseButton(2, 51, "Informace");
			d.Page("Informace");
			d.SetText("Taak, co bych ti tak rekl ...<BR> Mam tu informaci o stavu Delfinu v mistnich vodach, novinky o ostrovech na zapade, vim o nejakych uloupenych kamenech, mam zpravu o jedne carodejnici no a jeste bych ti mohl povedet neco o tehle pousti.");
			d.Html();
			d.SetText("Povez mi o delfinech");
			d.Response(false, false, "p2");
			d.SetText("Povez mi o ostrovech");
			d.Response(false, false, "p3");
			d.SetText("Povez mi o kamenech");
			d.Response(false, false, "p4");
			d.SetText("Povez mi o carodejce");
			d.Response(false, false, "p5");
			d.SetText("Povez mi o pousti");
			d.Response(false, false, "p6");
			d.Page("p2");
			d.SetText("Mnozi Umbarsti lovci ryb zacali pouzivat pro vylov site a velke navnady. Nedbaji na nejlepsiho spolecnika piratu a proto se tito krasni tvorove casto do jejich pasti chytaji. Vznikla proto namornicka organizace Modry svet, ktera hlida vody Velkeho more a tresta lovce delfinu. V ramci teto organizace bezi i projekt \"adoptuj si sveho delfina\". Staci dat 50 tisic zlatych a prispejet tak na pomoc temto hrdym tvorum.");
			d.Html();
			d.SetText("Kde mohu prispet?");
			d.Response(false, false, "p7");
			d.Page("p3");
			d.SetText("Na Zapad od pobrezi Umbaru, necha-li tvou lod mocny Ulmo v klidu plout, narazis na nekolik ostrovu. Vetsina z nich je neprilis osidlena. Drive se tamnimi vodami plavili elfove z Sedych pristavu a mytili vsechno zle, co jim stalo v ceste. Ovsem jeden ostrov je necim zvlastni. Jsou to vlastne ostrovy dva, ale prvni tvori prstenec kolem druheho. Rika se jim Slunecni ostrovy a na vnitrnim jeste stale najdes vice zla, nez obycejny smrtelnik snese, a projit tim ostrovem se casto povazuje za posledni zkousku muzstvi, ktera ti dovoli poznat nejzassi kouty tvych schopnosti a sahnout si na dno svych sil.");
			d.Html();
			d.Page("p4");
			d.SetText("Obyvatele techto koncin nejsou prilis spokojeni s tim, ze musi platit za prevoz sveho zbozi. Proto obcas postrasime par troufalcu a zabavime jim vse i s lodi. Nemuzeme si dovolit, aby v hlavach mistnich vzkvetala myslenka na odpor. Pri jednom z poslednich prepadu se vsak jedne z nasich lodi podarilo uloupit zvlastni kameny. Mely vyjmecne zarivou barvu a nepripominaly zadnou ze skal, kterou jsem kdy spatril. Puvodni majitel bohuzel nebyl v dobe objeveni beden plnych sena chraniciho tyto skovsty schopen mluvit a nikdo proto netusi, proc je prevazeli tak dusledne zabalene.");
			d.Html();
			d.SetText("Kde jsou ty kameny ted?");
			d.Response(false, false, "p9");
			d.Page("p5");
			d.SetText("Ano... Carodejnice ... Nikdy jsem nevidel maga krizovat more tak casto, jako ji. Nase lode ji potkavaji kazdou chvili a je jednou z mala osob, kterym dovolime bezstarostne rozcesavat vlasy Uinen, Pani mori. Pokud se nepletu, travi toto rocni obdobi na slunecnim ostrove. Zkus se tam po ni podivat.");
			d.Html();
			d.Page("p6");
			d.SetText("Je tu pisek, spousta prachu a skorpionu... Jinak tu je ale oproti jinym koncinam pomerne klid. Az na tu prisernou valku... Alespon se hejbou obchody se zbranemi a ja mam z ceho zit.");
			d.Html();
			d.Page("p7");
			d.SetText("Ty chces adpotovat delfina? No to je bajecne! Muzes dat ty penize primo me. Jsem clenem Modreho sveta.");
			d.Html();
			d.SetText("*Adoptovat delfina za 50 tisic*");
			d.Response(false, true, "rodrigo_informace:1", true);
			d.SetText("Ne diky tohle neni nic pro me...");
			d.Response(false, false, "p8");
			d.Page("p8");
			d.SetText("Je to jen tve rozhodnuti. Nikdo te nenuti.");
			d.Html();
			d.Page("p9");
			d.SetText("Na ostrove piratu na severu... Je to bezpecne vzdalene mistnim cestam.");
			d.Html();
		}

		//d_q007_rodrigo_informace2
		internal static void RodrigoInformace2(QuestDialogContent d) {
			d.BaseButton(2, 51, "Informace");
			d.Page("Informace");
			d.SetText("Taak zachrance delfinu, co bych ti mohl rict ...<BR> Mam tu informaci o stavu Delfinu v mistnich vodach, novinky o ostrovech na zapade, vim o nejakych uloupenych kamenech, mam zpravu o jedne carodejnici no a jeste bych ti mohl povedet neco o tehle pousti.");
			d.Html();
			d.SetText("Povez mi o ostrovech");
			d.Response(false, false, "p2");
			d.SetText("Povez mi o kamenech");
			d.Response(false, false, "p3");
			d.SetText("Povez mi o carodejce");
			d.Response(false, false, "p4");
			d.SetText("Povez mi o pousti");
			d.Response(false, false, "p5");
			d.Page("p2");
			d.SetText("Na Zapad od pobrezi Umbaru, necha-li tvou lod mocny Ulmo v klidu plout, narazis na nekolik ostrovu. Vetsina z nich je neprilis osidlena. Drive se tamnimi vodami plavili elfove z Sedych pristavu a mytili vsechno zle, co jim stalo v ceste. Ovsem jeden ostrov je necim zvlastni. Jsou to vlastne ostrovy dva, ale prvni tvori prstenec kolem druheho. Rika se jim Slunecni ostrovy a na vnitrnim jeste stale najdes vice zla, nez obycejny smrtelnik snese, a projit tim ostrovem se casto povazuje za posledni zkousku muzstvi, ktera ti dovoli poznat nejzassi kouty tvych schopnosti a sahnout si na dno svych sil.");
			d.Html();
			d.Page("p3");
			d.SetText("Obyvatele techto koncin nejsou prilis spokojeni s tim, ze musi platit za prevoz sveho zbozi. Proto obcas postrasime par troufalcu a zabavime jim vse i s lodi. Nemuzeme si dovolit, aby v hlavach mistnich vzkvetala myslenka na odpor. Pri jednom z poslednich prepadu se vsak jedne z nasich lodi podarilo uloupit zvlastni kameny. Mely vyjmecne zarivou barvu a nepripominaly zadnou ze skal, kterou jsem kdy spatril. Puvodni majitel bohuzel nebyl v dobe objeveni beden plnych sena chraniciho tyto skovsty schopen mluvit a nikdo proto netusi, proc je prevazeli tak dusledne zabalene.");
			d.Html();
			d.SetText("Kde jsou ty kameny ted?");
			d.Response(false, false, "p6", true);
			d.Page("p4");
			d.SetText("Ano... Carodejnice ... Nikdy jsem nevidel maga krizovat more tak casto, jako ji. Nase lode ji potkavaji kazdou chvili a je jednou z mala osob, kterym dovolime bezstarostne rozcesavat vlasy Uinen, Pani mori. Pokud se nepletu, travi toto rocni obdobi na slunecnim ostrove. Zkus se tam po ni podivat.");
			d.Html();
			d.Page("p5");
			d.SetText("Je tu pisek, spousta prachu a skorpionu... Jinak tu je ale oproti jinym koncinam pomerne klid. Az na tu prisernou valku... Alespon se hejbou obchody se zbranemi a ja mam z ceho zit.");
			d.Html();
			d.Page("p6");
			d.SetText("Na ostrove piratu na severu... Je to bezpecne vzdalene mistnim cestam.");
			d.Html();
		}
	}
}
