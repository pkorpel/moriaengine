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
	/// #1266 - Erebor's q007 (questy/q007_HaradirovoSelatko.scp), three quests in one file, as there:
	/// <para>a, Haradirovo selatko (qtag q007_haradirovo_selatko, 0-8): the hunter Haradir sends the player for the smoked piglet the
	/// innkeeper owes him. The innkeeper denies it; the witch Rachel, for 1000 bat wings, makes him talk; he fears the Evil Dryad of
	/// his island and wants her bow as proof she's dead; then the piglet goes to Haradir. Pirate Rodrigo sells rumours on the way.</para>
	/// <para>b, Kniha inskripce (q007_stary_inscripter, 0-3): after a, the Old Inscripter (Inscription 80) writes the Inscription book
	/// from three stones; the drunk Divous trades it for the magic pen.</para>
	/// <para>c, Adamantinove kladivko (q007_prastary_kovar, 0-2): after a, the Ancient Smith (Blacksmithing 100) makes the adamantine
	/// hammer from the same three stones.</para>
	/// <para>The quest items are player tags (QuestItems): the bow, the piglet, the stones, the book and the pen. The book is what
	/// lets one inscribe 8th-circle scrolls, as on Erebor; the pen turns a blank rune into a recall rune. The hammer is an item.
	/// Erebor's stones were GM loot only; here a pirate chest on the pirate island, where Rodrigo says they are, gives them.</para>
	/// </summary>
	public static class Q007 {
		public const string HaradirDef = "c_Haradir";
		public const string InnkeeperDef = "c_hospod";
		public const string RachelDef = "c_rachel";
		public const string SmithDef = "c_kovarskej";
		public const string InscripterDef = "c_maginscript";
		public const string DivousDef = "c_divous";
		public const string RodrigoDef = "c_rodrigo";
		public const string DryadDef = "c_Zla_Dryada";
		public const string ChestDef = "i_q007_poklad_piratu";
		public const string HammerDef = "i_adamantinum_kladivko";

		public const string Bow = "i_lukdryady", Piglet = "i_selatko", Book = "i_book_inscription", Pen = "i_magicke_pero";
		public static readonly string[] Stones = { "i_mesicni_kamen", "i_meteorit", "i_kamen_krve" };
		public const int BatWings = 1000, InscriptionNeeded = 799, SmithNeeded = 999, PenInscription = 849;
		public const int HammerBonus = 100; //tenths, Erebor's blacksmithing+100
		public const int DrinkSeconds = 300; //Erebor's 3000 tenths of serv.time
		public const int PiratesRange = 10;

		private static readonly TagKey stageTK = TagKey.Acquire("_q007Stage_");
		private static readonly TagKey inscripterTK = TagKey.Acquire("_q007Inscripter_");
		private static readonly TagKey smithTK = TagKey.Acquire("_q007Smith_");
		private static readonly TagKey rodrigoTK = TagKey.Acquire("_q007Rodrigo_");
		private static readonly TagKey dolphinTK = TagKey.Acquire("_q007Delfin_");
		private static readonly TagKey drinkTK = TagKey.Acquire("_q007DrinkAt_");
		internal static readonly TagKey HammerBoostTK = TagKey.Acquire("_q007HammerBoost_");

		public static QuestJournalEntry PigletEntry { get; private set; }
		public static QuestJournalEntry BookEntry { get; private set; }
		public static QuestJournalEntry HammerEntry { get; private set; }

		public static void Bootstrap() {
			PigletEntry = QuestJournal.Register(new QuestJournalEntry("Haradirovo selatko", 0x3106, p => Text(PigletTexts, GetStage(p)), p => GetStage(p) == 8));
			BookEntry = QuestJournal.Register(new QuestJournalEntry("Kniha inskripce", 0x3053, p => Text(BookTexts, GetInscripter(p)), p => GetInscripter(p) >= 2)); //Erebor showed text 2 at 3 too, its pen text never
			HammerEntry = QuestJournal.Register(new QuestJournalEntry("Adamantinove kladivko", 0x3059, p => Text(HammerTexts, GetSmith(p)), p => GetSmith(p) == 2));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			foreach (string npc in new[] { HaradirDef, InnkeeperDef, RachelDef, SmithDef, InscripterDef, DivousDef, RodrigoDef }) {
				QuestNpc.Attach(npc, SingletonScript<E_Q007Npc>.Instance);
			}
			QuestNpc.Attach(ChestDef, SingletonScript<E_Q007Chest>.Instance);
			QuestNpc.Attach("i_rune_tinker", SingletonScript<E_Q007BlankRune>.Instance);
			QuestItems.Register(Bow, "luk dryady");
			QuestItems.Register(Piglet, "selatko");
			QuestItems.Register(Stones[0], "mesicni kamen");
			QuestItems.Register(Stones[1], "meteorit");
			QuestItems.Register(Stones[2], "krvavy kamen");
			QuestItems.Register(Book, "Knihu Inscription 8 kruhu");
			QuestItems.Register(Pen, "magicke pero");
		}

		private static string Text(string[] texts, int stage) {
			return ((stage < 1) || (stage > texts.Length)) ? null : texts[stage - 1];
		}

		private static int Get(Player player, TagKey tk) {
			object tag = player.GetTag(tk);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		public static int GetStage(Player player) {
			return Get(player, stageTK);
		}

		public static int GetInscripter(Player player) {
			return Get(player, inscripterTK);
		}

		public static int GetSmith(Player player) {
			return Get(player, smithTK);
		}

		internal static void SetStage(Player player, int stage) {
			player.SetTag(stageTK, stage);
		}

		internal static void SetInscripter(Player player, int stage) {
			player.SetTag(inscripterTK, stage);
		}

		internal static void SetSmith(Player player, int stage) {
			player.SetTag(smithTK, stage);
		}

		internal static void Advance(Player player, int stage) {
			SetStage(player, stage);
			QuestJournal.Report(player, PigletEntry);
		}

		internal static void AdvanceInscripter(Player player, int stage) {
			SetInscripter(player, stage);
			QuestJournal.Report(player, BookEntry);
		}

		internal static void AdvanceSmith(Player player, int stage) {
			SetSmith(player, stage);
			QuestJournal.Report(player, HammerEntry);
		}

		public static bool PaidRodrigo(Player player) {
			return player.GetTag(rodrigoTK) != null;
		}

		internal static void SetPaidRodrigo(Player player) {
			player.SetTag(rodrigoTK, true);
		}

		public static bool AdoptedDolphin(Player player) {
			return player.GetTag(dolphinTK) != null;
		}

		internal static void SetAdoptedDolphin(Player player) {
			player.SetTag(dolphinTK, true);
		}

		public static bool HasStones(Player player) {
			return Array.TrueForAll(Stones, s => QuestItems.Has(player, s));
		}

		internal static void TakeStones(Player player) {
			foreach (string stone in Stones) {
				QuestItems.Take(player, stone);
			}
		}

		/// <summary>Who the chest gives stones to: a player Haradir told about the old smith or inscripter, sent for them by either.</summary>
		public static bool NeedsStones(Player player) {
			return (GetInscripter(player) == 1) || (GetSmith(player) == 1);
		}

		internal static void Reward(Player player, int experience) {
			LevelingSystem.AddExperience(player, experience);
		}

		internal static int Skill(Player player, SkillName skill) {
			return player.GetSkill((int) skill);
		}

		#region drinks
		/// <summary>Erebor's innkeeper's drinks: one every 5 minutes, whatever it was, paid as anything is.</summary>
		internal static void Drink(NPC innkeeper, Player player, string defname, int price, string said) {
			object last = player.GetTag(drinkTK);
			if ((last != null) && (Globals.TimeInSeconds - Convert.ToDouble(last, CultureInfo.InvariantCulture) < DrinkSeconds)) {
				innkeeper.Say("Myslim, ze mas prozatim dost.");
				return;
			}
			if (!GoldUtils.Pay(player, price, PaySource.Usual, "")) {
				innkeeper.Say("Podivej, bez penez muzes jit leda ven lizat snih.");
				return;
			}
			player.SetTag(drinkTK, Globals.TimeInSeconds);
			ThingDef.GetByDefname(defname).Create(player.Backpack);
			innkeeper.Say(said);
		}

		internal static void ForgetDrink(Player player) {
			player.RemoveTag(drinkTK);
		}
		#endregion drinks

		#region the hammer, the pen, the book
		/// <summary>Erebor's i_adamantinum_kladivko @equip: +10 Blacksmithing to a smith at 100. Not owner-bound.</summary>
		internal static void OnEquip(Character ch, Item item) {
			if ((item.Def != ThingDef.GetByDefname(HammerDef)) || !(ch is Player player) || (Skill(player, SkillName.Blacksmith) <= SmithNeeded)
					|| (item.GetTag(HammerBoostTK) != null)) {
				return;
			}
			player.ModifySkillValue(SkillName.Blacksmith, HammerBonus);
			item.SetTag(HammerBoostTK, player);
			player.SysMessage("Citis mistrovstvi spjate s timto kladivkem ... pokud jsi GM kovar, muzes kovat mithril.");
		}

		internal static void OnUnequip(Character ch, Item item) {
			if (item.GetTag(HammerBoostTK) is Character boosted) {
				item.RemoveTag(HammerBoostTK);
				if (!boosted.IsDeleted) {
					boosted.ModifySkillValue(SkillName.Blacksmith, -HammerBonus);
				}
			}
		}

		/// <summary>Erebor's i_book_inscription in the craft menu (inscrip.scp's TESTIF findid): the 8th circle wants the book.</summary>
		public static bool MayInscribe(SpellDef spell, Character chr) {
			return (spell == null) || (spell.Id < 57) || (spell.Id > 64) || QuestItems.Has(chr, Book) || chr.IsGM;
		}

		/// <summary>Erebor's i_magicke_pero @userdclick, from the rune's side as the pen is a tag: a blank rune becomes a recall rune.</summary>
		internal static bool TryInscribeRune(Item rune, Player player) {
			if (!QuestItems.Has(player, Pen)) {
				return false;
			}
			if ((Skill(player, SkillName.Inscribe) > PenInscription) && (rune.TopObj() == player)) {
				Container pack = player.Backpack;
				rune.Delete();
				ThingDef.GetByDefname("i_rune_marker").Create(pack);
				player.SysMessage("Magicke pero vepsalo do runy jeji cil.");
			} else {
				player.SysMessage("Toto pero zda se magii ocarovano tak ze inkoust na predmet jim naneseny, svou stopu navzdy zachova byt i neviditelna po case bude.");
			}
			return true;
		}
		#endregion the hammer, the pen, the book

		#region the pirates' chest
		/// <summary>The stolen stones in the pirates' chest: once a player needs them, with the pirates around it dead.</summary>
		internal static void OpenChest(Item chest, Player player) {
			if (!NeedsStones(player)) {
				player.SysMessage("Truhla je prazdna, krome par zetlelych hader.");
			} else if (HasStones(player)) {
				player.SysMessage("Kameny z truhly uz mas.");
			} else if (GrandmasterQuest.AreGuardsAlive(chest, null)) {
				player.RedMessage("Pirati svou truhlu hlidaji. Nejdriv se jich zbav!");
			} else {
				foreach (string stone in Stones) {
					QuestItems.Give(player, stone);
				}
				player.SysMessage("Mezi senem v truhle najdes mesicni kamen, meteorit a krvavy kamen.");
			}
		}
		#endregion the pirates' chest

		#region talking
		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			QuestDialogDef dialog = DialogOf(npc);
			if (dialog != null) {
				npc.Dialog(player, dialog, npc);
			}
		}

		//by the def itself: a def's PrettyDefname needn't keep the case its .def wrote ("c_Haradir")
		internal static QuestDialogDef DialogOf(NPC npc) {
			ThingDef def = npc.Def;
			if (def == ThingDef.GetByDefname(HaradirDef)) {
				return SingletonScript<D_Q007Haradir>.Instance;
			} else if (def == ThingDef.GetByDefname(InnkeeperDef)) {
				return SingletonScript<D_Q007Hospodsky>.Instance;
			} else if (def == ThingDef.GetByDefname(RachelDef)) {
				return SingletonScript<D_Q007Rachel>.Instance;
			} else if (def == ThingDef.GetByDefname(SmithDef)) {
				return SingletonScript<D_Q007Smith>.Instance;
			} else if (def == ThingDef.GetByDefname(InscripterDef)) {
				return SingletonScript<D_Q007Inscripter>.Instance;
			} else if (def == ThingDef.GetByDefname(DivousDef)) {
				return SingletonScript<D_Q007Divous>.Instance;
			} else if (def == ThingDef.GetByDefname(RodrigoDef)) {
				return SingletonScript<D_Q007Rodrigo>.Instance;
			}
			return null;
		}
		#endregion talking

		/// <summary>Erebor's d_questy_Haradirovo_selatko, d_questy_kniha_inscripce and d_questy_adamantinove_kladivko texts.</summary>
		internal static readonly string[] PigletTexts = {
			"<?src.sex(Rekl,Rekla)?> jsi haradirovi, ze mu prineses od hospodskeho selatko, ktere mu ten mizera slibil.",
			"Hospodsky odmita priznat, ze kdy Haradirovi nejake sele slibil a nenecha se presvedcit. Pokud Haradir nevi, jak hospodskeho presvedcit, aby slib dodrzel, nema nejmensi smysl zkouset sele vymahat.",
			"Haradir je presvedcen, ze hospodsky je zavazan zene jmenem Rachel. Mozna by stalo za to zkusit ji presvedcit, aby tomu lakomci domluvila.",
			"Rachel ti prislibila pomoc s hospodskym, pokud ji prineses jeden tisic netopyrich kridel (batwings).",
			"Rachel domluvila hospodskemu. Je cas zjistit, jestli to melo vliv.",
			"Ukazalo se, ze lakota hospodskeho je zpusobena strachem o holy zivot. Vsechny jeho zasoby jsou odvadeny prisluhovacum zle Dryady, ktera se na ostrove usidlila. Hospodsky ti nemuze sele predat, dokud ho Dryady nezbavis. Jako dukaz jeji smrti chce prinest jeji luk.",
			"<?src.sex(Zbavil,Zbavila)?> jsi hospodskeho nebezpeci a on ti za odmenu dal pro Haradira selatko, ktere slibil.",
			"Haradir od tebe velmi vdecne selatko prijal. Jedine, co ti mohl nabidnout vymenou je par zajimavych pribehu."
		};

		internal static readonly string[] BookTexts = {
			"Tento mocny carodej ti napise Knihu Inskripce, pokud mu doneses tri kameny:<br><br> Krvavy kamen, Mesicni kamen a Meteorit.",
			"<?src.sex(Obstaral,Obstarala)?> jsi suroviny pro aktivaci knihy a <?src.sex(odnesl,odnesla)?> jsi si od stareho kouzelnika cenny predmet a spoustu dulezitych zkusenosti.",
			"<?src.sex(Uzavrel,Uzavrela)?> jsi s muzem jmenem Divous dohodu a <?src.sex(vymenil,vymenila)?> jsi knihu Inskripce za magicke pero." //Erebor's said Rodrigo, it was Divous
		};

		internal static readonly string[] HammerTexts = {
			"Nejzrucnejsi kovar, jakeho jsi kdy <?src.sex(potkal,potkala)?> slibil, ze ti vytvori nastroj tvych kvalit, pokud mu prineses vsechny tyto kameny:<br><br> Krvavy kamen, Mesicni kamen a Meteorit.",
			"<?src.sex(Obstaral,Obstarala)?> jsi kovari suroviny pro vyrobu adamantinoveho kladivka, ktere ti slibil, a <?src.sex(stal jsi se jeho hrdym vlastnikem,stala jsi se jeho hrdou vlastnici)?>."
		};
	}

	/// <summary>#1266 - @UserDClick of q007's NPCs.</summary>
	public class E_Q007Npc : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			Player player = clicker as Player;
			if (player == null) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q007.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1266 - the pirates' chest, Erebor's TM_poklad_piratu.</summary>
	public class E_Q007Chest : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				if (Point2D.GetSimpleDistance(self, player) > QuestNpc.TalkDistance) {
					player.SysMessage("Na truhlu nedosahnes.");
				} else {
					Q007.OpenChest(self, player);
				}
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1266 - i_rune_tinker, the blank rune: with the magic pen a recall rune.</summary>
	public class E_Q007BlankRune : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			return ((clicker is Player player) && Q007.TryInscribeRune(self, player)) ? TriggerResult.Cancel : TriggerResult.Continue;
		}
	}

	/// <summary>#1266 - the speeches of q007's NPCs (Erebor's spk_q007_*), from close enough.</summary>
	public sealed class Q007Speech : AbstractSpeech {
		private readonly string[] patterns;

		public new static void Bootstrap() {
			new Q007Speech("spk_q007_haradir", new[] { "hi", "dobry", "ahoj", "hospodsky", "selatko" }).Register();
			new Q007Speech("spk_q007_hospodsky", new[] { "hi", "dobry", "ahoj", "selatko", "haradir", "eriol" }).Register();
			new Q007Speech("spk_q007_Carodejnice", new[] { "hi", "dobry", "ahoj", "selatko", "haradir", "rachel" }).Register();
			new Q007Speech("spk_q007_kovar", new[] { "hi", "dobry", "ahoj", "haradir", "mithril" }).Register();
			new Q007Speech("spk_q007_maginscript", new[] { "hi", "dobry", "ahoj", "haradir", "mithril" }).Register();
			new Q007Speech("spk_q007_divous", new[] { "hi", "buy", "ahoj" }).Register();
			new Q007Speech("spk_q007_rodrigo", new[] { "hi", "buy", "ahoj" }).Register();
		}

		private Q007Speech(string defname, string[] patterns)
			: base(defname) {
			this.patterns = patterns;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			NPC npc = listener as NPC;
			Player player = speechArgs.Speaker as Player;
			if ((npc != null) && (player != null) && (speechArgs.Speech != null) && SkillTeacherSpeech.ContainsAny(speechArgs.Speech, this.patterns)
					&& QuestNpc.IsCloseEnough(npc, player)) {
				Q007.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
