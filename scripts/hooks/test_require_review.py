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


def pr_by(files, body, author="pkorpel", approved=()):
    """A PR whose author and approvals are known, as fetch returns it."""
    return files, len(files), body, author, set(approved)


CORE = ["moriaengine/GameCore/Timers/Timer.cs", "moriaengine/scripts/Basic/x.cs"]


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

    def test_subsections_belong_to_the_review(self):
        for body in ("## Review\n### Round 1\nfound x, fixed\n### Round 2\nclean\n",
                     "## Review\n### Round 1\nclean\n## Before merge\n- [x] rebased",
                     "## Review\r\n### Round 1\r\nclean\r\n"):
            self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, body)})), body)
        self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, "### Review\n## Other\ntext")})), "7")

    def test_review_mentioned_but_not_a_heading_is_denied(self):
        for body in ("Review: none", "## Reviewer notes\nx", "the review found nothing", "    ## Review\nx"):
            self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, body)})), "7", body)

    def test_an_empty_review_section_is_denied(self):
        with open(os.path.join(HERE, "..", "..", ".github", "pull_request_template.md")) as f:
            template = f.read()
        self.assertIn("## Review", template)
        for body in (template, "## Review\n", "## Review\n\n## Other\ntext", "## Review\n<!-- what it found -->\n  \n"):
            self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, body)})), "7", body)
        for body in ("```\n## Review\nfoo\n```\n", "<!--\n## Review\nfoo\n-->", "#\nReviews are welcome\n"):
            self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, body)})), "7", body)
        at = template.index("## Review")  # fill the comment under the Review heading, not an earlier one
        filled = template[:at] + template[at:].replace("-->\n", "-->\nNo defects; one comment clarified.\n", 1)
        self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({"7": pr(CODE, filled)})))

    def test_docs_need_a_review_too(self):
        # #1559: docs-only PRs are no longer exempt
        self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr(DOCS, "docs")})), "7")
        self.assertIsNone(rr.denied(bash("gh pr merge 7"), prs({"7": pr(DOCS, REVIEWED)})))
        self.assertEqual(rr.denied(bash("gh pr merge 7"), prs({"7": pr([], "")})), "7")

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


class CoreReview(unittest.TestCase):
    """#1559: engine core needs janslav's approval (pkorpel's when janslav wrote it)."""

    def decide(self, files, author="pkorpel", approved=(), body=REVIEWED):
        return rr.denial(bash("gh pr merge 7"), prs({"7": pr_by(files, body, author, approved)}))

    def test_core_without_approval_is_denied(self):
        found = self.decide(CORE)
        self.assertEqual(found[0], "7")
        self.assertIn("janslav", found[1])
        self.assertIn("moriaengine/GameCore/Timers/Timer.cs", found[1])

    def test_core_approved_by_janslav_passes(self):
        self.assertIsNone(self.decide(CORE, approved=["janslav"]))

    def test_another_approver_is_not_enough(self):
        self.assertIsNotNone(self.decide(CORE, approved=["pkorpel", "bozyman3000"]))

    def test_janslavs_own_core_pr_needs_pkorpel(self):
        self.assertIsNotNone(self.decide(CORE, author="janslav", approved=["janslav"]))
        self.assertIsNone(self.decide(CORE, author="janslav", approved=["pkorpel"]))

    def test_every_core_path_counts(self):
        for path in ("moriaengine/Common/Tools.cs", "moriaengine/Communication/TCP/TCPServer.cs",
                     "moriaengine/AuxiliaryServer/Commands.cs",
                     "moriaengine/Tools/SteamEngine.Generators/X.cs",
                     "moriaengine/Tools/SteamEngine.Generators.Tests/Y.cs"):
            self.assertIsNotNone(self.decide([path]), path)

    def test_non_core_and_core_docs_pass_without_approval(self):
        self.assertIsNone(self.decide(["moriaengine/scripts/Basic/x.cs", "moriaengine/Tools/perf/x.cs"]))
        self.assertIsNone(self.decide(["moriaengine/GameCore/README.md", "moriaengine/scripts/Basic/x.cs"]))

    def test_a_missing_review_section_is_reported_first(self):
        found = self.decide(CORE, body="no section")
        self.assertIn("## Review", found[1])

    def test_a_cut_file_list_counts_as_core(self):
        files = ["moriaengine/scripts/Basic/x%d.cs" % i for i in range(100)]
        found = rr.denial(bash("gh pr merge 7"), prs({"7": (files, 140, REVIEWED, "pkorpel", set())}))
        self.assertIn("cut", found[1])
        self.assertIsNone(rr.denial(bash("gh pr merge 7"), prs({"7": (files, 140, REVIEWED, "pkorpel", {"janslav"})})))

    def test_unknown_approvals_fail_open(self):
        self.assertIsNone(rr.denial(bash("gh pr merge 7"), prs({"7": pr(CORE, REVIEWED)})))


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

    def test_reads_files_count_body_author_and_approvals(self):
        out = subprocess.CompletedProcess([], 0, json.dumps(
            {"files": [{"path": "a.cs"}], "changedFiles": 1, "body": "b", "author": {"login": "pkorpel"},
             "latestReviews": [{"author": {"login": "janslav"}, "state": "APPROVED"},
                               {"author": {"login": "jablonskyv"}, "state": "CHANGES_REQUESTED"},
                               {"author": {"login": "x"}, "state": "COMMENTED"}]}), "")
        got, calls = self.run_gh(out)
        self.assertEqual(got, (["a.cs"], 1, "b", "pkorpel", {"janslav"}))
        self.assertEqual(calls, [(["gh", "pr", "view", "7", "-R", "o/r", "--json",
                                   "files,changedFiles,body,author,latestReviews"], "/w/m")])

    def test_failures_are_none(self):
        for result, error in ((subprocess.CompletedProcess([], 1, "", "no pr"), None),
                              (subprocess.CompletedProcess([], 0, "not json", ""), None),
                              (None, FileNotFoundError("gh")),
                              (None, subprocess.TimeoutExpired("gh", 8))):
            self.assertIsNone(self.run_gh(result, error)[0])


class Main(unittest.TestCase):
    def run_main(self, payload, table):
        saved = rr.denial.__defaults__
        rr.denial.__defaults__ = (prs(table),)  # the default is bound at definition, so patch it there
        out = io.StringIO()
        sys.stdin = io.StringIO(payload)
        try:
            with redirect_stdout(out):
                rr.main()
        finally:
            sys.stdin = sys.__stdin__
            rr.denial.__defaults__ = saved
        return out.getvalue()

    def test_deny_output(self):
        decision = json.loads(self.run_main(json.dumps(bash("gh pr merge 7")), {"7": pr(CODE, "")}))["hookSpecificOutput"]
        self.assertEqual(decision["permissionDecision"], "deny")
        self.assertIn("## Review", decision["permissionDecisionReason"])

    def test_core_deny_output(self):
        out = self.run_main(json.dumps(bash("gh pr merge 7")), {"7": pr_by(CORE, REVIEWED)})
        decision = json.loads(out)["hookSpecificOutput"]
        self.assertEqual(decision["permissionDecision"], "deny")
        self.assertIn("needs-core-review", decision["permissionDecisionReason"])

    def test_allow_and_malformed_print_nothing(self):
        self.assertEqual(self.run_main(json.dumps(bash("gh pr merge 7")), {"7": pr(CODE, REVIEWED)}), "")
        self.assertEqual(self.run_main("not json", {}), "")


if __name__ == "__main__":
    unittest.main()
