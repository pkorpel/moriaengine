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
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

//
namespace SteamEngine.Converter {
	//	/**
	//	Hardcoded changes to converted scripts:
	//		1) The fishing pole is forced to be twohanded.
	//			(It's detected by checking if an item's layer is 2 and model is 0xdbf. 0xdc0 is a dupeitem of it and so will inherit its twohandedness.)
	//	
	//	*/
	//	


	public class ConvertedCharDef : ConvertedThingDef {
		public static Dictionary<string, ConvertedThingDef> charsByDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<int, ConvertedThingDef> charsByModel = new Dictionary<int, ConvertedThingDef>();
		//by model and by defnames


		private static LineImplTask[] firstStageImpl = {
				new LineImplTask("anim", MayBeHex_IgnorePoint), 
//TODO:
				new LineImplTask("npc", WriteAsComment),
				new LineImplTask("brain", WriteAsComment)
				


			};

		private static LineImplTask[] thirdStageImpl = {
			new LineImplTask("sound", HandleSound),
			new LineImplTask("mountid", HandleMountId),
			new LineImplTask("mountitem", HandleMountId),
			new LineImplTask("tag.mountid", HandleMountId),
			new LineImplTask("tag.guard", HandleGuardTag),
			new LineImplTask("icon", WriteAsIsLine),
			new LineImplTask("can", PassThroughUnlessPlayerDef),
			new LineImplTask("aggromode", PassThroughUnlessPlayerDef),
			new LineImplTask("combatspell", PassThroughUnlessPlayerDef),
			new LineImplTask("experience", PassThroughUnlessPlayerDef),
			new LineImplTask("tamingdifficulty", PassThroughUnlessPlayerDef),
			new LineImplTask("bloodcolor", PassThroughUnlessPlayerDef),
			new LineImplTask("resources", PassThroughUnlessPlayerDef),
			new LineImplTask("moverate", PassThroughUnlessPlayerDef),
			new LineImplTask("foodtype", PassThroughUnlessPlayerDef),
			new LineImplTask("attack", HandleAttack),
			new LineImplTask("dam", HandleAttack),
			new LineImplTask("armor", HandleArmor),
			//#606 - the real archive's tag.resist_X fields usually live inside the ON=@Create
			//trigger body (see ProcessCreateTriggerLine's own "tag.resist_" case below), but a
			//rare few chardefs (e.g. c_strazce_golem in specNPCVypravec.scp) set them as plain
			//TOP-LEVEL lines instead - these never reach ProcessCreateTriggerLine at all, so
			//they need their own LineImplTask entries, one per real alias name found in source.
			new LineImplTask("tag.resist_poison", HandleResistTag),
			new LineImplTask("tag.resist_para", HandleResistTag),
			new LineImplTask("tag.resist_bleed", HandleResistTag),
			new LineImplTask("tag.resist_fire", HandleResistTag),
			new LineImplTask("tag.resist_summon", HandleResistTag),
			new LineImplTask("tag.resist_sumon", HandleResistTag),
			new LineImplTask("tag.resist_elect", HandleResistTag),
			new LineImplTask("tag.resist_pierce", HandleResistTag),
			new LineImplTask("tag.resist_slash", HandleResistTag),
			new LineImplTask("tag.resist_cold", HandleResistTag),
			new LineImplTask("tag.resist_sharp", HandleResistTag),
			new LineImplTask("tag.resist_blunt", HandleResistTag),
			new LineImplTask("tag.resist_phys", HandleResistTag),
			new LineImplTask("tag.resist_magic", HandleResistTag),
			new LineImplTask("tag.resist_acid", HandleResistTag),
			new LineImplTask("tag.resist_dragon", HandleResistTag)
		};

