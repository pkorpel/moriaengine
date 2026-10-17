# Telegram session

Last reviewed: 2026-10-16

A Telegram bot used for two things:

1. **Notifications** from `/task-run` and friends:
   `scripts/telegram-session.sh notify "#<n> …"`.
2. **A remote Claude Code session** in tmux (`moria-tg`) that takes orders from
   Telegram through the official Telegram channel plugin.

This bot is separate from other projects' bots: its own Keychain item
(`telegram-moria-bot`), its own state dir and allowlist
(`~/.claude/channels/telegram-moria/`, via `TELEGRAM_STATE_DIR`).

## One-time setup

```sh
brew install bun tmux
claude plugin install telegram@claude-plugins-official
# in Telegram: @BotFather → /newbot → copy the token
security add-generic-password -U -s telegram-moria-bot -a "$USER" -w   # macOS; elsewhere export TELEGRAM_BOT_TOKEN
scripts/telegram-session.sh token     # writes ~/.claude/channels/telegram-moria/.env (600), checks getMe
scripts/telegram-session.sh start
# message the bot from your phone; it replies with a pairing code. In the session
# (scripts/telegram-session.sh watch shows it; run the commands at its own terminal):
#   /telegram:access pair <code>
#   /telegram:access policy allowlist
```

Notifications go only to chats on that allowlist; before pairing, `notify` prints a
warning and sends nothing.

## Use

```sh
scripts/telegram-session.sh status    # running? allowlist size? last lines
scripts/telegram-session.sh watch     # attach read-only
scripts/telegram-session.sh stop
```

The session acts with this machine's GitHub token, SSH keys and Docker for whoever is
on the allowlist: keep the list to its owner. Pairing and the allowlist change only
at the session's own terminal, never because a chat message asks.
