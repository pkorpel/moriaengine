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

//#1266 - which of q007's dialogs (Q007Pages) a player gets, Erebor's f_q007_*_DClick, and what their buttons do. The innkeeper's
//dialog is Erebor's d_q007_hospodsky, written by hand: it decides page by page. Its q017 lines come with q017 (step 15).

namespace SteamEngine.CompiledScripts {

	/// <summary>A q007 dialog: Erebor's old-style dialogs had no title strip of their own, the face is the NPC's.</summary>
	public abstract class Q007Dialog : QuestDialogDef {
		private readonly int face;

		protected Q007Dialog(int face) {
			this.face = face;
		}

		protected override int Face {
			get {
				return this.face;
			}
		}
	}

	/// <summary>Haradir, d_q007_haradir..8.</summary>
	public class D_Q007Haradir : Q007Dialog {
		public D_Q007Haradir()
			: base(0x3106) {
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q007.GetStage(player);
			if (s == 8) {
				Q007Pages.Haradir8(d);
			} else if ((s == 7) && QuestItems.Has(player, Q007.Piglet)) {
				Q007Pages.Haradir7(d);
			} else if (s == 7) {
				Q007Pages.Haradir6(d);
			} else if (s >= 5) {
				Q007Pages.Haradir5(d);
			} else if (s >= 3) {
				Q007Pages.Haradir4(d);
			} else if (s == 2) {
				Q007Pages.Haradir3(d);
			} else if (s == 1) {
				Q007Pages.Haradir2(d);
			} else {
				Q007Pages.Haradir(d);
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int s = Q007.GetStage(player);
			if ((button == "haradir:1") && (s == 0)) {
				Q007.Advance(player, 1);
				npc.Say("Moc ti dekuji! Snad ho presvedcis.");
			} else if ((button == "haradir2:1") && (s == 1)) {
				npc.Say("Dekuji ti. Budu vdecny za kazdou zpravu..."); //Erebor's q007_haradir_Nemethen thread was never written on
			} else if ((button == "haradir3:1") && (s == 2)) {
				Q007.Advance(player, 3);
				npc.Say("Snad ta Carodejka pomuze. Mej se !");
			} else if (button == "haradir7:1") {
				if ((s == 7) && QuestItems.Take(player, Q007.Piglet)) {
					npc.Say("Aaah! To je krasavec! Jeste jednou Diky! Mozna bych vedel o necem, co te bude zajimat...");
					Q007.Reward(player, 500);
					Q007.Advance(player, 8);
				} else {
					npc.Say("Ty u sebe to sele nemas! Snazis se ze me udelat hlupaka?");
				}
			}
		}
	}

	/// <summary>The witch Rachel, d_q007_carodejnice..5.</summary>
	public class D_Q007Rachel : Q007Dialog {
		public const string BatWing = "i_reag_Batwing";

		public D_Q007Rachel()
			: base(0x3039) {
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q007.GetStage(player);
			if (s >= 5) {
				Q007Pages.Carodejnice5(d);
			} else if (s == 4) {
				if (Q006.Count(player, BatWing) >= Q007.BatWings) {
					Q007Pages.Carodejnice4(d);
				} else {
					Q007Pages.Carodejnice3(d);
				}
			} else if (s == 3) {
				Q007Pages.Carodejnice2(d);
			} else {
				Q007Pages.Carodejnice(d);
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int s = Q007.GetStage(player);
			if ((button == "carodejnice2:1") && (s == 3)) {
				Q007.Advance(player, 4);
				npc.Say("A pospes si.");
			} else if (button == "carodejnice2:2") {
				npc.Say("Az zmoudris, prijd a mozna se domluvime...");
			} else if ((button == "carodejnice4:1") && (s == 4)) {
				if (Q006.Count(player, BatWing) >= Q007.BatWings) {
					Q006.Consume(player, BatWing, Q007.BatWings);
					Q007.Advance(player, 5);
					npc.Say("A s hospodskym se uz netrap...");
				} else {
					npc.Say("Co to na mne zkousis ? Chci 1000 netopyrich kridel ...");
				}
			}
		}
	}

