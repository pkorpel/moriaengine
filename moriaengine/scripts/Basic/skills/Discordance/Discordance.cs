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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's systems/skills/discordance.scp, its Enticement: a monster loses a quarter of the bard's skill
	/// in permille of everything, for 10 s plus a second for each 2.0 of skill. Hits, mana and stamina keep their proportion.</summary>
	[ViewableClass]
	public class DiscordanceSkillDef : SkillDef {
		internal static PluginKey effectPluginKey = PluginKey.Acquire("_discordanceEffect_");

		public DiscordanceSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (Bard.FindInstrument(self) == null) {
				self.SysMessage(Loc<BardLoc>.Get(self.Language).NoInstrumentAvailable);
			} else if (self is Player player) {
				self.SysMessage(Loc<DiscordanceLoc>.Get(self.Language).WhoToWeaken);
				player.Target(SingletonScript<Targ_Discordance>.Instance, skillSeqArgs);
			}
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (self.GetTimer(DiscordanceTimer.Key) != null) {
				self.SysMessage(Loc<DiscordanceLoc>.Get(self.Language).DontHurry);
			} else {
				this.Play(self, skillSeqArgs.Target1 as Character);
			}
			return TriggerResult.Cancel;
		}

		private void Play(Character self, Character target) {
			DiscordanceLoc texts = Loc<DiscordanceLoc>.Get(self.Language);
			NPC npc = target as NPC;
			if (target == null) {
				self.SysMessage(texts.OnlyMonsters);
			} else if ((npc == null) || (npc.GetSkill(SkillName.Discordance) > 0) || Bard.IsBoss(npc)) {
				self.SysMessage(texts.CantWeaken);
			} else if (Bard.Reach(this, self) < Bard.ExperienceOf(npc)) {
				self.SysMessage(texts.BeyondYou);
			} else {
				HidingSkillDef.Reveal(self);
				DiscordanceTimer timer = new DiscordanceTimer();
				timer.target = npc;
				timer.works = Bard.Plays(self, Bard.FindInstrument(self)) && (Bard.Power(this, self) >= Bard.ExperienceOf(npc));
				self.AddTimer(DiscordanceTimer.Key, timer).DueInSeconds = Bard.Delay(this, self);
			}
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

		/// <summary>stopscriptedskills.</summary>
		internal static void Spoil(Character self) {
			DiscordanceTimer timer = self.GetTimer(DiscordanceTimer.Key) as DiscordanceTimer;
			if (timer != null) {
				timer.works = false;
				timer.DueInSeconds = 0;
			}
		}
	}

	public class Targ_Discordance : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			return Start(targetted, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return Start(targetted, parameter);
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}

		private static TargetResult Start(Thing targetted, object parameter) {
			SkillSequenceArgs skillSeq = (SkillSequenceArgs) parameter;
			skillSeq.Target1 = targetted;
			skillSeq.PhaseStart();
			return TargetResult.Done;
		}
	}

	/// <summary>i_discordance_timer.</summary>
	[SaveableClass, DeepCopyableClass]
	public class DiscordanceTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_discordance_");

		[SaveableData, CopyableData]
		public NPC target;

		[SaveableData, CopyableData]
		public bool works;

		[LoadingInitializer, DeepCopyImplementation]
		public DiscordanceTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if ((this.target == null) || this.target.IsDeleted) {
				return;
			}
			DiscordanceLoc texts = Loc<DiscordanceLoc>.Get(self.Language);
			this.target.DeletePlugin(DiscordanceSkillDef.effectPluginKey);
			if (this.works) {
				self.SysMessage(texts.Weakened, Bandaging.ClassMessageColor);
				DiscordanceEffectPlugin plugin = (DiscordanceEffectPlugin) DiscordanceEffectPluginDef.instance.Create();
				plugin.discordEffectPower = self.GetSkill(SkillName.Discordance);
				this.target.AddPluginAsSimple(DiscordanceSkillDef.effectPluginKey, plugin);
				FightMemory.Record(this.target, self);
			} else {
				self.RedMessage(texts.DidntWork);
			}
		}
	}

	public partial class DiscordanceEffectPluginDef {
		public static readonly DiscordanceEffectPluginDef instance = (DiscordanceEffectPluginDef)
			new DiscordanceEffectPluginDef("p_discordanceEffect_", "C#scripts", -1).Register(); //#1163 - never was
	}

	[ViewableClass]
	public partial class DiscordanceEffectPlugin {
		private const int LeastSeconds = 10, SkillForASecond = 20, Permille = 1000;

		public void On_Assign() {
			Character cont = (Character) this.Cont;
			cont.Emote("looks weakened.");

			int lowerConst = this.discordEffectPower / 4;
			int hitsShare = Share(cont.Hits, cont.MaxHits), manaShare = Share(cont.Mana, cont.MaxMana), stamShare = Share(cont.Stam, cont.MaxStam);

			this.lowed_dex = (short) DiscordanceValueLower(cont.Dex, lowerConst);
			this.lowed_str = (short) DiscordanceValueLower(cont.Str, lowerConst);
			this.lowed_int = (short) DiscordanceValueLower(cont.Int, lowerConst);
			this.lowed_hits = (short) DiscordanceValueLower(cont.MaxHits, lowerConst);
			this.lowed_mana = (short) DiscordanceValueLower(cont.MaxMana, lowerConst);
			this.lowed_stam = (short) DiscordanceValueLower(cont.MaxStam, lowerConst);
			this.lowed_ei = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.EvalInt), lowerConst);
			this.lowed_magery = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Magery), lowerConst);
			this.lowed_resist = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.MagicResist), lowerConst);
			this.lowed_poison = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Poisoning), lowerConst);
			this.lowed_archer = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Archery), lowerConst);
			this.lowed_twohand = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Swords), lowerConst);
			this.lowed_mace = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Macing), lowerConst);
			this.lowed_onehand = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Fencing), lowerConst);
			this.lowed_wrestl = (ushort) DiscordanceValueLower(cont.GetSkill(SkillName.Wrestling), lowerConst);

			cont.Dex -= this.lowed_dex;
			cont.Str -= this.lowed_str;
			cont.Int -= this.lowed_int;
			cont.MaxHits -= this.lowed_hits;
			cont.MaxMana -= this.lowed_mana;
			cont.MaxStam -= this.lowed_stam;
			cont.ModifySkillValue(SkillName.EvalInt, -this.lowed_ei);
			cont.ModifySkillValue(SkillName.Magery, -this.lowed_magery);
			cont.ModifySkillValue(SkillName.MagicResist, -this.lowed_resist);
			cont.ModifySkillValue(SkillName.Poisoning, -this.lowed_poison);
			cont.ModifySkillValue(SkillName.Archery, -this.lowed_archer);
			cont.ModifySkillValue(SkillName.Swords, -this.lowed_twohand);
			cont.ModifySkillValue(SkillName.Macing, -this.lowed_mace);
			cont.ModifySkillValue(SkillName.Fencing, -this.lowed_onehand);
			cont.ModifySkillValue(SkillName.Wrestling, -this.lowed_wrestl);

			cont.Hits = (short) ((cont.MaxHits * hitsShare) / Permille);
			cont.Mana = (short) ((cont.MaxMana * manaShare) / Permille);
			cont.Stam = (short) ((cont.MaxStam * stamShare) / Permille);
			this.Timer = LeastSeconds + (this.discordEffectPower / SkillForASecond);
		}

		public void On_UnAssign(Character cont) {
			if (cont.Hits <= 0) {
				return;
			}
			cont.Emote("seems to be at full strength.");
			int hitsShare = Share(cont.Hits, cont.MaxHits), manaShare = Share(cont.Mana, cont.MaxMana), stamShare = Share(cont.Stam, cont.MaxStam);
			cont.Dex += this.lowed_dex;
			cont.Str += this.lowed_str;
			cont.Int += this.lowed_int;
			cont.MaxHits += this.lowed_hits;
			cont.MaxMana += this.lowed_mana;
			cont.MaxStam += this.lowed_stam;
			cont.ModifySkillValue(SkillName.EvalInt, this.lowed_ei);
			cont.ModifySkillValue(SkillName.Magery, this.lowed_magery);
			cont.ModifySkillValue(SkillName.MagicResist, this.lowed_resist);
			cont.ModifySkillValue(SkillName.Poisoning, this.lowed_poison);
			cont.ModifySkillValue(SkillName.Archery, this.lowed_archer);
			cont.ModifySkillValue(SkillName.Swords, this.lowed_twohand);
			cont.ModifySkillValue(SkillName.Macing, this.lowed_mace);
			cont.ModifySkillValue(SkillName.Fencing, this.lowed_onehand);
			cont.ModifySkillValue(SkillName.Wrestling, this.lowed_wrestl);
			cont.Hits = (short) ((cont.MaxHits * hitsShare) / Permille);
			cont.Mana = (short) ((cont.MaxMana * manaShare) / Permille);
			cont.Stam = (short) ((cont.MaxStam * stamShare) / Permille);
		}

		private static int Share(int value, int most) {
			return ((value == most) || (most == 0)) ? Permille : (value * Permille) / most;
		}

		private static int DiscordanceValueLower(int value, int lowerConst) {
			int lowedVal = ((value * lowerConst) / 1000);
			if (value < lowedVal) {
				lowedVal = value;
			}
			return lowedVal;
		}

		public void On_Timer() {
			this.Delete();
		}
	}

	[ViewableClass]
	public partial class DiscordanceEffectPluginDef {
	}

	internal class DiscordanceLoc : CompiledLocStringCollection<DiscordanceLoc> {
		public string WhoToWeaken = "Koho chces zkusit oslabit?";
		public string DontHurry = "Nespechej s pouzivanim dovednosti.";
		public string OnlyMonsters = "Zameruj jen monstra.";
		public string CantWeaken = "Tohle nemuzes oslabit.";
		public string BeyondYou = "Oslabeni teto prisery presahuje tve moznosti.";
		public string Weakened = "Oslabeni bylo uspesne.";
		public string DidntWork = "Oslabeni se nepovedlo.";
	}
}
