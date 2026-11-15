# Code Generator Architecture Guidance

This document captures the reusable architecture used by the `ThingAndPluginClasses` generator and
is intended as the default blueprint for modernizing or creating other generators in this codebase.

## Core architecture: 4-stage pipeline

Use a strict pipeline with four main classes and one-way data flow:

1. **`Generator` (orchestrator)**
   - Owns Roslyn incremental wiring and source emission.
   - Collects discovery outputs, runs analysis, emits generated files, reports diagnostics.
   - Contains no domain parsing/validation/rendering logic.

2. **`Parser` (discovery)**
   - Finds candidate syntax nodes and resolves raw symbol/attribute inputs.
   - Performs lightweight extraction only (what exists), not policy decisions (what is valid).
   - Produces discovery models plus immediate parse-time diagnostics.

3. **`Analyzer` (semantic analysis + normalization)**
   - Correlates related inputs, applies policy checks, detects conflicts, and validates canonical symbols.
   - Resolves defaults and normalizes data to a deterministic render input shape.
   - Produces finalized render models and analysis diagnostics.

4. **`Renderer` (pure emission)**
   - Consumes only finalized render models.
   - Generates deterministic C# text.
   - Does not query Roslyn symbols, parser helpers, or analyzer internals.

The key rule is: **parse early, analyze once, render from data only**.

## Model layering (required)

Keep model types separated by stage; avoid mixing responsibilities:

- **Discovery models**: parser output (`*Candidate`, discovery result wrappers, raw parse diagnostics).
- **Semantic working models**: analyzer-internal, Roslyn-backed correlation/validation state.
- **Render IR models**: Roslyn-free finalized data (generator-specific DTOs and member specs).

`Renderer` should only accept render IR models. If renderer needs more context, extend render IR,
do not call back into parser/analyzer.

## Diagnostic boundaries

Split diagnostics by where facts are known:

- **Parser diagnostics**: malformed/local attribute usage, unsupported literal shapes.
- **Analyzer diagnostics**: cross-type conflicts, duplicate keys, policy violations, shadowing/canonical symbol checks.
- **Renderer diagnostics**: ideally none; rendering should be a pure transform over valid IR.

Give diagnostics stable IDs, clear category (`Usage`), and messages that explain both the failing
member/type and the violated rule.

## Design guardrails (baked in early)

Guardrails must be part of design, not a post-migration add-on:

- Enforce canonical symbol identity for generator-owned attributes.
- Detect and report shadowed lookalike attributes.
- Validate uniqueness/invariants before rendering (for example save/load keys).
- Keep deterministic ordering for generated members/methods where ordering is observable.

This minimizes hidden drift and makes migration safer.

## Recommended work phases for new generator modernization

Use this sequence when replacing legacy/runtime generators:

1. **Architecture and contract**
   - Define class boundaries, model layers, naming, and target generated surface.
2. **Guardrails and diagnostics**
   - Implement validation rules and stable diagnostics before large rollout.
3. **Parity implementation**
   - Reproduce current behavior in generated output with focused tests.
4. **Migration rollout (last)**
   - Move consumers/templates incrementally, remove legacy runtime path only after parity and checks pass.

Migration is intentionally last: rollout is safer when architecture and guardrails are already stable.

## Verification expectations

For each generator change:

- Build the generator project.
- Build script/game projects that consume it.
- Run targeted tests that exercise generated behavior (fast tier by default, broader tier if behavior touched).
- Confirm diagnostics are actionable and deterministic.

## Anti-patterns to avoid

- Renderer depending on Roslyn symbols or parser/analyzer callbacks.
- A single model type carrying mixed discovery + semantic + render responsibilities.
- Silent fallback behavior that hides invalid input instead of producing diagnostics.
- Large migration before diagnostics/guardrails are in place.
