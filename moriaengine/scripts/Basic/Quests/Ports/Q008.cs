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
	/// #1274 - Erebor's q008 "Black Mustang" (questy/q008_black.scp, _chars, _items): the old horse breeder Ancam lets a ranger with
	/// Taming and Animal Lore 95 try his black mustang (C_A_MUSTANG_BLACK, a magic mount, #925) if they kill the four snow-island monsters
	/// and fill his crate with reagents. The corral gate opens for them; inside, a pad locks it behind them ("no way back") until the
	/// mustang is tamed. Repeatable whenever a new mustang has come.
	/// <para>Erebor's char tags q008_black (1 the crate, 2 the gate is theirs, 3 inside, 4 out with the mustang), q008_black_a (handed in
	/// before a mustang came), q008_black_monster_1..4, q008_black_rejected (a rude answer: never again). Erebor had no journal page;
	/// this one has its own short texts.</para>
	/// </summary>
	public static class Q008 {
		public const string AncamDef = "c_q008_old_man", CrateDef = "i_q008_benda_regy", MustangDef = "c_a_mustang_black";
		public const string GateDef = "i_q008_vrata_1", PadInsideDef = "i_q008_pad_closeDoor_inside", PadOutsideDef = "i_q008_pad_closeDoor_outside";
		public static readonly string[] MonsterDefs = { "c_q008_elem_snow", "c_q008_elem_ice", "c_q008_eye_snow", "c_q008_eye_ice" };
		public const int SkillNeeded = 950, ReagentsNeeded = 2500;
		public const int CorralRange = 30, GateRange = 10;
		public const int WaitExp = 400, CorralExp = 100;

		private static readonly TagKey stageTK = TagKey.Acquire("_q008Black_");
		private static readonly TagKey handedInTK = TagKey.Acquire("_q008BlackA_");
		private static readonly TagKey rejectedTK = TagKey.Acquire("_q008Rejected_");
		private static readonly TagKey[] killTKs = { TagKey.Acquire("_q008Monster1_"), TagKey.Acquire("_q008Monster2_"), TagKey.Acquire("_q008Monster3_"), TagKey.Acquire("_q008Monster4_") };
		internal static readonly TagKey ReagentsTK = TagKey.Acquire("_q008Reagents_"); //on the crate: Erebor's mnozstvi_regu
		internal static readonly TagKey FullTK = TagKey.Acquire("_q008Full_"); //on the crate: Erebor's nasbirano
		internal static readonly TagKey UnlockedTK = TagKey.Acquire("_q008Unlocked_"); //on the gate: Erebor's more1 = 0

		public static QuestJournalEntry Journal { get; private set; }

		public static void Bootstrap() {
			Journal = QuestJournal.Register(new QuestJournalEntry("Cerny mustang", 0x3059, GetJournalText, p => GetStage(p) == 4));
		}

		//called by ClassManager on every startup/recompile, see ExpertQuest.Init
		public static void Init() {
			QuestNpc.Attach(AncamDef, SingletonScript<E_Q008Ancam>.Instance);
			QuestNpc.Attach(CrateDef, SingletonScript<E_Q008Crate>.Instance);
			QuestNpc.Attach(PadInsideDef, SingletonScript<E_Q008Pad>.Instance);
			QuestNpc.Attach(PadOutsideDef, SingletonScript<E_Q008Pad>.Instance);
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
			QuestJournal.Report(player, Journal);
		}

		public static bool HandedIn(Player player) {
			return player.GetTag(handedInTK) != null;
		}

		internal static void SetHandedIn(Player player, bool value) {
			if (value) {
				player.SetTag(handedInTK, 1);
			} else {
				player.RemoveTag(handedInTK);
			}
		}

		public static bool IsRejected(Player player) {
			return player.GetTag(rejectedTK) != null;
		}

		internal static void Reject(Player player) {
			player.SetTag(rejectedTK, 1);
			player.RemoveTag(stageTK);
		}

		public static bool Killed(Player player, int index) {
			return player.GetTag(killTKs[index]) != null;
		}

		internal static void SetKilled(Player player, int index, bool value) {
			if (value) {
				player.SetTag(killTKs[index], 1);
			} else {
				player.RemoveTag(killTKs[index]);
			}
		}

		internal static bool AllKilled(Player player) {
			for (int i = 0; i < killTKs.Length; i++) {
				if (!Killed(player, i)) {
					return false;
				}
			}
			return true;
		}

		internal static void ForgetKills(Player player) {
			for (int i = 0; i < killTKs.Length; i++) {
				SetKilled(player, i, false);
			}
		}

		/// <summary>The four quest monsters' @Death (NPC.On_Death): a tag on the killer (Erebor's act), whatever experience they're worth.</summary>
		internal static void OnKillCredit(Player player, Character victim) {
			if (player == null) {
				return;
			}
			for (int i = 0; i < MonsterDefs.Length; i++) {
				if (victim.Def == ThingDef.GetByDefname(MonsterDefs[i])) {
					SetKilled(player, i, true);
				}
			}
		}
		#endregion tags

		#region the crate
		internal static Item FindCrate(Player player) {
			return (player.Backpack == null) ? null : FindIn(player.Backpack, ThingDef.GetByDefname(CrateDef));
		}

		private static Item FindIn(Container container, ThingDef def) {
			foreach (AbstractItem item in container) {
				if (item.Def == def) {
					return (Item) item;
				}
				if ((item is Container inside) && (inside.Count > 0)) {
					Item found = FindIn(inside, def);
					if (found != null) {
						return found;
					}
				}
			}
			return null;
		}

		internal static bool CrateFull(Player player) {
			Item crate = FindCrate(player);
			return (crate != null) && (crate.GetTag(FullTK) != null);
		}

		internal static int CrateReagents(Item crate) {
			object tag = crate.GetTag(ReagentsTK);
			return (tag == null) ? 0 : Convert.ToInt32(tag, CultureInfo.InvariantCulture);
		}

		/// <summary>i_q008_benda_regy's @TargOn_Item with a reagent: under 2500 it counts them; the deposit after that fills the crate
		/// (Erebor's literal rule: its reagents go in uncounted). Any reagent does.</summary>
		internal static void Deposit(Item crate, Player player, Item reagent) {
			if (crate.GetTag(FullTK) == null) {
				int had = CrateReagents(crate);
				if (had < ReagentsNeeded) {
					crate.SetTag(ReagentsTK, had + reagent.Amount);
				} else {
					crate.SetTag(FullTK, 1);
					crate.RemoveTag(ReagentsTK);
				}
			}
			reagent.Delete();
			player.SysMessage(((player.Gender == Gender.Female) ? "Vlozila" : "Vlozil") + " jsi do bedny regy.");
		}

		internal static void GiveCrate(Player player) {
			Item crate = FindCrate(player);
			if (crate == null) {
				Q006.GiveBag(player, CrateDef); //Erebor's attr 014: newbie and fixed in the pack
			}
		}
		#endregion the crate

		#region the mustang and the corral
		/// <summary>Erebor's mustspawnid link: the last mustang the corral's spawn made, still in the world and untamed. Here: an untamed
		/// black mustang within CorralRange of `near` (Ancam or the gate).</summary>
		public static bool MustangThere(Thing near) {
			ThingDef def = ThingDef.GetByDefname(MustangDef);
			foreach (AbstractCharacter ch in near.GetMap().GetCharsInRange(near.X, near.Y, CorralRange)) {
				if ((ch.Def == def) && (ch is Character mustang) && !mustang.Flag_Dead && (mustang.ControlMaster == null)) {
					return true;
				}
			}
			return false;
		}

		internal static Item GateNear(Thing near) {
			ThingDef def = ThingDef.GetByDefname(GateDef);
			foreach (AbstractItem item in near.GetMap().GetItemsInRange(near.X, near.Y, GateRange)) {
				if (item.Def == def) {
					return (Item) item;
				}
			}
			return null;
		}

		internal static void CloseGate(Item gate) {
			gate.RemoveTag(UnlockedTK);
			if (gate.IsDoorOpen) {
				gate.UseDoor();
			}
		}

		/// <summary>i_q008_vrata_1's @UserDClick. True when it swings.</summary>
		internal static bool UseGate(Item gate, Player player) {
			int stage = GetStage(player);
			bool allowed;
			if (player.IsGM) {
				allowed = true;
			} else if (stage == 4) {
				allowed = false;
			} else if ((stage == 3) && !MustangThere(gate)) { //tamed: the way out
				gate.SetTag(UnlockedTK, 1);
				allowed = true;
			} else {
				allowed = gate.GetTag(UnlockedTK) != null; //stage 2 once the outside pad has unlocked it
			}
			if (!allowed) {
				player.SysMessage("Dvere jsou pevne zavrene.");
				return false;
			}
			gate.UseDoor();
			return true;
		}

		/// <summary>i_q008_pad_closeDoor_inside's @step.</summary>
		internal static void StepInside(Item pad, Player player) {
			Item gate = GateNear(pad);
			if (player.IsGM || (gate == null)) {
				return;
			}
			if (!MustangThere(gate)) {
				gate.SetTag(UnlockedTK, 1);
			} else if ((gate.GetTag(UnlockedTK) != null) && (GetStage(player) == 2)) { //Erebor's ran on anyone not at 3, sneaking in too
				CloseGate(gate);
				player.SysMessage("Zaviras za sebou branku.");
				player.SysMessage("Ted jiz neni cesty zpet!");
				SetStage(player, 3);
				LevelingSystem.AddExperience(player, CorralExp);
			}
		}

		/// <summary>i_q008_pad_closeDoor_outside's @step.</summary>
		internal static void StepOutside(Item pad, Player player) {
			Item gate = GateNear(pad);
			if (player.IsGM || (gate == null)) {
				return;
			}
			int stage = GetStage(player);
			if (!MustangThere(gate)) {
				if (stage == 3) {
					SetStage(player, 4);
				}
				CloseGate(gate);
			} else if (stage == 2) {
				if (gate.GetTag(UnlockedTK) == null) {
					player.SysMessage("Oteviras branku k mustangovi");
				}
				gate.SetTag(UnlockedTK, 1);
			} else {
				CloseGate(gate);
			}
		}
		#endregion the mustang and the corral

		internal static readonly string[] JournalTexts = {
			"Ancam ti dovoli zkusit ochocit jeho cerneho mustanga, pokud zabijes snezneho a ledoveho elementala, snezne a ledove oko na sneznych ostrovech a naplnis jeho bednu regy.",
			"Ancam ti otevrel cestu k mustangovi. Projdi brankou do ohrady a ochoc ho.",
			"Jsi v ohrade s cernym mustangem. Branka se za tebou zavrela, ven se dostanes, az ho ochocis.",
			"<?src.sex(Ochocil,Ochocila)?> jsi Ancamova cerneho mustanga. Az prijde novy, Ancam ti mozna dovoli zkusit to znovu."
		};

		internal static string GetJournalText(Player player) {
			int stage = GetStage(player);
			if ((stage < 1) || (stage > 4)) {
				return HandedIn(player) ? "Ancam ma tve regy. Ceka, az se objevi novy cerny mustang." : null;
			}
			return JournalTexts[stage - 1];
		}

		/// <summary>f_q008_black_oldman_DClick.</summary>
		internal static void Talk(NPC npc, Player player) {
			if (player.Flag_Dead) {
				return;
			}
			if (IsRejected(player)) {
				npc.Say("Nejsi zde vitan!");
				return;
			}
			if ((GetStage(player) == 2) && MustangThere(npc)) {
				npc.Say("Jdi za tim mustangem! Jen jdi ...");
				return;
			}
			npc.Dialog(player, SingletonScript<D_Q008Ancam>.Instance, npc);
		}

		/// <summary>Which of Erebor's d_q008_black_oldman_* the player sees: 1-10 or 0 for no_tamer.</summary>
		internal static int DialogNumber(NPC npc, Player player) {
			if (player.Profession != Professions.Ranger) {
				return 0;
			}
			bool there = MustangThere(npc);
			int stage = GetStage(player);
			if (stage >= 3) {
				return there ? 10 : 9;
			}
			if (stage == 2) {
				return 8; //with the mustang there, Talk only says "go"
			}
			if (HandedIn(player)) {
				return there ? 6 : 5;
			}
			if ((stage == 1) && AllKilled(player) && CrateFull(player)) {
				return there ? 7 : 4;
			}
			if (stage == 1) {
				return 3;
			}
			if ((player.GetRealSkillValue(SkillName.AnimalTaming) >= SkillNeeded) && (player.GetRealSkillValue(SkillName.AnimalLore) >= SkillNeeded)) {
				return there ? 2 : 1;
			}
			return 0;
		}
	}

	/// <summary>#1274 - Ancam, Erebor's d_q008_black_oldman_1..10 and _no_tamer.</summary>
	public class D_Q008Ancam : QuestDialogDef {
		protected override int Face {
			get {
				return 0x3059;
			}
		}

		protected override void Build(QuestDialogContent d, Character npc, Player player) {
			switch (Q008.DialogNumber((NPC) npc, player)) {
				case 1: Q008Pages.Ancam1(d); break;
				case 2: Q008Pages.Ancam2(d); break;
				case 3: Q008Pages.Ancam3(d); break;
				case 4: Q008Pages.Ancam4(d); break;
				case 5: Q008Pages.Ancam5(d); break;
				case 6: Q008Pages.Ancam6(d); break;
				case 7: Q008Pages.Ancam7(d); break;
				case 8: Q008Pages.Ancam8(d); break;
				case 9: Q008Pages.Ancam9(d); break;
				case 10: Q008Pages.Ancam10(d); break;
				default: Q008Pages.AncamNoTamer(d); break;
			}
		}

		protected override void OnButton(string button, Character npc, Player player) {
			int shown = Q008.DialogNumber((NPC) npc, player); //each button only from the dialog that has it
			if (((button == "ancam_2:1") && (shown == 2)) || ((button == "ancam_10:1") && (shown == 10))) {
				Q008.GiveCrate(player);
				Q008.SetStage(player, 1);
			} else if ((button == "ancam_2:2") && (shown == 2)) {
				npc.Say("Uz se tady nikdy neukazuj!");
				Q008.Reject(player);
			} else if ((button == "ancam_10:2") && (shown == 10)) {
				npc.Say("Tva hamiznost se mi prici! Uz se zde nikdy neukazuj. Lituji, ze jsem ti drive dovolil priblizit se k memu zivreti.");
				Q008.Reject(player);
			} else if ((button == "ancam_4:1") && (shown == 4)) { //the crate taken, the mustang still to come
				Item crate = Q008.FindCrate(player);
				if (crate != null) {
					crate.Delete();
				}
				Q008.SetHandedIn(player, true);
				Q008.ForgetKills(player);
				Q008.SetStage(player, 0);
			} else if ((button == "ancam_6:1") && (shown == 6)) {
				Q008.SetHandedIn(player, false);
				Q008.SetStage(player, 2);
				LevelingSystem.AddExperience(player, Q008.WaitExp);
				npc.Say("hodne stesti");
			} else if ((button == "ancam_7:1") && (shown == 7)) {
				Item crate = Q008.FindCrate(player);
				if (crate != null) {
					crate.Delete();
				}
				Q008.ForgetKills(player);
				Q008.SetStage(player, 2);
			}
		}
	}

	/// <summary>#1274 - Ancam's double click.</summary>
	public class E_Q008Ancam : CompiledTriggerGroup {
		public TriggerResult On_DClick(NPC self, Character clicker) {
			if (!(clicker is Player player)) {
				return TriggerResult.Continue;
			}
			if (QuestNpc.IsCloseEnough(self, player)) {
				Q008.Talk(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1274 - i_q008_benda_regy: a double click asks for a reagent to put in.</summary>
	public class E_Q008Crate : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				player.SysMessage("Jaky reagent chces pridat do Ancamovy bedny?");
				player.Target(SingletonScript<Targ_Q008Crate>.Instance, self);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1274 - the crate's cursor.</summary>
	public class Targ_Q008Crate : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			self.SysMessage("Tohle sem cpat nebudes... alespon ne zive.");
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			Item crate = parameter as Item;
			if ((crate == null) || crate.IsDeleted) {
				return TargetResult.Done;
			}
			if (!self.CanSeeLOS(targetted).Allow) {
				self.SysMessage("Nevidis na to, z tehle strany to nepujde");
			} else if (Point2D.GetSimpleDistance(self, targetted.TopObj()) > 3) {
				self.SysMessage("Je to prilis daleko.");
			} else if ((targetted == crate) || (targetted.Type == null) || (targetted.Type.Defname != "t_reagent")) {
				self.SysMessage("Jsi si temer jisty, ze tohle se Ancamovi hodit nebude.");
			} else {
				Q008.Deposit(crate, self, targetted);
			}
			return TargetResult.Done;
		}
	}

	/// <summary>#1274 - the corral gate, i_q008_vrata_1: a trigger group on the gate itself (setup adds it), so it runs before its
	/// locked door's own and no key is asked for.</summary>
	public class E_Q008Gate : CompiledTriggerGroup {
		public TriggerResult On_DClick(Item self, Character clicker) {
			if (clicker is Player player) {
				Q008.UseGate(self, player);
			}
			return TriggerResult.Cancel;
		}
	}

	/// <summary>#1274 - the corral's step pads.</summary>
	public class E_Q008Pad : CompiledTriggerGroup {
		public void On_Step(Item self, Character stepping) {
			if (!(stepping is Player player)) {
				return;
			}
			if (self.Def == ThingDef.GetByDefname(Q008.PadInsideDef)) {
				Q008.StepInside(self, player);
			} else {
				Q008.StepOutside(self, player);
			}
		}
	}

	/// <summary>#1274 - spk_q008_black_oldman.</summary>
	public sealed class Q008Speech : AbstractSpeech {
		private static readonly string[] greetings = { "hi", "dobry", "ahoj", "zdrav", "mustang", "black", "cerny" };

		public new static void Bootstrap() {
			new Q008Speech("spk_q008_black_oldman").Register();
		}

		private Q008Speech(string defname)
			: base(defname) {
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			if ((listener is NPC npc) && (speechArgs.Speaker is Player player) && (speechArgs.Speech != null)
					&& SkillTeacherSpeech.ContainsAny(speechArgs.Speech, greetings) && QuestNpc.IsCloseEnough(npc, player)) {
				Q008.Talk(npc, player);
			}
			return SpeechResult.IgnoredOrActedUpon;
		}
	}
}
