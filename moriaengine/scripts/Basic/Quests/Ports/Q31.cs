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
	/// #1274 - Erebor's q31 "Kuchar Mrkvicka" (questy/q31_kuchar.scp): the cook, Gondor's c_q31_kuchar and Mordor's c_q31_kuchara,
	/// wants 50 dead birds for a rich client's roast. His bag (a collector, #1247) takes them from bodies of "Ptacek"; full, it buys
	/// a llama figurine and 50 experience. Turning him down, lying about Ariadne's liqueur or giving up loses him for good.
	/// <para>Erebor's char tags q31_kuchar (1 bringing birds, 2 done) and q31_neuspel (lost).</para>
	/// </summary>
	public static class Q31 {
		public const string GondorCookDef = "c_q31_kuchar", MordorCookDef = "c_q31_kuchara";
		public const string BagDef = "i_q31_kuchar_kolektor";
		public const string LlamaDef = "c_llama";
		public const int RewardExp = 50;

		private static readonly TagKey stageTK = TagKey.Acquire("_q31Kuchar_");
		private static readonly TagKey lostTK = TagKey.Acquire("_q31Neuspel_");

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Kuchar Mrkvicka", 0x3001, GetJournalText, p => IsLost(p) || (GetStage(p) == 2)));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(GondorCookDef, SingletonScript<E_Q31Cook>.Instance);
			QuestNpc.Attach(MordorCookDef, SingletonScript<E_Q31Cook>.Instance);
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

		public static bool IsLost(Player player) {
			return player.GetTag(lostTK) != null;
		}

		internal static void SetLost(Player player, bool lost) {
			if (lost) {
				player.SetTag(lostTK, 1);
			} else {
				player.RemoveTag(lostTK);
			}
		}

		/// <summary>He's lost for good: Erebor's q31_neuspel, and q31_kuchar removed.</summary>
		internal static void Lose(Player player) {
			SetStage(player, 0);
			SetLost(player, true);
			QuestJournal.Report(player, Journal);
		}

		internal static readonly string[] JournalTexts = {
			"<?src.sex(Slibil,Slibila)?> jsi pomoci Kuchari, ktery stale ceka na dodavku ptacku.",
			"<?src.sex(Pomohl,Pomohla)?> jsi kuchari a ten ted opet muze varit sve speciality.",
			"<?src.sex(Rozzlobil,Rozzlobila)?> jsi kuchare tak, ze se s tebou odmita bavit. Ten uz s tebou do konce zivota nepromluvi..."
		};

		internal static string GetJournalText(Player player) {
			int stage = GetStage(player);
			if ((stage == 1) || (stage == 2)) {
				return JournalTexts[stage - 1];
			}
			return IsLost(player) ? JournalTexts[2] : null;
		}

		/// <summary>f_q31_kuchar / f_q31_kuchar_mordor: each cook shrieks at the other realm (Erebor picked the function by the
		/// speaker's realm, not by the cook, so either cook served Gondor; here each serves its own side and those of none).</summary>
		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			string refusal = Refusal(npc, player);
			if (refusal != null) {
				npc.Say(refusal);
			} else {
				npc.Dialog(player, SingletonScript<D_Q31Kuchar>.Instance, npc);
			}
		}

		/// <summary>What the cook says instead of talking, or null.</summary>
		internal static string Refusal(NPC npc, Player player) {
			bool gondorCook = npc.Def == ThingDef.GetByDefname(GondorCookDef);
			if (gondorCook && (player.Realm == Realm.Mordor)) {
				return "AAAAAA, Mordorani !!! Zachran se kdo muzes !!!";
			} else if (!gondorCook && (player.Realm == Realm.Gondor)) {
				return "AAAAAA, Gondori !!! Zachran se kdo muzes !!!";
			} else if (player.Level < 1) {
				return "Promin, ale v tuto chvili se nemam s tebou o cem bavi. Prijd, az budes silnejsi!";
			} else if (IsLost(player)) {
				return "S tebou se uz nemam o cem bavit. Nerus me prosim mam praci!";
			}
			return null;
		}

		/// <summary>i_pet_llama: a figurine that brings a llama out as the hero's own.</summary>
		internal static PetFigurine GiveLlama(Player player) {
			PetFigurine figurine = (PetFigurine) ThingDef.GetByDefname("i_pet_figurine").Create(player.Backpack);
			figurine.Hold((NPCDef) ThingDef.GetByDefname(LlamaDef));
			return figurine;
		}
	}

	/// <summary>#1274 - Kuchar Mrkvicka, Erebor's d_q31_kuchar_1..3.</summary>
	public class D_Q31Kuchar : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3001;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			switch (Q31.GetStage(player)) {
				case 1:
					Q31Pages.Kuchar2(d);
					break;
				case 2:
					Q31Pages.Kuchar3(d);
					break;
				default:
					Q31Pages.Kuchar1(d);
					break;
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int stage = Q31.GetStage(player);
			if (Q31.IsLost(player)) {
				return;
			}
			if (stage == 0) {
				if (button == "kuchar_1:1") {
					npc.Say("Doufam ze se vratis co nejdrive, hodne stesti.");
					Q31.SetStage(player, 1);
					player.RedMessage("Prijmas ukol");
					Q006.GiveBag(player, Q31.BagDef);
					QuestJournal.Report(player, Q31.Journal);
				} else if (button == "kuchar_1:2") {
					Q31.Lose(player);
					player.SysMessage("Asi jsi kuchare nastval, vypada nasupene");
				} else if (button == "kuchar_1:3") {
					npc.Say("Nesnasim lhare a lidi kteri si vymysleji. Jdi prosim, mam praci.");
					Q31.Lose(player);
				}
			} else if (stage == 1) {
				if (button == "kuchar_2:1") {
					if (!Collectors.IsFull(player, Q31.BagDef)) {
						player.SysMessage("Jeste nemas 50 ptacku!");
						return;
					}
					Collectors.FindIn(player, Q31.BagDef).Delete();
					Q31.SetStage(player, 2);
					player.SysMessage("Skvele, splnil jsi kucharuv ukol. Zaslouzis si odmenu.");
					npc.Say("Dekuji ti mnohokrat! Tady je tva odmena.");
					LevelingSystem.AddExperience(player, Q31.RewardExp);
					Q31.GiveLlama(player);
					QuestJournal.Report(player, Q31.Journal);
				} else if (button == "kuchar_2:2") {
					npc.Say("Dobra ale pospes si prosim, jiz neni mnoho casu.");
				} else if (button == "kuchar_2:3") {
					npc.Say("HMMMMMMMMM ... pripravil jsi mne o mnoho casu. At uz te tu nevidim ! A ten pytel na ty ptaky si vezmu!");
					Collector bag = Collectors.FindIn(player, Q31.BagDef);
					if (bag != null) {
						bag.Delete();
					}
					Q31.Lose(player);
				} else if (button == "kuchar_2:4") { //a fresh bag; Erebor swapped only one still held, here a lost one comes back too
					Collector bag = Collectors.FindIn(player, Q31.BagDef);
					if (bag != null) {
						bag.Delete();
					}
					Q006.GiveBag(player, Q31.BagDef);
					player.RedMessage("Opraveno");
				}
			} else if ((stage == 2) && (button == "kuchar_3:1")) {
				npc.Say("Mej se a nekdy se zastav...");
			}
		}
	}

	/// <summary>#1274 - the cooks' double click.</summary>
	public class E_Q31Cook : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q31.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1274 - spk_q31_kuchar (both cooks).</summary>
	public sealed class Q31Speech : AbstractSpeech {
		private static readonly string[] greetings = { "ahoj", "zdravim", "dobry den" };

		public new static void Bootstrap() {
			new Q31Speech("spk_q31_kuchar").Register();
		}

		private Q31Speech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, greetings) && QuestNpc.IsCloseEnough(npc, player)) {
				Q31.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
