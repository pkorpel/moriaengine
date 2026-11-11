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

namespace SteamEngine.CompiledScripts {

	public static class DamageManager {
		// Standard UO red speech hue, used for the damage-number overhead message below - no
		// existing color convention for this already present in the codebase to match.
		private const int damageMessageColor = 0x21;

		/// <summary>f_rsst_inRange</summary>
		private static int ClampResist(int resist) {
			return Math.Max(-40000, Math.Min(1000, resist));
		}

		private static int ResistIf(DamageType damageType, DamageType flag, int resist) {
			return ((damageType & flag) == flag) ? ClampResist(resist) : 0;
		}

		private static int Count(DamageType damageType, params DamageType[] flags) {
			int count = 0;
			foreach (DamageType flag in flags) {
				if ((damageType & flag) == flag) {
					count++;
				}
			}
			return count;
		}

		private static int ApplyGroupResist(int groupResist, int factor) {
			return ((1000 - groupResist) * factor) / 1000;
		}

		/// <summary>#860 - Erebor's handleresists (resists.scp). Resists are in permille, 150 is 15%.
		/// The subtypes of a kind are averaged, then that kind's group resist (physical, magic)
		/// multiplies them. Physical and magic together average. Returns the share of the damage
		/// that goes through; negative resists raise it. A type with no subtype isn't reduced.</summary>
		public static double GetResistModifier(Character resistingChar, DamageType damageType) {
			int physCount = Count(damageType, DamageType.Blunt, DamageType.Sharp, DamageType.Slashing, DamageType.Stabbing, DamageType.Bleed, DamageType.Archery);
			int physFactor = 1000;
			if (physCount > 0) {
				physFactor = 1000 * physCount;
				physFactor -= ResistIf(damageType, DamageType.Slashing, resistingChar.ResistSlashing);
				physFactor -= ResistIf(damageType, DamageType.Stabbing, resistingChar.ResistStabbing);
				physFactor -= ResistIf(damageType, DamageType.Blunt, resistingChar.ResistBlunt);
				physFactor -= ResistIf(damageType, DamageType.Sharp, resistingChar.ResistSharp);
				physFactor -= ResistIf(damageType, DamageType.Bleed, resistingChar.ResistBleed);
				physFactor -= ResistIf(damageType, DamageType.Archery, resistingChar.ResistArchery);
				physFactor /= physCount;
			}
			if ((physCount > 0) || ((damageType & DamageType.Physical) == DamageType.Physical)) {
				physFactor = ApplyGroupResist(ClampResist(resistingChar.ResistPhysical), physFactor);
			}

			int magCount = Count(damageType, DamageType.Poison, DamageType.Fire, DamageType.Cold, DamageType.Electric, DamageType.Acid, DamageType.Dragon, DamageType.Mystical);
			int magFactor = 1000;
			if (magCount > 0) {
				magFactor = 1000 * magCount;
				magFactor -= ResistIf(damageType, DamageType.Fire, resistingChar.ResistFire);
				magFactor -= ResistIf(damageType, DamageType.Cold, resistingChar.ResistCold);
				magFactor -= ResistIf(damageType, DamageType.Acid, resistingChar.ResistAcid);
				magFactor -= ResistIf(damageType, DamageType.Poison, resistingChar.ResistPoison);
				magFactor -= ResistIf(damageType, DamageType.Electric, resistingChar.ResistElectric);
				magFactor -= ResistIf(damageType, DamageType.Dragon, resistingChar.ResistDragon);
				magFactor -= ResistIf(damageType, DamageType.Mystical, resistingChar.ResistMystical);
				magFactor /= magCount;
			}
			if ((magCount > 0) || ((damageType & DamageType.Magic) == DamageType.Magic)) {
				magFactor = ApplyGroupResist(ClampResist(resistingChar.ResistMagic), magFactor);
			}

			int factor = 1000;
			if ((physCount > 0) && (magCount > 0)) {
				factor = (physFactor + magFactor) / 2;
			} else if (physCount > 0) {
				factor = physFactor;
			} else if (magCount > 0) {
				factor = magFactor;
			}
			factor = ApplyGroupResist(ResistIf(damageType, DamageType.Summon, resistingChar.ResistSummon), factor);
			return factor / 1000.0;
		}

		/// <summary>Cause damage to defender. Returns number of hitpoints that was actually removed. Runs @Damage triggers.</summary>
		public static int CauseDamage(Character attacker, Character defender, DamageType flags, double damage) {
			double unused;
			return CauseDamage(attacker, defender, flags, damage, false, false, out unused);
		}

		private static void ShowDamageNumber(Character attacker, Character defender, string text) {
			var toDefender = defender.GameState?.Conn;
			Networking.PacketSequences.SendOverheadMessageFrom(toDefender, defender, text, damageMessageColor);
			var toAttacker = attacker?.GameState?.Conn;
			if ((toAttacker != null) && (toAttacker != toDefender)) {
				Networking.PacketSequences.SendOverheadMessageFrom(toAttacker, defender, text, damageMessageColor);
			}
		}

