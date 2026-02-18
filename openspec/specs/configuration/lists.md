---
spec-id: configuration/lists
created: 2026-02-05
status: draft
---

# Lists

## Purpose

Document how shared lists and configuration lists are managed across FieldWorks.

## User Stories

- As an administrator, I want to manage shared lists so that all tools use the same categories.
- As a lexicographer, I want list changes to flow into entry and text workflows.

## Context

List editing appears across multiple dialogs and shared UI. This spec captures the cross-cutting expectations for list storage and usage.

## Behavior

- List configuration uses shared dialogs and settings infrastructure.
- List updates propagate to lexicon and text UI without custom per-module storage.

### References

- [Framework settings](../../../Src/Common/Framework/AGENTS.md#key-components) — Shared settings and list storage
- [Shared dialogs](../../../Src/FwCoreDlgs/AGENTS.md#key-components) — Configuration dialogs

## Constraints

- Lists should be centrally stored and reused across modules.
- Avoid duplicating list definitions in module-specific stores.

## Anti-patterns

- Module-specific list storage without synchronization.

## Open Questions

- Do we need a standard list migration workflow across releases?
