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
		private static readonly TagKey summonerTK = TagKey.Acquire("_summoner_");
		private static readonly TimerKey despawnTimerKey = TimerKey.Acquire("_summonDespawn_");

		private FieldValue summonChar;

		public SummonSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.summonChar = this.InitTypedField("summonChar", null, typeof(NPCDef));
			this.summonChars = this.InitTypedField("summonChars", new NPCDef[0], typeof(NPCDef[]));
			this.hostile = this.InitTypedField("hostile", false, typeof(bool));
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

		private FieldValue summonChars;

		/// <summary>#483/#488 - Summon Creature's "random creature" pool (the mage-tier animal
		/// summons in summons.def - c_rat_giant_mage/c_bear_*_mage/c_scorpion_giant_mage/
		/// c_horse_brown_mage - distinct from the Circle VIII elemental/daemon summons, which are
		/// each their own dedicated single-SummonChar spell). Comma-separated list, same `Def[]`
		/// syntax ProfessionDef.AllowedSpells already establishes for this engine's .def parser.
		/// Takes priority over the singular SummonChar when non-empty; a spell with only one
		/// fixed creature (Blade Spirits) just uses SummonChar instead and leaves this empty.</summary>
		public NPCDef[] SummonChars {
			get {
				return (NPCDef[]) this.summonChars.CurrentValue;
			}
			set {
				this.summonChars.CurrentValue = value;
			}
		}

		private FieldValue hostile;

		/// <summary>#483/#488 - distinguishes Blade Spirits-shaped summons (false default's
		/// opposite) from Summon Creature-shaped ones. False (default, what Summon Creature
		/// uses): a normal controlled pet - ControlMaster set, same Taming-proven Following AI
		/// Phase 1 already relied on, won't fight until its owner gives a "kill"/"guard" order
		/// (NPC.ShouldAggroOn's own `this.ControlMaster != null` exclusion - tamed pets never
		/// pick fights on their own, by design, same as any real tamed animal in this engine).
		/// True (Blade Spirits): no ControlMaster at all, and AggroMode.Hostile is force-set on
		/// the instance - the existing autonomous-aggro path (NPCAiTimer.FindTarget ->
		/// ShouldAggroOn) then attacks any player it notices with no leash and no owner able to
		/// call it off, matching the "popular kill-all" warning in this spell's own old pre-port
		/// script comment.</summary>
		public bool Hostile {
			get {
				return (bool) this.hostile.CurrentValue;
			}
			set {
				this.hostile.CurrentValue = value;
			}
		}

		/// <summary>True for any character this mechanism created - checked (once #486 lands) so
		/// Dispel knows this NPC is removable, and usable generally to distinguish a real
		/// player/monster from a conjured one.</summary>
		public static bool IsSummonedCreature(Character character) {
			return character.GetTag(isSummonedCreatureTK) is bool flag && flag;
		}

		/// <summary>#1033 - creatures caster summoned that are near it, pets or hostile ones.</summary>
		public static int CountSummonsNear(Character caster) {
			int count = 0;
			foreach (AbstractCharacter ch in caster.GetMap().GetCharsInRange(caster.X, caster.Y, Globals.MaxUpdateRange)) {
				if (ch.GetTag(summonerTK) == caster) {
					count++;
				}
			}
			return count;
		}

		/// <summary>Marks a character as "summoned" for IsSummonedCreature's own check, same tag
		/// this spell's own On_EffectGround uses - exposed so OTHER summon-shaped mechanisms
		/// (e.g. MonsterSpecialAttacks.TryApplySummoning's own reinforcements) mark their spawns
		/// the same way, keeping them consistently recognizable to Dispel/e_summoneater too.</summary>
		public static void MarkAsSummonedCreature(Character character) {
			character.SetTag(isSummonedCreatureTK, true);
		}

		protected override void On_EffectGround(IPoint3D target, SpellEffectArgs spellEffectArgs) {
			base.On_EffectGround(target, spellEffectArgs);

			NPCDef[] pool = this.SummonChars;
			NPCDef npcDef = SummonMenu.TakeChoice(spellEffectArgs.Caster, this) ?? ((pool.Length > 0) ? pool[new Random().Next(pool.Length)] : this.SummonChar);
			if (npcDef == null) {
				return;
			}

			Character caster = spellEffectArgs.Caster;
			Point4D summonPoint = new Point4D(target.X, target.Y, target.Z, caster.M);
			NPC summon = (NPC) npcDef.Create(summonPoint);

			if (this.Hostile) {
				summon.AggroMode = AggroMode.Hostile; //no ControlMaster - autonomous aggro, see Hostile's own doc comment
			} else {
				summon.ControlMaster = caster;
				summon.Flag_WarMode = false;
			}
			summon.SetTag(isSummonedCreatureTK, true);
			summon.SetTag(summonerTK, caster);
			NecroAbilities.StrengthenUndead(summon, caster); //#1017

			int spellPower = spellEffectArgs.SpellPower;
			double durationSecs = this.GetDurationForValue(spellPower);
			summon.AddTimer(despawnTimerKey, new SummonDespawnTimer()).DueInSpan = TimeSpan.FromSeconds(durationSecs);
		}

		/// <summary>#813 - unsummons now (Sphere's removeAgressiveSummon zeroes the summon's timer):
		/// its despawn timer, or a new one for a summon that had none, runs out on the next timer
		/// tick - so a caller in the middle of the summon's own step doesn't delete it under itself.</summary>
		public static void DespawnNow(Character summon) {
			Timer timer = summon.GetTimer(despawnTimerKey) ?? summon.AddTimer(despawnTimerKey, new SummonDespawnTimer());
			timer.DueInSpan = TimeSpan.Zero;
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