		/// <summary>#860 - a harmful spell's damage: rounded, and a monster's on a monster does at least 5. #1022 - the
		/// target's hit reactions run on it too, as Erebor's @aftergetswing did for spells (the user's call).</summary>
		public static int CauseSpellDamage(Character attacker, Character defender, DamageType flags, double damage) {
			double unused;
			damage = ShamanAbilities.AbsorbSpell(attacker, defender, damage); //#1017 - voodoo shield
			int dealt = CauseDamage(attacker, defender, flags, damage, false, true, out unused);
			if ((attacker != null) && !attacker.IsDeleted && !defender.IsDeleted && !defender.Flag_Dead) {
				MonsterSpecialAttacks.TryEmoteDamage(defender, dealt, flags);
				MonsterSpecialAttacks.TryReactToHit(attacker, defender);
				if (!defender.IsDeleted && !attacker.IsDeleted) {
					MonsterSpecialAttacks.TryDragonReactions(attacker, defender); //the dragons' @aftergetswing as well
				}
			}
			return dealt;
		}

		/// <summary>#872 - a bleed's tick: casting is only sometimes interrupted (chance_to_fizz). #1401 - Erebor's
		/// damage_noresponse (i_krvaceni's tick): no energy shield.</summary>
		public static int CauseBleedDamage(Character attacker, Character defender, DamageType flags, double damage, bool interruptCasting) {
			double unused;
			return CauseDamage(attacker, defender, flags, damage, false, false, out unused, interruptCasting, true);
		}

		/// <summary>#1401 - Erebor's damage_noresponse with a source (dragon fire and stamp, hazard tiles, multishot):
		/// straight to the hits, past damage_final, so the energy shield doesn't see it. Sourceless damage never
		/// reaches it either (Sphere's @GetHit isn't raised without a source).</summary>
		public static int CauseDamageNoResponse(Character attacker, Character defender, DamageType flags, double damage) {
			double unused;
			return CauseDamage(attacker, defender, flags, damage, false, false, out unused, true, true);
		}

		/// <summary>#860 - Erebor's f_dmgFinal_MvM_minimalDMG and f_dmgFinal_PvX_minimalDMG, after
		/// resists. A target immune to the damage type (resist modifier 0) stays unhurt.</summary>
		internal static double ApplyMinimalDamage(Character attacker, Character defender, DamageType flags, double damage, bool isSwing) {
			if (attacker.IsPlayerForCombat) {
				if (isSwing && (attacker.FindLayer(LayerNames.Hand1) == null) && (attacker.FindLayer(LayerNames.Hand2) == null) &&
						!Professions.HasProfession(attacker, Professions.Mystic)) {
					return (damage == 0) ? 1 : (attacker.GetSkill(SkillName.Wrestling) / 200) + 1;
				}
			} else if (!defender.IsPlayerForCombat && (damage < 1) && ((flags & DamageType.Summon) != DamageType.Summon)) {
				return 5;
			}
			return damage;
		}

		/// <summary>#868 - only blows, not spells. reactiveReturn is what ProcessSwing sends back,
		/// there, because the attacker can die of it.</summary>
		private static double ApplyReactiveArmor(Character attacker, Character defender, DamageType flags, double damage, out double reactiveReturn) {
			reactiveReturn = 0;
			if ((damage <= 0) || ((flags & DamageType.Physical) != DamageType.Physical) || !ReactiveArmorPluginDef.HasReactiveArmor(defender)) {
				return damage;
			}
			MagerySettings settings = MagerySettings.instance;
			bool isPvP = attacker.IsPlayerForCombat;
			ReactiveArmorPluginDef.Calculate((int) damage, isPvP ? settings.reactiveArmorAbsorbPvP : settings.reactiveArmorAbsorbPvM,
				isPvP ? settings.reactiveArmorReturnPvP : settings.reactiveArmorReturnPvM, out int taken, out int returned);
			reactiveReturn = returned;
			return taken;
		}

