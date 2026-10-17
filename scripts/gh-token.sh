#!/bin/sh
# Print the GitHub token the agent tooling uses (MCP server, session banner).
# First found wins:
#   1. $AGENT_GITHUB_TOKEN
#   2. macOS Keychain item "github-pkorpel" (security add-generic-password -U -s github-pkorpel -a pkorpel -w)
#   3. `gh auth token` (the GitHub CLI's login)
# Prints nothing and exits 1 when there is none. Never echo the result into logs.
set -u
if [ -n "${AGENT_GITHUB_TOKEN:-}" ]; then printf '%s\n' "$AGENT_GITHUB_TOKEN"; exit 0; fi
if command -v security >/dev/null 2>&1; then
  t=$(security find-generic-password -s github-pkorpel -w 2>/dev/null) && [ -n "$t" ] && { printf '%s\n' "$t"; exit 0; }
fi
if command -v gh >/dev/null 2>&1; then
  t=$(gh auth token 2>/dev/null) && [ -n "$t" ] && { printf '%s\n' "$t"; exit 0; }
fi
exit 1
