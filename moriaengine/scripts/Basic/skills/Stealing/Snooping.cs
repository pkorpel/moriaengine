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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class SnoopingSkillDef : SkillDef {

		public SnoopingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		internal static PluginKey snoopedPluginKey = PluginKey.Acquire("_snoopedBackpacks_");

		private const int Counselor = 2, MarkReach = 2, TooFarFromMark = 501584, ChanceOfCrime = 60, KarmaLost = 10, KarmaFloor = -500;

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		//Sphere's Skill_Snooping, with Erebor's @start: the mark's Dexterity is what's to beat, a higher plevel can't be snooped
		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Container cnt = (Container) skillSeqArgs.Target1;
			Character mark = (Character) cnt.TopObj();
			if ((mark is PlayerVendor) || (mark is EreborVendor)) {
				self.SysMessage(Loc<SnoopingLoc>.Get(self.Language).notPlayerVendors);
				return TriggerResult.Cancel;
			}
			if (!InReach(self, cnt, mark) || (self.Plevel < mark.Plevel)) {
				return TriggerResult.Cancel;
			}
			int difficulty = ((self.Plevel >= Counselor) && (self.Plevel > mark.Plevel)) ? 0 : (mark.Dex * 10);
			skillSeqArgs.Success = (difficulty <= 0) || this.CheckSuccess(self, difficulty);
			return TriggerResult.Continue;
		}

		private static bool InReach(Character self, Container cnt, Character mark) {
			if (!self.CanReachWithMessage(cnt)) {
				return false;
			}
			Player player = mark as Player;
			if (((player != null) && (player.GameState == null)) || (Point2D.GetSimpleDistance(self, mark) > MarkReach)) {
				self.ClilocSysMessage(TooFarFromMark);
				return false;
			}
			return true;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Container cnt = (Container) skillSeqArgs.Target1;
			Character mark = (Character) cnt.TopObj();
			if (!InReach(self, cnt, mark)) {
				return;
			}
			CrimeSeen(self, mark, cnt, Loc<SnoopingLoc>.Get(self.Language).peeking);
			cnt.OpenTo(self);
			SnoopingPlugin sb = self.GetPlugin(snoopedPluginKey) as SnoopingPlugin;
			if (sb == null) {
				sb = (SnoopingPlugin) self.AddNewPlugin(snoopedPluginKey, SnoopingPluginDef.instance);
			}
			sb.Add(cnt);
			sb.Timer = SnoopingPlugin.duration;
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Container cnt = (Container) skillSeqArgs.Target1;
			Character mark = (Character) cnt.TopObj();
			if (self.Flag_InvisByMagic) {
				Reputation.OnCrime(self, mark); //#1243
				Notoriety.FlagCriminal(self);
			}
			self.SysMessage(Loc<SnoopingLoc>.Get(self.Language).failed);
			if (InReach(self, cnt, mark)) {
				CrimeSeen(self, mark, cnt, Loc<SnoopingLoc>.Get(self.Language).tryingToPeek);
			}
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>Sphere's CheckCrimeSeen. Everyone who sees it may notice, the mark more readily, and a skilled snoop less.
		/// Seen, it costs karma, and 1 time in 60 a witness takes it as a crime.</summary>
		internal static void CrimeSeen(Character self, Character mark, Container cnt, string action) {
			bool seen = false;
			foreach (AbstractCharacter other in self.GetMap().GetCharsInRange(self.X, self.Y, Globals.MaxUpdateRange)) {
				Character witness = other as Character;
				if ((witness == null) || (witness == self) || !witness.CanSeeLOS(self).Allow || !Notices(self, witness, witness == mark)) {
					continue;
				}
				witness.SysMessage(string.Format(Loc<SnoopingLoc>.Get(witness.Language).youNotice, self.Name, action, (witness == mark) ? "your" : (mark.Name + "'s"), cnt.Name));
				if (!witness.IsGM) {
					seen = true;
					if (Globals.dice.Next(ChanceOfCrime) == 0) {
						CrimeWitness.Notice(witness, self, mark);
					}
					if (witness == mark) {
						CrimeWitness.Protest(witness as NPC, self);
					}
				}
			}
			if (seen) {
				CrimeWitness.ChangeKarma(self, -KarmaLost, KarmaFloor);
			}
		}

		private static bool Notices(Character self, Character witness, bool isMark) {
			if ((witness.IsGM || self.IsGM) && (witness.IsGM != self.IsGM)) {
				return witness.IsGM;
			}
			int chance = 1000 + witness.GetSkill(SkillName.Snooping) - self.GetSkill(SkillName.Snooping);
			chance = isMark ? System.Math.Max(50, chance + (chance / 3)) : System.Math.Max(10, chance);
			return Globals.dice.Next(1000) <= chance;
		}
	}

	internal class SnoopingLoc : CompiledLocStringCollection<SnoopingLoc> {
		public string whatToSteal = "Co chces zkusit ukrast?";
		public string notPlayerVendors = "Nesmis okradat hracske vendory.";
		public string failed = "Sacovani se nezdarilo.";
		public string peeking = "peeking into";
		public string tryingToPeek = "attempting to peek into";
		public string youNotice = "You notice {0} {1} {2} {3}.";
	}

	[ViewableClass]
	public partial class SnoopingPlugin {

		internal static PluginKey snoopedPluginKey = PluginKey.Acquire("_snoopedBackpacks_");
		public static int duration = 180;

		public TriggerResult On_DenyPickupItem(DenyPickupArgs args) {
			Container conta = args.ManipulatedItem.Cont as Container;
			Sanity.IfTrueThrow(conta == null, "args.ManipulatedItem.Cont == null");

			if (this.Contains(conta)) {
				Character thief = args.PickingChar as Character;

				SkillSequenceArgs stealing = SkillSequenceArgs.Acquire(thief, SkillName.Stealing, args.ManipulatedItem, null, null, null, null);
				stealing.PhaseSelect();
				bool success = stealing.Success;
				//stealing.Dispose();

				if (!success) {
					args.Result = DenyResultMessages.Deny_ThatDoesNotBelongToYou;
					return TriggerResult.Cancel;
				}
			}

			return TriggerResult.Continue;
		}

		public void Add(Container cont) {
			if (this.snoopedBackpacks == null) {
				this.snoopedBackpacks = new LinkedList<Container>();
			}
			if (!this.snoopedBackpacks.Contains(cont)) {
				this.snoopedBackpacks.AddFirst(cont);
			}
		}

		public bool Contains(Container cont)
		{
			if (this.snoopedBackpacks == null) {
				return false;
			}
			return this.snoopedBackpacks.Contains(cont);
		}

		public void On_Timer() {
			foreach (Container cont in this.snoopedBackpacks) {
				if (OpenedContainers.HasContainerOpen((Character) this.Cont, cont).Allow) {
					this.Timer = duration;
					return;
				}
			}
			this.Delete();
		}
	}

	[ViewableClass]
	public partial class SnoopingPluginDef {
		public static readonly SnoopingPluginDef instance = (SnoopingPluginDef)
			new SnoopingPluginDef("p_snoopedBackpacks", "C#scripts", -1).Register(); //#1163 - never was
	}
}