		//#606 - real archive resist-tag suffix -> this engine's existing Character.ct
		//ResistX field name(s) (already a fully live, consumed-in-combat mechanic via
		//DamageManager.GetResistModifier - just starved of content until now). "sharp" has
		//no dedicated property - DamageType.Sharp = Slashing|Stabbing is already a combined
		//flag in this engine's own enum, so the faithful mapping writes the same value to
		//both. "sumon" is a confirmed real typo variant of "summon" in the source archive.
		//"curse" maps to ResistCurse (added for this field specifically - a real, shipped
		//Curse spell/effect exists, CurseSpellEffectPlugin, unlike silence/taunt/working
		//below, which still have no matching mechanic anywhere in moriaengine and stay
		//deliberately unmapped).
		private static readonly Dictionary<string, string[]> resistFieldAliases =
			new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
				{ "poison", new[] { "ResistPoison" } },
				{ "para", new[] { "ResistParalyse" } },
				{ "bleed", new[] { "ResistBleed" } },
				{ "fire", new[] { "ResistFire" } },
				{ "summon", new[] { "ResistSummon" } },
				{ "sumon", new[] { "ResistSummon" } },
				{ "elect", new[] { "ResistElectric" } },
				{ "pierce", new[] { "ResistStabbing" } },
				{ "slash", new[] { "ResistSlashing" } },
				{ "cold", new[] { "ResistCold" } },
				{ "sharp", new[] { "ResistSlashing", "ResistStabbing" } },
				{ "blunt", new[] { "ResistBlunt" } },
				{ "phys", new[] { "ResistPhysical" } },
				{ "magic", new[] { "ResistMagic" } },
				{ "acid", new[] { "ResistAcid" } },
				{ "dragon", new[] { "ResistDragon" } },
				{ "curse", new[] { "ResistCurse" } }
			};

		//shared by both placements (top-level LineImplTask above, @Create-body
		//ProcessCreateTriggerLine case below). Emits a SINGLE value ("N") or a real range
		//("N1 N2") depending on how many numbers the source actually had - NOT averaged
		//anymore. A Sphere {min max}/<eval {min max}> range (e.g. c_titan's own
		//tag.resist_para=<eval {700 800}>) means "roll a different value per spawn", same
		//semantics as every other ranged NPCDef field (Str/MaxHits/skill values/etc) -
		//averaging into one flat number was silently discarding that per-instance
		//randomization. NPCDef's own NPC.cs LoadScriptLine (engine side, not this converter)
		//parses this "N1 N2" shape via the same ParseStatRange helper those other fields use.
		//NPCDef-only - no PlayerDef in the archive ever sets these.
		private static void EmitResistFields(ConvertedDef def, string resistKind, string value, string comment) {
			string[] fieldNames;
			if (!resistFieldAliases.TryGetValue(resistKind, out fieldNames)) {
				return; //silence/taunt/working/etc - no matching mechanic, leave unmapped
			}
			MatchCollection resistMatches = numberRE.Matches(value);
			if (resistMatches.Count == 0) {
				return;
			}
			int first = (int) Math.Round(double.Parse(resistMatches[0].Value, CultureInfo.InvariantCulture));
			string emitted;
			if (resistMatches.Count > 1) {
				int second = (int) Math.Round(double.Parse(resistMatches[1].Value, CultureInfo.InvariantCulture));
				emitted = first + " " + second;
			} else {
				emitted = first.ToString(CultureInfo.InvariantCulture);
			}
			//#656 - "specific" (a single-field alias, e.g. slash/pierce) always wins over
			//"general" (a multi-field alias - currently only "sharp") for a given field,
			//regardless of which one this chardef happens to list first.
			bool specific = fieldNames.Length == 1;
			ConvertedCharDef charDef = (ConvertedCharDef) def;
			foreach (string fieldName in fieldNames) {
				PendingResist existing;
				if (charDef.pendingResistFields.TryGetValue(fieldName, out existing) && existing.Specific && !specific) {
					continue; //a specific tag already claimed this field - the general tag defers to it
				}
				charDef.pendingResistFields[fieldName] = new PendingResist { Value = emitted, Comment = comment, Specific = specific };
			}
		}

		private static void HandleResistTag(ConvertedDef d, PropsLine line) {
			ConvertedCharDef def = (ConvertedCharDef) d;
			if (def.IsPlayerDefByName) {
				WriteAsComment(def, line);
				return;
			}
			string resistKind = line.Name.Substring("tag.resist_".Length);
			EmitResistFields(def, resistKind, line.Value, line.Comment);
		}

		//guards against both "attack" and "dam" showing up on the same def
		private bool attackHandled;

		//#565 - captured from the "karma"/"npc" @Create lines as they're seen (possibly in
		//either order - confirmed real archive content has both orders), then consulted
		//together in ThirdStage once the whole trigger body has been read, to derive AggroMode.
		private double? pendingKarmaMidpoint;
		private string pendingBrainType;

		//#700 - captured from the real archive's TOP-LEVEL "tag.guard=1/2" line (user-confirmed
		//domain knowledge: 1 = Gondor-side guard, attacks Mordor; 2 = Mordor-side, attacks
		//Gondor - not documented anywhere in this repo before now). Consulted in ThirdStage
		//alongside pendingBrainType/pendingKarmaMidpoint to override the generic AggroMode
		//derivation (#565) for guards specifically, and to also emit a Realm so
		//NPCAiTimer.IsOppositeRealm has something real to compare against.
		private int? pendingGuardSide;

