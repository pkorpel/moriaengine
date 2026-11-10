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
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.LScript;
using SteamEngine.Networking;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	//temporary? Mozna to bude neco jako MovementBrain, kazdopadne to chci takhle na testovani
	[ViewableClass]
	[SaveableClass, DeepCopyableClass]
	public sealed class DynamicMovementSettings : IMovementSettings {

		[SaveableData, CopyableData]
		public bool canCrossLand;

		[SaveableData, CopyableData]
		public bool canSwim;

		[SaveableData, CopyableData]
		public bool canCrossLava;

		[SaveableData, CopyableData]
		public bool canFly;

		[SaveableData, CopyableData]
		public bool ignoreDoors;

		[SaveableData, CopyableData]
		public int climbPower;

		[LoadingInitializer, DeepCopyImplementation]
		public DynamicMovementSettings() {
			this.canCrossLand = true;
			this.canSwim = false;
			this.canCrossLava = false;
			this.canFly = false;
			this.ignoreDoors = false;
			this.climbPower = 2;
		}

		public void ShowValues() {
			Globals.SrcWriteLine("canCrossLand: " + this.canCrossLand);
			Globals.SrcWriteLine("canSwim: " + this.canSwim);
			Globals.SrcWriteLine("canCrossLava: " + this.canCrossLava);
			Globals.SrcWriteLine("canFly: " + this.canFly);
			Globals.SrcWriteLine("ignoreDoors: " + this.ignoreDoors);
			Globals.SrcWriteLine("climbPower: " + this.climbPower);
		}

		public DynamicMovementSettings(bool canCrossLand, bool canSwim, bool canCrossLava,
					bool canFly, bool ignoreDoors, int climbPower) {
			this.canCrossLand = canCrossLand;
			this.canSwim = canSwim;
			this.canCrossLava = canCrossLava;
			this.canFly = canFly;
			this.ignoreDoors = ignoreDoors;
			this.climbPower = climbPower;
		}

		bool IMovementSettings.CanCrossLand {
			get {
				return this.canCrossLand;
			}
		}
		bool IMovementSettings.CanSwim {
			get {
				return this.canSwim;
			}
		}
		bool IMovementSettings.CanCrossLava {
			get {
				return this.canCrossLava;
			}
		}
		bool IMovementSettings.CanFly {
			get {
				return this.canFly;
			}
		}
		bool IMovementSettings.IgnoreDoors {
			get {
				return this.ignoreDoors;
			}
		}
		int IMovementSettings.ClimbPower {
			get {
				return this.climbPower;
			}
		} //max positive difference in 1 step
	}

	/// <summary>How an NPC decides whether to attack a player it notices - see NPCAiTimer.ShouldAggroOn. Deliberately extensible: NPC/NPCDef subclasses can override ShouldAggroOn for policies beyond these three builtins (e.g. time-of-day, criminal-only) without touching the AI tick loop.</summary>
	public enum AggroMode {
		/// <summary>Never initiates an attack - the default, used by townsfolk/vendors/etc.</summary>
		Calm,
		/// <summary>Attacks any valid player that comes into aggro range, regardless of realm/notoriety - wild monsters.</summary>
		Hostile,
		/// <summary>Attacks players whose Realm is on the opposite side of this NPC's own Realm (Gondor vs Mordor, including outcasts) - town guards.</summary>
		GuardsRealm
	}

	[ViewableClass]
	public partial class NPCDef {
		private static readonly TagKey combatSpellTK = TagKey.Acquire("_combatSpell_");

		/// <summary>
		/// The spell this NPC casts at its combat target instead of swinging a weapon, when it has
		/// one configured and enough mana (see NPCAiTimer.TryCastSpell, enhancements #57). Null
		/// (the default) means this NPC fights purely with weapons/unarmed combat, same as before.
		/// </summary>
		public SpellDef CombatSpell {
			get {
				return this.GetTag(combatSpellTK) as SpellDef;
			}
			set {
				this.SetTag(combatSpellTK, value);
			}
		}

		private static readonly TagKey mountCharDefTK = TagKey.Acquire("_npcMountCharDef_");

		/// <summary>#700 - the mount creature species (e.g. c_horse_brown_dk) this NPC should
		/// spawn and already be riding when created (tag.mount in the real archive). Null (the
		/// default) means this NPC isn't pre-mounted - unaffected, same as before this field
		/// existed. The spawned mount's own MountItem/Color (the latter overridable per-NPCDef
		/// via MountColor below) drive what actually gets rendered - this field only says WHICH
		/// creature to spawn, see NPC.On_Create.</summary>
		public NPCDef MountCharDef {
			get {
				return this.GetTag(mountCharDefTK) as NPCDef;
			}
			set {
				this.SetTag(mountCharDefTK, value);
			}
		}

		private static readonly TagKey instanceSpellsTK = TagKey.Acquire("_npcSpells_");

		/// <summary>#564 - the full set of spells this NPC species can cast (addspell= in the real
		/// archive, often 2-4 per mage-type NPC), as opposed to CombatSpell (a single, always-the-
		/// same spell). When non-empty, NPCAiTimer.TryCastSpell picks uniformly at random from THIS
		/// list each cast attempt instead of always using CombatSpell - a simple, source-faithful-
		/// enough interim policy (the issue itself flags "random per cast" as a reasonable option;
		/// no situational/weighted targeting data exists in the public archive to justify anything
		/// fancier). CombatSpell stays the fallback when this list is empty/unconfigured, so nothing
		/// regresses for any existing single-spell-only NPCDef.</summary>
		public List<SpellDef> Spells {
			get {
				return this.GetTag(instanceSpellsTK) as List<SpellDef>;
			}
			set {
				this.SetTag(instanceSpellsTK, value);
			}
		}

		private static readonly TagKey experienceWorthTK = TagKey.Acquire("_experienceWorth_");

		/// <summary>Experience range {min, max} granted to the player who kills an NPC of this
		/// type - a flat hand-tuned value, not derived from stats (Erebor's own pattern,
		/// tag.experience={min max} - see LevelingSystem.AddExperience / #127). Null (the
		/// default) means this NPC type grants no experience - most NPC content doesn't have
		/// this configured yet (see #12, NPC content is ~55-60% incomplete).</summary>
		public int[] ExperienceWorth {
			get {
				return this.GetTag(experienceWorthTK) as int[];
			}
			set {
				this.SetTag(experienceWorthTK, value);
			}
		}

		/// <summary>Rolls a random value in [min, max] (inclusive) from ExperienceWorth, or 0 if unset.</summary>
		public int RollExperienceReward() {
			int[] range = this.ExperienceWorth;
			if (range == null) {
				return 0;
			}
			if (range[0] >= range[1]) {
				return range[0];
			}
			return Globals.dice.Next(range[0], range[1] + 1);
		}

		private static readonly TagKey strRangeTK = TagKey.Acquire("_strRange_");
		private static readonly TagKey dexRangeTK = TagKey.Acquire("_dexRange_");
		private static readonly TagKey intRangeTK = TagKey.Acquire("_intRange_");

		/// <summary>Starting Str/Dex/Int {min, max} range for a fresh NPC of this type (#559),
		/// rolled once at creation by NPC.On_Create - mirrors ExperienceWorth's own shape.
		/// Null (the default) means unconfigured - On_Create leaves the Character's own
		/// stat-default (15, see Character.ct) untouched rather than zeroing it.</summary>
		public int[] StrRange {
			get { return this.GetTag(strRangeTK) as int[]; }
			set { this.SetTag(strRangeTK, value); }
		}
		public int[] DexRange {
			get { return this.GetTag(dexRangeTK) as int[]; }
			set { this.SetTag(dexRangeTK, value); }
		}
		public int[] IntRange {
			get { return this.GetTag(intRangeTK) as int[]; }
			set { this.SetTag(intRangeTK, value); }
		}

		/// <summary>Rolls a value in [min, max] (inclusive) from `range`, or -1 if `range` is unset
		/// (unconfigured) - the sentinel NPC.On_Create checks before overwriting the live default.</summary>
		internal static int RollStatRange(int[] range) {
			if (range == null) {
				return -1;
			}
			if (range[0] >= range[1]) {
				return range[0];
			}
			return Globals.dice.Next(range[0], range[1] + 1);
		}

		public int RollStr() { return RollStatRange(this.StrRange); }
		public int RollDex() { return RollStatRange(this.DexRange); }
		public int RollInt() { return RollStatRange(this.IntRange); }

		private static readonly TagKey stunChanceTK = TagKey.Acquire("_npcStunChance_");
		private static readonly TagKey stunDurationTK = TagKey.Acquire("_npcStunDuration_");
		private static readonly TagKey stunCooldownTK = TagKey.Acquire("_npcStunCooldown_");
		private static readonly TagKey criticChanceTK = TagKey.Acquire("_npcCriticChance_");
		private static readonly TagKey criticDamageTK = TagKey.Acquire("_npcCriticDamage_");
		private static readonly TagKey roundattackChanceTK = TagKey.Acquire("_npcRoundattackChance_");
		private static readonly TagKey roundattackDamageTK = TagKey.Acquire("_npcRoundattackDamage_");
		private static readonly TagKey roundattackCooldownTK = TagKey.Acquire("_npcRoundattackCooldown_");
		private static readonly TagKey pullDistanceTK = TagKey.Acquire("_npcPullDistance_");
		private static readonly TagKey bleedChanceTK = TagKey.Acquire("_npcBleedChance_");
		private static readonly TagKey knockbackChanceTK = TagKey.Acquire("_npcKnockbackChance_");
		private static readonly TagKey selfresDelayTK = TagKey.Acquire("_npcSelfresDelay_");
		private static readonly TagKey npcSpeedTK = TagKey.Acquire("_npcSpeed_");
		private static readonly TagKey parakrikChanceTK = TagKey.Acquire("_npcParakrikChance_");
		private static readonly TagKey parakrikCooldownTK = TagKey.Acquire("_npcParakrikCooldown_");
		private static readonly TagKey npcCurseChanceTK = TagKey.Acquire("_npcCurseChance_");
		private static readonly TagKey npcCurseCooldownTK = TagKey.Acquire("_npcCurseCooldown_");
		private static readonly TagKey npcCurseDamageTK = TagKey.Acquire("_npcCurseDamage_");
		private static readonly TagKey sniperChanceTK = TagKey.Acquire("_npcSniperChance_");
		private static readonly TagKey sniperDamageTK = TagKey.Acquire("_npcSniperDamage_");
		private static readonly TagKey summoningChanceTK = TagKey.Acquire("_npcSummoningChance_");
		private static readonly TagKey summoningCooldownTK = TagKey.Acquire("_npcSummoningCooldown_");
		private static readonly TagKey summoningLifetimeTK = TagKey.Acquire("_npcSummoningLifetime_");

		/// <summary>Real-user-reported follow-up to #653's teventStubs batch - per-species
		/// override {min, max} ranges for StunAbilityDef/CriticAbilityDef/
		/// RoundattackAbilityDef's own default fields (same two-layer precedent as
		/// SpellCastRange: an AbilityDef field is the engine-wide default, this is the
		/// per-NPCDef override, confirmed real variance exists in the archive - e.g. stun
		/// chance ranges from 3% on weak monsters to 100% on dragons). Null (the default)
		/// means unconfigured - the consuming combat code falls back to the granted
		/// AbilityDef's own field instead. Unlike StrRange/FameRange (rolled ONCE at
		/// creation), these are rolled fresh every time the ability actually fires (e.g. a
		/// fresh crit-damage roll on every landed hit) - see RollStatRange's own callers in
		/// WeaponSkillDef/NPCAiTimer for where that happens.</summary>
		public int[] StunChance {
			get { return this.GetTag(stunChanceTK) as int[]; }
			set { this.SetTag(stunChanceTK, value); }
		}
		public int[] StunDuration {
			get { return this.GetTag(stunDurationTK) as int[]; }
			set { this.SetTag(stunDurationTK, value); }
		}
		public int[] StunCooldown {
			get { return this.GetTag(stunCooldownTK) as int[]; }
			set { this.SetTag(stunCooldownTK, value); }
		}
		public int[] CriticChance {
			get { return this.GetTag(criticChanceTK) as int[]; }
			set { this.SetTag(criticChanceTK, value); }
		}
		public int[] CriticDamage {
			get { return this.GetTag(criticDamageTK) as int[]; }
			set { this.SetTag(criticDamageTK, value); }
		}
		public int[] RoundattackChance {
			get { return this.GetTag(roundattackChanceTK) as int[]; }
			set { this.SetTag(roundattackChanceTK, value); }
		}
		public int[] RoundattackDamage {
			get { return this.GetTag(roundattackDamageTK) as int[]; }
			set { this.SetTag(roundattackDamageTK, value); }
		}
		public int[] RoundattackCooldown {
			get { return this.GetTag(roundattackCooldownTK) as int[]; }
			set { this.SetTag(roundattackCooldownTK, value); }
		}
		public int[] PullDistance {
			get { return this.GetTag(pullDistanceTK) as int[]; }
			set { this.SetTag(pullDistanceTK, value); }
		}
		/// <summary>Real archive semantics (confirmed in Bleed.scp): unset means the chance
		/// check never even runs, i.e. ALWAYS bleeds - BleedAbilityDef.Chance's own default is
		/// already {1000,1000} (100%) to match, so null here (unconfigured) correctly falls
		/// back to "always" via the normal two-layer precedent, nothing special needed. A
		/// configured override only ever LOWERS it on specific species (real variance 10%-100%
		/// confirmed in the archive).</summary>
		public int[] BleedChance {
			get { return this.GetTag(bleedChanceTK) as int[]; }
			set { this.SetTag(bleedChanceTK, value); }
		}
		/// <summary>Already on the SAME 0-1000 permille scale as every other ability override
		/// here - the real archive's own tag.npc_knockback is on a 0-100 scale, converted ×10
		/// by SphereShardConverter on the way in (see ConvertedCharDef.cs's own comment) so
		/// nothing downstream has to remember a special-case unit for this one field.</summary>
		public int[] KnockbackChance {
			get { return this.GetTag(knockbackChanceTK) as int[]; }
			set { this.SetTag(knockbackChanceTK, value); }
		}
		/// <summary>{min,max} seconds after death before a fresh instance of this NPC respawns -
		/// real archive field is tag.resstimer.</summary>
		/// <summary>#860 - tag.npc_speed {min, max}: what this monster's swing speed is counted
		/// from instead of its max stamina (f_getspeed_npc), rolled once at creation.</summary>
		public int[] NpcSpeed {
			get { return this.GetTag(npcSpeedTK) as int[]; }
			set { this.SetTag(npcSpeedTK, value); }
		}

		public int[] SelfresDelay {
			get { return this.GetTag(selfresDelayTK) as int[]; }
			set { this.SetTag(selfresDelayTK, value); }
		}
		/// <summary>Real archive fields are tag.parakrik_chance/tag.parakrik_delay.</summary>
		public int[] ParaKrikChance {
			get { return this.GetTag(parakrikChanceTK) as int[]; }
			set { this.SetTag(parakrikChanceTK, value); }
		}
		public int[] ParaKrikCooldown {
			get { return this.GetTag(parakrikCooldownTK) as int[]; }
			set { this.SetTag(parakrikCooldownTK, value); }
		}
		/// <summary>Real archive fields are tag.npc_curse_chance/_delay/_dmg.</summary>
		public int[] NpcCurseChance {
			get { return this.GetTag(npcCurseChanceTK) as int[]; }
			set { this.SetTag(npcCurseChanceTK, value); }
		}
		public int[] NpcCurseCooldown {
			get { return this.GetTag(npcCurseCooldownTK) as int[]; }
			set { this.SetTag(npcCurseCooldownTK, value); }
		}
		public int[] NpcCurseDamage {
			get { return this.GetTag(npcCurseDamageTK) as int[]; }
			set { this.SetTag(npcCurseDamageTK, value); }
		}
		/// <summary>Real archive fields are tag.npc_sniper_chance/_dmg.</summary>
		public int[] SniperChance {
			get { return this.GetTag(sniperChanceTK) as int[]; }
			set { this.SetTag(sniperChanceTK, value); }
		}
		public int[] SniperDamage {
			get { return this.GetTag(sniperDamageTK) as int[]; }
			set { this.SetTag(sniperDamageTK, value); }
		}
		/// <summary>Real archive fields are tag.npc_summoning_chance/_time/_removeTime.</summary>
		public int[] SummoningChance {
			get { return this.GetTag(summoningChanceTK) as int[]; }
			set { this.SetTag(summoningChanceTK, value); }
		}
		public int[] SummoningCooldown {
			get { return this.GetTag(summoningCooldownTK) as int[]; }
			set { this.SetTag(summoningCooldownTK, value); }
		}
		public int[] SummoningLifetime {
			get { return this.GetTag(summoningLifetimeTK) as int[]; }
			set { this.SetTag(summoningLifetimeTK, value); }
		}

		//#797/#799 - monster abilities batch 4+ per-species overrides, same {min,max}-or-null shape as
		//the ones above. Each .def field name (lowercased) maps to its own tag key here, so
		//LoadScriptLine handles all of them with one lookup instead of one if-block each.
		private static readonly Dictionary<string, TagKey> moreAbilityRangeTags = new Dictionary<string, TagKey>(StringComparer.OrdinalIgnoreCase) {
			{ "phoenixfiredamage", TagKey.Acquire("_npcPhoenixFireDamage_") },
			{ "explodedamage", TagKey.Acquire("_npcExplodeDamage_") },
			{ "regenerationhp", TagKey.Acquire("_npcRegenerationHp_") },
			{ "regenerationinterval", TagKey.Acquire("_npcRegenerationInterval_") },
			{ "lifeleechamount", TagKey.Acquire("_npcLifeLeechAmount_") },
			{ "reactivearmorpercent", TagKey.Acquire("_npcReactiveArmorPercent_") },
			{ "stamdmgchance", TagKey.Acquire("_npcStamDmgChance_") },
			{ "stamdmgpower", TagKey.Acquire("_npcStamDmgPower_") },
			{ "lightningschance", TagKey.Acquire("_npcLightningsChance_") },
			{ "regenerationdivisor", TagKey.Acquire("_npcRegenerationDivisor_") },
			{ "deathdamage", TagKey.Acquire("_npcDeathDamage_") },
			{ "lifeleechcap", TagKey.Acquire("_npcLifeLeechCap_") },
			{ "acidchance", TagKey.Acquire("_npcAcidChance_") },
			{ "aciddamage", TagKey.Acquire("_npcAcidDamage_") },
			{ "resistcountdownchance", TagKey.Acquire("_npcResistCountdownChance_") },
			{ "resistcountdownamount", TagKey.Acquire("_npcResistCountdownAmount_") },
			{ "lightningsrange", TagKey.Acquire("_npcLightningsRange_") },
			{ "npccursemanachance", TagKey.Acquire("_npcCurseManaChance_") },
			{ "npccursemanacooldown", TagKey.Acquire("_npcCurseManaCooldown_") },
			{ "npccursemanadamage", TagKey.Acquire("_npcCurseManaDamage_") },
			{ "npccursestamchance", TagKey.Acquire("_npcCurseStamChance_") },
			{ "npccursestamcooldown", TagKey.Acquire("_npcCurseStamCooldown_") },
			{ "npccursestamdamage", TagKey.Acquire("_npcCurseStamDamage_") },
			{ "pushallchance", TagKey.Acquire("_npcPushallChance_") },
			{ "pushallcooldown", TagKey.Acquire("_npcPushallCooldown_") },
			{ "magicshieldstrength", TagKey.Acquire("_npcMagicShieldStrength_") },
			{ "dupecount", TagKey.Acquire("_npcDupeCount_") },
			{ "backstabdamagereducer", TagKey.Acquire("_npcBackstabDamageReducer_") },
			{ "multishotchance", TagKey.Acquire("_npcMultishotChance_") },
			{ "multishotcooldown", TagKey.Acquire("_npcMultishotCooldown_") },
			{ "multishotdamage", TagKey.Acquire("_npcMultishotDamage_") },
			{ "dispelbombchance", TagKey.Acquire("_npcDispelBombChance_") },
			{ "dispelbombrange", TagKey.Acquire("_npcDispelBombRange_") },
			{ "dispelbombcooldown", TagKey.Acquire("_npcDispelBombCooldown_") },
			{ "warcrycooldown", TagKey.Acquire("_npcWarcryCooldown_") },
			//#1022 dragons: Erebor's tag.dragon_fire_*, dragon_fire_bomb_*, fire_dragon_round_*, dragon_fire_cage_*, dragon_fire_aura_*, bahenny_drak_aura_*
			{ "firedragonchance", TagKey.Acquire("_npcFireDragonChance_") },
			{ "firedragoncooldown", TagKey.Acquire("_npcFireDragonCooldown_") },
			{ "firedragondamage", TagKey.Acquire("_npcFireDragonDamage_") },
			{ "firedragonfiretype", TagKey.Acquire("_npcFireDragonFireType_") },
			{ "firedragonbombchance", TagKey.Acquire("_npcFireDragonBombChance_") },
			{ "firedragonbombcooldown", TagKey.Acquire("_npcFireDragonBombCooldown_") },
			{ "firedragonbombdamage", TagKey.Acquire("_npcFireDragonBombDamage_") },
			{ "firedragonbombfiretype", TagKey.Acquire("_npcFireDragonBombFireType_") },
			{ "firedragonroundchance", TagKey.Acquire("_npcFireDragonRoundChance_") },
			{ "firedragonroundcooldown", TagKey.Acquire("_npcFireDragonRoundCooldown_") },
			{ "firedragonrounddamage", TagKey.Acquire("_npcFireDragonRoundDamage_") },
			{ "firedragonroundfiretype", TagKey.Acquire("_npcFireDragonRoundFireType_") },
			{ "firedragonroundturns", TagKey.Acquire("_npcFireDragonRoundTurns_") },
			{ "firedragoncagechance", TagKey.Acquire("_npcFireDragonCageChance_") },
			{ "firedragoncagecooldown", TagKey.Acquire("_npcFireDragonCageCooldown_") },
			{ "firedragoncagefiretype", TagKey.Acquire("_npcFireDragonCageFireType_") },
			{ "firedragoncageduration", TagKey.Acquire("_npcFireDragonCageDuration_") },
			{ "firedragonaurachance", TagKey.Acquire("_npcFireDragonAuraChance_") },
			{ "firedragonauracooldown", TagKey.Acquire("_npcFireDragonAuraCooldown_") },
			{ "firedragonaurainterval", TagKey.Acquire("_npcFireDragonAuraInterval_") },
			{ "firedragonauraticks", TagKey.Acquire("_npcFireDragonAuraTicks_") },
			{ "firedragonauradamage", TagKey.Acquire("_npcFireDragonAuraDamage_") },
			{ "firedragonauraresist", TagKey.Acquire("_npcFireDragonAuraResist_") },
			{ "swampdragonaurachance", TagKey.Acquire("_npcSwampDragonAuraChance_") },
			{ "swampdragonauracooldown", TagKey.Acquire("_npcSwampDragonAuraCooldown_") },
			{ "swampdragonaurainterval", TagKey.Acquire("_npcSwampDragonAuraInterval_") },
			{ "swampdragonauraticks", TagKey.Acquire("_npcSwampDragonAuraTicks_") },
			{ "swampdragonauradamage", TagKey.Acquire("_npcSwampDragonAuraDamage_") },
			//#1022 the dying bosses: Erebor's tag.e_explosionlava_*, tag.bomba_boy_dmg
			{ "lavabombchance", TagKey.Acquire("_npcLavaBombChance_") },
			{ "lavabombrange", TagKey.Acquire("_npcLavaBombRange_") },
			{ "lavabombcooldown", TagKey.Acquire("_npcLavaBombCooldown_") },
			{ "bombaboydamage", TagKey.Acquire("_npcBombaBoyDamage_") },
			//#1022 Halda Molle: Erebor's tag.e_copy_chance, e_frosty_wave_*, warniak_type, warniak_count, e_warniak_hit_chance
			{ "rozdelenicopychance", TagKey.Acquire("_npcRozdeleniCopyChance_") },
			{ "frostwavechance", TagKey.Acquire("_npcFrostWaveChance_") },
			{ "frostwavestrength", TagKey.Acquire("_npcFrostWaveStrength_") },
			{ "warniaktype", TagKey.Acquire("_npcWarniakType_") },
			{ "warniakcount", TagKey.Acquire("_npcWarniakCount_") },
			{ "warniakdrophitchance", TagKey.Acquire("_npcWarniakDropHitChance_") },
			//#1022 HCN tombs: Erebor's tag.npc_port_distance (f_npc_portTimer), tag.hcn_madnessAbilityChance/Strength
			{ "npcportdistance", TagKey.Acquire("_npcPortDistance_") },
			{ "hcnmadnesschance", TagKey.Acquire("_npcHcnMadnessChance_") },
			{ "hcnmadnessstrength", TagKey.Acquire("_npcHcnMadnessStrength_") }
		};

		/// <summary>#1022 - a per-species tunable by its .def field name, null when unset or unknown.</summary>
		internal int[] AbilityTuning(string fieldName) {
			return moreAbilityRangeTags.TryGetValue(fieldName, out TagKey key) ? (this.GetTag(key) as int[]) : null;
		}

		private int[] GetAbilityRange(string fieldName) {
			return this.GetTag(moreAbilityRangeTags[fieldName]) as int[];
		}

		private void SetAbilityRange(string fieldName, int[] value) {
			this.SetTag(moreAbilityRangeTags[fieldName], value);
		}

		/// <summary>Real archive field is tag.phoenix_fire_damage.</summary>
		public int[] PhoenixFireDamage {
			get { return this.GetAbilityRange("phoenixfiredamage"); }
			set { this.SetAbilityRange("phoenixfiredamage", value); }
		}
		/// <summary>Real archive field is tag.explode_dmg.</summary>
		public int[] ExplodeDamage {
			get { return this.GetAbilityRange("explodedamage"); }
			set { this.SetAbilityRange("explodedamage", value); }
		}
		/// <summary>Real archive fields are tag.regeneration_hp/_speed (seconds).</summary>
		public int[] RegenerationHp {
			get { return this.GetAbilityRange("regenerationhp"); }
			set { this.SetAbilityRange("regenerationhp", value); }
		}
		public int[] RegenerationInterval {
			get { return this.GetAbilityRange("regenerationinterval"); }
			set { this.SetAbilityRange("regenerationinterval", value); }
		}
		/// <summary>Real archive field is tag.npc_lifeLeech.</summary>
		public int[] LifeLeechAmount {
			get { return this.GetAbilityRange("lifeleechamount"); }
			set { this.SetAbilityRange("lifeleechamount", value); }
		}
		/// <summary>Real archive field is tag.reactivearmor_damagePerc (percent).</summary>
		public int[] ReactiveArmorPercent {
			get { return this.GetAbilityRange("reactivearmorpercent"); }
			set { this.SetAbilityRange("reactivearmorpercent", value); }
		}
		/// <summary>Real archive fields are tag.stam_dmg_chance/_power.</summary>
		public int[] StamDmgChance {
			get { return this.GetAbilityRange("stamdmgchance"); }
			set { this.SetAbilityRange("stamdmgchance", value); }
		}
		public int[] StamDmgPower {
			get { return this.GetAbilityRange("stamdmgpower"); }
			set { this.SetAbilityRange("stamdmgpower", value); }
		}
		/// <summary>#1022 - real archive field is tag.npc_lifeLeech_cap.</summary>
		public int[] LifeLeechCap {
			get { return this.GetAbilityRange("lifeleechcap"); }
			set { this.SetAbilityRange("lifeleechcap", value); }
		}
		/// <summary>#1022 - real archive fields are tag.acid_ppst and tag.acid_dmg.</summary>
		public int[] AcidChance {
			get { return this.GetAbilityRange("acidchance"); }
			set { this.SetAbilityRange("acidchance", value); }
		}
		public int[] AcidDamage {
			get { return this.GetAbilityRange("aciddamage"); }
			set { this.SetAbilityRange("aciddamage", value); }
		}
		/// <summary>#1022 - real archive field is tag.death_damage.</summary>
		public int[] DeathDamage {
			get { return this.GetAbilityRange("deathdamage"); }
			set { this.SetAbilityRange("deathdamage", value); }
		}
		/// <summary>#1022 - real archive field is tag.regespeed.</summary>
		public int[] RegenerationDivisor {
			get { return this.GetAbilityRange("regenerationdivisor"); }
			set { this.SetAbilityRange("regenerationdivisor", value); }
		}
		/// <summary>#1022 - real archive fields are tag.resist_countdown_chance/_amount.</summary>
		public int[] ResistCountdownChance {
			get { return this.GetAbilityRange("resistcountdownchance"); }
			set { this.SetAbilityRange("resistcountdownchance", value); }
		}
		public int[] ResistCountdownAmount {
			get { return this.GetAbilityRange("resistcountdownamount"); }
			set { this.SetAbilityRange("resistcountdownamount", value); }
		}
		/// <summary>Real archive fields are tag.e_lightnings_chance/_range.</summary>
		public int[] LightningsChance {
			get { return this.GetAbilityRange("lightningschance"); }
			set { this.SetAbilityRange("lightningschance", value); }
		}
		public int[] LightningsRange {
			get { return this.GetAbilityRange("lightningsrange"); }
			set { this.SetAbilityRange("lightningsrange", value); }
		}
		/// <summary>Real archive fields are tag.npc_curse_mana_chance/_delay/_dmg.</summary>
		public int[] NpcCurseManaChance {
			get { return this.GetAbilityRange("npccursemanachance"); }
			set { this.SetAbilityRange("npccursemanachance", value); }
		}
		public int[] NpcCurseManaCooldown {
			get { return this.GetAbilityRange("npccursemanacooldown"); }
			set { this.SetAbilityRange("npccursemanacooldown", value); }
		}
		public int[] NpcCurseManaDamage {
			get { return this.GetAbilityRange("npccursemanadamage"); }
			set { this.SetAbilityRange("npccursemanadamage", value); }
		}
		/// <summary>Real archive fields are tag.npc_curse_stam_chance/_delay/_dmg.</summary>
		public int[] NpcCurseStamChance {
			get { return this.GetAbilityRange("npccursestamchance"); }
			set { this.SetAbilityRange("npccursestamchance", value); }
		}
		public int[] NpcCurseStamCooldown {
			get { return this.GetAbilityRange("npccursestamcooldown"); }
			set { this.SetAbilityRange("npccursestamcooldown", value); }
		}
		public int[] NpcCurseStamDamage {
			get { return this.GetAbilityRange("npccursestamdamage"); }
			set { this.SetAbilityRange("npccursestamdamage", value); }
		}
		/// <summary>Real archive fields are tag.e_pushall_chance/_time (seconds).</summary>
		public int[] PushallChance {
			get { return this.GetAbilityRange("pushallchance"); }
			set { this.SetAbilityRange("pushallchance", value); }
		}
		public int[] PushallCooldown {
			get { return this.GetAbilityRange("pushallcooldown"); }
			set { this.SetAbilityRange("pushallcooldown", value); }
		}
		/// <summary>Real archive field is tag.npc_magicShield (percent of a swing absorbed).</summary>
		public int[] MagicShieldStrength {
			get { return this.GetAbilityRange("magicshieldstrength"); }
			set { this.SetAbilityRange("magicshieldstrength", value); }
		}
		/// <summary>Real archive field is tag.dupecount (how many copies e_duplicator may make).</summary>
		public int[] DupeCount {
			get { return this.GetAbilityRange("dupecount"); }
			set { this.SetAbilityRange("dupecount", value); }
		}
		/// <summary>Real archive field is tag.backstub_damageReducer (permille of the ambush bonus kept).</summary>
		public int[] BackstabDamageReducer {
			get { return this.GetAbilityRange("backstabdamagereducer"); }
			set { this.SetAbilityRange("backstabdamagereducer", value); }
		}
		/// <summary>Real archive fields are tag.multishot_chance/_delay (seconds)/_dmg.</summary>
		public int[] MultishotChance {
			get { return this.GetAbilityRange("multishotchance"); }
			set { this.SetAbilityRange("multishotchance", value); }
		}
		public int[] MultishotCooldown {
			get { return this.GetAbilityRange("multishotcooldown"); }
			set { this.SetAbilityRange("multishotcooldown", value); }
		}
		public int[] MultishotDamage {
			get { return this.GetAbilityRange("multishotdamage"); }
			set { this.SetAbilityRange("multishotdamage", value); }
		}
		/// <summary>Real archive fields are tag.e_explosiondispell_chance/_range/_time (seconds).</summary>
		public int[] DispelBombChance {
			get { return this.GetAbilityRange("dispelbombchance"); }
			set { this.SetAbilityRange("dispelbombchance", value); }
		}
		public int[] DispelBombRange {
			get { return this.GetAbilityRange("dispelbombrange"); }
			set { this.SetAbilityRange("dispelbombrange", value); }
		}
		public int[] DispelBombCooldown {
			get { return this.GetAbilityRange("dispelbombcooldown"); }
			set { this.SetAbilityRange("dispelbombcooldown", value); }
		}
		/// <summary>Real archive field is tag.NPC_Warcry_delay - raw deciseconds, same convention as StunCooldown.</summary>
		public int[] WarcryCooldown {
			get { return this.GetAbilityRange("warcrycooldown"); }
			set { this.SetAbilityRange("warcrycooldown", value); }
		}

		private static readonly TagKey summonedCreaturesTK = TagKey.Acquire("_npcSummonedCreatures_");
		private static readonly TagKey battleSummonsTK = TagKey.Acquire("_npcBattleSummons_");

		/// <summary>#1022 - e_npc_summoner's list, tag.npc_summoner_char[N].</summary>
		public List<NPCDef> BattleSummons {
			get { return this.GetTag(battleSummonsTK) as List<NPCDef>; }
			set { this.SetTag(battleSummonsTK, value); }
		}


		/// <summary>e_npc_summoning's own per-species reinforcement list (real archive fields
		/// tag.npc_summoning_char[i]/_count) - same list-of-NPCDef shape as the already-shipped
		/// Spells (#564). When non-empty, SummonerAbilityDef's consumer spawns ONE instance of
		/// EVERY entry here per proc (matching the archive's own loop-over-all-indices), not a
		/// random pick.</summary>
		public List<NPCDef> SummonedCreatures {
			get { return this.GetTag(summonedCreaturesTK) as List<NPCDef>; }
			set { this.SetTag(summonedCreaturesTK, value); }
		}

		private static readonly TagKey fameRangeTK = TagKey.Acquire("_fameRange_");
		private static readonly TagKey karmaRangeTK = TagKey.Acquire("_karmaRange_");

		/// <summary>Starting Fame/Karma {min, max} range for a fresh NPC of this type (#560),
		/// rolled once at creation by NPC.On_Create - same shape as StrRange/DexRange/IntRange
		/// (#559), EXCEPT Fame/Karma can legitimately roll to a real negative value in source
		/// (e.g. c_titan's KARMA={-5000 -5999}) - #559's "-1 means unconfigured" sentinel would
		/// collide with a real negative roll, so RollFame/RollKarma return a nullable int?
		/// instead (null = unconfigured, leave the Character default of 0 untouched).</summary>
		public int[] FameRange {
			get { return this.GetTag(fameRangeTK) as int[]; }
			set { this.SetTag(fameRangeTK, value); }
		}
		public int[] KarmaRange {
			get { return this.GetTag(karmaRangeTK) as int[]; }
			set { this.SetTag(karmaRangeTK, value); }
		}

		/// <summary>Rolls a value in [min, max] (inclusive) from `range`, or null if `range` is
		/// unset (unconfigured). Unlike RollStatRange, this can genuinely return a negative
		/// rolled value - use the null check, not a sentinel value, to detect "unconfigured".</summary>
		private static int? RollNullableStatRange(int[] range) {
			if (range == null) {
				return null;
			}
			if (range[0] >= range[1]) {
				return range[0];
			}
			return Globals.dice.Next(range[0], range[1] + 1);
		}

		public int? RollFame() { return RollNullableStatRange(this.FameRange); }
		public int? RollKarma() { return RollNullableStatRange(this.KarmaRange); }

		private static readonly TagKey maxHitsRangeTK = TagKey.Acquire("_maxHitsRange_");
		private static readonly TagKey maxStamRangeTK = TagKey.Acquire("_maxStamRange_");
		private static readonly TagKey maxManaRangeTK = TagKey.Acquire("_maxManaRange_");

		/// <summary>Starting MaxHits/MaxStam/MaxMana {min, max} range for a fresh NPC of this
		/// type (#561), rolled once at creation by NPC.On_Create - same shape as StrRange/
		/// DexRange/IntRange (#559). Unlike Fame/Karma, these can only ever be a non-negative
		/// quantity in real source (confirmed - e.g. c_titan's own maxmana=0 is the lowest real
		/// value seen, never negative), so #559's simple -1 "unconfigured" sentinel (via
		/// RollStatRange, not the nullable RollNullableStatRange #560 needed) is safe to reuse
		/// here.</summary>
		public int[] MaxHitsRange {
			get { return this.GetTag(maxHitsRangeTK) as int[]; }
			set { this.SetTag(maxHitsRangeTK, value); }
		}
		public int[] MaxStamRange {
			get { return this.GetTag(maxStamRangeTK) as int[]; }
			set { this.SetTag(maxStamRangeTK, value); }
		}
		public int[] MaxManaRange {
			get { return this.GetTag(maxManaRangeTK) as int[]; }
			set { this.SetTag(maxManaRangeTK, value); }
		}

		public int RollMaxHits() { return RollStatRange(this.MaxHitsRange); }
		public int RollMaxStam() { return RollStatRange(this.MaxStamRange); }
		public int RollMaxMana() { return RollStatRange(this.MaxManaRange); }

		private static readonly TagKey skillValueRangesTK = TagKey.Acquire("_skillValueRanges_");

		/// <summary>Starting skill-value {min, max} ranges for a fresh NPC of this type (#562),
		/// keyed by SkillName, rolled once at creation by NPC.On_Create via Character.
		/// SetRealSkillValue. A generic dictionary rather than one property per skill (unlike
		/// StrRange/FameRange/etc) since real chardefs set any of ~25-29 different skills, not a
		/// small fixed set - the .def field name for a skill line is the literal SkillName enum
		/// member name (e.g. "Wrestling = 700 800"), matched case-insensitively in
		/// LoadScriptLine's fallback below.</summary>
		public Dictionary<SkillName, int[]> SkillValueRanges {
			get { return this.GetTag(skillValueRangesTK) as Dictionary<SkillName, int[]>; }
			set { this.SetTag(skillValueRangesTK, value); }
		}

		/// <summary>Rolls the configured range for `skill`, or -1 if this NPCDef never
		/// configured a starting value for it.</summary>
		public int RollSkillValue(SkillName skill) {
			Dictionary<SkillName, int[]> ranges = this.SkillValueRanges;
			int[] range;
			if ((ranges == null) || !ranges.TryGetValue(skill, out range)) {
				return -1;
			}
			return RollStatRange(range);
		}

		//real archive resist tags are frequently a Sphere {min max}/<eval {min max}> range
		//(e.g. c_titan's tag.resist_para=<eval {700 800}>, meant to roll a different value
		//per spawn - UO precedent: a titan's paralyze resist genuinely varies instance to
		//instance, not a fixed 750 every time) - these exact Character/CharacterDef property
		//names (Character.cs's own ResistX wrapper properties), kept in sync by hand since
		//Character.ct's Def: section has no reflection-friendly "list all resist fields"
		//mechanism of its own.
		private static readonly HashSet<string> knownResistFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"ResistMagic", "ResistFire", "ResistElectric", "ResistAcid", "ResistCold",
			"ResistPoison", "ResistMystical", "ResistPhysical", "ResistSlashing",
			"ResistStabbing", "ResistSharp", "ResistBlunt", "ResistArchery", "ResistBleed", "ResistSummon",
			"ResistDragon", "ResistParalyse", "ResistCurse"
		};

		private static readonly TagKey resistRangesTK = TagKey.Acquire("_resistRanges_");

		/// <summary>Starting Resist* {min, max} ranges for a fresh NPC of this type, keyed by
		/// the real Character property name (e.g. "ResistPoison") - same generic-dictionary
		/// shape as SkillValueRanges above, since there are 17 resist fields, not a small fixed
		/// set worth a dedicated property each. Rolled once at creation by NPC.On_Create, which
		/// sets the rolled value directly on the live Character instance (via reflection, since
		/// the property name is only known as a string) - Character's own ResistX setters
		/// already store it as a delta against this NPCDef's own (now otherwise-unused) base
		/// Resist* scalar, exactly like the Str/Dex/Int precedent (#559) above.</summary>
		public Dictionary<string, int[]> ResistRanges {
			get { return this.GetTag(resistRangesTK) as Dictionary<string, int[]>; }
			set { this.SetTag(resistRangesTK, value); }
		}

		/// <summary>One "Equip = defname[, amount or {min max}]" line (#563) - the real archive's
		/// ITEMNEWBIE grants. AmountRange is null when the line didn't specify one (just equip
		/// one of the item).</summary>
		public class StartingEquipmentEntry {
			public string Defname;
			public int[] AmountRange;

			/// <summary>Raw, unresolved "EquipColor = ..." text (null = no per-item color
			/// override) - the real archive's own "ITEMNEWBIE=.../COLOR=..." pairing, which
			/// colors THIS item, not the character itself (see the converter's own
			/// "pendingEquipColorTarget" comment). Resolved at creation time in NPC.On_Create -
			/// either a plain color expression (hex/symbolic range, same text a top-level
			/// Color= line would carry) or the literal "match_hair" keyword, meaning "use
			/// whatever color the most recently equipped hair/facial-hair item got."</summary>
			public string Color;

			/// <summary>#793 - "EquipName = ...": the item's own name (Erebor's "ITEMNEWBIE=.../NAME=..."
			/// in @NPCRestock, e.g. a quest NPC's named robe). Null = the itemdef's name.</summary>
			public string Name;

			/// <summary>#775 - set for a Sphere weighted random pick, "Equip = {i_bardiche 1 i_halberd 1}":
			/// one of these defnames is rolled per spawn (see PickDefname), Defname then only holds
			/// the raw "{...}" text for messages. Null for a plain single-item entry.</summary>
			public string[] RandomDefnames;
			public int[] RandomWeights;

			/// <summary>The defname to create for this spawn - Defname itself, or a weighted roll among RandomDefnames.</summary>
			public string PickDefname(Random random) {
				if (this.RandomDefnames == null) {
					return this.Defname;
				}
				return PickWeighted(this.RandomDefnames, this.RandomWeights, random);
			}
		}

		/// <summary>#775 - weighted pick: names[i] comes up weights[i] times in sum(weights).</summary>
		public static string PickWeighted(string[] names, int[] weights, Random random) {
			int total = 0;
			foreach (int w in weights) {
				total += w;
			}
			int roll = random.Next(total);
			for (int i = 0; i < names.Length; i++) {
				roll -= weights[i];
				if (roll < 0) {
					return names[i];
				}
			}
			return names[names.Length - 1];
		}

		/// <summary>#775 - splits an "Equip =" value into its item part and the optional ", amount" part.
		/// The item part may be a "{...}" random list, which can itself contain commas.</summary>
		public static void SplitEquipLine(string args, out string itemPart, out string amountPart) {
			string trimmed = args.Trim();
			int split;
			if (trimmed.StartsWith("{", StringComparison.Ordinal)) {
				int close = trimmed.IndexOf('}');
				split = (close < 0) ? -1 : trimmed.IndexOf(',', close);
			} else {
				split = trimmed.IndexOf(',');
			}
			if (split < 0) {
				itemPart = trimmed;
				amountPart = null;
			} else {
				itemPart = trimmed.Substring(0, split).Trim();
				amountPart = trimmed.Substring(split + 1).Trim();
			}
		}

		/// <summary>#775 - parses Sphere's weighted random pick "{name weight name weight ...}" (spaces
		/// and/or commas). A list without weights ("{a b c}") gives every name weight 1. False if the
		/// text isn't a "{...}" list or is malformed (empty, a non-positive weight).</summary>
		public static bool TryParseRandomDefnames(string text, out string[] names, out int[] weights) {
			names = null;
			weights = null;
			string trimmed = text.Trim();
			if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal)) {
				return false;
			}
			string[] tokens = trimmed.Substring(1, trimmed.Length - 2).Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length == 0) {
				return false;
			}
			List<string> nameList = new List<string>();
			List<int> weightList = new List<int>();
			int i = 0;
			while (i < tokens.Length) {
				int weight;
				if (int.TryParse(tokens[i], out weight)) {
					return false; //a weight with no name before it
				}
				nameList.Add(tokens[i]);
				if ((i + 1 < tokens.Length) && int.TryParse(tokens[i + 1], out weight)) {
					if (weight <= 0) {
						return false;
					}
					weightList.Add(weight);
					i += 2;
				} else {
					weightList.Add(1);
					i += 1;
				}
			}
			names = nameList.ToArray();
			weights = weightList.ToArray();
			return true;
		}

		private static readonly TagKey instanceStartingEquipmentTK = TagKey.Acquire("_npcStartingEquipment_");

		/// <summary>Starting-equipment list for a fresh NPC of this type (#563) - each entry
		/// created and handed to ThingDef.Create(this) once at creation by NPC.On_Create, which
		/// equips it automatically if it's equippable (a bow, a spellbook) or drops it into the
		/// backpack otherwise (ammo like arrows). Multiple entries are normal - a real chardef
		/// can have several `Equip =` lines (e.g. a ranger's bow THEN its arrows).</summary>
		public List<StartingEquipmentEntry> StartingEquipment {
			get { return this.GetTag(instanceStartingEquipmentTK) as List<StartingEquipmentEntry>; }
			set { this.SetTag(instanceStartingEquipmentTK, value); }
		}

		private static readonly TagKey lootTK = TagKey.Acquire("_npcLoot_");

		/// <summary>#791 - the monster's loot, Erebor's ON=@NPCRestock "ITEM=" lines ("Loot = ..." in
		/// .def content, see the converter): each "item[,amount]" - an itemdef, a RANDOM_* pick-list
		/// constant or a loot_*/tm_* template - is put into the backpack when the NPC is created
		/// (NPC.On_Create), so it falls to the corpse on death. "Loot = i_gold" is the gold marker
		/// whose amount MonsterLoot sets from the monster's experience.</summary>
		public List<string> Loot {
			get { return this.GetTag(lootTK) as List<string>; }
			set {
				this.SetTag(lootTK, value);
				this.lootRunners = null;
			}
		}

		private LScriptHolder[] lootRunners; //compiled "ITEM = <loot line>" scripts, built on first use

		/// <summary>#791 - creates this NPCDef's loot in `container` (the new NPC's backpack), each
		/// line through the template ITEM op - so random lists, nested templates and "{0 1}"-style
		/// amounts behave exactly as ITEM= does in templates. A broken line is logged and skipped.</summary>
		public void CreateLoot(Thing container) {
			List<string> loot = this.Loot;
			if (loot == null) {
				return;
			}
			if (this.lootRunners == null) {
				LScriptHolder[] runners = new LScriptHolder[loot.Count];
				for (int i = 0; i < loot.Count; i++) {
					try {
						runners[i] = LScriptMain.GetNewSnippetRunner("NPCDef " + this.PrettyDefname, 0, "ITEM = " + loot[i]);
					} catch (Exception e) {
						Logger.WriteWarning("NPCDef " + this.PrettyDefname, "loot line '" + loot[i] + "' doesn't compile: " + e.Message);
					}
				}
				this.lootRunners = runners;
			}
			for (int i = 0; i < this.lootRunners.Length; i++) {
				LScriptHolder runner = this.lootRunners[i];
				if (runner == null) {
					continue;
				}
				string questItem = QuestItems.LootDefname(loot[i]);
				if (QuestItems.IsQuestItem(questItem)) { //#1090 - a quest item is a tag, carried to the corpse, not an item
					QuestItems.Carry(container.TopObj(), questItem);
					continue;
				}
				try {
					runner.Run(container, (ScriptArgs) null);
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteWarning("NPCDef " + this.PrettyDefname, "loot line '" + loot[i] + "' failed: " + e.Message);
				}
			}
		}

		/// <summary>Real archive content rolls starting hair/beard via 3 magic "Equip ="
		/// keywords instead of a literal defname - "random_male_hair"/"random_female_hair"/
		/// "random_facial_hair" (confirmed widespread, e.g. c_m_HEALER/c_f_HEALER) - a native
		/// Sphere random-pick mechanism, not a real ItemDef, so "ThingDef.GetByDefname" always
		/// failed for them (silently dropped with a warning, never actually equipped). Pools
		/// are every real converted T_HAIR/T_BEARD EquippableDef (itemb7.def) - this engine has
		/// no gendered-hairstyle data, so male/female share one pool, matching how the real
		/// client lets either gender wear any hairstyle. Plain mutable public static fields
		/// (not yet GM-tunable via .settings - add a category later if ever worth it).</summary>
		public static string[] MaleOrFemaleHairPool = {
			"i_hair_short", "i_hair_long", "i_hair_ponytail", "i_hair_mohawk",
			"i_hair_pageboy", "i_hair_buns", "i_hair_afro", "i_hair_receding",
			"i_hair_2_pigtails", "i_hair_krisna"
		};

		public static string[] FacialHairPool = {
			"i_beard_long", "i_beard_short", "i_beard_goatee", "i_beard_mustache",
			"i_beard_short_med", "i_beard_long_med", "i_beard_vandyke"
		};

		/// <summary>#728 follow-up - the identical native Sphere "roll me something" mechanism
		/// above also covers "random_pants"/"random_shoes"/"random_skirt" (confirmed widespread
		/// across vendors.def/guards.def/etc - same silent "not a known ItemDef" drop the 3 hair
		/// keywords had before #563). Pools mirror defs.def's own real RANDOM_PANTS/RANDOM_SHOES/
		/// RANDOM_SKIRT Constants groups (unweighted - this engine doesn't parse weighted
		/// Constants-group item lists at all, so a flat uniform pick is the closest real
		/// equivalent without building that whole separate mechanism).</summary>
		public static string[] PantsPool = { "i_pants_short", "i_pants_long" };
		public static string[] ShoesPool = { "i_shoes_plain", "i_sandals", "i_boots_calf" };
		public static string[] SkirtPool = { "i_skirt_long", "i_kilt", "i_skirt_short" };

		private List<AbilityDef> startingAbilities;

		/// <summary>Stub abilities (#4 of the 2026-10-26 monster-testing batch) granted once at
		/// creation by NPC.On_Create, one per legacy TEVENTS= name that didn't resolve to a
		/// real TriggerGroup - see TryResolveUnknownTriggerGroupName below. Each is a real,
		/// zero-effect PassiveAbilityDef (same shape as the pre-existing unimplementedAbilities.
		/// scp stubs) so the monster's real intent is now inspectable via .info's Abilities view
		/// (#651) instead of a silently-dropped load warning - ready for a genuine
		/// implementation later without touching content again.
		/// #1375 - a plain field, not a def value: it's made from the TEVENTS lines when their names resolve, after the scripts
		/// load, and as a tag every save wrote it as if a GM had set it.</summary>
		public List<AbilityDef> StartingAbilities {
			get { return this.startingAbilities; }
			set { this.startingAbilities = value; }
		}

		//#1375 - tags older saves hold on an NPCDef: typed Defs: fields in NPC.ct or plain fields now. An unknown tag line is loaded as a
		//new untyped tag and written into every save after, so they're dropped (a GM's change to one of these in an old save is lost).
		private static readonly string[] retiredSavedTags = { "_aggroMode_", "_npcDefRealm_", "_npcDefTeachesSkill_", "_tamingDifficulty_",
			"_moveRate_", "_npcSpellCastRange_", "_npcIsBoss_", "_npcDragonBrain_", "_npcNoAnimate_", "_npcHypnotizable_",
			"_npcResistSilence_", "_npcMonsterType_", "_npcMonsterType2_", "_npcMonsterType3_", "_npcMagicHorse_", "_npcSerpentForm_",
			"_npcStartingAbilities_" };

		public override void LoadFromSaves(PropsSection input) {
			foreach (string tag in retiredSavedTags) {
				input.TryPopPropsLine("tag." + tag);
			}
			base.LoadFromSaves(input);
		}

		/// <summary>The naming convention a legacy TEVENTS= name maps to its stub AbilityDef
		/// under - deliberately NOT stripping/abbreviating the original name (e.g. "e_monster"
		/// -&gt; "a_tevent_e_monster", not "a_monster") so the mapping is unambiguous, trivially
		/// greppable back to its source, and can never collide with an unrelated real ability
		/// that happens to share a short name.</summary>
		private static string TeventAbilityDefname(string tgName) {
			return "a_tevent_" + tgName;
		}

		protected override bool TryResolveUnknownTriggerGroupName(string tgName) {
			AbilityDef stubDef = AbstractScript.GetByDefname(TeventAbilityDefname(tgName)) as AbilityDef;
			if (stubDef == null) {
				return false; //not one of our known stubs (e.g. a spk_* speech trigger) - warn as before
			}
			if (this.StartingAbilities == null) {
				this.StartingAbilities = new List<AbilityDef>();
			}
			if (!this.StartingAbilities.Contains(stubDef)) {
				this.StartingAbilities.Add(stubDef);
			}
			return true;
		}

		/// <summary>One "Resources = amount defname" entry (#586) - the real archive's
		/// RESOURCES= butcher-yield list (what a player gets by chopping this NPC's corpse
		/// with a sharp weapon, a real UO/Erebor mechanic). The butchering INTERACTION itself
		/// (double-click/.usehand + target corpse + chop) doesn't exist in this engine yet -
		/// see the separate deferred follow-up issue. This is purely the data template.</summary>
		public class ResourceEntry {
			public string Defname;
			public int Amount;
		}

		private static readonly TagKey instanceButcherResourcesTK = TagKey.Acquire("_npcButcherResources_");

		/// <summary>Butcher-yield data for a fresh NPC of this type (#586) - defined here,
		/// consumed nowhere yet. Ready for whenever the corpse-butchering mechanic gets built.</summary>
		public List<ResourceEntry> ButcherResources {
			get { return this.GetTag(instanceButcherResourcesTK) as List<ResourceEntry>; }
			set { this.SetTag(instanceButcherResourcesTK, value); }
		}

		/// <summary>One "FoodType = [amount] category-or-itemdef" entry (#593) - the real
		/// archive's FOODTYPE= field. CategoryOrDefname is either a "t_"-prefixed food
		/// category (resolved against FoodCategories' real-item mapping - see PetHungerSystem)
		/// or a literal itemdef (e.g. dragons eating "i_gold" directly). SatiationValue is the
		/// entry's own leading number - real Sphere's `typedef.maxfood` has no recoverable
		/// value anywhere in the public archive, so (per explicit user decision) this engine
		/// derives MaxFood from these numbers instead, rather than inventing an unrelated flat
		/// default.</summary>
		public class FoodTypeEntry {
			public string CategoryOrDefname;
			public int SatiationValue;
		}

		//Real Sphere's nakrmho formula floors a food item's own per-unit weight at 10 before
		//computing nutrition - reused here as the default SatiationValue for a bare FOODTYPE
		//entry with no explicit leading number (e.g. "FOODTYPE = t_coin,t_ore,t_ingot").
		private const int DefaultFoodSatiationValue = 10;

		private static readonly TagKey instanceFoodTypesTK = TagKey.Acquire("_npcFoodTypes_");

		/// <summary>Acceptable food list for a fresh NPC of this type (#593). Null (the
		/// default, most NPCDefs) means this species was never configured with real food
		/// data - PetHungerSystem deliberately never starts tracking hunger for it at all,
		/// same "incomplete content defaults permissive" precedent used throughout this
		/// engine (ExperienceWorth, attr_decay, etc).</summary>
		public List<FoodTypeEntry> FoodTypes {
			get { return this.GetTag(instanceFoodTypesTK) as List<FoodTypeEntry>; }
			set { this.SetTag(instanceFoodTypesTK, value); }
		}

		/// <summary>Sum of every configured FoodTypes entry's own SatiationValue (0 if
		/// FoodTypes is unset) - see FoodTypeEntry's own doc comment for why this engine
		/// derives MaxFood this way instead of a native Sphere constant.</summary>
		public int MaxFood {
			get {
				List<FoodTypeEntry> types = this.FoodTypes;
				if (types == null) {
					return 0;
				}
				int sum = 0;
				foreach (FoodTypeEntry entry in types) {
					sum += entry.SatiationValue;
				}
				return sum;
			}
		}

		private static int[] ParseStatRange(string filename, int line, string param, string args) {
			string[] parts = args.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 1) {
				int value = ConvertTools.ParseInt32(parts[0]);
				return new int[] { value, value };
			}
			if (parts.Length == 2) {
				return new int[] { ConvertTools.ParseInt32(parts[0]), ConvertTools.ParseInt32(parts[1]) };
			}
			Logger.WriteWarning(filename, line, param + " '" + args + "' must be either a single value or 'min max'.");
			return null;
		}

		private static readonly TagKey defaultMovementTK = TagKey.Acquire("_defaultMovementCapability_");

		/// <summary>The movement capability template new NPCs of this type get on creation (see NPC.On_Create) - each instance gets its own deep copy, not this shared object, so runtime effects can mutate one NPC's capability without affecting every other NPC of the same type.</summary>
		public IMovementSettings DefaultMovementSettings {
			get {
				return (IMovementSettings) this.GetTag(defaultMovementTK);
			}
			set {
				this.SetTag(defaultMovementTK, value);
			}
		}

		/// <summary>
		/// Restores the legacy "CAN = MT_WALK|MT_FLY|..." .def property (converted content,
		/// enhancements #12) as a real DefaultMovementSettings template. Only the subset that
		/// actually maps to IMovementSettings is handled (WALK/RUN -> land, SWIM, FLY) -
		/// MT_USEHANDS/MT_EQUIP/MT_FIRE_IMMUNE/MT_MALE/MT_FEMALE/MT_GHOST/MT_NONMOVER aren't
		/// movement-capability concepts in this engine (equip/gender/immunity/etc - gender in
		/// particular is already derived from the model via CharModelInfo, not a separate flag)
		/// and are intentionally ignored rather than guessed at.
		/// </summary>
		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if (param == "can") {
				this.DefaultMovementSettings = ParseMovementCapability(args);
				return;
			}
			if (param == "combatspell") {
				SpellDef spell = SpellDef.GetByDefname(args);
				if (spell == null) {
					Logger.WriteWarning(filename, line, "combatspell '" + args + "' is not a known SpellDef.");
				}
				this.CombatSpell = spell;
				return;
			}
			if (param == "mountnpc") {
				NPCDef mountDef = ThingDef.GetByDefname(args) as NPCDef;
				if (mountDef == null) {
					Logger.WriteWarning(filename, line, "mountnpc '" + args + "' is not a known NPCDef.");
				}
				this.MountCharDef = mountDef;
				return;
			}
			if (param == "addspell") {
				SpellDef spell = SpellDef.GetByDefname(args);
				if (spell == null) {
					Logger.WriteWarning(filename, line, "addspell '" + args + "' is not a known SpellDef.");
					return;
				}
				if (this.Spells == null) {
					this.Spells = new List<SpellDef>();
				}
				this.Spells.Add(spell);
				return;
			}
			if (param == "experience") {
				string[] parts = args.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 1) {
					int value = ConvertTools.ParseInt32(parts[0]);
					this.ExperienceWorth = new int[] { value, value };
				} else if (parts.Length == 2) {
					this.ExperienceWorth = new int[] { ConvertTools.ParseInt32(parts[0]), ConvertTools.ParseInt32(parts[1]) };
				} else {
					Logger.WriteWarning(filename, line, "experience '" + args + "' must be either a single value or 'min max'.");
				}
				return;
			}
			if (param == "str") {
				this.StrRange = ParseStatRange(filename, line, "str", args);
				return;
			}
			if (param == "dex") {
				this.DexRange = ParseStatRange(filename, line, "dex", args);
				return;
			}
			if (param == "int") {
				this.IntRange = ParseStatRange(filename, line, "int", args);
				return;
			}
			if (param == "fame") {
				this.FameRange = ParseStatRange(filename, line, "fame", args);
				return;
			}
			if (param == "karma") {
				this.KarmaRange = ParseStatRange(filename, line, "karma", args);
				return;
			}
			if (param == "stunchance") {
				this.StunChance = ParseStatRange(filename, line, "stunchance", args);
				return;
			}
			if (param == "stunduration") {
				this.StunDuration = ParseStatRange(filename, line, "stunduration", args);
				return;
			}
			if (param == "stuncooldown") {
				this.StunCooldown = ParseStatRange(filename, line, "stuncooldown", args);
				return;
			}
			if (param == "criticchance") {
				this.CriticChance = ParseStatRange(filename, line, "criticchance", args);
				return;
			}
			if (param == "criticdamage") {
				this.CriticDamage = ParseStatRange(filename, line, "criticdamage", args);
				return;
			}
			if (param == "roundattackchance") {
				this.RoundattackChance = ParseStatRange(filename, line, "roundattackchance", args);
				return;
			}
			if (param == "roundattackdamage") {
				this.RoundattackDamage = ParseStatRange(filename, line, "roundattackdamage", args);
				return;
			}
			if (param == "roundattackcooldown") {
				this.RoundattackCooldown = ParseStatRange(filename, line, "roundattackcooldown", args);
				return;
			}
			if (param == "pulldistance") {
				this.PullDistance = ParseStatRange(filename, line, "pulldistance", args);
				return;
			}
			if (param == "bleedchance") {
				this.BleedChance = ParseStatRange(filename, line, "bleedchance", args);
				return;
			}
			if (param == "knockbackchance") {
				this.KnockbackChance = ParseStatRange(filename, line, "knockbackchance", args);
				return;
			}
			if (param == "npcspeed") {
				this.NpcSpeed = ParseStatRange(filename, line, "npcspeed", args);
				return;
			}
			if (param == "selfresdelay") {
				this.SelfresDelay = ParseStatRange(filename, line, "selfresdelay", args);
				return;
			}
			if (param == "parakrikchance") {
				this.ParaKrikChance = ParseStatRange(filename, line, "parakrikchance", args);
				return;
			}
			if (param == "parakrikcooldown") {
				this.ParaKrikCooldown = ParseStatRange(filename, line, "parakrikcooldown", args);
				return;
			}
			if (param == "npccursechance") {
				this.NpcCurseChance = ParseStatRange(filename, line, "npccursechance", args);
				return;
			}
			if (param == "npccursecooldown") {
				this.NpcCurseCooldown = ParseStatRange(filename, line, "npccursecooldown", args);
				return;
			}
			if (param == "npccursedamage") {
				this.NpcCurseDamage = ParseStatRange(filename, line, "npccursedamage", args);
				return;
			}
			if (param == "sniperchance") {
				this.SniperChance = ParseStatRange(filename, line, "sniperchance", args);
				return;
			}
			if (param == "sniperdamage") {
				this.SniperDamage = ParseStatRange(filename, line, "sniperdamage", args);
				return;
			}
			if (param == "summoningchance") {
				this.SummoningChance = ParseStatRange(filename, line, "summoningchance", args);
				return;
			}
			if (param == "summoningcooldown") {
				this.SummoningCooldown = ParseStatRange(filename, line, "summoningcooldown", args);
				return;
			}
			if (param == "summoninglifetime") {
				this.SummoningLifetime = ParseStatRange(filename, line, "summoninglifetime", args);
				return;
			}
			if (moreAbilityRangeTags.ContainsKey(param)) {
				this.SetAbilityRange(param, ParseStatRange(filename, line, param, args));
				return;
			}
			if (param == "addbattlesummon") {
				NPCDef battleSummonDef = ThingDef.GetByDefname(args) as NPCDef;
				if (battleSummonDef == null) {
					Logger.WriteWarning(filename, line, "addbattlesummon '" + args + "' is not a known NPCDef.");
					return;
				}
				if (this.BattleSummons == null) {
					this.BattleSummons = new List<NPCDef>();
				}
				this.BattleSummons.Add(battleSummonDef);
				return;
			}
			if (param == "addsummon") {
				NPCDef summonDef = ThingDef.GetByDefname(args) as NPCDef;
				if (summonDef == null) {
					Logger.WriteWarning(filename, line, "addsummon '" + args + "' is not a known NPCDef.");
					return;
				}
				if (this.SummonedCreatures == null) {
					this.SummonedCreatures = new List<NPCDef>();
				}
				this.SummonedCreatures.Add(summonDef);
				return;
			}
			if (param == "maxhits") {
				this.MaxHitsRange = ParseStatRange(filename, line, "maxhits", args);
				return;
			}
			if (param == "maxstam") {
				this.MaxStamRange = ParseStatRange(filename, line, "maxstam", args);
				return;
			}
			if (param == "maxmana") {
				this.MaxManaRange = ParseStatRange(filename, line, "maxmana", args);
				return;
			}
			if (param == "loot") {
				//#791 - "Loot = item[,amount]", one line per Erebor @NPCRestock ITEM= line
				List<string> loot = this.Loot ?? new List<string>();
				loot.Add(args.Trim());
				this.Loot = loot;
				return;
			}
			if (param == "equip") {
				//#563 - "Equip = defname" or "Equip = defname, amount" or "Equip = defname, min max"
				//#775 - defname can also be a Sphere weighted random pick, "{i_bardiche 1 i_halberd 1}"
				string defname, amountText;
				SplitEquipLine(args, out defname, out amountText);
				int[] amountRange = null;
				if (amountText != null) {
					amountRange = ParseStatRange(filename, line, "equip amount", amountText);
				}
				StartingEquipmentEntry entry = new StartingEquipmentEntry { Defname = defname, AmountRange = amountRange };
				if (defname.StartsWith("{", StringComparison.Ordinal) &&
						!TryParseRandomDefnames(defname, out entry.RandomDefnames, out entry.RandomWeights)) {
					Logger.WriteWarning(filename, line, "equip random list '" + defname + "' must be '{defname weight defname weight ...}'.");
					return;
				}
				if (this.StartingEquipment == null) {
					this.StartingEquipment = new List<StartingEquipmentEntry>();
				}
				this.StartingEquipment.Add(entry);
				return;
			}
			if (param == "equipcolor") {
				//the real archive's "COLOR=..." line immediately following an "ITEMNEWBIE="
				//line colors THAT item, not the character - see StartingEquipmentEntry.Color's
				//own doc comment. Always belongs to whichever "Equip =" entry was just added.
				if (this.StartingEquipment != null && this.StartingEquipment.Count > 0) {
					this.StartingEquipment[this.StartingEquipment.Count - 1].Color = args.Trim();
				}
				return;
			}
			if (param == "equipname") {
				//#793 - same pairing as equipcolor, for Erebor's "ITEMNEWBIE=.../NAME=..."
				if (this.StartingEquipment != null && this.StartingEquipment.Count > 0) {
					this.StartingEquipment[this.StartingEquipment.Count - 1].Name = args.Trim();
				}
				return;
			}
			if (param == "resources") {
				//#586 - "Resources = amount defname, amount defname, ..." - the real archive's
				//butcher-yield list (corpse-chopping data, not death loot - the butchering
				//interaction itself is a separate, not-yet-built mechanic, see #587).
				List<ResourceEntry> entries = new List<ResourceEntry>();
				string[] resourceParts = args.Split(',');
				foreach (string part in resourceParts) {
					string[] tokens = part.Trim().Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length == 2) {
						entries.Add(new ResourceEntry { Amount = ConvertTools.ParseInt32(tokens[0]), Defname = tokens[1] });
					} else {
						Logger.WriteWarning(filename, line, "resources entry '" + part + "' must be 'amount defname'.");
					}
				}
				this.ButcherResources = entries;
				return;
			}
			if (param == "foodtype") {
				//#593 - "FoodType = [amount] category-or-itemdef, ..." - the real archive's
				//FOODTYPE= field. Unlike "resources" above, an entry's leading amount is
				//OPTIONAL (bare category entries like "t_coin" are real, confirmed in archive
				//content) - falls back to DefaultFoodSatiationValue when omitted.
				List<FoodTypeEntry> entries = new List<FoodTypeEntry>();
				foreach (string part in args.Split(',')) {
					string trimmed = part.Trim();
					if (trimmed.Length == 0) {
						continue;
					}
					string[] tokens = trimmed.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length == 2) {
						entries.Add(new FoodTypeEntry { SatiationValue = ConvertTools.ParseInt32(tokens[0]), CategoryOrDefname = tokens[1] });
					} else if (tokens.Length == 1) {
						entries.Add(new FoodTypeEntry { SatiationValue = DefaultFoodSatiationValue, CategoryOrDefname = tokens[0] });
					} else {
						Logger.WriteWarning(filename, line, "foodtype entry '" + part + "' must be '[amount] category'.");
					}
				}
				this.FoodTypes = entries;
				return;
			}
			//a .def field name matching a real Character ResistX property (case-insensitive)
			//is a starting resist {min max} range, e.g. "ResistParalyse = 700 800" or the
			//single-value "ResistBlunt = 150" shape - same ParseStatRange helper and "never
			//forward to base.LoadScriptLine" precedent as str/dex/int/fame/karma/maxhits
			//above, so the NPCDef's own (otherwise redundant) base Resist* scalar never needs
			//to be kept in sync - only the per-instance rolled value, set directly on the live
			//Character by NPC.On_Create, actually matters.
			if (knownResistFieldNames.Contains(param)) {
				int[] resistRange = ParseStatRange(filename, line, param, args);
				if (resistRange != null) {
					if (this.ResistRanges == null) {
						this.ResistRanges = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
					}
					//stored key may not be in Character's exact PascalCase (the .def parser's
					//own casing for `param` varies) - NPC.On_Create's roll step below looks the
					//matching Character property up case-insensitively, so this is fine as-is.
					this.ResistRanges[param] = resistRange;
				}
				return;
			}
			//#562 - a .def field name matching a SkillName enum member (case-insensitive) is a
			//starting skill-value range, e.g. "Wrestling = 700 800" or "MagicResist = 900"
			SkillName skillName;
			if (Enum.TryParse(param, true, out skillName)) {
				int[] range = ParseStatRange(filename, line, param, args);
				if (range != null) {
					if (this.SkillValueRanges == null) {
						this.SkillValueRanges = new Dictionary<SkillName, int[]>();
					}
					this.SkillValueRanges[skillName] = range;
				}
				return;
			}
			base.LoadScriptLine(filename, line, param, args);
		}

		private static DynamicMovementSettings ParseMovementCapability(string args) {
			DynamicMovementSettings settings = new DynamicMovementSettings();
			settings.canCrossLand = false; //start from nothing, grant only what's explicitly listed
			foreach (string rawToken in args.Split('|')) {
				switch (rawToken.Trim().ToUpperInvariant()) {
					case "MT_WALK":
					case "MT_RUN":
						settings.canCrossLand = true;
						break;
					case "MT_SWIM":
						settings.canSwim = true;
						break;
					case "MT_FLY":
						settings.canFly = true;
						break;
				}
			}
			return settings;
		}
	}

	[ViewableClass]
	public partial class NPC : Character {

		private static readonly TagKey instanceAggroModeTK = TagKey.Acquire("_npcAggroMode_");

		/// <summary>Per-instance AggroMode (#260) - seeded from this NPC's own NPCDef.AggroMode
		/// at creation, then lives independently from there on. All live aggro/notoriety logic
		/// (ShouldAggroOn, Notoriety.GetHighlightColor) reads THIS, not the Def-level value -
		/// NPCDef.AggroMode remains only the species-level default new spawns start from.
		/// Deliberately a plain public property (not Def-level) so .info's existing reflection-
		/// based editable-field support (confirmed to handle enums via a button picker) surfaces
		/// and lets a GM fix one specific monster's aggro behavior without needing to touch - or
		/// re-author - its whole species' .def content.</summary>
		public AggroMode AggroMode {
			get {
				object tag = this.GetTag(instanceAggroModeTK);
				if (tag is AggroMode mode) {
					return mode;
				}
				if ((tag is int intTag) && Enum.IsDefined(typeof(AggroMode), intTag)) {
					return (AggroMode) intTag; //defensive coercion, see NPCDef.AggroMode's own doc comment (#265)
				}
				return AggroMode.Calm;
			}
			set {
				this.SetTag(instanceAggroModeTK, value);
			}
		}

		private static readonly TagKey instanceExperienceRewardTK = TagKey.Acquire("_npcExperienceReward_");

		/// <summary>Per-instance experience reward (#260) - rolled ONCE from this NPC's own
		/// NPCDef.ExperienceWorth {min,max} range at creation, then fixed for this instance's
		/// lifetime (not re-rolled on every death-grant) so a GM can see and manually adjust
		/// exactly what THIS monster is worth via .info, same reasoning as AggroMode above.</summary>
		public int ExperienceReward {
			get {
				object tag = this.GetTag(instanceExperienceRewardTK);
				if (tag is int reward) {
					return reward;
				}
				return 0;
			}
			set {
				this.SetTag(instanceExperienceRewardTK, value);
			}
		}

		private static readonly TagKey instanceMoveRateTK = TagKey.Acquire("_npcMoveRate_");

		/// <summary>Per-instance MoveRate (#599) - seeded from this NPC's own NPCDef.MoveRate
		/// at creation, then lives independently from there on. NPCAiTimer's own think-tick
		/// cadence reads THIS, not the Def-level value - NPCDef.MoveRate remains only the
		/// species-level default new spawns start from. Same "plain public property, not
		/// Def-level, so .info's reflection-based editor surfaces it" reasoning as AggroMode/
		/// ExperienceReward above - #590 shipped MoveRate without this per-instance mirror,
		/// which is exactly why a GM couldn't retune one specific spawned monster's speed.
		/// #603 - the setter also reschedules NPCAiTimer's own tick cadence immediately, so a
		/// live .info edit takes effect on this NPC's very next think-tick, not just at the
		/// next natural reschedule.</summary>
		public double MoveRate {
			get {
				object tag = this.GetTag(instanceMoveRateTK);
				if (tag is double rate) {
					return rate;
				}
				if (tag is int intTag) {
					return intTag;
				}
				return 3.0;
			}
			set {
				this.SetTag(instanceMoveRateTK, value);
				NPCAiTimer timer = NPCAiTimer.GetFor(this);
				if (timer != null) {
					timer.RescheduleForMoveRate(value);
				}
			}
		}

		private static bool IsHairRandomizerKeyword(string defname) {
			return string.Equals(defname, "random_male_hair", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(defname, "random_female_hair", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(defname, "random_woman_hair", StringComparison.OrdinalIgnoreCase) //a single real content use (specNPCSafira.def) - same pool, no separate gendered hair data
				|| string.Equals(defname, "random_facial_hair", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Resolves a "StartingEquipment" entry's Defname - either a real ItemDef, or
		/// (if it's one of the real archive's native Sphere "roll me something" random-pick
		/// keywords) a random real pick from the matching pool instead. #563 covered the hair/
		/// beard keywords only; #728 found the identical silent-drop symptom for
		/// random_pants/random_shoes/random_skirt - same mechanism, just covering every real
		/// keyword actually used across the converted content instead of hair alone.</summary>
		private static ItemDef ResolveStartingEquipmentItemDef(string defname, bool isHairKeyword) {
			string[] pool = null;
			if (isHairKeyword) {
				pool = string.Equals(defname, "random_facial_hair", StringComparison.OrdinalIgnoreCase)
					? NPCDef.FacialHairPool
					: NPCDef.MaleOrFemaleHairPool;
			} else if (string.Equals(defname, "random_pants", StringComparison.OrdinalIgnoreCase)) {
				pool = NPCDef.PantsPool;
			} else if (string.Equals(defname, "random_shoes", StringComparison.OrdinalIgnoreCase)) {
				pool = NPCDef.ShoesPool;
			} else if (string.Equals(defname, "random_skirt", StringComparison.OrdinalIgnoreCase)) {
				pool = NPCDef.SkirtPool;
			}
			if (pool != null && pool.Length > 0) {
				defname = pool[Globals.dice.Next(pool.Length)];
			}
			ItemDef found = ThingDef.GetByDefname(defname) as ItemDef;
			int model;
			if ((found == null) && (defname.Length > 1) && (defname[0] == '0') &&
					int.TryParse(defname, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out model)) {
				//#793 - Sphere's "ITEMNEWBIE=01f07": a hex graphic, not a defname (Sphere's numbers
				//with a leading 0 are hex) - the itemdef of that graphic
				found = ThingDef.FindItemDef(model) as ItemDef;
			}
			if ((found == null) && defname.StartsWith("random_", StringComparison.OrdinalIgnoreCase)) {
				//#793 - a Sphere pick-list constant, e.g. RANDOM_LIGHT { i_CANDLE, 1, i_TORCH, 1, i_LANTERN, 1 }
				try {
					object picked = LScriptMain.RunSnippet("<equip>", 0, Globals.Instance, "return(" + defname + ")");
					found = (picked as ItemDef) ?? (ThingDef.GetByDefname(Convert.ToString(picked, CultureInfo.InvariantCulture)) as ItemDef);
				} catch (Exception e) {
					Logger.WriteDebug("equip '" + defname + "' is not a pick-list constant: " + e.Message);
				}
			}
			return found;
		}

		/// <summary>Resolves a StartingEquipmentEntry.Color's raw text to a concrete color
		/// value. "match_hair" (the real archive's own pseudo-color, meaning "same color as
		/// the hair/facial-hair item equipped just before this one") has no fixed value at all -
		/// copies lastHairColor instead of ever reaching the generic expression evaluator, which
		/// has nothing to resolve it to and would otherwise throw (the real #<issue> crash:
		/// ConvertTools.ParseAnyNumber("match_hair") inside Thing's own constructor, aborting
		/// NPC creation entirely). Anything else (a plain hex value, or a symbolic range like
		/// "colors_hair") is resolved the exact same way a top-level "Color =" field already
		/// would be - a real LScript snippet, so symbolic defs.scp constants and Sphere
		/// {min,max} ranges both just work, no separate parsing needed.</summary>
		private static int? ResolveStartingEquipmentColor(string rawColor, int? lastHairColor, string npcDefName) {
			if (string.Equals(rawColor, "match_hair", StringComparison.OrdinalIgnoreCase)) {
				return lastHairColor;
			}
			try {
				object resolved = LScriptMain.RunSnippet("<equipcolor>", 0, Globals.Instance, "return(" + rawColor + ")");
				return ConvertTools.ToInt32(resolved);
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteWarning("NPC.On_Create (NPCDef " + npcDefName + ")",
					"equipcolor '" + rawColor + "' could not be resolved: " + e.Message);
				return null;
			}
		}

		private static readonly TagKey swingStaminaTK = TagKey.Acquire("_swingStamina_");

		/// <summary>#860 - this NPC's tag.npc_speed, 0 if it has none.</summary>
		public int SwingStamina {
			get { return Convert.ToInt32(this.GetTag(swingStaminaTK) ?? 0); }
			set { this.SetTag(swingStaminaTK, value); }
		}

		/// <summary>#1201 - an NPC saved asleep wakes after a load: whether a player is near is known only now, and it
		/// falls asleep again at its first think if nobody is.</summary>
		public override void On_AfterLoad() {
			NPCAiTimer.WakeUp(this);
			base.On_AfterLoad();
		}

		public override void On_Create() {
			base.On_Create();

			NPCDef def = (NPCDef) this.Def;
			if ((this.Name != null) && (this.Name.IndexOf("#NAMES_", StringComparison.OrdinalIgnoreCase) >= 0)) {
				this.Name = NameList.Resolve(this.Name); //#1070 - Sphere's NAME=#NAMES_HUMANMALE
			}
			this.AggroMode = def.AggroMode;
			//#700 - only override the live Character default (Realm.None, no RealmPlugin
			//installed) when this NPCDef actually configured a real side via tag.guard -
			//matches Character.Realm's own documented "untouched characters are left
			//completely unaffected" intent instead of installing a RealmPlugin on every NPC.
			if (def.Realm != Realm.None) {
				this.Realm = def.Realm;
			}
			this.ExperienceReward = def.RollExperienceReward();
			//#603 - MoveRate must be seeded BEFORE EnsureInstalled, so the AI timer's very
			//first schedule already reflects it instead of falling back to the tag-unset
			//default (3.0) for one tick and only self-correcting on this setter's own
			//reschedule hook the next time MoveRate happens to change.
			this.MoveRate = def.MoveRate;
			NPCAiTimer.EnsureInstalled(this);

			//#559 - only override the live Character default (15, see Character.ct) when this
			//NPCDef actually configured a range; -1 means unconfigured, leave the default alone
			int str = def.RollStr();
			if (str >= 0) {
				this.Str = (short) str;
			}
			int dex = def.RollDex();
			if (dex >= 0) {
				this.Dex = (short) dex;
			}
			int intel = def.RollInt();
			if (intel >= 0) {
				this.Int = (short) intel;
			}

			//#560 - Fame/Karma can legitimately roll negative, so use the nullable
			//RollFame/RollKarma (null means unconfigured) rather than #559's -1 sentinel
			int? fame = def.RollFame();
			if (fame.HasValue) {
				this.Fame = (short) fame.Value;
			}
			int? karma = def.RollKarma();
			if (karma.HasValue) {
				this.Karma = (short) karma.Value;
			}

			//#561 - only override the live default (100, see NPC.ct) when configured; -1 means
			//unconfigured, same sentinel as #559's Str/Dex/Int (MaxHits/MaxStam/MaxMana can
			//only ever be non-negative in real source, unlike Fame/Karma). Current Hits/Mana/
			//Stam are ALSO raised to match so a freshly-seeded NPC spawns fully healed, not at
			//the old 100-point default against its new, much larger max pool.
			int maxHits = def.RollMaxHits();
			if (maxHits >= 0) {
				this.MaxHits = (short) maxHits;
				this.Hits = this.MaxHits;
			}
			int swingStamina = NPCDef.RollStatRange(def.NpcSpeed);
			if (swingStamina > 0) {
				this.SwingStamina = swingStamina;
			}
			int maxStam = def.RollMaxStam();
			if (maxStam >= 0) {
				this.MaxStam = (short) maxStam;
				this.Stam = this.MaxStam;
			}
			int maxMana = def.RollMaxMana();
			if (maxMana >= 0) {
				this.MaxMana = (short) maxMana;
				this.Mana = this.MaxMana;
			}

			//#562 - starting skill values, any subset of SkillName the NPCDef configured
			Dictionary<SkillName, int[]> skillRanges = def.SkillValueRanges;
			if (skillRanges != null) {
				foreach (SkillName skillName in skillRanges.Keys) {
					int rolled = def.RollSkillValue(skillName);
					if (rolled >= 0) {
						this.SetRealSkillValue((int) skillName, rolled);
					}
				}
			}

			//starting resist values, any subset of the 17 real Resist* properties the NPCDef
			//configured - rolled once here (same as every other range above) and set directly
			//on `this` (not def), via reflection since the property name is only known as a
			//string. Character's own ResistX setters (Character.cs) already store this as a
			//delta against TypeDef.ResistX, so the roll is correct regardless of whatever this
			//NPCDef's own base Resist* scalar happens to be.
			Dictionary<string, int[]> resistRanges = def.ResistRanges;
			if (resistRanges != null) {
				foreach (KeyValuePair<string, int[]> entry in resistRanges) {
					PropertyInfo resistProperty = typeof(Character).GetProperty(entry.Key,
						BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
					if (resistProperty == null) {
						Logger.WriteWarning("NPC.On_Create (NPCDef " + def.PrettyDefname + ")",
							"'" + entry.Key + "' is not a known Character resist property.");
						continue;
					}
					int rolled = NPCDef.RollStatRange(entry.Value);
					resistProperty.SetValue(this, rolled, null);
				}
			}

			//#563 - starting equipment (real archive's ITEMNEWBIE grants). ThingDef.Create(this)
			//already equips the item automatically if it's equippable (a bow, a spellbook) or
			//drops it into the backpack otherwise (ammo like arrows) - no separate equip call
			//needed here.
			List<NPCDef.StartingEquipmentEntry> equipment = def.StartingEquipment;
			bool hasMonsterCreate = false; //#791 - an "Equip = i_monster_create" entry, see MonsterLoot
			if (equipment != null) {
				//real-user-reported: creating c_f_HEALER/c_m_HEALER logged "starting equipment
				//item 'random_female_hair' is not a known ItemDef" and, worse, crashed entirely
				//on a later "Color = match_hair" line (ConvertTools.ParseAnyNumber throwing
				//uncaught inside Thing's own constructor) - neither is a real ItemDef/color
				//value, both are native Sphere "roll me something"/"color THIS like THAT"
				//directives. lastHairColor tracks whichever hair/facial-hair item was most
				//recently rolled+colored in this same loop, for a later "match_hair" line to copy.
				int? lastHairColor = null;
				//#728 follow-up - real-user-reported: a guard with BOTH a weapon and a spellbook
				//in its StartingEquipment (Equip= i_spellbook then later Equip= i_axe_exec) spawned
				//with the spellbook lying on the ground at its feet instead of in its backpack.
				//Root cause: i_spellbook and most one-handed weapons both declare the same real
				//Layer (Hand1) - InternalItemEnter's normal, correct-for-players equip-eviction
				//logic (whatever already occupies a hand gets knocked loose and dropped on the
				//ground, same as a player swapping weapons) fires here too, but a freshly-spawned
				//NPC never asked for that swap - its whole StartingEquipment list is meant to all
				//be carried at once. Track every item created in this loop and sweep for any that
				//ended up IsOnGround (evicted by a LATER entry in this same loop, not its own) once
				//the whole list has been applied, moving them into the backpack instead - keeps the
				//real per-player ground-drop behavior on live equip actions completely untouched.
				List<Item> createdStartingEquipment = new List<Item>();
				foreach (NPCDef.StartingEquipmentEntry entry in equipment) {
					string entryDefname = entry.PickDefname(Globals.dice); //#775 - rolls a "{a 1 b 1}" random pick
					if (MonsterLoot.IsMonsterCreateMarker(entryDefname)) {
						//#791 - Erebor's i_monster_create is a one-shot script item, not equipment: its
						//loot logic runs below (MonsterLoot.ApplyCreatureStart), the item isn't created
						hasMonsterCreate = true;
						continue;
					}
					bool isHairKeyword = IsHairRandomizerKeyword(entryDefname);
					ItemDef itemDef = ResolveStartingEquipmentItemDef(entryDefname, isHairKeyword);
					if (itemDef == null) {
						Logger.WriteWarning("NPC.On_Create (NPCDef " + def.PrettyDefname + ")",
							"starting equipment item '" + entryDefname + "' is not a known ItemDef.");
						continue;
					}
					Thing created = itemDef.Create(this);
					if (entry.AmountRange != null) {
						AbstractItem item = created as AbstractItem;
						if (item != null) {
							int amount = NPCDef.RollStatRange(entry.AmountRange);
							if (amount > 0) {
								item.Amount = amount;
							}
						}
					}
					if (entry.Color != null) {
						int? resolvedColor = ResolveStartingEquipmentColor(entry.Color, lastHairColor, def.PrettyDefname);
						if (resolvedColor != null) {
							created.Color = resolvedColor.Value;
							if (isHairKeyword) {
								lastHairColor = resolvedColor.Value;
							}
						}
					}
					if (entry.Name != null) {
						created.Name = entry.Name;
					}
					//real-user-reported: a monster's starting-equipment item (a mage's spellbook,
					//a ranger's bow) was lootable off the corpse and could be farmed. The real
					//archive's own field name for this whole mechanism is literally ITEMNEWBIE=
					//("newbie item") - confirms every StartingEquipment grant across the whole
					//archive is meant to carry attr_newbie, not just these two examples. Item.
					//CanFallToCorpse already treats attr_newbie as "never falls to the corpse" -
					//this makes the grant disappear with the monster instead of becoming loot.
					Item createdItem = created as Item;
					if (createdItem != null) {
						createdItem.SetAttribute(ItemAttributes.attr_newbie, true);
						createdStartingEquipment.Add(createdItem);
					}
				}

				foreach (Item createdItem in createdStartingEquipment) {
					if (createdItem.IsOnGround) {
						createdItem.Cont = this.Backpack;
					}
				}
			}

			//#791 - the monster's loot (Erebor's ON=@NPCRestock), into the backpack so it falls to
			//the corpse - then, for i_monster_create monsters, the gold amount, rare extras and
			//(#794) the elite roll (Erebor's creaturestart), in the same order Sphere ran them -
			//creaturestart on a 0 s timer, as in Erebor, so it sees a spawn gem's tag
			def.CreateLoot(this.Backpack);
			if (hasMonsterCreate) {
				MonsterLoot.ScheduleCreatureStart(this);
			}

			//#4 of the 2026-10-26 monster-testing batch - stub abilities standing in for legacy
			//TEVENTS= names that didn't resolve to a real TriggerGroup (see
			//TryResolveUnknownTriggerGroupName above). 1 real point is enough for the ability to
			//genuinely "exist" on this NPC (shows up in .info's Abilities view, #651) - these are
			//all zero-effect PassiveAbilityDefs, so the exact point count has no gameplay effect.
			List<AbilityDef> startingAbilities = def.StartingAbilities;
			if (startingAbilities != null) {
				foreach (AbilityDef abilityDef in startingAbilities) {
					this.SetRealAbilityPoints(abilityDef, 1);
				}
			}

			IMovementSettings template = def.DefaultMovementSettings;
			if (template != null) {
				this.MovementSettings = (IMovementSettings) DeepCopyFactory.GetCopy(template);
			}

			//#700 - spawns the real mount creature (e.g. c_horse_brown_dk) and rides it, so a
			//guard/NPC authored as "already mounted" (tag.mount in the real archive) actually
			//renders that way - setting a raw MountItem number directly on THIS NPC's own def
			//(the original, wrong first attempt) has zero visual effect, since
			//DrawObjectOutPacket only ever reads a RIDDEN character's MountItem/Color
			//(AbstractCharacter.Mount), never a non-ridden character's own MountItem.
			if (def.MountCharDef != null) {
				NPC mount = (NPC) def.MountCharDef.Create(this.X, this.Y, this.Z, this.M);
				if (def.MountColor != -1) {
					mount.Color = def.MountColor;
				}
				this.Mount = mount;
				this.AutoSpawnedMount = mount;
			}
		}

		private static readonly TagKey autoSpawnedMountTK = TagKey.Acquire("_npcAutoSpawnedMount_");

		/// <summary>#705 - the mount creature spawned for THIS NPC via NPCDef.MountCharDef
		/// (#700/#703's tag.mount), captured separately from AbstractCharacter.Mount itself -
		/// Character.CauseDeath already calls Dismount() before On_Death ever runs, so by the
		/// time a dying NPC could check this.Mount here the rider/mount relationship is already
		/// severed. Used by On_Death below to delete the mount along with its rider, instead of
		/// leaving it behind as an ordinary, free, tamable/lootable creature - killing a mounted
		/// guard should not be a way to farm a rare mount species.</summary>
		private AbstractCharacter AutoSpawnedMount {
			get {
				return this.GetTag(autoSpawnedMountTK) as AbstractCharacter;
			}
			set {
				this.SetTag(autoSpawnedMountTK, value);
			}
		}

		/// <summary>Grants monster-kill experience to a player killer - see ExperienceReward
		/// (#260, rolled once at creation, not re-rolled here) and LevelingSystem.AddExperience
		/// (#127). NPCs themselves never level (Erebor's own level-up logic only ever runs "if
		/// (isplayer)") so there's nothing else to do here for the NPC side. Also deletes this
		/// NPC's auto-spawned mount (#705), if any - see AutoSpawnedMount's own doc comment.</summary>
		public override void On_Death(Character killedBy) {
			base.On_Death(killedBy);

			AbstractCharacter autoMount = this.AutoSpawnedMount;
			if ((autoMount != null) && !autoMount.IsDeleted) {
				autoMount.Delete();
			}

			PlayerRecord.MonsterDied(this, killedBy); //#1011
			MonsterSpecialAttacks.OnMonsterDeath(this, killedBy); //#1022
			Reputation.OnDeath(this, killedBy); //#1243 - a resident's killer is remembered in the city
			Q008.OnKillCredit(killedBy as Character, this); //#1274 - Ancam's monsters: Erebor's @Death tag on act, whatever their experience
			Q041.OnKillCredit(killedBy as Character, this); //#1278 - the smuggler's elf: Erebor's @death on act
			WorldEvents.OnDeath(this, killedBy); //#1284 - a raider, bandit or merchant of a world event
			Errands.OnThingDeath(this, killedBy); //#1293 - an errand's rat, thief or other thing
			Deeds.OnKill(this, killedBy); //#1296 - the slayer's deed
			LevelingSystem.ShareMonsterExperience(this, killedBy);
		}

		/// <summary>Decides whether this NPC should pick `candidate` as an aggro target - see AggroMode. Virtual so NPC subclasses/scripts can layer on additional policies beyond the three builtins.</summary>
		public virtual bool ShouldAggroOn(Character candidate) {
			if ((candidate == null) || !candidate.IsPlayer || !candidate.IsAliveAndValid || (candidate == this)) {
				return false;
			}
			if (candidate.Flag_Invulnerable) {
				return false; //#267 - nobody autonomously targets an invulnerable character, even one otherwise immune to the resulting damage
			}
			if (Jail.IsJailed(candidate)) {
				return false; //#1107 - Sphere's NPCs leave a jailed character alone
			}
			if (MonsterSpecialAttacks.LeavesPlayersAlone(this)) {
				return false; //#1022
			}
			if (this.ControlMaster != null) {
				return false; //tamed pets don't pick fights on their own - no "guard me"/"kill" command system yet (follow-up)
			}
			if (candidate == this.FollowTarget) {
				return false; //#1174 - nor a wild monster a GM commands ("come"): never its commander. aee9d81 meant to add this
				//and didn't, so FindTarget picked the commander every think and IsValidTarget dropped it on the next
			}
			if (MysticHypnosis.MonsterGoesFor(this, candidate)) {
				return true; //#1070 - e_monster's @npcseenewplayer
			}
			switch (this.AggroMode) {
				case AggroMode.Hostile:
					return true;
				case AggroMode.GuardsRealm:
					return GuardSpeech.AttacksOnSight(this, candidate); //#1022
				default:
					return false;
			}
		}

		/// <summary>Classic UO "rebel" rule: a creature never attacks its own owner/commander
		/// (see ShouldAggroOn/NPCAiTimer.IsValidTarget) UNLESS that owner attacks it first, at
		/// which point ownership drops immediately so the creature is free to defend itself.
		/// No separate "attack the ex-owner" logic is needed - once ControlMaster/FollowTarget
		/// are cleared, the normal autonomous-aggro path (ShouldAggroOn) is free to pick the
		/// former owner on its own, same as any other AggroMode.Hostile target.</summary>
		public override void On_HostileAction(Character enemy) {
			base.On_HostileAction(enemy);
			if ((enemy != null) && ((enemy == this.ControlMaster) || (enemy == this.FollowTarget))) {
				Logger.WriteDebug("NPC.On_HostileAction: " + this.Name + "#" + this.Uid + " rebels against its own owner/commander " + enemy.Name + " - dropping ControlMaster/FollowTarget");
				this.ControlMaster = null;
				this.FollowTarget = null;
				this.IsGuarding = false;
				this.ShouldFollowOwner = true; //reset to the default, matching PetCommands.cs's "release" verb cleanup
			}
			NPCAiTimer.DefendAgainst(this, enemy); //#768 - fight back (a rebel now fights its ex-owner too, as in Sphere)
		}

		//#778 - someone shoved past this NPC (#767): a strangler vine grabs them
		public override void On_PersonalSpace(Character pusher) {
			base.On_PersonalSpace(pusher);
			MonsterSpecialAttacks.TryEntangle(this, pusher);
		}

		//#807 - an item dropped on this NPC: e_item_refuser turns it down (the giver keeps it);
		//#808 - otherwise Sphere's NPC_OnItemGive (pets take/eat it, everyone else refuses)
		public override TriggerResult On_PutItemOn(ItemOnCharArgs args) {
			if (MonsterSpecialAttacks.TryRefuseItem(this, args.ManipulatedItem)) {
				return TriggerResult.Cancel;
			}
			if (NpcItemGive.OnItemGive(this, args.PuttingChar as Character, args.ManipulatedItem) == TriggerResult.Cancel) {
				return TriggerResult.Cancel;
			}
			return base.On_PutItemOn(args);
		}

		public override short MaxHits {
			get {
				return this.maxHitpoints;
			}
			set {
				if (value != this.maxHitpoints) {
					CharSyncQueue.AboutToChangeHitpoints(this);
					this.maxHitpoints = value;

					//check the hitpoints regeneration
					RegenerationPlugin.TryInstallPlugin(this, this.Hits, this.maxHitpoints, this.HitsRegenSpeed);
				}
			}
		}

		public override short MaxMana {
			get {
				return this.maxMana;
			}
			set {
				if (value != this.maxMana) {
					CharSyncQueue.AboutToChangeMana(this);
					this.maxMana = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Mana, this.maxMana, this.ManaRegenSpeed);
				}
			}
		}

		public override short MaxStam {
			get {
				return this.maxStamina;
			}
			set {
				if (value != this.maxStamina) {
					CharSyncQueue.AboutToChangeStamina(this);
					this.maxStamina = value;

					//regeneration...
					RegenerationPlugin.TryInstallPlugin(this, this.Stam, this.maxStamina, this.StamRegenSpeed);
				}
			}
		}

		public override IMovementSettings MovementSettings {
			get
			{
				if (this.movementSettings == null) {
					return base.MovementSettings;
				}
				return this.movementSettings;
			}
			set {
				this.movementSettings = value;
			}
		}
	}
}