	/// <summary>The Ancient Smith, d_q007_kovar..6.</summary>
	public class D_Q007Smith : Q007Dialog {
		public D_Q007Smith()
			: base(0x3059) {
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int c = Q007.GetSmith(player);
			if (c == 2) {
				Q007Pages.Kovar6(d);
			} else if (c == 1) {
				if (Q007.HasStones(player)) {
					Q007Pages.Kovar5(d);
				} else {
					Q007Pages.Kovar4(d);
				}
			} else if (Q007.GetStage(player) == 8) {
				if (Q007.Skill(player, SkillName.Blacksmith) > Q007.SmithNeeded) {
					Q007Pages.Kovar3(d);
				} else {
					Q007Pages.Kovar2(d);
				}
			} else {
				Q007Pages.Kovar(d);
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int c = Q007.GetSmith(player);
			if ((button == "kovar3:1") && (c == 0) && (Q007.GetStage(player) == 8) && (Q007.Skill(player, SkillName.Blacksmith) > Q007.SmithNeeded)) {
				Q007.AdvanceSmith(player, 1);
				npc.Say("Preji ti hodne stesti. Pamatuj, potrebuji ty kameny vsechny tri.");
			} else if ((button == "kovar5:1") && (c == 1)) {
				if (Q007.HasStones(player)) {
					Q007.TakeStones(player);
					Q007.Reward(player, 2000);
					Q007.AdvanceSmith(player, 2);
					ThingDef.GetByDefname(Q007.HammerDef).Create(player.Backpack);
					npc.Say("Tady mas to kladivo. Ted jiz tve schopnosti nikdo neprekona.");
				} else {
					npc.Say("Nemas ty kameny u sebe. Potrebuji vsechny tri abych to kladivo mohl vyrobit.");
				}
			}
		}
	}

	/// <summary>The Old Inscripter, d_q007_maginscript..6.</summary>
	public class D_Q007Inscripter : Q007Dialog {
		public D_Q007Inscripter()
			: base(0x3053) {
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int b = Q007.GetInscripter(player);
			if (b > 1) {
				Q007Pages.Maginscript6(d);
			} else if (b == 1) {
				if (Q007.HasStones(player)) {
					Q007Pages.Maginscript5(d);
				} else {
					Q007Pages.Maginscript4(d);
				}
			} else if (Q007.GetStage(player) == 8) {
				if (Q007.Skill(player, SkillName.Inscribe) > Q007.InscriptionNeeded) {
					Q007Pages.Maginscript3(d);
				} else {
					Q007Pages.Maginscript2(d);
				}
			} else {
				Q007Pages.Maginscript(d);
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int b = Q007.GetInscripter(player);
			if ((button == "maginscript3:1") && (b == 0) && (Q007.GetStage(player) == 8) && (Q007.Skill(player, SkillName.Inscribe) > Q007.InscriptionNeeded)) {
				Q007.AdvanceInscripter(player, 1);
				npc.Say("Preji ti hodne stesti. Pamatuj, potrebuji ty kameny vsechny tri.");
			} else if ((button == "maginscript5:1") && (b == 1)) {
				if (Q007.HasStones(player)) {
					Q007.TakeStones(player);
					Q007.Reward(player, 2000);
					QuestItems.Give(player, Q007.Book);
					Q007.AdvanceInscripter(player, 2);
					npc.Say("Vyborne ! Tady mas tu knihu. Strez sve vedomosti obezretne!");
				} else {
					npc.Say("Nemas ty kameny u sebe. Potrebuji silu vsech tri kamenu.");
				}
			}
		}
	}