		private static int CauseDamage(Character attacker, Character defender, DamageType flags, double damage, bool isSwing, bool isSpell, out double reactiveReturn, bool interruptCasting = true, bool noResponse = false) {
			reactiveReturn = 0;
			if (MonsterSpecialAttacks.TryRinald(attacker, defender)) { //#1022 e_rinald: only players hurt him, his invulnerability answers too
				return 0;
			}
			if (MonsterSpecialAttacks.CopyGuardRefuses(attacker, defender, flags)) { //#1022 e_rozdeleni: nothing hurts it while its copy lives
				return 0;
			}
			if (GuardBoss.Refuses(attacker, defender)) { //#1234 a guard spawn's monster still lives
				return 0;
			}
			if (defender.Flag_Invulnerable) {
				//same "nothing happened" eye-candy MagicReflectionPlugin.ReflectionEyeCandy already
				//uses for a reflected harmful spell - no dedicated "attack bounced off" effect exists
				//in this codebase, and this one is already an established "your attack had no effect"
				//visual language here, so reuse it rather than invent a new effect id.
				EffectFactory.StationaryEffect(defender, 0x37B9, 10, 5);
				return 0;
			}

			//#267: symmetric with the defender check above - an invulnerable character deals no
			//damage either, not just takes none. No extra visual cue here (the normal swing/cast
			//animation already plays regardless of the resulting damage amount).
			if ((attacker != null) && attacker.Flag_Invulnerable) {
				return 0;
			}

			//Defense in depth: a creature can never damage its own current owner/commander,
			//however the attack got triggered - target selection (NPC.ShouldAggroOn,
			//NPCAiTimer.IsValidTarget) already keeps this from happening in practice, but this
			//is the one choke point everything routes through, so it's the right place for a
			//hard backstop too. A legitimate "rebel" (owner attacks pet first) clears
			//ControlMaster/FollowTarget before this could ever trigger - see NPC.On_HostileAction.
			if ((attacker != null) && (defender == (attacker.FollowTarget ?? attacker.ControlMaster))) {
				return 0;
			}

			if (!defender.IsDeleted && !defender.Flag_Dead && !defender.Flag_Insubst) {
				defender.Trigger_HostileAction(attacker);
				if (!isSwing) {
					defender.Trigger_Disrupt(interruptCasting);//nebo jen pri damage > 0 ?
				}

				//#797 - e_explode remembers everyone who hurt it
				MonsterSpecialAttacks.RecordExplodeFoe(attacker, defender);

				//#797 - Irresistable (Sphere's dam_god, e.g. e_explode's blast) skips resists
				//entirely; GetResistModifier would throw on a type with neither magic nor physical
				double resistModifier = (flags == DamageType.Irresistable) ? 1 : GetResistModifier(defender, flags);
				damage *= resistModifier;
				// #511/#514: a resist above 1000 drives the modifier negative, which would flip
				// this into a heal. Floored here, before the @damage trigger below, so a future
				// script-level heal-via-negative-damage hook (none exists yet) still has a path.
				damage = Math.Max(0, damage);
				//#1401 - Erebor's damage_final: the energy shield after the resists, before the mana shield (@damage),
				//armor wear and minimal damage
				if (!noResponse) {
					damage = EnergyShieldPlugin.Absorb(attacker, defender, damage, isSpell);
				}
				if (isSwing || isSpell) {
					damage = CombatCalculator.RoundLikeSphere(damage);
					CombatWear.OnDamageTaken(defender, (int) damage, attacker);
					if ((resistModifier > 0) && (attacker != null)) {
						damage = ApplyMinimalDamage(attacker, defender, flags, damage, isSwing);
					}
					if (isSwing) {
						damage = ApplyReactiveArmor(attacker, defender, flags, damage, out reactiveReturn);
					}
				}

				int previousHits = defender.Hits;

				DamageArgs damageArgs = new DamageArgs(attacker, defender, flags, damage, isSwing, isSpell);
				Trigger_Damage(damageArgs);

				damage = damageArgs.damage;
				int newHits = (int) Math.Round(defender.Hits - damage);
				defender.Hits = (short) newHits;

				int actualDamage = previousHits - newHits;
				HpSight.ShowChange(defender, actualDamage, false); //#927
				if (isSwing) {
					WeeklyQuests.OnSwingDamage(attacker, defender, Math.Min(actualDamage, previousHits)); //#1090 - min(ubrano, hits)
				}
				if (actualDamage > 0) {
					Reputation.OnHarm(attacker, defender); //#1243 - hurting a resident costs standing in the city
				}
				if (isSwing && (actualDamage > 0) && !defender.IsDeleted) {
					defender.Trigger_Disrupt(DoesSwingInterrupt(attacker.IsPlayerForCombat, attacker.WeaponProjectileType != ProjectileType.None,
						defender.IsPlayerForCombat, actualDamage, defender.MaxHits));
				}
				if (actualDamage > 0) {
					//TODO create blood?
					if (newHits > 0) {
						SoundCalculator.PlayHurtSound(defender);
						AnimCalculator.PerformAnim(defender, GenericAnim.GetHit);
					} //otherwise death sound+anim, is done elsewhere

					// The Hits setter above calls CauseDeath synchronously once newHits<1 - for a
					// non-player defender that deletes it immediately (see
					// NPCAiTimer.AlertGuardiansOf's own comment on this same engine behavior), so
					// a killing blow can leave `defender` already deleted by the time we get
					// here. Touching a deleted Thing throws, so skip the overhead message in
					// that case rather than crashing the whole stroke.
					if (!defender.IsDeleted) {
						// Previously the only feedback for any damage source (melee, spells, poison)
						// was the target's HP bar silently changing - no animation cue or message at
						// all told anyone a hit actually landed. Overhead text is used rather than a
						// dedicated binary "damage number" packet (which postdates this shard's
						// targeted client era and isn't confirmed supported) - speech/overhead
						// messages are already a solid, universally-supported path.
						// #1156 - to the two in the fight. Message() went to Globals.src, which a timer-driven NPC
						// swing leaves as whoever last sent a packet: the number showed to a bystander, and threw
						// when that player had logged out.
						ShowDamageNumber(attacker, defender, "-" + actualDamage);
					}
				}

				return actualDamage;
			}

			return 0;
		}

