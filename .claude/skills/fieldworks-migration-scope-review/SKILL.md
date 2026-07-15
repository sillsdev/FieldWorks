---
name: fieldworks-migration-scope-review
description: "Review the scope and evidence claims of large FieldWorks migration PRs, OpenSpec changes, and foundational branches. Use when sizing or splitting a branch, judging draft-PR readiness, verifying that checked tasks match their evidence, or whenever a reviewer or author asks whether a migration PR is too big, mixed, or trustworthy."
---

# FieldWorks Migration Scope Review

## Review Posture

Treat foundational migration PRs as architecture and evidence packages.
The main question is whether reviewers can trust the scope, claims, and
validation boundary.

## Required Checks

- Scope review is branch-relative: compare `main..HEAD` or the merge-base
  diff, not calendar-time commit lists. Same-day commits already on `main`
  are not branch scope.
- Compare PR title/body/tasks against the actual diff.
- Classify files as plan/spec, characterization test, infrastructure,
  prototype, product behavior, or unrelated change.
- When product or global UI wiring appears, trace preview-vs-product
  routing and host/listener wiring separately from plan/test changes
  (apply `fieldworks-ui-wiring-review`).
- Verify checked tasks match evidence language; downgrade claims when
  evidence says substitute, placeholder, skipped, future, partial, or
  live-verification-only — the taxonomy is defined in
  `fieldworks-winforms-to-avalonia-migration/references/parity-evidence.md`
  §"Evidence language". A behavior/workflow claim with no headless integration
  scenario (parity-evidence.md §2a) when one is feasible is a downgrade.
- Confirm validation gates are explicit: OpenSpec validation
  (`openspec validate <change> --strict`), targeted tests, normal
  `./build.ps1` and `./test.ps1` coverage for Avalonia, and
  `CI: Full local check` when ready.

## Split Triggers

- Product-visible behavior appears in a planning/test PR.
- Branch-only diff mixes product-visible wiring with planning/test/docs/
  prototype work.
- Common infrastructure directly depends on the first feature module
  without an explicit decision.
- Test-runner/build graph changes are mixed with UI migration work.
- Unrelated behavior changes require their own review context.
- A multi-surface derisk branch carries more than one canonical screen per UI primitive,
  or ships work that should be documented-and-backed-out (see the hub skill's "Phase-1
  Landing Strategy"). XL surfaces that have their own OpenSpec change belong in their own
  follow-up PR, not folded into the spine PR.

## Review Red Flags

- A draft PR so broad that each reviewer must reverse-engineer intent.
- Scope complaints based on "commits made today" instead of the
  branch-only diff against `main`.
- Evidence stale after rebase or differing from visible CI state.
- A prototype wired as if it were a product feature.
- A "backed-out" / "unwired" claim taken from class names, comments, or an Explore
  summary rather than the product call site — Explore sweeps have produced false "unwired"
  negatives. Require a quoted `file:line` for every wiring claim.
- Skill/playbook updates from the migration retrospective missing from a
  PR that completed a migration phase (see the hub skill's workflow
  step 10) — institutional knowledge is part of the deliverable.

## Handoff

Lead with blockers, then list what to remove, split, reword, or validate
before review. Call out false scope signals separately from real
branch-only scope problems.

## Keep This Skill Current

When a new split trigger, evidence-language term, or scope failure mode
shows up in a real review, add it here in the same PR; route durable
lessons through
`fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
