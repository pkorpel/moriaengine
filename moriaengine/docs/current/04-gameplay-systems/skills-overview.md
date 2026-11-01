# Skills (overview)

The skill-sequence state machine lives in `scripts/Basic/skills/SkillDef.cs`, with
per-character skill values stored by `scripts/Basic/skills/Skill.cs`. Individual skills
(Magery, weapon skills, Hiding, Taming, etc) are `SkillDef` subclasses overriding the
abstract phase methods described below. This page covers the shared machinery; see
[combat.md](combat.md) for the weapon-skill specialization and
[magery-spells.md](magery-spells.md) for Magery's.

## The five-phase sequence

Every skill use runs through `SkillSequenceArgs` (`SkillDef.cs:344`), a small state object
(self, skill def, up to two targets, two generic params, a tool item, a delay, and a success
flag) carrying one skill attempt through 5 named phases, each firing a double trigger pair
(a generic `@skill<Phase>` on the character, then a skill-specific `@<phase>` on the
`SkillDef` itself) before calling the matching abstract `On_*` method:

1. **Select** (`PhaseSelect`/`Trigger_Select`, line 214) - decide whether this attempt is
   valid at all (e.g. `WeaponSkillDef.On_Select` rejects a null or self target, line 49).
2. **Start** (`PhaseStart`/`Trigger_Start`, line 237) - set up the delay
   (`GetDelayForChar`, which returns 0 immediately for a GM, line 139) and begin.
3. **Stroke** (`PhaseStroke`/`Trigger_Stroke`, line 256) - the "important moment in the
   middle" - this is where a `CheckSuccess` roll usually happens, setting
   `SkillSequenceArgs.Success`.
4. **Success or Fail** (`PhaseSuccess`/`PhaseFail`, `Trigger_Success`/`Trigger_Fail`) -
   branches on the `Success` flag set during Stroke.
5. **Abort** (`PhaseAbort`/`Trigger_Abort`, line 311) - the "interrupted from outside, no
   skillgain" path, distinct from a normal Fail.

Each phase's trigger pair can be cancelled at either the character level or the `SkillDef`
level - cancelling at any point short-circuits the rest of that phase (see
[triggers-and-deny-chains.md](../02-engine-core/triggers-and-deny-chains.md) for the general
pattern `TryCancellableTrigger` implements).

## Delay/scheduling

`DelayStroke` (line 538) either calls `PhaseStroke` immediately (a negative delay, the
sentinel `Timer.negativeOneSecond` used as the "no delay" default) or schedules a
`SkillStrokeTimer` (`BoundTimer` subclass, line 608) keyed by a fixed `TimerKey`
(`"_skillTimer_"`) on the character. `AbortSkill(Character)` (line 582) is the standard way
to cancel an in-progress skill from outside - it removes that timer and runs the Abort
phase. `GetSkillSequenceArgs`/`GetSkillSequenceTimer` (lines 591-602) let other code inspect
or interrupt whatever skill a character is currently mid-sequence on.

`SkillSequenceArgs` is `[SaveableClass]` with most fields `[SaveableData]` (Target1/Target2/
Param1/Param2/Tool/DelaySpan/Success) - an in-progress skill attempt (including its pending
`SkillStrokeTimer`) survives a save/reload exactly like any other per-character timer (see
[tags-and-timers.md](../02-engine-core/tags-and-timers.md)).

## Skill values: `Skill`/`SkillDef`

`Skill` (`Skill.cs:25`) is the per-character instance - `RealValue` (the raw, persisted
value, 0-1000 internally, displayed /10 as a percentage) and `ModifiedValue` (RealValue plus
a temporary `modification`, e.g. from a buff/equipped item - never goes negative even if the
sum would). Both setters route through `CharSyncQueue.AboutToChangeSkill` and fire
`SkillDef.Trigger_ValueChanged` (`@skillChange`) when the modified value actually changes.
`Skill.Cap` (line 97) is the live profession-based cap - delegates to
`ProfessionDef.GetSkillCap`, or 0 if the character has no profession at all (see
[professions.md](professions.md)). **This cap is read-only decoration today** - see the gap
below.