		public static void ProcessSwing(Character attacker, Character defender) {
			if (Camp.ProtectsFrom(attacker, defender)) {
				return; //#1107 - e_lagr_nodmg's @BeforeSwing
			}
			HidingSkillDef.Reveal(attacker); //#962
			WeaponSwingArgs swingArgs = GetWeaponSwingArgs(attacker, defender);
			SoundCalculator.PlayAttackSound(attacker);
			// The attacker never got any swing animation at all before this - only a sound
			// played, so every attack looked like the attacker just stood still. The anim
			// category (swing/stab/overhead/shoot) is picked by weapon type; AnimCalculator's
			// own TranslateAnim already handles handedness (left/right hand) on top of this.
			AnimCalculator.PerformAnim(attacker, GetSwingAnim(attacker.WeaponType));

			if (TriggerResult.Cancel != Trigger_BeforeSwing(swingArgs)) {
				CombatWear.OnSwing(attacker);
				//real-user-reported follow-up: a_tevent_e_summoneater (if the attacker has it)
				//instantly deletes a summoned-creature defender instead of dealing normal
				//damage - nothing left to apply critic/stun/knockback/bleed to, so bail out of
				//the whole swing immediately.
				if (MonsterSpecialAttacks.TryApplySummonEater(attacker, defender)) {
					return;
				}

				// #447/#448: a Mystic's swing deals mind-power magic damage (DamageType.
				// Mystical, confirmed as this engine's own purpose-built flag for exactly this -
				// see Enums.cs's "Mystikuv utok" comment) instead of the weapon's normal
				// physical damage type, even though the weapon itself (a staff) is a plain
				// OneHandBlunt/TwoHandBlunt per mystic.def.
				bool attackerIsMystic = Professions.HasProfession(attacker, Professions.Mystic);
				// #860 - Erebor's mystic strike is dam_magic|dam_cold
				DamageType damageType = !attackerIsMystic ? attacker.WeaponDamageType :
					(swingArgs.MysticLightning ? DamageType.MagicElectric : DamageType.MagicCold);

				//real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_critic
				//(if the attacker has it) can still add bonus damage to THIS swing, so it must
				//run before CauseDamage below, while swingArgs.DamageAfterAC is still mutable.
				MonsterSpecialAttacks.TryApplyCritic(attacker, swingArgs);

				//real-user-reported follow-up: a_tevent_e_npc_sniper (if the attacker has it)
				//can also add bonus damage while fighting at range vs a player - same mutable-
				//DamageAfterAC timing as critic above.
				MonsterSpecialAttacks.TryApplySniper(attacker, swingArgs);

				//#794 - an elite monster hits harder, and absorbs most of a player's swing
				EliteMonster.ModifySwingDamage(attacker, defender, swingArgs);

				//#797 - a_tevent_e_npc_magicshield on the DEFENDER swallows most of the swing;
				//#799 - an e_eye defender may make a physical blow bounce off entirely
				MonsterSpecialAttacks.TryApplyMagicShield(attacker, defender, swingArgs);
				MonsterSpecialAttacks.TryApplyEyeDeflect(attacker, defender, swingArgs);
				MonsterSpecialAttacks.TryAbsorbSwing(attacker, defender, swingArgs); //#1022 - scales, mana shield

				if (attacker is Player striker) {
					ClassAbilities.ModifyOwnBlow(striker, defender, swingArgs); //#1017
				}
				Trigger_SwingLanding(swingArgs); //#1429 - each class's own part, on its profession plugin
				if (defender.IsDeleted || attacker.IsDeleted) {
					return;
				}
				if (attacker is Player bolter) {
					RangerAbilities.TryBleedBolt(bolter, defender, swingArgs); //#1070 - e_bleed_bolt
				}
				MysticMind.ModifyBlow(attacker, defender, swingArgs); //#1017 - stolen talents
				MagicCombat.ModifyBlow(attacker, defender, swingArgs); //#1017 - magic items' slayers
				MysticMind.OnSwungAt(defender); //#1017 - a blow frees one held by catchField
				MysticHypnosis.OnSwungAt(defender); //#1017 - and breaks a hypnosis
				if (defender.IsDeleted || attacker.IsDeleted) {
					return;
				}
				Trigger_GetSwingLanding(swingArgs); //#1429 - the stance and the shield, on the profession plugin
				if (defender.IsDeleted || attacker.IsDeleted) {
					return;
				}

				//#866 - f_dmgFinal_dodge's order: abilities that send the blow back or stop it, then dodge
				double reflected = 0;
				if (ThiefDefence.TryAvoid(attacker, defender, damageType, swingArgs.DamageAfterAC, out reflected) ||
						TryDodge(attacker, defender, damageType)) {
					if (reflected > 0) {
						CauseDamage(defender, attacker, damageType, reflected);
						if (attacker.IsDeleted) {
							return;
						}
					}
					EndAsDodged(swingArgs);
					return;
				}

				if (ThiefDefence.WeakensOwnBlow(attacker, defender)) {
					swingArgs.DamageAfterAC = swingArgs.DamageAfterAC * CombatSettings.instance.vigilanceOwnDamage;
				}
				swingArgs.DamageAfterAC = Q026.WraithBlow(attacker, defender, swingArgs.DamageAfterAC); //#1272 e_q026_hcnPoisoning_NPC: curses, then hits a cursed hero harder

				//#1411 - Sphere's @hit and @getHit: the blow landed; a cancel ends it as a dodged one
				if (Trigger_Hit(swingArgs) == TriggerResult.Cancel) {
					if (!attacker.IsDeleted) {
						EndAsDodged(swingArgs);
					}
					return;
				}
				if (defender.IsDeleted || attacker.IsDeleted) {
					return;
				}

				int actualDamage = CauseDamage(attacker, defender, damageType, swingArgs.DamageAfterAC, true, false, out double reactiveReturn);

				//a_tevent_e_npc_stun (if the attacker has it) - a landed swing (we're only here
				//because Trigger_BeforeSwing wasn't cancelled) can also stun the defender.
				MonsterSpecialAttacks.TryApplyStun(attacker, defender);
				if ((attacker is Player stunner) && (defender is NPC stunned) && !stunned.IsDeleted) {
					WarriorAbilities.TryStun(stunner, stunned, actualDamage); //#1017
					ThiefAbilities.TryShieldStrike(stunner, stunned);
				}

				//real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_knockback
				//(if the attacker has it) can also push the defender back on a landed hit.
				if (actualDamage > 0) { //#1155 - the user's call: only a blow that hurt knocks back (Erebor's did on any landed one)
					MonsterSpecialAttacks.TryApplyKnockback(attacker, defender);
				}

				//a_tevent_e_npc_bleed - scales off the REAL landed damage (actualDamage), not
				//the pre-armor swing value, so this must run after CauseDamage above.
				MonsterSpecialAttacks.TryApplyBleed(attacker, defender, actualDamage);
				MagicCombat.AfterSwing(attacker, defender, actualDamage); //#1017 - leech, paralysis
				if (!defender.IsDeleted) {
					Trigger_AfterHit(swingArgs, actualDamage); //#1445 - the blow and its damage are done: the blood pact shares it
				}
				if (attacker is NPC poisoner) {
					Poison.FromMonster(poisoner, defender); //#962 - combat.scp's wasHarmed
				}

				//#794 - a hit interrupts an elite's gate spell
				EliteMonster.OnGotHit(defender);

				//#797 batch 4 - abilities reacting to a landed swing. Attacker side first (its own
				//leech/fire outburst), then the defender's, which only make sense while the
				//defender survived the blow (a killed NPC is already deleted here).
				MonsterSpecialAttacks.TryApplyLifeLeech(attacker);
				MonsterSpecialAttacks.TryReactToOwnSwing(attacker, defender, actualDamage); //#1022
				MonsterSpecialAttacks.TryEmoteDamage(defender, actualDamage, damageType); //#1022 e_emotedmg
				MonsterSpecialAttacks.TryApplyPhoenixFire(attacker);
				MonsterSpecialAttacks.TryApplyEyeDrain(attacker, defender, actualDamage); //#799
				MonsterSpecialAttacks.TryApplyBackstabAmbush(attacker, defender, actualDamage); //#799
				if (reactiveReturn > 0) {
					CauseDamage(defender, attacker, damageType, reactiveReturn);
				}
				if (attacker.IsDeleted) {
					return;
				}
				if (!defender.IsDeleted && defender.IsAliveAndValid) {
					MonsterSpecialAttacks.TryApplyReactiveArmor(attacker, defender, damageType, actualDamage);
					MonsterSpecialAttacks.TryApplyStamDmg(attacker, defender);
					MonsterSpecialAttacks.TryApplyManaGain(defender, actualDamage);
					MonsterSpecialAttacks.TryApplyPhoenixRebirth(defender);
					MonsterSpecialAttacks.TryApplyLightnings(defender);
					MonsterSpecialAttacks.TryApplyPushall(defender);
					MonsterSpecialAttacks.TryApplyAutoReflect(defender);
					if (!attacker.IsDeleted) {
						MonsterSpecialAttacks.TryReactToHit(attacker, defender); //#1022
					}
					//#799 batch 5 - the ones reading the attacker skip an NPC attacker reactive armor just killed
					MonsterSpecialAttacks.TryInterruptBackstabHide(defender);
					MonsterSpecialAttacks.TryApplyMultishot(defender);
					MonsterSpecialAttacks.TryApplyDispelBomb(defender);
					MonsterSpecialAttacks.TryDragonReactions(attacker, defender); //#1022 the dragons' @aftergetswing
					if (!attacker.IsDeleted) {
						MonsterSpecialAttacks.TryApplyCraftMonster(attacker, defender);
						MonsterSpecialAttacks.TryApplyEyeAttention(attacker, defender);
						MonsterSpecialAttacks.TryApplyGolemRetarget(attacker, defender);
						MonsterSpecialAttacks.TryApplyMassLeap(attacker, defender);
						MonsterSpecialAttacks.TryApplyDuplicator(attacker, defender);
					}
				}
				if (attacker.IsDeleted) {
					return; //reactive armor killed an NPC attacker - nothing left to message or drain
				}

				swingArgs.InternalSetFinalDamage(actualDamage);

				// Testing combat messages (CombatSettings.showCombatMessages) - the only
				// existing feedback for a landed hit was the defender's own anonymous "-N"
				// overhead message (still unconditional, see CauseDamage above); neither side
				// got a named, chat-log-visible line saying who hit whom for how much.
				CombatMessages.Send(attacker, "[Combat] You hit " + defender.Name + " for " + actualDamage + " damage.");
				CombatMessages.Send(defender, "[Combat] " + attacker.Name + " hits you for " + actualDamage + " damage.");

				// #450 - confirmed from the Erebor source: every Mystic swing drains a fraction
				// of current mana (the natural throttle instead of a hard mana gate - see
				// CalculateCombatWeaponValues's mana-driven WeaponDelay, which slows down as
				// this drains). Only on a landed swing (On_Success -> ProcessSwing), not a
				// missed one - WeaponSkillDef has no separate "cast attempt" step to hook for a
				// Mystic the way Erebor's own single dospellcustom_donesrc call would.
				if (attackerIsMystic) {
					if (swingArgs.MysticSpecial && !defender.IsDeleted && defender.IsAliveAndValid) {
						MysticAbilities.ApplySpecialStrike(attacker, defender, swingArgs.MysticLightning);
					}
					DropManaShieldForStrike(attacker);
					int manaBefore = attacker.Mana;
					attacker.Mana = (short) Math.Round(attacker.Mana * MagerySettings.instance.mysticManaLossPermille / 1000.0);
					CombatMessages.Send(attacker, "[Combat] Your attack channels " + (manaBefore - attacker.Mana) +
						" mana (" + attacker.Mana + " left).");
				}

				Trigger_AfterSwing(swingArgs);
			}
		}

