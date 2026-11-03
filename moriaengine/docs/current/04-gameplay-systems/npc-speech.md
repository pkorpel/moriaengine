# NPC speech

What NPCs do when a player says something near them: Erebor's `[SPEECH]` blocks, how they're attached,
how this engine runs them, and the placeholders that stand in for the unported ones (#825).

## How Sphere does it

A speech block is a list of `ON=` patterns, each followed by Sphere script. Consecutive `ON=` lines share
the body under the last one:

```
[speech spk_healer]
on=*res*
if (src.flag_dead)
  src.resurrect_player(<uid>)
endif
```

- **Patterns** match the whole utterance, case-insensitively: `*` is any run of characters and `?` is one
  character, so `*res*` matches anything containing "res". `SRC` in the body is the speaker.
- **Assignment**: `TSPEECH=` on a chardef (Erebor: 464 uses) or an itemdef (24, all ships), and `SPEECH=` on a
  single character (5). Sphere also takes a comma-separated list (`TSPEECH = spk_healer,spk_shopkeep`).
- **Order** in Sphere's `NPC_OnHear`: pet commands, the character's own `SPEECH` list, the chardef's `TSPEECH`
  list, a built-in healer reaction, then `@NPCHearUnknown`. Inside a block, a matched body that ends without a
  return or with `return 1` handles the speech: nothing else runs. `return 0` keeps looking at the block's later
  `ON=` lines.

The archive has 252 blocks with 2,061 patterns and about 3,250 lines of script. Most are one-off quest NPCs
(210 of the names are used by a single chardef) and 159 blocks call Erebor `f_*` quest functions, so the
script bodies can't be converted mechanically.

## How the engine runs speech

- `AbstractSpeech` (GameCore) is a TriggerGroup that answers the `hear` trigger. `TSPEECH`/`SPEECH` lines
  resolve exactly like `TEVENTS` (`AbstractDefTriggerGroupHolder`), comma lists included.
- Who hears (`Thing.cs`): characters within the speech distance that can see the speaker. Saying a listener's
  name first ("Name ...") talks to that one only, and an NPC that answered exclusively stays the speaker's
  conversation partner for the next line. A listener runs its TriggerGroups (its own and its def's) first, then
  `On_Hear` (pet commands, vendors).
- A speech returns `SpeechResult.ActedUponExclusively` to stop the dispatch, or `IgnoredOrActedUpon` to let
  others hear it too.
- Real speeches come in two shapes: a C# `AbstractSpeech` subclass registered in `Bootstrap()` (`BankerSpeech`
  is spk_jobbanker, `ShopkeeperSpeech` is spk_shopkeep), or an LScript `[Speech name]` section with the same
  `ON=*pattern*` triggers (`ScriptedSpeech`, e.g. spk_vendor_custom in `scripts/Optional/PlayerVendor`).

## Placeholders for unported speeches

The converter turns every archive `[SPEECH x]` into a `[SpeechStub x]` section (`ConvertedSpeech`): the `ON=`
patterns stay as they are and the Sphere script under them is kept as comments, ready for porting.
`SpeechStub` (`scripts/Basic/Speech/SpeechStub.cs`) loads it:

- every `TSPEECH` reference to an archive block resolves, so the speech shows on the def (no boot warning);
- players notice nothing. When a phrase matches one of the patterns, GMs within speech distance of the NPC get
  `[unported speech spk_guard on <NPC>: *guard*]`, so live testing shows which unported speech it hit;
- a real speech of the same name wins whichever loads first: a stub section is skipped when the name is
  already taken by a real speech, and `ScriptedSpeech` replaces a stub that loaded before it
  (`AbstractSpeech.IsPlaceholder`).

**Porting one**: write the C# `AbstractSpeech` (or an LScript `[Speech]`) under the archive name, using the
commented Sphere body in `scripts/converted` as the reference. Nothing in the converted content changes.

Still warning at boot (by design): 5 chardefs name speech blocks that don't exist in the archive
(`Hrobus`, `spk_uhlir`, `spk_mo_malicia`, `spk_kvest0001_Kapitan_straze_Barad`, `spk_kvest001_Liche_Sarfeos`).

## Ported speeches

### spk_guard - calling the guards (#826)

`GuardSpeech` (`scripts/Basic/Speech/GuardSpeech.cs`), on Erebor's 76 guard chardefs. Saying anything containing
"guard" or "straz" near a guard sends it after whoever the speaker has been fighting: `FightMemory`
(`scripts/Basic/AI/FightMemory.cs`), the engine's version of Sphere's `MEMORY_FIGHT`, remembers both sides of every
hostile act (`Character.Trigger_HostileAction`) for `guardCallMemorySeconds` (120, `.settings` "NPC Aggro"). The
guard leaves its target alone when it protects them (Sphere's `ismyguard`):

| Guard (archive `tag.guard`) | Protects |
|---|---|
| Gondor (1) | Gondor players who are neither criminal nor murderers |
| Mordor (2) | every Mordor player |

Only players are targeted, never an invulnerable one. Every guard in earshot answers on its own (each Sphere NPC
hears on its own), shouting one of Erebor's two Czech lines ("<name>, odejdi nebo zemri!" or "<name>, v <region>
nemas co pohledavat!") and engaging the target through `NPCAiTimer.EngageTarget`.

A possessing mystic (hypnosis) is left alone unless its body is evil (karma -800 or less), and counts as no murderer,
as Erebor's `hypno_mystik` checks and hypnoswap's karma.

Not ported: keep bans, which come with the keeps, and the guard's temporary karma (`guardkarma_*` + `i_memory_cleaner`). The archive's teleport-to-target
(`e_tele_to_target` / `i_attack_guard`) only fires when the target stands in an insta-logout area, and only 7 archive
areas have that flag (jail, klec, Lagr, the tutorial, bug) - guards here walk to their target like every NPC.

Test: `GuardSpeechTests` (protocol, two real clients, real hostile acts and real speech).

### spk_healer - "res" near a healer (#827)

`HealerSpeech` (`scripts/Basic/Speech/HealerSpeech.cs`), on 18 healer chardefs: anything containing "res" makes the
healer tell a realm outcast (Erebor's negative `tag.realm`, our `GondorOutcast`/`MordorOutcast`) that they're
cursed by the gods, and resurrect a dead speaker it sees. That second half never reaches an ordinary ghost, in
Erebor or here: 0.99z8 only lets an NPC understand ghost speech if it has the healer brain, Spirit Speak or is
invulnerable (`CanUnderstandGhost`), and Erebor's healers are plain vendor-brain NPCs; here no NPC sees a ghost, so
none hears one. Erebor's ghosts were resurrected by **single-clicking** a healer - `e_healer`, now a real ability
(`HealerAbilityDef`, see [Monster abilities](monster-abilities.md)). Both share `Healer` (`MonsterSpecialAttacks/Healer.cs`).

Not ported: `resurrect_player`'s account log line (`accMsg`), and Sphere's hard-coded healer-brain reactions
(Erebor's healers don't have that brain).

Test: `HealerTests` (protocol: real clicks and speech as a ghost and as an outcast).

### spk_mistr - skill teachers (#828)

`SkillTeacherSpeech` (`scripts/Basic/Speech/SkillTeacherSpeech.cs`), on Erebor's 36 teachers (`chars/ucitele.scp`, one
per skill: the archive's `tag.ucimskill`, set in their `@userclick`, converted to `NPCDef.TeachesSkill`). Skills grow
normally past 25/50/75 - Erebor's `skillgain` only checks the profession cap - and a teacher sells the jump from
**exactly** the threshold:

| Lesson | Skill | Becomes | Price | Level | Rank (with the switch on) | Profession cap |
|---|---|---|---|---|---|---|
| Expert | 25.0 | 30.0 | 5000 | 3+ | Expert | 30.0+ |
| Master | 50.0 | 55.0 | 10000 | 6+ | Master | 55.0+ |
| Grandmaster | 75.0 | 80.0 | 20000 | 12+ | Grandmaster | 80.0+ |

Greeting the teacher ("dobry den", "zdravim", "mistr", "hi", "hello", "buy"...) gets its offer (or Erebor's message
for what's missing: not ready, reached your maximum, train more, level too low); "ano"/"yes" pays (`GoldUtils.TryPay`)
and sets the skill. No lessons for a ghost, a player without a profession, or one under bless/curse/stat spells
(Erebor's `ismagic`). Greeting also rounds the taught skill to the nearest 5.0, never below the profession's
starting value (Erebor's `roundskill`: 27.4 becomes 25.0).

The player rank is `Player.SkillRank` (Novice/Expert/Master/Grandmaster, Erebor's `tag.skillrank`), editable in
`.info`. Players earn it through the [rank quests](rank-quests.md). The "Skill Teachers" `.settings` switch `requireRank`
is on by default: a world that hasn't run `.SetupWorld` needs it off, because nobody could earn a rank there. The same category holds `roundSkillOnGreeting`, the prices and the levels.
Not ported: the weekly "meet a master" quest (`f_weekly_meetMaster`).

Test: `SkillTeacherTests` (protocol: the real converted anatomy teacher, a mage and a priest student, real speech).

## Differences in `ScriptedSpeech` from Sphere

These matter only for hand-written LScript speeches. `ScriptedSpeech` matches its patterns anywhere in the
utterance (not the whole of it), stops at the first matching `ON=` (Sphere goes on after a `return 0`), and
treats a body with no return as not handled (Sphere treats it as handled). `SpeechStub` uses Sphere's
whole-utterance matching.

Tests: `SpeechStubTests` (protocol: real speech from a real client to a converted guard; GM gets the hint,
a player doesn't, ported speeches win) and `SpeechPlaceholderTests` (fast tier: load order).