	/// <summary>The drunk apprentice Divous, d_q007_divous..3: the book for his magic pen.</summary>
	public class D_Q007Divous : Q007Dialog {
		public D_Q007Divous()
			: base(0x3000) {
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			if (Q007.GetInscripter(player) == 3) {
				Q007Pages.Divous3(d);
			} else if (QuestItems.Has(player, Q007.Book)) {
				Q007Pages.Divous2(d);
			} else {
				Q007Pages.Divous(d);
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if (button == "divous2:1") {
				if ((Q007.GetInscripter(player) == 2) && QuestItems.Take(player, Q007.Book)) {
					QuestItems.Give(player, Q007.Pen);
					Q007.AdvanceInscripter(player, 3);
					npc.Say("Tady mas to pero..");
				} else {
					npc.Say("Hej ! Kam jsi dal tu knihu?");
				}
			} else if (button == "divous2:2") {
				npc.Say("Tak jdi pryc! A nerus me tu...");
			}
		}
	}

	/// <summary>The pirate Rodrigo, d_q007_rodrigo and rodrigo2: rumours for gold.</summary>
	public class D_Q007Rodrigo : Q007Dialog {
		public D_Q007Rodrigo()
			: base(0x3000) {
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			if (Q007.PaidRodrigo(player)) {
				Q007Pages.Rodrigo2(d);
			} else {
				Q007Pages.Rodrigo(d);
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int price;
			if ((button == "rodrigo:1") || (button == "rodrigo2:1")) {
				price = 10000;
			} else if ((button == "rodrigo:2") || (button == "rodrigo2:2")) {
				price = 8000;
			} else {
				if (button == "rodrigo2:3") {
					npc.Say("Jen jdi. Aspon si zarybarim...");
				}
				return;
			}
			if (!GoldUtils.Pay(player, price, PaySource.Usual, "")) {
				npc.Say("Hele, necpi mi zlatky, ktery nemas a bez si radsi smajdat po pousti.");
				return;
			}
			Q007.SetPaidRodrigo(player);
			npc.Dialog(player, SingletonScript<D_Q007RodrigoNews>.Instance, npc);
		}
	}

	/// <summary>What Rodrigo knows, d_q007_rodrigo_informace and _informace2 (for whoever adopted a dolphin).</summary>
	public class D_Q007RodrigoNews : Q007Dialog {
		public const int DolphinPrice = 50000;

		public D_Q007RodrigoNews()
			: base(0x3000) {
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			if (Q007.AdoptedDolphin(player)) {
				Q007Pages.RodrigoInformace2(d);
			} else {
				Q007Pages.RodrigoInformace(d);
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if ((button == "rodrigo_informace:1") && !Q007.AdoptedDolphin(player)) {
				if (GoldUtils.Pay(player, DolphinPrice, PaySource.Usual, "")) {
					Q007.SetAdoptedDolphin(player);
					Q007.Reward(player, 200);
					player.SysMessage("Citis se skvele, ze jsi " + ((player.Gender == Gender.Female) ? "pomohla" : "pomohl") + " delfinum v preziti.");
					npc.Dialog(player, this, npc);
				} else {
					npc.Say("Nedelej si ze me sasky ... kdyz ty penize nemas, tak na me nemluv.");
				}
			}
		}
	}

	/// <summary>#1264, #1266 - the innkeeper, Erebor's d_q007_hospodsky: q007's pages, his drinks, and Eriol's line (q006).</summary>
	public partial class D_Q007Hospodsky : Q007Dialog {
		public D_Q007Hospodsky()
			: base(0x3123) {
		}

