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
	/// #1270 - Erebor's q009 "Mistr krejci" (questy/q009_mistr_krejci.scp): the tailor Talathwen, going bankrupt, sells her family's
	/// ring to a flatterer for a cashier's cheque of exactly 5,000,000 gold, and has it ready "tomorrow": Erebor's 86400 of serv.time,
	/// which counts tenths, so 2.4 hours. Asking early puts it off by 18 minutes, a polite leave brings it 6 minutes nearer. The ring
	/// gives its owner +10.0 Tailoring at Tailoring 100, refuses its owner below that and anyone else.
	/// <para>Erebor's tags q009_krejci (1 = bring the cheque, 2 = wait, 3 = done) and q009_krejci_ringtime. It had no journal page; this
	/// one has its own short texts.</para>
	/// </summary>
	public static class Q009 {
		public const string TailorDef = "c_q009_krejci";
		public const string RingDef = "i_q009_ring_tailor";
		public const int Price = 5000000;
		public const int WaitSeconds = 8640, NagSeconds = 1080, PoliteSeconds = 360; //Erebor's 86400, 10800 and 3600 tenths
		public const int RingBonus = 100; //tenths, Erebor's tailoring+100

		private static readonly TagKey stageTK = TagKey.Acquire("_q009Krejci_");
		private static readonly TagKey readyAtTK = TagKey.Acquire("_q009RingTime_");
		internal static readonly TagKey RingOwnerTK = TagKey.Acquire("_q009RingOwner_");
		internal static readonly TagKey RingBoostTK = TagKey.Acquire("_q009RingBoost_");

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Mistr krejci", 0x3000, GetJournalText, p => GetStage(p) == 3));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(TailorDef, SingletonScript<E_Q009Tailor>.Instance);
			QuestNpc.Attach(RingDef, SingletonScript<E_Q009Ring>.Instance); //#1475
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

		/// <summary>Seconds of the server's clock until the ring is ready (negative when it is). No time at all reads as ready, as
		/// Erebor's empty tag was 0, long past.</summary>
		public static double SecondsToRing(Player player) {
			object tag = player.GetTag(readyAtTK);
			return (tag == null) ? -1 : Convert.ToDouble(tag, CultureInfo.InvariantCulture) - Globals.TimeInSeconds;
		}

		internal static void SetReadyIn(Player player, double seconds) {
			player.SetTag(readyAtTK, Globals.TimeInSeconds + seconds);
		}

		internal static void PutOff(Player player, double seconds) {
			object tag = player.GetTag(readyAtTK);
			if (tag != null) {
				player.SetTag(readyAtTK, Convert.ToDouble(tag, CultureInfo.InvariantCulture) + seconds);
			}
		}

		internal static void ForgetRingTime(Player player) {
			player.RemoveTag(readyAtTK);
		}

		/// <summary>Erebor's findlayer(layer_pack).findid(i_cashiers_check): the first cheque in the pack, its bags too (Sphere's
		/// FINDID goes down into them), if it's worth exactly 5M.</summary>
		internal static CashiersItem PriceCheque(Player player) {
			CashiersItem cheque = (player.Backpack == null) ? null : FirstCheque(player.Backpack);
			return ((cheque != null) && (cheque.Value == Price)) ? cheque : null;
		}

		/// <summary>The first cashier's cheque, depth first (#1278: q099 asks too).</summary>
		internal static CashiersItem FirstCheque(Container container) {
			foreach (AbstractItem item in container) {
				if ((item is CashiersItem cheque) && (cheque.Def == ThingDef.GetByDefname(CashiersItem.CheckDef))) {
					return cheque;
				}
				if ((item is Container inside) && (inside.Count > 0)) { //depth first, as Sphere's ContentFind
					CashiersItem found = FirstCheque(inside);
					if (found != null) {
						return found;
					}
				}
			}
			return null;
		}

		internal static string GetJournalText(Player player) {
			switch (GetStage(player)) {
				case 1:
					return "Talathwen ti prenecha rodinny prsten krejcich, pokud ji prineses sek na 5 000 000 zlatych.";
				case 2:
					return "<?src.sex(Zaplatil,Zaplatila)?> jsi Talathwen za prsten. Ma ho pro tebe upravit, zastav se u ni zitra. Kdo ji otravuje driv, ceka dele.";
				case 3:
					return "Talathwen ti sverila rodinny prsten. Krejcimu mistrovi zvedne zrucnost.";
				default:
					return null;
			}
		}

		internal static void Talk(NPC npc, Player player) {
			if (!player.Flag_Dead) {
				npc.Dialog(player, SingletonScript<D_Q009Krejci>.Instance, npc);
			}
		}

		#region the ring
		/// <summary>Erebor's i_q009_ring_tailor @equip refusals: its owner at Tailoring 100 only.</summary>
		internal static bool RefusesEquip(Item item, Character ch) {
			if ((item.Def != ThingDef.GetByDefname(RingDef)) || !(ch is Player player)) {
				return false;
			}
			Player owner = item.GetTag(RingOwnerTK) as Player;
			if (owner != player) {
				player.RedMessage("Tento prsten ti nepadne. Je na nem vyryto jmeno " + ((owner == null) ? "nekoho jineho" : owner.Name));
				return true;
			}
			if (player.GetSkill((int) SkillName.Tailoring) <= 999) {
				player.RedMessage("K manipulaci s timto prstenem nemas dost zkusenosti.");
				return true;
			}
			return false;
		}

		/// <summary>@equip: +10 Tailoring, a skill modifier the ring remembers it gave.</summary>
		internal static void OnEquip(Character ch, Item item) {
			if ((item.Def != ThingDef.GetByDefname(RingDef)) || !(ch is Player player) || (item.GetTag(RingOwnerTK) != player)
					|| (player.GetSkill((int) SkillName.Tailoring) <= 999) || (item.GetTag(RingBoostTK) != null)) {
				return;
			}
			player.ModifySkillValue(SkillName.Tailoring, RingBonus);
			item.SetTag(RingBoostTK, player);
			player.SysMessage("Mistrovstvi spjate s timto prstenem ti zveda obratnost pri vyrobe odevu.");
		}

		internal static void OnUnequip(Character ch, Item item) {
			if (item.GetTag(RingBoostTK) is Character boosted) {
				item.RemoveTag(RingBoostTK);
				if (!boosted.IsDeleted) {
					boosted.ModifySkillValue(SkillName.Tailoring, -RingBonus);
				}
			}
		}
		#endregion the ring
	}

	/// <summary>#1475 - i_q009_ring_tailor's own @equip and @unequip.</summary>
	public class E_Q009Ring : CompiledTriggerGroup {
		public void On_Equip(Item self, ItemInCharArgs args) {
			if (args.Cont is Character ch) {
				Q009.OnEquip(ch, self);
			}
		}

		public void On_UnEquip(Item self, ItemInCharArgs args) {
			if (args.Cont is Character ch) {
				Q009.OnUnequip(ch, self);
			}
		}
	}

	/// <summary>#1270 - Talathwen, Erebor's f_q009_krejci and d_q009_krejci_1..6.</summary>
	public class D_Q009Krejci : QuestDialogDef {
		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			switch (Q009.GetStage(player)) {
				case 3:
					Q009Pages.Krejci6(d);
					break;
				case 2:
					if (Q009.SecondsToRing(player) < 0) {
						Q009Pages.Krejci5(d);
					} else {
						Q009.PutOff(player, Q009.NagSeconds); //Erebor's f_q009_krejci: asking before it's ready puts it off
						Q009Pages.Krejci4(d);
					}
					break;
				case 1:
					if (Q009.PriceCheque(player) != null) {
						Q009Pages.Krejci3(d);
					} else {
						Q009Pages.Krejci2(d);
					}
					break;
				default:
					Q009Pages.Krejci1(d);
					break;
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int stage = Q009.GetStage(player);
			if (button.EndsWith(":2", StringComparison.Ordinal)) { //"Obchodovat?" in every dialog
				npc.Say("U mne najdes vse, co zkusena svadlena potrebuje.");
				player.Dialog(SingletonScript<Dialogs.D_VendorShop>.Instance);
				return;
			}
			if ((button == "krejci_1:1") && (stage == 0)) {
				Q009.Advance(player, 1);
				npc.Say("Dones mi tedy ten sek a dohodneme se.");
			} else if ((button == "krejci_3:1") && (stage == 1)) {
				CashiersItem cheque = Q009.PriceCheque(player);
				if (cheque == null) {
					npc.Say("Kam jsi dal ten sek ?");
					return;
				}
				cheque.Delete();
				Q009.SetReadyIn(player, Q009.WaitSeconds);
				Q009.Advance(player, 2);
				npc.Say("Tedy dohodnuto, stav se zde zitra, prsten by mel byt pripraven.");
			} else if ((button == "krejci_4:1") && (stage == 2)) {
				npc.Say("Pokusim se upravu prstenu uspisit. Bohuzel mne ted stale nekdo otravuje a to mi protahuje praci...");
				Q009.PutOff(player, -Q009.PoliteSeconds);
			} else if ((button == "krejci_5:1") && (stage == 2) && (Q009.SecondsToRing(player) < 0)) {
				npc.Say("... a to je cele. Opatruj ho velmi peclive. Verim, ze u tebe bude v dobrych rukou");
				if (player.GetSkill((int) SkillName.Tailoring) < 1000) {
					player.SysMessage("Vubec jsi " + ((player.Gender == Gender.Female) ? "nepochopila" : "nepochopil") + ", o cem ta svadlena mluvila... snad to nebyly vyhozene penize.");
				}
				Q009.ForgetRingTime(player);
				Item ring = (Item) ThingDef.GetByDefname(Q009.RingDef).Create(player.Backpack);
				ring.SetTag(Q009.RingOwnerTK, player);
				Q009.Advance(player, 3);
			} else if (button == "krejci_6:1") {
				npc.Say("Rada te tu zase nekdy uvidim !");
			}
		}
	}

	/// <summary>#1270 - Talathwen's double click (Erebor's spk_q009_krejci only spoke; a click does the same).</summary>
	public class E_Q009Tailor : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q009.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1270 - spk_q009_krejci.</summary>
	public sealed class Q009Speech : AbstractSpeech {
		private static readonly string[] greetings = { "ahoj", "zdrav", "ukol" };

		public new static void Bootstrap() {
			new Q009Speech("spk_q009_krejci").Register();
		}

		private Q009Speech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, greetings) && QuestNpc.IsCloseEnough(npc, player)) {
				Q009.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
