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
	/// #1278 - Erebor's q099 "Vyvrhel" (questy/q099_vyvrhel.scp, q099_vyvrhel_items.scp): Podivin frees an outcast of their exile.
	/// Four bags to fill (100 grizzly tails, 80 earth elemental ears, 50 eye essences, 5 poison phoenix feathers), Mining 40, then a
	/// cheque of exactly 250 000 and 10 levels (#1307, the user's call; Erebor took 15): the realm comes back (Erebor's function vyvrhel: Gondor with no murders, fame or
	/// karma, Mordor with -3000 karma). Erebor also wanted tag.vykoupeni, which nothing in the archive sets: here every outcast may ask.
	/// <para>Erebor's char tag q099: 1 met him, 2/4/6/8 filling a bag, 3/5/7 a bag handed in, 9 ready; it was removed at the end, here
	/// 10 marks it done and a new exile starts it again.</para>
	/// </summary>
	public static class Q099 {
		public const string PodivinDef = "c_q099_vyvrhel", ChequeDef = "i_cashiers_check";
		public static readonly string[] BagDefs = { "i_q099_vyvrhel_1", "i_q099_vyvrhel_2", "i_q099_vyvrhel_3", "i_q099_vyvrhel_4" };
		public const int Ready = 9, Done = 10, Price = 250000, LevelsTaken = 10, MiningNeeded = 400;
		public const short MordorKarma = -3000;

		private static readonly TagKey stageTK = TagKey.Acquire("_q099Vyvrhel_");

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Vyvrhel", 0x3030, GetJournalText, p => GetStage(p) == Done));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(PodivinDef, SingletonScript<E_Q099Podivin>.Instance);
		}

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

		internal static bool IsOutcast(Player player) {
			return (player.Realm == Realm.GondorOutcast) || (player.Realm == Realm.MordorOutcast);
		}

		/// <summary>The bag being filled at stage 2, 4, 6 or 8, or null.</summary>
		internal static string BagAt(int stage) {
			return ((stage >= 2) && (stage <= 8) && (stage % 2 == 0)) ? BagDefs[(stage / 2) - 1] : null;
		}

		internal static readonly string[] JournalTexts = {
			"<?src.sex(Potkal,Potkala)?> jsi podivneho cloveka ktery by ti snad mohl pomoci zbavit se vyhanstvi!",
			"Podivin po tobe chce 100 ocasku medvedu grizli do batohu ktery ti dal.",
			"Podivin ma pro tebe jiste dalsi ukol.",
			"Podivin po tobe chce 80 usi elementala zeme do batohu ktery ti dal.",
			"Podivin po tobe chce 50 esenci z monstra zvaneho oko (Eye).",
			"Podivin po tobe chce 5 pirek jedoveho fenixe.",
			"Jestli jsi <?src.sex(pripraven,pripravena)?> Podivin je pripraven zbavit te vyhnanstvi!",
			"Podivin te zbavil vyhnanstvi." //new: Erebor dropped the entry at the end
		};

		//d_questy_vyvrhel: which text each stage shows
		private static readonly int[] journalLine = { 0, 0, 1, 2, 3, 2, 4, 2, 5, 6, 7 };

		internal static string GetJournalText(Player player) {
			int stage = GetStage(player);
			return ((stage < 1) || (stage > Done)) ? null : JournalTexts[journalLine[stage]];
		}

		/// <summary>f_q099_vyvrhel: an outcast meeting him starts the quest, or starts it again after an earlier redemption.</summary>
		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			int stage = GetStage(player);
			if (IsOutcast(player) && ((stage == 0) || (stage == Done))) {
				Advance(player, 1);
			}
			npc.Dialog(player, SingletonScript<D_Q099Podivin>.Instance, npc);
		}

		/// <summary>srazitlevely, which the archive calls but doesn't hold: levels down (not below 1), the experience toward the next
		/// gone, and (#1307, the user's call) the points those levels gave taken back, spent or not, so levels earned again pay
		/// fairly: skill points from what's unspent, then from the skills bought with them (5.0 a step, the highest first, each step
		/// refunding what it cost); attribute points from the stats (the highest first, never under the profession's start); class
		/// abilities, which count their points from the level, reset to be spent again if they're over.</summary>
		internal static void TakeLevels(Player player, int levels) {
			int lost = Math.Min(levels, Math.Max(0, player.Level - 1));
			player.Level -= lost;
			player.Experience = 0;
			TakeSkillPoints(player, LevelingSkillPointsPerLevel * lost);
			player.UnspentAbilityPoints = Math.Max(0, player.UnspentAbilityPoints - (2 * lost));
			TakeAttributePoints(player);
			if (ClassAbilities.Spare(player) < 0) {
				ClassAbilities.Reset(player);
				player.SysMessage("Tve schopnosti byly zapomenuty. Rozdel body znovu.");
			}
			player.RefreshTitle();
		}

		internal const int LevelingSkillPointsPerLevel = 5; //LevelingSystem.AddExperience's grant

		//the skill points owed: unspent first, then 5.0 steps of the point-bought skills, the highest first
		private static void TakeSkillPoints(Player player, int owed) {
			int fromUnspent = Math.Min(owed, Math.Max(0, player.UnspentSkillPoints));
			player.UnspentSkillPoints -= fromUnspent;
			owed -= fromUnspent;
			ProfessionDef profession = player.Profession;
			while ((owed > 0) && (profession != null)) {
				SkillDef highest = null;
				int highestValue = -1;
				foreach (SkillDef skill in SkillDef.AllSkillDefs) {
					int value = player.GetRealSkillValue(skill);
					if (LevelingSystem.IsSkillPointSpendable((SkillName) skill.Id) && (value - LevelingSystem.SkillPointGain >= profession.GetSkillMinimum(skill))
							&& (value > highestValue)) {
						highest = skill;
						highestValue = value;
					}
				}
				if (highest == null) {
					break; //nothing bought left to take: the rest is forgiven
				}
				int lowered = highestValue - LevelingSystem.SkillPointGain;
				player.SetRealSkillValue(highest, lowered);
				owed -= LevelingSystem.SkillRaiseCost(lowered);
			}
			if (owed < 0) {
				player.UnspentSkillPoints += -owed; //the last step cost more than was owed: the change back
			}
		}

		//the attribute budget over: a point at a time from the highest stat above the profession's start
		private static void TakeAttributePoints(Player player) {
			ProfessionDef profession = player.Profession;
			while ((player.AttributePointBudget < 0) && (profession != null)) {
				AttributeStat? highest = null;
				int highestValue = -1;
				foreach (AttributeStat stat in new[] { AttributeStat.Str, AttributeStat.Dex, AttributeStat.Int, AttributeStat.Vit }) {
					int value = StatOf(player, stat);
					int[] range = profession.GetStatRange(stat);
					int floor = ((range != null) && (range.Length > 0)) ? range[0] : 1;
					if ((value > floor) && (value > highestValue)) {
						highest = stat;
						highestValue = value;
					}
				}
				if (highest == null) {
					break;
				}
				LowerStat(player, highest.Value);
			}
		}

		private static int StatOf(Player player, AttributeStat stat) {
			switch (stat) {
				case AttributeStat.Str: return player.Str;
				case AttributeStat.Dex: return player.Dex;
				case AttributeStat.Int: return player.Int;
				default: return player.Vit;
			}
		}

		private static void LowerStat(Player player, AttributeStat stat) {
			switch (stat) {
				case AttributeStat.Str:
					player.Str -= 1;
					player.StrPointsAllocated -= 1;
					break;
				case AttributeStat.Dex:
					player.Dex -= 1;
					player.DexPointsAllocated -= 1;
					break;
				case AttributeStat.Int:
					player.Int -= 1;
					player.IntPointsAllocated -= 1;
					break;
				default:
					player.Vit -= 1;
					player.VitPointsAllocated -= 1;
					break;
			}
		}

		/// <summary>Erebor's function vyvrhel on an outcast: the realm back.</summary>
		internal static void Restore(Player player) {
			player.Fame = 0;
			if (player.Realm == Realm.GondorOutcast) {
				player.Realm = Realm.Gondor;
				Notoriety.SetMurderCount(player, 0);
				player.Karma = 0;
			} else if (player.Realm == Realm.MordorOutcast) {
				player.Realm = Realm.Mordor;
				player.Karma = MordorKarma;
			}
		}

		/// <summary>The first cheque in the pack, or null.</summary>
		internal static CashiersItem FindCheque(Player player) {
			return (player.Backpack == null) ? null : Q009.FirstCheque(player.Backpack);
		}

		/// <summary>Button 9: the cheque of exactly 250 000, then the change.</summary>
		internal static void Redeem(NPC npc, Player player) {
			CashiersItem cheque = FindCheque(player);
			if (cheque == null) {
				npc.Say("A kde je ten sek? Nepokousej se me okrast!");
				return;
			}
			if (cheque.Value != Price) {
				npc.Say("To je malo!");
				return;
			}
			cheque.Delete();
			Restore(player);
			TakeLevels(player, LevelsTaken);
			EffectFactory.StationaryEffect(player, 0x374A, 6, 15); //i_fx_curse
			player.Sound(550);
			Advance(player, Done);
			Logger.WriteInfo(true, "Q099: " + player.Name + " (" + player.Account?.Name + ") se zbavil vyhnanstvi, realm " + player.Realm + ", level " + player.Level);
		}
	}

	/// <summary>#1278 - Podivin, Erebor's d_q099_vyvrhel (its own gump, drawn here as a quest dialog: the greeting on the first page,
	/// the task behind the Ukol button).</summary>
	public class D_Q099Podivin : QuestDialogDef {
		private static readonly int[] counts = { 100, 80, 50, 5 };
		private static readonly string[] tasks = {
			"Tvym prvnim ukolem je nasbirat mi 100 kusu ocasku medveda grizliho do batohu, ktery ti dam!",
			"Nyni mi prines 80 kusu usi elementala zeme!",
			"Nyni mi prines 50 kusu esenci z monstier zvanych oci (Eye)!",
			"Nyni mi prines 5 pirek z jedoveho fenixe!"
		};

		protected override int Face {
			get {
				return 0x3030;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			int s = Q099.GetStage(player);
			bool outcast = Q099.IsOutcast(player);
			if (outcast && (s > 0) && (s < Q099.Done)) {
				d.BaseButton(1, 65, "Ukol", QuestDialogContent.ColorBaseButton);
			}
			d.Page("Pozdrav");
			if (!outcast) {
				d.SetText("Di pryc! Nic nemam a nic nechci!");
				d.Html();
				return;
			}
			string bag = Q099.BagAt(s);
			if (s == 1) {
				d.SetText("Bud zdrav, vyvrheli! <basefont color=\"#ffff88\">*smeje se*<basefont color=\"#ffffff\"> Urcite si me vyhledal, aby ses zbavil sveho vyhnanstvi! Je to tak? <basefont color=\"#ffff88\">*smeje se*<basefont color=\"#ffffff\"> Jesli ano, nejdriv musis splnit nekolik mych ukolu!");
				d.Html();
			} else if ((bag != null) && (Collectors.FindIn(player, bag) != null)) {
				if (Collectors.IsFull(player, bag)) {
					d.SetText("Vyborne! Podej mi ten vacek!");
					d.Html();
					d.SetText("Podat vacek");
					d.Response(false, true, "q099:" + s);
				} else { //Erebor said nothing here for the first bag
					d.SetText("Hm, jeste toho nemas dost!");
					d.Html();
				}
			} else if ((s == 3) || (s == 5) || (s == 7) || (s == Q099.Ready)) {
				d.SetText("Mam pro tebe dalsi ukol!");
				d.Html();
			} else {
				d.SetText("Co chces?");
				d.Html();
			}
			d.Page("Ukol");
			if ((s == 1) || (s == 3) || (s == 5) || (s == 7)) {
				d.SetText(tasks[s / 2]);
				d.Html();
				d.SetText("Prinesu to");
				d.Response(false, true, "q099:" + s);
				d.SetText("Nemam zajem");
				d.ExitResponse(false, false);
			} else if (s == Q099.Ready) {
				if (player.GetSkill((int) SkillName.Mining) < Q099.MiningNeeded) {
					d.SetText("Tenhle ukol neni ani tak pro me, ale pro tebe. Nato abys <?src.sex(prezil,prezila)?> transformaci musis byt silnejsi! Bez kopat! Az budes <?src.sex(schopnej,schopna)?> kopat zeleznou rudu tak se vrat.");
					d.Html();
				} else {
					d.SetText("Vyborne, si dostatecne <?src.sex(silny,silna)?> a <?src.sex(pripraven,pripravena)?>. Ted mi prines 250 000 v seku a muzme se do toho pustit! Ale bude te to stat 10 urovni.");
					d.Html();
					d.SetText("Podat sek");
					d.Response(false, true, "q099:9");
				}
			} else {
				d.SetText("Az splnis to co sem ti zadal dostanes dalsi ukol!");
				d.Html();
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int s = Q099.GetStage(player);
			if (!Q099.IsOutcast(player) || (button != "q099:" + s)) {
				return;
			}
			if ((s == 1) || (s == 3) || (s == 5) || (s == 7)) { //a bag to fill
				Q006.GiveBag(player, Q099.BagDefs[s / 2]);
				Q099.Advance(player, s + 1);
			} else if (Q099.BagAt(s) != null) { //the full bag back
				string bag = Q099.BagAt(s);
				if (Collectors.IsFull(player, bag)) {
					Collectors.FindIn(player, bag).Delete();
					Q099.Advance(player, s + 1);
				}
			} else if ((s == Q099.Ready) && (player.GetSkill((int) SkillName.Mining) >= Q099.MiningNeeded)) {
				Q099.Redeem((NPC) npc, player);
			}
		}
	}

	/// <summary>#1278 - Podivin's double click.</summary>
	public class E_Q099Podivin : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q099.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1278 - spk_q099_vyvrhel.</summary>
	public sealed class Q099Speech : AbstractSpeech {
		private static readonly string[] greetings = { "hi", "dobry", "zdravi", "den" };

		public new static void Bootstrap() {
			new Q099Speech("spk_q099_vyvrhel").Register();
		}

		private Q099Speech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, greetings) && QuestNpc.IsCloseEnough(npc, player)) {
				Q099.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
