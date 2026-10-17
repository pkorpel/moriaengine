#!/bin/sh
# Start the GitHub MCP server for this repo on stdio; .mcp.json runs it.
#
# Token: scripts/gh-token.sh ($AGENT_GITHUB_TOKEN, Keychain, or `gh auth token`). It
# reaches the server through the environment only — never through the command line,
# where `ps` would show it.
#
# Runs the official github-mcp-server: a local binary on PATH if there is one,
# otherwise the Docker image pinned by digest (bump it on purpose, not by pulling
# :latest). Tools that write the repo outside of git (create/delete files, push_files,
# branches, forks, new repos) are excluded: code goes through git and PRs.
set -eu

GITHUB_PERSONAL_ACCESS_TOKEN=$(sh "$(dirname "$0")/gh-token.sh" || true)
if [ -z "$GITHUB_PERSONAL_ACCESS_TOKEN" ]; then
  echo "mcp-github: no token. Export AGENT_GITHUB_TOKEN, log in with 'gh auth login'," >&2
  echo "  or on macOS: security add-generic-password -U -s github-pkorpel -a pkorpel -w" >&2
  exit 1
fi
export GITHUB_PERSONAL_ACCESS_TOKEN

set -- stdio \
  --toolsets=issues,pull_requests,actions,repos,labels \
  --exclude-tools=create_or_update_file,delete_file,push_files,create_branch,create_repository,fork_repository

if command -v github-mcp-server >/dev/null 2>&1; then
  exec github-mcp-server "$@"
fi
command -v docker >/dev/null 2>&1 || {
  echo "mcp-github: needs Docker or a github-mcp-server binary on PATH" >&2
  echo "  (https://github.com/github/github-mcp-server/releases)" >&2
  exit 1
}
# v2.0.2
exec docker run -i --rm \
  -e GITHUB_PERSONAL_ACCESS_TOKEN \
  ghcr.io/github/github-mcp-server@sha256:ffced0d76e77428532a2ced185516992d77fd146d69eec2f244e6237d1a97796 \
  "$@"
