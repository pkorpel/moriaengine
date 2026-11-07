#!/usr/bin/env python3
"""Tests of require_review.py (#1230). Run: python3 scripts/hooks/test_require_review.py"""
import importlib.util
import io
import json
import os
import subprocess
import sys
import unittest
from contextlib import redirect_stdout
from unittest import mock

sys.dont_write_bytecode = True  # no __pycache__ beside the hooks

HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("require_review", os.path.join(HERE, "require_review.py"))
rr = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rr)

CODE = ["moriaengine/scripts/Basic/CharClasses/NPC.cs", "docs/current/x.md"]
DOCS = ["docs/current/x.md", "CLAUDE.md"]
REVIEWED = "## What and why\nfix\n\n## Review\nNo defects.\n"


def bash(command, cwd="/tmp"):
    return {"tool_name": "Bash", "tool_input": {"command": command}, "cwd": cwd}


def pr(files, body):
    return files, len(files), body


def prs(table):
    """A fake fetch: selector -> (files, changed, body); unknown selectors can't be looked up."""
    return lambda selector, repo, cwd: table.get(selector)


class Decision(unittest.TestCase):
    def test_code_without_review_is_denied(self):
        fetch = prs({"7": pr(CODE, "## What and why\nfix\n\n## How it was checked\n- fast tier")})
        self.assertEqual(rr.denied(bash("gh pr merge 7 --squash --admin"), fetch), "7")

    def test_code_with_review_passes(self):
        for body in (REVIEWED, "### Reviews\n- first round: fixed x", "intro\n## review\nok\n## Other\n", "# REVIEW\nclean"):
            self.assertIsNone(rr.denied(bash("gh pr merge 7 --squash"), prs({"7": pr(CODE, body)})), body)

    def test_review_mentioned_but_not_a_heading_is_denied(self):
        for body in ("Review: none", "## Reviewer notes\nx", "the review found nothing", "    ## Review\nx"):
            self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, body)})), "7", body)

    def test_an_empty_review_section_is_denied(self):
        with open(os.path.join(HERE, "..", "..", ".github", "pull_request_template.md")) as f:
            template = f.read()
        self.assertIn("## Review", template)
        for body in (template, "## Review\n", "## Review\n\n## Other\ntext", "## Review\n<!-- what it found -->\n  \n"):
            self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, body)})), "7", body)
        filled = template.replace("-->\n", "-->\nNo defects; one comment clarified.\n", 1)
        self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, filled)})))

    def test_docs_only_passes(self):
        self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({"7": pr(DOCS, "docs")})))

    def test_no_files_is_not_docs_only(self):
        self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr([], "")})), "7")

    def test_a_cut_file_list_is_not_docs_only(self):
        self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": (["a.md"] * 100, 140, "")})), "7")

    def test_lookup_failure_fails_open(self):
        self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({})))

    def test_any_merge_in_a_list_counts(self):
        fetch = prs({"7": pr(CODE, REVIEWED), "8": pr(CODE, "no section")})
        self.assertEqual(rr.denied(bash("gh pr merge 7 --squash && gh pr merge 8 --squash"), fetch), "8")

    def test_other_commands_pass_without_lookup(self):
        def fetch(selector, repo, cwd):
            raise AssertionError("looked up " + str(selector))
        for command in ("gh pr view 7", "gh pr create --body-file x", "echo gh pr merge 7", "git merge main",
                        "gh pr merge 7 --disable-auto"):
            self.assertIsNone(rr.denied(bash(command), fetch), command)

    def test_mcp_merge(self):
        seen = []

        def fetch(selector, repo, cwd):
            seen.append((selector, repo))
            return pr(CODE, "nothing")
        for number in (7, 7.0):
            data = {"tool_name": "mcp__github__merge_pull_request",
                    "tool_input": {"owner": "pkorpel", "repo": "moriaengine", "pullNumber": number}}
            self.assertEqual(rr.denied(data, fetch), "7")
        self.assertEqual(seen, [("7", "pkorpel/moriaengine")] * 2)


