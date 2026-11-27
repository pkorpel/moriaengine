# Local dev

Last reviewed: 2026-11-27

## One-time setup

- .NET SDK 10.0.x. `global.json` pins the 10.0.1xx band or a later one (`latestFeature`), and
  `moriaengine/Directory.Build.props` sets the target framework for every project (#1551).
- PowerShell 7 (`pwsh`): `build.ps1` needs it. macOS: `brew install powershell`.
- `pre-commit` and `gitleaks` for the git hooks, then once per clone:
  `pre-commit install` (installs the pre-commit, commit-msg and pre-push hooks).
- GitHub token for the agents' MCP server and the session banner, one of:
  - `gh auth login` (the tooling falls back to `gh auth token`);
  - `export AGENT_GITHUB_TOKEN=…`;
  - macOS Keychain: `security add-generic-password -U -s github-pkorpel -a pkorpel -w`.
  Needs Contents, Issues, Pull requests, Actions (read and write) on `pkorpel/moriaengine`.
- Docker running, or a `github-mcp-server` binary on PATH, for `scripts/mcp-github.sh`.

## Build and test

```sh
scripts/test-fast.sh                      # Debug build + Release compile + generator and fast-tier tests (~1 min)
scripts/test-fast.sh "FullyQualifiedName~RaceTests"  # filter the fast tier; all generator tests still run
scripts/test-full.sh                      # whole suite, class by class (one run at a time)
scripts/test-full.sh "FullyQualifiedName~LevelingTests"
```

All of these build through `moriaengine/build.ps1`. Before building, it deletes what the old code generators left
in older clones (#1592). The `*.Generated.cs` files under `scripts/` (the old `.ct` template output) are
gitignored, so a pull keeps them, and they break the build with thousands of SEGEN905/SEGEN012 errors. The dead
`Generated/` folder goes too. A build that bypasses `build.ps1` doesn't clean them: an IDE, a bare `dotnet build`,
or a GameCore boot that compiles the scripts itself (`run-local.sh start` without `--build`). After pulling into an
old clone, run `build.ps1` once.

## Run the shard

```sh
scripts/run-local.sh start --build   # Aux (2593 login, 2594 console) + GameCore (2595)
scripts/run-local.sh start --release # the same from the Release build (2-3x less CPU per NPC think)
scripts/run-local.sh status
scripts/run-local.sh logs            # moriaengine/logs/{aux,gamecore}.console.log
scripts/run-local.sh stop
```

Stop the shard before `scripts/test-full.sh`: same ports.
