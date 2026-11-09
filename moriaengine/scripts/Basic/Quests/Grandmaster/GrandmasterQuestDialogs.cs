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

//The texts are Erebor's, byte for byte (questy/velmistr.scp), with its own <src.sex(...)> markup: QuestDialogContent expands it.

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#833 - d_q000_Elqira: an old woman, mother of Elmira and Elvira.</summary>
	public partial class D_Q000Elqira : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int q = GrandmasterQuest.GetStage(player);
			if (q == 0) {
				d.BaseButton(1);
			} else {
				d.BaseButton(1, 50, "Drby...");
			}
			d.BaseButton(2);
			d.BaseButton(3);
			d.BaseButton(4);
			d.BaseButton(5);
			d.BaseButton(6);
			d.Page("Pozdrav");
			if (q == 0) {
				d.SetText("Bud <src.sex(\"zdrav\",\"zdrava\")>.");
				d.Html();
			} else if (q > 10) {
				d.SetText("Rada te znovu vidim! Takove prekvapeni! Kdybych jen tusila, ze prijdes, tak bych neco navarila! <def_questDialog_colorTextEmote>*Zena se pustila do svizneho ometani polic.*<def_questDialog_colorTextNormal> Takova vzacna navsteva a ja tu mam zrovna takovy neporadek!");
				d.Html();
			} else {
				d.SetText("Rada te znovu vidim! <src.sex(\"Nasel\",\"Nasla\")> jsi me dcery?");
				d.Html();
			}
			if (q == 0) {
				d.Page("Kdo jsi?");
				d.SetText("Jmenuji se <name>.");
				d.Html();
			} else {
				d.Page("Drby...");
				d.SetText("A co ja bych ti povidala. S lidma je to porad stejne, temhle vadi tohle, tamtem zase onohle. Porad se mezi sebou certi, ale kdyz na to prijde, tak jsou najednou jak bratri!");
				d.Html();
			}
			d.Page("Mohu pomoci?");
			if (GrandmasterQuest.IsReady(player)) {
				d.SetText("Ah, mne neni pomoci... <def_questDialog_colorTextEmote>*Starena skryla tvar ve svych dlanich a rozplakala se*<def_questDialog_colorTextNormal>");
				d.Html();
				if ((GrandmasterQuest.IsPriest(player)) || (GrandmasterQuest.IsShaman(player))) {
					d.SetText("*Vlozit zeniny ruce do svych*");
					d.Response(false, false, "q000_rightChoice", true);
					d.SetText("Jen se nebojte. Mam nejake zkusenosti, treba vam mohu pomoci!");
					d.Response(false, true, "q000_wrongChoice");
					d.SetText("Prece mi tu nebudes brecet babo! Co ti je?");
					d.Response(false, true, "q000_wrongChoice");
				} else if (player.Realm == Realm.Gondor) {
					d.SetText("*Vlozit zeniny ruce do svych*");
					d.Response(false, true, "q000_wrongChoice", true);
					d.SetText("Jen se nebojte. Mam nejake zkusenosti, treba vam mohu pomoci!");
					d.Response(false, false, "q000_rightChoice");
					d.SetText("Prece mi tu nebudes brecet babo! Co ti je?");
					d.Response(false, true, "q000_wrongChoice");
				} else {
					d.SetText("*Vlozit zeniny ruce do svych*");
					d.Response(false, true, "q000_wrongChoice", true);
					d.SetText("Jen se nebojte. Mam nejake zkusenosti, treba vam mohu pomoci!");
					d.Response(false, true, "q000_wrongChoice");
					d.SetText("Prece mi tu nebudes brecet babo! Co ti je?");
					d.Response(false, false, "q000_rightChoice");
				}
			} else if (q > 10) {
				d.SetText("Dekuji, ale to nejcennejsi jsi mi uz <src.sex(\"zachranil\",\"zachranila\")>.");
				d.Html();
			} else if (q > 0) {
				d.SetText("<src.sex(\"Nasel\",\"Nasla\")> jsi uz moje dcery? <def_questDialog_colorTextEmote>*Elqira na tebe uprene pohledla s nahlou nadeji namahave vytepanou do jeji tvare vekem zmozenymi svaly.*<def_questDialog_colorTextNormal>");
				d.Html();
				if (q == 10) {
					d.SetText("Vase dcery jsou uz v bezpeci.");
					d.Response(false, false, "q000_doughtersROK");
				}
			} else {
				d.SetText("<def_questDialog_colorTextEmote>*Elqira si te peclive prohledla*<def_questDialog_colorTextNormal> Ne, ty mi nepomuzes.");
				d.Html();
			}
			d.Page("Obchodovat");
			d.SetText("Nemam nic na prodej.");
			d.Html();
			d.Page("Hledam praci");
			d.SetText("U me zadnou praci nenajdes.");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Co pro me mas?");
			d.Html();
			d.Page("Prohlednout");
			if (GrandmasterQuest.IsReady(player)) {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis velmi starou zenu s hrbetem prohnutym tezkou praci v dome a udy ztuhlymi zimou. Jeji chuze je pomala a oci propadle. Zrejme ji neco velmi tizi.*");
			} else if (q > 9) {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis velmi starou zenu s hrbetem prohnutym tezkou praci v dome a udy ztuhlymi zimou. Jeji elan je k nepoznani. Poskakuje od jedne police ke druhe, nepostoji na jednom miste a jeji vrascity oblicej na tebe vrha hned nekolik usmevu nepravidelne razenych nad sebou.*");
			} else if (q > 0) {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis velmi starou zenu s hrbetem prohnutym tezkou praci v dome a udy ztuhlymi zimou. Sotva se drzi na nohou, ale v jejich ocich je videt zmena. Kapka nadeje, kterou ji tvuj slib pomoci prinesl, ozivila jeji stare krivky a prinesla trochu svetla do jejich temnych vyhlidek na zbytek stari o samote.*");
			} else {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis velmi starou zenu s hrbetem prohnutym tezkou praci v dome a udy ztuhlymi zimou.*");
			}
			d.Html();
			if (GrandmasterQuest.IsReady(player)) {
				d.Page("q000_rightChoice");
				if (GrandmasterQuest.IsPriest(player)) {
					d.SetText("<def_questDialog_colorTextEmote>*Zena na tebe z prvu rozpacite pohledla. Teplo z tvych dlani ji zacalo pomalu uklidnovat. Tve rty se zvolna rozpohybovaly. Konecky prstu se rozzarily svetelnymi pulzy blede modre barvy a zena pocitila slabe brneni v pazich, jak v jejim tele rostlo napeti. Krev se vlila do tvare ustarane matky a vysusila jeji vlhke lice. Pak jsi sve ruce opet <src.sex(\"stahl\",\"stahla\")>. Starena na tebe jeste chvili prekvapene zirala.*<def_questDialog_colorTextNormal> Ah... Omlouvam se... Za svou hloupost, me chovani, ja... Muzes mi prosim pomoci?!");
				} else if (player.Realm == Realm.Gondor) {
					d.SetText("Ty? <def_questDialog_colorTextEmote>*Zena, zda se, poprve poradne prozkoumala, s kym mluvi.*<def_questDialog_colorTextNormal> Mozna, ze stoji za to nekomu se sverit...");
				} else {
					d.SetText("Silna slova ! <def_questDialog_colorTextEmote>*Zena si hrubymi prsty utrela slzy.*<def_questDialog_colorTextNormal> Ja potrebuji nekoho, kdo se neboji jednat primo. Me trapeni nevyresi ti mistni povaleci, co pro me maji jen slova utechy! Dam ti vse, co mam, pokud mi pomuzes!");
				}
				d.Html();
				d.SetText("Prosim, jen mluv.");
				d.Response(false, false, "q000_questAssignment");
				d.Page("q000_questAssignment");
				d.SetText("Mam dve dcery, Elmiru a Elviru. Cely svuj zivot jsem jim obetovala. Ucila jsem je rozpoznat kdejaky ker, kazdou bylinu, kazde zvire i kamen v lese. Dnes si to uz jen vycitam! Probudila jsem ve svych dcerach prilis velkou zvedavost. Valka zene pres nas les mnoho tulaku. Od jednoho takoveho se ma mladsi dcera, Elmira, doslechla o zemi daleko na vychode Ledovych plani. Holka je to mlada a blazniva, podlehla slovum o krasne a bohate prirode, o skalach posteych ledovymi polstari, o stromech bujicich v celorocni pokryvce snehu a o bytostech, jednech tvrdych jako kamen a studenych jak led, druhych jemnych jako vitr a divokych jako blesk.");
				d.Html();
				d.SetText("*Poslouchat dal*");
				d.Response(false, false, "q000_questAssignment_2", true);
				d.Page("q000_questAssignment_2");
				d.SetText("Tak se pro to misto nadchla, ze nemluvila o nicem jinem a netrvalo dlouho a plamen sveho nadseni rozdmychala i ve starsi Elvire. Marne jsem jim domlouvala, at zustanou doma a nepletou si hlavu dalekym svetem. Pred dobrym rokem si sbalily veci a odesly. Jeste par tydnu prichazely zvesti od pocestnych a obchodnicich cestujicich putujicich na jih. Pred par mesici vsak veskere zpravy od nich utichly. Neni jim to podobne. Uz od mala o sobe davaly vedet, kdykoliv se treba jen o hodinu opozdily. Muselo se jim neco stat! Nikdo me tu neposloucha! <def_questDialog_colorTextEmote>*Elqira se znovu bourlive rozplakala*<def_questDialog_colorTextNormal> Najdi prosim te me dcery!");
				d.Html();
				d.SetText("Dobra! Vydam se na Ostrovy narku.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se na Slunecni ostrov.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se do Temneho hvozdu.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se na Ostrov Zatemnele mysli.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se do Haradske pouste.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se do Obrich lesu.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se k rhunskemu jezeru.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se na Ledove plane.");
				d.Response(false, false, "q000_questAssignment_3");
				d.SetText("Dobra! Vydam se na Ostrov Titanu.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se do Lorienu.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se na Ostrov Divu.");
				d.Response(false, true, "q000_wrongChoice2");
				d.SetText("Dobra! Vydam se do Rhunske pouste.");
				d.Response(false, true, "q000_wrongChoice2");
				d.Page("q000_questAssignment_3");
				d.SetText("<src.splitRealm(\"Iluvatar\",\"Melkor\")> ti bud ku pomoci! Nedovedes si predstavit, co pro me tva pomoc znamena!");
				d.Html();
				d.SetText("*Zeptat se na popis dcer a odejit.*");
				d.Response(false, true, "q000_questAssignment", true);
			} else if (q == 10) {
				d.Page("q000_doughtersROK");
				d.SetText("Ah zaplat <src.splitRealm(\"Iluvatar\",\"Melkor\")>! Ty jsi to skutecne <src.sex(\"dokazal\",\"dokazala\")>! Moje holcicky! <def_questDialog_colorTextEmote>*Elqira opet usedave place*<def_questDialog_colorTextNormal> Na svete neni daru, ktery by mohl splatit muj dluh tobe! Slibuji ti, ze vsem povim jak <src.splitRealm(\"<src.sex(\"uslechtily a statecny\",\"uslechtila a statecna\")>\",\"<src.sex(\"silny a nebojacny\",\"silna a nebojacna\")>\")> jsi! Ja mam takovou radost, tu mas, vezmi si alespon neco maleho. Je to to nejmensi, co ti mohu za tvou pomoc nabidnout.");
				d.Html();
				d.SetText("*Prevzit dar od Elqiry a odejit*");
				d.Response(false, true, "q000_questSolved", true);
			}
		}
	}

	/// <summary>#833 - d_q000_Elmira: the younger daughter, held in a cage.</summary>
	public partial class D_Q000Elmira : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int q = GrandmasterQuest.GetStage(player);
			if (q < 3) {
				d.BaseButton(1);
			}
			d.BaseButton(2);
			d.BaseButton(3);
			d.BaseButton(4);
			d.BaseButton(5);
			d.BaseButton(6);
			d.Page("Pozdrav");
			if (q < 3) {
				d.SetText("<def_questDialog_colorTextEmote>*Zena se vyplasene rozhlizi.*<def_questDialog_colorTextNormal>");
				d.Html();
			} else if ((q == 3) || (q == 4)) {
				d.SetText("Rada te znovu vidim! Nasel jsi mou sestru?");
				d.Html();
				if (q == 4) {
					d.SetText("<def_questDialog_colorTextResponse><src.sex(\"Nasel\",\"Nasla\")> jsem ji. Je v lese a sotva se drzi na nohou.");
					d.Response(true, false, "q000_foundElvira");
				}
				d.SetText("Ne, je mi lito...");
				d.Response(false, true, "q000_notFoundElvira");
			} else if (q < 10) {
				d.SetText("Ah to jsi ty! Podarilo se ti nalezt koren Este pro mou sestru!?");
				d.Html();
				d.SetText("Ne, bohuzel jeste ne");
				d.Response(false, false, "q000_NotFoundHerb");
				if (q == 6) {
					d.SetText("Ano, mam ho!");
					d.Response(false, false, "q000_FoundHerb");
				} else if ((q == 7) || (q == 8)) {
					d.SetText("Ne jen to, uz jsem ho dal tve sestre a je v poradku! Ted jsi na rade ty.");
					d.Response(false, false, "q000_promiseRescue");
				} else if (q == 9) {
					d.SetText("Kdyby jen to! Rinald, muz co te veznil, je mrtev!");
					d.Response(false, false, "q000_killedRinald");
				}
			} else {
				d.SetText("Rada te znovu vidim!");
				d.Html();
			}
			if (q < 3) {
				d.Page("Kdo jsi?");
				d.SetText("<name>, dcera Elqiry. Nemuzu s tebou mluvit!");
				d.Html();
				d.SetText("*Odejit.*");
				d.ExitResponse(false, true);
				if (q > 0) {
					d.SetText("<def_questDialog_colorTextResponse><name>? Tva matka me pozadala, abych tebe a tvou sestru <src.sex(\"nasel\",\"nasla\")>! Ma o vas strach a divi se, ze jste ji dlouho nedaly vedet, co s vami je.");
					d.Response(true, false, "q000_motherSentMe");
				}
			}
			d.Page("Mohu pomoci?");
			if (q < 3) {
				d.SetText("Promin, ale ty mi nepomuzes! Odejdi! <def_questDialog_colorTextEmote>*<name> se rozrusene rozhlizi kolem.*<def_questDialog_colorTextNormal>");
				d.Html();
				d.SetText("*Odejit.*");
				d.ExitResponse(false, true);
			} else if (q < 8) {
				d.SetText("Vzdyt vis, co mas delat! Rychle odejdi, nez te tu uvidi!");
				d.Html();
				d.ExitResponse();
				if (q == 6) {
					d.SetText("<src.sex(\"Nasel\",\"Nasla\")> jsem koren Este!");
					d.Response(false, false, "q000_FoundHerb");
				}
			} else {
				d.SetText("Dekuji ti, ale uz jsi pro me <src.sex(\"udelal\",\"udelala\")> vsechno, co jsi <src.sex(\"mohl\",\"mohla\")>.");
				d.Html();
			}
			d.Page("Obchodovat");
			if (q < 10) {
				d.SetText("Se mnou?");
			} else {
				d.SetText("Ach ja nic nemam. <def_questDialog_colorTextEmote>*<name> se usmala*<def_questDialog_colorTextNormal>");
			}
			d.Html();
			d.Page("Hledam praci");
			if (q < 10) {
				d.SetText("!?");
				d.Html();
				d.SetText("*Chapes a odchazis...*");
				d.ExitResponse(false, true);
			} else {
				d.SetText("U me zadnou praci nenajdes.");
				d.Html();
			}
			d.Page("Nesu ti");
			if (q < 3) {
				d.SetText("Jdi pryc! <def_questDialog_colorTextEmote>*<name> ma zrejme velky strach.*<def_questDialog_colorTextNormal>");
				d.Html();
				d.SetText("*Odejit.*");
				d.ExitResponse(false, true);
			} else {
				d.SetText("Co pro me mas?");
				d.Html();
			}
			d.Page("Prohlednout");
			if (q < 3) {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis divku zavrenou v kleci. Saty ma potrhane a cela se trese zimou.*<def_questDialog_colorTextNormal>");
			} else if (q < 10) {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis <name> zavrenou v kleci. Saty ma potrhane a cela se trese zimou. Tva pritomnost ji, zda se, trochu uklidnuje.*<def_questDialog_colorTextNormal>");
			} else {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis <name>. Diky tobe je ted volna a muze se tesit z krajiny, kterou sem se svou sestrou prisla obdivovat. Vdecne na tebe vrha hrejivy pohled, kdykoliv ji oslovis.*<def_questDialog_colorTextNormal>");
			}
			d.Html();
			if (q < 3) {
				if (q > 0) {
					d.Page("q000_motherSentMe");
					d.SetText("Tys <src.sex(\"mluvil\",\"mluvila\")> s mou matkou! Prosim te, pomoz mi! Nejaky muz me tu uveznil a stiha mou sestru! Mne neni pomoci, ale ji muzes zachranit! Videla jsem ji prchnout pred timto strasnym muzem primo do snehove boure. Nemohla utect daleko a ja vim, ze by me tu nikdy jen tak nenechala! Dej si pozor a vyhni se tomu muzi, ma obrovskou silu! Gondor ani Mordor tu nema zadnou moc, tady vladne on!");
					d.Html();
					if (q == 1) {
						d.SetText("<def_questDialog_colorTextResponse>Dobra, pokusim se najit tvou sestru, ale v teto nehostinne krajine to nebude snadne. <src.splitRealm(\"Zatim se tu opatruj!\",\"Ne ze mi tu zatim chcipnes!\")>");
						d.Response(true, true, "q000_goFindElvira");
					} else {
						d.SetText("<def_questDialog_colorTextResponse>Ja jsem uz tvou sestrou <src.sex(\"nasel\",\"nasla\")>. Je v lese a sotva se drzi na nohou. Kdykoliv se ji pokusim oslovit, tak jen nepritomne vykrikuje nesmyslna hesla. <src.splitRealm(\"Mam o ni strach.\",\"Ta to ma spocitane, moc bych ji <src.sex(\"nedaval\",\"nedavala\")> byt tebou.\")>");
						d.Response(true, false, "q000_foundElvira");
					}
					d.SetText("<src.splitRealm(\"Promin, ale ted nemohu...\",\"Hah! Na to ti cvrka pes ty blazniva!\")>");
					d.Response(false, true, "q000_refuse");
				}
			}
			if ((q == 2) || (q == 4)) {
				d.Page("q000_foundElvira");
				d.SetText("Ach ne! Co je ji? Mluv!");
				d.Html();
				d.SetText("*Popsat Elmire stav jeji sestry.*");
				d.Response(false, false, "q000_describeElvira", true);
				d.Page("q000_describeElvira");
				d.SetText("<def_questDialog_colorTextEmote>*<name> zkrivila svuj mrazem potrhany oblicej, jeji spodni celist nekolikrat spadla pri Elmirine pokusu udrzet telo pohromade. Nakonec se ji nohy podlomily. Svalila se ve svem vezeni na promrzlou skalu a s hlavou v kolenou zacla hlasite narikat*");
				d.Html();
				d.SetText("*Dat Elmire cas*");
				if (player.Realm == Realm.Gondor) {
					d.Response(false, false, "q000_receiveInstructions_g", true);
				} else {
					d.Response(false, true, "q000_refuseHelp", true);
				}
				d.SetText("Vzpamatuj se zenska! Vzdyt te jeste nekdo uslysi!");
				if (player.Realm == Realm.Gondor) {
					d.Response(false, true, "q000_refuseHelp");
				} else {
					d.Response(false, false, "q000_receiveInstructions_m");
				}
				if (player.Realm == Realm.Gondor) {
					d.Page("q000_receiveInstructions_g");
					d.SetText("<def_questDialog_colorTextEmote>*Elmira se pomalu sklidnila a jeji ruce se zatly v pest. Zdvihla k tobe sve oci. Dlouze se zahledela a nasledne odpoutala od zeme i cele sve podvyzivene telo.*<def_questDialog_colorTextNormal> Vim, jak sve sestre muzu pomoct...");
					d.Html();
				} else {
					d.Page("q000_receiveInstructions_m");
					d.SetText("<def_questDialog_colorTextEmote>*Elmiru tva vyzva zcela sokovala. Nejprve se jeji doprazdna uprene oci pocaly zastavovat na prekazkach, ktere ji ocelova komora kladla do cesty, pote je zdvihla k tobe. Zatla ruce v pest, dlouze se zahledela a nasledne odpoutala od zeme i cele sve podvyzivene telo.*<def_questDialog_colorTextNormal> Vim, jak sve sestre muzu pomoct...");
					d.Html();
				}
				d.SetText("*Pokynout Elmire, at pokracuje.*");
				d.Response(false, false, "q000_receiveInstructions", true);
				d.Page("q000_receiveInstructions");
				d.SetText("Znam jeji chorobu. Mnoho lidi, ktere jsme na sve ceste sem potkali trpelo podobnymi stavy. Nekteri z nich vypraveli o byline, korenu Valar Este, jako jedinem moznem leku. V dobach, kdy Este spolu s manzelem Irmem sidlili na ostrove Lorellinskeho jezera, jim byl posety cely Lorien. Jeho kvety vsak uvadly spolu s odchodem Valar. Posledni vyhonky teto vzacne byliny zmizely ze stredozeme pred stovkami let. Podle povesti nekolik malo semen zasadila Este v temne jeskyni, ktera se nachazi na vyhodni casti pohori Ered Mithrin, aby ji nikdo snadno neziskal. Dnes je tato jeskyne obyvana nejruznejsimi nestvurami, ktere moc teto kvetiny prilakala. Jedina nadeje me sestry je v techto nekolika malo sazenicich! Prosim, pomoz ji! Mela bych te ale varovat. Nestvur v jeskyni je prilis i na tebe. Premluv k ceste jine, silnejsi bojovniky, ale vydej se tam s nimi a nikomu nesdeluj pravy smysl tve cesty. Ta bylina leci mnohe nemoci a clovek, kteremu bys sveril jeji doruceni by se s ni nejspis nevratil.");
				d.Html();
				d.SetText("Dobra! Vydam se na Ostrovy narku.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se na Slunecni ostrov.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se do Temneho hvozdu.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se na ostrov Zatemnele mysli.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se do Haradske pouste.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se na Ostrov Titanu.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se k rhunskemu jezeru.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se k pohori Ered Mithrin.");
				d.Response(false, false, "q000_receiveInstructions_2");
				d.SetText("Dobra! Vydam se do Lorienu.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se na ostrov Divu.");
				d.Response(false, true, "q000_wrongChoice");
				d.SetText("Dobra! Vydam se do Rhunske pouste.");
				d.Response(false, true, "q000_wrongChoice");
				d.Page("q000_receiveInstructions_2");
				d.SetText("Vyborne! <def_questDialog_colorTextEmote>*Elmira na tebe hledi s nadeji*");
				d.Html();
				d.SetText("*Ziskat popis rostliny a vydat se k pohori Ered Mithrin*");
				d.Response(false, true, "q000_goForHerb", true);
			}
			if ((q > 4) && (q < 10)) {
				d.Page("q000_NotFoundHerb ");
				d.SetText("Tak spechej!");
				d.Html();
				d.ExitResponse();
				if (q == 6) {
					d.Page("q000_FoundHerb");
					d.SetText("<def_questDialog_colorTextEmote>*Elmire zvhlhly koutky oci radosti*<def_questDialog_colorTextNormal> Prosim spechej za Elvirou a koren ji dej! Ta rostlina je velmi mocna, zabere-li, poznas to rychle.");
					d.Html();
					d.SetText("Dobre, jdu za ni.");
					d.ExitResponse(false, false);
				} else if ((q == 7) || (q == 8)) {
					d.Page("q000_promiseRescue");
					d.SetText("Sestra je v poradku! Ja, ja .. Ja nevim co rict! Dekuji ti z celeho sveho srdce! Prosim, bud opatrny, ten muz je velmi nebezpecny.");
					d.Html();
					d.ExitResponse();
				} else if (q == 9) {
					d.Page("q000_killedRinald");
					d.SetText("Vyborne! Jak se ale dostanu z te klece ?");
					d.Html();
					d.SetText("Rinald mel u sebe klic.");
					d.Response(false, false, "q000_IHaveAKey");
					d.SetText("Tak to netusim.");
					d.Response(false, false, "q000_HowToOpen");
					d.Page("q000_IHaveAKey");
					d.SetText("Tak na co jeste cekas !");
					d.Html();
					d.SetText("*Jit vyzkouset klic*");
					d.ExitResponse(false, true);
					d.Page("q000_HowToOpen");
					d.SetText("Tak neco zkus!");
					d.Html();
					d.SetText("*Jit neco zkusit*");
					d.ExitResponse(false, true);
				}
			}
		}
	}

	/// <summary>#833 - d_q000_Elvira: the elder daughter, spellbound until healed.</summary>
	public partial class D_Q000Elvira : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int q = GrandmasterQuest.GetStage(player);
			d.BaseButton(2);
			d.BaseButton(3);
			d.BaseButton(4);
			d.BaseButton(5);
			d.BaseButton(6);
			d.Page("Pozdrav");
			if (q == 7) {
				d.SetText("Zdravim te <src.sex(\"priteli\",\"pritelkyne\")> a dekuji ti za lek. Kdo jsi?");
				d.Html();
				d.SetText("<def_questDialog_colorTextResponse>Me jmeno je <src.name> a posila me tva matka, abych <src.sex(\"nasel\",\"nasla\")> tebe a tvou sestru. Povez mi, co se stalo?");
				d.SaveText("q000_tellTheStory");
				d.Response(true, false, "q000_tellTheStory");
			} else {
				d.SetText("Vitam te zpatky!");
				d.Html();
			}
			d.Page("Mohu pomoci?");
			if (q == 7) {
				d.SetText("Prozrad mi co se se mnou stalo? A kdo jsi, <src.sex(\"Zachranil\",\"Zachranila\")> jsi me a ja ani nevim, komu za to vdecim.");
				d.Html();
				d.SameResponse(true, false, "q000_tellTheStory");
			} else {
				d.SetText("Nevim, co bys pro me <src.sex(\"mohl\",\"mohla\")> udelat.");
				d.Html();
			}
			d.Page("Obchodovat");
			d.SetText("Nemam nic na prodej.");
			d.Html();
			d.Page("Hledam praci");
			d.SetText("U me zadnou praci nenajdes.");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Co pro me mas?");
			d.Html();
			d.Page("Prohlednout");
			if (q == 7) {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis Elviru, sestru Elmiry a dceru Elqiry. Po podani leciveho korene je jeste trochu dezorientovana, ale do tvari se ji pomalu vraci zdrava barva. Tmave vlasy a ostre lice napadne pripominaji, i pres znacny vekovy rozdil, jeji matku.*");
			} else if (q ==8) {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis Elviru, sestru Elmiry a dceru Elqiry. Barva jeji kuze je uz zdrava a jeji projev je opet plny zivota. Na cele ji vsak ve vlnach svrastele kuze ctes starost o jeji sestru. Tmave vlasy a ostre lice napadne pripominaji, i pres znacny vekovy rozdil, jeji matku.*");
			} else {
				d.SetText("<def_questDialog_colorTextEmote>*Vidis Elviru, sestru Elmiry a dceru Elqiry. Tmave vlasy a ostre lice napadne pripominaji, i pres znacny vekovy rozdil, jeji matku. <src.sex(\"Podal\",\"Podala\")> jsi ji koren Este, ktery ji vylecil na tele. Zachranila jsi jeji sestru z uvezneni krutym Rinaldem a uzdravila tak jeji dusi. Ted pred tebou stoji silna, spokojena zena, ktera se opet obdivuje prirode. Obdiv, pro ktery kdysi malem polozila se svou sestrou zivot.");
			}
			d.Html();
			if (q == 7) {
				d.Page("q000_tellTheStory");
				d.SetText("<def_questDialog_colorTextEmote>*Elvira chvili patrala v pameti a pak zacala vypravet*<def_questDialog_colorTextNormal> Kdyz jsme se sestrou prisly do Ledovych plani, byly jsme nadsene. Vse se zdalo byt jeste krasnejsi, nez jsme si kdy predstavovaly, jen obcasna karavana plna pomatenych lidi vrhala slaby stin na nasi mysl. Prisuzovaly jsme to vzdy nehostinnym podminkam ledovych plani a dale si toho nevsimaly. Krmily jsme zver, hledaly nove rostliny a po lovcich prochazejicich timto krajem jsme domu posilay zpravy matce... <def_questDialog_colorTextEmote>*Elvira, stale jeste v soku z prodelane nemoci, zastenala a na chvili se odmlcela.*");
				d.Html();
				d.SetText("Prosim pokracuj.");
				d.Response(false, false, "q000_tellTheStory_2");
				d.Page("q000_tellTheStory_2");
				d.SetText("Jak jsme cestovaly dal na vychod, krajina houstla a potkavaly jsme stale mene a mene lidi. Nakonec jsme byly cele dva tydny odkazany samy na sebe v nehostinne krajine. Po prvnim tydnu zacalo slabe snezit a po dalsich dvou dnech jsme pochodovaly v silne bouri. Dostaly jsme se az ke skale a nasly v ni utociste. Behem neceleho tydne se boure uklidnila natolik, ze jsme se odvazily ven. Tehdy jsme zjistily, ze tu nejsme samy. V planine nekolik kilometru na jih jsme nasly rozvaliny, vyhasle kempy a obcas i stopy lidi. <def_questDialog_colorTextEmote>*Elvira opet lehce zastenala*<def_questDialog_colorTextNormal> Nikdo tu nepostal, vsichni se hned presouvali jinam. Zacalo nam to byt podezrele. Jeste par dni jsme bloudily krajinou, nez se nebe znovu zatahlo a byly jsme nuceny se opet schovat do skal...");
				d.Html();
				d.SetText("*Nechat Elviru mluvit dal.*");
				d.Response(false, false, "q000_tellTheStory_3", true);
				d.Page("q000_tellTheStory_3");
				d.SetText("Elmira sla shanet drevo a ja sla do jeskyne pripravit zajice, ktereho jsme ulovily. A tam jsem ho uvidela. <def_questDialog_colorTextEmote>*Elvira se vydesene zahledela do prazdna*<def_questDialog_colorTextNormal> Muz zjizveny a krivy. Pri chuzi jako by plul vzduchem a zeme se dotykal jen par prstu nad ni. Zprvu me nespatril a hovoril sam k sobe. Bodal do sebe jehlici napustenou v ruznych latkach a neustale se pri tom oslovoval jmenem Rinald. Pak prestal a dosel ke svym psum, bestiim hladem dohnanym k silenstvi. Nabidl jim svou ruku a bavil se vzteklemu vrceni tech zrud, jak se ji snazily rozervat! Na jeho pazi vsak nebyl jediny krvavy sram! Mocna sila musi chranit toho muze pred kazdym, kdo nezna jeho jmeno...");
				d.Html();
				d.SetText("Jak jsi ale prisla ke svemu zraneni?");
				d.Response(false, false, "q000_tellTheStory_4 ");
				d.Page("q000_tellTheStory_4");
				d.SetText("Kdyz jsem se snazila vyplizit, napadl me jeden z jeho psu. Rinald to okamzite zpozoroval a zacal vyslovovat slozita hesla. Zbytek si uz vybavuji jen velmi mlhave. Obrovsky zar v zadech, stekot psu a dunivy zvuk bortici se hory. Vybehla jsem z jeskyne a spechala zacinajici bouri nejrychlejsi cestou od Rinaldovy skryse. Zvedajici se vitr rychle halil me stopy zavejemi a spolu se silnym snezenim omezoval rozhled na nekolik malo metru. Tak se mi se stestim podarilo utect. Pak jsem ztratila vedomi. Moje dalsi vzpominkou jsi ty, jak se nademnou sklanis s korenem myticke byliny v rukou! Jsem ti zavazana svym zivotem a dluzim ti vice, nez kdy budu schopna splatit. Presto te musim poprosit o dalsi vec! Mas zbytky lecive byliny, znas jmeno neznameho muze a jsi bojem <src.sex(\"ostrileny\",\"ostrilena\")>. Vyuzij prosim svych znalosti a vysvobod moji sestru!");
				d.Html();
				d.SetText("Muzes mi vysvetlit, jak jsi se dostala pres tu rokli?");
				d.Response(false, false, "q000_ravine");
				d.SetText("Chapu. Uvidim, co mohu delat.");
				d.Response(false, true, "q000_goKillRinald");
				d.Page("q000_ravine");
				d.SetText("O jake rokli mluvis?");
				d.Html();
				d.SetText("Ale nic, uvidim, co pro tebe muzu udelat.");
				d.Response(false, true, "q000_goKillRinald");
				d.SetText("<def_questDialog_colorTextResponse>Kousek na sever od tebe je obrovska rokle, kterou jsem <src.sex(\"musel\",\"musela\")> obihat pul dne pres mosty na zapade.");
				d.Response(true, false, "q000_ravine_2");
				d.Page("q000_ravine_2");
				d.SetText("Nemam tuseni, asi jsem musela v tom poblouzneni vylezt nahoru.");
				d.Html();
				d.SetText("Steny te rokle jsou sto metru vysoke...");
				d.Response(false, false, "q000_ravine_3");
				d.Page("q000_ravine_3");
				d.SetText("Oh...");
				d.Html();
				d.SetText("Jen oh?");
				d.Response(false, false, "q000_ravine_4");
				d.SetText("<def_questDialog_colorTextResponse>Take jsou pokryte ledem a v rokli je pri bouri dost silny vitr, ktery velmi ponicil i peclive pripevnene mosty.");
				d.SaveText("q000_ravine_5");
				d.Response(true, false, "q000_ravine_5");
				d.Page("q000_ravine_4");
				d.SetText("Ou... <def_questDialog_colorTextEmote>*Elvira, zda se, upenlive premysli*<def_questDialog_colorTextNormal>");
				d.Html();
				d.SameResponse(true, false, "q000_ravine_5");
				d.Page("q000_ravine_5");
				d.SetText("<def_questDialog_colorTextEmote>*Ted uz na tebe Elvira jen spokojene hledi bez jedine reakce.*<def_questDialog_colorTextNormal>");
				d.Html();
				d.SetText("Eh... No dobra! Uvidim, co se pro tvou sestru da udelat.");
				d.Response(false, true, "q000_goKillRinald");
			}
		}
	}

	/// <summary>#833 - d_q000_Rinald: the man who holds Elmira.</summary>
	public partial class D_Q000Rinald : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int q = GrandmasterQuest.GetStage(player);
			d.Page("Pozdrav");
			d.SetText("Co tu delas ?! Co mi chces ty <src.sex(\"chudaku\",\"ubozacko\")>!");
			d.Html();
			d.SetText("Vim, ze drzis v zajeti Elmiru, pust ji!");
			d.Response(false, false, "q000_releaseElmira");
			d.Page("q000_releaseElmira");
			d.SetText("Ty spino! Okamzite mi zmiz z oci, mas stesti ze na tebe nemam cas! <def_questDialog_colorTextEmote>*Muz vzal do rukou nuz a parkrat jim teatralne prejel po sve pazi, na ktere nezustal jediny sram. Rinald se na tebe podival a hlasite se zacal smat.*<def_questDialog_colorTextNormal> Vidis, s cim si tu zahravas? Bez meho jmena jsi proti mne <src.sex(\"bezmocny\",\"bezmocna\")>. A me jmeno nikdo nezna!");
			d.Html();
			d.SetText("Bilbo!");
			d.Response(false, true, "q000_wrongName");
			d.SetText("Frodo!");
			d.Response(false, true, "q000_wrongName");
			d.SetText("Melkor!");
			d.Response(false, true, "q000_wrongName");
			if (q == 8) {
				d.SetText("Rinald!");
				d.Response(false, true, "q000_tellHisName");
			}
			d.SetText("Illuvatar!");
			d.Response(false, true, "q000_wrongName");
			d.SetText("Dante!");
			d.Response(false, true, "q000_wrongName");
			d.SetText("Karel!");
			d.Response(false, true, "q000_wrongName");
			d.SetText("Rudolf!");
			d.Response(false, true, "q000_wrongName");
			d.SetText("Martin!");
			d.Response(false, true, "q000_wrongName");
		}
	}

	/// <summary>#837 - f_q000_shouldBePromoted_Request/Answer/Button (velmistr.scp): another quest's NPC tells a hero ready for the
	/// rank that the envoy looks for them, which sets the optional stage 12. On Unas (d_q023_CarodejUnas) and Borek
	/// (d_mq002_borek); the other NPCs Erebor had say it (q010, q013, q034, gq002) have no dialogs here.</summary>
	public static class GrandmasterEnvoyHint {
		public const string Button = "q000_go4VM";

		/// <summary>The hero's line, on the page the NPC's dialog had it (Borek's greeting, Unas's "Kdo jsi?").</summary>
		internal static void Request(QuestDialogContent d, Player player) {
			if (GrandmasterQuest.CanBePromoted(player)) {
				d.SetText("<def_questDialog_colorTextResponse>Omlouvam se za svou troufalost, ale zda se mi, ze na me koukas trochu rospacite. Stalo se neco? Nejsi prvni, kdo se tu na mee takhle diva.");
				d.Response(true, false, "q000_getVMDir");
			}
		}

		/// <summary>Its pages.</summary>
		internal static void Answer(QuestDialogContent d, Player player) {
			if (!GrandmasterQuest.CanBePromoted(player)) {
				return;
			}
			d.Page("q000_getVMDir");
			d.SetText("Omlouvam se, ja jen, ze je tu ted slyset spousta zvesti o <src.sex(\"jistem muzi, ktery cestoval\",\"jiste zene, ktera cestovala\")> pres pul sveta aby <src.sex(\"pomohl\",\"pomohla\")> dceram Elqiry. Ty se strasne podobas popisu te osoby, je mi to hloupe.");
			d.Html();
			d.SetText("No to je tedy hloupost...");
			d.Response(false, false, "q000_plainPage");
			d.SetText("Nemusis se citit hloupe, jsem to opravdu ja.");
			d.Response(false, false, "q000_getVMDir_2");
			d.Page("q000_plainPage");
			d.SetText("Ja vim...");
			d.Html();
			d.Page("q000_getVMDir_2");
			d.SetText("To jsi opravdu ty! Neuveritelna nahoda, ze jsi <src.splitRealm(\"zavital\",\"zavitala\")> zrovna sem. Jeden by rekl, ze pobezis rovnou za nasim zmocnencem...");
			d.Html();
			d.SetText("Za zmocnencem? Proc?");
			d.Response(false, false, "q000_getVMDir_3");
			d.Page("q000_getVMDir_3");
			d.SetText("Ty to nevis? Shani te Zmocnenec <src.splitRealm(\"Gondoru\",\"Mordoru\")>. Vsichni o tom mluvi. <src.sex(\"Mel\",\"Mela\")> by jsi za nim dojit. Urcite se doslechl o tvych zasluhach.");
			d.Html();
			d.SetText("*Podekovat a jit za Zmocnencem*");
			d.SaveText(Button);
			d.Response(false, true, Button, true);
			d.SetText("Kde najdu Zmocnence <src.splitRealm(\"Gondoru\",\"Mordoru\")>?");
			d.Response(false, false, "q000_whereIsHe");
			d.Page("q000_whereIsHe");
			d.SetText("Zmocnence najdes <src.splitRealm(\"v Minas Thirith\",\"ve Rhunu\")>. Tam uz se doptas.");
			d.Html();
			d.SameResponse(false, true, Button, true);
		}

		/// <summary>True when it was this button.</summary>
		internal static bool OnButton(string button, Player player) {
			if ((button != Button) || !GrandmasterQuest.CanBePromoted(player)) {
				return false;
			}
			player.SysMessage("Mozna by ses " + GrandmasterQuest.Sex(player, "mel", "mela") + " jit za tim Zmocnencem podivat.");
			GrandmasterQuest.Advance(player, 12); //qtag(q000_velmistr,12,38)
			return true;
		}
	}

	/// <summary>#833 - the q000 branch of d_gq030_zmocnenec and d_mq030_zmocnenec: the envoy who grants the rank.</summary>
	public abstract partial class D_Q000EnvoyBase : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			bool gondor = IsGondor(npc);
			Q30.BaseButtons(d, player, gondor); //#1276 - q30, Erebor's d_gq030_zmocnenec / d_mq030_zmocnenec
			d.Page("Obchodovat");
			d.SetText("Kdepak, ja nejsem obchodnik.. ");
			d.Html();
			d.Page("Pozdrav");
			if (GrandmasterQuest.CanBePromoted(player)) {
				d.SetText("Zdravim te! Nejsi ty <src.sex(\"ten, kdo pomohl\",\"ta, ktera pomohla\")> Elqire zachranit jeji dcery?!");
				d.Html();
				d.SetText("Ano to jsem ja.");
				d.Response(false, false, "q000_getVM");
				d.Page("q000_getVM");
				d.SetText("Pak tedy poklekni! Nebot jsi tu <src.sex(\"ocekavan\",\"ocekavana\")>! Z moci mi sverene <src.splitRealm(\"Gondorskym\",\"Mordorskym\")> panovnikem ti udeluji vyssi spolecensky status! Budiz tva nova hodnost znama vsemu lidu <src.splitRealm(\"Gondorskemu\",\"Mordorskemu\")> a ty ji nos s uctou a zodpovednosti jak k sobe, tak k ostatnim, kteri ji obdrzeli predtebou a po tobe! Nyni opet povstan, Velmistre!");
				d.Html();
				d.SetText("*Povstat a prevzit novy titul*");
				d.Response(false, true, "q000_getVM", true);
			} else {
				d.SetText("Zdravim...");
				d.Html();
				Q30.GreetingLines(d, player, gondor);
			}
			Q30.Pages(d, player, gondor);
		}
	}

	/// <summary>#839 - d_questy_velmistr: the journal's text for each stage, 1 to 13.</summary>
	public static class GrandmasterQuestJournal {
		internal static readonly string[] Texts = {
			"Elvira a Elmira, dcery Elqiry, se sve matce jiz dlouhe mesice neozvaly. Pry se vydaly na vychod Ledovych plani poznat tamni krajinu. Jsou na pobyt v prirode zvykle a matka je presvedcena, ze se jim muselo neco stat..",
			"<src.sex(\"Nasel\",\"Nasla\")> jsi Elviru v lese na vychode Ledovych plani. Ta je zcela nepricetna a na tve pokusy o osloveni vubec nereaguje. Kde muze byt jeji sestra Elmira?",
			"<src.sex(\"Nasel\",\"Nasla\")> jsi Elmiru uzamcenou v kleci u skaly na vychode Ledovych plani. Prosi te, abys <src.sex(\"pomohl\",\"pomohla\")> alespon jeji sestre, Elvire, ktera je na uteku pred tim samym muzem, ktery Elmiru vezni. Elvira pry vbehla do snehove boure a nemohla se dostat daleko. Navic by sestru nikdy neopustila. Musi tu nekde byt!",
			"<src.sex(\"Nasel\",\"Nasla\")> jsi Elviru v lese na vychode Ledovych plani, kus od jeskyne, kde je uveznena jeji sestra. Elmira je zcela nepricetna a na tve pokusy o osloveni vubec nereaguje. Elmiru bude jiste osud jeji sestry zajimat.",
			"<src.sex(\"Rekl\",\"Rekla\")> jsi Elmire, jak je na tom jeji sestra. Ta si vybavila povest o lecive byline, korenu Este, jejiz posledni sazenice by mohly byt v temne jeskyni na vychodni casti pohori Ered Mithrin. Varuje te vsak pred sverenim ukolu nekomu jinemu, zaroven vsak takovou vypravu nemas podnikat <src.sex(\"sam\",\"sama\")>! Na tuto cestu bude nutne sehnat nekolik silnejsich a zkusenejsich bojovniku.",
			"Na konci temne jeskyne jsi <src.sex(\"utrhl\",\"utrhla\")> sazenici, ktera odpovida popisu Elmiry. Podle ni by koren teto vzcne byliny mohl vylecit Elviru z podivne nemoci. Elviru jsi naposledy <src.sex(\"videl\",\"videla\")> v lese na vychode Ledovych plani.",
			"<src.sex(\"Podal\",\"Podala\")> jsi Elvire koren Este, vzacnou bylinu z temne jeskyne. Elvira se velmi rychle zacala zotavovat ze sveho poblouzneni.",
			"Elvira ti prozradila cely pribeh o jejim a sestrinym prichodu do Ledovych plani a o Rinaldovi. Znalost jeho jmena ti dava silu jej zranit. Elvira te o to primo zada v prosbe o zachranu sve sestry.",
			"<src.sex(\"Zabil\",\"Zabila\")> jsi Rinalda, tyrana z Ledovych plani. <src.sex(\"Nalezl\",\"Nalezla\")> jsi u nej i nejaky klic.",
			"<src.sex(\"Vysvobodil\",\"Vysvobodila\")> jsi Elmiru ze zajeti a uzdravila jeji sestru Elviru. Matka Elqira bude z techto novin jiste nadsena.",
			"<src.sex(\"Popsal\",\"Popsala\")> jsi Elqire pribeh jejich dcer a <src.sex(\"utesil\",\"utesila\")> ji, ze jsou obe v poradku. Matka byla z teto noviny cela bez sebe a slibila, ze se o tvych zasluhach sveri vsem svym znamym.",
			"<src.sex(\"Doslechl\",\"Doslechla\")> jsi se, ze te shani Zmocnenec <src.splitRealm(\"Gondoru\",\"Mordoru\")>, pro tve zasluhy pote, co jsi <src.sex(\"popsal\",\"popsala\")> Elqire pribeh jejich dcer a <src.sex(\"utesil\",\"utesila\")> ji, ze jsou po tvem zasahu obe v poradku.",
			"Zmocnenec <src.splitRealm(\"Gondoru\",\"Mordoru\")> te pasoval na Velmistra pro tve zasluhy pote, co jsi <src.sex(\"zachranil\",\"zachranila\")> dcery stare Elqiry. Tvuj pribeh o osudu jejich dcer ji upokojil a ta z radosti, ze jsou po tvem zasahu obe v poradku, vypravi o tvych zasluhach kudy chodi.",
		};
	}
}
