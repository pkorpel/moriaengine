#!/bin/sh
# Make a worktree next to this checkout from origin/main, without the dormant 800 MB
# of Cruncher/ and OldUpdater/ (sparse checkout; uo-files/ stays, the build needs it).
#
#   scripts/worktree-add.sh <n>-<slug>       # → ../moria-<n>, new branch <n>-<slug>
#   scripts/worktree-add.sh --verify <n>     # → ../moria-verify-<n>, detached at origin/main
#   git worktree remove <path>               # when done
set -eu

root=$(cd "$(dirname "$0")/.." && pwd)
parent=$(dirname "$root")

if [ "${1:-}" = "--verify" ] && [ $# -eq 2 ]; then
  wt="$parent/moria-verify-$2"; set -- --detach
else
  [ $# -eq 1 ] || { sed -n '2,7p' "$0"; exit 2; }
  case "$1" in [0-9]*-*) ;; *) echo "worktree-add: branch must be <n>-<slug>" >&2; exit 2 ;; esac
  wt="$parent/moria-${1%%-*}"; set -- --no-track -b "$1"
fi
[ -e "$wt" ] && { echo "worktree-add: $wt already exists" >&2; exit 1; }

git -C "$root" fetch -q origin
git -C "$root" worktree add --no-checkout "$@" "$wt" origin/main
git -C "$wt" sparse-checkout set --no-cone '/*' '!/Cruncher/' '!/OldUpdater/'
git -C "$wt" checkout -q
echo "$wt"