		private static GenericAnim GetSwingAnim(WeaponType weaponType) {
			switch (weaponType) {
				case WeaponType.BareHands:
					return GenericAnim.AttackBareHands;
				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt:
					return GenericAnim.AttackOverhead;
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					return GenericAnim.AttackStab;
				case WeaponType.Bow:
				case WeaponType.XBow:
					return GenericAnim.AttackShoot;
				case WeaponType.OneHandSword:
				case WeaponType.TwoHandSword:
				case WeaponType.OneHandAxe:
				case WeaponType.TwoHandAxe:
				default:
					return GenericAnim.AttackSwing;
			}
		}

		public static WeaponSwingArgs GetWeaponSwingArgs(Character attacker, Character defender) {
			// #447/#448/#449: a Mystic's attack diverts entirely to the mind-power path -
			// confirmed from Erebor source, "combat_main_mystik" replaces the normal physical
			// attack function outright rather than adding to it. mindPower(attacker) and
			// mindDefense(defender) are already-computed Character properties
			// (CombatCalculator.CalculateCombatWeaponValues/CalculateCombatArmorValues) that
			// had zero consumers before this.
			if (Professions.HasProfession(attacker, Professions.Mystic)) {
				return GetMindPowerSwingArgs(attacker, defender);
			}

			CombatSettings settings = CombatSettings.instance;
			double piercing = attacker.WeaponPiercing;

			bool defenderIsPlayer = defender.IsPlayerForCombat;
			bool attackerIsPlayer = attacker.IsPlayerForCombat;

			// #860 - combat_main_nonmystik_countDmg and combat_main_npc. The armor multiplier is on
			// damagecust's side only, a monster hitting a player uses the plain armor (damagecust_MvP).
			double attack, armorClass;
			if (attackerIsPlayer) {
				if (defenderIsPlayer) {
					attack = CombatCalculator.RoundLikeSphere(attacker.WeaponAttackVsP * settings.weapAttack);
					attack = CombatCalculator.RoundLikeSphere(attack * settings.weapAttackPvP);
				} else {
					attack = CombatCalculator.RoundLikeSphere(attacker.WeaponAttackVsM * settings.weapAttack);
					attack = CombatCalculator.RoundLikeSphere(attack * GetAttackVsMonsterMultiplier(attacker, settings));
				}
				if (defenderIsPlayer) {
					armorClass = Math.Max(0, defender.ArmorClassVsP + GetPvPArmorChange(defender)) * settings.armorClassP;
				} else {
					armorClass = defender.ArmorClassVsP * settings.armorClassM;
				}
			} else {
				attack = CombatCalculator.RoundLikeSphere(attacker.WeaponAttackVsM * settings.weapAttackM);
				if (defenderIsPlayer) {
					armorClass = defender.ArmorClassVsM;
				} else {
					attack = CombatCalculator.RoundLikeSphere(attack * settings.weapAttackVsM);
					armorClass = defender.ArmorClassVsM * settings.armorClassM;
				}
			}

			double damageAfterAC = CombatCalculator.CalculateArmorClassEffect(attacker, defender, attack, piercing, armorClass);

			return new WeaponSwingArgs(attacker, defender, attack, piercing, armorClass, damageAfterAC);
		}

