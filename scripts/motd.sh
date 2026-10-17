#!/bin/sh
# Session banner and status line: where this checkout stands in the
# issue → PR → main → verified flow, and the one next step.
#
#   scripts/motd.sh           # banner as text
#   scripts/motd.sh --hook    # SessionStart JSON (systemMessage + additionalContext)
#   scripts/motd.sh --line    # one line for the Claude Code status line (stdin: its JSON)
#
# GitHub token: scripts/gh-token.sh (env, Keychain or gh). It goes to
# curl on stdin and is never printed. No token: local git state only. Network calls
# get MOTD_DEADLINE seconds (3) in total. --line caches the remote part per branch for
# STATUSLINE_TTL seconds (30) in the git common dir, so worktrees share it.
set -u

REPO=pkorpel/moriaengine
PREFIX=moria
API=https://api.github.com
DEADLINE="${MOTD_DEADLINE:-3}"
TTL="${STATUSLINE_TTL:-30}"
mode=text
case "${1:-}" in --hook) mode=hook ;; --line) mode=line ;; -h|--help) sed -n '2,12p' "$0"; exit 0 ;; esac
have() { command -v "$1" >/dev/null 2>&1; }

dir=$(cd "$(dirname "$0")/.." && pwd)
if [ "$mode" = line ] && [ ! -t 0 ]; then
  d=$(python3 -c 'import json,sys
try: j=json.load(sys.stdin)
except Exception: j={}
print((j.get("workspace") or {}).get("current_dir") or j.get("cwd") or "")' 2>/dev/null)
  [ -n "$d" ] && [ -d "$d" ] && dir=$d
fi

branch=n/a dirty=0 ahead=n/a common=
if git -C "$dir" rev-parse --git-dir >/dev/null 2>&1; then
  branch=$(git -C "$dir" branch --show-current); [ -n "$branch" ] || branch=detached
  dirty=$(git -C "$dir" status --porcelain --untracked-files=no | wc -l | tr -d ' ')
  ahead=$(git -C "$dir" rev-list --count origin/main..HEAD 2>/dev/null || echo n/a)
  common=$(git -C "$dir" rev-parse --path-format=absolute --git-common-dir)
fi
iid=; case "$branch" in [0-9]*-*) iid=${branch%%-*} ;; esac

remote() {  # prints key=value lines; empty on no token / timeout
  token=$(sh "$dir/scripts/gh-token.sh" 2>/dev/null || true)
  [ -n "$token" ] && have curl && have python3 || return 0
  get() { printf 'header = "Authorization: Bearer %s"\n' "$token" | curl -s -m "$DEADLINE" -K - -H 'Accept: application/vnd.github+json' "$API$1"; }
  tmp=$(mktemp -d); trap 'rm -rf "$tmp"' EXIT
  get "/repos/$REPO/issues?state=open&per_page=100" > "$tmp/issues" &
  [ -n "$iid" ] && get "/repos/$REPO/pulls?state=open&head=pkorpel:$branch" > "$tmp/pr" &
  wait
  python3 - "$tmp" "$iid" <<'EOF'
import json, os, shlex, sys
tmp, iid = sys.argv[1], sys.argv[2]
def load(n):
    try: return json.load(open(os.path.join(tmp, n)))
    except Exception: return None
issues = load("issues")
if not isinstance(issues, list): sys.exit()
issues = [i for i in issues if "pull_request" not in i]
count = {}
for i in issues:
    st = next((l["name"][7:] for l in i["labels"] if l["name"].startswith("status:")), "triage")
    count[st] = count.get(st, 0) + 1
    if str(i["number"]) == iid:
        print(f"issue_status={st}")
        print("issue_title=" + shlex.quote(i["title"][:50]))
print("board=" + shlex.quote(" ".join(f"{k}:{v}" for k, v in sorted(count.items()))))
pr = load("pr")
if isinstance(pr, list) and pr:
    print(f"pr={pr[0]['number']}")
EOF
}

if [ "$mode" = line ]; then
  cache="${common:-/tmp}/moria-statusline.$(printf '%s' "$branch" | tr '/' '_')"
  if [ ! -f "$cache" ] || [ $(( $(date +%s) - $(stat -f %m "$cache" 2>/dev/null || stat -c %Y "$cache" 2>/dev/null || echo 0) )) -gt "$TTL" ]; then
    remote > "$cache.tmp" 2>/dev/null && mv "$cache.tmp" "$cache"
  fi
  issue_status= pr= board=
  [ -f "$cache" ] && eval "$(cat "$cache")"
  out="$branch"; [ "$dirty" -gt 0 ] 2>/dev/null && out="$out*"
  [ -n "$iid" ] && out="$out · #$iid ${issue_status:-?}"
  [ -n "$pr" ] && out="$out · PR #$pr"
  [ -n "$board" ] && out="$out · $board"
  printf '%s\n' "$out"
  exit 0
fi

issue_status= issue_title= pr= board=
eval "$(remote 2>/dev/null)"

next="/task-pick to start a status:ready issue, or /task-new to file one"
case "$branch" in
  main) [ "$dirty" -gt 0 ] && next="uncommitted changes on main: move them to an issue branch" ;;
  [0-9]*-*)
    if [ -n "$pr" ]; then next="PR #$pr is open: /task-ship waits for CI and merges it"
    elif [ "$ahead" != n/a ] && [ "$ahead" -gt 0 ]; then next="$ahead commit(s) on #$iid: /task-ship when done"
    else next="work on #$iid, then /task-ship"; fi ;;
esac

text="$PREFIX · issue → branch → PR → main → verified locally → closed
  checkout  $branch$( [ "$dirty" -gt 0 ] && echo " ($dirty changed)")$( [ "$ahead" != n/a ] && echo ", $ahead ahead of origin/main")
  issue     $( [ -n "$iid" ] && echo "#$iid ${issue_status:-?} ${issue_title:-}" || echo none)
  board     ${board:-n/a (no GitHub token, or no access to $REPO)}
  next      $next"

if [ "$mode" = hook ]; then
  python3 -c 'import json,sys; t=sys.stdin.read(); print(json.dumps({"systemMessage": t, "hookSpecificOutput": {"hookEventName": "SessionStart", "additionalContext": t}}))' <<EOF
$text
EOF
else
  printf '%s\n' "$text"
fi
