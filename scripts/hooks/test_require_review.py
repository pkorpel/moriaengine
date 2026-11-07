#!/usr/bin/env python3
"""Tests of require_review.py (#1230). Run: python3 scripts/hooks/test_require_review.py"""
import importlib.util
import io
import json
import os
import sys
import unittest
from contextlib import redirect_stdout

sys.dont_write_bytecode = True  # no __pycache__ beside the hooks
spec = importlib.util.spec_from_file_location(
    "require_review", os.path.join(os.path.dirname(os.path.abspath(__file__)), "require_review.py"))
rr = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rr)

CODE = ["moriaengine/scripts/Basic/CharClasses/NPC.cs", "docs/current/x.md"]
DOCS = ["docs/current/x.md", "CLAUDE.md"]


def bash(command):
    return {"tool_name": "Bash", "tool_input": {"command": command}, "cwd": "/tmp"}


def prs(table):
    """A fake fetch: selector -> (files, body); unknown selectors can't be looked up."""
    return lambda selector, repo, cwd: table.get(selector)


class Decision(unittest.TestCase):
    def test_code_without_review_is_denied(self):
        fetch = prs({"7": (CODE, "## What and why\nfix\n\n## How it was checked\n- fast tier")})
        self.assertEqual(rr.denied(bash("gh pr merge 7 --squash --admin"), fetch), "7")

    def test_code_with_review_passes(self):
        for body in ("## Review\nno defects", "### Reviews\n- first", "intro\n## review\nok", "# REVIEW"):
            self.assertIsNone(rr.denied(bash("gh pr merge 7 --squash"), prs({"7": (CODE, body)})), body)

    def test_review_mentioned_but_not_a_heading_is_denied(self):
        for body in ("Review: none", "## Reviewer notes", "the review found nothing", "    ## Review"):
            self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": (CODE, body)})), "7", body)

    def test_docs_only_passes(self):
        self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({"7": (DOCS, "docs")})))

    def test_no_files_is_not_docs_only(self):
        self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": ([], "")})), "7")

    def test_lookup_failure_fails_open(self):
        self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({})))

    def test_any_merge_in_a_list_counts(self):
        fetch = prs({"7": (CODE, "## Review\nok"), "8": (CODE, "no section")})
        self.assertEqual(rr.denied(bash("gh pr merge 7 --squash && gh pr merge 8 --squash"), fetch), "8")

    def test_other_commands_pass_without_lookup(self):
        def fetch(selector, repo, cwd):
            raise AssertionError("looked up " + str(selector))
        for command in ("gh pr view 7", "gh pr create --body-file x", "echo gh pr merge 7", "git merge main"):
            self.assertIsNone(rr.denied(bash(command), fetch), command)

    def test_mcp_merge(self):
        data = {"tool_name": "mcp__github__merge_pull_request",
                "tool_input": {"owner": "pkorpel", "repo": "moriaengine", "pullNumber": 7}}
        seen = []

        def fetch(selector, repo, cwd):
            seen.append((selector, repo))
            return CODE, "nothing"
        self.assertEqual(rr.denied(data, fetch), "7")
        self.assertEqual(seen, [("7", "pkorpel/moriaengine")])


class Parsing(unittest.TestCase):
    def targets(self, command):
        return list(rr.targets(bash(command)))

    def test_selector_and_repo(self):
        self.assertEqual(self.targets("cd /w/m && gh pr merge 1226 --squash --admin"), [("1226", None)])
        self.assertEqual(self.targets('gh pr merge --squash -t "fix 12" -b "x 13" 99'), [("99", None)])
        self.assertEqual(self.targets("gh pr merge -R pkorpel/moriaengine 5"), [("5", "pkorpel/moriaengine")])
        self.assertEqual(self.targets("gh pr merge --repo=o/r https://github.com/o/r/pull/5"),
                         [("https://github.com/o/r/pull/5", "o/r")])
        self.assertEqual(self.targets("GH_TOKEN=x gh pr merge 5"), [("5", None)])

    def test_no_selector_is_the_current_branch(self):
        self.assertEqual(self.targets("gh pr merge --squash --admin 2>&1 | tail -3"), [(None, None)])
        self.assertEqual(self.targets("gh pr merge --squash > /tmp/out.txt\necho done"), [(None, None)])

    def test_unbalanced_quotes_fail_open(self):
        out = io.StringIO()
        sys.stdin = io.StringIO(json.dumps(bash("gh pr merge 7 'oops")))
        try:
            with redirect_stdout(out):
                rr.main()
        finally:
            sys.stdin = sys.__stdin__
        self.assertEqual(out.getvalue(), "")


class Main(unittest.TestCase):
    def run_main(self, payload, table):
        saved = rr.fetch
        rr.denied.__defaults__ = (prs(table),)
        out = io.StringIO()
        sys.stdin = io.StringIO(payload)
        try:
            with redirect_stdout(out):
                rr.main()
        finally:
            sys.stdin = sys.__stdin__
            rr.denied.__defaults__ = (saved,)
        return out.getvalue()

    def test_deny_output(self):
        decision = json.loads(self.run_main(json.dumps(bash("gh pr merge 7")), {"7": (CODE, "")}))["hookSpecificOutput"]
        self.assertEqual(decision["permissionDecision"], "deny")
        self.assertIn("## Review", decision["permissionDecisionReason"])

    def test_allow_and_malformed_print_nothing(self):
        self.assertEqual(self.run_main(json.dumps(bash("gh pr merge 7")), {"7": (CODE, "## Review\nok")}), "")
        self.assertEqual(self.run_main("not json", {}), "")


if __name__ == "__main__":
    unittest.main()