		//true right after an "itemnewbie=" line, cleared by whatever line comes next - used to
		//tell whether an immediately-following "color=" line is colorizing the just-granted
		//item (the real archive's "ITEMNEWBIE=x / COLOR=y" pairing, e.g. c_m_HEALER's facial
		//hair matching its own just-equipped head hair) rather than setting the character's own
		//base body color. See the "color" case in ProcessCreateTriggerLine below.
		private bool pendingEquipColorTarget;

		//#656 - same ThirdStage-deferred precedent as pendingKarmaMidpoint/pendingBrainType
		//above: a chardef's resist tags arrive one line at a time (possibly from either the
		//rare top-level LineImplTask path or the @Create-body ProcessCreateTriggerLine path,
		//in either order), but "sharp" maps to BOTH ResistSlashing and ResistStabbing (no
		//dedicated property exists - DamageType.Sharp combines both flags) while "slash"/
		//"pierce" each map to exactly one of those same fields. A chardef that sets a general
		//"sharp" AND a specific "slash"/"pierce" (very common in the real archive) must not
		//emit two competing Set() calls for the same field - the engine field is a plain
		//scalar Int32, so the real .def loader just applies whichever line happens to come
		//last, silently discarding the other and making the effective value depend on the
		//archive's own line order (sharp-before-slash/pierce, by accident, not by design).
		//Accumulated here instead, with the more specific tag always winning regardless of
		//order, then flushed exactly once per field in ThirdStage.
		private sealed class PendingResist {
			public string Value;
			public string Comment;
			public bool Specific;
		}
		private readonly Dictionary<string, PendingResist> pendingResistFields =
			new Dictionary<string, PendingResist>(StringComparer.OrdinalIgnoreCase);

		public ConvertedCharDef(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.byModel = charsByModel;
			this.byDefname = charsByDefname;

			this.firstStageImplementations.Add(firstStageImpl);
			this.thirdStageImplementations.Add(thirdStageImpl);
		}

		//TEVENTS/aggromode/etc are only valid on NPCDef - a PlayerDef (c_man/c_woman) can't
		//carry them, so PrettyDefname must already be resolved (it is, by ThirdStage time)
		private bool IsPlayerDefByName {
			get {
				switch (this.PrettyDefname.ToLowerInvariant()) {
					case "c_man":
					case "c_woman":
						return true;
					default:
						return false;
				}
			}
		}

		public override void ThirdStage() {
			base.ThirdStage();

			if (this.IsPlayerDefByName) {
				this.headerType = "PlayerDef";
			} else {
				this.headerType = "NPCDef";
				//#656 - flush the accumulated resist fields exactly once each, now that both the
				//rare top-level LineImplTask path and the @Create-body path have had their chance
				//to populate pendingResistFields (both run inside base.ThirdStage() above).
				foreach (KeyValuePair<string, PendingResist> pending in this.pendingResistFields) {
					this.Set(pending.Key, pending.Value.Value, pending.Value.Comment);
				}
				//#565/#700 - derive AggroMode now that the whole @Create trigger body AND the
				//top-level tag.guard line (if any) have been read (karma/npc can appear in
				//either order inside @Create; tag.guard is always top-level, but still only
				//guaranteed populated once ALL of base.ThirdStage() above has run), since
				//there's no literal AGGROMODE= field anywhere in the real archive to pass
				//through instead.
				if (this.pendingGuardSide.HasValue) {
					this.Set("AggroMode", "GuardsRealm", "");
					//#700 - user-confirmed domain knowledge: tag.guard=1 is the Gondor side
					//(attacks Mordor), tag.guard=2 is the Mordor side (attacks Gondor) - not
					//documented anywhere in the real archive's own comments, so not something
					//the converter could have inferred without this. Any other value is left
					//unmapped (no Realm emitted) rather than guessed.
					if (this.pendingGuardSide.Value == 1) {
						this.Set("Realm", "Gondor", "");
					} else if (this.pendingGuardSide.Value == 2) {
						this.Set("Realm", "Mordor", "");
					} else {
						this.Warning(this.origData.HeaderLine, "tag.guard value '" + this.pendingGuardSide.Value + "' is neither 1 (Gondor) nor 2 (Mordor) - Realm not set, GuardsRealm aggro will never trigger.");
					}
				} else {
					this.Set("AggroMode", DeriveAggroMode(this.pendingBrainType, this.pendingKarmaMidpoint), "");
				}
			}
		}

