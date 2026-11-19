# MoriaEngine — instructions for every developer and coding agent

MoriaEngine is SteamEngine, an Ultima Online server emulator in C# (.NET 8), being made into a faithful
port of the **Erebor** shard (Sphere 0.99z8 scripts). GitHub: `pkorpel/moriaengine`. Maintainer: pkorpel.

This file is shared by people, Claude Code (through `CLAUDE.md`), GitHub Copilot and any other agent.
Topic rules live in `.claude/rules/*.md`. They are plain markdown, so read the one for the area you touch,
whatever tool you use.

## Repository shape

- Active work is in `moriaengine/`. `Cruncher/`, `MoriaUpdater/` and `OldUpdater/` are dormant.
- `moriaengine/GameCore/` holds gameplay, world simulation, scripting and persistence.
  `AuxiliaryServer/` is the login server and remote console. `Common/`, `Communication/` and
  `ThirdParty/` are shared infrastructure.
- `moriaengine/scripts/` holds game scripts in C# and LScript (`.def`/`.scp`). Every server boot compiles
  them from disk. `scripts/converted/` is **generated** by `SphereShardConverter` from the Erebor archive.
- `moriaengine/Tools/SteamEngine.Generators*` holds the Roslyn source generators. `moriaengine/Generated/`
  is boot-time generated output. Neither kind of generated code is edited by hand; see
  `moriaengine/docs/current/02-engine-core/scripting-pipeline.md`.
- `moriaengine/Tests/` is xUnit: the fast unit tier, plus `Tests/Protocol/`, which boots real servers.
  `moriaengine/scripts/Tests/` holds the server-side test commands.
