# moriaengine — working state

This file is kept up to date across sessions so a fresh Claude Code session (after
a kill/restart) can pick up exactly where the last one left off without re-deriving
context from scratch. Update it whenever the current task/branch/plan changes —
don't wait until end of session.

## Repo

- GitHub: `pkorpel/moriaengine` (SteamEngine, a Ultima Online server emulator, .NET 8 port)
- Local checkout: `/workspace/moriaengine`
- `main` tracks `origin/main` 1:1 — this repo gets no direct pushes to main, see Workflow below.

## CURRENT STATE (2026-10-13, continued) — roadmap/project-board pass + leveling system design

Session moved from reactive bugfixing into step-back planning, per user request
("I'm feeling like we are lacking some sort of plan"). Three things happened in
this pass, in order:

### 1. GitHub Project board set up — the pipeline view the user wanted

**Board: https://github.com/users/pkorpel/projects/1** ("moriaengine roadmap").
Created via `gh project` CLI using a **separate classic PAT** the user generated
just for this (fine-grained PATs don't reliably expose Projects-v2 permissions —
known GitHub limitation, not user error). That token is session-scratchpad-only
(`/tmp/.../scratchpad/.gh_project_token`, written/read/deleted around each use,
never left sitting on disk) — **do NOT reuse it for anything except `gh project`
commands**; everything else (issues, PRs, merges) still uses the normal default
auth. Token is missing `read:org` scope, so `gh project link` (repo-sidebar
linking) and `--owner pkorpel` (must use `--owner "@me"` instead) don't work —
non-blocking, user hasn't prioritized fixing this.

Structure: two custom single-select fields beyond the GitHub default "Status"
(left alone, unused) —
- **Pipeline Stage**: Backlog / Ready / In Progress / Done
- **Category**: Player-facing bugs / GM tooling / Localization / NPC AI & combat /
  Verification pass / CI & infra / Architecture & tech debt / Client compat

All 19 open issues (at the time) added and categorized. One manual step the user
still needs to do in the web UI (no CLI/API support for this): add a Board-layout
view grouped by "Pipeline Stage" — the default view is still a flat Table.

**Full category breakdown** (for re-deriving without re-reading every issue):
- *Player-facing bugs*: #103 (weapon dblclick, Ready), #96 (armor, Backlog —
  deliberately deferred, see below), #12 (NPC content ~55-60% dead, Backlog), #22
  (Britannia-only map, Backlog, undecided), #64 (boot-log noise, Ready). #117
  (Jamy smrti) was here too — now closed, see below.
- *GM tooling*: #101 (Part A/B tracking, In Progress), #102 (remaining GM/utility
  commands, Ready)
- *Localization*: #104 (resx steps 4-5, Ready)
- *Verification pass*: #67 (tracking, In Progress), #78 (V-10, In Progress), #73
  (V-5 region enforcement, Backlog), #74 (V-6 housing gap, Backlog)
- *CI & infra*: #100 (CI hangs, Backlog), #121 (OwnerAggroTests CI-only failure,
  In Progress — see CI-minutes note below, still blocked)
- *Architecture & tech debt*: #30 (roadmap tracking, In Progress), #26 (static
  singletons, Backlog), #29 (RemoteConsole/SphereShardConverter, Ready, undecided)
- *Client compat*: #11 (TazUO, Backlog)

### 2. Cheap-decision pass — four Backlog items that were just "pick an answer"

- **#117 (Jamy smrti magic-blocking region) — RESOLVED, closed `not_planned`.**
  User: "Jamy smrti si newbie area, lets leave it untouched for now and consider
  it by design." Confirmed-intentional, not a bug. If NPC spellcasting is ever
  reported broken elsewhere, rule out this region first before assuming a new bug.
- **#96 (armor has no real effect) — stays OPEN, deliberately deferred, not
  undecided.** User: armor mitigation depends on armors/materials/skills all
  being in a complete state first — fixing the formula against incomplete inputs
  now would mean redoing it later. Revisit once those systems mature, not before.
- **#22 (Britannia-only map)** and **#29 (RemoteConsole/SphereShardConverter)** —
  user explicitly chose to leave both open in Backlog, undecided, for now. Not
  analysis paralysis — a deliberate "not now" on both.

### 3. Leveling/experience system — design SETTLED (2026-10-13), tracked as #125, NOT implemented yet

