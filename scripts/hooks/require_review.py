#!/usr/bin/env python3
"""PreToolUse hook: no merge of a code change without a review (#1230, see CLAUDE.md
"Workflow conventions").

Every code change gets an independent review agent before it merges, and the PR body
says what the review found under a `## Review` heading (`## Reviews`, any heading
level, any case). This hook checks that heading at the merge:

  - Bash: every `gh pr merge` in the command (split on `;`, `&&`, `||`, `|`, `&`,
    parentheses and newlines), with its PR as given (number, URL or branch; none =
    the current branch's PR, looked up from the hook's cwd) and its `-R/--repo`;
  - mcp__github__merge_pull_request: owner, repo, pullNumber.

The PR's files and body come from `gh pr view --json files,body`. A PR whose files
are all `*.md` is docs only and passes. Anything else without the heading is denied.

Fails open: malformed input, a missing `gh`, a failed or slow lookup (8 s) all allow
the call, so a hook bug or a network hiccup never blocks the session. Not detected:
merges through `gh api`, `git push` to main (denied in settings.json anyway), or a
`gh pr merge` nested in `bash -c`, `eval`, `$(...)` or behind `sudo`/`env`/`xargs`.
"""
import json
import os
import re
import shlex
import subprocess
import sys

REASON = ("PR {pr} changes code but its body has no '## Review' section. Every code change "
          "gets an independent review agent before merge (#1230, CLAUDE.md): run the review, "
          "fix or answer its findings, add '## Review' with what it found to the PR body "
          "(gh pr edit --body-file), then merge.")

SEPARATORS = {";", "&&", "||", "|", "&", "|&", ";;", "(", ")", "\n"}
# gh pr merge flags that take a separate value
VALUE_FLAGS = {"-b", "--body", "-F", "--body-file", "-t", "--subject", "-R", "--repo",
               "--match-head-commit", "-A", "--author-email"}
REVIEW_HEADING = re.compile(r"^\s{0,3}#{1,6}\s*reviews?\b", re.IGNORECASE | re.MULTILINE)


def has_review(body):
    return bool(REVIEW_HEADING.search(body or ""))


def docs_only(files):
    return bool(files) and all(f.lower().endswith(".md") for f in files)


def commands(command):
    # newline is a separator, not whitespace; quoted newlines stay in their word
    lexer = shlex.shlex(command, posix=True, punctuation_chars="();<>|&\n")
    lexer.whitespace = " \t\r"
    lexer.whitespace_split = True
    lexer.commenters = ""
    words = []
    skip = False
    for tok in lexer:  # ValueError on unbalanced quotes
        punctuation = set(tok) <= set(lexer.punctuation_chars)
        if punctuation and any(c in tok for c in "<>"):
            if words and words[-1].isdigit():
                words.pop()  # the 2 of 2>&1
            skip = True  # its target: a file, or the 1 of 2>&1
        elif punctuation:
            if words:
                yield words
            words = []
            skip = False
        elif skip:
            skip = False
        else:
            words.append(tok)
    if words:
        yield words


def merge_target(words):
    """(selector or None, repo or None) when words are a `gh pr merge`, else None."""
    while words and "=" in words[0] and not words[0].startswith("="):
        words = words[1:]  # FOO=bar gh ...
    if len(words) < 3 or os.path.basename(words[0]) != "gh" or words[1:3] != ["pr", "merge"]:
        return None
    selector, repo = None, None
    rest = words[3:]
    i = 0
    while i < len(rest):
        w = rest[i]
        if w in VALUE_FLAGS:
            if w in ("-R", "--repo") and i + 1 < len(rest):
                repo = rest[i + 1]
            i += 2
            continue
        if w.startswith("--repo="):
            repo = w.split("=", 1)[1]
        elif not w.startswith("-") and selector is None:
            selector = w
        i += 1
    return selector, repo


def targets(data):
    tool = data.get("tool_name")
    tool_input = data.get("tool_input") or {}
    if tool == "Bash":
        for words in commands(str(tool_input.get("command", ""))):
            target = merge_target(words)
            if target is not None:
                yield target
    elif tool == "mcp__github__merge_pull_request":
        owner, repo, number = tool_input.get("owner"), tool_input.get("repo"), tool_input.get("pullNumber")
        if number is not None:
            yield str(number), (owner + "/" + repo) if owner and repo else None


def fetch(selector, repo, cwd):
    """(files, body) of the PR, or None when gh can't say."""
    cmd = ["gh", "pr", "view"] + ([selector] if selector else []) + (["-R", repo] if repo else [])
    cmd += ["--json", "files,body"]
    try:
        out = subprocess.run(cmd, cwd=cwd or None, capture_output=True, text=True, timeout=8)
    except Exception:
        return None
    if out.returncode != 0:
        return None
    pr = json.loads(out.stdout)
    return [f.get("path", "") for f in pr.get("files") or []], pr.get("body") or ""


def denied(data, fetch=fetch):
    """The PR a merge is denied for, or None."""
    for selector, repo in targets(data):
        pr = fetch(selector, repo, data.get("cwd"))
        if pr is None:
            continue
        files, body = pr
        if not docs_only(files) and not has_review(body):
            return selector or "of this branch"
    return None


def main():
    try:
        pr = denied(json.load(sys.stdin))
    except Exception:
        return
    if pr:
        print(json.dumps({"hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": REASON.format(pr=pr),
        }}))


if __name__ == "__main__":
    main()