		/// <summary>#860 - nastaveni_global_armorchange_vP_&lt;profession&gt;, against players only.</summary>
		internal static int GetPvPArmorChange(Character defender) {
			Player player = defender as Player;
			ProfessionDef profession = (player != null) ? player.Profession : null;
			return (profession != null) ? profession.PvPArmorChange : 0;
		}

		private static PassiveAbilityDef weaponDodgeDef;

		/// <summary>#860 - f_dmgFinal_phys_dodge: a point of a_weapon_dodge dodges 1% of the monsters' physical
		/// blows (nastaveni_ability_pvMdodge). Players' blows can't be dodged this way.</summary>
		internal static bool TryDodge(Character attacker, Character defender, DamageType damageType) {
			if (attacker.IsPlayerForCombat || ((damageType & DamageType.Physical) != DamageType.Physical)) {
				return false;
			}
			if (weaponDodgeDef == null) {
				weaponDodgeDef = (PassiveAbilityDef) AbilityDef.GetByDefname("a_weapon_dodge");
			}
			if ((defender.GetAbility(weaponDodgeDef) * CombatSettings.instance.weaponDodgePerPoint) <= Globals.dice.Next(1000)) {
				return false;
			}
			defender.SysMessage(Loc<DamageManagerLoc>.Get(defender.Language).YouDodged);
			SoundCalculator.PlayMissSound(attacker);
			return true;
		}

		/// <summary>#860 - f_nonmystik_calcFizz: only a player's melee blow interrupts casting, a
		/// hard one if the target is a player, 5 damage if not. Ammo never does.</summary>
		public static bool DoesSwingInterrupt(bool attackerIsPlayer, bool isRanged, bool defenderIsPlayer, int damage, int defenderMaxHits) {
			if (!attackerIsPlayer || isRanged) {
				return false;
			}
			return damage >= (defenderIsPlayer ? (defenderMaxHits / 10) : 5);
		}

		internal static readonly PluginKey manaShieldKey = PluginKey.Acquire("spellEffect_mana_shield");

		/// <summary>#868 - combat_main_mystik: a Mystic's strike ends mana shield, for half of Int's mana.</summary>
		private static void DropManaShieldForStrike(Character attacker) {
			Plugin shield = attacker.GetPlugin(manaShieldKey);
			if (shield != null) {
				shield.Delete();
				attacker.Mana = (short) Math.Max(0, attacker.Mana - (attacker.Int / 2));
			}
		}

		/// <summary>#860 - vMweapattack grows with the player's level, up to 200.</summary>
		internal static double GetAttackVsMonsterMultiplier(Character attacker, CombatSettings settings) {
			Player player = attacker as Player;
			return CalculateAttackVsMonsterMultiplier((player != null) ? player.Level : 0, settings.weapAttackVsM,
				settings.weapAttackVsMPerLevel, settings.weapAttackVsMMaxLevel);
		}