		//#565 - brain types that are never meant to be hostile regardless of their often
		//mildly-negative "flavor" karma (confirmed against real archive data: 26/37 real
		//brain_vendor chardefs sit at exactly Karma=-1000).
		private static readonly HashSet<string> calmBrains = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"brain_vendor", "brain_banker", "brain_animal_trainer"
		};
		//#565 - brain types that are always hostile regardless of karma magnitude -
		//confirmed this also catches real undead-boss chardefs (liche/vampire/satan
		//families) that set Karma=0 explicitly, which a pure-Karma reading would miss.
		private static readonly HashSet<string> hostileBrains = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"brain_monster", "brain_dragon", "brain_beserk", "brain_daemon"
		};

		//#565 - 3-tier priority formula, derived from real archive data (698 chardefs with
		//Karma, cross-tabbed against NPC=brain_xxx): brain-type override first (vendor/
		//banker/trainer always Calm, monster/dragon/beserk/daemon always Hostile), then
		//Karma as the fallback for everything else (brain_human, brain_animal, no/
		//unrecognized brain) - Hostile only if the configured Karma midpoint is strictly
		//below -1000 (also the default when Karma isn't configured at all).
		private static string DeriveAggroMode(string brainType, double? karmaMidpoint) {
			if ((brainType != null) && calmBrains.Contains(brainType)) {
				return "Calm";
			}
			if ((brainType != null) && hostileBrains.Contains(brainType)) {
				return "Hostile";
			}
			if (karmaMidpoint.HasValue && (karmaMidpoint.Value < -1000)) {
				return "Hostile";
			}
			return "Calm";
		}

		private static void WriteAsIsLine(ConvertedDef def, PropsLine line) {
			def.Set(line);
		}

		private static readonly Regex numberRE = new Regex(@"[-+]?\d+(\.\d+)?",
			RegexOptions.CultureInvariant | RegexOptions.Compiled);

		//the real archive field is tag.experience=<eval {min max}>, embedded inside the
		//ON=@Create trigger body - NOT the top-level "experience" line the base LineImplTask
		//handlers above already cover. NPCDef.ExperienceWorth (#127) already understands the
		//"min max" shape this emits, so this is a pure converter fix - #566.
		protected override void ProcessCreateTriggerLine(string name, string value, string comment, int lineNum) {
			//the real archive's "COLOR=..." line right after an "ITEMNEWBIE=..." line colors
			//THAT item (e.g. c_m_HEALER: "ITEMNEWBIE=random_facial_hair / COLOR=match_hair" -
			//the beard matches the head hair equipped just before it), not the character's own
			//base body color - captured as the pending flag BEFORE this call can reset it below,
			//so an "itemnewbie" line 2+ calls back still correctly clears a STALE flag from a
			//color-less item in between.
			bool colorsPrecedingItem = this.pendingEquipColorTarget;
			this.pendingEquipColorTarget = false;
			if (name == "color" && colorsPrecedingItem) {
				if (!this.IsPlayerDefByName) {
					this.Set("EquipColor", value.Trim(), comment);
				}
				return;
			}
			//#565 - the real archive brain assignment lives inside @Create too (e.g.
			//c_dragon_v1: "NPC=brain_dragon"), right alongside karma/fame - captured here,
			//consulted together with pendingKarmaMidpoint in ThirdStage once the whole
			//trigger body has been read (the two lines can appear in either order).
			if (name == "npc") {
				if (!this.IsPlayerDefByName) {
					this.pendingBrainType = value.Trim().ToLowerInvariant();
				}
				return;
			}
			if (name == "tag.experience") {
				MatchCollection matches = numberRE.Matches(value);
				if (matches.Count > 0) {
					int min = (int) Math.Round(double.Parse(matches[0].Value, CultureInfo.InvariantCulture));
					int max = min;
					if (matches.Count > 1) {
						max = (int) Math.Round(double.Parse(matches[1].Value, CultureInfo.InvariantCulture));
					}
					this.Set("Experience", min + " " + max, comment);
				}
				return;
			}
			//#606 - the real archive's tag.resist_X lines overwhelmingly live inside @Create
			//(confirmed: 8576 of 8587 sampled occurrences across scripts/chars/*.scp), where
			//the base ProcessCreateTriggerLine's bare switch has no default case and silently
			//drops anything it doesn't recognize - these were never reaching the engine at
			//all before this. See resistFieldAliases/EmitResistFields above for the mapping.
			if (name.StartsWith("tag.resist_", StringComparison.OrdinalIgnoreCase)) {
				if (this.IsPlayerDefByName) {
					return;
				}
				EmitResistFields(this, name.Substring("tag.resist_".Length), value, comment);
				return;
			}
			//#559 - the real archive STR/DEX/INT lines live inside @Create too (e.g. c_titan:
			//"STR=1500 DEX=350 INT=60"), as either a plain number or a Sphere {min max} range -
			//NPCDef.StrRange/DexRange/IntRange (#559) understands the same "value"/"min max"
			//shape ExperienceWorth already uses. NPCDef-only (no PlayerDef in the archive ever
			//sets these - c_man/c_woman have no STR/DEX/INT line at all), same guard as attack/dam.
			if (name == "str" || name == "dex" || name == "int") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string fieldName = name == "str" ? "Str" : (name == "dex" ? "Dex" : "Int");
				MatchCollection matches = numberRE.Matches(value);
				if (matches.Count > 0) {
					int min = (int) Math.Round(double.Parse(matches[0].Value, CultureInfo.InvariantCulture));
					if (matches.Count > 1) {
						int max = (int) Math.Round(double.Parse(matches[1].Value, CultureInfo.InvariantCulture));
						this.Set(fieldName, min + " " + max, comment);
					} else {
						this.Set(fieldName, min.ToString(), comment);
					}
				}
				return;
			}
			//#560 - the real archive FAME/KARMA lines live inside @Create too (e.g. c_titan:
			//"FAME={100 2000} KARMA={-5000 -5999}"), always a Sphere {min max} range in source -
			//NPCDef.FameRange/KarmaRange (#560) understands the same shape. numberRE already
			//matches a leading sign, so KARMA's negative values parse correctly. NPCDef-only,
			//same guard as str/dex/int.
			if (name == "fame" || name == "karma") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string fieldName = name == "fame" ? "Fame" : "Karma";
				MatchCollection matches = numberRE.Matches(value);
				if (matches.Count > 0) {
					int min = (int) Math.Round(double.Parse(matches[0].Value, CultureInfo.InvariantCulture));
					if (matches.Count > 1) {
						int max = (int) Math.Round(double.Parse(matches[1].Value, CultureInfo.InvariantCulture));
						this.Set(fieldName, min + " " + max, comment);
						//#565 - stash the karma midpoint for the AggroMode derivation in ThirdStage
						if (name == "karma") {
							this.pendingKarmaMidpoint = (min + max) / 2.0;
						}
					} else {
						this.Set(fieldName, min.ToString(), comment);
						if (name == "karma") {
							this.pendingKarmaMidpoint = (double) min;
						}
					}
				}
				return;
			}
			//#561 - the real archive maxhits/maxstam/maxmana lines live inside @Create too
			//(e.g. c_titan: "maxhits={1500 1700} maxstam={350 400} maxmana=0"), as either a
			//plain number or a Sphere {min max} range - NPCDef.MaxHitsRange/MaxStamRange/
			//MaxManaRange (#561) understands the same shape. NPCDef-only, same guard as
			//str/dex/int/fame/karma.
			if (name == "maxhits" || name == "maxstam" || name == "maxmana") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string hpFieldName = name == "maxhits" ? "MaxHits" : (name == "maxstam" ? "MaxStam" : "MaxMana");
				MatchCollection hpMatches = numberRE.Matches(value);
				if (hpMatches.Count > 0) {
					int min = (int) Math.Round(double.Parse(hpMatches[0].Value, CultureInfo.InvariantCulture));
					if (hpMatches.Count > 1) {
						int max = (int) Math.Round(double.Parse(hpMatches[1].Value, CultureInfo.InvariantCulture));
						this.Set(hpFieldName, min + " " + max, comment);
					} else {
						this.Set(hpFieldName, min.ToString(), comment);
					}
				}
				return;
			}
			//#562 - the real archive sets skill starting values inside @Create too (e.g.
			//c_titan_ranger: "PARRYING={40.0 50.0} WRESTLING={70.0 80.0} ARCHERY={90.0 95.0}
			//resist=100.0 SpiritSpeak=60.0"), as a plain percentage or a Sphere {min max}
			//percentage range. Skills are stored x10 internally in this engine (confirmed
			//elsewhere - 70.0% is raw value 700), so the parsed number(s) are scaled by 10
			//before being emitted. The archive's own field names don't all match the
			//SkillName enum member names NPCDef.LoadScriptLine matches against (#562) -
			//skillNameAliases translates the archive's name to the canonical one to emit.
			string skillFieldName;
			if (skillNameAliases.TryGetValue(name, out skillFieldName)) {
				if (this.IsPlayerDefByName) {
					return;
				}
				MatchCollection skillMatches = numberRE.Matches(value);
				if (skillMatches.Count > 0) {
					int min = (int) Math.Round(double.Parse(skillMatches[0].Value, CultureInfo.InvariantCulture) * 10);
					if (skillMatches.Count > 1) {
						int max = (int) Math.Round(double.Parse(skillMatches[1].Value, CultureInfo.InvariantCulture) * 10);
						this.Set(skillFieldName, min + " " + max, comment);
					} else {
						this.Set(skillFieldName, min.ToString(), comment);
					}
				}
				return;
			}
			//#563 - the real archive grants starting equipment via ITEMNEWBIE lines inside
			//@Create (e.g. c_titan_ranger: "ITEMNEWBIE=i_bow" then "ITEMNEWBIE=i_arrow,{50
			//75}"), one line per item - a chardef commonly has several in a row. Confirmed real
			//forms in source: bare defname, "defname,amount" (plain number), and
			//"defname,{min max}" (Sphere range) - all three map onto NPCDef.StartingEquipment's
			//"Equip = defname[, amount or min max]" syntax. NPCDef-only, same guard as the
			//other @Create fields above.
			if (name == "itemnewbie") {
				if (this.IsPlayerDefByName) {
					return;
				}
				this.pendingEquipColorTarget = true;
				int comma = value.IndexOf(',');
				if (comma < 0) {
					this.Set("Equip", value.Trim(), comment);
				} else {
					string itemDefname = value.Substring(0, comma).Trim();
					string amountPart = value.Substring(comma + 1);
					MatchCollection amountMatches = numberRE.Matches(amountPart);
					if (amountMatches.Count > 0) {
						int min = (int) Math.Round(double.Parse(amountMatches[0].Value, CultureInfo.InvariantCulture));
						if (amountMatches.Count > 1) {
							int max = (int) Math.Round(double.Parse(amountMatches[1].Value, CultureInfo.InvariantCulture));
							this.Set("Equip", itemDefname + ", " + min + " " + max, comment);
						} else {
							this.Set("Equip", itemDefname + ", " + min, comment);
						}
					} else {
						this.Set("Equip", itemDefname, comment);
					}
				}
				return;
			}
			//#700 - "ITEM=i_npc_mount" is a fixed marker the real archive always writes right
			//before "tag.mount=.../tag.mountcolor=..." (confirmed archive-wide: every single
			//"ITEM=" line inside @Create is this exact marker, never a loot entry - see
			//ConvertedThingDef.ThirdStage's own doc comment for the full writeup of why this
			//line used to abort the rest of @Create entirely). The marker itself carries no
			//information beyond "the next 1-2 lines are a mount" - nothing to Set.
			if ((name == "item") && value.Trim().Equals("i_npc_mount", StringComparison.OrdinalIgnoreCase)) {
				return;
			}
			//#700 - resolves the same way HandleMountId already does for the OTHER real
			//archive naming convention (mountid/mountitem/tag.mountid) - a bare defname or
			//model number for the actual mount item, emitted as the same NPCDef.MountItem
			//field so both conventions land on one engine-side mechanism.
			if (name == "tag.mount") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string mountValue = value.Trim();
				int mountModel;
				if (!ConvertTools.TryParseInt32(mountValue, out mountModel)) {
					ConvertedThingDef mountDef;
					if (ConvertedItemDef.itemsByDefname.TryGetValue(mountValue, out mountDef)) {
						mountModel = mountDef.Model;
					} else {
						mountModel = -1;
					}
				}
				if (mountModel != -1) {
					this.Set("MountItem", "0x" + mountModel.ToString("x"), comment);
				} else {
					this.Warning(lineNum, "Unresolvable MountItem model '" + mountValue + "' (tag.mount)");
				}
				return;
			}
			//#700 - no engine equivalent exists: Character's own mount display reuses the
			//character's OWN body color (Thing.ShortColor - there's no independent per-
			//instance mount-color field), so this can't be mapped onto anything real without
			//also recoloring the rider's own skin. Preserved as a comment (not silently
			//dropped, not misapplied) in case mount-specific coloring gets added later.
			if (name == "tag.mountcolor") {
				if (!this.IsPlayerDefByName) {
					this.writtenData.Add("//tag.mountcolor = " + value.Trim() +
						" //no independent mount-color field in this engine yet, see #700" +
						(string.IsNullOrEmpty(comment) ? "" : " //" + comment));
				}
				return;
			}
			//#564 - the real archive grants multiple spells via addspell= lines inside @Create
			//(e.g. c_titan_mage: "addspell=s_lightning" then "addspell=s_fireball" then
			//"addspell=s_magic_reflection"), one line per spell - maps directly onto
			//NPCDef.Spells' "AddSpell = defname" syntax, no numeric parsing needed (unlike
			//str/fame/skills). NPCDef-only, same guard as the other @Create fields above.
			if (name == "addspell") {
				if (this.IsPlayerDefByName) {
					return;
				}
				this.Set("AddSpell", value.Trim(), comment);
				return;
			}
			//#607 - the real archive's tag.weapflag field (always inside @Create, confirmed
			//140/140 sampled occurrences) encodes a monster's own NATURAL attack damage type
			//(claws/bite/etc, not a worn weapon item) as pipe-separated dam_* tokens, e.g.
			//"tag.weapflag=dam_physical|dam_fire". Maps onto NPCDef.NaturalDamageType (#607) -
			//a new field, since WeaponType alone can't express a multi-flag elemental combo.
			if (name == "tag.weapflag") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string[] tokens = value.Split('|');
				//build the output as an explicit "DamageType.X|DamageType.Y" list of only the
				//matched flag names - NOT combined.ToString(), which for a value that doesn't
				//exactly match one declared named combo (e.g. Physical|Sharp|Summon) falls back
				//to a bare, non-"DamageType."-qualified comma list ("PhysicalSharp, Summon") -
				//a different, untested shape from the confirmed-working qualified-pipe syntax.
				List<string> matchedNames = new List<string>();
				foreach (string token in tokens) {
					DamageType flag;
					if (weapflagAliases.TryGetValue(token.Trim(), out flag)) {
						matchedNames.Add("DamageType." + flag);
					}
				}
				if (matchedNames.Count > 0) {
					this.Set("NaturalDamageType", string.Join("|", matchedNames), comment);
				}
				return;
			}
			base.ProcessCreateTriggerLine(name, value, comment, lineNum);
		}

		//#607 - real archive dam_* token -> this engine's existing DamageType [Flags] enum
		//member (Enums.cs) - all map cleanly, no new enum values needed. "lightning" and
		//"elect" are two different real spellings for the same thing in source.
		private static readonly System.Collections.Generic.Dictionary<string, DamageType> weapflagAliases =
			new System.Collections.Generic.Dictionary<string, DamageType>(StringComparer.OrdinalIgnoreCase) {
				{ "dam_physical", DamageType.Physical },
				{ "dam_blunt", DamageType.Blunt },
				{ "dam_slashing", DamageType.Slashing },
				{ "dam_sharp", DamageType.Sharp },
				{ "dam_piercing", DamageType.Stabbing },
				{ "dam_magic", DamageType.Magic },
				{ "dam_fire", DamageType.Fire },
				{ "dam_cold", DamageType.Cold },
				{ "dam_lightning", DamageType.Electric },
				{ "dam_elect", DamageType.Electric },
				{ "dam_summon", DamageType.Summon }
			};

		//#562 - archive field name (lowercased) -> canonical SkillName enum member name to
		//emit in the .def field, for the ~29 skill names real chardefs set inside @Create.
		//NPCDef.LoadScriptLine's own fallback matches against the literal enum member name
		//(case-insensitive), so most of these map 1:1 - only the handful below actually need
		//translating (archive vocabulary vs. this engine's SkillName naming).
		private static readonly Dictionary<string, string> skillNameAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			{ "wrestling", "Wrestling" },
			{ "tactics", "Tactics" },
			{ "resist", "MagicResist" },
			{ "parrying", "Parry" },
			{ "spiritspeak", "SpiritSpeak" },
			{ "peacemaking", "Peacemaking" },
			{ "provocation", "Provocation" },
			{ "swordsmanship", "Swords" },
			{ "magery", "Magery" },
			{ "fencing", "Fencing" },
			{ "macefighting", "Macing" },
			{ "evalint", "EvalInt" },
			{ "taming", "AnimalTaming" },
			{ "archery", "Archery" },
			{ "poisoning", "Poisoning" },
			{ "animallore", "AnimalLore" },
			{ "forensics", "Forensics" },
			{ "anatomy", "Anatomy" },
			{ "healing", "Healing" },
			{ "detectinghidden", "DetectHidden" },
			{ "hiding", "Hiding" },
			{ "stealth", "Stealth" },
			{ "meditation", "Meditation" },
			{ "inscription", "Inscribe" },
			{ "armslore", "ArmsLore" },
			{ "cooking", "Cooking" },
			{ "lumberjacking", "Lumberjacking" },
			{ "blacksmithing", "Blacksmith" },
			{ "veterinary", "Veterinary" }
		};

		//can/aggromode/combatspell/experience/tamingdifficulty are all NPCDef-only fields
		//(confirmed live in NPC.cs/AbstractCharacterDef.cs, #551) - comment them out rather
		//than emit them on a PlayerDef, where they'd produce an "Invalid data" load warning
		private static void PassThroughUnlessPlayerDef(ConvertedDef d, PropsLine line) {
			ConvertedCharDef def = (ConvertedCharDef) d;
			if (def.IsPlayerDefByName) {
				WriteAsComment(def, line);
			} else {
				def.Set(line);
			}
		}

		//attack/dam -> WeaponAttack, averaged the same way ConvertedItemDef.HandleArmorOrDam
		//averages weapon attack ranges - NPCDef-only, matches #44's own hand-edit precedent
		private static void HandleAttack(ConvertedDef d, PropsLine line) {
			ConvertedCharDef def = (ConvertedCharDef) d;
			if (def.IsPlayerDefByName) {
				WriteAsComment(def, line);
				return;
			}
			if (def.attackHandled) {
				return;
			}
			def.attackHandled = true;
			def.Set("WeaponAttack", AverageSphereNumberList(line.Value), line.Comment);
		}

		//ARMOR -> ArmorVsP/ArmorVsM, averaged and duplicated across both - Character-level,
		//safe for PlayerDef too (unlike attack/dam, which are NPCDef-only weapon fields)
		private static void HandleArmor(ConvertedDef d, PropsLine line) {
			ConvertedCharDef def = (ConvertedCharDef) d;
			string value = AverageSphereNumberList(line.Value);
			def.Set("armorVsP", value, line.Comment);
			def.Set("armorVsM", value, "");
		}

		private static string AverageSphereNumberList(string rawValue) {
			string[] values = Utility.SplitSphereString(rawValue, true);
			int n = values.Length;
			if (n == 0) {
				return rawValue;
			}
			double sum = 0;
			for (int i = 0; i < n; i++) {
				int number;
				if (ConvertTools.TryParseInt32(values[i], out number)) {
					sum += number;
				} else {
					return rawValue;
				}
			}
			return Math.Round(sum / n).ToString();
		}

		private static void HandleMountId(ConvertedDef def, PropsLine line) {
			int num = -1;
			if (!ConvertTools.TryParseInt32(line.Value, out num)) {
				ConvertedThingDef i;
				if (ConvertedItemDef.itemsByDefname.TryGetValue(line.Value, out i)) {
					num = i.Model;
				}
			}
			if (num != -1) {
				string retVal = "0x" + num.ToString("x");
				def.Set("MountItem", retVal, line.Comment);
				//return retVal;
			} else {
				def.Warning(line.Line, "Unresolvable MountItem model");
			}
			//return "";
		}

		//#700 - captures the TOP-LEVEL "tag.guard=1/2" line's raw value for ThirdStage's
		//AggroMode/Realm derivation (see its own doc comment), while still passing the tag
		//through unchanged - other systems (tag.guardopposite/tag.guardhire, both already
		//passed through the same generic way) may still want to read the raw value directly.
		private static void HandleGuardTag(ConvertedDef def, PropsLine line) {
			ConvertedCharDef charDef = def as ConvertedCharDef;
			if (charDef != null) {
				int guardSide;
				if (ConvertTools.TryParseInt32(line.Value, out guardSide)) {
					charDef.pendingGuardSide = guardSide;
				}
			}
			def.Set(line);
		}


		private static void HandleSound(ConvertedDef def, PropsLine line) {
			string retVal = line.Value;
			int num;
			if (ConvertTools.TryParseInt32(line.Value, out num)) {
				retVal = "0x" + num.ToString("x");
				def.Set("AngerSound", retVal, line.Comment);
				def.Set("IdleSound", "0x" + (num + 1).ToString("x"), "");
				def.Set("AttackSound", "0x" + (num + 2).ToString("x"), "");
				def.Set("HurtSound", "0x" + (num + 3).ToString("x"), "");
				def.Set("DeathSound", "0x" + (num + 4).ToString("x"), "");
			} else {
				def.Set("AngerSound", retVal, line.Comment);
				def.Set("IdleSound", retVal + " + 1", "");
				def.Set("AttackSound", retVal + " + 2", "");
				def.Set("HurtSound", retVal + " + 3", "");
				def.Set("DeathSound", retVal + " + 4", "");
			}
			//return retVal;
		}
	}
}
