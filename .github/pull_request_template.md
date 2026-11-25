Closes #

## What and why

## How it was checked

<!-- Only what really ran, on the final code. -->

- [ ] New test, red-checked: it fails with the fix reverted (A/B)
- [ ] Fast tier passed (pre-push or `scripts/test-fast.sh`)
- [ ] Protocol classes for the area (`scripts/test-full.sh "<filter>"`) passed, or not needed because: 
- [ ] Not tried in a real client (the issue gets `needs-in-game-check`), or tried in: 

## Review

<!-- Required for every change, docs included (#1230, #1559; the merge hook denies an empty section):
what the independent review agent found and what changed because of it, "no defects" included. -->

## Before merge

- [ ] Docs updated in this PR: `moriaengine/docs/current/` for a changed system, `docs/wiki/` for scripts, CI,
      hooks or process (pages: )
- [ ] Engine core touched (`GameCore/`, `Common/`, `Communication/`, `AuxiliaryServer/`, the generators):
      labelled `needs-core-review` and approved by janslav
