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
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #778 - e_ovinovac ("strangler vine", Erebor systems/new_events.scp). A player who shoves
	/// past the vine (#767's @personalSpace) gets entangled: neither of them can move and the vine
	/// attacks. The grip loosens once the vine has taken HitsToBreak landed hits, when it dies, or
	/// when the two no longer share a tile. Erebor checked the last two with a 2s timer item on the
	/// victim; here they're checked whenever the victim tries to step (CheckCanMove), which is the
	/// only time the hold matters. One victim at a time; invulnerable characters aren't grabbed.
	/// </summary>
	[ViewableClass]
	public class StranglerVineAbilityDef : PassiveAbilityDef {
		private FieldValue hitsToBreak;

		public StranglerVineAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.hitsToBreak = this.InitTypedField("vineHitsToBreak", 5, typeof(int));
		}

		/// <summary>Landed hits the vine has to take before it lets go - Erebor counted 1 + 4.</summary>
		public int HitsToBreak {
			get { return (int) this.hitsToBreak.CurrentValue; }
			set { this.hitsToBreak.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static StranglerVineAbilityDef stranglerVineAbilityDef;
		internal static StranglerVineAbilityDef StranglerVineDef {
			get {
				if (stranglerVineAbilityDef == null) {
					stranglerVineAbilityDef = AbstractScript.GetByDefname("a_tevent_e_ovinovac") as StranglerVineAbilityDef;
				}
				return stranglerVineAbilityDef;
			}
		}

		/// <summary>From NPC.On_PersonalSpace - `pusher` just shoved past `vine`.</summary>
		public static void TryEntangle(NPC vine, Character pusher) {
			StranglerVineAbilityDef def = StranglerVineDef;
			Ability ability;
			if ((def == null) || !vine.HasAbility(def, out ability)) {
				return;
			}
			if (vine.Flag_Dead || !pusher.IsPlayer || pusher.Flag_Dead || pusher.Flag_Invulnerable) {
				return;
			}
			if ((vine.GetPlugin(VineHoldPlugin.key) is VineHoldPlugin holding) && holding.IsIntact()) {
				return; //already holding someone
			}
			VineHoldPlugin.Hold(vine, pusher, (int) def.HitsToBreak);
			pusher.RedMessage("You have been entangled and cannot move!");
			NPCAiTimer timer = NPCAiTimer.GetFor(vine);
			if (timer != null) {
				timer.EngageTarget(pusher);
			}
		}

		/// <summary>Pure: hits left after one more landed hit (never below 0).</summary>
		public static int HitsLeftAfterHit(int hitsLeft) {
			return Math.Max(0, hitsLeft - 1);
		}

		/// <summary>Pure: a hold lasts while both are alive and still share the same tile.</summary>
		public static bool IsHoldIntact(bool vineAlive, bool victimAlive, int vineX, int vineY, int vineM, int victimX, int victimY, int victimM) {
			return vineAlive && victimAlive && (vineX == victimX) && (vineY == victimY) && (vineM == victimM);
		}
	}

	/// <summary>#778, #1409 - on a strangler vine's victim: no step while the hold is intact ("You are entangled"); a hold
	/// that isn't any more is released at the step and the step goes ahead.</summary>
	public partial class EntangledPlugin {
		public static readonly PluginKey key = PluginKey.Acquire("_entangled_");

		public TriggerResult On_DenyMove(bool running) {
			Character self = (Character) this.Cont;
			if ((this.vine != null) && (this.vine.GetPlugin(VineHoldPlugin.key) is VineHoldPlugin hold) && (hold.Victim == self) && hold.IsIntact()) {
				self.SysMessage("You are entangled and cannot move!");
				return TriggerResult.Cancel;
			}
			VineHoldPlugin.Release(this.vine, self);
			return TriggerResult.Continue;
		}
	}

	public partial class EntangledPluginDef {
		public static readonly EntangledPluginDef instance =
			(EntangledPluginDef) new EntangledPluginDef("p_entangled", "C# scripts", -1).Register();
	}

	/// <summary>#778, #1409 - on the vine holding someone: it doesn't walk off while the hold is intact, and every landed
	/// blow on it (its own @getHit) counts toward letting go.</summary>
	public partial class VineHoldPlugin {
		public static readonly PluginKey key = PluginKey.Acquire("_vineHold_");

		public Character Victim {
			get {
				return this.victim;
			}
		}

		internal static void Hold(Character vine, Character victim, int hitsToBreak) {
			VineHoldPlugin hold = (VineHoldPlugin) VineHoldPluginDef.instance.Create();
			hold.victim = victim;
			hold.hitsLeft = hitsToBreak;
			vine.AddPlugin(key, hold);
			EntangledPlugin entangled = (EntangledPlugin) EntangledPluginDef.instance.Create();
			entangled.vine = vine;
			victim.AddPlugin(EntangledPlugin.key, entangled);
		}

		internal bool IsIntact() {
			Character vine = (Character) this.Cont;
			Character victim = this.victim;
			if ((vine == null) || vine.IsDeleted || (victim == null) || victim.IsDeleted) {
				return false;
			}
			if (!(victim.GetPlugin(EntangledPlugin.key) is EntangledPlugin entangled) || (entangled.vine != vine)) {
				return false;
			}
			return MonsterSpecialAttacks.IsHoldIntact(!vine.Flag_Dead, !victim.Flag_Dead, vine.X, vine.Y, vine.M, victim.X, victim.Y, victim.M);
		}

		internal static void Release(Character vine, Character victim) {
			if ((vine != null) && !vine.IsDeleted && (vine.GetPlugin(key) is VineHoldPlugin hold) && (hold.victim == victim)) {
				vine.DeletePlugin(key);
			}
			if ((victim != null) && !victim.IsDeleted && (victim.GetPlugin(EntangledPlugin.key) is EntangledPlugin entangled) && (entangled.vine == vine)) {
				victim.DeletePlugin(EntangledPlugin.key);
				if (!victim.Flag_Dead) {
					victim.SysMessage("The grip loosens...");
				}
			}
		}

		public TriggerResult On_DenyMove(bool running) {
			if (this.IsIntact()) {
				return TriggerResult.Cancel; //the vine holds on, it doesn't walk off
			}
			Release((Character) this.Cont, this.victim);
			return TriggerResult.Continue;
		}

		/// <summary>Was DamageManager.ProcessSwing's CountEntangleHit: every landed blow on the vine (#1411's @getHit, also one
		/// CauseDamage then refuses).</summary>
		public void On_GetHit(WeaponSwingArgs swingArgs) {
			this.hitsLeft = MonsterSpecialAttacks.HitsLeftAfterHit(this.hitsLeft);
			if (this.hitsLeft <= 0) {
				Release((Character) this.Cont, this.victim);
			}
		}
	}

	public partial class VineHoldPluginDef {
		public static readonly VineHoldPluginDef instance =
			(VineHoldPluginDef) new VineHoldPluginDef("p_vine_hold", "C# scripts", -1).Register();
	}
}