		public static double CalculateAttackVsMonsterMultiplier(int level, double baseMultiplier, double perLevel, int maxLevel) {
			return baseMultiplier + (Math.Min(level, maxLevel) * perLevel);
		}

		/// <summary>#447/#448/#449 - Mystic's mind-power swing. `piercing` has no equivalent in
		/// this formula (mind defense isn't reduced by anything matching a weapon/ammo's
		/// physical piercing stat per the Erebor source), so it's always 0 here - kept in the
		/// WeaponSwingArgs shape only because that type is shared with the physical path.</summary>
		private static WeaponSwingArgs GetMindPowerSwingArgs(Character attacker, Character defender) {
			bool defenderIsPlayer = defender.IsPlayerForCombat;
			MagerySettings magery = MagerySettings.instance;
			double power = (defenderIsPlayer ? attacker.MindPowerVsP : attacker.MindPowerVsM) * 10;
			if (defenderIsPlayer) {
				power *= magery.spellPowerPvP;
			} else {
				Player player = attacker as Player;
				int level = (player != null) ? Math.Min(player.Level, 200) : 0;
				power *= magery.mysticPowerVsM * (magery.spellPowerVsM + (level * magery.mysticPowerPerLevel));
				if (MysticAbilities.RollCrushingBlow(attacker)) {
					power += Math.Truncate(power * magery.mysticCrushingBlowStrength);
				}
			}
			bool lightning = !defenderIsPlayer && MysticAbilities.StrikesWithLightning(attacker);
			bool special = MysticAbilities.RollSpecial(attacker);
			double effect = !special ? magery.mysticStrikeEffect : (lightning ? magery.mysticLightningStrikeEffect : magery.mysticSpecialStrikeEffect);
			int defense = CombatCalculator.GetSpellDefense(attacker, defender);
			double damageAfterMD = Math.Max(0, Math.Truncate(((Math.Truncate(power) - defense) * effect) / 1000));

			WeaponSwingArgs swingArgs = new WeaponSwingArgs(attacker, defender, power, 0, defense, damageAfterMD);
			swingArgs.MysticSpecial = special;
			swingArgs.MysticLightning = lightning;
			return swingArgs;
		}

		static TriggerKey beforeSwingTK = TriggerKey.Acquire("beforeSwing");
		static TriggerKey beforeGetSwingTK = TriggerKey.Acquire("beforeGetSwing");

