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

using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's systems/skills/taming.scp: a timer of 3-7 strokes of talk, not a skill stroke. An NPC's
	/// taming is its rolled skill (act.taming), a crook of the right wood is needed from the first animal on, and a
	/// spawned mount also teaches its tamer, see MountKnowledge. A pet is a ControlMaster, Sphere's memory_ipet.</summary>
	[ViewableClass]
	public class TamingSkillDef : SkillDef {
		internal const int Reach = 5;

		private const int ElfSpeakerSkill = 950;
		private static readonly TagKey tamedTK = TagKey.Acquire("tamed");

		//f_crook_test: above this much taming, a crook of this wood or better
		private static readonly KeyValuePair<int, string>[] crooks = {
			new KeyValuePair<int, string>(1200, "eben"), new KeyValuePair<int, string>(1000, "mahagon"),
			new KeyValuePair<int, string>(800, "teak"), new KeyValuePair<int, string>(600, "oak"),
			new KeyValuePair<int, string>(400, "chestnut"), new KeyValuePair<int, string>(0, "spruce")
		};

		private static readonly string[] phrases = {
			"Hodny {0}.", "Budes muj mazlicek.", "Pojd sem {0}.", "Budu te krmit {0}.", "Nikdy nebudes hladovet.",
			"Budu se o tebe pekne starat.", "Neublizim ti {0}.", "Budu na tebe hodny.", "Malej, hodnej, pojd ke mne.", "Tak je hodny.",
			"{0} pojd ke mne.", "No ty jsi ale krasne zvire!", "Mas hlad? Pojd sem pojd...", "Pojd bliz! Neboj se...", "{0} stuuuj !",
			"Klid, klid, taaak!", "U mne ti bude dobre.", "Pobez ke mne, nemas se ceho bat.", "Neboj se me, tak je hodny.", "Ty jsi ale krasavec.",
			"Hoou pojd bliz krasavce.", "No pojd sem {0}.", "Mam pro tebe krasne misto.", "Nemej strach, pojd bliz."
		};

		private static readonly string[] elfPhrases = {
			"Daro !", "Avon mathea le !", "Orenya linda cenwelyo.", "Nanye alassea omentielvanen.", "Nalye taura ohtar.",
			"Nalye vanima.", "Indolya na ve rava.", "Vanesselye sile calimave.", "Merelye apsa?", "Hauta!",
			"Tula sinome.", "Ava aista.", "Hilya inye.", "Sintanye tuolya mahtesse.", "Tula yo ni vantien ar eleninnar tirien."
		};

		public TamingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Player self = skillSeqArgs.Self as Player;
			if (self != null) {
				self.SysMessage(Loc<TamingLoc>.Get(self.Language).whatToTame);
				self.Target(SingletonScript<Targ_Tame>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Start(skillSeqArgs.Self, (Character) skillSeqArgs.Target1);
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>stopscriptedskills: the taming under way ends at once, or would: using Taming again takes the mark back.</summary>
		internal static void Spoil(Character self) {
			TamingTimer timer = self.GetTimer(TamingTimer.Key) as TamingTimer;
			if (timer != null) {
				timer.interrupted = true;
				timer.DueInSeconds = 0;
			}
		}

		//f_tamingstart
		private static void Start(Character self, Character target) {
			HidingSkillDef.Reveal(self);
			PlayerHunger.HurtIfStarving(self);
			TamingTimer running = self.GetTimer(TamingTimer.Key) as TamingTimer;
			if (running != null) {
				running.interrupted = false;
				self.SysMessage(Loc<TamingLoc>.Get(self.Language).mustWait);
			} else if (WorkLight.CheckWithMessage(self)) {
				Tame(self, target);
			}
		}

		//taming_TAME
		private static void Tame(Character self, Character target) {
			TamingLoc texts = Loc<TamingLoc>.Get(self.Language);
			NPC npc = target as NPC;
			int taming = target.GetSkill(SkillName.AnimalTaming);
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage(texts.cantSee);
			} else if (!HasCrookFor(self, taming)) {
				self.SysMessage(IsCrook(self.FindLayer(LayerNames.Hand2)) ? texts.betterCrook : texts.needCrook);
			} else if ((npc == null) || (taming == 0) || SummonSpellDef.IsSummonedCreature(npc)) {
				self.SysMessage(texts.cantTame);
			} else if (self.GetSkill(SkillName.AnimalTaming) < taming) {
				self.SysMessage(texts.tooWild);
			} else if (self.GetSkill(SkillName.AnimalLore) < npc.GetSkill(SkillName.AnimalLore)) {
				self.SysMessage(texts.noIdea);
			} else if (npc.ControlMaster != null) {
				self.SysMessage(texts.alreadyTame);
			} else if (ClothMaking.Count(npc.Backpack, (ItemDef) ThingDef.GetByDefname("i_rune_summon_creature")) > 0) {
				self.SysMessage(texts.devoted);
			} else if (Point2D.GetSimpleDistance(self, npc) > Reach) {
				self.SysMessage(texts.tooFar);
			} else if (Q008.RefusesTaming(self, npc)) { //#1274 - Ancam's corral mustang, only for the hero locked in with it
			} else {
				Talk(self, npc);
				TamingTimer timer = new TamingTimer();
				timer.target = npc;
				timer.strokes = Globals.dice.Next(TamingTimer.LeastStrokes, TamingTimer.MostStrokes + 1);
				self.AddTimer(TamingTimer.Key, timer);
				timer.PeriodInSeconds = TamingTimer.SecondsAStroke;
				timer.DueInSeconds = TamingTimer.SecondsAStroke;
			}
		}

		internal static void Talk(Character self, NPC npc) {
			Player player = self as Player;
			if ((player != null) && (player.Realm == Realm.Gondor)
					&& ((player.Race == Race.Elf) || (player.GetSkill(SkillName.AnimalTaming) > ElfSpeakerSkill))) {
				self.Say(elfPhrases[Globals.dice.Next(elfPhrases.Length)]);
			} else {
				self.Say(string.Format(CultureInfo.InvariantCulture, phrases[Globals.dice.Next(phrases.Length)], npc.Name));
			}
		}

		private static bool IsCrook(AbstractItem held) {
			return (held is Item item) && CraftingSkillDef.IsOfType(item, "t_weapon_mace_crook");
		}

		//f_crook_test and hasCrook
		private static bool HasCrookFor(Character self, int taming) {
			if (taming <= 0) {
				return true;
			}
			if (self.IsGM) {
				return true;
			}
			AbstractItem held = self.FindLayer(LayerNames.Hand2);
			if (!IsCrook(held)) {
				return false;
			}
			string wood = System.Array.Find(crooks, crook => taming > crook.Key).Value;
			return ConstantOf("crook_" + held.Def.PrettyDefname) >= ConstantOf("material_" + wood);
		}

		private static int ConstantOf(string name) {
			Constant constant = Constant.GetByName(name);
			return (constant == null) ? 0 : System.Convert.ToInt32(constant.Value, CultureInfo.InvariantCulture);
		}

		/// <summary>flag_spawned: its spawn gem, null when it has none or has left it.</summary>
		internal static Thing SpawnOf(NPC npc) {
			return npc.GetTag(spawnGemTK) as Thing;
		}

		private static readonly TagKey spawnGemTK = TagKey.Acquire("spawnGem");

		internal static bool Succeeds(Character self, NPC npc) {
			return (Globals.dice.Next(1001) < (self.GetSkill(SkillName.AnimalTaming) - npc.GetSkill(SkillName.AnimalTaming))) || npc.HasTag(tamedTK);
		}

		//taming_success
		internal static void MakePet(Character self, NPC npc) {
			Remove(npc, "i_nores");
			npc.ControlMaster = self;
			npc.Flag_WarMode = false;
			PetHungerTimer.LoseFood(npc, 1);
			npc.Karma = 0;
			LeaveSpawn(npc);
			SkillSequenceArgs.AbortSkill(npc);
			self.SysMessage(Loc<TamingLoc>.Get(self.Language).success);
		}

		internal static bool Remove(NPC npc, string defname) {
			ItemDef mark = (ItemDef) ThingDef.GetByDefname(defname);
			foreach (AbstractItem item in npc.Backpack) {
				if (item.Def == mark) {
					item.Delete();
					return true;
				}
			}
			return false;
		}

		//memory_ispawned goes: the spawn counts it as gone and makes another
		private static void LeaveSpawn(NPC npc) {
			if (SpawnOf(npc) != null) {
				npc.TryTrigger(TriggerKey.Acquire("leaveSpawn"), new ScriptArgs());
				npc.RemoveTag(spawnGemTK);
				npc.RemoveTriggerGroup(TriggerGroup.GetByDefname("e_spawned_char"));
			}
		}
	}

	public class Targ_Tame : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}
	}

	/// <summary>i_taming: every 2 s more talk, then the roll. A hit, another skill or a spell ends it; a spawned
	/// animal led more than 70 tiles from home goes back. The word is taming_failure's.</summary>
	[SaveableClass, DeepCopyableClass]
	public class TamingTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_taming_");

		internal const int LeastStrokes = 3, MostStrokes = 7;
		internal const double SecondsAStroke = 2;

		private const int SpokenOneIn = 3, HomeLeash = 70, MountTaming = 300, QuickRetrySeconds = 3;
		private const int LeastBonus = 500, MostBonus = 600, LeastFailBonus = 300, MostFailBonus = 400, PlainBonus = 100;
		private static readonly TagKey lastAttemptTK = TagKey.Acquire("LastTamingAttemp");

		[SaveableData, CopyableData]
		public NPC target;

		[SaveableData, CopyableData]
		public int strokes;

		[SaveableData, CopyableData]
		public bool interrupted;

		[LoadingInitializer, DeepCopyImplementation]
		public TamingTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if (this.Act(self)) {
				this.Delete();
			}
		}

		//true when it's over
		private bool Act(Character self) {
			NPC npc = this.target;
			if ((npc == null) || npc.IsDeleted) {
				return true;
			}
			if (self.IsGM) {
				TamingSkillDef.MakePet(self, npc);
				return true;
			}
			if (TamingSkillDef.Remove(npc, "i_poresu")) {
				TamingSkillDef.MakePet(self, npc);
				return true;
			}
			Thing home = TamingSkillDef.SpawnOf(npc);
			if ((home != null) && ((System.Math.Abs(npc.X - home.X) > HomeLeash) || (System.Math.Abs(npc.Y - home.Y) > HomeLeash))) {
				npc.P(home);
			}
			TamingLoc texts = Loc<TamingLoc>.Get(self.Language);
			if (!self.CanSeeLOS(npc).Allow) {
				self.SysMessage(self.IsFemale ? texts.lostSightFemale : texts.lostSightMale);
			} else if (this.interrupted) {
				self.SysMessage(texts.interrupted);
				object last = self.GetTag(lastAttemptTK);
				if ((last is double earlier) && (earlier >= Globals.TimeInSeconds)) {
					Logger.WriteWarning(string.Concat(self.Name, " is taming ", npc.Name, " suspiciously fast at ", self.P().ToString(), ": gaining by fizzing on purpose?"));
				}
				self.SetTag(lastAttemptTK, Globals.TimeInSeconds + QuickRetrySeconds);
			} else if (npc.ControlMaster != null) {
				self.SysMessage(texts.alreadyTame);
			} else if (Point2D.GetSimpleDistance(self, npc) > TamingSkillDef.Reach) {
				self.SysMessage(string.Format(texts.targetTooFar, npc.Name));
			} else if (this.strokes > 0) {
				if (Globals.dice.Next(SpokenOneIn) == 0) {
					TamingSkillDef.Talk(self, npc);
				}
				this.strokes--;
				return false;
			} else {
				Finish(self, npc);
			}
			return true;
		}

		private static void Finish(Character self, NPC npc) {
			double bonus = PlainBonus;
			int taming = npc.GetSkill(SkillName.AnimalTaming);
			bool spawned = TamingSkillDef.SpawnOf(npc) != null;
			if (TamingSkillDef.Succeeds(self, npc)) {
				if ((taming >= MountTaming) && spawned) {
					bonus = Globals.dice.Next(LeastBonus, MostBonus + 1);
					if (self is Player player) {
						MountKnowledge.Learn(player, npc);
					}
				}
				TamingSkillDef.MakePet(self, npc);
			} else {
				if ((taming > MountTaming) && spawned) {
					bonus = Globals.dice.Next(LeastFailBonus, MostFailBonus + 1);
				}
				self.SysMessage(Loc<TamingLoc>.Get(self.Language).failed);
			}
			SkillGainSystem.TryGainSkillCustomFormula(self, SkillName.AnimalTaming, SkillAdvancementSettings.instance.tamingAdvRate, bonus);
		}
	}

	public class TamingLoc : CompiledLocStringCollection<TamingLoc> {
		public string whatToTame = "Ktere zvire si prejes ochocit?";
		public string mustWait = "You must wait to perform another action.";
		public string cantSee = "Nevidis na cil";
		public string betterCrook = "Na ochoceni potrebujes lepsi pasteveckou hul.";
		public string needCrook = "Na ochocovani zvere potrebujes pasteveckou hul.";
		public string cantTame = "Tohle nemuzes ochocit.";
		public string tooWild = "Zvire je prilis divoke.";
		public string noIdea = "Nemas nejmensi poneti, jak toto zvire ochocit.";
		public string alreadyTame = "Zvire je jiz ochocene.";
		public string devoted = "Tohle zvire je oddane nekomu jinemu a nelze jej ochocit.";
		public string tooFar = "Jsi prilis daleko.";
		public string targetTooFar = "{0} je prilis daleko.";
		public string lostSightMale = "Ztratil jsi svuj cil z dohledu.";
		public string lostSightFemale = "Ztratila jsi svuj cil z dohledu.";
		public string interrupted = "Ochocovani bylo preruseno.";
		public string failed = "Nepovedlo se ti zvire ochocit.";
		public string success = "Zda se, ze te zvire prijalo za sveho pana.";
	}
}