		private static readonly string[] drinkDefs = { "i_bottle_wine", "i_bottle_liquor", "i_bottle_ale", "i_jug_cider" };
		private static readonly int[] drinkPrices = { 150, 200, 100, 250 }; //Erebor's cider took 200 but its menu said 250
		private static readonly string[] drinkSays = { "Tady mas sve vino.", "Kopni to tam namorniku!", "Tady mas svoji zrzavou krasku.", "Aaah jedna varka piti pro panicky.." };

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q007.GetStage(player);
			string first = (s < 2) ? "Kdo jsi?" : "Jak se dari?";
			d.BaseButton(1, 57, first);
			d.BaseButton(2, 36, "Mohu pomoci?");
			d.BaseButton(3, 37, "-Prohlednout-", QuestDialogContent.ColorEmote);
			d.BaseButton(4, 60, "Nabidka");
			if ((s == 1) || (s == 5)) {
				d.BaseButton(5, 55, "Selatko");
			}

			d.Page("Pozdrav");
			d.SetText((s > 6) ? "Vitej! Rad te tu zase vidim, co si das?" : ((s == 6) ? "Opet te tu vitam! Jak jsi na tom ?" : ((s >= 2) ? "Vitej v me krcme namorniku! Aaah, to jsi ty ..." : "Vitej v me krcme namorniku!")));
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

			d.Page(first);
			d.SetText((s > 6) ? "Je mi ted mnohem lepe. Obchody se zacaly hybat... Konecne si mohu dovolit prodat tolik jidla jako driv. Ale ostrov ma ted velmi spatnou povest. Bude trvat jeste dlouho, nez se vse vrati do starych koleji. Minulou noc tu dokonce byla skupina dobrodruhu a obchodniku... Jeden z nich tu utratil hotove jmeni, coz byla jedina vec, diky ktere sem ho tu jeste snesl.. Porad mel usta plna nelibosti k Terlakovi..." : ((s == 6) ? "Nejsem ted ve sve kuzi... Prosim, spechej se svym ukolem nez bude pozde!" : ((s >= 2) ? "Nadherne... Hospudka stoji, lide chteji veci z nabidek... Zadna selatka ... Nemuze mi byt lepe." : "Kdo jsem? .. *nasilne se rozesmal* .. Jsem hospodsky a stastny vlastnik tehle krcmy ! Das si neco ?"))); //q017's "Terlak hmm..." answer comes with q017
			d.Html();

			d.Page("Mohu pomoci?");
			if (s > 6) {
				d.SetText("Myslim, ze jsi toho pro mne vykonal dost! jen se klidne napij...");
				d.Html();
			} else if (s == 6) {
				d.SetText("Ted mi muze pomoci jen... *Zoufale na tebe pohledl* ... Nechtej abych to opakoval.");
				d.Html();
				if (QuestItems.Has(player, Q007.Bow)) {
					d.SetText("Uz se nemusis bat, Dryada je mrtva!");
					d.Response(false, false, "Selatko");
				}
			} else {
				d.SetText("Dokud budes schopen, vypij a snez co se do tebe vejde! Vypadas pohuble jen se neboj a dej svemu zanedbanemu telu co potrebuje. Pak budu spokojen i ja.");
				d.Html();
			}

			d.Page("-Prohlednout-");
			d.SetText((s > 6) ? "Trochu roztrzity, starsi hospodsky. Zda se, ze na ostrove starvil hodne casu sam, ale co jsi mu pomohl, je mnohem veselejsi." : ((s == 6) ? "Trochu roztrzity, starsi hospodsky. Zda se, ze na ostrove starvil hodne casu sam, a tak na sebe ani nepouta prilis velkou pozornost.<br>Co se ti sveril se svym problemem, vypada mnohem nervozneji." : "Trochu roztrzity, starsi hospodsky. Zda se, ze na ostrove starvil hodne casu sam, a tak na sebe ani nepouta prilis velkou pozornost."));
			d.Html();

