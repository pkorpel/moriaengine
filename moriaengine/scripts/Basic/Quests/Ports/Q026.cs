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

using System;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1272 - Erebor's q026 "Otrava z hrobek" (questy/q026_hcnPoison.scp, _items.scp): a wraith of the Black Numenoreans' tombs
	/// curses the hero (e_q026_hcnPoisoning_NPC). Cure potions don't help; a healer sends them to a cleric, Andor to a wizard, Unas
	/// to the Temple of Knowledge in the Rhun swamps: its tapestry points to Dol Guldur, whose secret room holds the key to the temple's
	/// library; a book there, deciphered by Unas, is a recipe Roxana brews from a heap of goods and 20,000 gold. The cure lifts the
	/// curse for good.
	/// <para>Erebor's qtag q026_hcnPoison: 1 cursed, 2 cure potions failed, 3 to a cleric, 4 to a wizard, (5 never set), 6 to the
	/// temple, 7 the tapestry read, 8 Dol Guldur's tile, 9 the key, 10 the door slid, 11 the book, 12 the recipe, 13 Roxana's goods,
	/// (14 the cure in the pack), 15 cured and immune. The curse: every 400-800 s a food point, or 25 damage at 4 food or less; 70% of
	/// a wraith's blows on a cursed hero up to 5/3 stronger.</para>
	/// </summary>
	public static class Q026 {
		public const string AndorDef = "c_q016_Andor", RoxanaDef = "c_q020_roxana";
		public const string TapestryDef = "i_q026_TOK_tapestry", BookPileDef = "i_q026_TOK_bookPile", DoorDef = "i_q026_TOK_slidingDoor";
		public const string AlarmDef = "i_q026_DG_secretRoom_alarm", WallDef = "i_q026_DG_secretDoor", BoxDef = "i_q026_DG_secretRoom_box";
		public const string CurseAbility = "a_tevent_e_q026_hcnpoisoning_npc";
		public const int Done = 15;
		public const int CureGold = 20000;
		public const int CurseDamage = 25, DoorDamage = 30, TapestryExp = 300;
		public const int OpenSeconds = 60;
		//def_q026_CureResources: batwings, bones, obsidian, sulfurous ash, obsidian ingots, spruce (copper) logs, chestnut (iron) logs
		public static readonly string[] Goods = { "i_reag_batwing", "i_reag_bone", "i_reag_obsidian", "i_reag_sulfur_ash", "i_ingot_obsidian", "i_log_copper", "i_log_iron" };
		public static readonly int[] GoodsNeeded = { 20, 30, 100, 50, 2, 4500, 2500 };
		private static readonly string[] goodsNames = { "netopyrich kridel (batwings)", "kosti (bones)", "hrutek drceneho obsidianu (obsidian)",
			"hrutek sirneho popele (Sulfurous Ash)", "x obsidianovy ingot", "smrkovych polen (spruce)", "kastanovych (chestnut) polen" };

		private static readonly TagKey stageTK = TagKey.Acquire("_q026HcnPoison_");
		private static readonly TagKey[] goodsTK = new TagKey[Goods.Length];
		internal static readonly TagKey OpenTK = TagKey.Acquire("_q026Open_");

		public static QuestJournalEntry Journal { get; private set; }

		static Q026() {
			for (int i = 0; i < Goods.Length; i++) {
				goodsTK[i] = TagKey.Acquire("_q026Goods" + i + "_");
			}
		}

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Otrava z hrobek", 0x1b6b, GetJournalText, p => GetStage(p) == Done));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(AndorDef, SingletonScript<E_Q026Npc>.Instance);
			QuestNpc.Attach(RoxanaDef, SingletonScript<E_Q026Npc>.Instance);
			foreach (string def in new[] { TapestryDef, BookPileDef, DoorDef, AlarmDef, WallDef, BoxDef }) {
				QuestNpc.Attach(def, SingletonScript<E_Q026Fixture>.Instance);
			}
		}

		public static int GetStage(Player player) {
			object tag = player.GetTag(stageTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		internal static void SetStage(Player player, int stage) {
			player.SetTag(stageTK, stage);
		}

		internal static void Advance(Player player, int stage) {
			SetStage(player, stage);
			QuestJournal.Report(player, Journal);
		}

		private static bool IsFemale(Character ch) {
			return ch.Gender == Gender.Female;
		}

		#region the curse
		/// <summary>e_q026_hcnPoisoning_NPC's @AfterSwing (DamageManager.ProcessSwing, a landed blow before its damage): the first
		/// blow on a hero curses them, later ones on a cursed hero are stronger 7 times in 10. A cured one is immune.</summary>
		public static double WraithBlow(Character attacker, Character defender, double damage) {
			AbilityDef def = AbilityDef.GetByDefname(CurseAbility);
			Ability ability;
			if ((def == null) || !(attacker is NPC) || !attacker.HasAbility(def, out ability) || !(defender is Player player) || player.IsGM || player.Flag_Dead
					|| EnergyShieldPlugin.IsUp(player)) { //#1401 - not through an energy shield
				return damage;
			}
			int stage = GetStage(player);
			if (stage == 0) {
				Curse(player);
			} else if ((stage < Done) && (Globals.dice.Next(100) < 70)) {
				damage = (damage * (30 + Globals.dice.Next(21))) / 30;
				player.SysMessage("Bolest z otravy prochazi celym tvym telem.");
			}
			return damage;
		}

		/// <summary>f_q026_HCNPoison_give's first part: i_q026_hcn_memoryPoison, here a timer on the hero.</summary>
		public static void Curse(Player player) {
			player.Sound(481);
			Advance(player, 1);
			Q026CurseTimer timer = new Q026CurseTimer();
			player.AddTimer(Q026CurseTimer.Key, timer);
			//a period that isn't "off", or BoundTimer deletes it as it fires and its own re-arm throws (PetHungerTimer says so too)
			timer.PeriodInSeconds = Q026CurseTimer.NextSeconds();
			timer.DueInSeconds = Q026CurseTimer.NextSeconds();
		}

		/// <summary>i_q026_hcn_memoryPoison's @timer.</summary>
		internal static void CurseTick(Player player) {
			int food = PlayerHunger.FoodOf(player);
			if (food > 4) {
				PlayerHunger.SetFood(player, food - 1);
			} else {
				DamageManager.CauseDamage(null, player, DamageType.Irresistable, CurseDamage); //damage_noresponse
			}
			if (Globals.dice.Next(5) == 1) { //Erebor's "not to spam all the time"
				player.SysMessage((Globals.dice.Next(2) == 0) ? "Jed z Hrobek cernych Numenorejcu prostupuje tvym telem." : "Najednou se citis byt hladovejsi... Jed asi postupuje.");
			}
		}

		/// <summary>e_q026_hcnCurePoison_check: a cure potion drunk while cursed only shows it isn't a poison.</summary>
		public static void OnCurePotion(Character drinker) {
			if ((drinker is Player player) && (GetStage(player) == 1)) {
				Advance(player, 2);
			}
		}

		/// <summary>The cure: f_q026_HCNPoison_remove and the stage that makes the hero immune.</summary>
		internal static void Cure(Player player) {
			player.DeleteTimer(Q026CurseTimer.Key);
			player.Sound(486);
			Advance(player, Done);
		}
		#endregion the curse

		#region the journal
		internal static readonly string[] JournalTexts = {
			"<?src.sex(\"Byl jsi otraven\",\"Byla jsi otravena\")?> v hrobkach cernych Numenorejcu.",
			"Zda se, ze na tvou otravu nezabiraji bezne protileky. Nejspis bude vhodne vyhledat zkuseneho lecitele.",
			"Bezni lecitele si s tvou otravou nevedeji rady. Tvou posledni nadeji se zda byt nejaky klerik znaly jedu.",
			"<?src.sex(\"Promluvil\",\"Promluvila\")?> jsi si s klerikem Andorem o sve otrave. Rozpoznal v tve chorobe priznaky cerne magie a poslal te zkusit stesti u nejakeho kouzelnika. Ti snad budou vedet vice.",
			"Zda se, ze o davnych metodach starych numenorejcu vi vice carodej Unas. Dlouhy cas pry putuje po Bystre Rece.",
			"Carodej Unas ma podezreni, ze tva otrava je zapricinena davnym prokletim mocnych nekromantu. Jde o velmi stare umeni, ktere je jiz davno zapomenuto. V bazinach severozapadne od Rhunskeho jezera je davny chram vedeni. Jdi tam a porozhledni se po odkazech starych Numenorejskych nekromantu.",
			"Na zdi jedne z budov chramu v Rhunskych bazinach jsi <?src.sex(\"nasel\",\"nasla\")?> napisy Numenorejcu. Textu prilis nerozumis, ale mnohe ikony poukazuji na Dol Guldurske podzemi. Dvere budovy byly peclive zamcene a nejspise je chrani mocne kouzlo. Bez vhodneho klice se dovnitr nedostanes.",
			"Jedna stena teto mistnosti vypada velice podezrele. Tapiserie na jedne ze zdi ti napadne pripomina tu z cernokneznikova chramu v Rhunskych bazinach.",
			"Ve skryte mistnosti Dol Gulduru jsi <?src.sex(\"nasel\",\"nasla\")?> napisy lemovany klic od dveri. Mohl by pasovat do zamku dveri cernokneznikovy knihovny v Rhunskych bazinach.",
			"<?src.sex(\"Pouzil\",\"Pouzila\")?> jsi klic z Dol Guldurske skryte mistnosti do zamku dveri v Rhunskych bazinach a <?src.sex(\"otevrel\",\"otevrela\")?> jsi cernokneznikovu knihovnu. Podle carodeje Unase by tu mohl byt lek nebo alespon kniha pojednavajici o temnych praktikach Cernych Numenorejcu.",
			"<?src.sex(\"Nasel\",\"Nasla\")?> jsi knihu s nazvem Lektvary a Masti Cernych Numenorejcu. Vlastni obsah se zda byt sifrovany a <?src.sex(\"sam\",\"sama\")?> jej nedokazes rozlustit. Unas ti rekl, ze studuje tuto oblast, treba bude vedet vice i o teto knize.",
			"<?src.sex(\"Ukazal\",\"Ukazala\")?> jsi knihu z Rhunskych bazin carodeji Unasovi. Ten skutecne rozlustil cernokneznikovu sifru a z alchymistickych symbolu v knize a popisu tvych priznaku sepsal recept k vyhotoveni protileku pro tvou otravu. Bohuzel Unas sam neni schopen tento lektvar umichat. Poslal te za Roxanou, ktera snad bude schopna lektvar zhotovit. Carodejka pry po leta zije na jihu Temneho Hvozdu.",
			null, //13: what Roxana still wants, GoodsText
			"Carodejka Roxana ti predala hotovy protijed.",
			"Zda se, ze priznaky tve otravy pominuly a byl-li Unasuv vyklad ucinku tohoto jedu spravny, pak jsi k nemu diky lektvaru od carodejky Roxany <?src.sex(\"ziskal\",\"ziskala\")?> trvalou imunitu."
		};

		internal static string GetJournalText(Player player) {
			int stage = GetStage(player);
			if ((stage < 1) || (stage > Done)) {
				return null;
			}
			return (stage == 13) ? GoodsText(player) : JournalTexts[stage - 1];
		}

		private static string GoodsText(Player player) {
			string text = "Carodejka Roxana zijici na jihu Temneho Hvozdu po tobe jeste potrebuje:";
			for (int i = 0; i < Goods.Length; i++) {
				text += "<br> " + Remaining(player, i) + " " + goodsNames[i];
			}
			return text + "<br> Za pripravu protijedu pak sama bude chtit 20 tisic zlatych.";
		}
		#endregion the journal

		#region Roxana's goods
		public static int Given(Player player, int index) {
			object tag = player.GetTag(goodsTK[index]);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		/// <summary>f_q026_hcnPoison_ResourceRemain.</summary>
		public static int Remaining(Player player, int index) {
			return Math.Max(0, GoodsNeeded[index] - Given(player, index));
		}

		/// <summary>f_q026_hcnPoison_ResourcesCheck.</summary>
		public static bool AllGoodsGiven(Player player) {
			for (int i = 0; i < Goods.Length; i++) {
				if (Remaining(player, i) > 0) {
					return false;
				}
			}
			return true;
		}

		/// <summary>f_q026_hcnPoison_ResourcesGive: of each, what the pack holds, never more than she still wants.</summary>
		internal static void GiveGoods(Player player) {
			for (int i = 0; i < Goods.Length; i++) {
				int take = Math.Min(Remaining(player, i), Q006.Count(player, Goods[i]));
				if (take > 0) {
					Q006.Consume(player, Goods[i], take);
					player.SetTag(goodsTK[i], Given(player, i) + take);
				}
			}
		}

		internal static void ForgetGoods(Player player) {
			foreach (TagKey key in goodsTK) {
				player.RemoveTag(key);
			}
		}
		#endregion Roxana's goods

		#region the healers, Andor, Unas
		/// <summary>f_q026_hcnPoison_HealersDialog_Request + _Answer, on a healer's page (Eriol's, the one ported healer).</summary>
		public static void HealerLines(QuestDialogContent d, Character npc, Player player) {
			int stage = GetStage(player);
			if ((stage != 2) && (stage != 3)) {
				return;
			}
			d.SetText((player.Realm == Realm.Gondor)
				? "<def_questDialog_colorTextResponse>Omlouvam se, ze obtezuji, ale jsem <?src.sex(\"nakazen\",\"nakazena\")?> zvlastni chorobou z hrobek cernych Numenorejcu. Nevis, co by mi mohlo byt?"
				: "<def_questDialog_colorTextResponse>Hele, jsem <?src.sex(\"nakazen\",\"nakazena\")?> jakousi zvlastni chorobou z hrobek cernych Numenorejcu. Co to je?");
			d.Response(true, false, "q026_hcnPoison");
			d.Page("q026_hcnPoison");
			d.SetText("Hmmm... Tak s timhle si nevim rady. Zda se mi, ze tva otrava je magickeho puvodu. Opravdu nic tak podivneho jsem ve svem zivote " + (IsFemale(npc) ? "nevidela" : "nevidel") + ". S takovym problemem ti muze pomoci jen nejaky zkuseny klerik. Poohledni se po nekom, kdo se vyzna v jedech.");
			d.Html();
			if (stage == 2) {
				d.SetText("*Podekovat a popremyslet o sdelene informaci*");
				d.Response(false, true, "q026_healer", true);
			}
		}

		/// <summary>f_q026_hcnPoison_HealersDialog_Button.</summary>
		public static bool HealerButton(string button, Player player) {
			if ((button == "q026_healer") && (GetStage(player) == 2)) {
				Advance(player, 3);
				return true;
			}
			return false;
		}

		/// <summary>f_q026_hcnPoison_Andor_Request + _Answer, on his "Kdo jsi?" page.</summary>
		internal static void AndorLines(QuestDialogContent d, Player player) {
			if (GetStage(player) != 3) {
				return;
			}
			d.SetText((player.Realm == Realm.Gondor)
				? "<def_questDialog_colorTextResponse>U Yavanny. Ty se zabyvas jedy? <?src.sex(\"Nakazil\",\"Nakazila\")?> jsem se zvlastni chorobou v hrobkach cernych Numenorejcu. Nevis, co by mi mohlo byt?"
				: "<def_questDialog_colorTextResponse>U Saurona prokletyho, ty se zabyvas jedy? <?src.sex(\"Nakazil\",\"Nakazila\")?> jsem se zvlastni chorobou v hrobkach cernych Numenorejcu. Nevis co to je?!");
			d.Response(true, false, "q026_hcnPoison");
			d.Page("q026_hcnPoison");
			d.SetText("<def_questDialog_colorTextEmote>*Klerik se nejprve nejiste pousmal, pote vsak na tebe upnul svuj pohled a jeho tvar zkamenela. Cely ten pachnouci kolos se v nahlem vystrizliveni a pristoupil k tobe.*<def_questDialog_colorTextNormal> Mohu ?");
			d.Html();
			d.SetText("*Dovolit klerikovi, aby te prohledl.*");
			d.Response(false, false, "q026_hcnPoison_2", true);
			d.Page("q026_hcnPoison_2");
			d.SetText("<def_questDialog_colorTextEmote>*Andor vzal tvou hlavu do dlani a patrave si te prohledl. Po chvili se odmlcel a usedl na zidli. Sklopil zrak...*<def_questDialog_colorTextNormal>");
			d.Html();
			d.SetText("Co se deje ?!");
			d.Response(false, false, "q026_hcnPoison_3");
			d.Page("q026_hcnPoison_3");
			d.SetText("Tva choroba neni bezna. Citim v tobe velke mnozstvi temne energie. Otrava te vysava zevnitr a postupne pohlcuje. Kdysi davno jsem cetl o podobnych priznacich. Ve velkych knihovnach lze zvedet vice o zapomenutych jedech, ovsem prehled v takovem mnozstvi informaci je spise domenou dlouhovekych kouzelniku. Co ti jini lecitele nedokazali sdelit je, ze jde spise o proklti nez otravu. Cerna magie ti sala z kazdeho poru v tele a neustale v tobe sili. Poptej se u starych, moudrych carodeju. Ti snad budou vedet vic. Ja tve prokleti vylecit nedokazu.");
			d.Html();
			d.SetText("*Odejit a porozhlednout se po carodeji, ktery by ti mohl pomoci*");
			d.Response(false, true, "q026_andor", true);
		}

		/// <summary>f_q026_hcnPoison_Unas_Request + _Answer, on his "Kdo jsi?" page (Q006's D_Q006Unas).</summary>
		public static void UnasLines(QuestDialogContent d, Player player) {
			int stage = GetStage(player);
			if ((stage == 4) || (stage == 5)) {
				d.SetText((stage == 4)
					? "<def_questDialog_colorTextResponse>Zdravim te. Shanim se po nejakem magovi, ktery se vyzna v praktikach starych nekromantu. <?src.sex(\"Byl jsem otraven\",\"Byla jsem otravena\")?> v hrobkach cernych Numenorejcu a nikdo si mou otravou nevi rady."
					: "<def_questDialog_colorTextResponse>Ah ! Prave tebe shanim! <?src.sex(\"Dozvedel\",\"Dozvedela\")?> jsem se, ze mas obsahle znalosti o praktikach starych nekromantu. Potrebuji vylecit mou otravu z hrobek cernych Numenorejcu.");
				d.Response(true, false, "q026_hcnPoison");
				d.Page("q026_hcnPoison");
				d.SetText("Ooo... Otrava z hrobek v pousti rikas. <def_questDialog_colorTextEmote>*Kouzelnik se dlouze zamyslel, nez znovu promluvil*<def_questDialog_colorTextNormal>");
				d.Html();
				d.SetText("*Pokracovat*");
				d.Response(false, false, "q026_hcnPoison_2", true);
				d.Page("q026_hcnPoison_2");
				d.SetText("Ach ano... Mozna ti mohu pomoci. Povim ti pro mnohe davno zapomenuty pribeh, jez jsem poskladal z nepatrnych odkazu. Precetl jsem tisice knih pri studiich puvodu nekomantskych praktik a myslim, ze bys <src.sex(\"mel\",\"mela\")> vedet, co naznacuji...");
				d.Html();
				d.SetText("Posloucham...");
				d.Response(false, false, "q026_hcnPoison_3");
				d.Page("q026_hcnPoison_3");
				d.SetText("Jeste za casu Melkora, prvniho temneho pana, se nekteri z mocnych Maiar strzenych temnou stranou zreklo povysene libovule sveho vladce. Zahledeli se do vlastnich tuzeb a prahli po vlastni moci. Odmitli dale bojovat pro jine a zavrhli okolni svet. Ve vysokych horach a hlubokych kobkach vystaveli rozlehle ukryty, ktere obestreli kletbami a znetvorenymi, nemrtvymi prisluhovaci, kteri meli branit jejich klid v hlubinach.");
				d.Html();
				d.SetText("*Poslouchat dal*");
				d.Response(false, false, "q026_hcnPoison_4", true);
				d.Page("q026_hcnPoison_4");
				d.SetText("Po staletich vsak jejich odpor prerostl v nenavist. Prestali prochazet sva sklepeni a zabarikadovali se v nejhlubsich komorach. Bez vedeni se zdala jejich sidla po letech pusta a nenapadna. Do jejich ukrytu se zacali odvazovat prvni skupiny vedene mocnymi cernoknezniky, kteri prahli po vedomostech ukrytych v norach spicich Maiar. Nejuspesnejsimi z dobyvatelu techto brlohu byli prave davni Numenorejci. Jejich nekromanti si tak osvojili mnohe vedomosti o magickych branach, zamcich, jedech, a dokonce se odvazovali probouzet mrtve. Tyto znalosti pak vyuzivali pro ochranu hrobek svych kralu a pecetili je tak proti chamtivym zlodejum.");
				d.Html();
				d.SetText("*Pokracovat*");
				d.Response(false, false, "q026_hcnPoison_5", true);
				d.Page("q026_hcnPoison_5");
				d.SetText("<src.sex(\"Rikal\",\"Rikala\")> jsi, ze k tve otrave doslo prave v hrobkach cernych Numenorejcu. Je-li tomu tak, pak tve vyleceni zavisi na nalezeni davno zapomenutych alchymistickych knih Numenorejskych nekromantu. Z nekolika pramenu jsem vycetl zminku o davnem chramu vedeni, ktery vyuzivaly narody jako neutralni uzemi pro uschovani a vymenu vedomosti. Zkoumam tuto oblast a cele Numenorejske vedeni po dlouha leta a vse nasvedcuje tomu, ze lezi uprostred techto neprostupnych bazin. Bez a najdi cestu do tohoto chramu. Patrej po ostatcich Numenorejske knihovny. Je-li na tvou otravu lek a budes-li mit stesti, pak jej tam najdes hotovy, nebo alespon popsany v nejake knize.");
				d.Html();
				d.SetText("*Podekovat a vydat se do bazin.*");
				d.Response(false, true, "q026_unasTemple", true);
			} else if (stage == 11) {
				d.SetText("<def_questDialog_colorTextResponse>Znovu te zdravim priteli. Podarilo se mi dostat do chramu vedeni a ziskat z Numenorejske knihovny tuto knihu. Jeji obsah je vsak pro mne necitelny. Dokazes jej rozlustit? Potrebuji lek na svou otravu.");
				d.Response(true, false, "q026_hcnPoison");
				d.Page("q026_hcnPoison");
				d.SetText("Ach vyborne! Ukaz, podivam se na tu knihu...");
				d.Html();
				d.SetText("*Podat carodeji knihu*");
				d.Response(false, false, "q026_hcnPoison_2", true);
				d.Page("q026_hcnPoison_2");
				d.SetText("<def_questDialog_colorTextEmote>*Carodej nejprve nejaky cas zkoumal text v knize, nez se znovu vzprimil a promluvil.*<def_questDialog_colorTextNormal> Ano.. Ano toto jsou stare alchymisticke symboly. Jde o sifru. Kazdy alchymista nejakou pouziva, aby chranil sva tajemstvi. Je tu mnoho masti, lektvaru a rozboru jejich ucinku. Ukaz, prohlednu si te a popis mi, co presne tva otrava zpusobuje.");
				d.Html();
				d.SetText("*Popsat carodeji priznaky tve otravy*");
				d.Response(false, false, "q026_hcnPoison_3", true);
				d.Page("q026_hcnPoison_3");
				d.SetText("<def_questDialog_colorTextEmote>*Carodej te prohledl a opet se na nejaky cas zaboril do urputne studie knihy. Po nekolika minutach se k tobe obratil s nadeji ve tvari.*<def_questDialog_colorTextNormal> Tady je to! Otrava prizraku. Jde o velmi mocnou temnou magii.. Aaah, tato neni lidskeho puvodu... Mas velike stesti. Zachoval se cely popis vyroby protileku a navic, je-li muj vyklad spravny, ziskas proti tomuto jedu trvalou imunitu. Neni cas otalet! Tady, vezmi si recept, ktery ti sepisu. Jeho priprava vyzaduje spoustu zkusenosti, az nadlidskou presnost pri priprave a mnoho surovin. Ani jedno ti nemohu nabidnout. Znam jedineho cloveka, ktery je neceho takoveho schopen. Vydej se za Roxanou. Zije po dlouha leta na jihu Temneho Hvozdu. Hodne stesti!");
				d.Html();
				d.SetText("*Prevzit recept a vydat se za Roxanou*");
				d.Response(false, true, "q026_unasRecipe", true);
			}
		}

		/// <summary>f_q026_hcnPoison_Unas_Button.</summary>
		public static bool UnasButton(string button, Player player) {
			int stage = GetStage(player);
			if ((button == "q026_unasTemple") && ((stage == 4) || (stage == 5))) {
				Advance(player, 6);
				return true;
			}
			if ((button == "q026_unasRecipe") && (stage == 11)) {
				Advance(player, 12);
				return true;
			}
			return false;
		}
		#endregion the healers, Andor, Unas

		#region the temple and Dol Guldur
		/// <summary>t_q026_TOK_tapestry's @UserDClick.</summary>
		internal static void ReadTapestry(Player player) {
			int stage = GetStage(player);
			if (stage == 6) {
				player.SysMessage("Zajimavee...");
				Advance(player, 7);
				LevelingSystem.AddExperience(player, TapestryExp);
			} else if (stage > 6) {
				player.SysMessage("Tady uz nic dalsiho nevyctes");
			}
		}

		private static readonly string[] bookMessages = { "Matematika...", "Dejiny...", "Smes nesrozumitelnych cmaranic...", "Hmm.. tak tahle kniha je prilis poskozena..",
			"Nevidis nic zajimaveho.", "Vetsina knih je potrhanych.", "Tato kniha se neda vubec cis.", "Na tyto knihy dlouho nikdo nesahl..", "Nektere knihy jsou polite.",
			"A hele! Na teto knize je dokonce plisen.", "Fuj, tak tohle snad ani nemas za potrebi...", "Kdesi v tomhle haraburdi preci musi byt neco o jedech", "Anatomie ... hmmm...",
			null, "Haraburdi..." };

		/// <summary>i_q026_TOK_bookPile's @UserDClick: one search in six turns up the book, if the hero has the key and hasn't it yet.</summary>
		internal static void SearchBooks(Item pile, Player player, bool lucky) {
			if (Point2D.GetSimpleDistance(pile, player) >= 2) {
				player.SysMessage("Jsi prilis daleko");
				return;
			}
			int stage = GetStage(player);
			if (lucky && ((stage == 9) || (stage == 10))) {
				Advance(player, 11);
				player.SysMessage("Tahle kniha vypada zajimave...");
				return;
			}
			string message = bookMessages[Globals.dice.Next(bookMessages.Length)];
			player.SysMessage(message ?? ("Kniha, kterou jsi prave " + (IsFemale(player) ? "vzala" : "vzal") + " do ruky se rozpadla."));
		}

		/// <summary>i_q026_DG_secretRoom_alarm's @Step.</summary>
		internal static void StepOnAlarm(Player player) {
			if (!player.IsGM && (GetStage(player) == 7)) {
				Advance(player, 8);
			}
		}

		/// <summary>i_q026_DG_secretDoor's @UserDClick: a hero past the tile pushes the wall aside for a minute.</summary>
		internal static void PushWall(Item wall, Player player) {
			if (GetStage(player) <= 7) {
				return;
			}
			if (wall.GetTag(OpenTK) != null) {
				CloseWall(wall);
				return;
			}
			wall.SetTag(OpenTK, wall.Model);
			wall.P(wall.X - 1, wall.Y, wall.Z);
			wall.Model = 0x0242;
			wall.Sound(543);
			player.SysMessage("Tlacis do steny a oteviras tajnou mistnost");
			wall.AddTimer(Q026CloseTimer.Key, new Q026CloseTimer()).DueInSeconds = OpenSeconds;
		}

		internal static void CloseWall(Item wall) {
			object model = wall.GetTag(OpenTK);
			if (model == null) {
				return;
			}
			wall.RemoveTag(OpenTK);
			wall.DeleteTimer(Q026CloseTimer.Key);
			wall.P(wall.X + 1, wall.Y, wall.Z);
			wall.Model = Convert.ToInt32(model, CultureInfo.InvariantCulture);
			wall.Sound(543);
		}

		/// <summary>i_q026_DG_secretRoom_box's @UserDClick: the key, here the stage.</summary>
		internal static void SearchBox(Player player) {
			int stage = GetStage(player);
			if (stage == 8) {
				Advance(player, 9);
				player.SysMessage("A hele, klicek..");
			} else if (stage > 8) {
				player.SysMessage("V truhle uz nic neni");
			}
		}

		public const int OpenDoorModel = 0x2a06; //i_q026_TOK_slidingDoorOpened: a door one walks onto, where its @step stops the keyless

		/// <summary>t_q026_TOK_slidingDoor's @UserDClick: with the key it slides open for a minute. Erebor swapped the closed door 30
		/// down and an open one 30 up; here the one door takes the open look where it stands.</summary>
		internal static void UseDoor(Item door, Player player) {
			int stage = GetStage(player);
			if (stage == 9) {
				player.SysMessage("A hele, ono to stacilo posunout do strany...");
				Advance(player, 10);
				stage = 10;
			}
			if (stage <= 9) {
				player.SysMessage("Cloumas dvermi vpred i vzad, ale nedari se ti je otevrit.");
				return;
			}
			if (door.GetTag(OpenTK) != null) {
				CloseDoor(door);
			} else {
				door.SetTag(OpenTK, door.Model);
				door.Model = OpenDoorModel;
				door.AddTimer(Q026CloseTimer.Key, new Q026CloseTimer()).DueInSeconds = OpenSeconds;
			}
			door.Sound(47);
		}

		internal static void CloseDoor(Item door) {
			if (door.GetTag(OpenTK) == null) {
				return;
			}
			object model = door.GetTag(OpenTK);
			door.RemoveTag(OpenTK);
			door.DeleteTimer(Q026CloseTimer.Key);
			door.Model = Convert.ToInt32(model, CultureInfo.InvariantCulture);
		}

		/// <summary>t_q026_TOK_slidingDoor's @step: a force keeps out anyone without the key.</summary>
		internal static void StepOnDoor(Item door, Player player) {
			if (player.IsGM) {
				return;
			}
			if (player.Flag_Dead || (GetStage(player) < 9)) {
				player.Go(player.X, player.Y + 1, player.Z, player.M); //move(s)
				if (!player.Flag_Dead) {
					DamageManager.CauseDamage(null, player, DamageType.Irresistable, DoorDamage);
					player.SysMessage("Ve dverich te zastavila jakasi sila.");
					door.Sound(14); //wind whistle
				}
			} else {
				player.SysMessage("Klic v tve kapse se lehce zachvel");
			}
		}
		#endregion the temple and Dol Guldur

		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			QuestDialogDef dialog = (npc.Def == ThingDef.GetByDefname(RoxanaDef)) ? (QuestDialogDef) SingletonScript<D_Q026Roxana>.Instance : SingletonScript<D_Q026Andor>.Instance;
			npc.Dialog(player, dialog, npc);
		}
	}

	/// <summary>#1272 - i_q026_hcn_memoryPoison's timer, on the hero: it ticks in the world only, as an item's timer on Sphere.</summary>
	[SaveableClass, DeepCopyableClass]
	public class Q026CurseTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_q026Curse_");

		[LoadingInitializer, DeepCopyImplementation]
		public Q026CurseTimer() {
		}

		/// <summary>Tests only: a short fixed cadence instead of Erebor's, 0 = off.</summary>
		internal static double testSeconds;

		internal static double NextSeconds() {
			return (testSeconds > 0) ? testSeconds : (400 + Globals.dice.Next(401)); //def_q026_poisonTimer {400 800}
		}

		protected override void OnTimeout(TagHolder cont) {
			Player player = (Player) cont;
			int stage = Q026.GetStage(player);
			if ((stage < 1) || (stage >= Q026.Done)) {
				player.DeleteTimer(Key);
				return;
			}
			if ((player.GameState != null) && !player.Flag_Dead) {
				Q026.CurseTick(player);
			}
			this.DueInSeconds = NextSeconds();
			this.PeriodInSeconds = this.DueInSeconds;
		}
	}

	/// <summary>#1272 - the temple door and the Dol Guldur wall close a minute after they open.</summary>
	[SaveableClass, DeepCopyableClass]
	public class Q026CloseTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_q026Close_");

		[LoadingInitializer, DeepCopyImplementation]
		public Q026CloseTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Item item = (Item) cont;
			if (item.Def == ThingDef.GetByDefname(Q026.WallDef)) {
				Q026.CloseWall(item);
			} else {
				Q026.CloseDoor(item);
			}
		}
	}

	/// <summary>#1272 - Klerik Andor, Erebor's d_q016_Andor without q016 (not ported).</summary>
	public class D_Q026Andor : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3001;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			d.BaseButton(1);
			d.BaseButton(2);
			d.BaseButton(5);
			d.BaseButton(6);
			d.Page("Pozdrav");
			d.SetText("Nech me... skyt... byt, mam neco na praci... skyt, nevidis?");
			d.Html();
			Leave(d);
			d.Page("Kdo jsi?");
			d.SetText("Eh.. <def_questDialog_colorTextEmote>*skytnul*<def_questDialog_colorTextNormal> .. uh .. klerik Ando-or.. jmeno meee... Odbo-ornik na jedy, znam-y sirokaleko <def_questDialog_colorTextEmote>*hyc*<def_questDialog_colorTextNormal> <src.sex(cizince,slicna daa-amo... maas peknou.. posta-avu..)> <def_questDialog_colorTextEmote>*zacal prudce a zurive zvracet*<def_questDialog_colorTextNormal>.");
			d.Html();
			Leave(d);
			Q026.AndorLines(d, player);
			d.Page("Nesu ti");
			d.SetText("Ehh... co.. Mas pro me neco?");
			d.Html();
			d.Page("Mohu pomoci?");
			d.SetText("Aaehlle... Ja nevim ... Nic od tebe nechci ...");
			d.Html();
			Leave(d);
			d.Page("Prohlednout");
			d.SetText("Velmi, VELMI opily klerik. Pres veskerou lasku k boji, pohledu na krev nebo zmuceny vyraz tvych nepratel, kdyz jim zasazujes posledni ranu, mas obcas pocit, ze jsi se <src.sex(minul,minula)> povolani. Tenhle muz zjevne pro pridel nechodi daleko.");
			d.Html();
		}

		private static void Leave(QuestDialogContent d) {
			d.SetText("*Odejit. Tenhle hovor nema smysl.*");
			d.Response(false, true, "q026_leave", true);
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if ((button == "q026_andor") && (Q026.GetStage(player) == 3)) {
				Q026.Advance(player, 4);
			}
		}
	}

	/// <summary>#1272 - Carodejnice Roxana, Erebor's d_q020_roxana without q020 and q022 (not ported).</summary>
	public class D_Q026Roxana : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3121;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int stage = Q026.GetStage(player);
			for (int i = 1; i <= 6; i++) {
				d.BaseButton(i);
			}
			d.Page("Pozdrav");
			d.SetText("Co chces?! Proc lezes do meho lesa?");
			d.Html();
			d.Page("Kdo jsi?");
			d.SetText("Me jmeno je Roxana. Jsem carodejnice.");
			d.Html();
			if (stage == 12) {
				d.SetText("<def_questDialog_colorTextResponse> Posila mne carodej Unas. <?src.sex(\"byl jsem otraven\",\"byla jsem otravena\")?> a potrebuji zhotovit lektvar. Pry jsi jedina, kdo to dokaze. <?src.sex(\"Nenalehal\",\"Nenalehala\")?> bych tolik, ale je to velmi nalehave!");
				d.Response(true, false, "q026_hcnPoison");
				d.Page("q026_hcnPoison");
				d.SetText("Podivam se na to... Ukaz mi ten recept.");
				d.Html();
				d.SetText("*Predat Unasuv recept carodejce*");
				d.Response(false, false, "q026_hcnPoison_2", true);
				d.Page("q026_hcnPoison_2");
				d.SetText("Oh, nic takoveho jsem jeste nevidela. Pokud ale Unas veri, ze ti to pomuze, pak tomu tak bude. Suroviny na pripravu si ale musis obstarat <?src.sex(\"sam\",\"sama\")?>. Nektere prisady mam, ale spousta mi toho jeste chybi. Cely lektvar se navic musi pripravit v obrovskem zaru, a tak bude potreba spousta dreva. Tu mas, sepisu ti, co vse jeste potrebuji donest. A priprav se, ze za svou ochotu a cas po tobe budu chtit 20 tisic zlatych. Je toho hodne, tak mi to nos postupne. Ja si to tady budu ukladat.");
				d.Html();
				d.SetText("*Prevzit od carodejky seznam a vydat se pro suroviny*");
				d.Response(false, true, "q026_roxanaAccept", true);
			}
			d.Page("Mohu pomoci?");
			d.SetText("Nepotrebuji od tebe zadnou pomoc.");
			d.Html();
			d.Page("Obchodovat");
			d.SetText("Nic neprodavam ani nekupuji.");
			d.Html();
			d.Page("Hledam praci");
			d.SetText("Pro tebe nemam zadnou praci!");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Co mi neses?");
			d.Html();
			if (stage == 13) {
				if (Q026.AllGoodsGiven(player)) {
					d.SetText("<def_questDialog_colorTextResponse><src.sex(\"Donesl\",\"Donesla\")> jsem ti vsechny suroviny na protilek, ktere jsi chtela. Muzes mi ho ted vyrobit ?");
					d.Response(true, false, "q026_hcnPoison");
					d.Page("q026_hcnPoison");
					d.SetText("Vyborne! Dobra tedy, dej mi tech slibenych 20 tisic a mas ho mit.");
					d.Html();
					d.SetText("Promin, nemam u sebe dost penez. Vratim se pozdeji.");
					d.Response(false, true, "q026_noMoney");
					if (GoldUtils.CanPay(player, Q026.CureGold)) {
						d.SetText("*Predat carodejce penize a ziskat protilek*");
						d.Response(false, true, "q026_getCure", true);
					}
				} else {
					d.SetText("*Odevzdat nejake suroviny na lek otravy z hrobek*");
					d.Response(false, true, "q026_roxanaGoods", true);
				}
			}
			d.Page("Prohlednout");
			d.SetText("<def_questDialog_colorTextEmote>*Vidis velmi starou carodejnici, z ktere i prez jeji vek vyzaruje autorita*<def_questDialog_colorTextNormal>");
			d.Html();
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int stage = Q026.GetStage(player);
			if ((button == "q026_roxanaAccept") && (stage == 12)) {
				Q026.ForgetGoods(player);
				Q026.Advance(player, 13);
			} else if ((button == "q026_roxanaGoods") && (stage == 13)) {
				Q026.GiveGoods(player);
				QuestJournal.Report(player, Q026.Journal); //f_questMenu_reportQuest: what's still wanted
			} else if ((button == "q026_getCure") && (stage == 13) && Q026.AllGoodsGiven(player)) {
				if (!GoldUtils.Pay(player, Q026.CureGold, PaySource.Usual, "")) {
					npc.Say("Tak kde mas ty penize ?!");
					return;
				}
				Q026.ForgetGoods(player);
				player.SysMessage("Carodejka ti podala protilek a ty jsi jej hned " + ((player.Gender == Gender.Female) ? "vypila" : "vypil") + ".");
				Q026.Cure(player);
			}
		}
	}

	/// <summary>#1272 - @UserDClick of Andor and Roxana.</summary>
	public class E_Q026Npc : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q026.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1272 - the temple's tapestry, book pile and door, Dol Guldur's tile, wall and box.</summary>
	public class E_Q026Fixture : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (!(clicker is Player player) || player.Flag_Dead) {
				return TriggerResult.Cancel;
			}
			ThingDef def = self.Def;
			if (player.IsGM && (def != ThingDef.GetByDefname(Q026.DoorDef)) && (def != ThingDef.GetByDefname(Q026.WallDef))) {
				player.SysMessage("Predmet questu q026 jed z hrobek (" + self.Def.PrettyDefname + ").");
				return TriggerResult.Cancel;
			}
			if (def == ThingDef.GetByDefname(Q026.TapestryDef)) {
				Q026.ReadTapestry(player);
			} else if (def == ThingDef.GetByDefname(Q026.BookPileDef)) {
				Q026.SearchBooks(self, player, Globals.dice.Next(6) == 1);
			} else if (def == ThingDef.GetByDefname(Q026.WallDef)) {
				Q026.PushWall(self, player);
			} else if (def == ThingDef.GetByDefname(Q026.BoxDef)) {
				Q026.SearchBox(player);
			} else if (def == ThingDef.GetByDefname(Q026.DoorDef)) {
				Q026.UseDoor(self, player);
			}
			return TriggerResult.Cancel;
		}

		public void On_Step(Item self, Character stepping) {
			if (!(stepping is Player player)) {
				return;
			}
			if (self.Def == ThingDef.GetByDefname(Q026.AlarmDef)) {
				Q026.StepOnAlarm(player);
			} else if (self.Def == ThingDef.GetByDefname(Q026.DoorDef)) {
				Q026.StepOnDoor(self, player);
			}
		}
	}

	/// <summary>#1272 - spk_q016_Andor and spk_q020_roxana: their greetings, from close enough.</summary>
	public sealed class Q026Speech : AbstractSpeech {
		private readonly string[] patterns;

		public new static void Bootstrap() {
			new Q026Speech("spk_q016_Andor", new[] { "hi", "ahoj", "cus", "hoj", "nazdar", "dobry den", "huja", "zdar" }).Register();
			new Q026Speech("spk_q020_roxana", new[] { "zdravim", "hi", "dobry", "den", "ahoj" }).Register();
		}

		private Q026Speech(string defname, string[] patterns)
			: base(defname) {
			this.patterns = patterns;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, this.patterns) && QuestNpc.IsCloseEnough(npc, player)) {
				Q026.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
