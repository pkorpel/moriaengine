---
name: Feature
about: New gameplay or engine behaviour, or an Erebor port
labels: ["type:feature"]
---

<!-- Labels: also set one prio: (1 urgent, 2 next, 3 some day) and the area: labels that apply. -->

## Who and why

As **<role: player, GM, shard admin>** I want **<capability>** so that **<outcome>**.

## Existing system

Filled in before any code (as a comment if the issue is already open). What we plug into, so nothing gets
reinvented:

- **Engine:** how the engine already does this or something close: its page under `moriaengine/docs/current/`,
  the code, and the original (`git show a27bbf6:<path>`).
- **Erebor:** how Erebor did it: archive script/def, and Sphere 0.99z8 behaviour where it matters.
- **Gaps or bugs found:** each becomes its own issue and is fixed first. A broken existing piece is fixed, never
  replaced.

A feature that adds a new system or touches more than one existing one also gets a short design note here, OKed
by the maintainer before building.

## Acceptance criteria

One line each: Given … when … then …

- [ ] Given … when … then …

## Test notes

Which tests prove each criterion: fast tier (`moriaengine/Tests/*.cs`) or full-boot (`moriaengine/Tests/Protocol/*`,
with server-side helpers in `moriaengine/scripts/Tests/`).

## Out of scope