		/// <summary>Happens before applying armor, can be cancelled.</summary>
		public static TriggerResult Trigger_BeforeSwing(WeaponSwingArgs swingArgs) {
			var result = swingArgs.attacker.TryCancellableTrigger(beforeSwingTK, swingArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = swingArgs.attacker.On_BeforeSwing(swingArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = swingArgs.defender.TryCancellableTrigger(beforeGetSwingTK, swingArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = swingArgs.defender.On_BeforeGetSwing(swingArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return result;
		}

		/// <summary>A blow that does nothing: dodged, or (#1411) cancelled in @hit/@getHit.</summary>
		private static void EndAsDodged(WeaponSwingArgs swingArgs) {
			Character attacker = swingArgs.attacker, defender = swingArgs.defender;
			swingArgs.InternalSetFinalDamage(0);
			MonsterSpecialAttacks.TryDeathBossOwnSwing(attacker); //#1022 e_bomba_boy's @AfterSwing runs on a dodged blow too
			MonsterSpecialAttacks.TryBalrogSwing(attacker, defender); //#1022 so does e_balrog's
			MonsterSpecialAttacks.TryHcnMadness(attacker, defender); //#1022 and e_hcn_madnessability's
			Trigger_AfterSwing(swingArgs);
		}

		static TriggerKey swingLandingTK = TriggerKey.Acquire("swingLanding");
		static TriggerKey getSwingLandingTK = TriggerKey.Acquire("getSwingLanding");

		/// <summary>#1429 - Erebor's @AfterSwing of a player: a blow that's landing, before the dodge, after armour and the
		/// monsters' own modifiers. Handlers change swingArgs.DamageAfterAC; nothing cancels here (@hit can). The warrior's
		/// profession plugin answers it.</summary>
		public static void Trigger_SwingLanding(WeaponSwingArgs swingArgs) {
			if (!swingArgs.attacker.IsDeleted) { //a monster's own modifier before it may have taken it away
				swingArgs.attacker.TryTrigger(swingLandingTK, swingArgs);
			}
		}

		/// <summary>#1429 - Erebor's @aftergetswing of a player, at the same stage on the defender, after the attacker's
		/// stolen talents and magic items. The profession plugins answer it (stance, shield block).</summary>
		public static void Trigger_GetSwingLanding(WeaponSwingArgs swingArgs) {
			if (!swingArgs.defender.IsDeleted) {
				swingArgs.defender.TryTrigger(getSwingLandingTK, swingArgs);
			}
		}

		static TriggerKey afterHitTK = TriggerKey.Acquire("afterHit");
		static TriggerKey afterGetHitTK = TriggerKey.Acquire("afterGetHit");

		/// <summary>#1445 - a landed blow's damage is dealt (args: the swing, the damage): @afterHit on the attacker, then
		/// @afterGetHit on the defender. Raised in ProcessSwing after the damage, the stun, the knockback, the bleeding and
		/// the magic items' leech and paralysis, before the monsters' after-swing reactions; not on a dodged or cancelled
		/// blow (those end in @afterSwing). The blood pact (PactPlugin) answers @afterGetHit.</summary>
		public static void Trigger_AfterHit(WeaponSwingArgs swingArgs, int damage) {
			ScriptArgs args = new ScriptArgs(swingArgs, damage);
			if (!swingArgs.attacker.IsDeleted) {
				swingArgs.attacker.TryTrigger(afterHitTK, args);
			}
			if (!swingArgs.defender.IsDeleted) {
				swingArgs.defender.TryTrigger(afterGetHitTK, args);
			}
		}

		static TriggerKey hitTK = TriggerKey.Acquire("hit");
		static TriggerKey getHitTK = TriggerKey.Acquire("getHit");

		/// <summary>#1411 - Sphere's @hit on the attacker, then @getHit on the defender: a blow that wasn't dodged, before its
		/// damage is applied. A handler may change swingArgs.DamageAfterAC (what CauseDamage gets) or cancel: the blow then
		/// does nothing, as a dodged one, and @getHit isn't raised after a cancelled @hit. Unlike Sphere's: raised after the
		/// dodge and the blow's own modifiers (armour, monster shields, class abilities); the damage stage (resists, shields,
		/// minimum damage, reactive armour) follows in CauseDamage. Only for swings (a mystic's strike too; not spells nor
		/// ability blows such as backstab or split shot), and SRC isn't set.</summary>
		public static TriggerResult Trigger_Hit(WeaponSwingArgs swingArgs) {
			var result = swingArgs.attacker.TryCancellableTrigger(hitTK, swingArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = swingArgs.attacker.On_Hit(swingArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if ((result != TriggerResult.Cancel) && !swingArgs.defender.IsDeleted) {
					result = swingArgs.defender.TryCancellableTrigger(getHitTK, swingArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = swingArgs.defender.On_GetHit(swingArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return result;
		}

		static TriggerKey causeDamageTK = TriggerKey.Acquire("causeDamage");
		static TriggerKey damageTK = TriggerKey.Acquire("damage");

		/// <summary>Happens before applying armor. If a script should want to negate or alter the damage, it should 'manually' alter Hits, and/or create a new damaging event altogether.</summary>
		public static void Trigger_Damage(DamageArgs damageArgs) {
			if (damageArgs.attacker != null) { //#927 - a bomb has no attacker: Sphere's damage_noresponse
				damageArgs.attacker.TryTrigger(causeDamageTK, damageArgs);
				try {
					damageArgs.attacker.On_CauseDamage(damageArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}

			damageArgs.defender.TryTrigger(damageTK, damageArgs);
			try {
				damageArgs.defender.On_Damage(damageArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		static TriggerKey afterSwingTK = TriggerKey.Acquire("afterSwing");
		static TriggerKey afterGetSwingTK = TriggerKey.Acquire("afterGetSwing");

		/// <summary>Happens after applying armor, can be cancelled.</summary>
		public static void Trigger_AfterSwing(WeaponSwingArgs swingArgs) {
			swingArgs.attacker.TryTrigger(afterSwingTK, swingArgs);

			try {
				swingArgs.attacker.On_AfterSwing(swingArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			if (!swingArgs.defender.IsDeleted) {
				swingArgs.defender.TryTrigger(afterGetSwingTK, swingArgs);

				try {
					swingArgs.defender.On_AfterGetSwing(swingArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}
	}

	public class DamageArgs : ScriptArgs {
		public readonly Character defender;
		public readonly Character attacker;
		public readonly DamageType flags;
		/// <summary>#1401 - what the defender loses: CauseDamage reads it back after @damage, so a handler may lower it
		/// (the energy shield).</summary>
		public double damage;
		/// <summary>#1401 - a landed blow or a harmful spell's damage; neither for poison, bleeding, bombs and the like.</summary>
		public readonly bool isSwing, isSpell;

		public DamageArgs(Character attacker, Character defender, DamageType flags, double damage)
			: this(attacker, defender, flags, damage, false, false) {
		}

		public DamageArgs(Character attacker, Character defender, DamageType flags, double damage, bool isSwing, bool isSpell)
			: base(attacker, defender, flags, damage) {
			this.defender = defender;
			this.attacker = attacker;
			this.flags = flags;
			this.damage = damage;
			this.isSwing = isSwing;
			this.isSpell = isSpell;
		}
	}

	public class WeaponSwingArgs : ScriptArgs {
		public readonly Character defender;
		public readonly Character attacker;
		public readonly double attack, piercing, armorClass;
		int finalDamage;

		/// <summary>#868 - a Mystic's strike: an Astral Power one, and of lightning, not cold.</summary>
		internal bool MysticSpecial, MysticLightning;

		public WeaponSwingArgs(Character attacker, Character defender, double attack, double piercing, double armorClass, double damageAfterAC)
			: base(attacker, defender, attack, piercing, armorClass, damageAfterAC, -1) {
			this.attacker = attacker;
			this.defender = defender;
			this.attack = attack;
			this.piercing = piercing;
			this.armorClass = armorClass;
		}

		public double DamageAfterAC {
			get {
				return Convert.ToDouble(this.Argv[5]);
			}
			set {
				this.Argv[5] = value;
			}
		}

		public int FinalDamage {
			get {
				return this.finalDamage;
			}
		}

		internal void InternalSetFinalDamage(int value) {
			this.Argv[6] = value;
			this.finalDamage = value;
		}
	}

	public class DamageManagerLoc : CompiledLocStringCollection<DamageManagerLoc> {
		internal string YouDodged = "Uhnul jsi uderu.";
	}
}
