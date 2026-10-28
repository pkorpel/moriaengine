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
		private static readonly TagKey aggroModeTK = TagKey.Acquire("_aggroMode_");

		/// <summary>#265: a save/reload cycle can retype this tag from the real AggroMode enum
		/// to a plain boxed int (same root-cause class as #168's TamingDifficulty double->int
		/// retyping) - #168's own fix (AbstractDef.isLoadingFromScript, PR #171) prevents NEW
		/// corruption from being written going forward, but can't retroactively fix an ALREADY-
		/// corrupted snapshot already sitting in a long-lived server's saves/ history, which
		/// then keeps getting faithfully reloaded (save-recovery runs AFTER .def loading, so it
		/// overwrites the correct freshly-parsed value). Coercing a numerically-valid int back
		/// into the enum here fixes this regardless of what's sitting in old save data, instead
		/// of requiring every affected server operator to go hunt down and delete a stale
		/// snapshot file.</summary>
		public AggroMode AggroMode {
			get {
				object tag = this.GetTag(aggroModeTK);
				if (tag is AggroMode mode) {
					return mode;
				}
				if ((tag is int intTag) && Enum.IsDefined(typeof(AggroMode), intTag)) {
					return (AggroMode) intTag;
				}
				return AggroMode.Calm;
			}
			set {
				this.SetTag(aggroModeTK, value);
			}
		}

		private static readonly TagKey realmTK = TagKey.Acquire("_npcDefRealm_");

		/// <summary>#700 - species-level default Realm for AggroMode.GuardsRealm NPCs (e.g. a
		/// real archive guard's tag.guard=1/2), seeded onto each new instance's own
		/// Character.Realm (RealmPlugin-backed, #410) in On_Create below. Same #265 boxed-int
		/// coercion precedent as AggroMode above, for the same reason (a save/reload cycle can
		/// retype an enum tag to a plain int).</summary>
		public Realm Realm {
			get {
				object tag = this.GetTag(realmTK);
				if (tag is Realm realm) {
					return realm;
				}
				if ((tag is int intTag) && Enum.IsDefined(typeof(Realm), intTag)) {
					return (Realm) intTag;
				}
				return Realm.None;
			}
			set {
				this.SetTag(realmTK, value);
			}
		}

		private static readonly TagKey tamingDifficultyTK = TagKey.Acquire("_tamingDifficulty_");

		/// <summary>Minimum effective AnimalTaming skill needed to tame this NPC type (see TamingSkillDef), on the classic 0-1000 permille scale matching CheckSuccess's difficulty parameter. Negative (the default) means not tameable at all.
		/// Also coerces a plain int tag (#265 - same vulnerability as AggroMode above, for the
		/// exact property #168 originally found this bug on) instead of silently discarding it.</summary>
		public double TamingDifficulty {
			get {
				object tag = this.GetTag(tamingDifficultyTK);
				if (tag is double difficulty) {
					return difficulty;
				}
				if (tag is int intTag) {
					return intTag;
				}
				return -1;
			}
			set {
				this.SetTag(tamingDifficultyTK, value);
			}
		}

		private static readonly TagKey moveRateTK = TagKey.Acquire("_moveRate_");

		/// <summary>Per-species movement speed (#590) - the real archive's `MOVERATE=` field.
		/// `3.0` is the real baseline/default (matches this engine's pre-existing fixed
		/// one-tile-per-AI-tick behavior exactly) - higher moves faster, lower moves slower.
		/// See `NPCAiTimer`'s move-budget accumulator for how this actually drives speed.</summary>
		public double MoveRate {
			get {
				object tag = this.GetTag(moveRateTK);
				if (tag is double rate) {
					return rate;
				}
				if (tag is int intTag) {
					return intTag;
				}
				return 3.0;
			}
			set {
				this.SetTag(moveRateTK, value);
			}
		}

		private static readonly TagKey spellCastRangeTK = TagKey.Acquire("_npcSpellCastRange_");

		/// <summary>#716 - per-monster override for how far this NPC holds its distance while
		/// casting (NPCAiTimer.Fight/TryCastSpell's own default is a flat constant, the same
		/// GM-tunable-in-spirit shape as NPCDef.WeaponRange's own bow/crossbow default). Negative
		/// (the default) means "use the engine's own default spell-cast range" - unaffected,
		/// same as before this field existed.</summary>
		public double SpellCastRange {
			get {
				object tag = this.GetTag(spellCastRangeTK);
				if (tag is double range) {
					return range;
				}
				if (tag is int intTag) {
					return intTag;
				}
				return -1;
			}
			set {
				this.SetTag(spellCastRangeTK, value);
			}
		}

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
		public int[] SelfresDelay {
			get { return this.GetTag(selfresDelayTK) as int[]; }
			set { this.SetTag(selfresDelayTK, value); }
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
			"ResistStabbing", "ResistBlunt", "ResistArchery", "ResistBleed", "ResistSummon",
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

		private static readonly TagKey instanceStartingAbilitiesTK = TagKey.Acquire("_npcStartingAbilities_");

		/// <summary>Stub abilities (#4 of the 2026-10-26 monster-testing batch) granted once at
		/// creation by NPC.On_Create, one per legacy TEVENTS= name that didn't resolve to a
		/// real TriggerGroup - see TryResolveUnknownTriggerGroupName below. Each is a real,
		/// zero-effect PassiveAbilityDef (same shape as the pre-existing unimplementedAbilities.
		/// scp stubs) so the monster's real intent is now inspectable via .info's Abilities view
		/// (#651) instead of a silently-dropped load warning - ready for a genuine
		/// implementation later without touching content again.</summary>
		public List<AbilityDef> StartingAbilities {
			get { return this.GetTag(instanceStartingAbilitiesTK) as List<AbilityDef>; }
			set { this.SetTag(instanceStartingAbilitiesTK, value); }
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
			if (param == "tamingdifficulty") {
				this.TamingDifficulty = ConvertTools.ParseDouble(args);
				return;
			}
			if (param == "moverate") {
				this.MoveRate = ConvertTools.ParseDouble(args);
				return;
			}
			if (param == "spellcastrange") {
				this.SpellCastRange = ConvertTools.ParseDouble(args);
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
			if (param == "aggromode") {
				AggroMode mode;
				if (Enum.TryParse(args, true, out mode)) {
					this.AggroMode = mode;
				} else {
					Logger.WriteWarning(filename, line, "aggromode '" + args + "' is not a known AggroMode value.");
				}
				return;
			}
			if (param == "realm") {
				Realm realm;
				if (Enum.TryParse(args, true, out realm)) {
					this.Realm = realm;
				} else {
					Logger.WriteWarning(filename, line, "realm '" + args + "' is not a known Realm value.");
				}
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
			if (param == "selfresdelay") {
				this.SelfresDelay = ParseStatRange(filename, line, "selfresdelay", args);
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
			if (param == "equip") {
				//#563 - "Equip = defname" or "Equip = defname, amount" or "Equip = defname, min max"
				string[] parts = args.Split(',');
				string defname = parts[0].Trim();
				int[] amountRange = null;
				if (parts.Length > 1) {
					amountRange = ParseStatRange(filename, line, "equip amount", parts[1].Trim());
				}
				if (this.StartingEquipment == null) {
					this.StartingEquipment = new List<StartingEquipmentEntry>();
				}
				this.StartingEquipment.Add(new StartingEquipmentEntry { Defname = defname, AmountRange = amountRange });
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
			return ThingDef.GetByDefname(defname) as ItemDef;
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

		public override void On_Create() {
			base.On_Create();

			NPCDef def = (NPCDef) this.Def;
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
					bool isHairKeyword = IsHairRandomizerKeyword(entry.Defname);
					ItemDef itemDef = ResolveStartingEquipmentItemDef(entry.Defname, isHairKeyword);
					if (itemDef == null) {
						Logger.WriteWarning("NPC.On_Create (NPCDef " + def.PrettyDefname + ")",
							"starting equipment item '" + entry.Defname + "' is not a known ItemDef.");
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

			Player player = killedBy as Player;
			if (player != null) {
				int reward = this.ExperienceReward;
				if (reward != 0) {
					LevelingSystem.AddExperience(player, reward);
				}
			}
		}

		/// <summary>Decides whether this NPC should pick `candidate` as an aggro target - see AggroMode. Virtual so NPC subclasses/scripts can layer on additional policies beyond the three builtins.</summary>
		public virtual bool ShouldAggroOn(Character candidate) {
			if ((candidate == null) || !candidate.IsPlayer || !candidate.IsAliveAndValid || (candidate == this)) {
				return false;
			}
			if (candidate.Flag_Invulnerable) {
				return false; //#267 - nobody autonomously targets an invulnerable character, even one otherwise immune to the resulting damage
			}
			if (this.ControlMaster != null) {
				return false; //tamed pets don't pick fights on their own - no "guard me"/"kill" command system yet (follow-up)
			}
			switch (this.AggroMode) {
				case AggroMode.Hostile:
					return true;
				case AggroMode.GuardsRealm:
					return NPCAiTimer.IsOppositeRealm(this.Realm, candidate.Realm);
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
					
					//meditation finish
					if (this.Mana >= this.MaxMana) {
						this.DeletePlugin(MeditationPlugin.meditationPluginKey);
					}
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