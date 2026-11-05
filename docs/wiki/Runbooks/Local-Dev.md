# Local dev

Last reviewed: 2026-10-16

## One-time setup

- .NET SDK 8.0.x (`global.json`, if present, pins it).
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
scripts/test-fast.sh                      # Debug build + Release compile + fast tier (~1 min)
scripts/test-fast.sh "FullyQualifiedName~RaceTests"
scripts/test-full.sh                      # whole suite, class by class (one run at a time)
scripts/test-full.sh "FullyQualifiedName~LevelingTests"
```

## Run the shard

```sh
scripts/run-local.sh start --build   # Aux (2593 login, 2594 console) + GameCore (2595)
scripts/run-local.sh start --release # the same from the Release build (2-3x less CPU per NPC think)
scripts/run-local.sh status
scripts/run-local.sh logs            # moriaengine/logs/{aux,gamecore}.console.log
scripts/run-local.sh stop
```

Stop the shard before `scripts/test-full.sh`: same ports.
