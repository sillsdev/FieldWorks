---
name: fieldworks-migration-scope-review
description: Use when reviewing large FieldWorks migration PRs, OpenSpec changes, foundational branches, scope splits, draft PR readiness, or evidence claims.
---

# FieldWorks Migration Scope Review

## Review Posture
Treat foundational migration PRs as architecture and evidence packages. The main question is whether reviewers can trust the scope, claims, and validation boundary.

## Required Checks
- Compare PR title/body/tasks against the actual diff.
- Classify files as plan/spec, characterization test, infrastructure, prototype, product behavior, or unrelated change.
- Verify checked tasks match evidence language; downgrade claims when evidence says substitute, placeholder, skipped, future, or partial.
- Confirm validation gates are explicit: OpenSpec validation, targeted tests, `./build.ps1`, and `CI: Full local check` when ready.

## Split Triggers
- Product-visible behavior appears in a planning/test PR.
- Common infrastructure directly depends on the first feature module without an explicit decision.
- Test-runner/build graph changes are mixed with UI migration work.
- Unrelated behavior changes require their own review context.

## Review Red Flags
- A draft PR is so broad that each reviewer must reverse-engineer intent.
- Evidence is stale after rebase or differs from visible CI state.
- A prototype is wired as if it were a product feature.

## Handoff
Lead with blockers, then list what to remove, split, reword, or validate before review.