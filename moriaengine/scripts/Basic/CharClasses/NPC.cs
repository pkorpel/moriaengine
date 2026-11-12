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
using System.Linq;
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
		//#1405 - the add-up lists are typed fields (NPC.ct); the properties below read them as they did the tags, built once
		//for the array they were built from, so a GM's change to a field shows
		private const int SlotSpells = 0, SlotSummons = 1, SlotBattleSummons = 2, SlotLoot = 3, SlotResources = 4, SlotFoodTypes = 5,
			SlotSkills = 6, SlotResists = 7, SlotCan = 8, SlotEquip = 9, SlotCount = 10;
		private readonly object[] listSources = new object[SlotCount];
		private readonly object[] listViews = new object[SlotCount];

		private T Derived<T>(int slot, object source, Func<T> build) where T : class {
			if (!ReferenceEquals(source, this.listSources[slot])) {
				this.listViews[slot] = (source == null) ? null : build();
				this.listSources[slot] = source;
			}
			return (T) this.listViews[slot];
		}

		private Dictionary<string, List<string>> pendingLists; //a section's lines gathered while it loads, by field

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
				SpellDef[] source = this.SpellList;
				return this.Derived(SlotSpells, source, () => new List<SpellDef>(source));
			}
			set {
				this.SpellList = value?.ToArray();
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

		/// <summary>#1022 - e_npc_summoner's list, tag.npc_summoner_char[N].</summary>
		public List<NPCDef> BattleSummons {
			get {
				NPCDef[] source = this.BattleSummonList;
				return this.Derived(SlotBattleSummons, source, () => new List<NPCDef>(source));
			}
			set { this.BattleSummonList = value?.ToArray(); }
		}

		/// <summary>e_npc_summoning's own per-species reinforcement list (real archive fields
		/// tag.npc_summoning_char[i]/_count) - same list-of-NPCDef shape as the already-shipped
		/// Spells (#564). When non-empty, SummonerAbilityDef's consumer spawns ONE instance of
		/// EVERY entry here per proc (matching the archive's own loop-over-all-indices), not a
		/// random pick.</summary>
		public List<NPCDef> SummonedCreatures {
			get {
				NPCDef[] source = this.SummonList;
				return this.Derived(SlotSummons, source, () => new List<NPCDef>(source));
			}
			set { this.SummonList = value?.ToArray(); }
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

		public int RollMaxHits() { return RollStatRange(this.MaxHitsRange); }
		public int RollMaxStam() { return RollStatRange(this.MaxStamRange); }
		public int RollMaxMana() { return RollStatRange(this.MaxManaRange); }

		/// <summary>Starting skill-value {min, max} ranges for a fresh NPC of this type (#562),
		/// keyed by SkillName, rolled once at creation by NPC.On_Create via Character.
		/// SetRealSkillValue. A generic dictionary rather than one property per skill (unlike
		/// StrRange/FameRange/etc) since real chardefs set any of ~25-29 different skills, not a
		/// small fixed set - the .def field name for a skill line is the literal SkillName enum
		/// member name (e.g. "Wrestling = 700 800"), matched case-insensitively in
		/// LoadScriptLine's fallback below.</summary>
		public Dictionary<SkillName, int[]> SkillValueRanges {
			get {
				string[] source = this.SkillRangeList;
				return this.Derived(SlotSkills, source, () => {
					Dictionary<SkillName, int[]> ranges = new Dictionary<SkillName, int[]>();
					foreach (string entry in source) {
						try {
							string[] t = entry.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
							ranges[(SkillName) Enum.Parse(typeof(SkillName), t[0], true)] = new int[] { ConvertTools.ParseInt32(t[1]), ConvertTools.ParseInt32(t[2]) };
						} catch (Exception e) {
							this.SkipBadElement("skillRangeList", entry, e);
						}
					}
					return ranges;
				});
			}
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

		/// <summary>Starting Resist* {min, max} ranges for a fresh NPC of this type, keyed by
		/// the real Character property name (e.g. "ResistPoison") - same generic-dictionary
		/// shape as SkillValueRanges above, since there are 17 resist fields, not a small fixed
		/// set worth a dedicated property each. Rolled once at creation by NPC.On_Create, which
		/// sets the rolled value directly on the live Character instance (via reflection, since
		/// the property name is only known as a string) - Character's own ResistX setters
		/// already store it as a delta against this NPCDef's own (now otherwise-unused) base
		/// Resist* scalar, exactly like the Str/Dex/Int precedent (#559) above.</summary>
		public Dictionary<string, int[]> ResistRanges {
			get {
				string[] source = this.ResistRangeList;
				return this.Derived(SlotResists, source, () => {
					Dictionary<string, int[]> ranges = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase);
					foreach (string entry in source) {
						try {
							string[] t = entry.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
							ranges[t[0]] = new int[] { ConvertTools.ParseInt32(t[1]), ConvertTools.ParseInt32(t[2]) };
						} catch (Exception e) {
							this.SkipBadElement("resistRangeList", entry, e);
						}
					}
					return ranges;
				});
			}
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


		/// <summary>Starting-equipment list for a fresh NPC of this type (#563) - each entry
		/// created and handed to ThingDef.Create(this) once at creation by NPC.On_Create, which
		/// equips it automatically if it's equippable (a bow, a spellbook) or drops it into the
		/// backpack otherwise (ammo like arrows). Multiple entries are normal - a real chardef
		/// can have several `Equip =` lines (e.g. a ranger's bow THEN its arrows).</summary>
		public List<StartingEquipmentEntry> StartingEquipment {
			get {
				string[] lines = this.EquipList, colors = this.EquipColorList, names = this.EquipNameList;
				object source = (lines == null) ? null : new object[] { lines, colors, names };
				if ((source != null) && (this.listSources[SlotEquip] is object[] was) && ReferenceEquals(was[0], lines)
						&& ReferenceEquals(was[1], colors) && ReferenceEquals(was[2], names)) {
					source = was;
				}
				return this.Derived(SlotEquip, source, () => {
					List<StartingEquipmentEntry> entries = new List<StartingEquipmentEntry>();
					for (int i = 0; i < lines.Length; i++) {
						StartingEquipmentEntry entry = ParseEquipEntry("NPCDef " + this.PrettyDefname, 0, lines[i]);
						if (entry != null) {
							entry.Color = ((colors != null) && (i < colors.Length) && !string.IsNullOrEmpty(colors[i])) ? colors[i] : null;
							entry.Name = ((names != null) && (i < names.Length) && !string.IsNullOrEmpty(names[i])) ? names[i] : null;
							entries.Add(entry);
						}
					}
					return entries;
				});
			}
		}

		/// <summary>#791 - the monster's loot, Erebor's ON=@NPCRestock "ITEM=" lines ("Loot = ..." in
		/// .def content, see the converter): each "item[,amount]" - an itemdef, a RANDOM_* pick-list
		/// constant or a loot_*/tm_* template - is put into the backpack when the NPC is created
		/// (NPC.On_Create), so it falls to the corpse on death. "Loot = i_gold" is the gold marker
		/// whose amount MonsterLoot sets from the monster's experience.</summary>
		public List<string> Loot {
			get {
				string[] source = this.LootList;
				return this.Derived(SlotLoot, source, () => new List<string>(source));
			}
			set {
				this.LootList = value?.ToArray();
			}
		}

		private LScriptHolder[] lootRunners; //compiled "ITEM = <loot line>" scripts, built on first use for lootRunnersOf
		private List<string> lootRunnersOf;

		/// <summary>#791 - creates this NPCDef's loot in `container` (the new NPC's backpack), each
		/// line through the template ITEM op - so random lists, nested templates and "{0 1}"-style
		/// amounts behave exactly as ITEM= does in templates. A broken line is logged and skipped.</summary>
		public void CreateLoot(Thing container) {
			List<string> loot = this.Loot;
			if (loot == null) {
				return;
			}
			if ((this.lootRunners == null) || !ReferenceEquals(this.lootRunnersOf, loot)) {
				this.lootRunnersOf = loot;
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

		public override void Unload() {
			this.startingAbilities = null; //made again from the TEVENTS lines when the scripts are read again
			this.pendingLists = null; //#1405 - the list lines are gathered again too
			base.Unload();
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

		/// <summary>Butcher-yield data for a fresh NPC of this type (#586) - defined here,
		/// consumed nowhere yet. Ready for whenever the corpse-butchering mechanic gets built.</summary>
		public List<ResourceEntry> ButcherResources {
			get {
				string[] source = this.ResourceList;
				return this.Derived(SlotResources, source, () => {
					List<ResourceEntry> entries = new List<ResourceEntry>();
					foreach (string entry in source) {
						try {
							string[] t = entry.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
							entries.Add(new ResourceEntry { Amount = ConvertTools.ParseInt32(t[0]), Defname = t[1] });
						} catch (Exception e) {
							this.SkipBadElement("resourceList", entry, e);
						}
					}
					return entries;
				});
			}
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

		/// <summary>Acceptable food list for a fresh NPC of this type (#593). Null (the
		/// default, most NPCDefs) means this species was never configured with real food
		/// data - PetHungerSystem deliberately never starts tracking hunger for it at all,
		/// same "incomplete content defaults permissive" precedent used throughout this
		/// engine (ExperienceWorth, attr_decay, etc).</summary>
		public List<FoodTypeEntry> FoodTypes {
			get {
				string[] source = this.FoodTypeList;
				return this.Derived(SlotFoodTypes, source, () => {
					List<FoodTypeEntry> entries = new List<FoodTypeEntry>();
					foreach (string entry in source) {
						try {
							string[] t = entry.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
							entries.Add((t.Length == 2)
								? new FoodTypeEntry { SatiationValue = ConvertTools.ParseInt32(t[0]), CategoryOrDefname = t[1] }
								: new FoodTypeEntry { SatiationValue = DefaultFoodSatiationValue, CategoryOrDefname = t[0] });
						} catch (Exception e) {
							this.SkipBadElement("foodTypeList", entry, e);
						}
					}
					return entries;
				});
			}
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

		//#1377 - ranges as typed Defs: fields in NPC.ct; the dragon, boss, Halda Molle and HCN abilities read their tuning by the
		//field's name (DragonEvents' Tuned). Every public int[] property counts as a range here and as a script keyword below, so an
		//int[] property that isn't a range field doesn't belong on NPCDef.
		private static readonly Dictionary<string, System.Reflection.PropertyInfo> rangeProperties = MakeRangeProperties();

		private static Dictionary<string, System.Reflection.PropertyInfo> MakeRangeProperties() {
			var byName = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.OrdinalIgnoreCase);
			foreach (System.Reflection.PropertyInfo property in typeof(NPCDef).GetProperties()) {
				if (property.PropertyType == typeof(int[])) {
					byName[property.Name] = property;
				}
			}
			return byName;
		}

		internal int[] AbilityTuning(string fieldName) {
			return rangeProperties.TryGetValue(fieldName, out System.Reflection.PropertyInfo property) ? (int[]) property.GetValue(this) : null;
		}

		//#1377 - script keywords that name a range field differently
		private static readonly Dictionary<string, string> rangeKeywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			{ "experience", "experienceWorth" },
			{ "str", "strRange" },
			{ "dex", "dexRange" },
			{ "int", "intRange" },
			{ "fame", "fameRange" },
			{ "karma", "karmaRange" },
			{ "maxhits", "maxHitsRange" },
			{ "maxstam", "maxStamRange" },
			{ "maxmana", "maxManaRange" },
		};

		//#1377 - a range is written "min max" or a single value (both ends) in the scripts; FieldValue reads an array as a comma list
		private static string RangeText(string filename, int line, string param, string args) {
			string[] parts = args.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 1) {
				return parts[0] + "," + parts[0];
			}
			if (parts.Length == 2) {
				return parts[0] + "," + parts[1];
			}
			Logger.WriteWarning(filename, line, param + " '" + args + "' must be either a single value or 'min max'.");
			return null;
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

		/// <summary>The movement capability template new NPCs of this type get on creation (see NPC.On_Create) - each instance gets its own deep copy, not this shared object, so runtime effects can mutate one NPC's capability without affecting every other NPC of the same type.</summary>
		public IMovementSettings DefaultMovementSettings {
			get {
				string source = this.Can;
				return this.Derived<IMovementSettings>(SlotCan, source, () => ParseMovementCapability(source));
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
				base.LoadScriptLine(filename, line, "can", Quoted(args.Trim()));
				return;
			}
			if (param == "mountnpc") { //#1379 - the keyword of the mountCharDef field
				base.LoadScriptLine(filename, line, "mountchardef", args);
				return;
			}
			if (param == "addspell") {
				SpellDef spell = SpellDef.GetByDefname(args);
				if (spell == null) {
					Logger.WriteWarning(filename, line, "addspell '" + args + "' is not a known SpellDef.");
					return;
				}
				this.AddListLine(filename, line, "spelllist", spell.PrettyDefname);
				return;
			}
			if (rangeKeywords.TryGetValue(param, out string rangeField)) {
				string text = RangeText(filename, line, param, args);
				if (text != null) {
					base.LoadScriptLine(filename, line, rangeField.ToLowerInvariant(), text);
				}
				return;
			}
			if (rangeProperties.ContainsKey(param)) {
				string text = RangeText(filename, line, param, args);
				if (text != null) {
					base.LoadScriptLine(filename, line, param, text);
				}
				return;
			}
			if (param == "addbattlesummon") {
				NPCDef battleSummonDef = ThingDef.GetByDefname(args) as NPCDef;
				if (battleSummonDef == null) {
					Logger.WriteWarning(filename, line, "addbattlesummon '" + args + "' is not a known NPCDef.");
					return;
				}
				this.AddListLine(filename, line, "battlesummonlist", battleSummonDef.PrettyDefname);
				return;
			}
			if (param == "addsummon") {
				NPCDef summonDef = ThingDef.GetByDefname(args) as NPCDef;
				if (summonDef == null) {
					Logger.WriteWarning(filename, line, "addsummon '" + args + "' is not a known NPCDef.");
					return;
				}
				this.AddListLine(filename, line, "summonlist", summonDef.PrettyDefname);
				return;
			}
			if (param == "loot") {
				//#791 - "Loot = item[,amount]", one line per Erebor @NPCRestock ITEM= line
				if (ListTextFits(filename, line, args)) {
					this.AddListLine(filename, line, "lootlist", Quoted(args.Trim()));
				}
				return;
			}
			if (param == "equip") {
				//#563 - "Equip = defname" or "Equip = defname, amount" or "Equip = defname, min max"
				//#775 - defname can also be a Sphere weighted random pick, "{i_bardiche 1 i_halberd 1}"
				if (!ListTextFits(filename, line, args) || (ParseEquipEntry(filename, line, args) == null)) {
					return;
				}
				this.Pending("equiplist").Add(Quoted(args.Trim()));
				this.Pending("equipcolor").Add("\"\"");
				this.Pending("equipname").Add("\"\"");
				this.LoadEquipFields(filename, line);
				return;
			}
			if ((param == "equipcolor") || (param == "equipname")) {
				//the real archive's "COLOR=..." / #793 "NAME=..." line right after an "ITEMNEWBIE=" line colors or names THAT
				//item, not the character - see StartingEquipmentEntry.Color; always the "Equip =" entry just added.
				//#1405 - a whole value in Sphere's <...> is that value (EquipColor = <colors_all> is colors_all)
				List<string> entries = this.Pending(param);
				if (entries.Count > 0) {
					string value = args.Trim();
					if ((param == "equipcolor") && value.StartsWith("<", StringComparison.Ordinal) && value.EndsWith(">", StringComparison.Ordinal)
							&& (value.IndexOf('<', 1) < 0)) {
						value = value.Substring(1, value.Length - 2).Trim();
					}
					if (ListTextFits(filename, line, value)) {
						entries[entries.Count - 1] = Quoted(value);
						this.LoadEquipFields(filename, line);
					}
				}
				return;
			}
			if (param == "resources") {
				//#586 - "Resources = amount defname, amount defname, ..." - the real archive's
				//butcher-yield list (corpse-chopping data, not death loot - the butchering
				//interaction itself is a separate, not-yet-built mechanic, see #587).
				List<string> entries = new List<string>();
				string[] resourceParts = args.Split(',');
				foreach (string part in resourceParts) {
					string[] tokens = part.Trim().Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length == 2) {
						ConvertTools.ParseInt32(tokens[0]); //as before: a bad amount fails here, at load
						entries.Add(Quoted(tokens[0] + " " + tokens[1]));
					} else {
						Logger.WriteWarning(filename, line, "resources entry '" + part + "' must be 'amount defname'.");
					}
				}
				this.SetListLines(filename, line, "resourcelist", entries); //the last line, as before
				return;
			}
			if (param == "foodtype") {
				//#593 - "FoodType = [amount] category-or-itemdef, ..." - the real archive's
				//FOODTYPE= field. Unlike "resources" above, an entry's leading amount is
				//OPTIONAL (bare category entries like "t_coin" are real, confirmed in archive
				//content) - falls back to DefaultFoodSatiationValue when omitted.
				List<string> entries = new List<string>();
				foreach (string part in args.Split(',')) {
					string trimmed = part.Trim();
					if (trimmed.Length == 0) {
						continue;
					}
					string[] tokens = trimmed.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
					if (tokens.Length == 2) {
						ConvertTools.ParseInt32(tokens[0]);
						entries.Add(Quoted(tokens[0] + " " + tokens[1]));
					} else if (tokens.Length == 1) {
						entries.Add(Quoted(tokens[0]));
					} else {
						Logger.WriteWarning(filename, line, "foodtype entry '" + part + "' must be '[amount] category'.");
					}
				}
				this.SetListLines(filename, line, "foodtypelist", entries);
				return;
			}
			//a .def field name matching a real Character ResistX property (case-insensitive)
			//is a starting resist {min max} range, e.g. "ResistParalyse = 700 800" or the
			//single-value "ResistBlunt = 150" shape - ParseStatRange, never forwarded to
			//base.LoadScriptLine, so the NPCDef's own (otherwise redundant) base Resist* scalar never needs
			//to be kept in sync - only the per-instance rolled value, set directly on the live
			//Character by NPC.On_Create, actually matters.
			if (knownResistFieldNames.Contains(param)) {
				int[] resistRange = ParseStatRange(filename, line, param, args);
				if (resistRange != null) {
					//stored key may not be in Character's exact PascalCase (the .def parser's
					//own casing for `param` varies) - NPC.On_Create's roll step below looks the
					//matching Character property up case-insensitively, so this is fine as-is.
					this.AddListLine(filename, line, "resistrangelist", Quoted(param + " " + resistRange[0] + " " + resistRange[1]));
				}
				return;
			}
			//#562 - a .def field name matching a SkillName enum member (case-insensitive) is a
			//starting skill-value range, e.g. "Wrestling = 700 800" or "MagicResist = 900"
			SkillName skillName;
			if (Enum.TryParse(param, true, out skillName)) {
				int[] range = ParseStatRange(filename, line, param, args);
				if (range != null) {
					this.AddListLine(filename, line, "skillrangelist", Quoted(skillName + " " + range[0] + " " + range[1]));
				}
				return;
			}
			base.LoadScriptLine(filename, line, param, args);
		}

		/// <summary>#1405 - a list element is kept as text between quotes: a quote in it would end it, and FieldValue runs a
		/// string with '&lt;' or '&gt;' through LScript once, when the field resolves, so a per-spawn expression would be
		/// fixed for good. No such line exists; one would be warned about and left out.</summary>
		private static bool ListTextFits(string filename, int line, string text) {
			if ((text.IndexOf('"') < 0) && (text.IndexOf('<') < 0) && (text.IndexOf('>') < 0)) {
				return true;
			}
			Logger.WriteWarning(filename, line, "'" + text + "': a list line can't hold a quote, '<' or '>' (#1405).");
			return false;
		}

		/// <summary>A list element the views can't read (a GM's or a save's malformed text) is warned about and skipped.</summary>
		private void SkipBadElement(string field, string element, Exception e) {
			Logger.WriteWarning("NPCDef " + this.PrettyDefname, field + " element '" + element + "' can't be read: " + e.Message);
		}

		/// <summary>#1405 - the lines gathered while loading aren't needed once the section is loaded.</summary>
		protected override void On_AfterLoadFromScripts() {
			this.pendingLists = null;
			base.On_AfterLoadFromScripts();
		}

		/// <summary>#1405 - a "quoted" list element: kept whole (commas, spaces) and as text by the field.</summary>
		private static string Quoted(string text) {
			return "\"" + text + "\"";
		}

		private List<string> Pending(string field) {
			if (this.pendingLists == null) {
				this.pendingLists = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			}
			if (!this.pendingLists.TryGetValue(field, out List<string> lines)) {
				lines = new List<string>();
				this.pendingLists[field] = lines;
			}
			return lines;
		}

		/// <summary>#1405 - one more line of a list keyword: the field gets the whole list so far, in script order.</summary>
		private void AddListLine(string filename, int line, string field, string element) {
			List<string> lines = this.Pending(field);
			lines.Add(element);
			base.LoadScriptLine(filename, line, field, string.Join(", ", lines));
		}

		/// <summary>#1405 - a keyword whose last line counts (Resources, FoodType): the field gets this line's elements.</summary>
		private void SetListLines(string filename, int line, string field, List<string> elements) {
			this.Pending(field).Clear();
			this.Pending(field).AddRange(elements);
			base.LoadScriptLine(filename, line, field, string.Join(", ", elements));
		}

		private void LoadEquipFields(string filename, int line) {
			base.LoadScriptLine(filename, line, "equiplist", string.Join(", ", this.Pending("equiplist")));
			base.LoadScriptLine(filename, line, "equipcolorlist", string.Join(", ", this.Pending("equipcolor")));
			base.LoadScriptLine(filename, line, "equipnamelist", string.Join(", ", this.Pending("equipname")));
		}

		/// <summary>An "Equip =" line as an entry (no colour or name yet), or null with a warning, as the load did.</summary>
		private static StartingEquipmentEntry ParseEquipEntry(string filename, int line, string args) {
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
				return null;
			}
			return entry;
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
					return (AggroMode) intTag; //#265 - saves before #407 wrote an enum tag as a plain number
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

		/// <summary>#1485 - Sphere's NPC DUPE (CChar's M_Dupe: CreateNPCOwned(GetID()) and MoveNearObj(this, 1)): a new NPC of
		/// the same def on a free tile beside this one (its own if none), made as any other (its own AI, gear and mount, not
		/// the spawn gem's), not a copy of this one's state. A deep copy failed on its AI timer, and past that would share
		/// its mount and lose its plugins' timers.</summary>
		public override Thing Dupe() {
			var map = this.GetMap();
			int x = this.X, y = this.Y;
			for (int tries = 0; tries < 8; tries++) {
				int nearX = this.X + Globals.dice.Next(-1, 2), nearY = this.Y + Globals.dice.Next(-1, 2);
				if (((nearX != this.X) || (nearY != this.Y)) && map.CanFit(nearX, nearY, this.Z, 16, true, true, true)) {
					x = nearX;
					y = nearY;
					break;
				}
			}
			return this.Def.Create(x, y, this.Z, this.M);
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