class Parsing(unittest.TestCase):
    def targets(self, command, cwd="/tmp"):
        return list(rr.targets(bash(command, cwd)))

    def test_selector_and_repo(self):
        self.assertEqual(self.targets("gh pr merge 1226 --squash --admin"), [("1226", None, "/tmp")])
        self.assertEqual(self.targets('gh pr merge --squash -t "fix 12" -b "x 13" 99'), [("99", None, "/tmp")])
        self.assertEqual(self.targets("gh pr merge -R pkorpel/moriaengine 5"), [("5", "pkorpel/moriaengine", "/tmp")])
        self.assertEqual(self.targets("gh pr merge --repo=o/r https://github.com/o/r/pull/5"),
                         [("https://github.com/o/r/pull/5", "o/r", "/tmp")])
        self.assertEqual(self.targets("GH_TOKEN=x gh pr merge 5"), [("5", None, "/tmp")])

    def test_redirects_keep_the_number(self):
        for command in ("gh pr merge 1226 >/dev/null 2>&1", "gh pr merge 1226 > /tmp/log", "gh pr merge 1226 &>/tmp/x",
                        "gh pr merge 1226 <<< y", "gh pr merge 1226 2>&1 | tail -3", "gh pr merge 2>/dev/null 1226"):
            self.assertEqual(self.targets(command), [("1226", None, "/tmp")], command)

    def test_no_selector_is_the_current_branch(self):
        self.assertEqual(self.targets("gh pr merge --squash --admin 2>&1 | tail -3"), [(None, None, "/tmp")])
        self.assertEqual(self.targets("gh pr merge --squash > /tmp/out.txt\necho done"), [(None, None, "/tmp")])

    def test_the_last_cd_sets_the_directory(self):
        self.assertEqual(self.targets("cd /workspace/moria-12 && gh pr merge --squash", "/workspace"),
                         [(None, None, "/workspace/moria-12")])
        self.assertEqual(self.targets("cd moria-12 && git push && gh pr merge", "/workspace"),
                         [(None, None, "/workspace/moria-12")])
        self.assertEqual(self.targets("cd /a; cd ../b && gh pr merge 3"), [("3", None, "/a/../b")])

    def test_unbalanced_quotes_fail_open(self):
        out = io.StringIO()
        sys.stdin = io.StringIO(json.dumps(bash("gh pr merge 7 'oops")))
        try:
            with redirect_stdout(out):
                rr.main()
        finally:
            sys.stdin = sys.__stdin__
        self.assertEqual(out.getvalue(), "")


class Fetch(unittest.TestCase):
    def run_gh(self, result=None, error=None):
        calls = []

        def run(cmd, **kwargs):
            calls.append((cmd, kwargs.get("cwd")))
            if error:
                raise error
            return result
        with mock.patch.object(rr.subprocess, "run", run):
            got = rr.fetch("7", "o/r", "/w/m")
        return got, calls

    def test_reads_files_count_and_body(self):
        out = subprocess.CompletedProcess([], 0, json.dumps(
            {"files": [{"path": "a.cs"}], "changedFiles": 1, "body": "b"}), "")
        got, calls = self.run_gh(out)
        self.assertEqual(got, (["a.cs"], 1, "b"))
        self.assertEqual(calls, [(["gh", "pr", "view", "7", "-R", "o/r", "--json", "files,changedFiles,body"], "/w/m")])

    def test_failures_are_none(self):
        for result, error in ((subprocess.CompletedProcess([], 1, "", "no pr"), None),
                              (subprocess.CompletedProcess([], 0, "not json", ""), None),
                              (None, FileNotFoundError("gh")),
                              (None, subprocess.TimeoutExpired("gh", 8))):
            self.assertIsNone(self.run_gh(result, error)[0])


class Main(unittest.TestCase):
    def run_main(self, payload, table):
        saved = rr.denied.__defaults__
        rr.denied.__defaults__ = (prs(table),)  # the default is bound at definition, so patch it there
        out = io.StringIO()
        sys.stdin = io.StringIO(payload)
        try:
            with redirect_stdout(out):
                rr.main()
        finally:
            sys.stdin = sys.__stdin__
            rr.denied.__defaults__ = saved
        return out.getvalue()

    def test_deny_output(self):
        decision = json.loads(self.run_main(json.dumps(bash("gh pr merge 7")), {"7": pr(CODE, "")}))["hookSpecificOutput"]
        self.assertEqual(decision["permissionDecision"], "deny")
        self.assertIn("## Review", decision["permissionDecisionReason"])

    def test_allow_and_malformed_print_nothing(self):
        self.assertEqual(self.run_main(json.dumps(bash("gh pr merge 7")), {"7": pr(CODE, REVIEWED)}), "")
        self.assertEqual(self.run_main("not json", {}), "")


if __name__ == "__main__":
    unittest.main()
