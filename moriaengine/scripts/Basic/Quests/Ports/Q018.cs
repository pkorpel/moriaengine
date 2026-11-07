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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #1270 - Erebor's q018 "Zlaty losos" (questy/q018_zlaty_losos.scp): an old fisherman wants a golden thread for a fly the river's
	/// giant salmon can't resist. Mirrana knows where the magic spinning wheel is, for a salve against her festering bite; the salve-maker
	/// Elnassir makes it from 20 spider silk, 80 nightshade, 150 bones and 10,000 gold. Her golden rune in Osgiliath's ruins takes the hero
	/// to the wheel in the Pits of Death; touching it spins the thread (+3.0 Tailoring) and sends them back. The fisherman pays 20,000 gold.
	/// <para>Erebor's qtag q018: 1 sent to Mirrana, 2 sent to Elnassir, 3 bring the ingredients, 4 the rune works, 5 the thread, 6 done.
	/// The salve and the thread are quest items, player tags (#1090's rule). Erebor's wheel had no stage check: anyone who got there
	/// could spin, again and again, +3.0 Tailoring a touch; here only at stage 4, once.</para>
	/// </summary>
	public static class Q018 {
		public const string FishermanDef = "c_q018_rybar", MirranaDef = "c_q018_mirrana", ElnassirDef = "c_q018_elnassir";
		public const string RuneDef = "i_q018_portujici_runa", WheelDef = "i_q018_zlate_kolo";
		public const string Salve = "i_q018_mirranin_odvar", Thread = "i_q018_zlata_snura";
		public const int Done = 6;
		public const int SalveGold = 10000, RewardGold = 20000;
		public const int TailoringGain = 30; //tenths, Erebor's +3.0 on the real value
		public static readonly Point4D RuneSpot = new Point4D(6008, 3860, -80, 0); //where the wheel sends the hero back to
		public static readonly Point4D WheelSpot = new Point4D(5390, 35, 0, 0);

		private static readonly TagKey stageTK = TagKey.Acquire("_q018ZlatyLosos_");

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Zlaty losos", 0x3122, GetJournalText, p => GetStage(p) == Done));
			QuestItems.Register(Salve, "odvar proti zanetum");
			QuestItems.Register(Thread, "zlata nit");
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(FishermanDef, SingletonScript<E_Q018Npc>.Instance);
			QuestNpc.Attach(MirranaDef, SingletonScript<E_Q018Npc>.Instance);
			QuestNpc.Attach(ElnassirDef, SingletonScript<E_Q018Npc>.Instance);
			QuestNpc.Attach(RuneDef, SingletonScript<E_Q018Rune>.Instance);
			QuestNpc.Attach(WheelDef, SingletonScript<E_Q018Wheel>.Instance);
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

		/// <summary>Erebor's d_questy_zlaty_losos texts, stage 1 to 6.</summary>
		internal static readonly string[] JournalTexts = {
			"Rybar podivin po tobe chce zlatou nit! Mas zajit za Mirranou kterou najdes cestou od mostu mezi Minasy smerem k poustnimu mestu Rhunu.",
			"Mirrana te poslala za Elnassirem mastickarem na Ebenove ostrovy pro mast na jeji nohu, pak ti prozradi kde najdes hledane kolo.",
			"Elnassir za lektvar chce 20 pavucin, 80 nightshade, 150 kosti a 10 tisic zlataku.",
			"Mirrana ti prozradila, ze na mordorske strane Osgiliathu naleznes schovanou runu. Kdyz ji zatlacis do zdi, kouzlo timto vyvolane te prenese az na misto, kde by melo byt zlate kolo schovane.",
			"Zlatou nit prines spatky k rybarovi.",
			"Rybar te odmenil za tvoji pomoc."
		};

		internal static string GetJournalText(Player player) {
			int stage = GetStage(player);
			return ((stage < 1) || (stage > Done)) ? null : JournalTexts[stage - 1];
		}

		/// <summary>Erebor's condition on Elnassir's "Ano, tady to je.": all of it in the pack, and the gold where Pay will take it.</summary>
		internal static bool HasSalveGoods(Player player) {
			return (Q006.Count(player, "i_reag_spider_silk") >= 20) && (Q006.Count(player, "i_reag_nightshade") >= 80)
				&& (Q006.Count(player, "i_reag_bone") >= 150) && GoldUtils.CanPay(player, SalveGold);
		}

		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			npc.Dialog(player, DialogOf(npc), npc);
		}

		//by the def itself, never PrettyDefname (lowercase, c_q018_Mirrana)
		internal static QuestDialogDef DialogOf(NPC npc) {
			if (npc.Def == ThingDef.GetByDefname(MirranaDef)) {
				return SingletonScript<D_Q018Mirrana>.Instance;
			} else if (npc.Def == ThingDef.GetByDefname(ElnassirDef)) {
				return SingletonScript<D_Q018Elnassir>.Instance;
			}
			return SingletonScript<D_Q018Fisherman>.Instance;
		}

		/// <summary>i_q018_portujici_runa's @userdclick.</summary>
		internal static void UseRune(Player player) {
			if (player.Flag_Dead) {
				return;
			}
			if (GetStage(player) != 4) {
				player.SysMessage("Nevis, jak bys tuto zvlastni runu " + ((player.Gender == Gender.Female) ? "pouzila" : "pouzil") + ". Asi nejsi " + ((player.Gender == Gender.Female) ? "ta prava" : "ten pravy") + " k jejimu upotrebeni.");
				return;
			}
			player.Say("Wow, trochu se mi zveda zaludek!");
			GoNear(player, WheelSpot, false);
		}

		/// <summary>i_q018_zlate_kolo's @userdclick.</summary>
		internal static void UseWheel(Item wheel, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			if (GetStage(player) != 4) {
				player.SysMessage("Kolo se ani nepohne.");
				return;
			}
			wheel.Message("Sotva jsi se kola " + ((player.Gender == Gender.Female) ? "dotkla" : "dotkl") + ", zlata nit se objevila...");
			wheel.Message("Zda se, ze se tve znalosti siti rozsirily!!!");
			QuestItems.Give(player, Thread);
			player.SysMessage("Ziskal" + ((player.Gender == Gender.Female) ? "a" : "") + " jsi zlatou nit.");
			LevelingSystem.AddExperience(player, 1500);
			player.SetRealSkillValue((int) SkillName.Tailoring, player.GetRealSkillValue(SkillName.Tailoring) + TailoringGain);
			Advance(player, 5);
			GoNear(player, RuneSpot, true);
		}

		/// <summary>Erebor's src.go to a spot it wrote down: exactly there when it gave a height and one fits there (Osgiliath's ruins
		/// lie under other floors), else the standable tile nearest to it.</summary>
		private static void GoNear(Player player, Point4D spot, bool withZ) {
			Map map = Map.GetMap(spot.M);
			if (withZ && map.CanFit(spot.X, spot.Y, spot.Z, 16, true, false, true)) {
				player.Go(spot);
				return;
			}
			int[] at = WorldSpots.NearestStandable(map, spot.X, spot.Y, 4);
			if (at == null) {
				player.Go(spot);
			} else {
				player.Go(at[0], at[1], (sbyte) at[2], spot.M);
			}
		}
	}

	/// <summary>#1270 - Rybar podivin, Erebor's d_q018_rybar.</summary>
	public class D_Q018Fisherman : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3122;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q018.GetStage(player);
			Q018Pages.BaseButtons(d);
			d.Page("Pozdrav");
			if (s == 6) {
				d.SetText("Ahojda, jak se dari? Toho parchanta lososovskyho jsem porad jeste nechytnul ale urcite mi na tu zlatou musku brzo skoci!");
				d.Html();
			} else if ((s == 5) && QuestItems.Has(player, Q018.Thread)) {
				d.SetText("Vyborne!!! Mockrat ti dekuji <src.name>. Podej mi ji!");
				d.Html();
				d.SetText("*podat zlatou nit*");
				d.Response(false, true, "rybar:2");
			} else {
				d.SetText("Zdravim te.");
				d.Html();
			}
			d.Page("Kdo jsi?");
			d.SetText("Jsem pouhy rybar. Ziju celej svuj prasivej zivot u tyhle reky. Je tu totiz nejvetsi losos, ktereho jsem kdy zahledl. Ale mam problem... at uz udelam jakoukoliv musku, on mi na ni proste neskoci... Tak me napadlo, ze byste mi mohli pomoct...");
			d.Html();
			d.Page("Muzu pomoct?");
			if (s == 0) {
				d.SetText("Takze jsi se nakonec <src.sex(rozhodl,rozhodla)> mi pomoci, <src.name>? To je skvele! Nuze dobra, tady ode me mas rybu jako dar a poslouchej co od tebe potrebuji...");
				d.Html();
				d.SetText("*poslouchat*");
				d.Response(false, false, "p5");
			} else if (s < 5) {
				d.SetText("Praci jsem ti zadal tak neotravuj a zmiz");
				d.Html();
			} else {
				d.SetText("Uz od tebe nic nepotrebuju, odmenu jsi dostal tak se pakuj odsud a neplas tu ryby!");
				d.Html();
			}
			d.Page("-Prohlednout-");
			d.SetText("*Vidis staryho muze namacejiciho si nohy po kolena ve studene vode s rybarskym prutem v ruce.*");
			d.Html();
			d.Page("p5");
			d.SetText("Cestu od Mostu mezi Minasy smerem k poustnimu mestu Rhunu. Tam najdete Mirranu, mou starou pritelkyni, ona by mela vedet neco blizsiho o kouzelnem kole, na kterem se da uprist zlata snura. Myslim, ze pomoci te uz bych byl schopen udelat musku neodolatelnou i pro takovehodle giganta... Moc by mi to pomohlo...");
			d.Html();
			d.SetText("To mam riskovat zivot a hledat to kouzelne kolo jen kvuli prasivymu lososovi?");
			d.Response(true, false, "p6");
			d.Page("p6");
			d.SetText("*Rybar sebou vztekle trhnul, ale pak se ovladl*. Hodne to pro me znamena, je to proste ma rybarska vasen... moc te o to prosim, kdyz mi tu snuru prineses, urcite se ti odmenim.. predem diky...");
			d.Html();
			d.SetText("Dobra tedy ...");
			d.Response(false, true, "rybar:1");
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int s = Q018.GetStage(player);
			if ((button == "rybar:1") && (s == 0)) { //Erebor checked nothing: a fish and the start again at any stage
				Q006.Give(player, "i_fish_small");
				Q018.Advance(player, 1);
			} else if ((button == "rybar:2") && (s == 5) && QuestItems.Take(player, Q018.Thread)) {
				LevelingSystem.AddExperience(player, 1000);
				((Item) ThingDef.GetByDefname("i_gold").Create(player.Backpack)).Amount = Q018.RewardGold;
				Q018.Advance(player, Q018.Done);
			}
		}
	}

	/// <summary>#1270 - Mirrana, Erebor's d_q018_mirrana.</summary>
	public class D_Q018Mirrana : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3065;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q018.GetStage(player);
			Q018Pages.BaseButtons(d);
			d.Page("Pozdrav");
			if (s == 1) {
				d.SetText("Vitej, <src.name>. Co pro tebe mohu udelat?");
				d.Html();
				d.SetText("Posila me rybar...");
				d.Response(false, false, "p5");
			} else if (s == 2) {
				d.SetText("Co jeste potrebujes vedet, <src.name>? Vse potrebne jsem ti jiz rekla...");
				d.Html();
			} else if ((s == 3) && QuestItems.Has(player, Q018.Salve)) {
				d.SetText("Vyborne! Mas muj lektvar! Tak ted dobre poslouchej...");
				d.Html();
				d.SetText("*poslouchat*");
				d.Response(false, false, "p6");
			} else if (s >= 4) {
				d.SetText("Zdravim, <src.sex(muj zachrance,move zachrankyne)>! To se nam ten den pekne vydaril, ze?");
				d.Html();
			} else {
				d.SetText("Vitej, <src.name>. Co pro tebe mohu udelat?");
				d.Html();
			}
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Mirrana.");
			d.Html();
			d.Page("Muzu pomoct?");
			d.SetText("Nepotrebuji zadnou pomoc, dekuji.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("*Vidis mladou pohlednou zenu*");
			d.Html();
			d.Page("p5");
			d.SetText("Zlata nit rikas? Ano, vim o miste, kde najdes to kouzelne kolo, na nemz se takova snura da uprist. Ale zadarmo ani kure nehrabe. Napadl me tu pred dvema tydny lizard, osklive me pokousal do nohy. Samo o sobe by to nebyla tak priserna rana, ale ty zpropadene vypary, jichz je na techto mistech bezpocet, museli obsahovat neco, co mi ranu zanitilo a zpusobilo, ze se nechce hojit. Najdete mastickare Ernassila, zije pry nekde na Ebenovych ostrovech. On by vam mel dat lektvar, jenz by mi mohl s timto zranenim pomoci. Potom vam reknu, kde najdete ono hledane kolo.");
			d.Html();
			d.SetText("Pokusim se...");
			d.Response(false, true, "mirrana:1");
			d.Page("p6");
			d.SetText("Na mordorske strane zricenin u Minaskeho mostu naleznes schovanou runu. Ma zlatou barvu, neprehlednes ji. Kdyz ji zatlacis do zdi , kouzlo timto vyvolane te prenese az na misto, kde by melo byt zlate kolo schovane. Ale opatrne - nejspis bude hlidane!");
			d.Html();
			d.SetText("*Podat lektvar*");
			d.Response(false, true, "mirrana:2");
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int s = Q018.GetStage(player);
			if ((button == "mirrana:1") && (s == 1)) {
				Q018.Advance(player, 2);
			} else if ((button == "mirrana:2") && (s == 3) && QuestItems.Take(player, Q018.Salve)) { //Erebor's took the salve at any stage
				LevelingSystem.AddExperience(player, 500);
				Q018.Advance(player, 4);
			}
		}
	}

	/// <summary>#1270 - Elnassir mastickar, Erebor's d_q018_elnassir.</summary>
	public class D_Q018Elnassir : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3103;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q018.GetStage(player);
			Q018Pages.BaseButtons(d);
			d.Page("Pozdrav");
			if (s == 2) {
				d.SetText("Vitej, <src.name>. Shanis neco konkretniho?");
				d.Html();
				d.SetText("Potrebuji masticku pro Mirranu...");
				d.Response(false, false, "p5");
			} else if ((s == 3) && !QuestItems.Has(player, Q018.Salve)) {
				d.SetText("Tak co, mas ty ingredience a penize?");
				d.Html();
				if (Q018.HasSalveGoods(player)) {
					d.SetText("Ano, tady to je.");
					d.Response(false, true, "elnassir:2");
				} else {
					d.SetText("Ne, jeste ne.");
					d.ExitResponse(false, false);
				}
			} else {
				d.SetText("Vitej, <src.name>. Co pro tebe mohu udelat?");
				d.Html();
			}
			d.Page("Kdo jsi?");
			d.SetText("Jmenuji se Elnassir, jsem mastickar.");
			d.Html();
			d.Page("Muzu pomoct?");
			d.SetText("Nepotrebuji pomoc, dekuji za optani.");
			d.Html();
			d.Page("-Prohlednout-");
			d.SetText("*Vidis, a citis, muze pachnouciho od ruznych bylin*");
			d.Html();
			d.Page("p5");
			d.SetText("Ano, takovou lecivou mast umim zhotovir. ale jsou k tomu potreba nejaky ty ingredience. Vis co myslim, korinky, bylinky, a tak podobne... Prines mi 20 spider silk, 80 nightshade, 150 bone a 10 tisic zlataku. *usmal se*");
			d.Html();
			d.SetText("Prinesu ti to vsechno.");
			d.Response(false, true, "elnassir:1");
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int s = Q018.GetStage(player);
			if ((button == "elnassir:1") && (s == 2)) {
				Q018.Advance(player, 3);
			} else if ((button == "elnassir:2") && (s == 3) && !QuestItems.Has(player, Q018.Salve)) {
				if (!Q018.HasSalveGoods(player) || !GoldUtils.Pay(player, Q018.SalveGold, PaySource.Usual, "")) {
					npc.Say("Nesnaz se me podvest!");
					return;
				}
				Q006.Consume(player, "i_reag_spider_silk", 20);
				Q006.Consume(player, "i_reag_nightshade", 80);
				Q006.Consume(player, "i_reag_bone", 150);
				QuestItems.Give(player, Q018.Salve);
				player.SysMessage("Ziskal" + ((player.Gender == Gender.Female) ? "a" : "") + " jsi odvar proti zanetum.");
			}
		}
	}

	internal static class Q018Pages {
		/// <summary>The three big buttons every q018 dialog has, where Erebor's argo.button lines put them.</summary>
		internal static void BaseButtons(QuestDialogContent d) {
			d.BaseButton(1, 50, "Kdo jsi?");
			d.BaseButton(2, 40, "Muzu pomoct?");
			d.BaseButton(3, 35, "-Prohlednout-", QuestDialogContent.ColorEmote);
		}
	}

	/// <summary>#1270 - @UserDClick of q018's NPCs.</summary>
	public class E_Q018Npc : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q018.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1270 - the golden rune in Osgiliath's ruins.</summary>
	public class E_Q018Rune : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				Q018.UseRune(player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1270 - the golden spinning wheel in the Pits of Death.</summary>
	public class E_Q018Wheel : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				Q018.UseWheel(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1270 - spk_q018_rybar, spk_q018_Mirrana and spk_q018_elnassir: their greetings, from close enough.</summary>
	public sealed class Q018Speech : AbstractSpeech {
		private readonly string[] patterns;

		public new static void Bootstrap() {
			new Q018Speech("spk_q018_rybar", new[] { "cau", "ahoj", "dobry", "den", "zdrav", "prace", "ukol" }).Register();
			new Q018Speech("spk_q018_Mirrana", new[] { "cau", "ahoj", "dobry", "den", "zdrav", "rybar" }).Register();
			new Q018Speech("spk_q018_elnassir", new[] { "cau", "ahoj", "dobry", "den", "zdrav", "mast" }).Register();
		}

		private Q018Speech(string defname, string[] patterns)
			: base(defname) {
			this.patterns = patterns;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, this.patterns) && QuestNpc.IsCloseEnough(npc, player)) {
				Q018.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
