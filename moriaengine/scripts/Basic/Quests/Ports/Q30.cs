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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1276 - Erebor's q30 "Do zbrane!" (questy/q30_zmocnenec_*.scp): the realm's envoy (Gondor's c_q30_zmocnenecg, Mordor's
	/// c_q30_zmocnenecm, the Grandmaster quest's NPCs too) tests recruits with 14 trophy hunts, each a collector bag (#1247), from level
	/// 1 to 90, 54,650 experience in all, and a warrior's cloak at the end (Plast bojovnika: +10 hit point regeneration, +20% against
	/// dragons, the realm's colour, its owner's only). The last hunt can be done again for a lost cloak: 8, then 12 dragon hearts, no
	/// experience.
	/// <para>Erebor's qtags gq030_zmocnenec / mq030_zmocnenec: even = may take a hunt (0, 2, ... 26), odd = on one, 28 = done;
	/// q030_finished counts the cloaks (at most 2); q030_presel keeps how far a hero who changed realm got on the other ladder. Such a
	/// hero (Erebor's tag.presel, set by the realm change of q041) starts again with half experience until past that stage, and a thief
	/// who began the other ladder isn't taken at all.</para>
	/// </summary>
	public static class Q30 {
		public const string CloakDef = "i_plast_bojovnika";
		public const int Done = 28;
		public const int GondorCloakColor = 0x098F, MordorCloakColor = 0x0993; //d_uniformRealm_clothes[1], [2]
		public const int RegenValue = 10, DragonValue = 20; //imi_value[70], [102]
		public static readonly int[] Levels = { 1, 3, 6, 12, 15, 20, 25, 30, 40, 50, 60, 70, 80, 90 };
		public static readonly int[] Experience = { 150, 300, 600, 1000, 1200, 1400, 1800, 2200, 3000, 4000, 6000, 8000, 10000, 15000 };
		private static readonly string[] bagSuffixes = { "03", "36", "612", "1215", "1520", "2025", "2530", "3040", "4050", "5060", "6070", "7080", "8090", "90100" };

		private static readonly TagKey gondorTK = TagKey.Acquire("_gq030Zmocnenec_");
		private static readonly TagKey mordorTK = TagKey.Acquire("_mq030Zmocnenec_");
		private static readonly TagKey finishedTK = TagKey.Acquire("_q030Finished_");
		private static readonly TagKey preselTK = TagKey.Acquire("_q030Presel_");
		/// <summary>Erebor's tag.presel: the hero changed realm (q041's realm change sets it).</summary>
		public static readonly TagKey SwitchedRealmTK = TagKey.Acquire("_presel_");
		internal static readonly TagKey CloakOwnerTK = TagKey.Acquire("_q030CloakOwner_");

		public static QuestJournalEntry GondorJournal { get; private set; }
		public static QuestJournalEntry MordorJournal { get; private set; }

		public static void Bootstrap() {
			GondorJournal = QuestJournal.Register(new QuestJournalEntry("Zmocnenec Gondoru", 0x3002, p => JournalText(p, true), p => IsDone(p, true)));
			MordorJournal = QuestJournal.Register(new QuestJournalEntry("Zmocnenec Mordoru", 0x3111, p => JournalText(p, false), p => IsDone(p, false)));
		}

		#region tags
		public static int GetStage(Player player, bool gondor) {
			object tag = player.GetTag(gondor ? gondorTK : mordorTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		internal static void SetStage(Player player, bool gondor, int stage) {
			if (stage == 0) {
				player.RemoveTag(gondor ? gondorTK : mordorTK);
			} else {
				player.SetTag(gondor ? gondorTK : mordorTK, stage);
			}
			QuestJournal.Report(player, gondor ? GondorJournal : MordorJournal);
		}

		public static int Finished(Player player) {
			object tag = player.GetTag(finishedTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		internal static void SetFinished(Player player, int value) {
			if (value == 0) {
				player.RemoveTag(finishedTK);
			} else {
				player.SetTag(finishedTK, value);
			}
		}

		public static int Presel(Player player) {
			object tag = player.GetTag(preselTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		internal static void ForgetPresel(Player player) {
			player.RemoveTag(preselTK);
		}

		public static bool IsDone(Player player, bool gondor) {
			return GetStage(player, gondor) == Done;
		}

		/// <summary>f_q030_playerCanGetWork: even.</summary>
		public static bool CanGetWork(int stage) {
			return (stage % 2) == 0;
		}

		/// <summary>The hunt a stage is about (0..13): its bag, level, experience and text.</summary>
		public static int HuntOf(int stage) {
			return Math.Min(stage, Done - 1) / 2;
		}

		/// <summary>f_q030_getRelevantCollector.</summary>
		public static string BagOf(int stage, bool gondor) {
			return (gondor ? "i_q30_zmocnenecg_collector_" : "i_q30_zmocnenecm_collector_") + bagSuffixes[HuntOf(stage)];
		}
		#endregion tags

		#region the envoy
		/// <summary>Erebor's f_gq030_refuseQDialog / f_mq030_refuseQDialog: what the envoy says instead of talking, or null. A hero who
		/// changed realm leaves the other ladder (remembered in q030_presel) on their first visit.</summary>
		public static string Refusal(Player player, bool gondor) {
			if (player.Realm != (gondor ? Realm.Gondor : Realm.Mordor)) {
				return gondor ? "Vyhni se memu meci, prisluhovaci Mordoru !" : "Vyhni se memu meci, prisluhovaci Gondoru !";
			}
			if (player.GetTag(SwitchedRealmTK) != null) {
				int other = GetStage(player, !gondor);
				if ((player.Profession == Professions.Thief) && (other >= 1)) {
					return "Jdi pryc";
				}
				if (Presel(player) == 0) {
					player.SetTag(preselTK, other);
				}
				player.RemoveTag(gondor ? mordorTK : gondorTK);
			}
			return null;
		}

		private static readonly string[] gondorWork = {
			"Vyborne, pripisuji si te na seznam dobrodruhu, kteri plni me ukoly. A ted k veci : Dam ti tady vacek, do ktereho mi hezky naskladej 20 zubu Velkych potkanu a prines mi je. Aspon pomuzes procistit mestskou kanalizaci. ",
			"No vypada to, ze krysy jsi <?src.sex(zvladl,zvladla)?> a mas odvahu zkusit neco tezsiho, ze ? Novy stin z Mordoru pouziva pro boj odporna stvoreni. Obzivle kostry lidi i skretu. Sem do toho vacku naskladas a prineses devet kosti z Kostlivcu Sekerniku, protoze ti se zdaji byt nejsilnejsi a musis se s nimi naucit bojovat. Nepochybne vis kde je najit.. ",
			"Haha, skvele nemas strach ani z nemrtvych kostlivcu, to se ti bude hodit. Protoze ted potrebuji aby ses utkal s Lichem, mocnym demonem temne moci. Musis mi prines 6, nebo ne lepsi bude osm oci techto demonu! Mel bys byt opatrny, protoze maji zvlastni schopnosti... ",
			"Zda se, ze to se sluzbou myslsi vazne, kdyz ses odvazil a porazil lichy. Ted vyuzijes svoje znalosti kanalizace, protoze potrebuji abys nasel infikovane krysy a prinesl mi z nich 10 drapku. A dej si pozor na jejich jed! ",
			"Vyborne, jde ti to skvele! Ale ted to bude opravdu nebezpecne. V dalekych krajich ziji obrovsti pavouci. Mezi nimi vynikaji cerne vdovy. Prines mi 7 vajicek techto pavouku.  ",
			"<?src.sex(Zvladl,Zvladla)?> jsi toho jiz hodne, ale musis zvladnout jeste vic, chces-li slouzit krali a byt cten jako gondorsky bojovnik. Obri, nebo titani, jak se jim zacalo rikat jsou silni protivnici. A prave to te ceka, potrebuju totiz jejich nehty. Alespon sedum. Odvazis se?",
			"Driv jsi mi <?src.sex(\"prinesl\",\"prinesla\")?> nehty obru. Mozna jsi <?src.sex(\"zahledl\",\"zahledla\")?> jejich vudce, lordy. A prave jejich nehty mi ted musis prinest. Ctyri, nebo radsi sest. Budu te ocekavat bojovniku.",
			"Uz jsi <?src.sex(\"bojoval\",\"bojovala\")?> s mnoha neprateli. Byli silni a velci i mali a zakerni. Ted se musis utkat s jedovym fenixem, postrachem neopatrnych poutniku. Jako dukaz mi prineses deset pirek.",
			"Tento ukol mozna rozhodne, jestli jsi <?src.sex(\"hoden\",\"hodna\")?>, nebot je velmi obtizny! 5 vlasu Strazce Ohne to je to, co po tobe zadam tentokrat. Kazdy ma jen jeden,proto jsou tak vzacne. Kde je najdes ti ale poradit nemuzu.",
			"Zle zpravy! Temny pan povolal do svych sluzeb obry a vybavil je tezkou zbroji! Nasi zvedove vsak hlasi zvlastni vec. Udajne se tito obri vzbourili a vrazdi ted i skrety! Nicmene jsou pro nas velkym nebezpecim protoze nici naco prijdou. Proto musi zemrit. 8 prstu musis prinest v tomto vacku.",
			"Opet jsi <?src.sex(\"prokazal\",\"prokazala\")?> sve schopnosti a odvahu, pri boji s obry. Ale ted musis porazit neco tak ohavneho, ze to ani nema jmeno v nasi reci! Prineses mi cerne srdce toho stvoreni, ale ne jenom jedno! Rovnou 3! Kdysi sme to stvoreni nazyvali Pan kobky, ale to uz je davno... ",
			"Pozor! Vzhledem k tomu jak jsi zkuseny tohle je velmi tezky ukol: Dam ti tady vacek, do ktereho mi hezky naskladej 3 vetvicky z Excelenta a prines mi je.",
			"Toto je uz velmi tezky ukol! Pamatujes si na ty ozbrojene obry, ze? Dari se nam je drzet mimo nase uzemi, avsak zjistili jsme neco zvlastniho. Nekolika z nich se povedlo ukryt se v Ledove Jeskyni. To by mohlo byt v budoucnu nebezpecne! Musis je vyhledat a zabit a donest 5 jejich usi!",
			"Toto je nejtezsi ukol jaky ti mohu nabidnout!  Prines mi tady do vacku 4 draci srdce! Pokud to dokazes, cele kralovstvi bude znat tve jmeno a budes vazenym bojovnikem!"
		};

		private static readonly string[] mordorWork = {
			"Vyborne, pripisuji si te na seznam brancu kteri plni me ukoly. A ted k veci : Tady mas vacek, do ktereho mi hezky naskladas 20 zubu Litych vlku a prineses mi je. Uvidime jestli te nesezerou.",
			"HA! Vlky zvladne kazde skrite! Ted hezky pudes a zabijes Trolly! Urezes jim prsty a sedm mi jich prineses! Rozumis? ",
			"Mozna v tobe neco bude spinavce, uvidime! Ted mi prineses 15 klepet obrich skorpionu, rozumis? Nebo se snad bojis?! ",
			"Mozna si myslis, buh vi co nejsi za valecnika? PCHE! Vratis se mezi skorpiony, najdes jejich krale a jejich klepeta mi prineses! Deset bude stacit. A ted uz bez! ",
			"Nejak ti rostou svaly! Ale v hlave porad prazdno jak vidim, hehe! 3 prsty krale lichu! Co ty nato? Tuhne ti krev v zilach, nebo co? A vis co? prines jich rovnou 5 ! Utikej! Nebo snad nechces slouzit svemu panu? ",
			"Ted uvidime jak umis bojovat s Kyklopy. Sedm jejich oci mi prines! Mordorsky valecnik to musi zvladnout!",
			"Dobra zprava, haha! Vratis se mezi Kyklopy. Najdi jejich krale. 12 nehtu musis prinest jako dukaz!",
			"Uvidime jak si poradis ted! Deset pirek jedoveho fenixe. To bys snad <?src.sex(mohl,mohla)?> zvladnout, ne?",
			"Poradil sis s obry i lichy, takze jsi uz dost silny a zkuseny abys mohl vyhledat Minotaury a prinest mi 5 kuzi. Nebo ne?",
			"Doufam, ze znas ty proradne obry, kterym jsme dali brneni a helmy, aby slouzili Mordoru? Vypada to ze se vzbourili a utoci i na nase! Musi zemrit. Prines mi 8 prstu jako dukaz! Nikdo nezradi Mordor!",
			"Mezi obrimy pavouky jsou i ti nejstarsi z davynch dob. Musis je najit a prinest mi jejich srdce. Aspon 3 nebo jeste lepe ctyri! ",
			"Pozor! Vzhledem k tomu jak jsi zkuseny ti zadam velmi tezky ukol! Prines mi 3 vetvicky z Excelenta. Vis doufam co myslim! ",
			"Haha! Uz se nam, i diky tobe, povedlo najit skoro vsechny ty proradne zradcovske obry! Ale zvedove hlasi, ze se nekteri ukryli v Ledove jeskyni! Kdo vi proc prave tam, ale pomsta Mordoru musi byt vykonana nade vsemi! Proto je musis najit! A pokud to jeste neudelal nekdo jiny, zabit a donest 4 jejich usi!",
			"Tak a ted uvidime! Prines mi tady do vacku 4 draci srdce! Jestli nezemres, budes znam v celem Mordoru!"
		};

		/// <summary>The dragon hearts of a repeated last hunt: 8, then 12 (Erebor's (q030_finished + 1) * 4).</summary>
		public static int RepeatHearts(Player player) {
			return (Finished(player) + 1) * 4;
		}

		/// <summary>d_gq030_zmocnenec / d_mq030_zmocnenec's base buttons.</summary>
		internal static void BaseButtons(QuestDialogContent d, Player player, bool gondor) {
			if (GetStage(player, gondor) == 0) {
				d.BaseButton(1);
			}
			d.BaseButton(2, 21, "Nejake drby ?");
			d.BaseButton(3);
			d.BaseButton(4);
			d.BaseButton(5);
			d.BaseButton(6);
		}

		/// <summary>The "lost my reward" line on the greeting page of a hero who is done.</summary>
		internal static void GreetingLines(QuestDialogContent d, Player player, bool gondor) {
			if (GetStage(player, gondor) == Done) {
				d.SetText("Ztratil jsem svuji odmenu za sluzby, chtel bych pozadat o novou");
				d.Response(false, false, "work_info");
			}
		}

		/// <summary>The rest of the envoy's pages.</summary>
		internal static void Pages(QuestDialogContent d, Player player, bool gondor) {
			int stage = GetStage(player, gondor);
			if (stage == 0) {
				d.Page("Kdo jsi?");
				d.SetText(gondor ? "Jsem Zmocnenec Gondorskeho kralovstvi, mam na starosti zkouseni schopnosti novacku, kteri se prihlasi k sluzbe krali. "
					: "Zdravim... jsem Zmocnenec Mordoru, mam na starosti zkouseni schopnosti novacku, kteri se prihlasi k sluzbe Mordoru! ");
				d.Html();
				d.SetText(gondor ? "Zmocnenec Gondoru? O tom jsem jeste nikdy <?src.sex(neslysel,neslysela)?> ..." : "Zmocnenec Mordoru? O tom jsem jeste nikdy <?src.sex(neslysel,neslysela)?> ... ");
				d.Response(false, false, "neslysel");
				d.SetText("Aha a ceho presne se ty ukoly tykaji?");
				d.Response(false, false, "ukoly");
				d.Page("neslysel");
				d.SetText(gondor ? "Ano ano... To je mozne, protoze tuto funkci nechal Kapitan strazi v Minas Tirithu zridit teprve pred nedavnem... Veri, ze pomuze zajistit lepsi pripravenost mesta, kdyby doslo k utoku... "
					: "Ano ano... To je mozne, protoze tuto funkci nechal Kapitan strazi v Rhunu zridit teprve pred nedavnem... Veri, ze pomuze zajistit lepsi pripravenost mesta, kdyby nahodou doslo k utoku...Od tech prasivych psu z Gondoru nikdy nevis, co cekat.");
				d.Html();
				d.SetText("Aha...");
				d.Response(false, false, "Kdo jsi?");
				d.Page("ukoly");
				d.SetText("Ukoly, ktere zadavam, maji otestovat bojove schopnosti a odvahu. A to jak jinak nez bojem ! Budes muset prokazat sve schopnosti v boji s ruznymi protivniky a samozrejme budu vyzadovat dukaz, ze jsi je <?src.sex(porazil,porazila)?>.");
				d.Html();
				d.SetText("Aha...");
				d.Response(false, false, "Kdo jsi?");
			}
			d.Page("Prohlednout");
			d.SetText(gondor ? "<def_questDialog_colorTextEmote>*Zmocnenec Gondoru vyhlizi dobrackym dojmem, ochotny kazdemu pomoct. Pusobi dojmem zdatneho a spravedliveho velitele.*"
				: "<def_questDialog_colorTextEmote>*Zmocnenec Mordoru vyhlizi az demonickym dojmem, skoro bilevlasy kontrastuji s jeho cernym odevem. Tvar prisne stazena do zdvorilostniho usmevu. Planouci oci ve sluzbach Mordoru...*");
			d.Html();
			d.Page("Nejake drby ?");
			d.SetText("Inu je az s podivem kolik novych zajemcu o me ukoly se zde zastavuje, vsak je mam taky vsechny hezky zapsane. Ale ani ne pulka dokaze moje ukoly splnit. To vis neni to zadnej med. Asi je laka vidina novych zkusenosti, ktere behem plneni nabydou...");
			d.Html();
			d.Page("Hledam praci");
			if (CanGetWork(stage)) { //28 is even: Erebor's "no more work" branch after it was never reached
				d.SetText("Hmm  nevim, co by ti sedlo nejlip..  muzes zkusit splnit par ukolu pro me a az nabudes vice zkusenosti, muzes se odvazit vypravit za dobrodruzstvim nekam dal... ");
				d.Html();
				d.SetText("Dobra, <?src.sex(chtel,chtela)?> bych zkusit splnit nejaky z tvych ukolu...");
				d.Response(false, false, "work_info");
				d.SetText("Hm .. jeste se po necem porozhlednu. Tve ukoly plnit nechci.");
				d.Response(false, true, "refuse");
			} else {
				d.SetText("Ja jsem ti uz praci dal! Tak ji spln a potom ti dam treba neco dalsiho. ");
				d.Html();
			}
			d.Page("Nesu ti");
			d.SetText("Hmm?");
			d.Html();
			if (!CanGetWork(stage)) {
				if (Collectors.IsFull(player, BagOf(stage, gondor))) {
					d.SetText("Uz jsem <?src.sex(splnil,splnila)?> tvuj ukol... tady to mas !");
					d.Response(false, true, "work_giveCollector");
				}
				d.SetText("Vlastne nic...");
				d.Response(false, true, "work_notFoundYet");
			}
			if (CanGetWork(stage)) {
				d.Page("work_info");
				if (stage == Done) {
					d.SetText("Hmm! Dobra... ale nejdriv mi tady do vacku prines " + RepeatHearts(player) + " dracich srdc!");
				} else {
					d.SetText((gondor ? gondorWork : mordorWork)[HuntOf(stage)]);
				}
				d.Html();
				d.SetText("Jdu na to...");
				d.Response(false, true, "work_accept");
				d.SetText("Tak to teda ne ...");
				d.Response(false, true, "refuse");
			}
		}

		/// <summary>d_gq030_zmocnenec button / d_mq030_zmocnenec button. True when the button was q30's.</summary>
		internal static bool Button(string button, NPC npc, Player player, bool gondor) {
			if (Refusal(player, gondor) != null) {
				return true;
			}
			int stage = GetStage(player, gondor);
			if (CanGetWork(stage)) {
				if (button == "refuse") {
					npc.Say("Aha...");
					return true;
				}
				if (button == "work_accept") {
					GetWork(npc, player, gondor, stage);
					return true;
				}
			} else if (button == "work_notFoundYet") {
				npc.Say("Tak se snaz, abys to " + ((player.Gender == Gender.Female) ? "splnila" : "splnil") + "...");
				return true;
			} else if (button == "work_giveCollector") {
				HandOver(npc, player, gondor, stage);
				return true;
			}
			return false;
		}

		/// <summary>f_q030_getWork: the bag of the next hunt, if the hero's level is up to it.</summary>
		internal static void GetWork(NPC npc, Player player, bool gondor, int stage) {
			int hunt = HuntOf(stage);
			if (player.Level < Levels[hunt]) {
				npc.Say("Na tento ukol jsi jeste mlady mlicnak. Prijd, az ziskas trochu vice zkusenosti!");
				return;
			}
			player.SysMessage("Prijmas ukol od Zmocnence " + (gondor ? "Gondoru" : "Mordoru") + ".");
			Collector old = Collectors.FindIn(player, BagOf(stage, gondor));
			if (old != null) {
				old.Delete();
			}
			Item bag = Q006.GiveBag(player, BagOf(stage, gondor));
			if ((stage == Done) || ((hunt == Levels.Length - 1) && (Finished(player) >= 1))) {
				bag.SetTag(Collector.MaxTK, RepeatHearts(player));
			}
			SetStage(player, gondor, (stage == Done) ? Done - 1 : stage + 1);
		}

		/// <summary>f_q030_handoverCollector: the full bag for the hunt's experience (half for a realm changer still below where they got
		/// on the other ladder), then the next stage; the last one also gives the cloak.</summary>
		internal static void HandOver(NPC npc, Player player, bool gondor, int stage) {
			Collector bag = Collectors.FindIn(player, BagOf(stage, gondor));
			if ((bag == null) || !bag.IsFull) {
				npc.Say("Ale vzdyt ty to jeste nemas!");
				return;
			}
			int next = stage + 1;
			int experience = Experience[HuntOf(stage)];
			bool last = next == Done;
			if (last && (Finished(player) >= 1)) {
				experience = 0; //bez expov
			}
			player.SysMessage("Uspesne jsi " + ((player.Gender == Gender.Female) ? "splnila" : "splnil") + " svuj ukol. Az se na to budes citit, muzes prijit pro dalsi.");
			if ((player.GetTag(SwitchedRealmTK) != null) && (Presel(player) > next)) {
				player.SysMessage("Nez si ziskas moji duveru bude tva odmena polovicni");
				experience /= 2;
			}
			if (experience > 0) {
				LevelingSystem.AddExperience(player, experience);
			}
			bag.Delete();
			SetStage(player, gondor, next);
			if (last) {
				SetFinished(player, Math.Min(Finished(player) + 1, 2));
				GiveCloak(player, gondor);
			}
		}

		/// <summary>f_q030_final_reward.</summary>
		public static Item GiveCloak(Player player, bool gondor) {
			Item cloak = (Item) ThingDef.GetByDefname(CloakDef).Create(player.Backpack);
			MagicItems.SetValue(cloak, 70, RegenValue);
			MagicItems.SetValue(cloak, 102, DragonValue);
			MagicItems.Finish(cloak);
			cloak.Name = "Plast bojovnika";
			cloak.Color = gondor ? GondorCloakColor : MordorCloakColor;
			cloak.SetTag(CloakOwnerTK, player);
			CraftingSkillDef.SetCrafter(cloak, player); //Erebor's craftedby: no magic-item exchange takes it
			return cloak;
		}

		/// <summary>i_plast_bojovnika's @equip: its owner's only.</summary>
		internal static bool RefusesEquip(Item item, Character ch) {
			if ((item.Def != ThingDef.GetByDefname(CloakDef)) || !(ch is Player player) || player.IsGM) {
				return false;
			}
			if (item.GetTag(CloakOwnerTK) != player) {
				player.SysMessage("Nemuzes nosit tenhle plast");
				return true;
			}
			return false;
		}
		#endregion the envoy

		#region the journal
		private static readonly string[] gondorTargets = { "20 zubu velkych potkanu", "9 kosti kostlivcu sekerniku", "8 oci lichu", "10 drapu infikovanych krys",
			"7 vajicek cernych vdov", "7 nehtu titanu", "6 nehtu panu titanu", "10 pirek jedoveho fenixe", "5 vlasu strazcu ohne", "8 prstu gladiator lorda",
			"3 srdce evila", "3 vetvicky z tela excelenta", "5 usi obru z jeskyne" };
		private static readonly string[] mordorTargets = { "20 zubu litych vlku", "7 prstu trollu", "15 klepet velkych skorpionu", "10 klepet kralu skorpionu",
			"5 prstu kralu lichu", "7 oci kyklopu", "12 nehtu kralu kyklopu", "10 pirek jedoveho fenixe", "5 kuzi z minotaura", "8 prstu gladiator lorda",
			"4 srdce prastarych pavouku", "3 vetvicky z tela Excelenta", "4 usi obru z jeskyne" };

		/// <summary>d_questy_zmocnenecg; Mordor's d_questy_zmocnenecm on the same pattern (Erebor's had its last two stages swapped).</summary>
		internal static string JournalText(Player player, bool gondor) {
			int stage = GetStage(player, gondor);
			if (stage == 0) {
				return null;
			}
			if (stage == Done) {
				return gondor ? "<?src.sex(Splnil,Splnila)?> jsi uspesne vsechny zmocnencovy zkousky a <?src.sex(stal jsi se uznavanym hrdinou Gondoru,stala jsi se uznavanou hrdinkou Gondoru)?>."
					: "<?src.sex(Splnil,Splnila)?> jsi uspesne vsechny zmocnencovy zkousky a <?src.sex(stal jsi se uznavanym hrdinou Mordoru,stala jsi se uznavanou hrdinkou Mordoru)?>.";
			}
			if (stage == Done - 1) {
				int hearts = (Finished(player) >= 1) ? RepeatHearts(player) : 4;
				return "Zmocnenec po tobe chce " + hearts + " draci srdce. Jen by te zajimalo, kde dnes potkas draka ... Tento ukol je snad naprosto neresitelny.";
			}
			if (!CanGetWork(stage)) {
				return "Zmocnenec po tobe chce " + (gondor ? gondorTargets : mordorTargets)[HuntOf(stage)] + ".";
			}
			return "Plnis zmocnencovy ukoly a postupujes v armadni hierarichii stale vyse. Zatim je stale kam stoupat. Az budes <?src.sex(pripraven,pripravena)?> zajdi za Zmocnencem a pozadej ho o dalsi ukol.";
		}
		#endregion the journal
	}
}
