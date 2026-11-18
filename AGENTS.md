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

Full process: `docs/wiki/Process/Dev-Process.md` (branches, hooks, CI, tests) and
`docs/wiki/Process/Agent-Workflow.md` (agent flow).

1. **Issue first.** Every piece of work has a GitHub issue before work starts. Findings, decisions and
   progress go on the issue as comments while you work. Close it once the PR has merged, referencing the
   PR (`/task-run` closes it after its local-boot verify).
2. **Branch** `<n>-<slug>`, in its own worktree `../moria-<n>`. `main` takes changes only through PRs,
   which are squash-merged.
3. **Hooks:** run `pre-commit install` once per clone. Never use `--no-verify`. Commit subject:
   `area: what changed (#1234)`, imperative, at most 72 characters, issue number at the end, and never a
   leading `#`, because git reads that as a comment. No `Co-Authored-By` or session trailers: the
   commit-msg hook rejects them.
4. **Tests:** a fix gets a test that fails without it. Run the fast tier plus the protocol classes for the
   area you touched. The full suite (~3 h) runs once at the end of a batch of work, not per PR. The PR
   body lists only the checks that really ran, on the final code.
5. **Review:** every change to code gets an independent review before merge, and the PR body has a
   `## Review` section saying what it found. Code is anything other than `*.md`: C#, `.def`/`.scp`,
   scripts, hooks, CI, `.claude/settings.json`. `.claude/` command, agent, rule and skill pages are `.md`,
   so they count as docs. For Claude a hook enforces this.
6. **Docs in the same PR:** a change to a system documented in `moriaengine/docs/current/` updates that
   page in the same PR. A new system gets a new page, linked from `moriaengine/docs/current/README.md`. A
   plain bugfix with no lasting "why" needs no doc change. The wiki (`docs/wiki/`) also changes in the
   same branch as the scripts, hooks, CI or commands it describes.

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
