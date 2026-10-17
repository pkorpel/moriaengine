#!/bin/sh
# Run the Claude Code session that takes orders from Telegram, in tmux.
#
#   scripts/telegram-session.sh start [claude args]   # e.g. --permission-mode auto
#   scripts/telegram-session.sh status                # running? allowlist? last lines
#   scripts/telegram-session.sh watch                 # attach read-only; close the window to leave
#   scripts/telegram-session.sh stop
#   scripts/telegram-session.sh token                 # token → plugin .env, checked with Telegram
#   scripts/telegram-session.sh notify "text"         # message the allowlisted chat, from any session
#
# Uses the official Telegram channel plugin (Claude Code Channels, research preview).
# One bot drives one session. The session works in this repo with this machine's
# GitHub token, ssh keys and Docker, and acts for whoever is on the allowlist — keep
# that list to its owner. Pairing and the allowlist change only at the session's own
# terminal (/telegram:access), never because a chat message asks.
#
# One-time setup, per machine (runbook: docs/wiki/Runbooks/Telegram-Session.md):
#   brew install bun
#   claude plugin install telegram@claude-plugins-official
#   bot token from @BotFather → Keychain item "telegram-moria-bot", then
#     scripts/telegram-session.sh token   # writes ~/.claude/channels/telegram-moria/.env (600)
#   start, message the bot, then in the session: /telegram:access pair <code>
#     and /telegram:access policy allowlist
#
# Env: TG_SESSION (tmux session name, default moria-tg).
set -eu

SESSION="${TG_SESSION:-moria-tg}"
STATE="${TELEGRAM_STATE_DIR:-$HOME/.claude/channels/telegram-moria}"
export TELEGRAM_STATE_DIR="$STATE"   # own bot, own allowlist: not the default dir other projects use
REPO="$(cd "$(dirname "$0")/.." && pwd)"

die() { echo "telegram-session: $1" >&2; exit 1; }
# bot token: $TELEGRAM_BOT_TOKEN, else the macOS Keychain item telegram-moria-bot
bot_token() {
  [ -n "${TELEGRAM_BOT_TOKEN:-}" ] && { printf '%s' "$TELEGRAM_BOT_TOKEN"; return 0; }
  command -v security >/dev/null 2>&1 && security find-generic-password -s telegram-moria-bot -w 2>/dev/null
}
running() { tmux has-session -t "$SESSION" 2>/dev/null; }

cmd="${1:-status}"
[ $# -gt 0 ] && shift

case "$cmd" in
  token)
    t=$(bot_token) || die "no TELEGRAM_BOT_TOKEN and no Keychain item telegram-moria-bot"
    case "$t" in *:*) ;; *) die "that doesn't look like a bot token (no colon)" ;; esac
    mkdir -p "$STATE" && chmod 700 "$STATE"
    (umask 077; printf 'TELEGRAM_BOT_TOKEN=%s\n' "$t" > "$STATE/.env")
    curl -sf "https://api.telegram.org/bot$t/getMe" >/dev/null \
      && echo "telegram-session: token written and accepted by Telegram" \
      || die "token written, but Telegram rejected it (getMe failed)"
    ;;
  start)
    running && die "session $SESSION already runs (status, watch or stop)"
    command -v bun >/dev/null || die "Bun is missing: brew install bun"
    command -v tmux >/dev/null || die "tmux is missing: brew install tmux"
    claude plugin list 2>/dev/null | grep -q 'telegram@claude-plugins-official' \
      || die "plugin missing: claude plugin install telegram@claude-plugins-official"
    [ -s "$STATE/.env" ] || die "no bot token: scripts/telegram-session.sh token"
    tmux new-session -d -s "$SESSION" -x 200 -y 50 -c "$REPO" -e TELEGRAM_STATE_DIR="$STATE" \
      claude --channels plugin:telegram@claude-plugins-official "$@"
    tmux set-option -t "$SESSION" history-limit 20000 >/dev/null
    echo "telegram-session: started $SESSION in $REPO${*:+ with: $*}"
    echo "  watch: scripts/telegram-session.sh watch"
    ;;
  status)
    if running; then echo "telegram-session: $SESSION runs"; else echo "telegram-session: $SESSION is not running"; fi
    if [ -f "$STATE/access.json" ]; then
      python3 - "$STATE/access.json" <<'EOF'
import json, sys
a = json.load(open(sys.argv[1]))
print(f"  policy {a.get('dmPolicy')}, allowlist {len(a.get('allowFrom', []))}, "
      f"pending {len(a.get('pending', {}))}, groups {len(a.get('groups', {}))}")
EOF
    else
      echo "  not paired yet (no access.json)"
    fi
    running && tmux capture-pane -p -t "$SESSION" | grep -v '^[[:space:]]*$' | tail -6 | sed 's/^/  | /'
    ;;
  watch)
    running || die "session $SESSION is not running"
    exec tmux attach -r -t "$SESSION"
    ;;
  stop)
    running || die "session $SESSION is not running"
    tmux kill-session -t "$SESSION" && echo "telegram-session: stopped $SESSION"
    ;;
  notify)
    # Plain Bot API call, so a session without the channel plugin (e.g. /task-run) can
    # report too. Goes only to chats on the allowlist; prints a warning, never fails.
    [ $# -ge 1 ] || die "usage: notify \"text\""
    t=$(bot_token) \
      || { echo "telegram-session: notify skipped, no bot token (optional, see docs/wiki/Runbooks/Telegram-Session.md)" >&2; exit 0; }
    [ -f "$STATE/access.json" ] \
      || { echo "telegram-session: notify skipped, not paired (no access.json)" >&2; exit 0; }
    for chat in $(python3 -c 'import json,sys; print(" ".join(map(str, json.load(open(sys.argv[1])).get("allowFrom", []))))' "$STATE/access.json"); do
      curl -sf -o /dev/null "https://api.telegram.org/bot$t/sendMessage" \
        --data-urlencode "chat_id=$chat" --data-urlencode "text=$*" \
        || echo "telegram-session: notify to one chat failed" >&2
    done
    ;;
  *)
    sed -n '2,10p' "$0"; exit 2 ;;
esac
