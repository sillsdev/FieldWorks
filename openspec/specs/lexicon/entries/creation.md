---
spec-id: lexicon/entries/creation
created: 2026-02-05
status: draft
---

# Entry Creation

## Purpose

Capture how new lexicon entries and senses are created using shared UI patterns.

## User Stories

- As a lexicographer, I want to add entries quickly using consistent dialogs.
- As a linguist, I want entry creation to honor shared configuration and validation.

## Context

Entry creation flows through Lexicon UI components and shared lexicon dialogs. This spec captures the shared creation workflow.

## Behavior

- Entry creation uses shared dialogs and menu handlers.
- Entry creation integrates with shared lexicon controls and validation.

### References

- [Lexicon UI](../../../../Src/LexText/Lexicon/AGENTS.md#key-components) — Entry menu handlers and dialogs
- [Lexicon controls](../../../../Src/LexText/LexTextControls/AGENTS.md#key-components) — Insert entry and sense dialogs

## Constraints

- Use shared dialogs for entry creation.
- Avoid bypassing validation logic in shared controls.

## Anti-patterns

- Creating entries via custom dialogs without shared validation.

## Open Questions

- Should entry creation enforce consistent default writing systems?
