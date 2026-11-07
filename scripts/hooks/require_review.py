#!/usr/bin/env python3
"""PreToolUse hook: no merge of a code change without a review (#1230, see CLAUDE.md
"Workflow conventions").

Every code change gets an independent review agent before it merges, and the PR body
says what the review found under a `## Review` heading (`## Reviews`, any heading
level, any case) with some text under it: an HTML comment alone, as in the PR
template, doesn't count. This hook checks that section at the merge:

  - Bash: every `gh pr merge` in the command (split on `;`, `&&`, `||`, `|`, `&`,
    parentheses and newlines), with its PR as given (number, URL or branch; none =
    the current branch's PR, looked up in the directory of the last `cd` before it,
    else the hook's cwd) and its `-R/--repo`; `--disable-auto` merges nothing;
  - mcp__github__merge_pull_request: owner, repo, pullNumber.

The PR's files and body come from `gh pr view --json files,changedFiles,body`. A PR
whose files are all `*.md` is docs only and passes (when gh lists every file: it
stops at 100). Anything else without the section is denied. The body is read before
the command runs, so `gh pr edit --body-file x && gh pr merge` in one call is denied:
edit first, merge in the next call.

Fails open: malformed input, unbalanced quotes anywhere in the command (an apostrophe
in a here-doc too), a missing `gh`, a failed or slow lookup (8 s each, the hook's
whole budget is 15 s in settings.json) all allow the call, so a hook bug or a network
hiccup never blocks the session. Not detected: merges through `gh api`, `git push` to
main (denied in settings.json anyway), or a `gh pr merge` nested in `bash -c`, `eval`
or backticks, or behind `sudo`/`env`/`xargs`/`time`/`command`/`nohup`. False
positives: here-doc lines read as commands, so a body line `gh pr merge 9` is looked
up as a merge too.
"""
import json
import os
import re
import shlex
import subprocess
import sys

REASON = ("PR {pr} changes code but its body has no '## Review' section with text under it. "
          "Every code change gets an independent review agent before merge (#1230, CLAUDE.md): "
          "run the review, fix or answer its findings, add '## Review' with what it found to "
          "the PR body (gh pr edit --body-file, in its own call), then merge.")

# gh pr merge flags that take a separate value
VALUE_FLAGS = {"-b", "--body", "-F", "--body-file", "-t", "--subject", "-R", "--repo",
               "--match-head-commit", "-A", "--author-email"}
HEADING = re.compile(r"^\s{0,3}#{1,6}\s", re.MULTILINE)
REVIEW_HEADING = re.compile(r"^\s{0,3}#{1,6}\s*reviews?\b.*$", re.IGNORECASE | re.MULTILINE)
COMMENT = re.compile(r"<!--.*?(-->|$)", re.DOTALL)
FD_BEFORE_REDIRECT = re.compile(r"(?:(?<=\s)|^)\d+(?=[<>])")  # the 2 of 2>&1, not a PR number


def has_review(body):
    """A review heading with text under it, up to the next heading."""
    body = body or ""
    for heading in REVIEW_HEADING.finditer(body):
        following = HEADING.search(body, heading.end())
        section = body[heading.end():following.start() if following else len(body)]
        if COMMENT.sub("", section).strip():
            return True
    return False


def docs_only(files, changed):
    return bool(files) and len(files) == changed and all(f.lower().endswith(".md") for f in files)


def commands(command):
    # newline is a separator, not whitespace; quoted newlines stay in their word
    lexer = shlex.shlex(FD_BEFORE_REDIRECT.sub("", command), posix=True, punctuation_chars="();<>|&\n")
    lexer.whitespace = " \t\r"
    lexer.whitespace_split = True
    lexer.commenters = ""
    words = []
    skip = False
    for tok in lexer:  # ValueError on unbalanced quotes
        punctuation = set(tok) <= set(lexer.punctuation_chars)
        if punctuation and any(c in tok for c in "<>"):
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
    if "--disable-auto" in words:
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
    """(selector, repo, cwd) of each merge."""
    tool = data.get("tool_name")
    tool_input = data.get("tool_input") or {}
    cwd = data.get("cwd")
    if tool == "Bash":
        for words in commands(str(tool_input.get("command", ""))):
            if words[0] == "cd" and len(words) == 2:
                cwd = os.path.join(cwd or os.getcwd(), os.path.expanduser(words[1]))
                continue
            target = merge_target(words)
            if target is not None:
                yield target + (cwd,)
    elif tool == "mcp__github__merge_pull_request":
        owner, repo, number = tool_input.get("owner"), tool_input.get("repo"), tool_input.get("pullNumber")
        if number is not None:
            yield str(int(number)), (owner + "/" + repo) if owner and repo else None, cwd


def fetch(selector, repo, cwd):
    """(files, changed file count, body) of the PR, or None when gh can't say."""
    cmd = ["gh", "pr", "view"] + ([selector] if selector else []) + (["-R", repo] if repo else [])
    cmd += ["--json", "files,changedFiles,body"]
    try:
        out = subprocess.run(cmd, cwd=cwd or None, capture_output=True, text=True, timeout=8)
        if out.returncode != 0:
            return None
        pr = json.loads(out.stdout)
        return [f.get("path", "") for f in pr.get("files") or []], pr.get("changedFiles"), pr.get("body") or ""
    except Exception:
        return None


def denied(data, fetch=fetch):
    """The PR a merge is denied for, or None."""
    for selector, repo, cwd in targets(data):
        pr = fetch(selector, repo, cwd)
        if pr is None:
            continue
        files, changed, body = pr
        if not docs_only(files, changed) and not has_review(body):
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