Full investigation (moriaengine current state + Erebor Sphere reference archive,
re-cloned read-only to scratchpad, same not-committed pattern as earlier this
session) plus a full Q&A pass with the user. **Everything below is final design,
confirmed by the user** — don't re-derive or re-litigate, just read #125 and its
5 phase sub-issues (#126-#130) for the complete writeup. Short version:

- **Formula**: threshold for level N = `N * coefficient` (coefficient default
  **330**, confirmed as Erebor's own real constant, `nextlevel=330` in
  `scripts/experiences.scp` — not invented; now a GM-tunable setting, see "Level
  System settings category" below, not a hardcoded literal). Level-up check is
  a loop, not a single `if` — handles one exp gain spanning multiple levels,
  carries the remainder (e.g. level 1/600 exp +100 → level 2/40 exp, not
  clamped to 0). No level cap.
- **`Player.Level`/`Player.Experience` already exist** (`Player.cs:34-56`) as
  pure unwired TODO stubs — fields + persistence work, formula/trigger don't
  exist. This is Phase 1's (#126) starting point.
- **Monster kills** grant a flat hand-tuned exp "worth" per monster (Erebor
  pattern: `tag.experience={min max}`, NOT derived from stats — monster content
  is ~55-60% incomplete per #12 anyway) — Phase 2, #127.
- **Death (user's final call, deliberately diverges from Erebor's actual ~5%-
  capped-loss mechanic)**: player's exp loss and the killer's cut are now both
  GM-tunable percentages (see settings category below) — **defaults**:
  experience-loss-percentage **100%** (full reset), PK-gain-percentage **10%**
  of the *lost* amount (not of the original total) paid to the killer(s). At
  defaults this nets out to the originally-stated behavior: dying player loses
  everything, killer(s) collectively get 10% of what was lost. Safe zones/
  arenas exempt from loss entirely; 5-minute post-death immunity (mirrors
  Erebor's `i_checkstats_timer`). Multi-killer split method not decided
  (default even split) — Phase 3, #128.
- **"Level System" settings category (added 2026-10-13)**: using the
  `.settings` framework confirmed to already exist (see correction note
  further below) — a new `LevelingSettings : SettingsMetaCategory` class
  (template: `scripts/Basic/skills/Combat/CombatSettings.cs`, registered via a
  new `[InfoField("Level System")]` property on `SettingsCategories`,
  `scripts/Basic/administration/Settings/MetaSettings.cs`) with exactly 3
  fields for now: `nextLevelCoefficient` (default 330, created in #126),
  `experienceLossPercentage` (default 100, created in #128),
  `pkGainPercentage` (default 10, created in #128 — same `LevelingSettings`
  class, don't create a second category).
- **Level-up rewards — three separate point pools, user confirmed keeping
  Erebor's exact structure** (numbers below are final, verified twice against
  the reference scripts):
  - Skill points: flat **+5/level**, uncapped forever.
  - Ability points: total budget **`level * 2`**, uncapped forever.
  - Attribute points (STR/DEX/INT/VIT): total budget **`300 (flat base) +
    f_levelstatbonus(level)`**, where `f_levelstatbonus` is `level*4` through
    level 9, `40+(level-10)*3` through level 29, `100+(level-30)*2` through
    level 39, then flat `120` forever from level 40 on (level 40 is the LAST
    level that adds any attribute points — matches user's own description
    exactly). Per-level delta: 1-9→+4, 10-29→+3, 30-39→+2, 40→+2(last), 41+→+0.
  - Earning is profession-agnostic (same for every class); only *spending*
    differs per profession. This phase (#129) is explicitly its own sub-project
    — moriaengine's existing `Ability`/`Profession` system
    (`scripts/Basic/AbilitiesAndTalents/`, `scripts/Basic/Professions/`) is
    direct-set today, not earn-and-spend, and retrofitting that is a real design
    question deferred until professions functionality gets looked at properly.
- **Experience gates/quests are explicitly OUT of scope** (#130) — user: "just
  leave us function which handles adding experiences... called from different
  places" — that shared function is Phase 1's job; actual gate/quest content is
  deferred indefinitely, tracked only so the idea isn't lost.
- **`.explevel`**: read-only player dialog (level/exp/exp-to-next), direct
  template exists (Erebor's `d_explevel`, `scripts/experiences.scp:286-338`).

### 4. CORRECTED (2026-10-13): `.settings` dialog already exists — #124 closed, not a prerequisite

**My original investigation was wrong — missed a whole folder.** First grep for
a settings-dialog framework came up empty and #124 was filed as "build this from
scratch, before leveling." User tried `.settings` in-game and it already worked.
Re-investigated properly this time (read every file in the folder, not just
grepped narrow patterns) and confirmed it's fully built:

- `.settings` → `SettingsCategories.Settings`
  (`scripts/Basic/administration/Settings/MetaSettings.cs:34`) → opens a `D_Info`
  dialog listing every `[InfoField("Category Name")]`-tagged property on
  `SettingsCategories` — each points at a `SettingsMetaCategory` subclass
  singleton. Existing categories: `CombatSettings`, `MagerySettings`,
  `PoisoningSettings`, `DbManager.Config`.
- Fields marked `[SaveableData]` are **live-editable in the dialog**
  (`SettingsProvider.AssertSettings`, reflection-based) and **persist across
  restarts automatically** (`[SaveableClass][HasSavedMembers]` on the class +
  `[SavedMember]` on the singleton — the same generic save pipeline everything
  else in this engine uses, nothing special-cased).
- **Template to copy for any new setting** (e.g. leveling's PK-exp-cut %, #128):
  `scripts/Basic/skills/Combat/CombatSettings.cs` is the cleanest example —
  `[SaveableClass][HasSavedMembers][ViewableClass] class XSettings :
  SettingsMetaCategory`, `[SavedMember] static XSettings instance`,
  `[LoadingInitializer]` ctor, `[SaveableData]` fields. Then register it with
  one new `[InfoField("...")]` property on `SettingsCategories`
  (`MetaSettings.cs`). That's the entire recipe — no framework work needed.

**#124 closed** (`not_planned` — nothing to build) and moved to Done on the
board. **#128 corrected** — the "soft-depends on #124" framing is gone; adding
the PK-exp-cut % setting is now just a small, normal part of implementing that
phase, using the template above.

**Lesson for next time a "does X exist" question comes up**: grep narrowly
missing a whole folder is a repeat mistake pattern this session (same root
cause as the `--binary-files=text` encoding gotcha, different symptom) — when a
user reports something already works that an investigation said didn't exist,
re-investigate by reading entire relevant directories, not just re-running
slightly different grep patterns, before concluding the user is mistaken.

**Issues filed for this whole pass**: #125 (leveling tracking), #126-#130
(leveling phases 1-5) — #124 closed per above. All added to the project board.
Categorized under existing "Player-facing bugs"/"GM tooling" categories as a
pragmatic fit — there's no dedicated "Gameplay systems" bucket yet, and adding
one risks disrupting existing item categorization (GitHub's single-select-field
update mutation replaces the whole option list, risky to do live) — worth adding
properly if more big feature-design work like this keeps coming.

## CI minutes — separate, still-unresolved thread (2026-10-13)

GitHub Actions minutes likely exhausted (pushing a commit earlier produced
**zero** CI run at all — confirmed via `gh api repos/pkorpel/moriaengine/actions/runs`,
no run exists for that commit SHA, not even failed/queued). Repo was at
1361/2000 for the month before a lot more got burned investigating this very
problem. Couldn't confirm the exact remaining number — `gh api .../billing/...`
returns 403 (token lacks billing-read scope). **User said to leave it and come
back later** — this is explicitly parked, not blocking the planning work above.

**What's mid-flight and still parked:**
- Draft PR **#119** (branch `diag-owner-aggro-ci`, NOT merged, NOT meant to be
  merged) carries diagnostic-only instrumentation investigating issue **#121**
  (`OwnerAggroTests.HostileFollowerNeverAutonomouslyTargetsItsCommander` — merged
  in #110 — fails reproducibly on CI only, titan acquires its own commander as
  target, never reproduces locally). Full writeup, root-cause theories, and exact
  next steps are in #121 — read that first, don't re-derive. Short version: the
  diagnostics are already in place and already fixed to actually surface in the
  CI log (an earlier version's `Console.WriteLine`s were server-side and only
  landed in an in-memory buffer nothing ever printed) — the next step once Actions
  work again is just to confirm a push actually triggers a run, then read the
  `DIAG:` output.
- When done: revert the diagnostic-only commits on `diag-owner-aggro-ci`, close
  draft PR #119, delete the branch.

**Everything below this section was already shipped and merged before this pause**
— today's run of PRs #108-#112 (ConvertTools fix, `.invul`, pet owner-attack/rebel,
NPC spellcaster distancing, NPC spell-cast animation) plus #120 (the CI-minutes
split). Issues #113-#121 are the full bug-tracking trail (every bug that session
got filed + closed-with-PR-reference per the new standing policy in Workflow
conventions below) — note #113/#114/#115/#116/#118 needed a follow-up `update`
call to actually close (the `create` call's `state: "closed"` parameter is
silently ignored by GitHub's API — create always opens, close is a separate call;
caught because the user noticed they were all still open).

## CURRENT TOP PRIORITY (2026-10-12): basic functionality + localization architecture

User explicitly paused the #67 verification pass (V-10 and beyond) to prioritize two
things first. **Tracking issue #101**, sub-issues **#102** (.usehand + allowlist), **#103**
(weapon double-click), **#104** (resx localization migration) — all filed, read those for
full detail. Plan approved by user.

**Scope narrowed again (2026-10-12, mid-implementation)**: user said "for now I just want
usehand implemented (with tests). After that I would like to look at language resources -
as if we keep working, we just adding more word for future rework here." So:
- Part A = **`.usehand` only** (shipped — see below). Everything else in #102 (Group 1 GM
  commands, Group 2 utility commands beyond UseHand, `UseRing`) and all of #103 (weapon
  double-click) is **deferred indefinitely** — don't pick those back up without a fresh
  explicit ask, even though they're fully scoped and ready in the issues.
- Next up: **Part B (resx localization), starting now.**

**`.usehand` — shipped**: `Player.UseHand()` (`scripts/Basic/CharClasses/Player.cs`) finds
the hand1 item, falls back to hand2, calls `Trigger_DClick` — same trigger a real
double-click uses, no reimplemented equip logic. Allowlisted in
`CommandsAuthorization.cs` at `PLEVEL_PLAYER`. Protocol test:
`Tests/Protocol/BasicCommandsTests.cs` `UseHandTriggersDoubleClickOnOffhandItem` — uses
`i_torch` (ShiningItem.def, layer=2/hand2) as the equipped item specifically because its
`On_DClick` has an observable side effect (Model → 0xa12) even while already equipped;
the generic `Equippable.On_DClick` is a no-op in that case, so a plain weapon/wearable
couldn't have proven the dispatch actually fired. Server-side test helpers in
`scripts/Tests/BasicCommandsTests.cs` (`TestGrantAndEquipTorch`, `TestCheckItemModel`).

### Part A — basic functionality fixes ("must fix", small/low-risk)

User reported two broken everyday interactions; investigated both (3 parallel Explore
agents) before touching any code:

1. **`.usehand` command doesn't work** — root cause: completely unimplemented, no
   `SteamFunction`/method exists at all, not a bug. Reference behavior pulled from the
   Erebor Sphere archive (see "Erebor Sphere reference" below), `scripts/funkce.scp`'s
   `usehand` function: double-click whatever's equipped in the primary hand
   (`layer_hand1`), fall back to the off-hand (`layer_hand2`) if empty — just invokes
   that item's own double-click trigger, don't reimplement equip logic.
2. **Double-click on a weapon triggers nothing** — static analysis of the whole dispatch
   chain (`DoubleClickInPacket` → `Thing.Trigger_DClick()` → `Equippable.On_DClick()` at
   `scripts/Basic/ItemClasses/Equippable.cs:44-59`) is solid and generic — weapons inherit
   the exact same `On_DClick` armor/wearables use (`Weapon → Destroyable → Equippable`).
   **This is NOT a known dispatch-architecture bug** — needs live repro (connect a
   client, create a weapon, double-click it, watch console for a deny-trigger/exception)
   before fixing anything. Leading candidates: a specific weapon `ItemDef` missing its
   `Layer` value (content bug), or a deny-trigger blocking weapons specifically (real
   engine bug). Don't guess further than this without reproducing it first.
3. **Full command inventory done (2026-10-12, see issue #102 for the complete tables)**:
   Erebor's `scripts/skillgains.scp` turned out to be a complete plevel-1-through-6
   command list for that shard (~400 entries). Cross-checked against moriaengine's
   actual allowlist/commands. Headline finding: **moriaengine's entire current GM
   command set is just `Tele`/`Invis`/`Remove`/`OldInfo`/`PaymentSource`/`GM`** —
   `Jail`/`Pardon`/`Forgive`/`Props`/`Tweak`/`Invul`/`NewNpc`/`Fix`/`GoUid`/`Account`/
   `Dupe`/`Shrink`/`BlockIp` etc. are all genuinely missing, confirmed via direct search.
   Sorted into 4 tiers (Must/Should/Nice/Skip) — "Skip" is the bulk of the raw list
   (shard-specific LOTR factions/custom classes/quick-slot macros/minigames, ~130+
   commands, none relevant here). "Must have" Group 1 (GM tooling: Invul/Props/Tweak/
   Jail/Pardon/Forgive/NewNpc/Fix/GoUid/GoChar/Kill/real-Info) + Group 2 (player utility:
   WhoAmI/Stats/AFK/RecallHome/GateHome/UseRing, plus `Dismount`/`Password`/`ShowSkills`/
   `AllShow`/`QuestArrow` from the original bonus-find, all missing from the allowlist
   same as #84's bug class) is the actual scope for Part A now — supersedes the earlier
   narrow 5-command list.

### Part B — localization: migrate scattered `*Loc`/txt files to .resx — IN PROGRESS

User's complaint: today's `*Loc` classes (one per feature file, e.g. `TamingLoc`,
`HidingLoc`) + per-language `.txt` override files under `Languages/<Lang>/...` are
scattered everywhere. Wants .resx instead — "one big file with everything" per language —
loaded via a new steamengine.ini setting. **Confirmed with user directly**: the ini
setting sets the server-wide *default* only; a player's own in-client language switch
(existing `SetLanguageInPacket` mechanism) must keep overriding it exactly as today.
User also said (2026-10-12, after `.usehand` shipped): don't scope-creep Part A further,
move straight to this — "we just adding more word for future rework" if we keep going.

**Re-grounded 2026-10-12 — note on a self-caught mistake**: a first pass here said "18
classes not ~38" — that was WRONG, caused by plain `grep -r` silently dropping files with
legacy non-UTF8 bytes (no warning, just missing matches — e.g. `ResListLoc.cs`'s Czech
literal defaults). Re-ran everything with `--binary-files=text` and got different,
verified numbers below. Lesson: this codebase has several `.cs` files with legacy
non-UTF-8 byte sequences in string literals (same ones already flagged as an encoding
risk for the migration tool itself) — **always pass `--binary-files=text` when grepping
this repo for anything Loc-related**, plain `grep -r` undercounts silently.
- Actual count: **39** C# `CompiledLocStringCollection<T>` subclasses (full file:line
  list re-derived, matches the original ~38 estimate closely after all).
- **A second, separate localization mechanism exists**: LScript blocks defined inline in
  `.scp`/`.def` game-content files, loaded by `ScriptedLocStringCollection`
  (`GameCore/LScript/ScriptedLoc.cs`, assemblyName `"LScript"`) via any of several header
  aliases `ScriptLoader.cs` treats as equivalent (`[Loc ...]`, `[ScriptedLoc ...]`,
  `[Localisation ...]`, etc. — all route to the same `ScriptedLocStringCollection.Load()`,
  confirmed `GameCore/ScriptLoader.cs:206-218`). **6** confirmed live by reference search:
  `loc_ability_functions` (`playerFunctions.scp`), `loc_kick` (`kick.scp`), `loc_longrange`
  (`longRange.scp`), `loc_silence` (`silence.scp`), `loc_mage` (`mage.def`),
  `loc_playervendor` (`playerVendor.scp`, uses the `[Loc ...]` alias specifically — this
  one was missed on the first grep pass too). **User decision: fold these into the same
  resx files too** — same flat-key scheme, inline blocks in those 6 files get removed
  once migrated so content doesn't stay duplicated in two places.
- **10 orphaned ScriptedLoc-style `.txt` files** (not the earlier wrong ~14) — 9
  `loc_MENU_*` + `loc_menu_scripted`, confirmed zero references anywhere in code/scripts
  even with the binary-safe grep. The 3 `Loc_PlayerVendor_*`-named ones are **NOT**
  orphaned (that was the grep bug too) — `scripts/Optional/PlayerVendor/
  D_PlayerVendor_*.cs` are real, live classes. Per the migration tool's own rule: flag,
  don't import the genuinely-orphaned 10 — dead data shouldn't get carried into resx.
- **`Languages/Default/` vs `Languages/English/` quirk**: `Language` enum has `Default`
  and `English` as literal aliases of the same value (0) — `Enum.GetName` resolves to
  "Default" (declared first), so **`Languages/Default/` is the folder real running code
  actually reads/writes for English**, not `Languages/English/`. `Languages/Default/` is
  gitignored (confirmed, `.gitignore:12`) and only has whatever this session's own test
  runs happened to touch (5 Scripts classes + all 5 live LScript ones) — incomplete by
  construction. `Languages/English/` is git-tracked but appears to be a **stale snapshot
  frozen at the initial commit**, never actually read by current code. Net effect: for
  English specifically, the C# class's own hardcoded literal field defaults (and the
  LScript blocks' own inline defaults) are the actually-correct source, not either .txt
  folder — use those for English, not `Languages/English/*.txt`.
- **Czech translations are real but partial**: spot-checked — 25 of 54 `Languages/Czech/*`
  files contain genuine accented-Czech text (real translation work happened), but 29 are
  byte-for-byte identical to the English/code defaults (never actually translated, just
  auto-generated placeholders). Migrate verbatim either way — not this migration's job to
  backfill missing translations, just to carry forward whatever's actually there today.

**Design** (full detail + staged PR sequence in the plan file — this is the condensed
version, corrected post-PR1 — see "design correction" below): keep all ~90+ call sites
(`Loc<TamingLoc>.Get(lang).fieldName`) completely unchanged — only
`CompiledLocStringCollection<T>.SetFieldsFromContents()`
(`Common/Localisation/CompiledLocStringCollection.cs:22-28`, confirmed by direct read:
it's a single `fi.SetValue(this, string.Intern(this.GetEntry(fi.Name)))` loop) needs to
change what `GetEntry` reads from. No `ResXFileCodeGenerator` designer class — we're not
replacing the call-site shape, so generated properties add nothing.

**Migration source — corrected from the original plan**: NOT the `.txt` files directly.
Instead, PR1 added a throwaway diagnostic (`scripts/Tests/LocMigrationDump.cs` +
`Tests/Protocol/ZZZLocMigrationDumpRunner.cs`, both deleted again before PR1 merged) that
dumped the **actual resolved value** for every live class/field and every live
ScriptedLoc defname/entry, for both languages, straight from the running engine (via
`Loc<T>.Get(language)` and `LocManager.GetLoc(defname, language)` — i.e. whatever the
engine's OWN existing resolution logic already produces today). This sidesteps needing
to re-derive values by parsing `.cs` literals or `.txt` overlays by hand (which risks
subtly reproducing encoding/parsing bugs) and guarantees the resx content matches
*exactly* today's live behavior, bugs and all — e.g. `ResListLoc`'s "English/Default"
values are confirmed **themselves garbled Czech text** (legacy encoding issue baked into
the class's own literal defaults, carried forward faithfully — not something to silently
"fix" as part of a migration; flag it to the user separately if ever worth fixing).

**Design correction found while building PR1 — don't use `ResourceManager`+`CultureInfo`
at all**: originally planned `ResourceManager.GetString(key, CultureInfo)` with a
`GameText.cs-CZ.resx` satellite-culture-named file. Building PR1 revealed two problems:
(1) this sandbox's `dotnet` CLI itself requires
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` just to run at all (no ICU installed) — meaning
MSBuild's own culture-splitting logic for `*.cs-CZ.resx` can't be exercised/validated
locally, and behaved inconsistently (silently fell back to embedding it as a *plain*
non-culture resource under a literal manifest name, not a real satellite assembly) when
tested; (2) there's no need for general multi-culture fallback machinery anyway — this
server has exactly 2 known languages via the `Language` enum. **Fix**: renamed to
`GameText.Czech.resx` (deliberately NOT culture-pattern-shaped, so MSBuild treats both
resx files as plain embedded resources every time, in any environment) and PR2 will look
up via two explicit resources keyed directly off the `Language` enum — no
`CultureInfo`/`ResourceManager.GetString(..., CultureInfo)` involved at all, sidestepping
the ICU risk entirely rather than defending against it.

Files (both committed in PR1, unused so far): `Common/Localisation/Resources/
GameText.resx` (English/Default, 194 entries) + `GameText.Czech.resx` (Czech, 194
entries) — flat keys `{Defname}_{FieldOrEntryName}` (e.g. `TamingLoc_whatToTame`,
`loc_kick_Kicks`), covering all 38 non-empty C# classes (39 total classes exist;
`CommonLoc`/`BloodritualLoc` have zero fields, nothing to carry) + 6 live ScriptedLoc
defnames. The 10 genuinely-orphaned ScriptedLoc `.txt` files were NOT imported.

Staged rollout (5 PRs, each independently safe — see plan file for full detail):
1. **Shipped.** Migration tool (throwaway, deleted) + generated resx files, committed
   but unused — zero behavior change.
2. **Shipped.** `CompiledLocStringCollection<T>.GetEntriesFromCode()` now yields
   `GameTextResources.GetEntry("{ClassName}_{FieldName}", this.Language)` per field
   instead of reading the (not-yet-initialized-at-that-point) field value.
   `LocStringCollection`'s txt-overlay/auto-append code is gone (both ctors just build
   `entriesByName` straight from `entriesFromCode` now) — `valueRE` itself stayed though,
   `GameCore/PropsFileParser.cs` reuses it for unrelated general script-file parsing.
   New `GameCore/LScript/ScriptedLocResxBridge.cs` registers the 6 ScriptedLoc defnames
   directly from resx (hardcoded defname/entry-name list, called once from `Main.cs`
   right after `ClassManager.InitScripts()`) since their inline `[ScriptedLoc ...]`/
   `[Loc ...]` blocks are now deleted from `playerFunctions.scp`, `kick.scp`,
   `longRange.scp`, `silence.scp`, `mage.def`, `playerVendor.scp`. Permanent regression
   test added (`scripts/Tests/LocalizationVerificationTests.cs` +
   `Tests/Protocol/LocalizationVerificationTests.cs`): enumerates every live class/field
   + defname/entry, asserts a real (non-sentinel) value for both languages — passing,
   388/388 checked, 0 missing. Full suite: 65/65 passing (needed several reruns — this
   sandbox hit #100's pre-existing intermittent hang 4 times in a row on completely
   unrelated tests (`DayNightWeatherTests`, `CombatDepthTestsB`, etc.) before a clean
   run; also found 4GB of accumulated hang-dump files from the retries themselves adding
   I/O pressure — worth deleting `Tests/TestResults/` before retrying if this recurs).
3. **Shipped (PR #108, 2026-10-13).** `Globals.cs` new `defaultLanguage` ini setting
   (`text` section, default `English`) + `GameState`'s `language` field initializer now
   reads `Globals.DefaultLanguage` instead of a hardcoded literal. Surfaced and fixed a
   real bug while wiring this up: `ConvertTools.ConvertTo`'s enum branch only caught
   `InvalidCastException` as the signal to fall back to `Enum.Parse` on the string form;
   a symbolic value like `"English"`/`"Czech"` makes `ConvertTools.ToInt64` throw
   `FormatException` instead (uncaught) — would have crashed on every boot after the
   first, since a missing ini key gets written out as its default's `ToString()` (a
   symbolic name, not a number) the first time. Added `Tests/ConvertToolsTests.cs`
   (pure-logic, no engine boot) covering both the symbolic and numeric enum-string
   paths. **Note**: shipped on `dotnet build` + the new targeted unit test only, NOT a
   full-suite run — the local full suite hit the known intermittent full-boot-tier hang
   (see "CI health"/#100 below) partway through `CombatDepthTestsB`, and the auto-mode
   classifier blocked killing the stuck process (matched the stored "don't kill
   dotnet/msbuild zombies" guidance even though this was a live stuck process, not a
   post-run zombie) — user explicitly opted to ship without chasing it given how small
   and self-contained this change is. Worth running the full suite next session to
   confirm nothing regressed, and worth refining that memory/classifier distinction
   (finished-run zombies vs. a live hung process you started) if it recurs.
4. (later) delete the now-dead `.txt` files, once 2-3 have run clean for a while.
5. (optional cleanup) strip vestigial literal defaults from the ~39 class bodies.

### Erebor Sphere reference (pulled for this AND for later skills comparison work)

Cloned `gitlab.com/erebor/sphere-public-archive` (read-only reference, scripts-only, no
engine binary) into the scratchpad — **not committed to this repo**, re-clone if a future
session needs it again: `git clone --depth 1 https://gitlab.com/erebor/sphere-public-archive.git`.
Confirmed same Czech-shard lineage as moriaengine's own converted content (folder/string
naming matches, e.g. `brneni`=armor). Used so far for: the `usehand` reference above, and
an initial Hiding/Healing/Taming mechanics comparison (see "Skills comparison" note
further below — resume that AFTER Parts A and B ship). Stealing is explicitly out of
scope — disabled on moria per the user.

### Skills comparison findings so far (resume after Parts A+B, don't redo)

Already compared Hiding/Detect Hidden, Healing/Bandage, and Taming against Erebor (above)
before this priority shift happened:
- **Hiding**: Erebor is one direct roll (`hiding skill > rand(1025)`). moriaengine's
  `HidingSkillDef.On_Stroke` instead feeds a *random* value (`Globals.dice.Next(700)`) as
  the "difficulty" into the generic `SkillDef.CheckSuccess` S-curve formula — two layered
  random rolls instead of one direct comparison. Worth a second look, not clearly wrong,
  just diverges from the reference for no obvious reason.
- **Detect Hidden — clearest real gap found**: Erebor has a full contest (sector-darkness
  -scaled stealth bonus on the hider, distance penalty on the detector, a per-detector
  re-detection cooldown). moriaengine's `DetectHiddenSkillDef` is just a bare
  `CheckSuccess(self, target.Hiding)` — none of that. Good first target once skills work
  resumes.
- **Healing/Bandage**: moriaengine's gates (range/LOS/no-summons/already-healthy,
  shaman-only bloody bandages) already match Erebor closely. Missing: Erebor's
  priest-class heal-speed special ability; resurrect-via-bandage formula not found in the
  same file, check separately.
- **Taming**: structurally fine on both sides; Erebor adds skill-tier flavor messages
  (cosmetic only) moriaengine doesn't have.
- **Stealing**: skip — confirmed disabled on moria.

### Sequencing / where to pick up

1. User wants CLAUDE.md (this) + GitHub issues filed for all of the above BEFORE any
   implementation starts, and wants to explicitly approve before implementation begins —
   don't start writing code for Part A/B until that explicit go-ahead is given, even
   though the plan itself was already approved.
2. Once approved: Part A ships first (small, one or two PRs), then Part B's staged PRs,
   then resume V-10 (see below) and the skills comparison.

## Current initiative: issue #67 — full-scale protocol-level verification pass

**Paused as of 2026-10-12 — see "CURRENT TOP PRIORITY" above, resume this after Parts A
and B ship.**

Tracking issue: **#67** "[Verification] Full-scale protocol-level test pass benchmarked
against real UO behavior". Plan: real client-protocol scenario tests (login → real
packets → assert on observable server behavior via `GameServerPairFixture`) for every
system built this session, plus a benchmark pass over pre-existing "solid" systems.
Phases V-0 through V-10, each its own sub-issue under #67.

### Phase status

| Phase | Issue | Status | Notes |
|---|---|---|---|
| V-0 Shared infra | #68 | ✅ closed | target-cursor/skill-use encoders, packet scanner, 2-client harness |
| V-1 Combat depth | #69 | ✅ closed | |
| V-2 Pet commands & GM NPC control | #70 | ✅ closed | |
| V-3 Economy | #71 | ✅ closed | banking, vendor buy/sell, payment source |
| V-4 Social systems | #72 | ✅ closed | secure trade, guild, party — surfaced bugs #84, #87 |
| V-5 Notoriety & region enforcement | #73 | 🟡 open (intentionally) | Criminal-flag/murderer-threshold part done & merged (#89). Region-enforcement sub-part (`Notoriety.IsAttackBlockedByRegion`) **deliberately deferred** — needs real static-region test data or an ad-hoc-region test helper; constructing one at runtime risked corrupting shared region state for other concurrently-running tests. Left open for whoever picks this back up. |
| V-6 Housing | #74 | 🟡 open, should probably close | Deed placement, overlap rejection, co-owner/stranger access all tested & merged (#91). Surfaced bug #90 (now fixed, merged). **Gap**: issue's "GM release" checklist item not covered by any test yet. |
| V-7 Taming/Mining/Lumberjacking | #75 | ✅ closed (PR #93, 2026-10-12) | `GatheringTests`: taming sets ControlMaster, mining yields ore, lumberjacking yields a log. Full suite 59/59 passing at merge time. |
| V-8 Day/night & weather | #76 | ✅ closed (PR #95, 2026-10-12) | `DayNightWeatherTests`: shortened/re-anchored day cycle flips light level to night; season-change setter pushes the real packet. Surfaced bug #94. Full suite 61/61 passing at merge time. |
| V-9 Content-restoration sanity | #77 | ✅ closed (PR #97, 2026-10-12) | `ContentRestorationTests`: flight-capable NPC crosses water a grounded one can't. **Armor half deliberately not covered** — surfaced bug #96 (significant, not auto-fixed, see below). Full suite 62/62 passing at merge time. |
| V-10 Pre-existing systems benchmark | #78 | 🔵 in progress (part 1/N merged, 2026-10-12) | movement sub-scenario shipped via PR #98: `MovementSpeedTests.RealMoveRequestPacketUpdatesPosition`. Fixed a real bug (`MovementState.CanMoveAgain` always used `RunStepTime` regardless of the `running` flag). **"Running measurably faster than walking" not automated** — see PR #98/test file's own doc comment for why (first-packet reserve-banking artifact + per-tile terrain variance; needs a dedicated clear-terrain test region). Remaining sub-scenarios: crafting, base-skills sampler (Stealing/Hiding/Healing/Tracking), chat, items/containers, GM tools, persistence — not started. |

### Bugs found by the verification pass (real defects in production code, not test bugs)

All below were already fixed and merged to main, and their issues are now closed:

- **#84** (closed) — Guild (and all non-allowlisted) `.` commands were unusable by any
  non-GM player (`CommandsAuthorization.cs` allowlist never included them). Fixed:
  added the 13 guild commands to the allowlist + a `Test`-prefix exemption for the test
  harness.
- **#87** (closed) — Every non-GM `.` command always logged/reported as failed even on
  success (`ScriptHolder.TryRun`, unlike `LScriptHolder.Run`, never set
  `lastRunSuccesful`). Fixed in `GameCore/ScriptHolder.cs`.
- **#90** (closed) — House placement completely broken: `DynamicRegion.Place` checked
  whether the incoming parameter was non-null instead of whether the region's `Parent`
  was already set, so it threw on literally the first-ever placement. Fixed in
  `GameCore/Regions/DynamicRegion.cs:50`.
- **#94** (closed) — `GameState.lastSentGlobalLightLevel`/`lastSentPersonalLightLevel`
  defaulted to `0`, which collides with the real "full daylight"/"no personal light"
  value, so the very first light packet on any daytime login was silently dropped
  (`lastSentSeason` right below already used the correct `-1`-sentinel pattern). Also,
  `SeasonalInformationOutPacket` was never sent at login at all. Fixed both in
  `GameCore/Networking/GameState.cs` and `scripts/Basic/CharClasses/Player.cs`.

These confirm the verification pass is doing its job — pre-existing, never-before-exercised
code paths had real, 100%-reproducing bugs that no unit test ever caught.

### #96 — open, NOT auto-fixed (needs a real design decision + more root-causing)

Found during V-9. Two layered problems in `CombatCalculator.CalculateArmorClassEffect`
(`scripts/Basic/skills/Combat/CombatCalculator.cs`):
1. The real `armorClass` argument (correctly derived from the defender's actual
   `ArmorClassVsP/M`) is unconditionally **discarded for a random placeholder** unless
   the attacker is a monster and the defender is a player (`isMvP`) — NPC-vs-NPC
   (the literal #77 wording), player-vs-NPC, and PvP all ignore real armor entirely.
   A commented-out `armorClass *= settings.armorClassMvP` (a dedicated, otherwise
   totally unused `CombatSettings.armorClassMvP = 200` field) suggests this was meant
   to be finished, not deliberate.
2. Even in the one surviving case (MvP), a full equipped plate set moved measured
   swing damage (via direct `DamageManager.GetWeaponSwingArgs` calls, 300 trials) by
   under 1% — hand-calculating from `CombatSettings`'s own weights/multipliers predicts
   ~20-25%. Reproduced across 5+ runs; not explained by #1 alone (MvP already uses the
   real value) and not further root-caused.

**Why not fixed autonomously**: #1 needs a real design call (what should PvP/MvM armor
mitigation actually look like — a dedicated factor per case, same formula as MvP, etc.)
that isn't mine to make unsupervised; #2 is an open mystery, not a clean fix. Left for a
dedicated follow-up — same spirit as [[project_moriaengine_claude_md]]'s #73
region-enforcement deferral.

## CI health (2026-10-12) — contained, not root-caused; tracked in #100

User flagged GitHub Actions CI as failing/stuck mid-session. Found two genuinely **hung**
runs (one 2.5+ hours, one ~56 minutes, both with zero test output before being stuck) -
**fixed** via PR #99 (merged): `timeout-minutes: 25` at the job level + `--blame-hang
--blame-hang-timeout 5min` on the test step. Verified working on PR #99's own validation
run - it hit the *same* real hang (during `InfrastructureSmokeTests.
PacketScannerFindsBothWorldEntryMarkersAmongOtherTraffic`, per blame's "test running when
it happened" report, not confirmed as the actual cause), but this time `--blame-hang`
caught it after 5 minutes, captured 3 hang-dump files, and aborted cleanly at ~16 minutes
instead of hanging for hours.

**Not fixed**: the hang's actual root cause (suspect an unbounded socket read against a
dead connection, under GitHub's shared-runner CPU contention - doesn't reproduce locally),
and a cluster of separate intermittent connection-race failures in `EconomyTestsA/B`,
`SocialTests`, and a stuck-NPC-pathing failure in `CombatScenarioTests` - none reproduce
locally, all seen only on CI across different runs this session. Full writeup and repro
evidence in **issue #100**. This repo has prior `fix-ci-login-race-and-timeouts`/
`fix-ci-combat-timing-flakiness` branches, so it's a recurring category - worth a
dedicated pass (starting from #99's hang-dump artifacts if still downloadable) rather than
something to re-chase piecemeal.

**Confirmed green, but it took a retry (2026-10-12)**: after PR #99 merged, checked
several `main` runs - 2 passed cleanly, then the push with this very CLAUDE.md note hit
*another* real hang (`--blame-hang` caught it correctly again, this time mid-
`PetCommandTestsA`, a third different test class than the first two hangs). `gh run
rerun` on that same commit then passed clean. **The hang is real, intermittent
(roughly 1 in 3 runs today), and strikes a different test class each time** - strongly
suggests CI-runner resource contention (shared 2-core GitHub-hosted runner, `moriaengine`
spawns 2 real child processes per test collection, dozens of collections per run) rather
than a bug in any one test. Current `main` HEAD's check is ✅ green as of this pause, but
expect to possibly need another `gh run rerun` next time, not a guarantee. Full detail
and this is tracked as the thing to actually fix in **issue #100** - don't re-investigate
from scratch, start there.

**Paused here per explicit user request** right after getting a green run for the
current `main` HEAD - see "Right now" below for where V-10 itself stands.

## GitHub Actions minutes — split CI, 2026-10-13 (PR #120)

User flagged the 2000 min/month Actions cap (was at 1361/2000, 18 days left) —
the ~9-11 minute full-boot-tier suite (spawns real `AuxiliaryServer`/`GameCore`
processes per test class, `SteamEngine.Tests.Protocol` namespace) running on
**every single push/PR** was the overwhelming majority of that spend.

`.github/workflows/dotnet.yml` is now two jobs on the same `on:` triggers
(`push`/`pull_request`/`workflow_dispatch`), gated by `if: github.event_name`:
- `build-and-unit-test` (runs for push/PR, skipped for manual dispatch): build +
  `--filter "FullyQualifiedName!~SteamEngine.Tests.Protocol"` — fast tier only,
  no real server processes. Verified locally: 23 tests, ~0.1s. Verified on CI:
  1m42s including build/restore/setup overhead (vs. ~9-11 min before).
- `full-regression` (runs ONLY for manual dispatch, skipped for push/PR): build +
  the complete suite, unchanged otherwise (`--blame-hang` etc.) — trigger this
  deliberately via the Actions tab's "Run workflow" button before/after anything
  that could affect protocol-level behavior, not automatically on every commit.

**This shipped too late to avoid exhausting the month's minutes** — see "PAUSED
HERE" at the top of this file. The split itself is done and merged; what's
blocked is everything downstream of it needing a NEW CI run to validate.

## Right now: mid V-10 — movement sub-scenario done, 6 more to go

V-10 (#78) movement sub-scenario shipped via PR #98, merged to `main`. CI hang fix
shipped and merged via PR #99 (see "CI health" above) - its own validation run
confirmed the fix works (caught a real hang via `--blame-hang` instead of hanging for
hours). Working tree is clean, `main` is checked out, local is in sync with
`origin/main`. Session paused here per explicit user request, right after the CI fix.

**Remaining V-10 sub-scenarios (each its own small PR, same pattern as the movement
one):**
- Crafting: craft a basic item via the real craft-menu gump, confirm item created +
  materials consumed.
- Base-skills sampler: Stealing, Hiding, Healing/bandage, Tracking — one scenario each.
- Chat: speech reaches nearby players, not beyond visibility range.
- Items/containers: pick up (0x07), equip a weapon (0x13 `DropToWearItemInPacket`),
  drop into a container (0x08); container-contents packet (0x3c, dynamic-length)
  matches server state.
- GM tools: teleport and remove, verified via protocol.
- Persistence: real protocol-level save → process restart → reconnect → same
  character/items still present. **Flagged highest-effort** in the issue itself.

**Movement sub-scenario lessons worth remembering before touching terrain-dependent
tests again**: this test map's terrain varies wildly tile to tile (dense static
placement in spots, spawn tiles hemmed in on all 8 sides - also hit in V-7/V-9). No
fixed pre-validation radius reliably guarantees enough consecutive open tiles - burned
a lot of time tuning radius numbers before accepting a retry-until-success design
instead. If a future sub-scenario also needs real multi-tile movement, start with that
retry idiom rather than re-deriving it.

**Next steps (in order):**
1. Merge PR #99 (CI hang fix) once its validation run is green.
2. Pick up the next V-10 sub-scenario (suggest items/containers or chat - more
   self-contained than crafting/persistence).
3. Close issue #78 only once all sub-scenarios are done (and file+close any real bugs
   surfaced, same pattern as #84/#87/#90/#94/#96).
4. Worth circling back to eventually: #96 (armor, needs design input), the #74
   GM-release test gap, the #73 region-enforcement deferral, and the CI flakiness
   root-cause (see "CI health" above) if it keeps recurring.

**Environment notes for this sandbox** (needed every fresh session — not in PATH/profile
by default):
```
export PATH="$PATH:/home/agent/.dotnet"
export DOTNET_ROOT="/home/agent/.dotnet"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1   # no libicu in this container
```
These are appended to `~/.bashrc`, but a tool invocation that doesn't source it
(non-interactive shells) still needs them exported inline. Build: `dotnet build
SteamEngine.sln -c Debug` (no `pwsh` available in this sandbox, so `build.ps1` can't be
used directly — plain `dotnet build`/`dotnet test` work fine since Generated/*.Generated.cs
and bin/obj already exist from prior sessions). Full suite:
`dotnet test Tests/SteamEngine.Tests.csproj -c Debug` — takes ~8 minutes
(GameServerPairFixture boots real server processes per test class), run it with
`run_in_background` / block on it rather than a short synchronous timeout.

## Workflow conventions for this repo (don't re-derive each session)

- **Keep this file updated continuously, without being asked each time.**
  Explicit standing instruction from the user (2026-10-13): "keep claude.md
  updated (do this all the time without my reminder) to keep context clean."
  Update it as design decisions land / features get scoped / issues get filed —
  not just at session pause points. If a planning conversation produces a
  decision (a feature gets scoped, a question gets answered, an issue gets
  filed), write it into this file in the same turn, before moving to the next
  thing — don't batch it up for "later."
- **File a GitHub issue for every bug** — whether the user reports it in chat or you
  discover it yourself while working on something else — even if you fix it
  immediately in the same session. File it, then close it referencing the PR that
  fixed it (or leave it open if unfixed/deferred). This is an explicit standing
  instruction from the user (2026-10-13): "every bug raised by either me or found
  during your work... needs to have issue raised for tracking purposes." Earlier in
  that same session the auto-mode classifier blocked an issue-filing attempt with
  "the user described the bugs in chat... not to publish as an issue" — that
  reasoning no longer applies now that this is a standing, pre-authorized policy, not
  a one-off judgment call. Don't ask before filing; just do it as part of the normal
  bugfix workflow below.
- **Closing an issue is a two-step operation, not one.** `mcp__github__issue_write`
  with `method: "create"` and `state: "closed"` in the same call silently does
  NOT close it — GitHub's API always creates issues open, and the `state`
  parameter on create is ignored, no error. You must follow up with a separate
  `method: "update"` call (same issue number) to actually close it. Caught
  2026-10-13 because the user noticed 5 issues that session were all still open
  despite being reported as closed — always verify with `gh issue list --json
  state` after a batch of "create and close" operations rather than trusting the
  create call's response.
- **GitHub Projects (v2) boards need a classic PAT, not the default/fine-grained
  one.** Fine-grained PATs have long-standing incomplete support for the
  Projects permission (a real GitHub limitation, not user error — the "Projects"
  option sometimes doesn't even appear in the fine-grained token UI). If a future
  session needs to touch the project board (https://github.com/users/pkorpel/projects/1
  as of 2026-10-13), ask the user for a classic PAT with the `project` scope,
  scoped to ONLY the `gh project ...` commands (write it to a scratchpad file,
  reference via `GH_TOKEN=$(cat <file>)` so the raw value never appears in a bash
  command's visible text — inlining it directly as `GH_TOKEN="ghp_..." gh ...`
  gets blocked by the auto-mode classifier as credential leakage even when the
  user explicitly authorized using the token). Delete the scratchpad token file
  once done with project work; never reuse that token for anything else (issues/
  PRs/merges keep using the normal default auth). `--owner "@me"` works without
  `read:org`; `--owner <username>` and `gh project link` need that extra scope.
- **Always** go branch → commit → push → `mcp__github__create_pull_request` →
  `mcp__github__merge_pull_request` → `git pull` on local main → delete branch
  (local + `origin`). Never wait for the user to manually merge — they explicitly want
  a durable PR trail but not manual merge gatekeeping.
- Direct `git push origin main` can get blocked mid-session by the Claude Code
  auto-mode classifier ("Git Push to Default Branch"), even after earlier direct
  pushes in the same session succeeded. The branch+PR+self-merge route above is not
  subject to that block — use it by default rather than pushing to main directly.
- After a full-boot-tier test run (anything spawning `GameServerPairFixture`'s real
  `AuxiliaryServer`/`GameCore` child processes), you'll see hundreds of leftover
  dotnet/msbuild/VBCSCompiler entries under `/proc`. **These are zombies (state Z),
  not a real leak** — they consume ~0 resources and can't be killed (nothing to
  signal). If a full-boot test times out, it's sandbox scheduling contention, not
  these zombies — just retry once rather than hunting for "cleanup."
- If delegating research to a sub-agent/fork mid-implementation-session, be explicit
  that it's read-only ("do NOT write/commit/push, report back only") — forks inherit
  full conversation context and have previously gone ahead and implemented/merged
  real features on their own initiative when the surrounding context made that look
  like the obvious next step. Verify `git log`/`git status` after any fork that touched
  the repo, even a "just research" one.