`SkillDef` (`SkillDef.cs:27`) is the per-skill-TYPE definition (`AdvRate`/`Delay`/`Effect`
ranges, all `.def`-loaded `double[]` permille-interpolated via `ScriptUtil.EvalRangePermille`
- e.g. `GetEffectForChar` maps a character's raw skill value through that curve, with a GM
treated as having the maximum value, line 161-168). `CheckSuccess(Character, int difficulty)`
(line 175) is the generic pass/fail roll every skill's Stroke phase calls - a GM always
succeeds; everyone else goes through `SkillUtils.CheckSuccess(skillValue, difficulty)` (an
S-curve, not consulted further on this page - see the Hiding note below for one place this
shape itself was questioned).

## Known gap: skill caps are not enforced, by design decision (#140)

`Skill.Cap` is correctly computed but, as of this writing, is read in exactly one place:
`GameCore/Networking/OutgoingPackets.cs` (lines ~1069/1084), purely to decorate the client's
skill-list UI. **Nothing prevents a skill's real value from exceeding its profession cap.**
There is also no organic "use a skill to raise it" mechanic anywhere in the engine - the
obvious extension point (`SkillDef.cs`'s `tkSkillGain`/`tkGain` trigger keys, commented out
at lines 201-202) is dead code.

This is a deliberate, confirmed-with-the-user deferral, not an oversight: most skills are
meant to be raised through the Leveling system's skill-point pool (flat +5/level, spent
through `LevelingSystem.TrySpendSkillPoints` - see
[leveling-experience.md](leveling-experience.md)), which DOES validate against
`Profession.GetSkillCap` at spend time. Only a smaller list of skills (Mining,
Lumberjacking, Fishing, Cooking, Taming, Lockpicking, Blacksmithing, Carpentry, Bowcraft,
Alchemy, Tailoring, Tinkering, Inscription - mostly crafting/gathering) are meant to be
raised through actual use instead, and that mechanism is explicitly deferred until
crafting/gathering content becomes the active focus (#140, Backlog).

## Worth a second look, not a confirmed bug: Hiding's double-random roll

`HidingSkillDef.On_Stroke` feeds a *random* value (`Globals.dice.Next(700)`) into
`CheckSuccess` as the "difficulty" argument, rather than doing Erebor's own direct
`hiding_skill > rand(1025)` single comparison. The net effect is two layered random rolls
instead of one. This diverges from the Sphere reference for no documented reason, but
nothing confirms it's actually wrong (the generic `SkillDef.CheckSuccess` S-curve is used
this way by other skills too) - flagged as worth a second look if Hiding's success rate
ever gets reported as feeling off, not something to "fix" pre-emptively.

## Known gap: Detect Hidden is a bare skill check

`DetectHiddenSkillDef` is just `CheckSuccess(self, target.Hiding)` - no contest mechanic at
all. Erebor's own reference implementation has a full contest (a sector-darkness-scaled
stealth bonus for the hider, a distance penalty for the detector, and a per-detector
re-detection cooldown), none of which is ported. This is a real, confirmed content/mechanic
gap, not a design decision - it's just never been picked up.

## Skill checks and gaining by use (#962)

Sphere's numbers. A check is an S-curve of skill minus difficulty with a variance of 10.0: equal is 50%, 10.0 over is 75%, 10.0 under is 25%. Before #962 the variance was 25.0, so skills are less forgiving both ways now.

Gaining by use is `Skill_Experience`, `SkillGainSystem.TryGainSkill`, with the attempt's difficulty, negative for a failure:

- Failing teaches only under 30.0, and then counts as half the skill less 0.8.
- The chance is 1 in `ADVRATE` successes, read at skill + (skill - difficulty), so a hard job teaches far more than an easy one.
- Over 40.0 below the skill it's halved, over 50.0 below it's nothing.

Only skills with a real `ADVRATE` on Erebor gain this way: crafting, Fishing, Lumberjacking, Mining. On Erebor the rest are `0fffffff`, here 0. Lockpicking and Taming have their own formula. Gathering's difficulty is the resource's own `SKILL` range. Mining's first point is Erebor's 1010, so it starts very slowly.

Not ported: Sphere's skill-sum degrade, as `skillsum` is 30000 on every profession. Stat gains by use: every skill has `BONUSSTATS=0` on Erebor.