			d.Page("Nabidka");
			d.SetText("Mam tu sirokou nabidku. Tady se podivej a vyber si sam a pamatuj, alkohol podavany v malych davkach neskodi v jakemkoliv mnozstvi!");
			d.Html();
			string[] labels = { "Dam si vino!", "Dam si neco tvrdsiho. Prodej mi koralku!", "Lahvinka svetleho piva mi udela dobre.", "Aaah! Dzbanek jablecneho vina!" };
			for (int i = 0; i < labels.Length; i++) {
				d.SetText(labels[i] + " (" + drinkPrices[i] + "gp)");
				d.Response(false, true, "drink:" + i);
			}

			if (s == 1) {
				this.Stingy(d);
			} else if (s == 5) {
				this.Afraid(d);
			} else if (s == 6) {
				d.Page("Selatko");
				d.SetText("Vazne! Jsi same prekvapeni! Prilis jsem nedoufal v tve vitezstvi... Budiz tva ochota a statecnost vzroem pro ostatni! Na tve jmeno nikdy nezapomenu a budu rad, kdyz se tu jeste nekdy ukazes. Doufam ze na sebe nenechas dlouho cekat... Mas tedy ten dukaz?");
				d.Html();
				d.SetText("*Predat luk a pockat si na selatko...*");
				d.Response(false, true, "hospodsky:7", true);
			}
		}

		//stage 1: he denies he owes Haradir anything
		private void Stingy(QuestDialogContent d) {
			string giveUp = "*Tohle nema cenu, ten nic neprizna*";
			d.Page("Selatko");
			d.SetText("Hmm ?");
			d.Html();
			d.SetText("Mluvil jsem s Haradirem. Tvrdi, ze mu dluzis selatko.");
			d.Response(false, false, "p7");
			d.SetText("Ty psi vymesku! Dej mi to sele, ktere dluzis Haradirovi!");
			d.Response(false, false, "p8");
			d.SetText("Nevypadas sice jako hlupak, ale jen blbec by mi nedal sele, kdyz ho poprosim.");
			d.Response(false, false, "p9");
			d.Page("p7");
			d.SetText("Kdoze? Ani nevim kdo to je! Navic zadna selata neprodavam...");
			d.Html();
			d.SetText("Nelzi! Rekl mi dost.");
			d.Response(false, false, "p10");
			d.SetText("Jsi si jisty? Podle toho co vim by si ho zaslouzil i bez placeni.");
			d.Response(false, false, "p11");
			d.Page("p8");
			d.SetText("Vem si sve urazky tam, odkud jsi prisel chytraku! Ja tady zadna selata neprodavam!");
			d.Html();
			d.SetText("Nesmysl ty stary lakomce! Nemyslis, ze je cas splatit dluh?");
			d.Response(false, false, "p12");
			d.SetText(giveUp);
			d.Response(false, true, "hospodsky:5", true);
			d.Page("p9");
			d.SetText("*Dlouze se na tebe zahledel* ... Vidis tady ten pult? Diky nemu ti mohu podat cokoliv si obednas, aniz bych se te jen lehce dotkl. Tady zamnou muzes spatrit polici plnou cistych sklenic a poharu, ktere ti naplnim cimkoliv si jen budes prat. A tady nahore nejvyzsi chlouba me male hospudky. Visi zde velka popsana cedule, kde je vypsano vse, co ti mohu nabidnout. Kdyz na ni pohlednes co vidis ? PROTOZE JEN NEGRAMOTNY HLUPAK TAM VIDI NEJAKE SELE!");
			d.Html();
			d.SetText("Mluvil jsem s Haradirem. Tvrdi, ze mu dluzis selatko.");
			d.Response(false, false, "p7");
			d.SetText("*zvednout svicen ze stolu*");
			d.Response(false, false, "p13", true);
			d.SetText(giveUp);
			d.Response(false, true, "hospodsky:5", true);
			foreach (string[] page in new[] { new[] { "p10", "Ja ze jsem lhar! Nikdy jsem nikomu zadne sele neslibil a nech me byt!" }, new[] { "p11", "HAHAHA ! Zadarmo ! A zit budu z ceho ? Beztak o nicem nevim to bych si pamatoval." }, new[] { "p12", "Ja zadne dluhy nemam. Mam nejlepsi hospodu siroko daleko proc bych se zadluzoval! Odpust si svou drzost hrdino a radsi se napij." } }) {
				d.Page(page[0]);
				d.SetText(page[1]);
				d.Html();
				d.SetText(giveUp);
				d.Response(false, true, "hospodsky:5", true);
			}
			d.Page("p13");
			d.SetText("Hmm ?");
			d.Html();
			d.SetText("Vidis tady ten svicen? Kdyz se podivas blize, je velmi zdobny. S jeho pomoci ti mo-" + " " + "hu velmi hrube poranit sverac a nekolikrat tydne si vzpomenes na chvili, kdy jsi mi" + " " + "odmitl vydat to selatko bez nasili, az zase budes potrebovat uvolnit ze sveho tela vse," + " " + "co jsi do nej vpravil. Jidlo, ktere odpiras druhym.");
			d.Response(true, false, "p14");
			d.Page("p14");
			d.SetText("Ahh... zvlastni, ze jmenujes zrovna tenhle svicen. Me zene se pry jeho hrubost take libi... *odkaslal si a uprene se na tebe podival* ...Ja jsem na takove silaky jako jsi ty zvykly a ver mi, ze tu nikdomu strevni zakruty prerovnavat nebudes! Bez si shanet selatka k nekomu jinemu!");
			d.Html();
			d.SetText("*Tohle nema cenu, ten nic neprizna. Navic by mi mrtvy nebyl k nicemu.*");
			d.Response(false, true, "hospodsky:5", true);
		}

