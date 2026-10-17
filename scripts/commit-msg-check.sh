#!/bin/sh
# commit-msg hook: keep attribution out of the history and subjects short.
# It bans trailers, not words: "llm: switch extraction to sonnet-4-6" is a normal
# commit in an app that calls an LLM.
set -eu
msg="$1"

# the message as git will store it: without the comment lines
body=$(grep -v '^#' "$msg" || true)
subject=$(printf '%s\n' "$body" | sed -n '/./{p;q;}')

fail() { echo "commit-msg: $1" >&2; echo "  (message kept in $msg)" >&2; exit 1; }

if printf '%s\n' "$body" | grep -qiE '^(co-authored-by|claude-session):'; then
  fail "remove the Co-Authored-By / Claude-Session trailer"
fi
if printf '%s\n' "$body" | grep -qiE 'generated with \[?claude|claude\.ai/code/session'; then
  fail "remove the 'Generated with' line or session link"
fi

case "$subject" in
  fixup!*|squash!*|amend!*|Merge*|Revert*) exit 0 ;;
esac
len=$(printf '%s' "$subject" | wc -m | tr -d ' ')
[ "$len" -le 72 ] || fail "subject is $len characters, keep it to 72"
