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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	partial class WeaponSkillTargetQueuePlugin {
		LinkedList<Character> targetQueue = new LinkedList<Character>();

		private new Character Cont {
			get {
				return (Character) base.Cont;
			}
		}

		[SteamFunction]
		public static void AddTarget(Character attacker, Character target) {
			AcquireCombatPlugin(attacker).AddTarget(target);
		}

		[SteamFunction]
		public static void RemoveTarget(Character attacker, Character target) {
			WeaponSkillTargetQueuePlugin p = attacker.GetPlugin(combatPluginPK) as WeaponSkillTargetQueuePlugin;
			if (p != null) {
				p.RemoveTarget(target);
			}
		}

		[SteamFunction]
		public static void FightCurrentTarget(Character self) {
			WeaponSkillTargetQueuePlugin p = self.GetPlugin(combatPluginPK) as WeaponSkillTargetQueuePlugin;
			if (p != null) {
				p.FightCurrentTarget();
			}
		}

		public void AddTarget(Character target) {
			LinkedListNode<Character> node = this.targetQueue.First;
			if (node != null) {
				if (target != this.targetQueue.First.Value) {
					while (node != null) {
						if (node.Value == target) {
							this.targetQueue.Remove(node);
							break;
						}
						node = node.Next;
					}
					this.targetQueue.AddFirst(target);
				}
			} else {
				this.targetQueue.AddFirst(target);
			}

			this.FightCurrentTarget();
		}

		public void RemoveTarget(Character target) {
			LinkedListNode<Character> node = this.targetQueue.First;
			while (node != null) {
				if (node.Value == target) {
					this.targetQueue.Remove(node);
					break;
				}
				node = node.Next;
			}
			this.FightCurrentTarget();
		}

		public void FightCurrentTarget() {
			if (this.targetQueue.Count > 0) {
				this.Timer = CombatSettings.instance.secondsToRememberTargets;
				Character target = null;
				while (true) {
					target = this.targetQueue.First.Value;
					if (!target.IsAliveAndValid) {
						this.targetQueue.RemoveFirst();
						target = null;
					} else {
						break;
					}
				}
				if (target != null) {
					SkillName skill = GetSkillNameByWeaponType(this.Cont.WeaponType);
					SkillSequenceArgs seq = SkillSequenceArgs.Acquire(this.Cont, skill, target, null, this.Cont.Weapon, null, null);
					seq.PhaseSelect();
					return;
				}
			}
			this.Delete();//also aborts the skill
		}

		public static SkillName GetSkillNameByWeaponType(WeaponType weapType) {
			switch (weapType) {
				case WeaponType.BareHands:
					return SkillName.Wrestling;
				case WeaponType.XBow:
					return SkillName.Marksmanship;
				case WeaponType.Bow:
					return SkillName.Archery;
				case WeaponType.TwoHandAxe:
				case WeaponType.TwoHandSword:
					return SkillName.Swords;
				case WeaponType.OneHandSword:
				case WeaponType.OneHandAxe:
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					return SkillName.Fencing;
				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt:
					return SkillName.Macing;
			}
			throw new SEException("weapType out of range");
		}

		public void On_WarModeChange() {
			if (!this.Cont.Flag_WarMode) {//
				this.Delete();
			}
		}

		public void On_UnAssign(Character cont) {
			if (cont.CurrentSkill is WeaponSkillDef) {
				cont.AbortSkill();
			}
		}

		public void On_Assign() {
			this.Timer = CombatSettings.instance.secondsToRememberTargets;
		}

		public void On_Timer() {
			this.Delete();
		}

		public void On_Death() {
			this.Delete();
		}

		public void On_NewPosition() {
			Character cont = this.Cont;
			SkillSequenceArgs skillSeq = SkillSequenceArgs.GetSkillSequenceArgs(cont);

			if (skillSeq != null) {
				if (skillSeq.SkillDef.Id == (int) SkillName.Marksmanship) {//nonrun archery
					cont.AbortSkill();
					this.FightCurrentTarget();
				} else if (skillSeq.SkillDef is WeaponSkillDef) {
					skillSeq.PhaseStroke();
				}
			}
		}

		public void On_ItemEnter(Character cont, Item i) {
			if (i is Weapon) {
				cont.InvalidateCombatWeaponValues();
				if (cont.CurrentSkill is WeaponSkillDef) {
					cont.AbortSkill();
					//this.FightCurrentTarget();
				}
			}
		}

		public void On_ItemLeave(Character cont, Item i) {
			if (i is Weapon) {
				cont.InvalidateCombatWeaponValues();
				if (cont.CurrentSkill is WeaponSkillDef) {
					cont.AbortSkill();
					//this.FightCurrentTarget();
				}
			}
		}

		private static PluginKey combatPluginPK = PluginKey.Acquire("_combatPlugin_");

		[SteamFunction]
		public static WeaponSkillTargetQueuePlugin AcquireCombatPlugin(Character self) {
			WeaponSkillTargetQueuePlugin p = self.GetPlugin(combatPluginPK) as WeaponSkillTargetQueuePlugin;
			if (p == null) {
				p = (WeaponSkillTargetQueuePlugin) self.AddNewPlugin(combatPluginPK, WeaponSkillTargetQueuePluginDef.instance);
			}
			return p;
		}
	}

	[ViewableClass]
	partial class WeaponSkillTargetQueuePluginDef {
		public static WeaponSkillTargetQueuePluginDef instance = (WeaponSkillTargetQueuePluginDef)
			new WeaponSkillTargetQueuePluginDef("p_weaponSkillTargetQueue", "C# scripts", -1).Register();
	}

}