- `uo-files/` holds the UO client files the server needs at runtime. They are checked in, not build output.
- `moriaengine/bin/` holds 19 tracked legacy tools and libraries, not build output (whatever
  `.gitignore`'s `[Bb]in/` suggests). Never delete it.
- Docs: `moriaengine/docs/current/` is the reference for how each system works and why.
  `docs/wiki/` covers process and runbooks. `docs/history/` holds the old CLAUDE.md work log, read-only.
  The older `moriaengine/docs/*` files are legacy and not reliable.

## Build, test, run

```bash
cd moriaengine && pwsh ./build.ps1 [-Configuration Release] [-Run Both]   # source of truth for builds
scripts/test-fast.sh [filter]     # build + fast tier: the CI gate and the pre-push hook
scripts/test-fast.sh "FullyQualifiedName~SomeTest"   # one fast-tier test (the filter is ANDed with the tier)
scripts/test-full.sh [filter]     # fast tier + each Tests/Protocol class in its own run (~3 h for all)
scripts/run-local.sh start|status|logs|stop    # local shard on ports 2593 (aux) / 2595 (game)
scripts/worktree-add.sh <n>-<slug>             # sparse worktree ../moria-<n> from origin/main
```

- Full-boot tests use fixed ports, so run one `test-full.sh` at a time and never a raw `dotnet test` on
  `Tests/Protocol`. A filter is a class-name substring: one that matches nothing passes without running
  anything.
- Build with the scripts. They source `scripts/lean-build-env.sh`, which stops idle MSBuild and compiler
  processes. A bare `dotnet build`/`dotnet test` leaves them behind.
- After removing or renaming something the boot-generated `Generated/` files refer to (a compiled trigger
  group's method, a `[ViewableClass]` member), the old files can break a plain build until the next boot.
  `build.ps1` handles it. The Roslyn generators run at compile time and don't have this problem.
- `steamengine.ini`/`steamaux.ini` are local and not committed. The first boot writes them and exits.

## How changes land

Full process: `docs/wiki/Process/Dev-Process.md` (branches, hooks, CI, tests, review) and
`docs/wiki/Process/Tasks.md` (issues, labels, definition of done). Agents: `.claude/rules/workflow.md`.

1. **Issue first, labelled.** Every piece of work has a GitHub issue before it starts, with exactly one
   `type:` (bug, feature, chore, docs, research), one `prio:` (1-3) and the `area:` labels that apply.
2. **"Picking this up"** is the first comment when work starts: the plan and the branch. Findings and
   decisions go on the issue as comments while you work.
3. **A feature investigates the existing system first** and plugs into it: an "Existing system" comment on
   the issue covering the engine (its `docs/current/` page, the code, the original `a27bbf6`) and Erebor. If
   the existing piece is broken, it's fixed first in its own issue, never replaced. A new system, or one
   touching more than one existing system, gets a design note the maintainer OKs first.
4. **Branch** `<n>-<slug>`, in its own worktree `../moria-<n>`. `main` takes changes only through
   squash-merged PRs. Run `pre-commit install` once per clone, and never `--no-verify`. Commit subject:
   `area: what changed (#1234)`, imperative, at most 72 characters, never a leading `#`. No `Co-Authored-By`
   or session trailers: the commit-msg hook rejects them.
5. **Done means, in order:**
   1. code and its docs in the same PR (a changed system updates its `moriaengine/docs/current/` page, a new
      page is linked from its README, and the wiki changes with the scripts, hooks or process it describes);
   2. an independent review of every change, docs included (for docs it checks the stated facts against the
      code), with every finding addressed and the PR's `## Review` section saying what it found;
   3. a new test that fails without the fix (A/B), the fast tier, and the area's protocol classes, with the
      PR listing only what ran (the full suite, ~3 h, runs once per batch);
   4. **engine core** (`GameCore/`, `Common/`, `Communication/`, `AuxiliaryServer/`,
      `Tools/SteamEngine.Generators*`) **approved by janslav**, or by pkorpel when janslav wrote it
      (`.github/CODEOWNERS`, `needs-core-review`);
   5. CI green, merged, and the branch and worktree removed;
   6. the issue closed with a summary comment, and `needs-in-game-check` if it hasn't been tried in a real
      client.

## Rules that hold everywhere

- **Mirror Erebor.** Behaviour, numbers and messages follow Erebor's scripts and Sphere 0.99z8, unless the
  maintainer decided otherwise. Port; don't invent. Details: `.claude/rules/erebor-port.md`.
- **Never edit `moriaengine/scripts/converted/`.** Fix the archive fork or the converter and regenerate.
  Details: `.claude/rules/converter.md`.
- **Engine pattern first.** Before using or changing an engine mechanism (plugins, code-made defs, def
  fields, tags, triggers, timers), read its page in `docs/current/` and the existing code. The original
  engine is commit `a27bbf6`. Build in that pattern. Fix a bug in the pattern itself and never add a
  second mechanism. Details: `.claude/rules/engine-patterns.md`.
- **Czech text is written without diacritics** (`hrac`, not `hráč`), in code, `.resx` and `.def`.
  The old 3.0.6m clients can't show them.
- **Game strings live in the `.resx` files**, in both English and Czech. See `.claude/rules/localization.md`.
- **World content goes through `scripts/Setup` and `.SetupWorld`**, never one-off placement code. See
  `.claude/rules/world-setup.md`.
- **The server is blank:** no migration code for old saves when saved data is renamed or removed.
- **Players mostly run the 3.0.6m client.** Design for it first. The test client (7.0.15.1) and TazUO
  prove nothing about it.
- **Zero build warnings.**
- **Line endings vary per file** (some are CRLF), and some sources aren't UTF-8. Keep each file's existing
  endings and encoding. Search with `grep -a` / `--binary-files=text`.

## Topic rules (`.claude/rules/`)

| File | Covers |
|---|---|
| `workflow.md` | Issue lifecycle, merge safety, scope, questions, worktrees |
| `erebor-port.md` | Mirroring Erebor, reference sources, what is not ported |
| `engine-patterns.md` | Which engine doc to read first, rules whose breakage is silent (C#) |
| `testing.md` | Writing tests that prove something |
| `converter.md` | Never editing `scripts/converted/` |
| `lscript.md` | `.def`/`.scp` |
| `localization.md` | `.resx` and loc strings |
| `world-setup.md` | `scripts/Setup` and `.SetupWorld` |

How the engine and its systems work lives in `moriaengine/docs/current/`, not in these rules. The rules point
there and add only what has to be followed.
