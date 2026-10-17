#!/usr/bin/env python3
"""PreToolUse hook: the developer agent may not change tests/ (see wiki Process/Agent-Workflow).

Acts only when the input has agent_type "developer" (set inside subagent calls);
the tester and the main session pass through. A path counts as a test path when
one of its segments is "tests", in any case (`Tests/` too, on every OS, because
macOS filesystems ignore case; the cost is that a `Tests/` directory elsewhere is
protected as well).

Denied for the developer:
  - Edit / Write / MultiEdit (file_path) and NotebookEdit (notebook_path) on a
    test path
  - Bash, in every command of a list split on `;`, `&&`, `||`, `|`, `&`,
    parentheses and newlines: `>`/`>>` redirection to a test path, `sed -i` (GNU
    and BSD `-i ''`), `tee`, `rm`, `mv` (source or target), `cp` with the target
    in tests/, `git checkout` / `restore` / `rm` / `mv` naming a test path (also
    after `git -C <dir>`, `-c k=v` or `--opt=value`)

Not detected (limits): writes through an interpreter (`python -c`, `perl -i`),
`find -delete`, `cp -t tests/`, `dd`, `truncate`, `ln`, `git apply`/`git stash`,
git options taking a separate value other than `-C`/`-c` (`git --git-dir <x> rm
...`, `--work-tree <x>`), commands behind a prefix (`sudo`, `command`, `env`,
`xargs`, `time`, `nohup`), paths built from variables, globs or `cd tests && ...`,
and commands nested in `bash -c`, `eval`, `$(...)`, backticks or here-docs.
`#` does not start a comment, so a commented-out command is still checked.
False positives: a `sed -i` whose expression contains a `/tests/` segment is
denied even on other files, and here-doc lines read as commands, so a body line
like `rm tests/x` is denied.

Malformed input (bad JSON, unparseable shell quoting) is allowed: exit 0, no
output, so a hook bug never blocks the session.
"""
import json
import os
import shlex
import sys

REASON = ("Tests/ belongs to the tester: the developer may not change tests "
          "(enforced by hook). Ask the orchestrator if a test looks wrong.")

SEPARATORS = {";", "&&", "||", "|", "&", "|&", ";;", "(", ")", "\n"}
REDIRECTS = {">", ">>", ">|", "&>", "&>>"}


def is_test_path(path):
    return "tests" in path.lower().split("/")


def args(words):
    return [w for w in words[1:] if not w.startswith("-")]


def command_writes_tests(words):
    while words and "=" in words[0] and not words[0].startswith("="):
        words = words[1:]  # FOO=bar cmd ...
    if not words:
        return False
    cmd = os.path.basename(words[0])
    if cmd == "sed":
        in_place = any(w.startswith("-i") or w.startswith("--in-place")
                       for w in words[1:])
        return in_place and any(is_test_path(w) for w in words[1:])
    if cmd in ("tee", "rm", "mv"):
        return any(is_test_path(w) for w in args(words))
    if cmd == "cp":
        rest = args(words)
        return bool(rest) and is_test_path(rest[-1])
    if cmd == "git":
        rest = words[1:]
        while rest and rest[0].startswith("-"):  # git -C <dir> / -c k=v / --git-dir=…
            rest = rest[2:] if rest[0] in ("-C", "-c") else rest[1:]
        if rest and rest[0] in ("checkout", "restore", "rm", "mv"):
            return any(is_test_path(w) for w in rest[1:])
    return False


def bash_writes_tests(command):
    # newline is a separator, not whitespace; quoted newlines stay in their word
    lexer = shlex.shlex(command, posix=True, punctuation_chars="();<>|&\n")
    lexer.whitespace = " \t\r"
    lexer.whitespace_split = True
    lexer.commenters = ""
    tokens = []
    for tok in lexer:  # ValueError on unbalanced quotes
        if "\n" in tok and set(tok) <= set(lexer.punctuation_chars):
            # a punctuation run such as ";\n" or "\n\n": newline on its own
            tokens += [t for t in tok.replace("\n", " \n ").split(" ") if t]
        else:
            tokens.append(tok)
    words = []
    i = 0
    while i < len(tokens):
        tok = tokens[i]
        if tok in REDIRECTS:
            if i + 1 < len(tokens) and is_test_path(tokens[i + 1]):
                return True
            i += 2
            continue
        if tok in SEPARATORS or tok == "<":
            if tok == "<":
                i += 1  # skip the input file
            elif command_writes_tests(words):
                return True
            else:
                words = []
            i += 1
            continue
        words.append(tok)
        i += 1
    return command_writes_tests(words)


def denied(data):
    if data.get("agent_type") != "developer":
        return False
    tool = data.get("tool_name")
    tool_input = data.get("tool_input") or {}
    if tool in ("Edit", "Write", "MultiEdit"):
        return is_test_path(str(tool_input.get("file_path", "")))
    if tool == "NotebookEdit":
        return is_test_path(str(tool_input.get("notebook_path", "")))
    if tool == "Bash":
        return bash_writes_tests(str(tool_input.get("command", "")))
    return False


def main():
    try:
        deny = denied(json.load(sys.stdin))
    except Exception:
        return
    if deny:
        print(json.dumps({"hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": "deny",
            "permissionDecisionReason": REASON,
        }}))


if __name__ == "__main__":
    main()