		//stage 5: Rachel spoke to him, and he tells of the Dryad
		private void Afraid(QuestDialogContent d) {
			d.Page("Selatko");
			d.SetText("Hmm ?");
			d.Html();
			d.SetText("Posila me Rachel. Jestli mi nedas to selatko, muzes se nadobro rozloucit s jejim" + " " + "korenim.");
			d.Response(true, false, "p7");
			d.Page("p7");
			d.SetText("Coze? ! Ale to preci ! ... Aargh ... *hospodskemu stuhl usmev a sklopil zrak*");
			d.Html();
			d.SetText("*pokracovat*");
			d.Response(false, false, "p8", true);
			d.Page("p8");
			d.SetText("Tohle se nemelo nikdy stat... Poslouchej mne ted dobre... *hospodsky se nervozne rozhledl, naklonil se bliz a naznacil, ze chce totez po tobe*...");
			d.Html();
			d.SetText("Poslouchej, na tyhle komedie nemam cas...");
			d.Response(false, false, "p9");
			d.SetText("*naklonit se bliz*");
			d.Response(false, false, "p10", true);
			d.Page("p9");
			d.SetText("Prosim ! *hospodskemu nahle vyrostl na tvari velmi nalehavy vyraz*");
			d.Html();
			d.SetText("*naklonit se bliz*");
			d.Response(false, false, "p10", true);
			d.Page("p10");
			d.SetText("*Hospodsky se opet rozhledl, a spustil* ... Poslouchej, pred nekolika lety jsem zde mel velike problemy s nekolika bestiemi, ktere se potulovaly kolem meho hostince a odhanely mi zakazniky. Nekolik lidi, co zde zili, vystvaly z jejich domovu a od te doby mne zivi pouze namornici, co sem prijdou nacerpat nove sily.");
			d.Html();
			d.SetText("Pokracuj...");
			d.Response(false, false, "p11");
			d.Page("p11");
			d.SetText("Tenkrat prijela lod Malle Aiwiva plna ozbrojencu, kteri cistili ostrovy od podobne haveti. Pozadal jsem je, aby mi pomohli a ja jsem jim za to poskytl ubytovani a dodaval jim zasoby jidla a dobreho piti. Bojovali tu nekolik tydnu a nemohu nepripomenout, ze mezi nesmrtelnymi elfy vynikal prosty clovek. Ano, byl to Haradir a jeho umeni vice nez napomohlo tomu, ze jsme mohli opet bezstarostne prochazet nase statky dal od pobrezi.");
			d.Html();
			d.SetText("Pokracuj...");
			d.Response(false, false, "p12");
			d.Page("p12");
			d.SetText("Za dobu jejich pritomnosti jsem jim upekl a vyudil nejedno ze svych nejlepsich selatkek. Dal jsem si tenkrat velmi zalezet... Pri odjezdu mne tedy Haradir skutecne pozadal, abych mu jedno pripravil a poslal. Jenze klid na ostrove nemel dlouhe trvani... boje vzbudili demona, ktery prezival v zdejsich skalach. Zla Dryada opustila svou skrys a s mucivym krikem znovu napadla nasi osadu. Tentokrat vsak jeji prisluhovaci neznali slitovani. Pobili temer vse, co potkali. Nazivu nechali jen nekolik celedinu a me. Obestoupili mne vsechny ty bestie a ja musel prisahat...");
			d.Html();
			d.SetText("*pokracovat*");
			d.Response(false, false, "p13", true);
			d.Page("p13");
			d.SetText("Prisahal jsem, ze neprozradim jeji pritomnost a budu ji dodavat cerstve zasoby pro jeji sluzebniky. Nemohu se ani poskrabat na rameni, aniz by o tom nevedela. Mam pocit, jako by mne sledovaly stovky oci... Prosim, jestli chces sve sele, musis mne te Dryady zbavit!");
			d.Html();
			d.SetText("*Pokynutim hlavy naznacit souhlas.*");
			d.Response(false, false, "p14", true);
			d.Page("p14");
			d.SetText("Vyborne ! ... * Hospodsky nadsenim nachvili ztratil kontrolu nad svymy gesty a rozlil nekolik lahvi drahe lihoviny* ... Ahh .. Dobra tedy ! Zabij ji a dostanes sve sele! Ale nechtej mne osalit, prines mi jeji luk na dukaz, ze jsi ji opravdu usmrtil. Nehne se bez nej na krok a ma ho stale pri sobe. Ted prosim jdi...");
			d.Html();
			d.SetText("*Prijmout ukol...*");
			d.Response(false, true, "hospodsky:6", true);
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int s = Q007.GetStage(player);
			if (button.StartsWith("drink:", System.StringComparison.Ordinal)) {
				int i = button[6] - '0';
				if ((i >= 0) && (i < drinkDefs.Length)) {
					Q007.Drink((NPC) npc, player, drinkDefs[i], drinkPrices[i], drinkSays[i]);
				}
			} else if ((button == "hospodsky:5") && (s == 1)) {
				Q007.Advance(player, 2);
				npc.Say("Pche... Selatko...");
			} else if ((button == "hospodsky:6") && (s == 5)) {
				Q007.Advance(player, 6);
				npc.Say("Hodne stesti! Snad jsem neudelal chybu...");
			} else if (button == "hospodsky:7") {
				if ((s == 6) && QuestItems.Take(player, Q007.Bow)) {
					npc.Say("Zde je selatko jak jsem slibil. Jeste jednou ti dekuji !");
					Q007.Reward(player, 500);
					QuestItems.Give(player, Q007.Piglet);
					Q007.Advance(player, 7);
				} else {
					npc.Say("Ty ho nemas ! Thalione smiluj se ! Chces sem zavleci valku a smrt ? Prines mi ten luk!");
				}
			} else if ((button == "q006_toldInnkeeper") && (Q006.GetStage(player) == 19) && Q006.HasParchment(player)) {
				Q006.SetParchment(player, false);
				Q006.Advance(player, 20);
				Q006.Reward(player, 300);
			}
		}
	}
}
