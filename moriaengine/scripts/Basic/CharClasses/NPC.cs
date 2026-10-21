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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
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
		private static int RollStatRange(int[] range) {
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
			if (param == "combatspell") {
				SpellDef spell = SpellDef.GetByDefname(args);
				if (spell == null) {
					Logger.WriteWarning(filename, line, "combatspell '" + args + "' is not a known SpellDef.");
				}
				this.CombatSpell = spell;
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

		public override void On_Create() {
			base.On_Create();
			NPCAiTimer.EnsureInstalled(this);

			NPCDef def = (NPCDef) this.Def;
			this.AggroMode = def.AggroMode;
			this.ExperienceReward = def.RollExperienceReward();

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

			IMovementSettings template = def.DefaultMovementSettings;
			if (template != null) {
				this.MovementSettings = (IMovementSettings) DeepCopyFactory.GetCopy(template);
			}
		}

		/// <summary>Grants monster-kill experience to a player killer - see ExperienceReward
		/// (#260, rolled once at creation, not re-rolled here) and LevelingSystem.AddExperience
		/// (#127). NPCs themselves never level (Erebor's own level-up logic only ever runs "if
		/// (isplayer)") so there's nothing else to do here for the NPC side.</summary>
		public override void On_Death(Character killedBy) {
			base.On_Death(killedBy);
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