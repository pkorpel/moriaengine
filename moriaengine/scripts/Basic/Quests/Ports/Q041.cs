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
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1278 - Erebor's q041 "Paserak" (questy/prestupak.scp, prestupak_chars.scp, zmodrani.scp): the smuggler takes a hero of
	/// level 15 to the other realm. 10 000 gold for a word with him, then proof: a Gondor hero kills the innocent elf by Rivendell, a
	/// Mordor hero brings farmer Borek by Nurn 40 cooked fish steaks and 5 refresh potions. Then the price: 10 iron ingots, a magic
	/// bag for each realm change so far plus one (at most 10), a roast pig and level x 10 000 + 1 000 000 gold. His portal takes the
	/// hero to the realm gate, which changes the realm, sends them home and counts the change (Erebor's tag.presel, which q30 reads).
	/// Outcasts may never change realm. Repeatable.
	/// <para>Erebor's char tag q041_paserak: 1 promised the money, 2 paid it, 3 wants to change, 4 promised to kill the elf, 5 killed
	/// him, 6 promised to help Borek, 7 helped him, 8 promised the gifts, 9 brought them, 10 changed realm.</para>
	/// </summary>
	public static class Q041 {
		public const string PaserakDef = "c_q041_prestupak_paserak", ElfDef = "c_q041_prestupak_elf", BorekDef = "c_mq002_borek";
		public const string PortalDef = "i_port_prechod_2", GateDef = "i_level_gate";
		public const int MinLevel = 15, Done = 10, Bribe = 10000, MaxBags = 10;
		public const int Ingots = 10, FishSteaks = 40, RefreshPotions = 5;
		public const string IngotDef = "i_ingot_iron", BagDef = "i_mi_bag", PigDef = "i_pig_roast", FishDef = "i_fish_cut_cooked", RefreshDef = "i_potion_refresh";
		public const short MordorKarma = -3000;
		public const int GateQuietSeconds = 60;

		/// <summary>Where the portal throws those it doesn't take, on the smuggler's island (Ostrov nestalosti).</summary>
		public static readonly Point2D RefusedSpot = new Point2D(1142, 3897);

		private static readonly TagKey stageTK = TagKey.Acquire("_q041Paserak_");
		internal static readonly TagKey PassedGateTK = TagKey.Acquire("_q041PassedGate_");

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Paserak", 0x3093, GetJournalText, p => GetStage(p) == Done));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(PaserakDef, SingletonScript<E_Q041Paserak>.Instance);
			QuestNpc.Attach(BorekDef, SingletonScript<E_Q041Borek>.Instance);
			QuestNpc.Attach(PortalDef, SingletonScript<E_Q041Portal>.Instance);
			QuestNpc.Attach(GateDef, SingletonScript<E_Q041Gate>.Instance);
		}

		#region tags
		public static int GetStage(Player player) {
			object tag = player.GetTag(stageTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		internal static void SetStage(Player player, int stage) {
			if (stage == 0) {
				player.RemoveTag(stageTK);
			} else {
				player.SetTag(stageTK, stage);
			}
		}

		internal static void Advance(Player player, int stage) {
			SetStage(player, stage);
			QuestJournal.Report(player, Journal);
		}

		/// <summary>Erebor's tag.presel: how many times the hero changed realm.</summary>
		public static int Changes(Player player) {
			return Collectors.ToInt(player.GetTag(Q30.SwitchedRealmTK));
		}

		/// <summary>Erebor's DonesItemu: a magic bag for each change so far plus one, at most 10.</summary>
		public static int BagsWanted(Player player) {
			return Math.Min(Changes(player) + 1, MaxBags);
		}

		/// <summary>The bank transfer: level x 10 000 + 1 000 000.</summary>
		public static long Price(Player player) {
			return ((long) player.Level * 10000) + 1000000;
		}

		internal static bool IsOutcast(Player player) {
			return (player.Realm == Realm.GondorOutcast) || (player.Realm == Realm.MordorOutcast);
		}
		#endregion tags

		#region journal
		internal static string GetJournalText(Player player) {
			switch (GetStage(player)) {
				case 1:
					return "Dokud neprineses paserakovi deset tisic zlatych, nebude se s tebou bavit.";
				case 2:
					return "<?src.sex(Donesl,Donesla)?> jsi paserakovi deset tisic zlataku. Ted uz snad bude ochotnejsi.";
				case 3:
					return "<?src.sex(Rozhodl,Rozhodla)?> jsi se prestoupit k druhe strane. Zeptej se paseraka, co to bude obnaset.";
				case 4:
					return "Paserak ti dal za ukol najit a zabit nevinneho elfa, ktery se pohybuje pobliz Roklinky. Pry si mam poradne vychutnat vyhasinajici zivot v jeho ocich...";
				case 5:
					return "<?src.sex(Zabil,Zabila)?> jsi nevinneho elfa. Vrat se za paserakem a sdel mu, ze jsi svuj ukol <?src.sex(splnil,splnila)?>.";
				case 6:
					return "Paserak ti dal za ukol prinest farmari Borkovi do Nurnu ctyricet rybich stejku a pet beznych lektvaru osvezeni.";
				case 7:
					return "Pomoci farmari Borkovi jsi <?src.sex(prokazal,prokazala)?> dobrou vuli. Vrat se za paserakem a sdel mu, ze jsi svuj ukol <?src.sex(splnil,splnila)?>.";
				case 8:
					return "Prines paserakovi deset zeleznych ingotu, " + BagsWanted(player) + "x zeleny pytlik s magickym predmetem a jedno pecene prase. V bance bys <?src.sex(mel,mela)?> mit pripraveno minimalne " + Price(player).ToString(CultureInfo.InvariantCulture) + " zlataku.";
				case 9:
					return "<?src.sex(Splnil,Splnila)?> jsi vsechny paserakovy pozadavky. Vstupem do jeho magickeho portalu prejdes ke druhe strane.";
				case Done:
					return "<?src.sex(Presel,Presla)?> jsi ke druhe strane.";
				default:
					return null;
			}
		}
		#endregion journal

		#region talking
		/// <summary>f_q041_paserak.</summary>
		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			string refusal = Refusal(player);
			if (refusal != null) {
				npc.Say(refusal);
			} else {
				npc.Dialog(player, SingletonScript<D_Q041Paserak>.Instance, npc);
			}
		}

		/// <summary>What the smuggler says instead of talking, or null.</summary>
		internal static string Refusal(Player player) {
			if ((player.Level < MinLevel) || IsOutcast(player)) {
				return "S tebou se nemam zatim o cem bavit. Prijd, az budes silnejsi!";
			}
			if ((player.Realm != Realm.Gondor) && (player.Realm != Realm.Mordor)) {
				return "S tebou se ani ja bavit nebudu!";
			}
			return null;
		}

		/// <summary>f_q041_prestup_has_items: the gifts in the pack, the transfer payable.</summary>
		internal static bool HasGifts(Player player) {
			return GoldUtils.CanPay(player, Price(player), PaySource.Bank) && (Q006.Count(player, IngotDef) >= Ingots)
				&& (Q006.Count(player, BagDef) >= BagsWanted(player)) && (Q006.Count(player, PigDef) >= 1);
		}

		/// <summary>f_q041_prestup_consume_items: the transfer from the bank only, as Erebor's pay_from_bank (through GoldUtils.Pay).</summary>
		internal static bool TakeGifts(Player player) {
			int bags = BagsWanted(player);
			if (!HasGifts(player) || !GoldUtils.Pay(player, Price(player), PaySource.Bank)) {
				return false;
			}
			Q006.Consume(player, IngotDef, Ingots);
			Q006.Consume(player, BagDef, bags);
			Q006.Consume(player, PigDef, 1);
			return true;
		}

		/// <summary>f_mq002_Borek_DClick: a Gondor hero is sent away.</summary>
		internal static void TalkToBorek(NPC borek, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			if (player.Realm == Realm.Gondor) {
				borek.Say("Zmiz, gondorsky pse!");
			} else {
				borek.Dialog(player, SingletonScript<D_Q041Borek>.Instance, borek);
			}
		}

		/// <summary>f_q041_prestup_has_items_for_borek.</summary>
		internal static bool HasBoreksGifts(Player player) {
			return (Q006.Count(player, FishDef) >= FishSteaks) && (Q006.Count(player, RefreshDef) >= RefreshPotions);
		}
		#endregion talking

		#region the elf, the portal and the gate
		/// <summary>The elf's @death: act, the killer at stage 4, goes to 5. A pet's kill counts for its master.</summary>
		internal static void OnKillCredit(Character killer, Character victim) {
			if (victim.Def != ThingDef.GetByDefname(ElfDef)) {
				return;
			}
			Player player = (killer as Player) ?? (killer?.ControlMaster as Player);
			if ((player != null) && (GetStage(player) == 4)) {
				Advance(player, 5);
				player.RedMessage("Skvely pocit, dobre mu tak!");
			}
		}

		/// <summary>i_port_prechod_2: below level 15, or not at stage 9, back to the island's shore; else on to the gate (MOREP).</summary>
		internal static void StepOnPortal(Item portal, Player player) {
			if (player.IsGM || ((player.Level >= MinLevel) && (GetStage(player) == 9))) {
				if (portal.MoreP != null) {
					player.Go(portal.MoreP);
				}
				return;
			}
			player.SysMessage("Svatyne te neprijala.");
			Map map = Map.GetMap(portal.M);
			player.Go(RefusedSpot.X, RefusedSpot.Y, (sbyte) WorldSpots.StandZ(map, RefusedSpot.X, RefusedSpot.Y), portal.M);
		}

		/// <summary>i_level_gate's @step: an outcast never changes, anyone else only at stage 9. A minute's quiet after a change
		/// (i_prave_prosel_levelgatou).</summary>
		internal static void StepOnGate(Item gate, Player player) {
			if (player.IsGM || HasJustPassed(player)) {
				return;
			}
			if (IsOutcast(player)) {
				player.RedMessage("Tve prohresky se staly prilis velkou hrozbou pro vsechny v tvem okoli.");
				player.RedMessage("Dalsi sanci nedostanes!");
				GateEffect(player);
				return;
			}
			if (GetStage(player) != 9) {
				player.RedMessage("Nejprve musis promluvit s paserakem.");
				return;
			}
			ChangeRealm(player);
		}

		private static bool HasJustPassed(Player player) {
			object at = player.GetTag(PassedGateTK);
			return (at is double seconds) && (Globals.TimeInSeconds - seconds < GateQuietSeconds);
		}

		/// <summary>#1307 - everything the player held in the realm they leave: its office (the General, the first Nazgul), a Nazgul's
		/// ring, a mayoralty, citizenship and candidacy, the realm uniform, a keep's captaincy.</summary>
		internal static void LeaveRealmPosts(Player player, Realm old) {
			RealmOffice office = RealmOffice.For(old);
			if ((office != null) && (office.holder == player)) {
				office.Dismiss();
			}
			PowerBadge ring = NazgulRings.RingOf(player);
			if (ring != null) {
				NazgulRings.TakeAway(ring, player);
			}
			foreach (Town town in new List<Town>(Town.All)) {
				if (town.mayor == player) {
					town.ClearMayor();
				}
				town.RemoveCitizen(player);
				town.WithdrawCandidate(player);
			}
			RealmUniform.Remove(player);
			KeepCaptains.DismissFrom(player);
			player.SysMessage("Vse, co jsi drzel ve sve stare risi - urad, prsten, obcanstvi, uniforma i velitelstvi - zustava za tebou.");
		}

		/// <summary>The gate's change: Mordor to Gondor clears the murders and karma, Gondor to Mordor takes Erebor's -3000 karma
		/// (its 30 000 kills marked Mordor's side, which the realm alone does here); the realm's capital is the new home.</summary>
		internal static void ChangeRealm(Player player) {
			bool male = player.Gender != Gender.Female;
			if ((player.Realm == Realm.Mordor) || (player.Realm == Realm.Gondor)) {
				LeaveRealmPosts(player, player.Realm); //#1307 - the user's call, against Erebor: nothing of the old realm goes along
			}
			if (player.Realm == Realm.Mordor) {
				player.Realm = Realm.Gondor;
				Notoriety.SetMurderCount(player, 0);
				player.Karma = 0;
				player.Home = NationData.GetHome(Nation.Gondoran);
				player.RedMessage("Citis se byt " + (male ? "ocisten" : "ocistena") + " od svych byvalych hrichu. Gondor te nyni prijme.");
			} else if (player.Realm == Realm.Gondor) {
				player.Realm = Realm.Mordor;
				player.Karma = MordorKarma;
				player.Home = NationData.GetHome(Nation.Mordoran);
				player.RedMessage("Zlo tebou prostupuje, strez se hnevu Gondorskych - nyni patris Temnemu Panu!");
			} else {
				return;
			}
			player.Sound(0x20F); //snd_spell_polymorph
			player.SetTag(PassedGateTK, Globals.TimeInSeconds);
			player.SetTag(Q30.SwitchedRealmTK, Changes(player) + 1);
			Advance(player, Done);
			player.Go(player.Home);
			GateEffect(player);
			string realm = (player.Realm == Realm.Mordor) ? "Mordoru" : "Gondoru";
			Logger.WriteInfo(true, "Q041: " + player.Name + " (" + player.Account?.Name + ") level " + player.Level + " dokoncil prestupni quest do "
				+ realm + " (celkem " + Changes(player) + "x) a byl prenesen do " + player.Region?.Name + " (" + player.P() + ")");
		}

		/// <summary>level_gate_efekt: fire around the hero.</summary>
		private static void GateEffect(Player player) {
			EffectFactory.StationaryEffect(player, 0x3709, 10, 30);
		}
		#endregion the elf, the portal and the gate
	}

	/// <summary>#1278 - Paserak, Erebor's d_q041_paserak.</summary>
	public class D_Q041Paserak : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3164;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q041.GetStage(player);
			bool free = (s == 0) || (s == Q041.Done);
			bool gondor = player.Realm == Realm.Gondor;
			if (free) {
				d.BaseButton(1);
				d.BaseButton(2, 21, "Muzu pomoci?");
			} else if (s >= 2) {
				d.BaseButton(1, 45, "Prechod?");
			}
			d.BaseButton(3);
			d.BaseButton(4);
			d.BaseButton(5);
			d.BaseButton(6);

			d.Page("Pozdrav");
			d.SetText("<def_questDialog_colorTextEmote>*Vidis mladeho muze s napadne neklidnym pohledem.*");
			d.Html();
			if (free) {
				d.Page("Kdo jsi?");
				d.SetText("Do toho ti nic neni, ja te tu taky nevyslycham. A pro sluzby, ktere poskytuji, to ostatne ani neni podstatne...");
				d.Html();
			} else if (s >= 2) {
				d.Page("Prechod?");
				this.Crossing(d, player, s, gondor);
			}
			if (free) {
				d.Page("Muzu pomoci?");
				d.SetText("Jasne, vazne by se mi hodilo deset tisic zlataku. Mozna bychom pak nasli spolecnou rec.");
				d.Html();
				d.SetText("Dobra, tak ja ti je prinesu.");
				d.Response(false, true, "q041_bring_money");
			}
			d.Page("Obchodovat");
			d.SetText("Tesi me, ze asi vypadam jako sporadany obchodnik. Moje podnikani se ale venuje spise preprave veci a osob.");
			d.Html();
			d.Page("Hledam praci");
			d.SetText("Tady zadnou praci nenajdes...");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Copak mi neses?");
			d.Html();
			d.SetText("Vlastne nic, promin.");
			d.Response(false, true, "goodbye");
			if ((s == 1) && GoldUtils.CanPay(player, Q041.Bribe)) {
				d.SetText("Mam tu pro tebe ty penize!");
				d.Response(false, true, "q041_give_money");
			}
			d.Page("Prohlednout");
			d.SetText("<def_questDialog_colorTextEmote>*Muz je po zuby ozbrojeny a velmi ostrazity. Je ti jasne, ze neni dobry napad ho rozcilit.*");
			d.Html();
		}

		private void Crossing(QuestDialogContent d, Player player, int s, bool gondor) {
			if (s == 2) {
				d.SetText(gondor
					? "Takze tobe je v Gondoru horko a <src.sex(\"rad\",\"rada\")> bys do zeme neomezenych moznosti? Myslis, ze jsi dost <src.sex(\"silny\",\"silna\")>, abys v Mordoru <src.sex(\"prezil\",\"prezila\")>? Uvedom si taky, ze cesta zpatky muze byt narocnejsi, nez se na prvni pohled zda..."
					: "Dalsi <src.sex(\"chudacek\",\"chudinka\")>, co nevydrzi Marghulovu vladu? Zachtelo se ti radu a poradku? Myslis, ze jsi <src.sex(\"schopny\",\"schopna\")> akceptovat pravidla Gondoru? Uvedom si taky, ze cesta zpatky muze byt narocnejsi, nez se na prvni pohled zda...");
				d.Html();
				d.SetText("Ano rozhodne chci prejit, jsem si vseho <src.sex(\"vedom\",\"vedoma\")> a na rizika nehledim!");
				d.Response(false, true, "q041_want_to");
				d.SetText("Jeste si to radsi rozmyslim.");
				d.Response(false, true, "q041_think_about");
			} else if ((s == 3) && gondor) {
				d.SetText("Dobra, ale nez do tebe budu investovat cas a penize, potrebuji si byt jisty, ze to myslis vazne. Kousek od Roklinky se potuluje elf. Nikomu nic neudelal, vsichni ho maji radi. Ma manzelku a jedno milujici dite. <br>Zabij ho.");
				d.Html();
				d.SetText("Nesnasim elfy! To, ze je elf, mi jako zaminka bohate staci.");
				d.Response(false, true, "q041_will_kill");
				d.SetText("Nezabiji nevinneho!");
				d.Response(false, true, "q041_wont_kill");
			} else if (s == 3) {
				d.SetText("Dobra, ale nez do tebe budu investovat cas a penize, potrebuji si byt jisty, ze to myslis vazne. Kousek od Nurnu na poli tezce pracuje farmar Borek. Dones mu ctyricet opecenych rybich stejku a pet obycejnych lektvaru na osvezeni.");
				d.Html();
				d.SetText("Dobre, obdaruji jej!");
				d.Response(false, true, "q041_give_him");
				d.SetText("<src.sex(\"Sam\",\"Sama\")> mam malo, nikomu nic nedam. A uz vubec ne zadarmo!");
				d.Response(false, true, "q041_wont_give");
			} else if ((s == 4) || (s == 5)) {
				d.SetText("Jeste porad zije?");
				d.Html();
				if (s == 5) {
					d.SetText("Uz je mrtvej...");
					d.Response(false, true, "q041_killed_elf");
				}
			} else if ((s == 6) || (s == 7)) {
				d.SetText("<src.sex(\"Udelal\",\"Udelala\")> jsi to uz?");
				d.Html();
				if (s == 7) {
					d.SetText("Ano, <src.sex(\"dal\",\"dala\")> jsem mu to.");
					d.Response(false, true, "q041_gave_him");
				}
			} else if (s == 8) {
				d.SetText("Budu ti muset zajistit celou novou identitu, coz samozrejme neco stoji. Pro zacatek mi prines deset zeleznych ingotu, "
					+ Q041.BagsWanted(player) + "x zeleny pytlik s magickym predmetem a jedno pecene prase. A pocitej s tim, ze mi tu jeste podepises lejstro na presun decentni sumicky mezi nasimi bankovnimi ucty. Konkretne to bude krasnych "
					+ Q041.Price(player).ToString(CultureInfo.InvariantCulture) + " zlataku.");
				d.Html();
				if (Q041.HasGifts(player)) {
					d.SetText("Zde mas ty veci... a tady autogram, uf.");
					d.Response(false, true, "q041_have_items");
				} else {
					d.SetText("Dobra, dam to dohromady.");
					d.Response(false, true, "q041_dont_have_yet");
				}
			} else if (s == 9) {
				d.SetText("Vsecko je zarizeny, ted uz ti staci jenom projit tamhle tim portalem...");
				d.Html();
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if (Q041.Refusal(player) != null) {
				return;
			}
			int s = Q041.GetStage(player);
			bool gondor = player.Realm == Realm.Gondor;
			switch (button) {
				case "q041_bring_money":
					if ((s == 0) || (s == Q041.Done)) {
						npc.Say("Tak ale honem!");
						Q041.Advance(player, 1);
					}
					break;
				case "q041_give_money":
					if (s == 1) {
						if (GoldUtils.Pay(player, Q041.Bribe, PaySource.Usual, "")) {
							npc.Say("Ooo, to je od tebe ale mile!");
							Q041.Advance(player, 2);
						} else {
							npc.Say("Pockat, tady mi neco nesedi...");
						}
					}
					break;
				case "q041_want_to":
					if (s == 2) {
						npc.Say("Dobra, tak se do toho pustime...");
						Q041.Advance(player, 3);
					}
					break;
				case "q041_think_about":
					npc.Say("Je to na tobe...");
					break;
				case "q041_will_kill":
					if ((s == 3) && gondor) {
						npc.Say("Vyborne. A at ti pri umirani hezky vidi do oci!");
						Q041.Advance(player, 4);
					}
					break;
				case "q041_wont_kill":
				case "q041_wont_give":
					npc.Say("Jak chces. Kdyby sis to " + ((player.Gender == Gender.Female) ? "rozmyslela" : "rozmyslel") + ", vis, kde me najdes!");
					break;
				case "q041_give_him":
					if ((s == 3) && !gondor) {
						npc.Say("To jsem chtel slyset. Az to provedes, prijd za mnou!");
						Q041.Advance(player, 6);
					}
					break;
				case "q041_killed_elf":
				case "q041_gave_him":
					if (s == ((button == "q041_killed_elf") ? 5 : 7)) {
						npc.Say("Tak se mi to libi!");
						Q041.Advance(player, 8);
					}
					break;
				case "q041_have_items":
					if (s == 8) {
						if (Q041.TakeGifts(player)) {
							npc.Say("Dobre, vse by melo byt zarizene. Ted uz ti staci jenom projit tamhle tim portalem...");
							Q041.Advance(player, 9);
						} else {
							npc.Say("Pockat, tady mi neco nesedi...");
						}
					}
					break;
				case "q041_dont_have_yet":
					npc.Say("Tak pak prijd...");
					break;
				case "goodbye":
					npc.Say("Mej se...");
					break;
			}
		}
	}

	/// <summary>#1278 - farmer Borek, Erebor's d_mq002_borek as a hero who hasn't begun mq002 sees it (mq002 isn't ported), with q041's
	/// bargain on his greeting (f_q041_paserak_borek_bargain/answer/Button).</summary>
	public class D_Q041Borek : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3118;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			d.BaseButton(1);
			d.BaseButton(3);
			d.BaseButton(4);
			d.BaseButton(5);
			d.BaseButton(6);
			d.Page("Pozdrav");
			d.SetText("Vitej...");
			d.Html();
			bool bargain = Q041.GetStage(player) == 6;
			if (bargain) {
				d.SetText("Posila me za tebou nas spolecny pritel. Pry by se ti hodila trocha hmotne vypomoci...");
				d.Response(false, false, "q041_paserak_send_him");
			}
			d.Page("Kdo jsi?");
			d.SetText("Jmenuju se Borek a jsem zemedelec. Mam tady male policko a snazim se nejak uzivit. Nekdy je to ale tezke... sakra tezke.");
			d.Html();
			d.Page("Obchodovat");
			d.SetText("Nemam, co bych ti nabidl. Vse, co sklidim, hned prodavam na trhu.");
			d.Html();
			d.Page("Hledam praci");
			d.SetText("Mno, ja ti praci nenabidnu. Jsem rad, ze uzivim sam sebe...");
			d.Html();
			d.Page("Nesu ti");
			d.SetText("Copak pro me mas?");
			d.Html();
			d.Page("Prohlednout");
			d.SetText("Borek je mensiho vzrustu, podsadity, ale svalnaty. Tvar ma strhanou od tezke prace a ruce plne mozolu. V jeho ocich presto nachazis jiskricku uprimne radosti ze zivota.");
			d.Html();
			if (bargain) {
				d.Page("q041_paserak_send_him");
				d.SetText("No vis, doba je ted tezka... kazdemu by se hodila naka ta pomoc. Co pro me mas?");
				d.Html();
				if (Q041.HasBoreksGifts(player)) {
					d.SetText("Mam pro tebe neco k zakousnuti a par lektvaru na osvezeni!");
					d.Response(false, true, "q041_has_items");
				} else {
					d.SetText("Vlastne nic...");
					d.Response(false, true, "q041_doesnt_have_items");
				}
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			if (Q041.GetStage(player) != 6) {
				return;
			}
			if (button == "q041_doesnt_have_items") {
				npc.Say("Tak holt treba priste...");
			} else if (button == "q041_has_items") {
				if (Q041.HasBoreksGifts(player)) {
					Q006.Consume(player, Q041.FishDef, Q041.FishSteaks);
					Q006.Consume(player, Q041.RefreshDef, Q041.RefreshPotions);
					npc.Say("Tyjo, mockrat diky!");
					Q041.Advance(player, 7);
				} else {
					npc.Say("Aha. A kde?");
				}
			}
		}
	}

	/// <summary>#1278 - the smuggler's double click.</summary>
	public class E_Q041Paserak : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q041.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1278 - Borek's double click (f_mq002_Borek_DClick).</summary>
	public class E_Q041Borek : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q041.TalkToBorek(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1278 - i_port_prechod_2.</summary>
	public class E_Q041Portal : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			if (stepping is Player player) {
				Q041.StepOnPortal(self, player);
			}
		}
	}

	/// <summary>#1278 - i_level_gate.</summary>
	public class E_Q041Gate : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			if (stepping is Player player) {
				Q041.StepOnGate(self, player);
			}
		}
	}

	/// <summary>#1278 - spk_q041_paserak.</summary>
	public sealed class Q041Speech : AbstractSpeech {
		private static readonly string[] greetings = { "ukol", "prace", "zdravim", "ahoj", "hi" };

		public new static void Bootstrap() {
			new Q041Speech("spk_q041_paserak").Register();
		}

		private Q041Speech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, greetings) && QuestNpc.IsCloseEnough(npc, player)) {
				Q041.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}

	/// <summary>#1278 - spk_mq002_borek, his greetings opening his dialog.</summary>
	public sealed class Q041BorekSpeech : AbstractSpeech {
		private static readonly string[] greetings = { "ahoj", "radlice", "pluh", "ukol", "prace", "den" };

		public new static void Bootstrap() {
			new Q041BorekSpeech("spk_mq002_borek").Register();
		}

		private Q041BorekSpeech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, greetings) && QuestNpc.IsCloseEnough(npc, player)) {
				Q041.TalkToBorek(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
