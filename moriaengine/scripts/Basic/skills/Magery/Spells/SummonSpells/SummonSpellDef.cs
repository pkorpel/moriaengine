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
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #483/#485 - generic "summon a temporary creature controlled by the caster" mechanism,
	/// the one piece every summon-shaped spell needs (Blade Spirits/Summon Creature in Circle V,
	/// Energy Vortex/the 4 elementals/Summon Daemon in Circle VIII - build once here, author each
	/// as a thin `.def` entry on top rather than inventing this 6+ times). Modeled directly on
	/// WallSpellDef's own On_EffectGround shape - a ground-targeted spell that creates a Thing at
	/// the target location and gives it a lifetime, except the Thing is an NPC instance (via
	/// `SpellEffectItem.CheckPositionForItem`'s same fit-check isn't used - real Erebor-adjacent
	/// precedent for spawning a monster near a point, MonsterAmbushSystem.TryAmbush, just calls
	/// NPCDef.Create(location) directly with no fit check, so this follows that simpler
	/// established pattern instead) rather than a wall item, and its "duration" ends with the NPC
	/// being deleted rather than an item being removed.
	///
	/// Controlled-pet relationship reuses exactly what Taming already proved works
	/// (Character.ControlMaster + NPCAiTimer's existing Following AI state, ShouldFollowOwner
	/// defaults true) - no new pet-AI code needed, same precedent TamingSkillDef.On_Success
	/// already established.
	///
	/// Dispel integration is deliberately NOT done here - #486 (Character/Item.On_Dispell being
	/// empty virtuals) is a separate, not-yet-shipped phase. A summoned creature is tagged
	/// IsSummonedCreature so #486's fix has something to check for once it lands; until then, a
	/// summon can only be removed by its own despawn timer or normal death, not by casting Dispel
	/// on it - a known, deliberate gap, not an oversight.
	/// </summary>
	[ViewableClass]
	public class SummonSpellDef : DurableSpellDef {
		private static readonly TagKey isSummonedCreatureTK = TagKey.Acquire("_isSummonedCreature_");
		private static readonly TimerKey despawnTimerKey = TimerKey.Acquire("_summonDespawn_");

		private FieldValue summonChar;

		public SummonSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.summonChar = this.InitTypedField("summonChar", null, typeof(NPCDef));
		}

		/// <summary>The real monster content to summon - e.g. c_vortex_mage/c_elem_fire_m (the
		/// mage-tier summon variants that already exist in summons.def, confirmed during #483's
		/// research - NOT the overpowered wild monster chardefs that happen to share a similar
		/// name).</summary>
		public NPCDef SummonChar {
			get {
				return (NPCDef) this.summonChar.CurrentValue;
			}
			set {
				this.summonChar.CurrentValue = value;
			}
		}

		/// <summary>True for any character this mechanism created - checked (once #486 lands) so
		/// Dispel knows this NPC is removable, and usable generally to distinguish a real
		/// player/monster from a conjured one.</summary>
		public static bool IsSummonedCreature(Character character) {
			return character.GetTag(isSummonedCreatureTK) is bool flag && flag;
		}

		protected override void On_EffectGround(IPoint3D target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectGround(target, spellEffectArgs);

			NPCDef npcDef = this.SummonChar;
			if (npcDef == null) {
				return;
			}

			Character caster = spellEffectArgs.Caster;
			Point4D summonPoint = new Point4D(target.X, target.Y, target.Z, caster.M);
			NPC summon = (NPC) npcDef.Create(summonPoint);

			summon.ControlMaster = caster;
			summon.Flag_WarMode = false;
			summon.SetTag(isSummonedCreatureTK, true);

			int spellPower = spellEffectArgs.SpellPower;
			double durationSecs = this.GetDurationForValue(spellPower);
			summon.AddTimer(despawnTimerKey, new SummonDespawnTimer()).DueInSpan = TimeSpan.FromSeconds(durationSecs);
		}

		[SaveableClass]
		[DeepCopyableClass]
		public class SummonDespawnTimer : BoundTimer {
			[DeepCopyImplementation]
			[LoadingInitializer]
			public SummonDespawnTimer() {
			}

			protected override void OnTimeout(TagHolder cont) {
				cont.Delete();
			}
		}
	}
}
